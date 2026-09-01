namespace RealEstate.Application.Interfaces;

/// <summary>
/// Publishes a "re-embed this unit" message to RabbitMQ instead of running the OpenAI embedding
/// call inline on the write request. Implementations must be best-effort (swallow/log broker
/// failures) -- a down queue must never break Unit CRUD, matching the existing inline behavior
/// it replaces.
/// </summary>
public interface IUnitReindexPublisher
{
    Task PublishAsync(string unitId, CancellationToken ct = default);
}
