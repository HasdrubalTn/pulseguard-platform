using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using PulseGuard.Telemetry.Domain;

namespace PulseGuard.Telemetry.Infrastructure.Migrations;

[DbContext(typeof(TelemetryDbContext))]
public sealed partial class TelemetryDbContextModelSnapshot : ModelSnapshot
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

            entity.ToTable("inbox_messages", "telemetry");
        });

        modelBuilder.Entity("PulseGuard.Telemetry.Domain.TelemetryMeasurement", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid")
                .HasColumnName("id");

            entity.Property<string>("DeviceId")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("character varying(128)")
                .HasColumnName("device_id");

            entity.Property<DateTimeOffset>("MeasuredAtUtc")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("measured_at_utc");

            entity.Property<Guid>("PatientId")
                .HasColumnType("uuid")
                .HasColumnName("patient_id");

            entity.Property<DateTimeOffset>("ReceivedOnUtc")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("received_on_utc");

            entity.Property<VitalMeasurementType>("Type")
                .HasColumnType("integer")
                .HasColumnName("vital_type");

            entity.Property<string>("Unit")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("character varying(32)")
                .HasColumnName("unit");

            entity.Property<double>("Value")
                .HasColumnType("double precision")
                .HasColumnName("value");

            entity.HasKey("Id")
                .HasName("pk_measurements");

            entity.HasIndex("PatientId", "MeasuredAtUtc")
                .HasDatabaseName("ix_measurements_patient_measured_at");

            entity.ToTable("measurements", "telemetry");
        });
    }
}
