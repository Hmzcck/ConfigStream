namespace ConfigStream.RabbitMq;

public class ConfigurationUpdatedEvent
{
    public string ApplicationName { get; init; }
    public string Key { get; init; }
    public string Value { get; init; }
    public DateTime UpdatedAt { get; init; }
}