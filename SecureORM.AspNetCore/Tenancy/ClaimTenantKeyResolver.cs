using Microsoft.AspNetCore.Http;

namespace SecureORM.AspNetCore.Tenancy;

/// <summary>
/// Resolves tenant key from an authenticated user's claims.
/// </summary>
public class ClaimTenantKeyResolver : ITenantKeyResolver
{
    private readonly string _claimType;

    public ClaimTenantKeyResolver(string claimType = "tenant_key")
    {
        _claimType = claimType;
    }

    public string? ResolveTenantKey(HttpContext context)
    {
        return context.User?.FindFirst(_claimType)?.Value;
    }
}
