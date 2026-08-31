using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.ValueObjects;

namespace RealEstate.Core.Interfaces;

public interface IUnitRepository : IRepository<Unit>
{
    Task<IReadOnlyList<Unit>> GetByPropertyIdAsync(string propertyId, CancellationToken ct = default);
    Task<IReadOnlyList<Unit>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
    Task<bool> ExistsByUnitNumberAsync(string propertyId, string unitNumber, string? excludeId = null, CancellationToken ct = default);
    Task UpdateStatusAsync(string unitId, UnitStatus status, CancellationToken ct = default);
    Task UpdateProjectSnapshotAsync(string projectId, ProjectSnapshot snapshot, CancellationToken ct = default);
    Task UpdatePropertySnapshotAsync(string propertyId, PropertySnapshot snapshot, CancellationToken ct = default);
}
