using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PulseGuard.Framework.Messaging.EntityFrameworkCore;

public static class TransactionalMessagingServiceCollectionExtensions
{
    public static IServiceCollection AddTransactionalOutboxDispatcher<TDbContext>(
        this IServiceCollection services,
        Action<TransactionalMessagingOptions> configure)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services
            .AddOptions<TransactionalMessagingOptions>()
            .Configure(configure)
            .Validate(options => options.BatchSize is > 0 and <= 1000, "Outbox batch size must be between 1 and 1000.")
            .Validate(
                options => options.PollingInterval >= TimeSpan.FromMilliseconds(100) &&
                           options.PollingInterval <= TimeSpan.FromMinutes(1),
                "Outbox polling interval must be between 100 milliseconds and one minute.")
            .Validate(options => options.EventTypes.Count > 0, "At least one integration event type must be registered.")
            .ValidateOnStart();

        services.TryAddSingleton<IIntegrationEventSerializer, JsonIntegrationEventSerializer>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddHostedService<OutboxDispatcher<TDbContext>>();
        return services;
    }
}
