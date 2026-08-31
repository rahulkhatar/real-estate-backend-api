using AutoMapper;
using FluentAssertions;
using MediatR;
using Moq;
using RealEstate.Application.Common.Mappings;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Bookings.Commands;
using RealEstate.Application.Features.Units.Commands;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;
using Xunit;

namespace RealEstate.Tests.Features;

public class UpdateBookingStatusCommandHandlerTests
{
    private readonly Mock<IBookingRepository> _bookingRepo = new();
    private readonly Mock<IUnitRepository> _unitRepo = new();
    private readonly Mock<IAgentRepository> _agentRepo = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly IMapper _mapper;

    public UpdateBookingStatusCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
    }

    private UpdateBookingStatusCommandHandler CreateHandler() =>
        new(_bookingRepo.Object, _unitRepo.Object, _agentRepo.Object, _mediator.Object, _mapper);

    private static Booking PendingBooking() => new()
    {
        Id = "b1",
        UnitId = "u1",
        AgentId = "a1",
        Status = BookingStatus.Pending,
    };

    [Fact]
    public async Task Handle_Complete_SellsUnitViaMediator()
    {
        var booking = PendingBooking();
        _bookingRepo.Setup(r => r.GetByIdAsync("b1", It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var handler = CreateHandler();
        var dto = new UpdateBookingStatusDto { Status = "Completed" };
        await handler.Handle(new UpdateBookingStatusCommand("b1", dto), CancellationToken.None);

        booking.Status.Should().Be(BookingStatus.Completed);
        _mediator.Verify(
            m => m.Send(
                It.Is<UpdateUnitStatusCommand>(c => c.Id == "u1" && c.Status == "Sold"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _bookingRepo.Verify(r => r.UpdateAsync(booking, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Complete_CreditsAgentTotalRevenue()
    {
        var booking = PendingBooking();
        booking.TotalAmount = 5_000_000;
        var agent = new Agent { Id = "a1", TotalRevenue = 1_000_000 };
        _bookingRepo.Setup(r => r.GetByIdAsync("b1", It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        _agentRepo.Setup(r => r.GetByIdAsync("a1", It.IsAny<CancellationToken>())).ReturnsAsync(agent);

        var handler = CreateHandler();
        var dto = new UpdateBookingStatusDto { Status = "Completed" };
        await handler.Handle(new UpdateBookingStatusCommand("b1", dto), CancellationToken.None);

        agent.TotalRevenue.Should().Be(6_000_000);
        _agentRepo.Verify(r => r.UpdateAsync(agent, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Cancel_FreesUnitAndDecrementsAgentBookings()
    {
        var booking = PendingBooking();
        var agent = new Agent { Id = "a1", TotalBookings = 3 };
        _bookingRepo.Setup(r => r.GetByIdAsync("b1", It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        _agentRepo.Setup(r => r.GetByIdAsync("a1", It.IsAny<CancellationToken>())).ReturnsAsync(agent);

        var handler = CreateHandler();
        var dto = new UpdateBookingStatusDto { Status = "Cancelled", CancellationReason = "Customer changed mind" };
        await handler.Handle(new UpdateBookingStatusCommand("b1", dto), CancellationToken.None);

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.CancellationReason.Should().Be("Customer changed mind");
        booking.CancelledAt.Should().NotBeNull();
        agent.TotalBookings.Should().Be(2);

        _unitRepo.Verify(r => r.UpdateStatusAsync("u1", UnitStatus.Available, It.IsAny<CancellationToken>()), Times.Once);
        _mediator.Verify(m => m.Send(It.IsAny<UpdateUnitStatusCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CancelAtZeroBookings_ClampsAtZero()
    {
        var booking = PendingBooking();
        var agent = new Agent { Id = "a1", TotalBookings = 0 };
        _bookingRepo.Setup(r => r.GetByIdAsync("b1", It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        _agentRepo.Setup(r => r.GetByIdAsync("a1", It.IsAny<CancellationToken>())).ReturnsAsync(agent);

        var handler = CreateHandler();
        var dto = new UpdateBookingStatusDto { Status = "Cancelled", CancellationReason = "Duplicate booking" };
        await handler.Handle(new UpdateBookingStatusCommand("b1", dto), CancellationToken.None);

        agent.TotalBookings.Should().Be(0);
    }

    [Theory]
    [InlineData(BookingStatus.Completed)]
    [InlineData(BookingStatus.Cancelled)]
    public async Task Handle_TerminalStatus_ThrowsConflict(BookingStatus terminalStatus)
    {
        var booking = PendingBooking();
        booking.Status = terminalStatus;
        _bookingRepo.Setup(r => r.GetByIdAsync("b1", It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var handler = CreateHandler();
        var dto = new UpdateBookingStatusDto { Status = "Confirmed" };

        var act = () => handler.Handle(new UpdateBookingStatusCommand("b1", dto), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_SameStatus_IsNoOp()
    {
        var booking = PendingBooking();
        _bookingRepo.Setup(r => r.GetByIdAsync("b1", It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var handler = CreateHandler();
        var dto = new UpdateBookingStatusDto { Status = "Pending" };
        await handler.Handle(new UpdateBookingStatusCommand("b1", dto), CancellationToken.None);

        _bookingRepo.Verify(r => r.UpdateAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitRepo.Verify(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<UnitStatus>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
