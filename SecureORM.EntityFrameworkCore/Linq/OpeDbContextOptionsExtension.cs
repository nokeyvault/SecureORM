using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using SecureORM.Core.Encoding;

namespace SecureORM.EntityFrameworkCore.Linq;

/// <summary>
/// EF Core options extension that registers the OPE LINQ method call translator.
/// </summary>
public class OpeDbContextOptionsExtension : IDbContextOptionsExtension
{
    private readonly OPEEncoder _encoder;

    public OpeDbContextOptionsExtension(OPEEncoder encoder)
    {
        _encoder = encoder;
    }

    public DbContextOptionsExtensionInfo Info => new OpeExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        var encoder = _encoder;
        services.AddSingleton<IMethodCallTranslatorPlugin>(sp =>
            new OpeMethodCallTranslatorPlugin(
                encoder,
                sp.GetRequiredService<ISqlExpressionFactory>()));
    }

    public void Validate(IDbContextOptions options) { }

    private sealed class OpeExtensionInfo : DbContextOptionsExtensionInfo
    {
        public OpeExtensionInfo(IDbContextOptionsExtension extension) : base(extension) { }

        public override bool IsDatabaseProvider => false;
        public override string LogFragment => "SecureORM.OPE ";

        public override int GetServiceProviderHashCode() => 0;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is OpeExtensionInfo;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["SecureORM:OpeTranslator"] = "enabled";
        }
    }
}
