namespace PulseGuard.Framework.Hl7;

public sealed class Hl7Message
{
    internal Hl7Message(string raw, IReadOnlyList<Hl7Segment> segments)
    {
        Raw = raw;
        Segments = segments;

        Hl7Segment header = segments[0];
        string[] messageType = header.GetField(9).Split('^');
        MessageCode = messageType.ElementAtOrDefault(0) ?? string.Empty;
        TriggerEvent = messageType.ElementAtOrDefault(1) ?? string.Empty;
        ControlId = header.GetField(10);
        Version = header.GetField(12);
        SendingApplication = header.GetField(3);
        SendingFacility = header.GetField(4);
    }

    public string Raw { get; }

    public IReadOnlyList<Hl7Segment> Segments { get; }

    public string MessageCode { get; }

    public string TriggerEvent { get; }

    public string ControlId { get; }

    public string Version { get; }

    public string SendingApplication { get; }

    public string SendingFacility { get; }

    public Hl7Segment? FindSegment(string name) =>
        Segments.FirstOrDefault(segment => segment.Name.Equals(name, StringComparison.Ordinal));
}
