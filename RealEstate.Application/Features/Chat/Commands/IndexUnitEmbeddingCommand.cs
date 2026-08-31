using MediatR;
using RealEstate.Application.Interfaces;
using RealEstate.Core.Entities;
using RealEstate.Core.Interfaces;

namespace RealEstate.Application.Features.Chat.Commands;

/// <summary>
/// Re-embeds and upserts a single unit's AI listing index entry. Fired from Create/UpdateUnitCommand
/// so the chat assistant reflects catalog changes without a manual reindex. Best-effort by design —
/// callers should swallow failures (e.g. OpenAI not configured) rather than let indexing block the
/// underlying Unit CRUD operation from succeeding.
/// </summary>
public record IndexUnitEmbeddingCommand(string UnitId) : IRequest<bool>;

public class IndexUnitEmbeddingCommandHandler(
    IUnitRepository unitRepository,
    IListingEmbeddingRepository embeddingRepository,
    IEmbeddingService embeddingService) : IRequestHandler<IndexUnitEmbeddingCommand, bool>
{
    public async Task<bool> Handle(IndexUnitEmbeddingCommand request, CancellationToken cancellationToken)
    {
        var unit = await unitRepository.GetByIdAsync(request.UnitId, cancellationToken);
        if (unit is null) return false;

        var text = ListingTextBuilder.Build(unit);
        var vector = await embeddingService.EmbedAsync(text, cancellationToken);

        await embeddingRepository.UpsertAsync(new ListingEmbedding
        {
            UnitId = unit.Id,
            PropertyId = unit.PropertyId,
            ProjectId = unit.ProjectId,
            SourceText = text,
            Vector = vector,
            UnitNumber = unit.UnitNumber,
            PropertyName = unit.PropertySnapshot.Name,
            ProjectName = unit.ProjectSnapshot.Name,
            City = unit.ProjectSnapshot.City,
            Type = unit.Type.ToString(),
            Price = unit.Price,
            Status = unit.Status.ToString(),
            ImageUrl = unit.Images.Count > 0 ? unit.Images[0].Url : null,
        }, cancellationToken);

        return true;
    }
}
