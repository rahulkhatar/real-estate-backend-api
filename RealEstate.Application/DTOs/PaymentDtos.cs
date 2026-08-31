namespace RealEstate.Application.DTOs;

public class PaymentDto
{
    public string Id { get; set; } = string.Empty;
    public string BookingId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ProviderOrderId { get; set; } = string.Empty;
    public string ProviderPaymentId { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreatePaymentOrderDto
{
    public string BookingId { get; set; } = string.Empty;

    /// <summary>"Stripe" or "Razorpay".</summary>
    public string Provider { get; set; } = string.Empty;
}

public class RecordManualPaymentDto
{
    public string BookingId { get; set; } = string.Empty;

    /// <summary>How the payment was received, e.g. "Cash", "Bank transfer UTR123456". Optional.</summary>
    public string Reference { get; set; } = string.Empty;
}

public class PaymentOrderResponseDto
{
    public string PaymentId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ProviderOrderId { get; set; } = string.Empty;

    /// <summary>Stripe only — the PaymentIntent client secret used by Stripe.js/Elements.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>The publishable/key-id to hand to the frontend SDK.</summary>
    public string? PublicKey { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}
