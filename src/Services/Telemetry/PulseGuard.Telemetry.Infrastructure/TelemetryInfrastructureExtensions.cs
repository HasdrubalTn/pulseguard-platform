using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PulseGuard.Framework.Messaging.EntityFrameworkCore;
using PulseGuard.Telemetry.Application;

namespace PulseGuard.Telemetry.Infrastructure;

public static class TelemetryInfrastructureExtensions
{
    public static IServiceCollection AddTelemetryInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string connectionString = configuration.GetConnectionString("Telemetry")
            ?? throw new InvalidOperationException(
                "The Telemetry connection string is missing. Run eng/initialize-development.ps1.");

        services.AddDbContextFactory<TelemetryDbContext>(options => options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "telemetry")));
        services.AddTransactionalInboxProcessor<TelemetryDbContext>();
        services.AddSingleton<ITelemetryMeasurementProcessor, PostgresTelemetryMeasurementProcessor>();
        return services;
    }

    public static async Task ApplyTelemetryMigrationsAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        IDbContextFactory<TelemetryDbContext> factory = services
            .GetRequiredService<IDbContextFactory<TelemetryDbContext>>();
        await using TelemetryDbContext dbContext = await factory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
