using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Grpc.Core;
using PulseGuard.Framework.Messaging.EntityFrameworkCore;
using PulseGuard.Telemetry.Application;
using PulseGuard.Telemetry.Contracts;
using PulseGuard.Telemetry.Domain;

namespace PulseGuard.Telemetry.Grpc;

public sealed partial class TelemetryIngestionService(
    ITelemetryMeasurementProcessor processor,
    ILogger<TelemetryIngestionService> logger)
    : TelemetryIngestor.TelemetryIngestorBase
{
    private static readonly Meter Meter = new("PulseGuard.Telemetry.Grpc");
    private static readonly Counter<long> AcceptedMeasurements = Meter.CreateCounter<long>(
        "pulseguard.telemetry.measurements.accepted",
        description: "Number of accepted synthetic telemetry measurements.");
    private static readonly Counter<long> RejectedMeasurements = Meter.CreateCounter<long>(
        "pulseguard.telemetry.measurements.rejected",
        description: "Number of rejected synthetic telemetry measurements.");
    private static readonly Counter<long> DuplicateMeasurements = Meter.CreateCounter<long>(
        "pulseguard.telemetry.measurements.duplicates",
        description: "Number of duplicate synthetic telemetry measurements suppressed by the Inbox.");

    public override async Task<IngestionSummary> Ingest(
        IAsyncStreamReader<VitalMeasurement> requestStream,
        ServerCallContext context)
    {
        int accepted = 0;
        int rejected = 0;
        int duplicates = 0;

        await foreach (VitalMeasurement measurement in requestStream.ReadAllAsync(context.CancellationToken))
        {
            if (!TryMap(measurement, out TelemetryMeasurementInput? input))
            {
                rejected++;
                RejectedMeasurements.Add(1);
                LogMeasurementRejected(logger, measurement.MeasurementId);
                continue;
            }

            try
            {
                TelemetryIngestionOutcome outcome = await processor
                    .ProcessAsync(input, context.CancellationToken)
                    .ConfigureAwait(false);
                if (outcome == TelemetryIngestionOutcome.Duplicate)
                {
                    duplicates++;
                    DuplicateMeasurements.Add(
                        1,
                        new KeyValuePair<string, object?>("vital.type", measurement.Type.ToString()));
                    LogMeasurementDuplicate(
                        logger,
                        measurement.Type,
                        measurement.MeasurementId,
                        measurement.DeviceId);
                    continue;
                }

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
            catch (InboxMessageConflictException)
            {
                rejected++;
                RejectedMeasurements.Add(1);
                LogMeasurementConflict(logger, measurement.MeasurementId);
            }
        }

        return new IngestionSummary { Accepted = accepted, Rejected = rejected, Duplicates = duplicates };
    }

    private static bool TryMap(
        VitalMeasurement measurement,
        [NotNullWhen(true)] out TelemetryMeasurementInput? input)
    {
        input = null;
        if (!Guid.TryParse(measurement.MeasurementId, out Guid measurementId) ||
            measurementId == Guid.Empty ||
            !Guid.TryParse(measurement.PatientId, out Guid patientId) ||
            patientId == Guid.Empty ||
            string.IsNullOrWhiteSpace(measurement.DeviceId) ||
            measurement.DeviceId.Trim().Length > 128 ||
            string.IsNullOrWhiteSpace(measurement.Unit) ||
            measurement.Unit.Trim().Length > 32 ||
            measurement.Type == default ||
            !double.IsFinite(measurement.Value) ||
            measurement.MeasuredAtUtc is null)
        {
            return false;
        }

        VitalMeasurementType type = (VitalMeasurementType)(int)measurement.Type;
        if (type == VitalMeasurementType.Unspecified || !Enum.IsDefined(type))
        {
            return false;
        }

        DateTimeOffset measuredAtUtc;
        try
        {
            measuredAtUtc = measurement.MeasuredAtUtc.ToDateTimeOffset();
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        input = new TelemetryMeasurementInput(
            measurementId,
            patientId,
            measurement.DeviceId,
            type,
            measurement.Value,
            measurement.Unit,
            measuredAtUtc);
        return true;
    }

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

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Information,
        Message = "Suppressed duplicate {VitalType} measurement {MeasurementId} from device {DeviceId}")]
    private static partial void LogMeasurementDuplicate(
        ILogger logger,
        VitalType vitalType,
        string measurementId,
        string deviceId);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Warning,
        Message = "Rejected telemetry measurement {MeasurementId} because its ID was reused with different content")]
    private static partial void LogMeasurementConflict(ILogger logger, string measurementId);
}
