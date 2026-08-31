using MediatR;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Bookings.Commands;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Enums;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Payments.Commands;

/// <summary>
/// Handles an inbound webhook from a payment provider. Returns false (rather than throwing) for
/// events that don't correspond to a payment we created — provider retries on non-2xx, and an
/// unrecognized order id isn't something retrying will fix, so the controller still acks it.
/// </summary>
public record ProcessPaymentWebhookCommand(PaymentProvider Provider, string Payload, string Signature) : IRequest<bool>;

public class ProcessPaymentWebhookCommandHandler(
    IEnumerable<IPaymentGateway> gateways,
    IPaymentRepository paymentRepository,
    IBookingRepository bookingRepository,
    IMediator mediator) : IRequestHandler<ProcessPaymentWebhookCommand, bool>
{
    public async Task<bool> Handle(ProcessPaymentWebhookCommand request, CancellationToken cancellationToken)
    {
        var gateway = gateways.Single(g => g.Provider == request.Provider);
        var webhookEvent = gateway.ParseWebhook(request.Payload, request.Signature);

        if (!webhookEvent.IsValid)
            throw new UnauthorizedAppException("Invalid webhook signature.");

        var payment = await paymentRepository.GetByProviderOrderIdAsync(request.Provider, webhookEvent.ProviderOrderId, cancellationToken);
        if (payment is null)
            return false;

        if (payment.Status is PaymentStatus.Succeeded or PaymentStatus.Refunded)
            return true; // already handled — providers retry webhooks, this keeps it idempotent

        payment.Status = webhookEvent.Status;
        payment.ProviderPaymentId = webhookEvent.ProviderPaymentId;
        if (webhookEvent.Status == PaymentStatus.Succeeded)
            payment.PaidAt = DateTime.UtcNow;

        await paymentRepository.UpdateAsync(payment, cancellationToken);

        if (webhookEvent.Status == PaymentStatus.Succeeded)
        {
            var booking = await bookingRepository.GetByIdAsync(payment.BookingId, cancellationToken);
            // Payment success is what closes the deal — straight to Completed (cascades unit to Sold).
            if (booking is { Status: BookingStatus.Pending or BookingStatus.Confirmed })
            {
                await mediator.Send(
                    new UpdateBookingStatusCommand(booking.Id, new UpdateBookingStatusDto { Status = nameof(BookingStatus.Completed) }),
                    cancellationToken);
            }
        }

        return true;
    }
}
