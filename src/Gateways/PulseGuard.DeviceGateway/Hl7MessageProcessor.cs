using System.Diagnostics;
using System.Diagnostics.Metrics;
using PulseGuard.Framework.Hl7;
using PulseGuard.Framework.Messaging;

namespace PulseGuard.DeviceGateway;

public sealed partial class Hl7MessageProcessor(
    Hl7AcknowledgementFactory acknowledgementFactory,
    IIntegrationEventPublisher publisher,
    TimeProvider timeProvider,
    ILogger<Hl7MessageProcessor> logger)
{
    private static readonly ActivitySource ActivitySource = new("PulseGuard.DeviceGateway");
    private static readonly Meter Meter = new("PulseGuard.DeviceGateway");
    private static readonly Counter<long> AcceptedMessages = Meter.CreateCounter<long>(
        "pulseguard.hl7.messages.accepted",
        description: "Number of accepted HL7 v2 messages.");

    public async ValueTask<string> ProcessAsync(string payload, CancellationToken cancellationToken)
    {
        using Activity? activity = ActivitySource.StartActivity("hl7.process", ActivityKind.Consumer);
        Hl7Message message = Hl7MessageParser.Parse(payload);

        activity?.SetTag("messaging.system", "hl7v2");
        activity?.SetTag("messaging.operation.type", "process");
        activity?.SetTag("hl7.message.code", message.MessageCode);
        activity?.SetTag("hl7.trigger.event", message.TriggerEvent);

        // Raw clinical payloads are intentionally excluded from logs to avoid leaking sensitive data.
        LogMessageReceived(
            logger,
            message.MessageCode,
            message.TriggerEvent,
            message.ControlId,
            message.SendingApplication);

        Hl7MessageReceivedIntegrationEvent integrationEvent = new(
            Guid.NewGuid(),
            timeProvider.GetUtcNow(),
            1,
            message.MessageCode,
            message.TriggerEvent,
            message.ControlId,
            message.SendingApplication,
            message.SendingFacility);

        await publisher.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
        AcceptedMessages.Add(
            1,
            new KeyValuePair<string, object?>("hl7.message.code", message.MessageCode),
            new KeyValuePair<string, object?>("hl7.trigger.event", message.TriggerEvent));
        return acknowledgementFactory.CreateApplicationAccept(message);
    }

    [LoggerMessage(
        EventId = 1200,
        Level = LogLevel.Information,
        Message = "Received HL7 {MessageCode}^{TriggerEvent} with control ID {ControlId} from {SendingApplication}")]
    private static partial void LogMessageReceived(
        ILogger logger,
        string messageCode,
        string triggerEvent,
        string controlId,
        string sendingApplication);
}
