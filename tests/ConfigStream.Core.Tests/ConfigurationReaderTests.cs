using ConfigStream.Core.Interfaces;
using ConfigStream.Core.Models;
using ConfigStream.Core.Services;
using FluentAssertions;
using Moq;

namespace ConfigStream.Core.Tests;

public class ConfigurationReaderTests : IDisposable
{
    private readonly Mock<IFileCacheService> _mockFileCacheService;
    private readonly Mock<IConfigurationStorage> _mockMongoStorage;

    private readonly string _applicationName = "TEST_APP";
    private readonly string _connectionString = "mongodb://localhost:27017/test";

    public ConfigurationReaderTests()
    {
        _mockFileCacheService = new Mock<IFileCacheService>();
        _mockMongoStorage = new Mock<IConfigurationStorage>();
    }

    [Fact]
    public void Constructor_ValidParameters_InitializesCorrectly()
    {
        // Act
        using var reader = new ConfigurationReader(_applicationName, _connectionString, 5000);

        // Assert
        reader.Should().NotBeNull();
    }


    [Fact]
    public void Constructor_ZeroRefreshInterval_DoesNotStartTimer()
    {
        // Act & Assert
        using var reader = new ConfigurationReader(_applicationName, _connectionString, 0);

        reader.Should().NotBeNull();
    }

    [Fact]
    public async Task GetValueAsync_NoStorageAvailable_ReturnsDefault()
    {
        // Arrange
        _mockFileCacheService
            .Setup(x => x.GetConfigurationAsync(_applicationName, "nonExistentKey", CancellationToken.None))
            .ReturnsAsync((ConfigurationItem?)null);

        using var reader = new ConfigurationReader(_applicationName, _connectionString, 0);

        // Act
        var result = await reader.GetValueAsync<string>("nonExistentKey");

        // Assert
        result.Should().BeNull();
    }


    [Fact]
    public async Task GetValueAsync_ReturnsDefault_WhenKeyNotFoundInBothStorages()
    {
        // Arrange
        var key = "MissingKey";
        using var reader = new ConfigurationReader(_applicationName, _connectionString, 0);

        _mockMongoStorage
            .Setup(x => x.GetAsync(_applicationName, key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfigurationItem?)null);

        _mockFileCacheService
            .Setup(x => x.GetConfigurationAsync(_applicationName, key,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfigurationItem?)null);

        // Act
        var result = await reader.GetValueAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetValueAsync_Throws_IfCancellationRequested()
    {
        // Arrange
        using var reader = new ConfigurationReader(_applicationName, _connectionString, 0);
        var key = "TestKey";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => reader.GetValueAsync<string>(key, cts.Token));
    }

    [Fact]
    public void GetValue_ReturnsDefault_WhenKeyNotFound()
    {
        // Arrange
        using var reader = new ConfigurationReader(_applicationName, _connectionString, 0);
        var key = "MissingKey";

        _mockMongoStorage
            .Setup(x => x.GetAsync(_applicationName, key, CancellationToken.None))
            .ReturnsAsync((ConfigurationItem?)null);

        _mockFileCacheService
            .Setup(f => f.GetConfigurationAsync(_applicationName, key, CancellationToken.None))
            .ReturnsAsync((ConfigurationItem?)null);

        // Act
        var result = reader.GetValue<string>(key);

        // Assert
        result.Should().BeNull();
    }


    public void Dispose()
    {
    }
}
