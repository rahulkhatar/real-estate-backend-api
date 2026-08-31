using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RealEstate.Application.Common.Mappings;
using RealEstate.Application.Features.Units.Commands;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Interfaces;
using Xunit;

namespace RealEstate.Tests.Features;

public class UpdateUnitStatusCommandHandlerTests
{
    private readonly Mock<IUnitRepository> _unitRepo = new();
    private readonly Mock<IPropertyRepository> _propertyRepo = new();
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<IListingEmbeddingRepository> _embeddingRepo = new();
    private readonly Mock<ILogger<UpdateUnitStatusCommandHandler>> _logger = new();
    private readonly IMapper _mapper;

    public UpdateUnitStatusCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
    }

    private UpdateUnitStatusCommandHandler CreateHandler() =>
        new(_unitRepo.Object, _propertyRepo.Object, _projectRepo.Object, _embeddingRepo.Object, _logger.Object, _mapper);

    [Fact]
    public async Task Handle_LastUnitSold_MarksPropertyAndProjectSold()
    {
        var unit = new Unit { Id = "u1", PropertyId = "p1", ProjectId = "pr1", Status = UnitStatus.Available };
        var property = new Property { Id = "p1", ProjectId = "pr1", TotalUnits = 1, SoldUnits = 0, Status = PropertyStatus.Available };
        var project = new Project { Id = "pr1", TotalProperties = 1, SoldProperties = 0, Status = ProjectStatus.Active };

        _unitRepo.Setup(r => r.GetByIdAsync("u1", It.IsAny<CancellationToken>())).ReturnsAsync(unit);
        _propertyRepo.Setup(r => r.GetByIdAsync("p1", It.IsAny<CancellationToken>())).ReturnsAsync(property);
        _projectRepo.Setup(r => r.GetByIdAsync("pr1", It.IsAny<CancellationToken>())).ReturnsAsync(project);

        var handler = CreateHandler();
        await handler.Handle(new UpdateUnitStatusCommand("u1", "Sold"), CancellationToken.None);

        property.SoldUnits.Should().Be(1);
        property.Status.Should().Be(PropertyStatus.Sold);
        project.SoldProperties.Should().Be(1);
        project.Status.Should().Be(ProjectStatus.Sold);

        _propertyRepo.Verify(r => r.UpdateAsync(property, It.IsAny<CancellationToken>()), Times.Once);
        _projectRepo.Verify(r => r.UpdateAsync(project, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OneOfManyUnitsSold_DoesNotMarkPropertySold()
    {
        var unit = new Unit { Id = "u1", PropertyId = "p1", ProjectId = "pr1", Status = UnitStatus.Available };
        var property = new Property { Id = "p1", ProjectId = "pr1", TotalUnits = 5, SoldUnits = 0, Status = PropertyStatus.Available };

        _unitRepo.Setup(r => r.GetByIdAsync("u1", It.IsAny<CancellationToken>())).ReturnsAsync(unit);
        _propertyRepo.Setup(r => r.GetByIdAsync("p1", It.IsAny<CancellationToken>())).ReturnsAsync(property);

        var handler = CreateHandler();
        await handler.Handle(new UpdateUnitStatusCommand("u1", "Sold"), CancellationToken.None);

        property.SoldUnits.Should().Be(1);
        property.Status.Should().Be(PropertyStatus.Available);

        _projectRepo.Verify(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SameStatus_IsNoOp()
    {
        var unit = new Unit { Id = "u1", PropertyId = "p1", ProjectId = "pr1", Status = UnitStatus.Booked };
        _unitRepo.Setup(r => r.GetByIdAsync("u1", It.IsAny<CancellationToken>())).ReturnsAsync(unit);

        var handler = CreateHandler();
        await handler.Handle(new UpdateUnitStatusCommand("u1", "Booked"), CancellationToken.None);

        _unitRepo.Verify(r => r.UpdateAsync(It.IsAny<Unit>(), It.IsAny<CancellationToken>()), Times.Never);
        _propertyRepo.Verify(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
