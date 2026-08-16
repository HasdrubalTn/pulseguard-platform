namespace PulseGuard.PatientRegistry.Domain;

public readonly record struct PatientId(Guid Value)
{
    public static PatientId New() => new(Guid.NewGuid());

    public static PatientId Parse(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString("D");
}
