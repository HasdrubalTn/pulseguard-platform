using System.Text.Json;
using PulseGuard.Framework.Messaging.EntityFrameworkCore;
using PulseGuard.Telemetry.Application;
using PulseGuard.Telemetry.Domain;

namespace PulseGuard.Telemetry.Infrastructure;

public sealed class PostgresTelemetryMeasurementProcessor(
    TransactionalInboxProcessor<TelemetryDbContext> inboxProcessor,
    TimeProvider timeProvider) : ITelemetryMeasurementProcessor
{
    private const string InboxEventType = "VitalMeasurement.v1";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<TelemetryIngestionOutcome> ProcessAsync(
        TelemetryMeasurementInput measurement,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(measurement);

        TelemetryMeasurement entity = TelemetryMeasurement.Record(
            measurement.MeasurementId,
            measurement.PatientId,
            measurement.DeviceId,
            measurement.Type,
            measurement.Value,
            measurement.Unit,
            measurement.MeasuredAtUtc,
            timeProvider.GetUtcNow());
        byte[] content = JsonSerializer.SerializeToUtf8Bytes(measurement, SerializerOptions);
        InboxProcessingResult result = await inboxProcessor.ProcessAsync(
            measurement.MeasurementId,
            InboxEventType,
            content,
            (dbContext, _) =>
            {
                dbContext.Measurements.Add(entity);
                return Task.CompletedTask;
            },
            cancellationToken).ConfigureAwait(false);

        return result == InboxProcessingResult.Processed
            ? TelemetryIngestionOutcome.Accepted
            : TelemetryIngestionOutcome.Duplicate;
    }
}
