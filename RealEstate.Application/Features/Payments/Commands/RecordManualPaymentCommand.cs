using AutoMapper;
using FluentValidation;
using MediatR;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Bookings.Commands;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Payments.Commands;

/// <summary>
/// Records a payment collected outside Stripe/Razorpay (cash, bank transfer, cheque) and marks
/// it succeeded immediately — there's no external order or webhook to wait on. Also doubles as
/// the way to exercise the full booking+payment pipeline (auto-confirm, status tracking) without
/// needing real gateway keys configured.
/// </summary>
public record RecordManualPaymentCommand(RecordManualPaymentDto Dto) : IRequest<PaymentDto>;

public class RecordManualPaymentCommandValidator : AbstractValidator<RecordManualPaymentCommand>
{
    public RecordManualPaymentCommandValidator()
    {
        RuleFor(x => x.Dto.BookingId).NotEmpty();
    }
}

public class RecordManualPaymentCommandHandler(
    IBookingRepository bookingRepository,
    IPaymentRepository paymentRepository,
    IMediator mediator,
    IMapper mapper) : IRequestHandler<RecordManualPaymentCommand, PaymentDto>
{
    public async Task<PaymentDto> Handle(RecordManualPaymentCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(request.Dto.BookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), request.Dto.BookingId);

        if (booking.Status is BookingStatus.Completed or BookingStatus.Cancelled)
            throw new ConflictException($"This booking is {booking.Status.ToString().ToLower()} and can no longer accept payment.");

        var payment = new Payment
        {
            BookingId = booking.Id,
            AgentId = booking.AgentId,
            Provider = PaymentProvider.Manual,
            ProviderOrderId = $"MANUAL-{Guid.NewGuid():N}",
            Reference = string.IsNullOrWhiteSpace(request.Dto.Reference) ? "Manual / test payment" : request.Dto.Reference,
            Amount = booking.BookingAmount,
            Currency = "INR",
            Status = PaymentStatus.Succeeded,
            PaidAt = DateTime.UtcNow,
        };
        payment = await paymentRepository.AddAsync(payment, cancellationToken);

        // A successful payment is what closes the deal — it takes the booking straight to
        // Completed (which cascades the unit to Sold), not just "Confirmed". There's no manual
        // "mark completed" step anymore; payment success is the only way to get there.
        if (booking.Status is BookingStatus.Pending or BookingStatus.Confirmed)
        {
            await mediator.Send(
                new UpdateBookingStatusCommand(booking.Id, new UpdateBookingStatusDto { Status = nameof(BookingStatus.Completed) }),
                cancellationToken);
        }

        return mapper.Map<PaymentDto>(payment);
    }
}
