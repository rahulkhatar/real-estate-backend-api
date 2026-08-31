using MongoDB.Driver;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Interfaces;

namespace RealEstate.Infrastructure.Persistence.Repositories;

public class PaymentRepository(IMongoDbContext context)
    : GenericRepository<Payment>(context, CollectionNames.Payments), IPaymentRepository
{
    public async Task<Payment?> GetByBookingIdAsync(string bookingId, CancellationToken ct = default) =>
        await Collection.Find(p => p.BookingId == bookingId && !p.IsDeleted)
            .SortByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<Payment?> GetByProviderOrderIdAsync(PaymentProvider provider, string providerOrderId, CancellationToken ct = default) =>
        await Collection.Find(p => p.Provider == provider && p.ProviderOrderId == providerOrderId && !p.IsDeleted)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Payment>> GetHistoryByBookingIdAsync(string bookingId, CancellationToken ct = default) =>
        await Collection.Find(p => p.BookingId == bookingId && !p.IsDeleted)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
}
