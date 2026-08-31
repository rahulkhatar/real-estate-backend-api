using MongoDB.Driver;

namespace RealEstate.Infrastructure.Persistence;

public interface IMongoDbContext
{
    IMongoCollection<T> GetCollection<T>(string name);
    IMongoDatabase Database { get; }
}
