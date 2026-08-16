using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace PulseGuard.Framework.Messaging.RabbitMq;

internal sealed class RabbitMqMessagePublisher : IRabbitMqMessagePublisher, IAsyncDisposable
{
    private const string InstrumentationName = "PulseGuard.Framework.Messaging.RabbitMq";
    private static readonly ActivitySource ActivitySource = new(InstrumentationName);
    private static readonly Meter Meter = new(InstrumentationName);
    private static readonly Counter<long> PublishedMessages = Meter.CreateCounter<long>(
        "pulseguard.messaging.rabbitmq.published",
        description: "Number of RabbitMQ messages confirmed by the broker.");
    private static readonly Counter<long> FailedMessages = Meter.CreateCounter<long>(
        "pulseguard.messaging.rabbitmq.failed",
        description: "Number of RabbitMQ publish attempts that failed.");

    private readonly IRabbitMqConnectionManager _connectionManager;
    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _publishGate = new(1, 1);
    private IChannel? _channel;

    public RabbitMqMessagePublisher(
        IRabbitMqConnectionManager connectionManager,
        IOptions<RabbitMqOptions> options)
    {
        ArgumentNullException.ThrowIfNull(connectionManager);
        ArgumentNullException.ThrowIfNull(options);

        _connectionManager = connectionManager;
        _options = options.Value;
    }

    public async ValueTask PublishAsync(RabbitMqMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.PublishTimeout);

        await _publishGate.WaitAsync(timeout.Token);
        try
        {
            using Activity? activity = ActivitySource.StartActivity("rabbitmq.publish", ActivityKind.Producer);
            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination.name", _options.ExchangeName);
            activity?.SetTag("messaging.rabbitmq.destination.routing_key", message.RoutingKey);
            activity?.SetTag("messaging.message.type", message.EventType);

            IChannel channel = await GetOrCreateChannelAsync(timeout.Token);
            BasicProperties properties = new()
            {
                ContentEncoding = "utf-8",
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                Headers = new Dictionary<string, object?>
                {
                    ["schema-version"] = message.SchemaVersion,
                },
                MessageId = message.MessageId.ToString("D"),
                Timestamp = new AmqpTimestamp(message.OccurredOnUtc.ToUnixTimeSeconds()),
                Type = message.EventType,
            };

            await channel.BasicPublishAsync(
                exchange: _options.ExchangeName,
                routingKey: message.RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: message.Body,
                cancellationToken: timeout.Token);

            PublishedMessages.Add(1, new KeyValuePair<string, object?>("event.type", message.EventType));
        }
        catch
        {
            FailedMessages.Add(1, new KeyValuePair<string, object?>("event.type", message.EventType));
            await ResetChannelAsync();
            throw;
        }
        finally
        {
            _publishGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _publishGate.WaitAsync();
        try
        {
            await ResetChannelAsync();
        }
        finally
        {
            _publishGate.Release();
            _publishGate.Dispose();
        }
    }

    private async ValueTask<IChannel> GetOrCreateChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await ResetChannelAsync();

        IConnection connection = await _connectionManager.GetConnectionAsync(cancellationToken);
        CreateChannelOptions channelOptions = new(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
        IChannel channel = await connection.CreateChannelAsync(channelOptions, cancellationToken);

        try
        {
            await channel.ExchangeDeclareAsync(
                exchange: _options.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);
            _channel = channel;
            return channel;
        }
        catch
        {
            await channel.DisposeAsync();
            throw;
        }
    }

    private async ValueTask ResetChannelAsync()
    {
        IChannel? currentChannel = _channel;
        _channel = null;

        if (currentChannel is not null)
        {
            await currentChannel.DisposeAsync();
        }
    }
}
