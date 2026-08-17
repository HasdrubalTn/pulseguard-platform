using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PulseGuard.PatientRegistry.Domain;

namespace PulseGuard.PatientRegistry.Infrastructure.Migrations;

[DbContext(typeof(PatientRegistryDbContext))]
public sealed partial class PatientRegistryDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.11")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        modelBuilder.Entity("PulseGuard.Framework.Messaging.EntityFrameworkCore.InboxMessage", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("id");

            entity.Property<string>("ContentHash")
                .IsRequired()
                .IsFixedLength()
                .HasMaxLength(64)
                .HasColumnType("character(64)")
                .HasColumnName("content_hash");

            entity.Property<string>("EventType")
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("character varying(256)")
                .HasColumnName("event_type");

            entity.Property<DateTimeOffset?>("ProcessedOnUtc")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("processed_on_utc");

            entity.Property<DateTimeOffset>("ReceivedOnUtc")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("received_on_utc");

            entity.HasKey("Id")
                .HasName("pk_inbox_messages");

            entity.HasIndex("ProcessedOnUtc", "ReceivedOnUtc")
                .HasDatabaseName("ix_inbox_messages_pending");

            entity.ToTable("inbox_messages", "patient_registry");
        });

        modelBuilder.Entity("PulseGuard.Framework.Messaging.EntityFrameworkCore.OutboxMessage", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("id");

            entity.Property<string>("EventType")
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("character varying(256)")
                .HasColumnName("event_type");

            entity.Property<DateTimeOffset>("OccurredOnUtc")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("occurred_on_utc");

            entity.Property<string>("Payload")
                .IsRequired()
                .HasColumnType("jsonb")
                .HasColumnName("payload");

            entity.Property<DateTimeOffset?>("PublishedOnUtc")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("published_on_utc");

            entity.Property<int>("SchemaVersion")
                .HasColumnType("integer")
                .HasColumnName("schema_version");

            entity.HasKey("Id")
                .HasName("pk_outbox_messages");

            entity.HasIndex("PublishedOnUtc", "OccurredOnUtc")
                .HasDatabaseName("ix_outbox_messages_pending");

            entity.ToTable("outbox_messages", "patient_registry");
        });

        modelBuilder.Entity("PulseGuard.PatientRegistry.Domain.Patient", entity =>
        {
            entity.Property<PatientId>("Id")
                .HasConversion(new ValueConverter<PatientId, Guid>(
                    patientId => patientId.Value,
                    value => new PatientId(value)))
                .HasColumnType("uuid")
                .HasColumnName("id");

            entity.Property<DateOnly>("BirthDate")
                .HasColumnType("date")
                .HasColumnName("birth_date");

            entity.Property<string>("FamilyName")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)")
                .HasColumnName("family_name");

            entity.Property<string>("GivenName")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)")
                .HasColumnName("given_name");

            entity.Property<string>("MedicalRecordNumber")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("character varying(64)")
                .HasColumnName("medical_record_number");

            entity.Property<DateTimeOffset>("RegisteredOnUtc")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("registered_on_utc");

            entity.Property<long>("Version")
                .IsConcurrencyToken()
                .HasColumnType("bigint")
                .HasColumnName("version");

            entity.HasKey("Id")
                .HasName("pk_patients");

            entity.HasIndex("MedicalRecordNumber")
                .IsUnique()
                .HasDatabaseName("ux_patients_medical_record_number");

            entity.ToTable("patients", "patient_registry");
        });
    }
}
