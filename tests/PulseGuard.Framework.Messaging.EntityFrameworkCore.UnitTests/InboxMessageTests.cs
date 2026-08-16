using System.Text;
using AutoFixture;
using FluentAssertions;
using PulseGuard.Framework.Messaging.EntityFrameworkCore;

namespace PulseGuard.Framework.Messaging.EntityFrameworkCore.UnitTests;

public sealed class InboxMessageTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void ReceiveComputesAStablePayloadFingerprint()
    {
        Guid messageId = _fixture.Create<Guid>();
        DateTimeOffset receivedOnUtc = _fixture.Create<DateTimeOffset>();
        byte[] payload = Encoding.UTF8.GetBytes("{\"patientId\":\"synthetic\"}");

        InboxMessage first = InboxMessage.Receive(messageId, "PatientRegisteredIntegrationEvent", payload, receivedOnUtc);
        InboxMessage duplicate = InboxMessage.Receive(messageId, "PatientRegisteredIntegrationEvent", payload, receivedOnUtc);

        first.Id.Should().Be(messageId);
        first.ContentHash.Should().HaveLength(64).And.Be(duplicate.ContentHash);
        first.ProcessedOnUtc.Should().BeNull();
    }
}
