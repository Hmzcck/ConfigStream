using ConfigStream.Core.Interfaces;
using ConfigStream.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace ConfigStream.Core.Services;

public class ConfigurationReader : IConfigurationReader, IDisposable
{
    private readonly ITypeConverterService _typeConverter;
    private readonly string _applicationName;
    private readonly string _connectionString;
    private readonly int _refreshIntervalMs;
    private IConfigurationStorage? _mongoStorage;
    private readonly IFileCacheService _fileCacheService;
    private readonly Timer? _refreshTimer;
    private bool _disposed;
    private int _refreshInProgress = 0;

    public ConfigurationReader(string applicationName, string connectionString, int refreshTimerIntervalInMs = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _applicationName = applicationName;
        _connectionString = connectionString;
        _refreshIntervalMs = refreshTimerIntervalInMs;
        _typeConverter = new TypeConverterService();

        _fileCacheService = new FileCacheService(NullLogger<FileCacheService>.Instance);

        if (_refreshIntervalMs > 0)
        {
            _refreshTimer = new Timer(async _ =>
            {
                if (Interlocked.CompareExchange(ref _refreshInProgress, 1, 0) == 1)
                    return; // Another refresh is already running

                try
                {
                    await RefreshConfigurationsCallback();
                }
                catch (Exception)
                {
                    // Log error but don't crash the timer
                }
                finally
                {
                    Interlocked.Exchange(ref _refreshInProgress, 0);
                }
            }, null, TimeSpan.FromMilliseconds(_refreshIntervalMs), TimeSpan.FromMilliseconds(_refreshIntervalMs));
        }
    }

    public T? GetValue<T>(string key)
    {
        IConfigurationStorage? mongoStorage = GetMongoStorage();
        if (mongoStorage != null)
        {
            try
            {
                ConfigurationItem? config = Task.Run(async () => await mongoStorage.GetAsync(_applicationName, key))
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                if (config != null && config.IsActive == 1)
                {
                    try
                    {
                        Task.Run(async () =>
                                await _fileCacheService.SaveConfigurationAsync(_applicationName, key, config))
                            .ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // Ignore cache save failures
                    }

                    return _typeConverter.Convert<T>(config.Value, config.Type);
                }
            }
            catch (Exception)
            {
                // MongoDB failed, fall back to file cache
            }
        }

        // No storage connection
        try
        {
            ConfigurationItem? fileConfig = Task
                .Run(async () => await _fileCacheService.GetConfigurationAsync(_applicationName, key))
                .ConfigureAwait(false).GetAwaiter().GetResult();

            if (fileConfig != null && fileConfig.IsActive == 1)
            {
                return _typeConverter.Convert<T>(fileConfig.Value, fileConfig.Type);
            }
        }
        catch (Exception)
        {
            // File cache also failed
        }

        return default;
    }

    public async Task<T?> GetValueAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Try MongoDB storage first
        IConfigurationStorage? mongoStorage = GetMongoStorage();
        if (mongoStorage != null)
        {
            try
            {
                ConfigurationItem? config = await mongoStorage.GetAsync(_applicationName, key, cancellationToken);
                if (config != null && config.IsActive == 1)
                {
                    // Don't block main flow
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _fileCacheService.SaveConfigurationAsync(_applicationName, key, config,
                                CancellationToken.None);
                        }
                        catch (Exception)
                        {
                            // Ignore cache save failures
                        }
                    }, CancellationToken.None);

                    try
                    {
                        return _typeConverter.Convert<T>(config.Value, config.Type);
                    }
                    catch (Exception)
                    {
                        // Type conversion failed, try file cache
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // MongoDB failed, fall back to file cache
            }
        }

        // Fallback to file cache
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConfigurationItem? fileConfig =
                await _fileCacheService.GetConfigurationAsync(_applicationName, key, cancellationToken);

            if (fileConfig != null && fileConfig.IsActive == 1)
            {
                try
                {
                    return _typeConverter.Convert<T>(fileConfig.Value, fileConfig.Type);
                }
                catch (Exception)
                {
                    // Type conversion failed
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // File cache also failed
        }

        return default;
    }

    private IConfigurationStorage? GetMongoStorage()
    {
        if (_mongoStorage != null)
            return _mongoStorage;

        try
        {
            Assembly mongoAssembly = Assembly.Load("ConfigStream.MongoDb");
            Type? mongoStorageType = mongoAssembly.GetType(
                "ConfigStream.MongoDb.MongoConfigurationStorage");

            _mongoStorage = (IConfigurationStorage)Activator.CreateInstance(
                mongoStorageType!, _connectionString)!;

            return _mongoStorage;
        }
        catch (Exception)
        {
            // Failed to initialize MongoDB storage - application will run in offline mode
            return null;
        }
    }

    private async Task RefreshConfigurationsCallback()
    {
        if (_disposed) return;

        try
        {
            IConfigurationStorage? mongoStorage = GetMongoStorage();
            if (mongoStorage == null)
            {
                return;
            }

            using CancellationTokenSource cancellationTokenSource =
                new CancellationTokenSource(TimeSpan.FromMinutes(5));
            IEnumerable<ConfigurationItem> allConfigurations =
                await mongoStorage.GetAllAsync(_applicationName, cancellationTokenSource.Token);

            var activeConfigs = allConfigurations.Where(c => c.IsActive == 1).ToList();

            // offline fallback
            if (activeConfigs.Count > 0)
            {
                try
                {
                    await _fileCacheService.SaveAllConfigurationsAsync(_applicationName, activeConfigs,
                        cancellationTokenSource.Token);
                }
                catch (Exception)
                {
                    // Failed to save configurations to file cache
                }
            }
        }
        catch (Exception)
        {
            // Configuration refresh failed
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _refreshTimer?.Dispose();
        (_fileCacheService as IDisposable)?.Dispose();
        (_mongoStorage as IDisposable)?.Dispose();

        _disposed = true;
    }
}
