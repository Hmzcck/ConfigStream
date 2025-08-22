using MassTransit;
using Microsoft.Extensions.Logging;

namespace ConfigStream.RabbitMq.Services;

public class ConfigurationUpdatedConsumer(ILogger<ConfigurationUpdatedConsumer> _logger)
    : IConsumer<ConfigurationUpdatedEvent>
{
    public Task Consume(ConsumeContext<ConfigurationUpdatedEvent> context)
    {
        _logger.LogInformation("Configuration for {ApplicationName} updated: {Key} = {Value} at {UpdatedAt}",
            context.Message.ApplicationName,
            context.Message.Key,
            context.Message.Value,
            context.Message.UpdatedAt);

        return Task.CompletedTask;
    }
}