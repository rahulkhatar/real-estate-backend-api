using MongoDB.Driver;
using RealEstate.Core.Entities;
using RealEstate.Core.Interfaces;
using RealEstate.Core.ValueObjects;

namespace RealEstate.Infrastructure.Persistence.Repositories;

public class PropertyRepository(IMongoDbContext context)
    : GenericRepository<Property>(context, CollectionNames.Properties), IPropertyRepository
{
    public async Task<IReadOnlyList<Property>> GetByProjectIdAsync(string projectId, CancellationToken ct = default) =>
        await Collection.Find(p => p.ProjectId == projectId && !p.IsDeleted).ToListAsync(ct);

    public async Task UpdateProjectSnapshotAsync(string projectId, ProjectSnapshot snapshot, CancellationToken ct = default)
    {
        var update = Builders<Property>.Update
            .Set(p => p.ProjectSnapshot, snapshot)
            .Set(p => p.UpdatedAt, DateTime.UtcNow);

        await Collection.UpdateManyAsync(p => p.ProjectId == projectId, update, cancellationToken: ct);
    }

    public async Task IncrementSoldUnitsAsync(string propertyId, int delta, CancellationToken ct = default)
    {
        var update = Builders<Property>.Update
            .Inc(p => p.SoldUnits, delta)
            .Set(p => p.UpdatedAt, DateTime.UtcNow);

        await Collection.UpdateOneAsync(p => p.Id == propertyId, update, cancellationToken: ct);
    }
}
