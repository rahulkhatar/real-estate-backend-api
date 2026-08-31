using MongoDB.Driver;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Interfaces;
using RealEstate.Core.ValueObjects;

namespace RealEstate.Infrastructure.Persistence.Repositories;

public class UnitRepository(IMongoDbContext context)
    : GenericRepository<Unit>(context, CollectionNames.Units), IUnitRepository
{
    public async Task<IReadOnlyList<Unit>> GetByPropertyIdAsync(string propertyId, CancellationToken ct = default) =>
        await Collection.Find(u => u.PropertyId == propertyId && !u.IsDeleted).ToListAsync(ct);

    public async Task<IReadOnlyList<Unit>> GetByProjectIdAsync(string projectId, CancellationToken ct = default) =>
        await Collection.Find(u => u.ProjectId == projectId && !u.IsDeleted).ToListAsync(ct);

    public async Task<bool> ExistsByUnitNumberAsync(string propertyId, string unitNumber, string? excludeId = null, CancellationToken ct = default)
    {
        var filter = Builders<Unit>.Filter.Where(u =>
            u.PropertyId == propertyId && u.UnitNumber == unitNumber && !u.IsDeleted);

        if (!string.IsNullOrEmpty(excludeId))
            filter &= Builders<Unit>.Filter.Ne(u => u.Id, excludeId);

        return await Collection.Find(filter).AnyAsync(ct);
    }

    public async Task UpdateStatusAsync(string unitId, UnitStatus status, CancellationToken ct = default)
    {
        var update = Builders<Unit>.Update
            .Set(u => u.Status, status)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        await Collection.UpdateOneAsync(u => u.Id == unitId, update, cancellationToken: ct);
    }

    public async Task UpdateProjectSnapshotAsync(string projectId, ProjectSnapshot snapshot, CancellationToken ct = default)
    {
        var update = Builders<Unit>.Update
            .Set(u => u.ProjectSnapshot, snapshot)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        await Collection.UpdateManyAsync(u => u.ProjectId == projectId, update, cancellationToken: ct);
    }

    public async Task UpdatePropertySnapshotAsync(string propertyId, PropertySnapshot snapshot, CancellationToken ct = default)
    {
        var update = Builders<Unit>.Update
            .Set(u => u.PropertySnapshot, snapshot)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        await Collection.UpdateManyAsync(u => u.PropertyId == propertyId, update, cancellationToken: ct);
    }
}
