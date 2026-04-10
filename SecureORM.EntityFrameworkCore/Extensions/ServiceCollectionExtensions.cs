using Microsoft.Extensions.DependencyInjection;
using SecureORM.Core.Encoding;
using SecureORM.EntityFrameworkCore.Configuration;

namespace SecureORM.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the OPE encoding services (OpeEncoderFactory, OPEEncoder, OpeQueryHelper).
    /// </summary>
    public static IServiceCollection AddSecureOrm(
        this IServiceCollection services,
        Action<OpeEncodingOptions> configure)
    {
        var options = new OpeEncodingOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton<OpeEncoderFactory>();
        services.AddSingleton(sp => sp.GetRequiredService<OpeEncoderFactory>().Encoder);
        services.AddSingleton<OpeQueryHelper>();

        return services;
    }
}
