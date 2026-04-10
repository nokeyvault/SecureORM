using Microsoft.AspNetCore.Http;

namespace SecureORM.AspNetCore.Tenancy;

/// <summary>
/// Tries multiple resolvers in order, returning the first non-null key.
/// </summary>
public class CompositeTenantKeyResolver : ITenantKeyResolver
{
    private readonly ITenantKeyResolver[] _resolvers;

    public CompositeTenantKeyResolver(params ITenantKeyResolver[] resolvers)
    {
        _resolvers = resolvers;
    }

    public string? ResolveTenantKey(HttpContext context)
    {
        foreach (var resolver in _resolvers)
        {
            var key = resolver.ResolveTenantKey(context);
            if (!string.IsNullOrEmpty(key))
                return key;
        }
        return null;
    }
}
