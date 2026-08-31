using RealEstate.Core.Enums;

namespace RealEstate.Application.Interfaces;

public record CreatePaymentOrderRequest(string ReceiptId, decimal Amount, string Currency, string Description);

public record PaymentOrderResult(
    string ProviderOrderId,
    string? ClientSecret,
    string? PublicKey,
    string Currency,
    decimal Amount);

public record PaymentWebhookEvent(
    bool IsValid,
    string ProviderOrderId,
    string ProviderPaymentId,
    PaymentStatus Status,
    string RawEventType);

/// <summary>
/// One implementation per payment provider (Stripe, Razorpay), registered side-by-side so a
/// caller resolves the right one from <see cref="IEnumerable{IPaymentGateway}"/> by <see cref="Provider"/>.
/// </summary>
public interface IPaymentGateway
{
    PaymentProvider Provider { get; }

    /// <summary>False when the provider's API keys haven't been configured yet.</summary>
    bool IsConfigured { get; }

    Task<PaymentOrderResult> CreateOrderAsync(CreatePaymentOrderRequest request, CancellationToken ct = default);

    /// <summary>Verifies the webhook signature and extracts the event; IsValid is false on a bad/unverifiable signature.</summary>
    PaymentWebhookEvent ParseWebhook(string payload, string signatureHeader);
}
