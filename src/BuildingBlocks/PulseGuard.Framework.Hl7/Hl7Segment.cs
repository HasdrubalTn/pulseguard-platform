namespace PulseGuard.Framework.Hl7;

public sealed class Hl7Segment
{
    private readonly IReadOnlyList<string> _fields;

    internal Hl7Segment(string name, char fieldSeparator, IReadOnlyList<string> fields)
    {
        Name = name;
        FieldSeparator = fieldSeparator;
        _fields = fields;
    }

    public string Name { get; }

    public char FieldSeparator { get; }

    public string GetField(int fieldNumber)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fieldNumber);

        if (Name.Equals("MSH", StringComparison.Ordinal))
        {
            if (fieldNumber == 1)
            {
                return FieldSeparator.ToString();
            }

            return GetValueOrEmpty(fieldNumber - 2);
        }

        return GetValueOrEmpty(fieldNumber - 1);
    }

    private string GetValueOrEmpty(int index) => index < _fields.Count ? _fields[index] : string.Empty;
}
