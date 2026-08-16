using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PulseGuard.PatientRegistry.Infrastructure;

public sealed class PatientRegistryDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PatientRegistryDbContext>
{
    public PatientRegistryDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PatientRegistry")
            ?? "Host=localhost;Port=5432;Database=pulseguard;Username=pulseguard";

        DbContextOptions<PatientRegistryDbContext> options = new DbContextOptionsBuilder<PatientRegistryDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "patient_registry"))
            .Options;

        return new PatientRegistryDbContext(options);
    }
}
