using Microsoft.AspNetCore.Http;
using SecureORM.AspNetCore.Configuration;
using SecureORM.AspNetCore.Tenancy;
using SecureORM.Core.Encoding;
using System.Collections.Concurrent;

namespace SecureORM.AspNetCore.Middleware;

/// <summary>
/// Middleware that resolves the tenant key from the request and creates
/// a scoped OPEEncoder for the current request.
/// </summary>
public class OpeMultiTenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITenantKeyResolver _resolver;
    private readonly MultiTenantOpeOptions _options;

    // Cache encoders by tenant key to avoid re-creating on every request
    private readonly ConcurrentDictionary<string, OPEEncoder> _encoderCache = new();

    public OpeMultiTenantMiddleware(
        RequestDelegate next,
        ITenantKeyResolver resolver,
        MultiTenantOpeOptions options)
    {
        _next = next;
        _resolver = resolver;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context, TenantOpeEncoderAccessor accessor)
    {
        var tenantKey = _resolver.ResolveTenantKey(context);

        if (string.IsNullOrEmpty(tenantKey))
        {
            if (!string.IsNullOrEmpty(_options.DefaultTenantKey))
            {
                tenantKey = _options.DefaultTenantKey;
            }
            else
            {
                throw new InvalidOperationException(
                    "No tenant key found in the request and no default key configured. " +
                    "Ensure a tenant key resolver is configured or set a DefaultTenantKey.");
            }
        }

        var encoder = _encoderCache.GetOrAdd(tenantKey, key =>
            new OPEEncoder(key, _options.NumberPadWidth, _options.SupportNegatives));

        accessor.Encoder = encoder;

        await _next(context);
    }
}
