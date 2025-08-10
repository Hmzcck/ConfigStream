namespace ConfigStream.Core.Models;

public class ConfigurationItem
{
    public string? Id { get; set; }
    public required string Name { get; set; }
    public required string ApplicationName { get; set; }
    public string? Value { get; set; }
    public ConfigurationType Type { get; set; }
    public int IsActive { get; set; }
}
