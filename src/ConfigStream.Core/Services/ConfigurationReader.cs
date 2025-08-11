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
            _refreshTimer = new Timer(RefreshConfigurationsCallback, null,
                TimeSpan.FromMilliseconds(_refreshIntervalMs), TimeSpan.FromMilliseconds(_refreshIntervalMs));
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

    private async void RefreshConfigurationsCallback(object? state)
    {
        if (_disposed) return;

        try
        {
            var mongoStorage = GetMongoStorage();
            if (mongoStorage == null)
            {
                return;
            }

            var allConfigurations = await mongoStorage.GetAllAsync(_applicationName);

            var activeConfigs = new List<ConfigurationItem>();

            foreach (var config in allConfigurations)
            {
                if (config.IsActive == 1)
                {
                    activeConfigs.Add(config);
                }
            }

            // offline fallback
            if (activeConfigs.Count > 0)
            {
                try
                {
                    await _fileCacheService.SaveAllConfigurationsAsync(_applicationName, activeConfigs);
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
