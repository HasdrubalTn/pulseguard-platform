using PulseGuard.Framework.Observability;
using PulseGuard.Identity;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddPulseGuardObservability();

string? licenseKey = builder.Configuration["Duende:LicenseKey"];
string postmanClientSecret = builder.Configuration["Clients:Postman:ClientSecret"]
    ?? throw new InvalidOperationException(
        "The Postman client secret is missing. Run eng/initialize-development.ps1 before starting IdentityServer.");

IIdentityServerBuilder identityServer = builder.Services
    .AddIdentityServer(options =>
    {
        options.KeyManagement.Enabled = !builder.Environment.IsDevelopment();
        if (!string.IsNullOrWhiteSpace(licenseKey))
        {
            options.LicenseKey = licenseKey;
        }
    })
    .AddInMemoryApiScopes(IdentityConfiguration.ApiScopes)
    .AddInMemoryApiResources(IdentityConfiguration.ApiResources)
    .AddInMemoryClients(IdentityConfiguration.Clients(postmanClientSecret));

if (builder.Environment.IsDevelopment())
{
    identityServer.AddDeveloperSigningCredential();
}

builder.Services.AddHealthChecks();

WebApplication app = builder.Build();
app.UseHttpsRedirection();
app.UseIdentityServer();
app.MapHealthChecks("/health/live");
app.MapGet("/", () => Results.Ok(new
{
    service = "PulseGuard Identity",
    protocol = "OpenID Connect",
    discovery = "/.well-known/openid-configuration",
}));
app.Run();

public partial class Program;
