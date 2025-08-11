using ConfigStream.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ConfigStream.MongoDb.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMongoDbStorage(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IConfigurationStorage>(_ => new MongoConfigurationStorage(connectionString));

        return services;
    }
}