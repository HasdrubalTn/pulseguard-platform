namespace PulseGuard.Framework.Hl7;

public sealed class Hl7MessageParser
{
    private const int MinimumHeaderLength = 4;

    public Hl7Message Parse(string rawMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawMessage);

        string normalized = rawMessage.Replace("\r\n", "\r", StringComparison.Ordinal).Replace('\n', '\r');
        string[] rawSegments = normalized.Split('\r', StringSplitOptions.RemoveEmptyEntries);

        if (rawSegments.Length == 0 || rawSegments[0].Length < MinimumHeaderLength ||
            !rawSegments[0].StartsWith("MSH", StringComparison.Ordinal))
        {
            throw new FormatException("An HL7 v2 message must start with an MSH segment.");
        }

        char fieldSeparator = rawSegments[0][3];
        List<Hl7Segment> segments = new(rawSegments.Length);

        foreach (string rawSegment in rawSegments)
        {
            string[] parts = rawSegment.Split(fieldSeparator);
            string segmentName = parts[0];

            if (segmentName.Length != 3 || !segmentName.All(char.IsAsciiLetterOrDigit))
            {
                throw new FormatException($"Invalid HL7 segment name '{segmentName}'.");
            }

            segments.Add(new Hl7Segment(segmentName, fieldSeparator, parts.Skip(1).ToArray()));
        }

        Hl7Message message = new(normalized, segments);
        ValidateHeader(message);
        return message;
    }

    private static void ValidateHeader(Hl7Message message)
    {
        if (string.IsNullOrWhiteSpace(message.MessageCode) || string.IsNullOrWhiteSpace(message.ControlId))
        {
            throw new FormatException("MSH-9 message type and MSH-10 control ID are required.");
        }

        if (!message.Version.StartsWith("2.", StringComparison.Ordinal))
        {
            throw new NotSupportedException($"HL7 version '{message.Version}' is not supported; expected HL7 v2.x.");
        }
    }
}
