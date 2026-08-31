using FluentAssertions;
using MediatR;
using Moq;
using RealEstate.Application.Features.Bookings.Commands;
using RealEstate.Application.Features.Payments.Commands;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;
using Xunit;

namespace RealEstate.Tests.Features;

public class ProcessPaymentWebhookCommandHandlerTests
{
    private readonly Mock<IPaymentGateway> _stripeGateway = new();
    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<IBookingRepository> _bookingRepo = new();
    private readonly Mock<IMediator> _mediator = new();

    public ProcessPaymentWebhookCommandHandlerTests()
    {
        _stripeGateway.Setup(g => g.Provider).Returns(PaymentProvider.Stripe);
    }

    private ProcessPaymentWebhookCommandHandler CreateHandler() =>
        new([_stripeGateway.Object], _paymentRepo.Object, _bookingRepo.Object, _mediator.Object);

    [Fact]
    public async Task Handle_InvalidSignature_ThrowsUnauthorized()
    {
        _stripeGateway.Setup(g => g.ParseWebhook(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new PaymentWebhookEvent(false, "", "", PaymentStatus.Failed, ""));

        var handler = CreateHandler();
        var act = () => handler.Handle(new ProcessPaymentWebhookCommand(PaymentProvider.Stripe, "{}", "bad-sig"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAppException>();
    }

    [Fact]
    public async Task Handle_UnknownOrder_ReturnsFalseWithoutThrowing()
    {
        _stripeGateway.Setup(g => g.ParseWebhook(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new PaymentWebhookEvent(true, "pi_unknown", "pi_unknown", PaymentStatus.Succeeded, "payment_intent.succeeded"));
        _paymentRepo.Setup(r => r.GetByProviderOrderIdAsync(PaymentProvider.Stripe, "pi_unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new ProcessPaymentWebhookCommand(PaymentProvider.Stripe, "{}", "sig"), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Succeeded_UpdatesPaymentAndCompletesPendingBooking()
    {
        var payment = new Payment { Id = "p1", BookingId = "b1", Provider = PaymentProvider.Stripe, ProviderOrderId = "pi_1", Status = PaymentStatus.Created };
        var booking = new Booking { Id = "b1", Status = BookingStatus.Pending };

        _stripeGateway.Setup(g => g.ParseWebhook(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new PaymentWebhookEvent(true, "pi_1", "pi_1", PaymentStatus.Succeeded, "payment_intent.succeeded"));
        _paymentRepo.Setup(r => r.GetByProviderOrderIdAsync(PaymentProvider.Stripe, "pi_1", It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _bookingRepo.Setup(r => r.GetByIdAsync("b1", It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var handler = CreateHandler();
        var result = await handler.Handle(new ProcessPaymentWebhookCommand(PaymentProvider.Stripe, "{}", "sig"), CancellationToken.None);

        result.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Succeeded);
        payment.PaidAt.Should().NotBeNull();
        _paymentRepo.Verify(r => r.UpdateAsync(payment, It.IsAny<CancellationToken>()), Times.Once);
        _mediator.Verify(
            m => m.Send(It.Is<UpdateBookingStatusCommand>(c => c.Id == "b1" && c.Dto.Status == "Completed"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadySucceeded_IsIdempotentAndSkipsUpdate()
    {
        var payment = new Payment { Id = "p1", BookingId = "b1", Provider = PaymentProvider.Stripe, ProviderOrderId = "pi_1", Status = PaymentStatus.Succeeded };

        _stripeGateway.Setup(g => g.ParseWebhook(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new PaymentWebhookEvent(true, "pi_1", "pi_1", PaymentStatus.Succeeded, "payment_intent.succeeded"));
        _paymentRepo.Setup(r => r.GetByProviderOrderIdAsync(PaymentProvider.Stripe, "pi_1", It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var handler = CreateHandler();
        var result = await handler.Handle(new ProcessPaymentWebhookCommand(PaymentProvider.Stripe, "{}", "sig"), CancellationToken.None);

        result.Should().BeTrue();
        _paymentRepo.Verify(r => r.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediator.Verify(m => m.Send(It.IsAny<UpdateBookingStatusCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
