namespace PulseGuard.Framework.Messaging.RabbitMq;

public sealed class RabbitMqOptions
{
    public const string SectionName = "Messaging:RabbitMq";

    public string HostName { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string VirtualHost { get; init; } = "/";

    public string UserName { get; init; } = "pulseguard";

    public string Password { get; init; } = string.Empty;

    public string ExchangeName { get; init; } = "pulseguard.events";

    public string ClientProvidedName { get; init; } = "pulseguard";

    public TimeSpan RequestedHeartbeat { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan ConnectionTimeout { get; init; } = TimeSpan.FromSeconds(10);

    public TimeSpan PublishTimeout { get; init; } = TimeSpan.FromSeconds(10);

    public Dictionary<string, string> Routes { get; init; } = new(StringComparer.Ordinal);
}
