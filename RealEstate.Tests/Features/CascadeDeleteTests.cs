using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using RealEstate.Application.Features.Projects.Commands;
using RealEstate.Application.Features.Properties.Commands;
using RealEstate.Application.Features.Units.Commands;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Interfaces;
using Xunit;
using Unit = RealEstate.Core.Entities.Unit;

namespace RealEstate.Tests.Features;

public class DeleteUnitCommandHandlerTests
{
    private readonly Mock<IUnitRepository> _unitRepo = new();
    private readonly Mock<IPropertyRepository> _propertyRepo = new();
    private readonly Mock<IUnitLayoutRepository> _layoutRepo = new();
    private readonly Mock<IBookingRepository> _bookingRepo = new();
    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<IListingEmbeddingRepository> _embeddingRepo = new();
    private readonly Mock<ILogger<DeleteUnitCommandHandler>> _logger = new();

    private DeleteUnitCommandHandler CreateHandler() => new(
        _unitRepo.Object, _propertyRepo.Object, _layoutRepo.Object,
        _bookingRepo.Object, _paymentRepo.Object, _embeddingRepo.Object, _logger.Object);

    [Fact]
    public async Task Handle_UnitWithLayoutsAndBookings_CascadesDeleteToAllChildren()
    {
        var unit = new Unit { Id = "u1", PropertyId = "p1" };
        var layout1 = new UnitLayout { Id = "l1", UnitId = "u1" };
        var layout2 = new UnitLayout { Id = "l2", UnitId = "u1" };
        var booking = new Booking { Id = "b1", UnitId = "u1" };
        var payment1 = new Payment { Id = "pay1", BookingId = "b1" };
        var payment2 = new Payment { Id = "pay2", BookingId = "b1" };

        _unitRepo.Setup(r => r.GetByIdAsync("u1", It.IsAny<CancellationToken>())).ReturnsAsync(unit);
        _layoutRepo.Setup(r => r.GetByUnitIdAsync("u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([layout1, layout2]);
        _bookingRepo.Setup(r => r.GetByUnitIdAsync("u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([booking]);
        _paymentRepo.Setup(r => r.GetHistoryByBookingIdAsync("b1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([payment1, payment2]);
        _propertyRepo.Setup(r => r.GetByIdAsync("p1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Property?)null);

        var handler = CreateHandler();
        await handler.Handle(new DeleteUnitCommand("u1"), CancellationToken.None);

        _unitRepo.Verify(r => r.DeleteAsync("u1", It.IsAny<CancellationToken>()), Times.Once);
        _layoutRepo.Verify(r => r.DeleteAsync("l1", It.IsAny<CancellationToken>()), Times.Once);
        _layoutRepo.Verify(r => r.DeleteAsync("l2", It.IsAny<CancellationToken>()), Times.Once);
        _paymentRepo.Verify(r => r.DeleteAsync("pay1", It.IsAny<CancellationToken>()), Times.Once);
        _paymentRepo.Verify(r => r.DeleteAsync("pay2", It.IsAny<CancellationToken>()), Times.Once);
        _bookingRepo.Verify(r => r.DeleteAsync("b1", It.IsAny<CancellationToken>()), Times.Once);
        _embeddingRepo.Verify(r => r.DeleteByUnitIdAsync("u1", It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class DeletePropertyCommandHandlerTests
{
    private readonly Mock<IPropertyRepository> _propertyRepo = new();
    private readonly Mock<IUnitRepository> _unitRepo = new();
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<IMediator> _mediator = new();

    private DeletePropertyCommandHandler CreateHandler() =>
        new(_propertyRepo.Object, _unitRepo.Object, _projectRepo.Object, _mediator.Object);

    [Fact]
    public async Task Handle_PropertyWithUnits_CascadesDeleteToEachUnitInsteadOfBlocking()
    {
        var property = new Property { Id = "p1", ProjectId = "proj1", Name = "Tower A" };
        var unit1 = new Unit { Id = "u1", PropertyId = "p1" };
        var unit2 = new Unit { Id = "u2", PropertyId = "p1" };

        _propertyRepo.Setup(r => r.GetByIdAsync("p1", It.IsAny<CancellationToken>())).ReturnsAsync(property);
        _unitRepo.Setup(r => r.GetByPropertyIdAsync("p1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([unit1, unit2]);
        _projectRepo.Setup(r => r.GetByIdAsync("proj1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var handler = CreateHandler();
        await handler.Handle(new DeletePropertyCommand("p1"), CancellationToken.None);

        _mediator.Verify(m => m.Send(
            It.Is<DeleteUnitCommand>(c => c.Id == "u1"), It.IsAny<CancellationToken>()), Times.Once);
        _mediator.Verify(m => m.Send(
            It.Is<DeleteUnitCommand>(c => c.Id == "u2"), It.IsAny<CancellationToken>()), Times.Once);
        _propertyRepo.Verify(r => r.DeleteAsync("p1", It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class DeleteProjectCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<IPropertyRepository> _propertyRepo = new();
    private readonly Mock<IMediator> _mediator = new();

    private DeleteProjectCommandHandler CreateHandler() =>
        new(_projectRepo.Object, _propertyRepo.Object, _mediator.Object);

    [Fact]
    public async Task Handle_ProjectWithProperties_CascadesDeleteToEachPropertyInsteadOfBlocking()
    {
        var project = new Project { Id = "proj1", Name = "Skyline" };
        var property1 = new Property { Id = "p1", ProjectId = "proj1" };
        var property2 = new Property { Id = "p2", ProjectId = "proj1" };

        _projectRepo.Setup(r => r.GetByIdAsync("proj1", It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _propertyRepo.Setup(r => r.GetByProjectIdAsync("proj1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([property1, property2]);

        var handler = CreateHandler();
        await handler.Handle(new DeleteProjectCommand("proj1"), CancellationToken.None);

        _mediator.Verify(m => m.Send(
            It.Is<DeletePropertyCommand>(c => c.Id == "p1"), It.IsAny<CancellationToken>()), Times.Once);
        _mediator.Verify(m => m.Send(
            It.Is<DeletePropertyCommand>(c => c.Id == "p2"), It.IsAny<CancellationToken>()), Times.Once);
        _projectRepo.Verify(r => r.DeleteAsync("proj1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
