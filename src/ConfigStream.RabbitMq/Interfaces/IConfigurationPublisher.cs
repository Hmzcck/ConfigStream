namespace ConfigStream.RabbitMq.Interfaces;

public interface IConfigurationPublisher
{
    Task PublishConfigurationUpdated(string applicationName, string key, string value);
}