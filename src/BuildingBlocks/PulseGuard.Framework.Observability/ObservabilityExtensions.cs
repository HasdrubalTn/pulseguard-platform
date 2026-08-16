using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace PulseGuard.Framework.Observability;

public static class ObservabilityExtensions
{
    public static IHostApplicationBuilder AddPulseGuardObservability(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        IConfigurationSection section = builder.Configuration.GetSection(ObservabilityOptions.SectionName);
        ObservabilityOptions options = section.Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        builder.Services
            .AddOptions<ObservabilityOptions>()
            .Bind(section)
            .Validate(
                candidate => candidate.OtlpEndpoint.IsAbsoluteUri,
                "The OpenTelemetry OTLP endpoint must be an absolute URI.")
            .Validate(
                candidate => !string.IsNullOrWhiteSpace(candidate.ServiceNamespace),
                "The OpenTelemetry service namespace is required.")
            .ValidateOnStart();

        ResourceBuilder resource = ResourceBuilder
            .CreateDefault()
            .AddService(
                builder.Environment.ApplicationName,
                serviceNamespace: options.ServiceNamespace,
                serviceVersion: typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString())
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment.name"] = builder.Environment.EnvironmentName,
            });

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.ParseStateValues = true;
            logging.SetResourceBuilder(resource);

            if (options.Enabled)
            {
                logging.AddOtlpExporter(exporter => exporter.Endpoint = options.OtlpEndpoint);
            }
        });

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resourceBuilder => resourceBuilder.AddService(
                builder.Environment.ApplicationName,
                serviceNamespace: options.ServiceNamespace,
                serviceVersion: typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString()))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(builder.Environment.ApplicationName)
                    .AddSource("PulseGuard.Framework.Messaging.RabbitMq")
                    .AddSource("RabbitMQ.Client.*")
                    .AddSource("Npgsql")
                    .AddAspNetCoreInstrumentation(instrumentation => instrumentation.RecordException = true)
                    .AddHttpClientInstrumentation(instrumentation => instrumentation.RecordException = true);

                if (options.Enabled)
                {
                    tracing.AddOtlpExporter(exporter => exporter.Endpoint = options.OtlpEndpoint);
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(builder.Environment.ApplicationName)
                    .AddMeter("PulseGuard.Framework.Messaging.RabbitMq")
                    .AddMeter("Npgsql")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (options.Enabled)
                {
                    metrics.AddOtlpExporter(exporter => exporter.Endpoint = options.OtlpEndpoint);
                }
            });

        return builder;
    }
}
