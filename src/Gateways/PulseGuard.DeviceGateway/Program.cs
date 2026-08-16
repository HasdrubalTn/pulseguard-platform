using PulseGuard.DeviceGateway;
using PulseGuard.Framework.Hl7;
using PulseGuard.Framework.Messaging;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services
    .AddOptions<MllpListenerOptions>()
    .Bind(builder.Configuration.GetRequiredSection(MllpListenerOptions.SectionName))
    .Validate(options => System.Net.IPAddress.TryParse(options.BindAddress, out _), "BindAddress must be an IP address.")
    .Validate(options => options.Port is > 0 and <= 65535, "Port must be between 1 and 65535.")
    .Validate(options => options.MaximumMessageBytes is >= 1024 and <= 16 * 1024 * 1024, "Message size is outside the safe range.")
    .Validate(options => options.ReadTimeoutSeconds is >= 1 and <= 300, "Read timeout is outside the safe range.")
    .Validate(options => options.MaximumConcurrentConnections is >= 1 and <= 1000, "Connection limit is outside the safe range.")
    .ValidateOnStart();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<Hl7MessageParser>();
builder.Services.AddSingleton<Hl7AcknowledgementFactory>();
builder.Services.AddSingleton<IIntegrationEventPublisher, LoggingIntegrationEventPublisher>();
builder.Services.AddSingleton<Hl7MessageProcessor>();
builder.Services.AddHostedService<MllpListenerService>();

WebApplication app = builder.Build();
app.MapHealthChecks("/health/live");
app.Run();

public partial class Program;
