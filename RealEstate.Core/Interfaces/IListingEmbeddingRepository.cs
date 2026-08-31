using RealEstate.Core.Entities;

namespace RealEstate.Core.Interfaces;

public interface IListingEmbeddingRepository : IRepository<ListingEmbedding>
{
    Task<ListingEmbedding?> GetByUnitIdAsync(string unitId, CancellationToken ct = default);

    /// <summary>Inserts or replaces the embedding for a unit, keyed by UnitId (not Id).</summary>
    Task UpsertAsync(ListingEmbedding embedding, CancellationToken ct = default);

    /// <summary>Cheap denormalized-field-only update (no re-embedding) — used when a unit's status changes but its description hasn't.</summary>
    Task UpdateStatusAsync(string unitId, string status, CancellationToken ct = default);

    /// <summary>Removes a unit's embedding entirely, e.g. when the unit itself is deleted.</summary>
    Task DeleteByUnitIdAsync(string unitId, CancellationToken ct = default);
}
