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
    private IConfigurationStorage? _mongoStorage;
    private readonly IFileCacheService _fileCacheService;
    private bool _disposed;

    public ConfigurationReader(string applicationName, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _applicationName = applicationName;
        _connectionString = connectionString;
        _typeConverter = new TypeConverterService();

        _fileCacheService = new FileCacheService(NullLogger<FileCacheService>.Instance);
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

    public void Dispose()
    {
        if (_disposed) return;

        (_fileCacheService as IDisposable)?.Dispose();
        (_mongoStorage as IDisposable)?.Dispose();

        _disposed = true;
    }
}
