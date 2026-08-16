using PulseGuard.Framework.Hl7;
using PulseGuard.Framework.Messaging;

namespace PulseGuard.DeviceGateway;

public sealed partial class Hl7MessageProcessor(
    Hl7MessageParser parser,
    Hl7AcknowledgementFactory acknowledgementFactory,
    IIntegrationEventPublisher publisher,
    TimeProvider timeProvider,
    ILogger<Hl7MessageProcessor> logger)
{
    public async ValueTask<string> ProcessAsync(string payload, CancellationToken cancellationToken)
    {
        Hl7Message message = parser.Parse(payload);

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
