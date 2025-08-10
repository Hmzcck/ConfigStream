using ConfigStream.Core.Interfaces;
using ConfigStream.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ConfigStream.Core.Services;

public class FileCacheService : IFileCacheService
{
    private readonly ILogger<FileCacheService> _logger;
    private readonly string _cacheDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileCacheService(ILogger<FileCacheService> logger)
    {
        _logger = logger;
        _cacheDirectory = Path.Combine(Path.GetTempPath(), "DynamicConfig");
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        Directory.CreateDirectory(_cacheDirectory);
    }

    public async Task<IEnumerable<ConfigurationItem>> GetAllConfigurationsAsync(string applicationName)
    {
        List<ConfigurationItem> configurations = new List<ConfigurationItem>();
        string appDirectory = Path.Combine(_cacheDirectory, SanitizeFileName(applicationName));

        if (!Directory.Exists(appDirectory))
            return configurations;

        string[] files = Directory.GetFiles(appDirectory, "*.json");
        foreach (string file in files)
        {
            try
            {
                string json = await File.ReadAllTextAsync(file);
                ConfigurationItem? config = JsonSerializer.Deserialize<ConfigurationItem>(json, _jsonOptions);
                if (config != null)
                    configurations.Add(config);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read cache file {File}", file);
            }
        }

        return configurations;
    }

    public async Task<ConfigurationItem?> GetConfigurationAsync(string applicationName, string key)
    {
        try
        {
            string filePath = GetCacheFilePath(applicationName, key);
            if (!File.Exists(filePath))
                return null;

            string json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<ConfigurationItem>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read cache file for {Key}", key);
            return null;
        }
    }

    public async Task SaveConfigurationAsync(string applicationName, string key, ConfigurationItem configuration)
    {
        try
        {
            string filePath = GetCacheFilePath(applicationName, key);
            string json = JsonSerializer.Serialize(configuration, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);

            _logger.LogDebug("Saved configuration {Key} to cache", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration {Key} to cache", key);
        }
    }

    private string GetCacheFilePath(string applicationName, string key)
    {
        string appDirectory = Path.Combine(_cacheDirectory, SanitizeFileName(applicationName));
        Directory.CreateDirectory(appDirectory);
        return Path.Combine(appDirectory, $"{SanitizeFileName(key)}.json");
    }

    private static string SanitizeFileName(string fileName)
    {
        return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
    }
}
