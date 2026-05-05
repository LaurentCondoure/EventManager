using EventManager.Domain.Entities;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace EventManager.Infrastructure.Mappings;

public static class MongoDbMappings
{
    private static readonly object _lock = new();
    private static bool _registered;

    public static void Register()
    {
        if (_registered) return;

        lock (_lock)
        {
            if (_registered) return;

            BsonClassMap.RegisterClassMap<EventComment>(map =>
            {
                map.AutoMap();
                map.MapMember(c => c.EventId).SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
                map.MapMember(c => c.UserId).SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            });

            _registered = true;
        }
    }
}
