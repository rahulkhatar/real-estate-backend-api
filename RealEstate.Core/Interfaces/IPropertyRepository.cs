using RealEstate.Core.Entities;
using RealEstate.Core.ValueObjects;

namespace RealEstate.Core.Interfaces;

public interface IPropertyRepository : IRepository<Property>
{
    Task<IReadOnlyList<Property>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);

    /// <summary>
    /// Refreshes the embedded ProjectSnapshot on every property under the given project.
    /// Called after a project's display fields (name/city) change, since properties denormalize them for read performance.
    /// </summary>
    Task UpdateProjectSnapshotAsync(string projectId, ProjectSnapshot snapshot, CancellationToken ct = default);

    Task IncrementSoldUnitsAsync(string propertyId, int delta, CancellationToken ct = default);
}
