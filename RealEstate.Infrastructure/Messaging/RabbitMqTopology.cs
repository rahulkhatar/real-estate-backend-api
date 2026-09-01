using RabbitMQ.Client;

namespace RealEstate.Infrastructure.Messaging;

/// <summary>
/// Idempotent exchange/queue declarations shared by the publisher and the consumer -- either one
/// may be the first to run, so both declare the same topology before using it.
/// </summary>
internal static class RabbitMqTopology
{
    public const string RoutingKey = "unit.reindex";

    public static async Task DeclareAsync(IChannel channel, RabbitMqSettings settings, CancellationToken ct = default)
    {
        var deadLetterExchangeName = settings.ExchangeName + ".dlx";
        var deadLetterQueueName = settings.QueueName + ".dead";

        await channel.ExchangeDeclareAsync(deadLetterExchangeName, ExchangeType.Fanout, durable: true, autoDelete: false, cancellationToken: ct);
        await channel.QueueDeclareAsync(deadLetterQueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(deadLetterQueueName, deadLetterExchangeName, routingKey: string.Empty, cancellationToken: ct);

        await channel.ExchangeDeclareAsync(settings.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);

        var queueArgs = new Dictionary<string, object?> { ["x-dead-letter-exchange"] = deadLetterExchangeName };
        await channel.QueueDeclareAsync(settings.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs, cancellationToken: ct);
        await channel.QueueBindAsync(settings.QueueName, settings.ExchangeName, RoutingKey, cancellationToken: ct);
    }
}
