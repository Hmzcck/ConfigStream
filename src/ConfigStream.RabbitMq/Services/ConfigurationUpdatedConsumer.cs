using ConfigStream.Core.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ConfigStream.RabbitMq.Services;

public class ConfigurationUpdatedConsumer(
    ILogger<ConfigurationUpdatedConsumer> _logger,
    IFileCacheService _fileCacheService)
    : IConsumer<ConfigurationUpdatedEvent>
{
    public async Task Consume(ConsumeContext<ConfigurationUpdatedEvent> context)
    {
        _logger.LogInformation("Configuration for {ApplicationName} updated: {Key} = {Value} at {UpdatedAt}",
            context.Message.ApplicationName,
            context.Message.Key,
            context.Message.Value,
            context.Message.UpdatedAt);

        try
        {
            await _fileCacheService.UpdateConfigurationAsync(
                context.Message.ApplicationName,
                context.Message.Key,
                context.Message.Value);

            _logger.LogDebug("Cache updated for {ApplicationName}:{Key}", 
                context.Message.ApplicationName, context.Message.Key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update cache for {ApplicationName}:{Key}", 
                context.Message.ApplicationName, context.Message.Key);
        }
    }
}