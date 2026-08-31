using MongoDB.Driver;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Interfaces;

namespace RealEstate.Infrastructure.Persistence.Repositories;

public class BookingRepository(IMongoDbContext context)
    : GenericRepository<Booking>(context, CollectionNames.Bookings), IBookingRepository
{
    public async Task<IReadOnlyList<Booking>> GetByAgentIdAsync(string agentId, CancellationToken ct = default) =>
        await Collection.Find(b => b.AgentId == agentId && !b.IsDeleted)
            .SortByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

    public async Task<bool> HasActiveBookingForUnitAsync(string unitId, CancellationToken ct = default) =>
        await Collection.Find(b =>
                b.UnitId == unitId &&
                !b.IsDeleted &&
                (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .AnyAsync(ct);
}
