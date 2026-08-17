namespace PulseGuard.Telemetry.Application;

public interface ITelemetryMeasurementProcessor
{
    Task<TelemetryIngestionOutcome> ProcessAsync(
        TelemetryMeasurementInput measurement,
        CancellationToken cancellationToken);
}
