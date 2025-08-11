using ConfigStream.Core.Models;

namespace ConfigStream.Core.Interfaces;

public interface IConfigurationStorage
{
    Task<ConfigurationItem?> GetAsync(string applicationName, string key,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ConfigurationItem>> GetAllAsync(string applicationName,
        CancellationToken cancellationToken = default);

    Task<ConfigurationItem?> SetAsync(ConfigurationItem item, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string applicationName, string key, CancellationToken cancellationToken = default);
}