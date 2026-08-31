using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Enums;
using RealEstate.Core.Exceptions;

namespace RealEstate.Infrastructure.ExternalServices.PaymentGateways;

/// <summary>
/// Razorpay integration via its REST API directly (no official SDK dependency) — creates an
/// Order that the frontend completes with Razorpay's Checkout.js popup, and verifies the
/// HMAC-SHA256 webhook signature Razorpay sends on payment.captured/payment.failed events.
/// </summary>
public class RazorpayPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _http;
    private readonly RazorpaySettings _settings;

    public PaymentProvider Provider => PaymentProvider.Razorpay;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.KeyId) && !string.IsNullOrWhiteSpace(_settings.KeySecret);

    public RazorpayPaymentGateway(HttpClient http, IOptions<RazorpaySettings> options)
    {
        _http = http;
        _settings = options.Value;
    }

    public async Task<PaymentOrderResult> CreateOrderAsync(CreatePaymentOrderRequest request, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new PaymentGatewayNotConfiguredException("Razorpay");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "orders")
        {
            Content = JsonContent.Create(new
            {
                amount = ToSmallestUnit(request.Amount),
                currency = request.Currency.ToUpperInvariant(),
                receipt = request.ReceiptId,
            }),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", BasicAuthValue());

        var response = await _http.SendAsync(httpRequest, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new ConflictException($"Razorpay order creation failed: {body}");

        using var doc = JsonDocument.Parse(body);
        var orderId = doc.RootElement.GetProperty("id").GetString() ?? string.Empty;

        return new PaymentOrderResult(orderId, null, _settings.KeyId, request.Currency, request.Amount);
    }

    public PaymentWebhookEvent ParseWebhook(string payload, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(_settings.WebhookSecret) || string.IsNullOrWhiteSpace(signatureHeader))
            return new PaymentWebhookEvent(false, string.Empty, string.Empty, PaymentStatus.Failed, string.Empty);

        var expected = ComputeHmacHex(payload, _settings.WebhookSecret);
        if (!SignatureMatches(expected, signatureHeader))
            return new PaymentWebhookEvent(false, string.Empty, string.Empty, PaymentStatus.Failed, string.Empty);

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var eventType = root.GetProperty("event").GetString() ?? string.Empty;
        var paymentEntity = root.GetProperty("payload").GetProperty("payment").GetProperty("entity");
        var orderId = paymentEntity.GetProperty("order_id").GetString() ?? string.Empty;
        var paymentId = paymentEntity.GetProperty("id").GetString() ?? string.Empty;

        var status = eventType switch
        {
            "payment.captured" => PaymentStatus.Succeeded,
            "payment.failed" => PaymentStatus.Failed,
            _ => PaymentStatus.Pending,
        };

        return new PaymentWebhookEvent(true, orderId, paymentId, status, eventType);
    }

    private string BasicAuthValue() =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.KeyId}:{_settings.KeySecret}"));

    // Razorpay amounts are in the currency's smallest unit (paise for INR).
    private static long ToSmallestUnit(decimal amount) => (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);

    private static string ComputeHmacHex(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool SignatureMatches(string expectedHex, string providedHex) =>
        expectedHex.Length == providedHex.Length &&
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expectedHex), Encoding.UTF8.GetBytes(providedHex));
}
