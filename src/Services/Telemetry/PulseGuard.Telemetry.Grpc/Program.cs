using PulseGuard.Framework.Observability;
using PulseGuard.Framework.Security;
using PulseGuard.Telemetry.Grpc;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddPulseGuardObservability();

builder.Services.AddGrpc();
builder.Services.AddHealthChecks();
builder.Services.AddPulseGuardOidc(builder.Configuration, "pulseguard.telemetry.write");

WebApplication app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapGrpcService<TelemetryIngestionService>().RequireAuthorization("pulseguard.telemetry.write");
app.MapHealthChecks("/health/live").AllowAnonymous();
app.MapGet("/", () => Results.Ok(new { service = "PulseGuard Telemetry gRPC", contract = "contracts/protobuf/telemetry.proto" }));
app.Run();

public partial class Program;
