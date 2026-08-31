using MediatR;
using RealEstate.Application.DTOs;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Entities;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Chat.Commands;

/// <summary>
/// Rebuilds the entire vector index in one batched pass — for a full backfill (e.g. after a reseed,
/// or for units that existed before auto-indexing was added). Day-to-day, individual Unit
/// create/update/delete now auto-index themselves via <see cref="IndexUnitEmbeddingCommand"/>; this
/// command is the manual "rebuild everything" fallback, kept Admin-only in the API.
/// </summary>
public record ReindexListingsCommand : IRequest<ReindexResultDto>;

public class ReindexListingsCommandHandler(
    IUnitRepository unitRepository,
    IListingEmbeddingRepository embeddingRepository,
    IEmbeddingService embeddingService) : IRequestHandler<ReindexListingsCommand, ReindexResultDto>
{
    private const int BatchSize = 100;

    public async Task<ReindexResultDto> Handle(ReindexListingsCommand request, CancellationToken cancellationToken)
    {
        var units = await unitRepository.ListAllAsync(cancellationToken);
        var indexed = 0;

        foreach (var batch in units.Chunk(BatchSize))
        {
            var texts = batch.Select(ListingTextBuilder.Build).ToList();
            var vectors = await embeddingService.EmbedManyAsync(texts, cancellationToken);

            for (var i = 0; i < batch.Length; i++)
            {
                var unit = batch[i];
                await embeddingRepository.UpsertAsync(new ListingEmbedding
                {
                    UnitId = unit.Id,
                    PropertyId = unit.PropertyId,
                    ProjectId = unit.ProjectId,
                    SourceText = texts[i],
                    Vector = vectors[i],
                    UnitNumber = unit.UnitNumber,
                    PropertyName = unit.PropertySnapshot.Name,
                    ProjectName = unit.ProjectSnapshot.Name,
                    City = unit.ProjectSnapshot.City,
                    Type = unit.Type.ToString(),
                    Price = unit.Price,
                    Status = unit.Status.ToString(),
                    ImageUrl = unit.Images.Count > 0 ? unit.Images[0].Url : null,
                }, cancellationToken);
                indexed++;
            }
        }

        return new ReindexResultDto { IndexedCount = indexed };
    }
}
