using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace RealEstate.Infrastructure.Messaging;

/// <summary>
/// Lazily creates and holds one shared RabbitMQ connection for the process. Both the publisher
/// and the reindex consumer pull channels from it, matching StackExchange.Redis's single-shared-
/// multiplexer style rather than opening a connection per use.
/// </summary>
public sealed class RabbitMqConnectionManager(IOptions<RabbitMqSettings> options) : IAsyncDisposable
{
    private readonly RabbitMqSettings settings = options.Value;
    private readonly SemaphoreSlim gate = new(1, 1);
    private IConnection? connection;

    public async Task<IConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        if (connection is { IsOpen: true })
            return connection;

        await gate.WaitAsync(ct);
        try
        {
            if (connection is { IsOpen: true })
                return connection;

            var factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                Port = settings.Port,
                VirtualHost = settings.VirtualHost,
                UserName = settings.UserName,
                Password = settings.Password,
            };

            connection = await factory.CreateConnectionAsync(ct);
            return connection;
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
            await connection.DisposeAsync();
    }
}
