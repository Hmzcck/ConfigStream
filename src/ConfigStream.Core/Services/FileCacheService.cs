using ConfigStream.Core.Interfaces;
using ConfigStream.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace ConfigStream.Core.Services;

public class FileCacheService : IFileCacheService
{
    private readonly ILogger<FileCacheService> _logger;
    private readonly string _cacheDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _fileLock;
    private readonly TimeSpan _defaultCacheExpiry;

    public FileCacheService(ILogger<FileCacheService> logger)
    {
        _logger = logger;
        _cacheDirectory = Path.Combine(Path.GetTempPath(), "DynamicConfig");
        _fileLock = new SemaphoreSlim(1, 1);
        _defaultCacheExpiry = TimeSpan.FromHours(24);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        EnsureCacheDirectoryExists();
    }


    public async Task<ConfigurationItem?> GetConfigurationAsync(string applicationName, string key,
        CancellationToken cancellationToken = default)

    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            CachedConfiguration? cachedConfig = await LoadCachedConfigurationAsync(applicationName, cancellationToken);

            if (cachedConfig == null || IsExpired(cachedConfig))
            {
                _logger.LogDebug("Cache expired or not found for {ApplicationName}", applicationName);
                return null;
            }

            if (cachedConfig.Configurations.TryGetValue(key, out CachedConfigurationItem? cachedItem))
            {
                return ConvertToConfigurationItem(key, applicationName, cachedItem);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read configuration {Key} from cache for {ApplicationName}", key,
                applicationName);
            return null;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SaveConfigurationAsync(string applicationName, string key, ConfigurationItem configuration,
        CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            CachedConfiguration cachedConfig = await LoadCachedConfigurationAsync(applicationName, cancellationToken) ??
                                               CreateNewCachedConfiguration(applicationName);

            cachedConfig.Configurations[key] = new CachedConfigurationItem
            {
                Value = configuration.Value,
                Type = configuration.Type.ToString(),
                IsActive = configuration.IsActive,
                CachedAt = DateTime.UtcNow
            };

            cachedConfig.LastUpdated = DateTime.UtcNow;
            cachedConfig.ExpiresAt = DateTime.UtcNow.Add(_defaultCacheExpiry);

            await SaveCachedConfigurationAsync(applicationName, cachedConfig, cancellationToken);

            _logger.LogDebug("Saved configuration {Key} to cache for {ApplicationName}", key, applicationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration {Key} to cache for {ApplicationName}", key,
                applicationName);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<IEnumerable<ConfigurationItem>> GetAllConfigurationsAsync(string applicationName,
        CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            CachedConfiguration? cachedConfig = await LoadCachedConfigurationAsync(applicationName, cancellationToken);

            if (cachedConfig == null || IsExpired(cachedConfig))
            {
                _logger.LogDebug("Cache expired or not found for {ApplicationName}", applicationName);
                return [];
            }

            return cachedConfig.Configurations.Select(kvp =>
                ConvertToConfigurationItem(kvp.Key, applicationName, kvp.Value)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load all configurations from cache for {ApplicationName}",
                applicationName);
            return [];
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SaveAllConfigurationsAsync(string applicationName, IEnumerable<ConfigurationItem> configurations,
        CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            CachedConfiguration cachedConfig = CreateNewCachedConfiguration(applicationName);

            foreach (ConfigurationItem config in configurations)
            {
                cachedConfig.Configurations[config.Name] = new CachedConfigurationItem
                {
                    Value = config.Value,
                    Type = config.Type.ToString(),
                    IsActive = config.IsActive,
                    CachedAt = DateTime.UtcNow
                };
            }

            await SaveCachedConfigurationAsync(applicationName, cachedConfig, cancellationToken);

            _logger.LogDebug("Saved {Count} configurations to cache for {ApplicationName}",
                cachedConfig.Configurations.Count, applicationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save all configurations to cache for {ApplicationName}", applicationName);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task CleanupExpiredCacheAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_cacheDirectory))
            return;

        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            string[] files = Directory.GetFiles(_cacheDirectory, "*_config.json");
            int cleanupCount = 0;

            foreach (string file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    string json = await File.ReadAllTextAsync(file, cancellationToken);
                    CachedConfiguration? cachedConfig =
                        JsonSerializer.Deserialize<CachedConfiguration>(json, _jsonOptions);

                    if (cachedConfig != null && IsExpired(cachedConfig))
                    {
                        File.Delete(file);
                        cleanupCount++;
                        _logger.LogDebug("Deleted expired cache file: {FilePath}", file);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process cache file {File} during cleanup", file);
                    try
                    {
                        File.Delete(file);
                        cleanupCount++;
                    }
                    catch
                    {
                        //TODO: Implement later
                    }
                }
            }

            if (cleanupCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired cache files", cleanupCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired cache files");
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task ClearCacheAsync(string applicationName, CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            string filePath = GetCacheFilePath(applicationName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogDebug("Cleared cache for {ApplicationName}", applicationName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cache for {ApplicationName}", applicationName);
        }
        finally
        {
            _fileLock.Release();
        }
    }


    private async Task<CachedConfiguration?> LoadCachedConfigurationAsync(string applicationName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string filePath = GetCacheFilePath(applicationName);

        if (!File.Exists(filePath))
            return null;
        try
        {
            string json = await File.ReadAllTextAsync(filePath, cancellationToken);
            return JsonSerializer.Deserialize<CachedConfiguration>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in cache file for {ApplicationName}", applicationName);
            return null;
        }
    }

    private async Task SaveCachedConfigurationAsync(string applicationName, CachedConfiguration cachedConfig,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string filePath = GetCacheFilePath(applicationName);
        string json = JsonSerializer.Serialize(cachedConfig, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    private CachedConfiguration CreateNewCachedConfiguration(string applicationName)
    {
        return new CachedConfiguration
        {
            ApplicationName = applicationName,
            LastUpdated = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(_defaultCacheExpiry),
            Configurations = new Dictionary<string, CachedConfigurationItem>()
        };
    }

    private ConfigurationItem ConvertToConfigurationItem(string key, string applicationName,
        CachedConfigurationItem cachedItem)
    {
        return new ConfigurationItem
        {
            Name = key,
            ApplicationName = applicationName,
            Value = cachedItem.Value,
            Type = Enum.Parse<ConfigurationType>(cachedItem.Type, true),
            IsActive = cachedItem.IsActive
        };
    }

    private bool IsExpired(CachedConfiguration cachedConfig)
    {
        return DateTime.UtcNow > cachedConfig.ExpiresAt;
    }

    private string GetCacheFilePath(string applicationName)
    {
        string sanitizedName = SanitizeFileName(applicationName);
        return Path.Combine(_cacheDirectory, $"{sanitizedName}_config.json");
    }

    private static string SanitizeFileName(string fileName)
    {
        HashSet<char> invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());
        StringBuilder sanitized = new();
        foreach (char ch in fileName)
        {
            sanitized.Append(!invalidChars.Contains(ch) ? ch : '_');
        }

        return sanitized.ToString();
    }

    private void EnsureCacheDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
                _logger.LogDebug("Created cache directory: {CacheDirectory}", _cacheDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create cache directory: {CacheDirectory}", _cacheDirectory);
            throw;
        }
    }
}
