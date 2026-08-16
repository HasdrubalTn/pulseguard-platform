using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace PulseGuard.Identity;

internal static class IdentityConfiguration
{
    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope("pulseguard.patient.read", "Read patient registry data"),
        new ApiScope("pulseguard.patient.write", "Write patient registry data"),
        new ApiScope("pulseguard.telemetry.write", "Stream telemetry measurements"),
    ];

    public static IEnumerable<ApiResource> ApiResources =>
    [
        new ApiResource("pulseguard.patient-registry", "PulseGuard Patient Registry")
        {
            Scopes = { "pulseguard.patient.read", "pulseguard.patient.write" },
        },
        new ApiResource("pulseguard.telemetry", "PulseGuard Telemetry")
        {
            Scopes = { "pulseguard.telemetry.write" },
        },
    ];

    public static IEnumerable<Client> Clients(string postmanClientSecret) =>
    [
        new Client
        {
            ClientId = "pulseguard.postman",
            ClientName = "PulseGuard Postman development client",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret(postmanClientSecret.Sha256()) },
            AllowedScopes =
            {
                "pulseguard.patient.read",
                "pulseguard.patient.write",
                "pulseguard.telemetry.write",
            },
        },
    ];
}
