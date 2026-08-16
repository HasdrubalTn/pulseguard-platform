using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PulseGuard.Framework.Security;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddPulseGuardOidc(
        this IServiceCollection services,
        IConfiguration configuration,
        params string[] requiredScopes)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        OidcOptions oidc = configuration
            .GetRequiredSection(OidcOptions.SectionName)
            .Get<OidcOptions>()
            ?? throw new InvalidOperationException("The OIDC configuration is missing.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = oidc.Authority;
                options.Audience = oidc.Audience;
                options.RequireHttpsMetadata = oidc.RequireHttpsMetadata;
                options.MapInboundClaims = false;
            });

        services.AddAuthorization(options =>
        {
            foreach (string scope in requiredScopes)
            {
                options.AddPolicy(scope, policy =>
                    policy.RequireAuthenticatedUser().RequireAssertion(context => HasScope(context.User, scope)));
            }
        });

        return services;
    }

    private static bool HasScope(System.Security.Claims.ClaimsPrincipal principal, string expectedScope) =>
        principal
            .FindAll("scope")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(expectedScope, StringComparer.Ordinal);
}
