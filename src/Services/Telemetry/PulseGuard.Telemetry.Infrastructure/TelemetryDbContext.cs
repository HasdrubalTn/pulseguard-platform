using Microsoft.EntityFrameworkCore;
using PulseGuard.Framework.Messaging.EntityFrameworkCore;
using PulseGuard.Telemetry.Domain;

namespace PulseGuard.Telemetry.Infrastructure;

public sealed class TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : DbContext(options)
{
    public DbSet<TelemetryMeasurement> Measurements => Set<TelemetryMeasurement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TelemetryDbContext).Assembly);
        modelBuilder.AddTransactionalInbox("telemetry");
    }
}
