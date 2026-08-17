using PulseGuard.Framework.Observability;
using PulseGuard.Framework.Security;
using PulseGuard.Telemetry.Grpc;
using PulseGuard.Telemetry.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddPulseGuardObservability();

builder.Services.AddGrpc();
builder.Services.AddHealthChecks();
builder.Services.AddPulseGuardOidc(builder.Configuration, "pulseguard.telemetry.write");
builder.Services.AddTelemetryInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

if (builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.Services.ApplyTelemetryMigrationsAsync();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapGrpcService<TelemetryIngestionService>().RequireAuthorization("pulseguard.telemetry.write");
app.MapHealthChecks("/health/live").AllowAnonymous();
app.MapGet("/", () => Results.Ok(new { service = "PulseGuard Telemetry gRPC", contract = "contracts/protobuf/telemetry.proto" }));
app.Run();

public partial class Program;
