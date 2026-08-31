using RealEstate.Core.Entities;
using RealEstate.Core.Enums;

namespace RealEstate.Core.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetByBookingIdAsync(string bookingId, CancellationToken ct = default);
    Task<Payment?> GetByProviderOrderIdAsync(PaymentProvider provider, string providerOrderId, CancellationToken ct = default);

    /// <summary>All payment attempts for a booking (newest first) — a booking can have more than one if an earlier attempt failed or was retried with a different provider.</summary>
    Task<IReadOnlyList<Payment>> GetHistoryByBookingIdAsync(string bookingId, CancellationToken ct = default);
}
