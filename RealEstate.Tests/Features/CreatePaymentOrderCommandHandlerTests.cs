using FluentAssertions;
using Moq;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Payments.Commands;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;
using Xunit;

namespace RealEstate.Tests.Features;

public class CreatePaymentOrderCommandHandlerTests
{
    private readonly Mock<IBookingRepository> _bookingRepo = new();
    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<IPaymentGateway> _stripeGateway = new();
    private readonly Mock<IPaymentGateway> _razorpayGateway = new();

    public CreatePaymentOrderCommandHandlerTests()
    {
        _stripeGateway.Setup(g => g.Provider).Returns(PaymentProvider.Stripe);
        _razorpayGateway.Setup(g => g.Provider).Returns(PaymentProvider.Razorpay);

        _paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken _) => p);
    }

    private CreatePaymentOrderCommandHandler CreateHandler() =>
        new(_bookingRepo.Object, _paymentRepo.Object, [_stripeGateway.Object, _razorpayGateway.Object]);

    private static Booking PendingBooking() => new()
    {
        Id = "b1",
        AgentId = "a1",
        BookingAmount = 50000,
        Status = BookingStatus.Pending,
        UnitSnapshot = new() { UnitNumber = "A-101" },
    };

    [Fact]
    public async Task Handle_ConfiguredGateway_CreatesPaymentAndReturnsOrder()
    {
        var booking = PendingBooking();
        _bookingRepo.Setup(r => r.GetByIdAsync("b1", It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        _stripeGateway.Setup(g => g.IsConfigured).Returns(true);
        _stripeGateway.Setup(g => g.CreateOrderAsync(It.IsAny<CreatePaymentOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentOrderResult("pi_123", "secret_123", "pk_test", "INR", 50000));

        var handler = CreateHandler();
        var result = await handler.Handle(new CreatePaymentOrderCommand(new CreatePaymentOrderDto { BookingId = "b1", Provider = "Stripe" }), CancellationToken.None);

        result.ProviderOrderId.Should().Be("pi_123");
        result.ClientSecret.Should().Be("secret_123");
        _paymentRepo.Verify(r => r.AddAsync(It.Is<Payment>(p => p.BookingId == "b1" && p.Amount == 50000), It.IsAny<CancellationToken>()), Times.Once);
        _paymentRepo.Verify(r => r.UpdateAsync(It.Is<Payment>(p => p.ProviderOrderId == "pi_123"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnconfiguredGateway_ThrowsPaymentGatewayNotConfigured()
    {
        var booking = PendingBooking();
        _bookingRepo.Setup(r => r.GetByIdAsync("b1", It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        _razorpayGateway.Setup(g => g.IsConfigured).Returns(false);

        var handler = CreateHandler();
        var act = () => handler.Handle(new CreatePaymentOrderCommand(new CreatePaymentOrderDto { BookingId = "b1", Provider = "Razorpay" }), CancellationToken.None);

        await act.Should().ThrowAsync<PaymentGatewayNotConfiguredException>();
        _paymentRepo.Verify(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(BookingStatus.Completed)]
    [InlineData(BookingStatus.Cancelled)]
    public async Task Handle_TerminalBooking_ThrowsConflict(BookingStatus status)
    {
        var booking = PendingBooking();
        booking.Status = status;
        _bookingRepo.Setup(r => r.GetByIdAsync("b1", It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var handler = CreateHandler();
        var act = () => handler.Handle(new CreatePaymentOrderCommand(new CreatePaymentOrderDto { BookingId = "b1", Provider = "Stripe" }), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
