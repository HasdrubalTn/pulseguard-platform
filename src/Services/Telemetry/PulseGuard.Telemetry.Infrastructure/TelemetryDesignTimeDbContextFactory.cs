using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PulseGuard.Telemetry.Infrastructure;

public sealed class TelemetryDesignTimeDbContextFactory : IDesignTimeDbContextFactory<TelemetryDbContext>
{
    public TelemetryDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Telemetry")
            ?? "Host=localhost;Port=5432;Database=pulseguard;Username=pulseguard";

        DbContextOptions<TelemetryDbContext> options = new DbContextOptionsBuilder<TelemetryDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "telemetry"))
            .Options;

        return new TelemetryDbContext(options);
    }
}
