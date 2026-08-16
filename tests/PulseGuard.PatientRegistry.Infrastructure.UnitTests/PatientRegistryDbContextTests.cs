using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using PulseGuard.Framework.Messaging.EntityFrameworkCore;
using PulseGuard.PatientRegistry.Domain;
using PulseGuard.PatientRegistry.Infrastructure;

namespace PulseGuard.PatientRegistry.Infrastructure.UnitTests;

public sealed class PatientRegistryDbContextTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void ModelMapsPatientAggregateToDedicatedSchema()
    {
        using PatientRegistryDbContext sut = CreateContext();

        IEntityType patient = sut.Model.FindEntityType(typeof(Patient))!;

        patient.GetSchema().Should().Be("patient_registry");
        patient.GetTableName().Should().Be("patients");
        patient.FindProperty(nameof(Patient.Id))!.GetColumnName().Should().Be("id");
        patient.FindProperty(nameof(Patient.BirthDate))!.GetColumnType().Should().Be("date");
    }

    [Fact]
    public void ModelEnforcesUniqueMedicalRecordNumber()
    {
        using PatientRegistryDbContext sut = CreateContext();

        IEntityType patient = sut.Model.FindEntityType(typeof(Patient))!;
        IIndex medicalRecordNumberIndex = patient.GetIndexes().Single(index =>
            index.Properties.Single().Name == nameof(Patient.MedicalRecordNumber));

        medicalRecordNumberIndex.IsUnique.Should().BeTrue();
        medicalRecordNumberIndex.GetDatabaseName().Should().Be("ux_patients_medical_record_number");
    }

    [Fact]
    public void ModelKeepsTransactionalMessagesInsidePatientRegistrySchema()
    {
        using PatientRegistryDbContext sut = CreateContext();

        IEntityType outbox = sut.Model.FindEntityType(typeof(OutboxMessage))!;
        IEntityType inbox = sut.Model.FindEntityType(typeof(InboxMessage))!;

        outbox.GetSchema().Should().Be("patient_registry");
        outbox.GetTableName().Should().Be("outbox_messages");
        outbox.FindProperty(nameof(OutboxMessage.Payload))!.GetColumnType().Should().Be("jsonb");
        inbox.GetSchema().Should().Be("patient_registry");
        inbox.GetTableName().Should().Be("inbox_messages");
    }

    [Fact]
    public void ContextDiscoversInitialMigration()
    {
        using PatientRegistryDbContext sut = CreateContext();

        sut.Database.GetMigrations().Should().Equal(
            "20260816190000_InitialPatientRegistry",
            "20260816213000_AddTransactionalOutboxAndInbox");
    }

    private PatientRegistryDbContext CreateContext()
    {
        string databaseName = _fixture.Create<Guid>().ToString("N");
        DbContextOptions<PatientRegistryDbContext> options = new DbContextOptionsBuilder<PatientRegistryDbContext>()
            .UseNpgsql($"Host=localhost;Database={databaseName};Username=pulseguard")
            .Options;

        return new PatientRegistryDbContext(options);
    }
}
