using AutoMapper;
using FluentAssertions;
using MediatR;
using Moq;
using RealEstate.Application.Common.Mappings;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Bookings.Commands;
using RealEstate.Application.Features.Payments.Commands;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;
using Xunit;

namespace RealEstate.Tests.Features;

public class RecordManualPaymentCommandHandlerTests
{
    private readonly Mock<IBookingRepository> _bookingRepo = new();
    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly IMapper _mapper;

    public RecordManualPaymentCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();

        _paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken _) => p);
    }

    private RecordManualPaymentCommandHandler CreateHandler() =>
        new(_bookingRepo.Object, _paymentRepo.Object, _mediator.Object, _mapper);

    [Fact]
    public async Task Handle_PendingBooking_RecordsSucceededPaymentAndCompletesBooking()
    {
        var booking = new Booking { Id = "b1", AgentId = "a1", BookingAmount = 30000, Status = BookingStatus.Pending };
        _bookingRepo.Setup(r => r.GetByIdAsync("b1", It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var handler = CreateHandler();
        var dto = new RecordManualPaymentDto { BookingId = "b1", Reference = "Cash" };
        var result = await handler.Handle(new RecordManualPaymentCommand(dto), CancellationToken.None);

        result.Status.Should().Be("Succeeded");
        result.Provider.Should().Be("Manual");
        result.Reference.Should().Be("Cash");
        _paymentRepo.Verify(r => r.AddAsync(It.Is<Payment>(p => p.Status == PaymentStatus.Succeeded && p.PaidAt != null), It.IsAny<CancellationToken>()), Times.Once);
        _mediator.Verify(
            m => m.Send(It.Is<UpdateBookingStatusCommand>(c => c.Id == "b1" && c.Dto.Status == "Completed"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ConfirmedBooking_PaymentAlsoCompletesBooking()
    {
        var booking = new Booking { Id = "b1", AgentId = "a1", BookingAmount = 30000, Status = BookingStatus.Confirmed };
        _bookingRepo.Setup(r => r.GetByIdAsync("b1", It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var handler = CreateHandler();
        var dto = new RecordManualPaymentDto { BookingId = "b1" };
        await handler.Handle(new RecordManualPaymentCommand(dto), CancellationToken.None);

        _mediator.Verify(
            m => m.Send(It.Is<UpdateBookingStatusCommand>(c => c.Id == "b1" && c.Dto.Status == "Completed"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NoReferenceGiven_DefaultsToPlaceholderText()
    {
        var booking = new Booking { Id = "b1", AgentId = "a1", BookingAmount = 30000, Status = BookingStatus.Pending };
        _bookingRepo.Setup(r => r.GetByIdAsync("b1", It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var handler = CreateHandler();
        var dto = new RecordManualPaymentDto { BookingId = "b1", Reference = "" };
        var result = await handler.Handle(new RecordManualPaymentCommand(dto), CancellationToken.None);

        result.Reference.Should().Be("Manual / test payment");
    }

    [Theory]
    [InlineData(BookingStatus.Completed)]
    [InlineData(BookingStatus.Cancelled)]
    public async Task Handle_TerminalBooking_ThrowsConflict(BookingStatus status)
    {
        var booking = new Booking { Id = "b1", AgentId = "a1", Status = status };
        _bookingRepo.Setup(r => r.GetByIdAsync("b1", It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var handler = CreateHandler();
        var act = () => handler.Handle(new RecordManualPaymentCommand(new RecordManualPaymentDto { BookingId = "b1" }), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _paymentRepo.Verify(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
