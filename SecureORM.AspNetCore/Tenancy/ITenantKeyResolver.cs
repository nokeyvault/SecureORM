using Microsoft.AspNetCore.Http;

namespace SecureORM.AspNetCore.Tenancy;

/// <summary>
/// Resolves the OPE client key for the current HTTP request.
/// </summary>
public interface ITenantKeyResolver
{
    string? ResolveTenantKey(HttpContext context);
}
