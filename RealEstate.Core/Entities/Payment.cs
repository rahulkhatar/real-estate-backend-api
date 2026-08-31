using RealEstate.Core.Common;
using RealEstate.Core.Enums;

namespace RealEstate.Core.Entities;

public class Payment : BaseEntity
{
    public string BookingId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;

    public PaymentProvider Provider { get; set; }
    public string ProviderOrderId { get; set; } = string.Empty;
    public string ProviderPaymentId { get; set; } = string.Empty;

    /// <summary>Free-text reference for a Manual payment (e.g. "Cash", "Bank transfer UTR123456").</summary>
    public string Reference { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";

    public PaymentStatus Status { get; set; } = PaymentStatus.Created;
    public string FailureReason { get; set; } = string.Empty;

    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
}
