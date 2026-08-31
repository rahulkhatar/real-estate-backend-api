using FluentAssertions;
using Moq;
using RealEstate.Application.Features.Chat.Commands;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Entities;
using RealEstate.Core.Interfaces;
using RealEstate.Core.ValueObjects;
using Xunit;

namespace RealEstate.Tests.Features;

public class ReindexListingsCommandHandlerTests
{
    private readonly Mock<IUnitRepository> _unitRepo = new();
    private readonly Mock<IListingEmbeddingRepository> _embeddingRepo = new();
    private readonly Mock<IEmbeddingService> _embeddingService = new();

    private ReindexListingsCommandHandler CreateHandler() =>
        new(_unitRepo.Object, _embeddingRepo.Object, _embeddingService.Object);

    private static Core.Entities.Unit MakeUnit(string id, string unitNumber) => new()
    {
        Id = id,
        UnitNumber = unitNumber,
        PropertySnapshot = new PropertySnapshot { Name = "Block A" },
        ProjectSnapshot = new ProjectSnapshot { Name = "Test Project", City = "Mumbai" },
        Size = new SizeInfo { Value = 900 },
        Price = 5_000_000,
    };

    [Fact]
    public async Task Handle_IndexesEveryUnitAndUpsertsByUnitId()
    {
        var units = new List<Core.Entities.Unit> { MakeUnit("u1", "A-1"), MakeUnit("u2", "A-2") };
        _unitRepo.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(units);
        _embeddingService
            .Setup(s => s.EmbedManyAsync(It.Is<IReadOnlyList<string>>(t => t.Count == 2), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new float[] { 1, 2 }, new float[] { 3, 4 }]);

        var handler = CreateHandler();
        var result = await handler.Handle(new ReindexListingsCommand(), CancellationToken.None);

        result.IndexedCount.Should().Be(2);
        _embeddingRepo.Verify(r => r.UpsertAsync(It.Is<ListingEmbedding>(e => e.UnitId == "u1"), It.IsAny<CancellationToken>()), Times.Once);
        _embeddingRepo.Verify(r => r.UpsertAsync(It.Is<ListingEmbedding>(e => e.UnitId == "u2"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoUnits_IndexesNothing()
    {
        _unitRepo.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var handler = CreateHandler();
        var result = await handler.Handle(new ReindexListingsCommand(), CancellationToken.None);

        result.IndexedCount.Should().Be(0);
        _embeddingService.Verify(s => s.EmbedManyAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
