using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace RealEstate.Infrastructure.Persistence;

public class MongoDbContext : IMongoDbContext
{
    public IMongoDatabase Database { get; }

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        Database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string name) => Database.GetCollection<T>(name);
}
