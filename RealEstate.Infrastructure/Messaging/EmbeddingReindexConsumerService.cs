using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RealEstate.Application.Features.Chat.Commands;

namespace RealEstate.Infrastructure.Messaging;

/// <summary>
/// Consumes "re-embed this unit" messages published by RabbitMqUnitReindexPublisher and replays
/// them through the existing IndexUnitEmbeddingCommand handler, unchanged. Runs as a singleton
/// BackgroundService, so it only ever resolves scoped services (IMediator, repositories) from a
/// freshly created scope per message -- never via constructor injection, which would capture them
/// for the process lifetime.
/// </summary>
public class EmbeddingReindexConsumerService(
    RabbitMqConnectionManager connectionManager,
    IOptions<RabbitMqSettings> options,
    IServiceScopeFactory scopeFactory,
    ILogger<EmbeddingReindexConsumerService> logger) : BackgroundService
{
    private readonly RabbitMqSettings settings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // A broker outage pauses reindexing, it must never crash the API process -- back
                // off and retry the connection instead of letting the exception stop the host.
                logger.LogWarning(ex, "RabbitMQ reindex consumer lost its connection; retrying in 10s.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var connection = await connectionManager.GetConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await RabbitMqTopology.DeclareAsync(channel, settings, stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var unitId = ExtractUnitId(ea.Body);
            if (unitId is null)
            {
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                return;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new IndexUnitEmbeddingCommand(unitId), stoppingToken);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to process reindex message for unit {UnitId}; routing to dead-letter queue.", unitId);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(settings.QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        // The consumer's ReceivedAsync callback above does the actual work; block here until
        // shutdown or a connection failure throws, which the outer loop reconnects on.
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private static string? ExtractUnitId(ReadOnlyMemory<byte> body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("unitId", out var value) ? value.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
