using RealEstate.Core.Entities;

namespace RealEstate.Core.Interfaces;

public interface IBookingRepository : IRepository<Booking>
{
    Task<IReadOnlyList<Booking>> GetByAgentIdAsync(string agentId, CancellationToken ct = default);
    Task<bool> HasActiveBookingForUnitAsync(string unitId, CancellationToken ct = default);
}
