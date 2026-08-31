using Microsoft.Extensions.Options;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Enums;
using RealEstate.Core.Exceptions;
using Stripe;

namespace RealEstate.Infrastructure.ExternalServices.PaymentGateways;

/// <summary>
/// Stripe integration via Stripe.net. Creates a PaymentIntent per order — the frontend
/// completes it client-side with Stripe.js/Elements using the returned client secret.
/// </summary>
public class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeSettings _settings;

    public PaymentProvider Provider => PaymentProvider.Stripe;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.SecretKey);

    public StripePaymentGateway(IOptions<StripeSettings> options)
    {
        _settings = options.Value;
    }

    public async Task<PaymentOrderResult> CreateOrderAsync(CreatePaymentOrderRequest request, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new PaymentGatewayNotConfiguredException("Stripe");

        var client = new StripeClient(_settings.SecretKey);
        var service = new PaymentIntentService(client);

        var intent = await service.CreateAsync(new PaymentIntentCreateOptions
        {
            Amount = ToSmallestUnit(request.Amount),
            Currency = request.Currency.ToLowerInvariant(),
            Description = request.Description,
            Metadata = new Dictionary<string, string> { ["receiptId"] = request.ReceiptId },
        }, cancellationToken: ct);

        return new PaymentOrderResult(intent.Id, intent.ClientSecret, _settings.PublishableKey, request.Currency, request.Amount);
    }

    public PaymentWebhookEvent ParseWebhook(string payload, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(_settings.WebhookSecret))
            return new PaymentWebhookEvent(false, string.Empty, string.Empty, PaymentStatus.Failed, string.Empty);

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, _settings.WebhookSecret);

            if (stripeEvent.Data.Object is not PaymentIntent intent)
                return new PaymentWebhookEvent(false, string.Empty, string.Empty, PaymentStatus.Failed, stripeEvent.Type);

            var status = stripeEvent.Type switch
            {
                "payment_intent.succeeded" => PaymentStatus.Succeeded,
                "payment_intent.payment_failed" => PaymentStatus.Failed,
                _ => PaymentStatus.Pending,
            };

            return new PaymentWebhookEvent(true, intent.Id, intent.Id, status, stripeEvent.Type);
        }
        catch (StripeException)
        {
            return new PaymentWebhookEvent(false, string.Empty, string.Empty, PaymentStatus.Failed, string.Empty);
        }
    }

    // Stripe amounts are in the currency's smallest unit (e.g. paise for INR, cents for USD).
    private static long ToSmallestUnit(decimal amount) => (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
}
