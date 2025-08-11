using ConfigStream.Core.Models;
using ConfigStream.Core.Services;

namespace ConfigStream.Core.Tests;

public class FileCacheServiceTests : IDisposable
{
    private readonly FileCacheService _cacheService;
    private readonly string _appName = "TestApp";
    private readonly string _cacheDir;

    public FileCacheServiceTests()
    {
        _cacheService = new FileCacheService();
        _cacheDir = Path.Combine(Path.GetTempPath(), "DynamicConfig");
        ClearCacheFiles();
    }

    private void ClearCacheFiles()
    {
        if (Directory.Exists(_cacheDir))
        {
            foreach (var file in Directory.GetFiles(_cacheDir, "*_config.json"))
            {
                File.Delete(file);
            }
        }
    }

    [Fact]
    public async Task SaveAndGetConfigurationAsync_ShouldSaveAndRetrieveConfig()
    {
        var configItem = new ConfigurationItem
        {
            ApplicationName = _appName,
            Name = "TestKey",
            Value = "TestValue",
            Type = ConfigurationType.String,
            IsActive = 1
        };

        await _cacheService.SaveConfigurationAsync(_appName, configItem.Name, configItem);

        var retrieved = await _cacheService.GetConfigurationAsync(_appName, "TestKey");

        Assert.NotNull(retrieved);
        Assert.Equal(configItem.Value, retrieved!.Value);
        Assert.Equal(configItem.Name, retrieved.Name);
    }

    [Fact]
    public async Task GetConfigurationAsync_ShouldReturnNull_IfNotExist()
    {
        var result = await _cacheService.GetConfigurationAsync(_appName, "NonExistingKey");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllConfigurationsAsync_ShouldReturnAllSavedConfigurations()
    {
        var config1 = new ConfigurationItem
        {
            ApplicationName = _appName,
            Name = "Key1",
            Value = "Value1",
            Type = ConfigurationType.String,
            IsActive = 1
        };
        var config2 = new ConfigurationItem
        {
            ApplicationName = _appName,
            Name = "Key2",
            Value = "Value2",
            Type = ConfigurationType.String,
            IsActive = 1
        };

        await _cacheService.SaveConfigurationAsync(_appName, config1.Name, config1);
        await _cacheService.SaveConfigurationAsync(_appName, config2.Name, config2);

        var allConfigs = await _cacheService.GetAllConfigurationsAsync(_appName);
        Assert.NotNull(allConfigs);
        Assert.Equal(2, allConfigs.Count());
        Assert.Contains(allConfigs, c => c.Name == "Key1");
        Assert.Contains(allConfigs, c => c.Name == "Key2");
    }

    [Fact]
    public async Task ClearCacheAsync_ShouldDeleteCacheFile()
    {
        var config = new ConfigurationItem
        {
            ApplicationName = _appName,
            Name = "KeyToClear",
            Value = "SomeValue",
            Type = ConfigurationType.String,
            IsActive = 1
        };

        await _cacheService.SaveConfigurationAsync(_appName, config.Name, config);

        var filePath = Path.Combine(_cacheDir, $"{_appName}_config.json");
        Assert.True(File.Exists(filePath));

        await _cacheService.ClearCacheAsync(_appName);

        Assert.False(File.Exists(filePath));
    }

    public void Dispose()
    {
        _cacheService.Dispose();
        ClearCacheFiles();
    }
}
