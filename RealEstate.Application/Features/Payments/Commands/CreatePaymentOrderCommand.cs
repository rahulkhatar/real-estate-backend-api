using FluentValidation;
using MediatR;
using RealEstate.Application.DTOs;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Payments.Commands;

public record CreatePaymentOrderCommand(CreatePaymentOrderDto Dto) : IRequest<PaymentOrderResponseDto>;

public class CreatePaymentOrderCommandValidator : AbstractValidator<CreatePaymentOrderCommand>
{
    public CreatePaymentOrderCommandValidator()
    {
        RuleFor(x => x.Dto.BookingId).NotEmpty();
        RuleFor(x => x.Dto.Provider)
            .NotEmpty()
            .Must(p => Enum.TryParse<PaymentProvider>(p, true, out var parsed) && parsed != PaymentProvider.Manual)
            .WithMessage("Provider must be 'Stripe' or 'Razorpay'. Use POST /api/payments/manual to record an offline payment.");
    }
}

public class CreatePaymentOrderCommandHandler(
    IBookingRepository bookingRepository,
    IPaymentRepository paymentRepository,
    IEnumerable<IPaymentGateway> gateways) : IRequestHandler<CreatePaymentOrderCommand, PaymentOrderResponseDto>
{
    public async Task<PaymentOrderResponseDto> Handle(CreatePaymentOrderCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(request.Dto.BookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), request.Dto.BookingId);

        if (booking.Status is BookingStatus.Completed or BookingStatus.Cancelled)
            throw new ConflictException($"This booking is {booking.Status.ToString().ToLower()} and can no longer accept payment.");

        var provider = Enum.Parse<PaymentProvider>(request.Dto.Provider, true);
        var gateway = gateways.Single(g => g.Provider == provider);

        if (!gateway.IsConfigured)
            throw new PaymentGatewayNotConfiguredException(provider.ToString());

        var payment = new Payment
        {
            BookingId = booking.Id,
            AgentId = booking.AgentId,
            Provider = provider,
            Amount = booking.BookingAmount,
            Currency = "INR",
            Status = PaymentStatus.Created,
        };
        payment = await paymentRepository.AddAsync(payment, cancellationToken);

        var orderResult = await gateway.CreateOrderAsync(
            new CreatePaymentOrderRequest(payment.Id, payment.Amount, payment.Currency, $"Booking {booking.UnitSnapshot.UnitNumber}"),
            cancellationToken);

        payment.ProviderOrderId = orderResult.ProviderOrderId;
        await paymentRepository.UpdateAsync(payment, cancellationToken);

        return new PaymentOrderResponseDto
        {
            PaymentId = payment.Id,
            Provider = provider.ToString(),
            ProviderOrderId = orderResult.ProviderOrderId,
            ClientSecret = orderResult.ClientSecret,
            PublicKey = orderResult.PublicKey,
            Amount = orderResult.Amount,
            Currency = orderResult.Currency,
        };
    }
}
