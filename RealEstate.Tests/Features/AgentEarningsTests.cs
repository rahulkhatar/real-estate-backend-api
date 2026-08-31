using AutoMapper;
using FluentAssertions;
using Moq;
using RealEstate.Application.Common.Mappings;
using RealEstate.Application.DTOs;
using RealEstate.Application.Features.Agents;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Exceptions;
using RealEstate.Core.Interfaces;
using RealEstate.Core.ValueObjects;
using Xunit;

namespace RealEstate.Tests.Features;

public class GetAgentEarningsQueryHandlerTests
{
    private readonly Mock<IAgentRepository> _agentRepo = new();
    private readonly Mock<IBookingRepository> _bookingRepo = new();

    private GetAgentEarningsQueryHandler CreateHandler() => new(_agentRepo.Object, _bookingRepo.Object);

    private static Booking CompletedBooking(string id, decimal totalAmount, DateTime updatedAt) => new()
    {
        Id = id,
        AgentId = "a1",
        Status = BookingStatus.Completed,
        TotalAmount = totalAmount,
        CustomerName = "Test Customer",
        UnitSnapshot = new UnitSnapshot { UnitNumber = "A-1" },
        PropertySnapshot = new PropertySnapshot { Name = "Block A" },
        ProjectSnapshot = new ProjectSnapshot { Name = "Test Project" },
        UpdatedAt = updatedAt,
    };

    [Fact]
    public async Task Handle_OnlyCountsCompletedBookings_AndComputesCommissionFromCurrentRate()
    {
        var agent = new Agent { Id = "a1", Name = "Agent One", CommissionPercentage = 2 };
        var bookings = new List<Booking>
        {
            CompletedBooking("b1", 5_000_000, new DateTime(2026, 7, 10)),
            CompletedBooking("b2", 3_000_000, new DateTime(2026, 8, 5)),
            new() { Id = "b3", AgentId = "a1", Status = BookingStatus.Pending, TotalAmount = 9_000_000 },
            new() { Id = "b4", AgentId = "a1", Status = BookingStatus.Cancelled, TotalAmount = 9_000_000 },
        };

        _agentRepo.Setup(r => r.GetByIdAsync("a1", It.IsAny<CancellationToken>())).ReturnsAsync(agent);
        _bookingRepo.Setup(r => r.GetByAgentIdAsync("a1", It.IsAny<CancellationToken>())).ReturnsAsync(bookings);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetAgentEarningsQuery("a1"), CancellationToken.None);

        result.TotalDeals.Should().Be(2);
        result.TotalRevenue.Should().Be(8_000_000);
        result.TotalCommission.Should().Be(160_000); // 2% of 8,000,000
        result.AverageCommissionPerDeal.Should().Be(80_000);
        result.History.Should().HaveCount(2);
        result.MonthlyBreakdown.Should().HaveCount(2);
        result.MonthlyBreakdown.Should().Contain(m => m.Month == "2026-07" && m.Deals == 1 && m.Revenue == 5_000_000);
        result.MonthlyBreakdown.Should().Contain(m => m.Month == "2026-08" && m.Deals == 1 && m.Revenue == 3_000_000);
    }

    [Fact]
    public async Task Handle_NoCompletedBookings_ReturnsZeroedTotals()
    {
        var agent = new Agent { Id = "a1", CommissionPercentage = 2 };
        _agentRepo.Setup(r => r.GetByIdAsync("a1", It.IsAny<CancellationToken>())).ReturnsAsync(agent);
        _bookingRepo.Setup(r => r.GetByAgentIdAsync("a1", It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetAgentEarningsQuery("a1"), CancellationToken.None);

        result.TotalDeals.Should().Be(0);
        result.TotalCommission.Should().Be(0);
        result.AverageCommissionPerDeal.Should().Be(0);
    }

    [Fact]
    public async Task Handle_UnknownAgent_ThrowsNotFound()
    {
        _agentRepo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((Agent?)null);

        var handler = CreateHandler();
        var act = () => handler.Handle(new GetAgentEarningsQuery("missing"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class UpdateAgentCommissionCommandHandlerTests
{
    private readonly Mock<IAgentRepository> _agentRepo = new();
    private readonly IMapper _mapper;

    public UpdateAgentCommissionCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
    }

    private UpdateAgentCommissionCommandHandler CreateHandler() => new(_agentRepo.Object, _mapper);

    [Fact]
    public async Task Handle_UpdatesCommissionPercentage()
    {
        var agent = new Agent { Id = "a1", CommissionPercentage = 2 };
        _agentRepo.Setup(r => r.GetByIdAsync("a1", It.IsAny<CancellationToken>())).ReturnsAsync(agent);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateAgentCommissionCommand("a1", new UpdateAgentCommissionDto { CommissionPercentage = 3.5m }), CancellationToken.None);

        agent.CommissionPercentage.Should().Be(3.5m);
        result.CommissionPercentage.Should().Be(3.5m);
        _agentRepo.Verify(r => r.UpdateAsync(agent, It.IsAny<CancellationToken>()), Times.Once);
    }
}
