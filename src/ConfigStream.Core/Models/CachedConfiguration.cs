using System.Text.Json.Serialization;

namespace ConfigStream.Core.Models;

public class CachedConfiguration
{
    [JsonPropertyName("applicationName")] public string ApplicationName { get; set; } = string.Empty;

    [JsonPropertyName("lastUpdated")] public DateTime LastUpdated { get; set; }

    [JsonPropertyName("expiresAt")] public DateTime ExpiresAt { get; set; }

    [JsonPropertyName("configurations")]
    public Dictionary<string, CachedConfigurationItem> Configurations { get; set; } = new();
}

public class CachedConfigurationItem
{
    [JsonPropertyName("value")] public string? Value { get; set; }

    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;

    [JsonPropertyName("isActive")] public int IsActive { get; set; }

    [JsonPropertyName("cachedAt")] public DateTime CachedAt { get; set; }
}
