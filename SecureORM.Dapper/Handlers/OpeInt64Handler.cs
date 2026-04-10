using System.Data;
using Dapper;
using SecureORM.Core.Encoding;
using SecureORM.Dapper.Types;

namespace SecureORM.Dapper.Handlers;

public class OpeInt64Handler : SqlMapper.TypeHandler<OpeInt64>
{
    private readonly OPEEncoder _encoder;

    public OpeInt64Handler(OPEEncoder encoder) => _encoder = encoder;

    public override void SetValue(IDbDataParameter parameter, OpeInt64 value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = _encoder.EncodeInteger(value.Value);
    }

    public override OpeInt64 Parse(object value)
    {
        return new OpeInt64(_encoder.DecodeInteger(value.ToString()!));
    }
}
