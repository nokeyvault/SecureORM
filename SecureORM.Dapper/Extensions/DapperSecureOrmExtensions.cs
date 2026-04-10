using Dapper;
using SecureORM.Core.Encoding;
using SecureORM.Dapper.Handlers;

namespace SecureORM.Dapper.Extensions;

public static class DapperSecureOrmExtensions
{
    /// <summary>
    /// Registers all OPE type handlers with Dapper's SqlMapper.
    /// Call once at application startup.
    /// </summary>
    public static void AddSecureOrmDapper(OPEEncoder encoder, int decimalFractionalWidth = 6)
    {
        SqlMapper.AddTypeHandler(new OpeStringHandler(encoder));
        SqlMapper.AddTypeHandler(new OpeInt64Handler(encoder));
        SqlMapper.AddTypeHandler(new OpeDecimalHandler(encoder, decimalFractionalWidth));
    }
}
