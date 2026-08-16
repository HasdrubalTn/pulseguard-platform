using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PulseGuard.Framework.Messaging;

namespace PulseGuard.Framework.Messaging.RabbitMq;

public static class RabbitMqServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqIntegrationEventPublisher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection section = configuration.GetRequiredSection(RabbitMqOptions.SectionName);
        services
            .AddOptions<RabbitMqOptions>()
            .Bind(section)
            .Validate(options => !string.IsNullOrWhiteSpace(options.HostName), "The RabbitMQ host name is required.")
            .Validate(options => options.Port is > 0 and <= 65535, "The RabbitMQ port must be between 1 and 65535.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.VirtualHost), "The RabbitMQ virtual host is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.UserName), "The RabbitMQ user name is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Password), "The RabbitMQ password is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ExchangeName), "The RabbitMQ exchange name is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ClientProvidedName), "The RabbitMQ client name is required.")
            .Validate(options => IsValidTimeout(options.RequestedHeartbeat), "The RabbitMQ heartbeat is outside the safe range.")
            .Validate(options => IsValidTimeout(options.ConnectionTimeout), "The RabbitMQ connection timeout is outside the safe range.")
            .Validate(options => IsValidTimeout(options.PublishTimeout), "The RabbitMQ publish timeout is outside the safe range.")
            .Validate(
                options => options.Routes.Count > 0 && options.Routes.All(route =>
                    !string.IsNullOrWhiteSpace(route.Key) && !string.IsNullOrWhiteSpace(route.Value)),
                "At least one valid RabbitMQ event route is required.")
            .ValidateOnStart();

        services.TryAddSingleton<IRabbitMqConnectionManager, RabbitMqConnectionManager>();
        services.TryAddSingleton<IRabbitMqMessagePublisher, RabbitMqMessagePublisher>();
        services.TryAddSingleton<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();

        return services;
    }

    private static bool IsValidTimeout(TimeSpan value) =>
        value > TimeSpan.Zero && value <= TimeSpan.FromMinutes(5);
}
