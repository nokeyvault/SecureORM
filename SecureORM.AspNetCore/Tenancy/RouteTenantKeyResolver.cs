using Microsoft.AspNetCore.Http;

namespace SecureORM.AspNetCore.Tenancy;

/// <summary>
/// Resolves tenant key from route data (e.g., /api/{tenantId}/...).
/// </summary>
public class RouteTenantKeyResolver : ITenantKeyResolver
{
    private readonly string _routeParameterName;

    public RouteTenantKeyResolver(string routeParameterName = "tenantId")
    {
        _routeParameterName = routeParameterName;
    }

    public string? ResolveTenantKey(HttpContext context)
    {
        return context.Request.RouteValues.TryGetValue(_routeParameterName, out var value)
            ? value?.ToString()
            : null;
    }
}
