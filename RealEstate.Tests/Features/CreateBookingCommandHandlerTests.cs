using AutoMapper;
using FluentAssertions;
using Moq;
using RealEstate.Application.Common.Mappings;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Bookings.Commands;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;
using Xunit;

namespace RealEstate.Tests.Features;

public class CreateBookingCommandHandlerTests
{
    private readonly Mock<IBookingRepository> _bookingRepo = new();
    private readonly Mock<IUnitRepository> _unitRepo = new();
    private readonly Mock<IPropertyRepository> _propertyRepo = new();
    private readonly Mock<IAgentRepository> _agentRepo = new();
    private readonly IMapper _mapper;

    public CreateBookingCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
    }

    private CreateBookingCommandHandler CreateHandler() =>
        new(_bookingRepo.Object, _unitRepo.Object, _propertyRepo.Object, _agentRepo.Object, _mapper);

    private static Core.Entities.Unit AvailableUnit() => new()
    {
        Id = "u1",
        PropertyId = "p1",
        ProjectId = "pr1",
        UnitNumber = "A-1",
        Status = UnitStatus.Available,
        Price = 5_000_000,
    };

    private static CreateBookingDto Dto() => new()
    {
        UnitId = "u1",
        AgentId = "a1",
        CustomerName = "Jane Buyer",
        CustomerEmail = "jane@example.com",
        CustomerPhone = "9999999999",
        BookingAmount = 50000,
    };

    [Fact]
    public async Task Handle_ValidRequest_CreditsTheExplicitlyChosenAgent()
    {
        var unit = AvailableUnit();
        var agent = new Agent { Id = "a1", Name = "Agent One", TotalBookings = 2 };
        _unitRepo.Setup(r => r.GetByIdAsync("u1", It.IsAny<CancellationToken>())).ReturnsAsync(unit);
        _agentRepo.Setup(r => r.GetByIdAsync("a1", It.IsAny<CancellationToken>())).ReturnsAsync(agent);
        _bookingRepo.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking b, CancellationToken _) => b);

        var handler = CreateHandler();
        var result = await handler.Handle(new CreateBookingCommand(Dto()), CancellationToken.None);

        result.AgentId.Should().Be("a1");
        agent.TotalBookings.Should().Be(3);
        _unitRepo.Verify(r => r.UpdateStatusAsync("u1", UnitStatus.Booked, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownAgent_ThrowsNotFound()
    {
        _unitRepo.Setup(r => r.GetByIdAsync("u1", It.IsAny<CancellationToken>())).ReturnsAsync(AvailableUnit());
        _agentRepo.Setup(r => r.GetByIdAsync("a1", It.IsAny<CancellationToken>())).ReturnsAsync((Agent?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(new CreateBookingCommand(Dto()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_UnitNotAvailable_ThrowsConflict()
    {
        var unit = AvailableUnit();
        unit.Status = UnitStatus.Booked;
        _unitRepo.Setup(r => r.GetByIdAsync("u1", It.IsAny<CancellationToken>())).ReturnsAsync(unit);

        var handler = CreateHandler();
        var act = () => handler.Handle(new CreateBookingCommand(Dto()), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _agentRepo.Verify(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
