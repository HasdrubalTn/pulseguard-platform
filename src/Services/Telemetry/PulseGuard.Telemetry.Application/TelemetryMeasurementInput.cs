using PulseGuard.Telemetry.Domain;

namespace PulseGuard.Telemetry.Application;

public sealed record TelemetryMeasurementInput(
    Guid MeasurementId,
    Guid PatientId,
    string DeviceId,
    VitalMeasurementType Type,
    double Value,
    string Unit,
    DateTimeOffset MeasuredAtUtc);
