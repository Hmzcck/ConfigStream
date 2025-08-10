using ConfigStream.Core.Models;

namespace ConfigStream.Core.Interfaces;

public interface IConfigurationStorage
{
    Task<ConfigurationItem?> GetAsync(string applicationName, string key);
    Task<IEnumerable<ConfigurationItem>> GetAllAsync(string applicationName);
    Task<ConfigurationItem?> SetAsync(ConfigurationItem item);
    Task<bool> DeleteAsync(string applicationName, string key);
}