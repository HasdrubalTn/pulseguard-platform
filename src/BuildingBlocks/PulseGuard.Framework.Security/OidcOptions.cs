namespace PulseGuard.Framework.Security;

public sealed class OidcOptions
{
    public const string SectionName = "Oidc";

    public required string Authority { get; init; }

    public required string Audience { get; init; }

    public bool RequireHttpsMetadata { get; init; } = true;
}
