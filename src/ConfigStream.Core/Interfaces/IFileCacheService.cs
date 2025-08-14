using ConfigStream.Core.Models;

namespace ConfigStream.Core.Interfaces;

public interface IFileCacheService
{
  Task<ConfigurationItem?> GetConfigurationAsync(string applicationName, string key,
    CancellationToken cancellationToken = default);

  Task SaveConfigurationAsync(string applicationName, string key, ConfigurationItem configuration,
    CancellationToken cancellationToken = default);

  Task<IEnumerable<ConfigurationItem>> GetAllConfigurationsAsync(string applicationName,
    CancellationToken cancellationToken = default);

  Task SaveAllConfigurationsAsync(string applicationName, IEnumerable<ConfigurationItem> configurations,
    CancellationToken cancellationToken = default);

  Task CleanupExpiredCacheAsync(CancellationToken cancellationToken = default);
  Task ClearCacheAsync(string applicationName, CancellationToken cancellationToken = default);
  Task<IEnumerable<string>> GetAllApplicationNamesAsync(CancellationToken cancellationToken = default);
}