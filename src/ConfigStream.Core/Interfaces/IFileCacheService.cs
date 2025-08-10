using ConfigStream.Core.Models;

namespace ConfigStream.Core.Interfaces;

  public interface IFileCacheService
  {
      Task<ConfigurationItem?> GetConfigurationAsync(string applicationName, string key);
      Task SaveConfigurationAsync(string applicationName, string key, ConfigurationItem configuration);
      Task<IEnumerable<ConfigurationItem>> GetAllConfigurationsAsync(string applicationName);
  }