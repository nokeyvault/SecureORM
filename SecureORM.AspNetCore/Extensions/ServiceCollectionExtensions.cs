using Microsoft.Extensions.DependencyInjection;
using SecureORM.AspNetCore.Configuration;
using SecureORM.AspNetCore.Tenancy;

namespace SecureORM.AspNetCore.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers multi-tenant OPE services. After this, add middleware with UseSecureOrmMultiTenant().
    /// </summary>
    public static IServiceCollection AddSecureOrmMultiTenant(
        this IServiceCollection services,
        Action<MultiTenantOpeOptions> configure)
    {
        var options = new MultiTenantOpeOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddScoped<TenantOpeEncoderAccessor>();

        return services;
    }

    /// <summary>
    /// Registers a tenant key resolver. Can be called multiple times to register
    /// multiple resolvers (wrap with CompositeTenantKeyResolver).
    /// </summary>
    public static IServiceCollection AddTenantKeyResolver<TResolver>(
        this IServiceCollection services)
        where TResolver : class, ITenantKeyResolver
    {
        services.AddSingleton<ITenantKeyResolver, TResolver>();
        return services;
    }

    /// <summary>
    /// Registers a tenant key resolver instance.
    /// </summary>
    public static IServiceCollection AddTenantKeyResolver(
        this IServiceCollection services, ITenantKeyResolver resolver)
    {
        services.AddSingleton(resolver);
        return services;
    }
}
