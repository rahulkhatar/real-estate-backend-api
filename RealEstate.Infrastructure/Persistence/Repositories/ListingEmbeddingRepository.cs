using MongoDB.Driver;
using RealEstate.Core.Entities;
using RealEstate.Core.Interfaces;

namespace RealEstate.Infrastructure.Persistence.Repositories;

public class ListingEmbeddingRepository(IMongoDbContext context)
    : GenericRepository<ListingEmbedding>(context, CollectionNames.ListingEmbeddings), IListingEmbeddingRepository
{
    public async Task<ListingEmbedding?> GetByUnitIdAsync(string unitId, CancellationToken ct = default) =>
        await Collection.Find(e => e.UnitId == unitId && !e.IsDeleted).FirstOrDefaultAsync(ct);

    public async Task UpsertAsync(ListingEmbedding embedding, CancellationToken ct = default)
    {
        var existing = await GetByUnitIdAsync(embedding.UnitId, ct);
        if (existing is null)
        {
            await AddAsync(embedding, ct);
        }
        else
        {
            embedding.Id = existing.Id;
            embedding.CreatedAt = existing.CreatedAt;
            await UpdateAsync(embedding, ct);
        }
    }

    public async Task UpdateStatusAsync(string unitId, string status, CancellationToken ct = default)
    {
        var update = Builders<ListingEmbedding>.Update
            .Set(e => e.Status, status)
            .Set(e => e.UpdatedAt, DateTime.UtcNow);
        await Collection.UpdateOneAsync(e => e.UnitId == unitId && !e.IsDeleted, update, cancellationToken: ct);
    }

    public async Task DeleteByUnitIdAsync(string unitId, CancellationToken ct = default) =>
        await Collection.DeleteOneAsync(e => e.UnitId == unitId, ct);
}
