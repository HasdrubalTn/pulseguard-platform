namespace PulseGuard.Framework.Observability;

public sealed class ObservabilityOptions
{
    public const string SectionName = "OpenTelemetry";

    public bool Enabled { get; init; }

    public Uri OtlpEndpoint { get; init; } = new("http://localhost:4317");

    public string ServiceNamespace { get; init; } = "PulseGuard";
}
