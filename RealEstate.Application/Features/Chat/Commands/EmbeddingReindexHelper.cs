using MediatR;
using Microsoft.Extensions.Logging;

namespace RealEstate.Application.Features.Chat.Commands;

internal static class EmbeddingReindexHelper
{
    public static async Task ReindexUnitsAsync(
        IReadOnlyList<Core.Entities.Unit> units,
        IMediator mediator,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        foreach (var unit in units)
        {
            try
            {
                await mediator.Send(new IndexUnitEmbeddingCommand(unit.Id), cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to re-index unit {UnitId} for the AI chat assistant.", unit.Id);
            }
        }
    }
}
