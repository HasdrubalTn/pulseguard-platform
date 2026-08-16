using Grpc.Core;
using PulseGuard.Telemetry.Contracts;

namespace PulseGuard.Telemetry.Grpc;

public sealed partial class TelemetryIngestionService(ILogger<TelemetryIngestionService> logger)
    : TelemetryIngestor.TelemetryIngestorBase
{
    public override async Task<IngestionSummary> Ingest(
        IAsyncStreamReader<VitalMeasurement> requestStream,
        ServerCallContext context)
    {
        int accepted = 0;
        int rejected = 0;

        await foreach (VitalMeasurement measurement in requestStream.ReadAllAsync(context.CancellationToken))
        {
            if (IsValid(measurement))
            {
                accepted++;
                LogMeasurementAccepted(
                    logger,
                    measurement.Type,
                    measurement.MeasurementId,
                    measurement.DeviceId);
            }
            else
            {
                rejected++;
                LogMeasurementRejected(logger, measurement.MeasurementId);
            }
        }

        return new IngestionSummary { Accepted = accepted, Rejected = rejected };
    }

    private static bool IsValid(VitalMeasurement measurement) =>
        Guid.TryParse(measurement.MeasurementId, out _) &&
        Guid.TryParse(measurement.PatientId, out _) &&
        !string.IsNullOrWhiteSpace(measurement.DeviceId) &&
        measurement.Type != default &&
        !double.IsNaN(measurement.Value) &&
        !double.IsInfinity(measurement.Value) &&
        measurement.MeasuredAtUtc is not null;

    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Debug,
        Message = "Accepted {VitalType} measurement {MeasurementId} from device {DeviceId}")]
    private static partial void LogMeasurementAccepted(
        ILogger logger,
        VitalType vitalType,
        string measurementId,
        string deviceId);

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Warning,
        Message = "Rejected malformed telemetry measurement {MeasurementId}")]
    private static partial void LogMeasurementRejected(ILogger logger, string measurementId);
}
