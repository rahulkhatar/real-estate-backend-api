using RealEstate.Core.Entities;

namespace RealEstate.Core.Interfaces;

public interface IUnitLayoutRepository : IRepository<UnitLayout>
{
    Task<IReadOnlyList<UnitLayout>> GetByUnitIdAsync(string unitId, CancellationToken ct = default);
}
