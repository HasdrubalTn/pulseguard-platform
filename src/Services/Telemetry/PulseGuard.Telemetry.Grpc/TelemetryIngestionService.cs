using Grpc.Core;
using PulseGuard.Telemetry.Contracts;

namespace PulseGuard.Telemetry.Grpc;

public sealed class TelemetryIngestionService(ILogger<TelemetryIngestionService> logger)
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
                logger.LogDebug(
                    "Accepted {VitalType} measurement {MeasurementId} from device {DeviceId}",
                    measurement.Type,
                    measurement.MeasurementId,
                    measurement.DeviceId);
            }
            else
            {
                rejected++;
                logger.LogWarning("Rejected malformed telemetry measurement {MeasurementId}", measurement.MeasurementId);
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
}
