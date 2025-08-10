using ConfigStream.Core.Models;

namespace ConfigStream.Core.Interfaces;

public interface IFileCacheService
{
  Task<ConfigurationItem?> GetConfigurationAsync(string applicationName, string key);
  Task SaveConfigurationAsync(string applicationName, string key, ConfigurationItem configuration);
  Task<IEnumerable<ConfigurationItem>> GetAllConfigurationsAsync(string applicationName);
  Task SaveAllConfigurationsAsync(string applicationName, IEnumerable<ConfigurationItem> configurations);
  Task CleanupExpiredCacheAsync();
  Task ClearCacheAsync(string applicationName);
}