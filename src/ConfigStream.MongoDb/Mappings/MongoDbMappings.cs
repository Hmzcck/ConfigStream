using ConfigStream.Core.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace ConfigStream.MongoDb.Mappings;

public static class MongoDbMappings
{
    private static bool _initialized;
    private static readonly object Lock = new object();

    public static void Initialize()
    {
        lock (Lock)
        {
            if (_initialized)
                return;

            RegisterClassMaps();
            _initialized = true;
        }
    }

    private static void RegisterClassMaps()
    {
        BsonClassMap.RegisterClassMap<ConfigurationItem>(cm =>
        {
            cm.AutoMap();

            // Id mapping
            cm.MapIdMember(c => c.Id)
                .SetSerializer(new StringSerializer(BsonType.ObjectId));

            // Index hints
            cm.MapMember(c => c.ApplicationName).SetElementName("applicationName");
            cm.MapMember(c => c.Name).SetElementName("name");
            cm.MapMember(c => c.Type).SetElementName("type");
            cm.MapMember(c => c.Value).SetElementName("value");
            cm.MapMember(c => c.IsActive).SetElementName("isActive");

            // Ignore extra elements for backward compatibility
            cm.SetIgnoreExtraElements(true);
        });

        // Enum serialization
        BsonSerializer.RegisterSerializer(typeof(ConfigurationType),
            new EnumSerializer<ConfigurationType>(BsonType.Int32));
    }
}