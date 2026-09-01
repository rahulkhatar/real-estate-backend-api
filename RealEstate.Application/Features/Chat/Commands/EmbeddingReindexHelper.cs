using Microsoft.Extensions.Logging;
using RealEstate.Application.Interfaces;

namespace RealEstate.Application.Features.Chat.Commands;

internal static class EmbeddingReindexHelper
{
    public static async Task ReindexUnitsAsync(
        IReadOnlyList<Core.Entities.Unit> units,
        IUnitReindexPublisher reindexPublisher,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        foreach (var unit in units)
        {
            try
            {
                await reindexPublisher.PublishAsync(unit.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to queue re-index for unit {UnitId} for the AI chat assistant.", unit.Id);
            }
        }
    }
}
