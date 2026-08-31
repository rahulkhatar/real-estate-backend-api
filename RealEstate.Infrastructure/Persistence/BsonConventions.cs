using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;

namespace RealEstate.Infrastructure.Persistence;

/// <summary>
/// Global BSON serialization conventions: camelCase field names (matches the JS-style
/// schemas in the design docs), tolerate unknown fields on read, and store enums as
/// their string name rather than an ordinal int so documents stay human-readable in Compass.
/// </summary>
public static class BsonConventions
{
    private static bool _registered;
    private static readonly Lock RegistrationLock = new();

    public static void Register()
    {
        if (_registered) return;

        lock (RegistrationLock)
        {
            if (_registered) return;

            var pack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true),
                new EnumRepresentationConvention(BsonType.String)
            };

            ConventionRegistry.Register("RealEstateConventions", pack, _ => true);
            _registered = true;
        }
    }
}
