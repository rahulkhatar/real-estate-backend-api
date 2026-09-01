namespace RealEstate.Infrastructure.Messaging;

public class RabbitMqSettings
{
    public const string SectionName = "RabbitMQ";

    public string HostName { get; set; } = string.Empty;
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ExchangeName { get; set; } = "real-estate.embedding";
    public string QueueName { get; set; } = "real-estate.embedding.reindex";
}
