using Microsoft.AspNetCore.Http;

namespace SecureORM.AspNetCore.Tenancy;

/// <summary>
/// Resolves tenant key from an HTTP request header (default: X-Tenant-Key).
/// </summary>
public class HeaderTenantKeyResolver : ITenantKeyResolver
{
    private readonly string _headerName;

    public HeaderTenantKeyResolver(string headerName = "X-Tenant-Key")
    {
        _headerName = headerName;
    }

    public string? ResolveTenantKey(HttpContext context)
    {
        return context.Request.Headers.TryGetValue(_headerName, out var value)
            ? value.ToString()
            : null;
    }
}
