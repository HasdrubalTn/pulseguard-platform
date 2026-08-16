using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseGuard.PatientRegistry.Domain;

namespace PulseGuard.PatientRegistry.Infrastructure;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("patients", "patient_registry");
        builder.HasKey(patient => patient.Id)
            .HasName("pk_patients");

        builder.Property(patient => patient.Id)
            .HasColumnName("id")
            .HasConversion(
                patientId => patientId.Value,
                value => new PatientId(value))
            .ValueGeneratedNever();

        builder.Property(patient => patient.MedicalRecordNumber)
            .HasColumnName("medical_record_number")
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(patient => patient.MedicalRecordNumber)
            .IsUnique()
            .HasDatabaseName("ux_patients_medical_record_number");

        builder.Property(patient => patient.GivenName)
            .HasColumnName("given_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(patient => patient.FamilyName)
            .HasColumnName("family_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(patient => patient.BirthDate)
            .HasColumnName("birth_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(patient => patient.RegisteredOnUtc)
            .HasColumnName("registered_on_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(patient => patient.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .IsRequired();

        builder.Ignore(patient => patient.DomainEvents);
    }
}
