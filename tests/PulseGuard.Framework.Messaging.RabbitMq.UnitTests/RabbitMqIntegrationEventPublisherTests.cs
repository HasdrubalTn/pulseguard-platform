using System.Text.Json;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using PulseGuard.Framework.Messaging;
using PulseGuard.Framework.Messaging.RabbitMq;

namespace PulseGuard.Framework.Messaging.RabbitMq.UnitTests;

public sealed class RabbitMqIntegrationEventPublisherTests
{
    private readonly Fixture _fixture = new();

    public RabbitMqIntegrationEventPublisherTests()
    {
        _fixture.Customize(new AutoNSubstituteCustomization());
    }

    [Fact]
    public async Task PublishAsyncMapsEventMetadataAndConfiguredRoute()
    {
        TestIntegrationEvent integrationEvent = _fixture.Create<TestIntegrationEvent>();
        IRabbitMqMessagePublisher transport = Substitute.For<IRabbitMqMessagePublisher>();
        RabbitMqOptions options = CreateOptions(integrationEvent.GetType().Name, "pulseguard.test.event.v1");
        RabbitMqIntegrationEventPublisher sut = new(transport, Options.Create(options));
        CancellationToken cancellationToken = CancellationToken.None;

        await sut.PublishAsync(integrationEvent, cancellationToken);

        RabbitMqMessage message = transport
            .ReceivedCalls()
            .Should()
            .ContainSingle()
            .Which
            .GetArguments()
            .OfType<RabbitMqMessage>()
            .Should()
            .ContainSingle()
            .Which;

        message.MessageId.Should().Be(integrationEvent.EventId);
        message.OccurredOnUtc.Should().Be(integrationEvent.OccurredOnUtc);
        message.SchemaVersion.Should().Be(integrationEvent.SchemaVersion);
        message.EventType.Should().Be(nameof(TestIntegrationEvent));
        message.RoutingKey.Should().Be("pulseguard.test.event.v1");

        using JsonDocument document = JsonDocument.Parse(message.Body);
        document.RootElement.GetProperty("deviceId").GetString().Should().Be(integrationEvent.DeviceId);
    }

    [Fact]
    public async Task PublishAsyncRejectsAnEventWithoutConfiguredRoute()
    {
        TestIntegrationEvent integrationEvent = _fixture.Create<TestIntegrationEvent>();
        IRabbitMqMessagePublisher transport = Substitute.For<IRabbitMqMessagePublisher>();
        RabbitMqIntegrationEventPublisher sut = new(
            transport,
            Options.Create(new RabbitMqOptions()));

        Func<Task> act = async () =>
            await sut.PublishAsync(integrationEvent, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{nameof(TestIntegrationEvent)}*");
        transport.ReceivedCalls().Should().BeEmpty();
    }

    private static RabbitMqOptions CreateOptions(string eventType, string routingKey) =>
        new()
        {
            Routes = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [eventType] = routingKey,
            },
        };

    private sealed record TestIntegrationEvent(
        Guid EventId,
        DateTimeOffset OccurredOnUtc,
        int SchemaVersion,
        string DeviceId) : IntegrationEvent(EventId, OccurredOnUtc, SchemaVersion);
}
