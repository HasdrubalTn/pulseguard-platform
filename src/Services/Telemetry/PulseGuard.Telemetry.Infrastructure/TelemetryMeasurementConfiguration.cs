using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseGuard.Telemetry.Domain;

namespace PulseGuard.Telemetry.Infrastructure;

internal sealed class TelemetryMeasurementConfiguration : IEntityTypeConfiguration<TelemetryMeasurement>
{
    public void Configure(EntityTypeBuilder<TelemetryMeasurement> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("measurements", "telemetry");
        builder.HasKey(measurement => measurement.Id).HasName("pk_measurements");

        builder.Property(measurement => measurement.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(measurement => measurement.PatientId)
            .HasColumnName("patient_id")
            .IsRequired();
        builder.Property(measurement => measurement.DeviceId)
            .HasColumnName("device_id")
            .HasMaxLength(128)
            .IsRequired();
        builder.Property(measurement => measurement.Type)
            .HasColumnName("vital_type")
            .IsRequired();
        builder.Property(measurement => measurement.Value)
            .HasColumnName("value")
            .IsRequired();
        builder.Property(measurement => measurement.Unit)
            .HasColumnName("unit")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(measurement => measurement.MeasuredAtUtc)
            .HasColumnName("measured_at_utc")
            .IsRequired();
        builder.Property(measurement => measurement.ReceivedOnUtc)
            .HasColumnName("received_on_utc")
            .IsRequired();

        builder.HasIndex(measurement => new { measurement.PatientId, measurement.MeasuredAtUtc })
            .HasDatabaseName("ix_measurements_patient_measured_at");
    }
}
