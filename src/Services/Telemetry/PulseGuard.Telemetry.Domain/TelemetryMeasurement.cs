namespace PulseGuard.Telemetry.Domain;

public sealed class TelemetryMeasurement
{
    private TelemetryMeasurement()
    {
    }

    private TelemetryMeasurement(
        Guid id,
        Guid patientId,
        string deviceId,
        VitalMeasurementType type,
        double value,
        string unit,
        DateTimeOffset measuredAtUtc,
        DateTimeOffset receivedOnUtc)
    {
        Id = id;
        PatientId = patientId;
        DeviceId = deviceId;
        Type = type;
        Value = value;
        Unit = unit;
        MeasuredAtUtc = measuredAtUtc;
        ReceivedOnUtc = receivedOnUtc;
    }

    public Guid Id { get; private set; }

    public Guid PatientId { get; private set; }

    public string DeviceId { get; private set; } = string.Empty;

    public VitalMeasurementType Type { get; private set; }

    public double Value { get; private set; }

    public string Unit { get; private set; } = string.Empty;

    public DateTimeOffset MeasuredAtUtc { get; private set; }

    public DateTimeOffset ReceivedOnUtc { get; private set; }

    public static TelemetryMeasurement Record(
        Guid id,
        Guid patientId,
        string deviceId,
        VitalMeasurementType type,
        double value,
        string unit,
        DateTimeOffset measuredAtUtc,
        DateTimeOffset receivedOnUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Measurement ID cannot be empty.", nameof(id));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Patient ID cannot be empty.", nameof(patientId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(unit);
        if (deviceId.Trim().Length > 128)
        {
            throw new ArgumentException("Device ID cannot exceed 128 characters.", nameof(deviceId));
        }

        if (unit.Trim().Length > 32)
        {
            throw new ArgumentException("Measurement unit cannot exceed 32 characters.", nameof(unit));
        }

        if (type == VitalMeasurementType.Unspecified || !Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Vital measurement type is not supported.");
        }

        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Measurement value must be finite.");
        }

        return new TelemetryMeasurement(
            id,
            patientId,
            deviceId.Trim(),
            type,
            value,
            unit.Trim(),
            measuredAtUtc.ToUniversalTime(),
            receivedOnUtc.ToUniversalTime());
    }
}
