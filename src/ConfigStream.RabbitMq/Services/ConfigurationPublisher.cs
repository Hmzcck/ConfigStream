using ConfigStream.RabbitMq.Interfaces;
using MassTransit;

namespace ConfigStream.RabbitMq.Services;

public class ConfigurationPublisher(IPublishEndpoint publishEndpoint) : IConfigurationPublisher
{
    public async Task PublishConfigurationUpdated(string applicationName, string key, string value)
    {
        await publishEndpoint.Publish(new ConfigurationUpdatedEvent
        {
            ApplicationName = applicationName, Key = key, Value = value, UpdatedAt = DateTime.UtcNow
        });
    }
}