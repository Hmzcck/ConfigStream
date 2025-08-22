using ConfigStream.RabbitMq.Configuration;
using ConfigStream.RabbitMq.Services;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConfigStream.RabbitMq.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration config)
    {
        var rabbitMqSettings = config.GetSection("RabbitMqSettings").Get<RabbitMqSettings>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<ConfigurationUpdatedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqSettings!.Host, rabbitMqSettings.Port, rabbitMqSettings.VirtualHost, h =>
                {
                    h.Username(rabbitMqSettings.Username);
                    h.Password(rabbitMqSettings.Password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IConfigurationProvider, ConfigurationProvider>();

        return services;
    }
}