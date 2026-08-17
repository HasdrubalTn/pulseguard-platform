using Microsoft.EntityFrameworkCore;
using PulseGuard.Framework.Messaging.EntityFrameworkCore;
using PulseGuard.PatientRegistry.Domain;

namespace PulseGuard.PatientRegistry.Infrastructure;

public sealed class PatientRegistryDbContext(DbContextOptions<PatientRegistryDbContext> options)
    : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PatientRegistryDbContext).Assembly);
        modelBuilder.AddTransactionalMessaging("patient_registry", "jsonb");
    }
}
