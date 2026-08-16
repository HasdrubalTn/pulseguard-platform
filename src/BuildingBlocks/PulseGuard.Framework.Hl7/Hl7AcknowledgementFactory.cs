namespace PulseGuard.Framework.Hl7;

public sealed class Hl7AcknowledgementFactory
{
    private readonly TimeProvider _timeProvider;

    public Hl7AcknowledgementFactory()
        : this(TimeProvider.System)
    {
    }

    public Hl7AcknowledgementFactory(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    public string CreateApplicationAccept(Hl7Message request) => Create(request, "AA", null);

    public string CreateApplicationError(Hl7Message request, string error) => Create(request, "AE", error);

    private string Create(Hl7Message request, string acknowledgementCode, string? error)
    {
        ArgumentNullException.ThrowIfNull(request);

        string timestamp = _timeProvider
            .GetUtcNow()
            .ToString("yyyyMMddHHmmsszzz", System.Globalization.CultureInfo.InvariantCulture)
            .Replace(":", string.Empty, StringComparison.Ordinal);
        string acknowledgementControlId = Guid.NewGuid().ToString("N");
        string trigger = string.IsNullOrWhiteSpace(request.TriggerEvent) ? string.Empty : $"^{request.TriggerEvent}";

        List<string> segments =
        [
            $"MSH|^~\\&|PULSEGUARD|PULSEGUARD|{request.SendingApplication}|{request.SendingFacility}|{timestamp}||ACK{trigger}|{acknowledgementControlId}|P|{request.Version}",
            $"MSA|{acknowledgementCode}|{request.ControlId}",
        ];

        if (!string.IsNullOrWhiteSpace(error))
        {
            segments.Add($"ERR|||207^Application internal error^HL70357|E|||{Sanitize(error)}");
        }

        return string.Join('\r', segments) + '\r';
    }

    private static string Sanitize(string value) => value.Replace('|', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
