using System.Data;
using Dapper;
using SecureORM.Core.Encoding;
using SecureORM.Dapper.Types;

namespace SecureORM.Dapper.Handlers;

public class OpeStringHandler : SqlMapper.TypeHandler<OpeString>
{
    private readonly OPEEncoder _encoder;

    public OpeStringHandler(OPEEncoder encoder) => _encoder = encoder;

    public override void SetValue(IDbDataParameter parameter, OpeString value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = _encoder.EncodeString(value.Value);
    }

    public override OpeString Parse(object value)
    {
        return new OpeString(_encoder.DecodeString(value.ToString()!));
    }
}
