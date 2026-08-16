namespace PulseGuard.DeviceGateway;

public sealed class MllpListenerOptions
{
    public const string SectionName = "Hl7:Mllp";

    public string BindAddress { get; init; } = "127.0.0.1";

    public int Port { get; init; } = 2575;

    public int MaximumMessageBytes { get; init; } = 1024 * 1024;

    public int ReadTimeoutSeconds { get; init; } = 30;

    public int MaximumConcurrentConnections { get; init; } = 100;
}
