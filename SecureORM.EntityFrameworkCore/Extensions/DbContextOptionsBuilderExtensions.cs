using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SecureORM.Core.Encoding;
using SecureORM.EntityFrameworkCore.Linq;

namespace SecureORM.EntityFrameworkCore.Extensions;

public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Registers the OPE LINQ query translator so that OpeStartsWith() and
    /// OpeInRange() methods are translated to native SQL.
    /// </summary>
    public static DbContextOptionsBuilder UseSecureOrmTranslations(
        this DbContextOptionsBuilder builder, OPEEncoder encoder)
    {
        ((IDbContextOptionsBuilderInfrastructure)builder)
            .AddOrUpdateExtension(new OpeDbContextOptionsExtension(encoder));
        return builder;
    }
}
