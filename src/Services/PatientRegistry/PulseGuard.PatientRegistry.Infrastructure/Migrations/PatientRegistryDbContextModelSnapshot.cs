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
