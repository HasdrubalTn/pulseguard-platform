using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Options;
using PulseGuard.Framework.Messaging;
using PulseGuard.Framework.Messaging.EntityFrameworkCore;

namespace PulseGuard.Framework.Messaging.EntityFrameworkCore.UnitTests;

public sealed class JsonIntegrationEventSerializerTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void DeserializeRehydratesARegisteredConcreteEvent()
    {
        TransactionalMessagingOptions options = new();
        options.RegisterEvent<TestIntegrationEvent>();
        JsonIntegrationEventSerializer sut = new(Options.Create(options));
        TestIntegrationEvent expected = _fixture.Create<TestIntegrationEvent>();

        string payload = sut.Serialize(expected);
        IntegrationEvent actual = sut.Deserialize(nameof(TestIntegrationEvent), payload);

        actual.Should().Be(expected);
    }

    [Fact]
    public void DeserializeRejectsAnUnregisteredEventType()
    {
        JsonIntegrationEventSerializer sut = new(Options.Create(new TransactionalMessagingOptions()));

        Action act = () => sut.Deserialize("UnknownEvent", "{}");

        act.Should().Throw<InvalidOperationException>().WithMessage("*UnknownEvent*");
    }

    private sealed record TestIntegrationEvent(
        Guid EventId,
        DateTimeOffset OccurredOnUtc,
        int SchemaVersion,
        string Source) : IntegrationEvent(EventId, OccurredOnUtc, SchemaVersion);
}
