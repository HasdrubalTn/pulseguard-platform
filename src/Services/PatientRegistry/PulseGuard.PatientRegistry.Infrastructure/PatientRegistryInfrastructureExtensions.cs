using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PulseGuard.Framework.Messaging.EntityFrameworkCore;
using PulseGuard.PatientRegistry.Application;
using PulseGuard.PatientRegistry.Contracts;

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

        services.AddSingleton<IDomainEventMapper, PatientRegistryIntegrationEventMapper>();
        services.AddScoped<TransactionalOutboxSaveChangesInterceptor>();
        services.AddDbContext<PatientRegistryDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "patient_registry"));
            options.AddInterceptors(serviceProvider.GetRequiredService<TransactionalOutboxSaveChangesInterceptor>());
        });
        services.AddScoped<IPatientRepository, PostgresPatientRepository>();
        services.AddTransactionalOutboxDispatcher<PatientRegistryDbContext>(options =>
        {
            options.BatchSize = 50;
            options.PollingInterval = TimeSpan.FromSeconds(1);
            options.RegisterEvent<PatientRegisteredIntegrationEvent>();
        });

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
