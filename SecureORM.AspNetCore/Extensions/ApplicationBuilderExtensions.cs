using Microsoft.AspNetCore.Builder;
using SecureORM.AspNetCore.Middleware;

namespace SecureORM.AspNetCore.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the multi-tenant OPE middleware to the request pipeline.
    /// Call after UseAuthentication() if using ClaimTenantKeyResolver.
    /// </summary>
    public static IApplicationBuilder UseSecureOrmMultiTenant(this IApplicationBuilder app)
    {
        return app.UseMiddleware<OpeMultiTenantMiddleware>();
    }
}
