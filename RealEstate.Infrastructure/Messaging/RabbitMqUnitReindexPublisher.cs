using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RealEstate.Application.Interfaces;

namespace RealEstate.Infrastructure.Messaging;

public class RabbitMqUnitReindexPublisher(
    RabbitMqConnectionManager connectionManager,
    IOptions<RabbitMqSettings> options,
    ILogger<RabbitMqUnitReindexPublisher> logger) : IUnitReindexPublisher
{
    private readonly RabbitMqSettings settings = options.Value;

    public async Task PublishAsync(string unitId, CancellationToken ct = default)
    {
        try
        {
            var connection = await connectionManager.GetConnectionAsync(ct);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
            await RabbitMqTopology.DeclareAsync(channel, settings, ct);

            var body = JsonSerializer.SerializeToUtf8Bytes(new { unitId });
            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
            };

            await channel.BasicPublishAsync(
                exchange: settings.ExchangeName,
                routingKey: RabbitMqTopology.RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            // Best-effort by design, same as the inline OpenAI call this replaces -- a down or
            // unreachable broker must never break Unit CRUD.
            logger.LogWarning(ex, "Failed to publish reindex message for unit {UnitId}.", unitId);
        }
    }
}
