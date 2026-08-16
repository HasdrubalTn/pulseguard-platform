using AutoFixture;
using FluentAssertions;
using PulseGuard.PatientRegistry.Contracts;
using PulseGuard.PatientRegistry.Domain;
using PulseGuard.PatientRegistry.Infrastructure;

namespace PulseGuard.PatientRegistry.Infrastructure.UnitTests;

public sealed class PatientRegistryIntegrationEventMapperTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void MapConvertsPatientRegistrationWithoutClinicalIdentifiers()
    {
        PatientRegisteredDomainEvent domainEvent = new(
            _fixture.Create<Guid>(),
            _fixture.Create<DateTimeOffset>(),
            new PatientId(_fixture.Create<Guid>()),
            _fixture.Create<string>());
        PatientRegistryIntegrationEventMapper sut = new();

        PatientRegisteredIntegrationEvent integrationEvent = sut.Map(domainEvent)
            .Should()
            .BeOfType<PatientRegisteredIntegrationEvent>()
            .Subject;

        integrationEvent.EventId.Should().Be(domainEvent.EventId);
        integrationEvent.OccurredOnUtc.Should().Be(domainEvent.OccurredOnUtc);
        integrationEvent.SchemaVersion.Should().Be(1);
        integrationEvent.PatientId.Should().Be(domainEvent.PatientId.Value);
        integrationEvent.ToString().Should().NotContain(domainEvent.MedicalRecordNumber);
    }
}
