using System.Diagnostics.Metrics;
using Grpc.Core;
using PulseGuard.Telemetry.Contracts;

namespace PulseGuard.Telemetry.Grpc;

public sealed partial class TelemetryIngestionService(ILogger<TelemetryIngestionService> logger)
    : TelemetryIngestor.TelemetryIngestorBase
{
    private static readonly Meter Meter = new("PulseGuard.Telemetry.Grpc");
    private static readonly Counter<long> AcceptedMeasurements = Meter.CreateCounter<long>(
        "pulseguard.telemetry.measurements.accepted",
        description: "Number of accepted synthetic telemetry measurements.");
    private static readonly Counter<long> RejectedMeasurements = Meter.CreateCounter<long>(
        "pulseguard.telemetry.measurements.rejected",
        description: "Number of rejected synthetic telemetry measurements.");

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
                AcceptedMeasurements.Add(
                    1,
                    new KeyValuePair<string, object?>("vital.type", measurement.Type.ToString()));
                LogMeasurementAccepted(
                    logger,
                    measurement.Type,
                    measurement.MeasurementId,
                    measurement.DeviceId);
            }
            else
            {
                rejected++;
                RejectedMeasurements.Add(1);
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
