using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PulseGuard.PatientRegistry.Application;

namespace PulseGuard.PatientRegistry.Infrastructure;

public static class PatientRegistryInfrastructureExtensions
{
    public static IServiceCollection AddPatientRegistryInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string connectionString = configuration.GetConnectionString("PatientRegistry")
            ?? throw new InvalidOperationException(
                "The PatientRegistry connection string is missing. Run eng/initialize-development.ps1.");

        services.AddDbContext<PatientRegistryDbContext>(options => options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "patient_registry")));
        services.AddScoped<IPatientRepository, PostgresPatientRepository>();

        return services;
    }

    public static async Task ApplyPatientRegistryMigrationsAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using AsyncServiceScope scope = services.CreateAsyncScope();
        PatientRegistryDbContext dbContext = scope.ServiceProvider.GetRequiredService<PatientRegistryDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
