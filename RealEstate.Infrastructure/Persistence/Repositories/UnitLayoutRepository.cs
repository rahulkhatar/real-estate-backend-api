using MongoDB.Driver;
using RealEstate.Core.Entities;
using RealEstate.Core.Interfaces;

namespace RealEstate.Infrastructure.Persistence.Repositories;

public class UnitLayoutRepository(IMongoDbContext context)
    : GenericRepository<UnitLayout>(context, CollectionNames.UnitLayouts), IUnitLayoutRepository
{
    public async Task<IReadOnlyList<UnitLayout>> GetByUnitIdAsync(string unitId, CancellationToken ct = default) =>
        await Collection.Find(l => l.UnitId == unitId && !l.IsDeleted).ToListAsync(ct);
}
