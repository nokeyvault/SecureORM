using System.Data;
using Dapper;
using SecureORM.Core.Encoding;
using SecureORM.Dapper.Types;

namespace SecureORM.Dapper.Handlers;

public class OpeDecimalHandler : SqlMapper.TypeHandler<OpeDecimalValue>
{
    private readonly OPEEncoder _encoder;
    private readonly int _fractionalWidth;

    public OpeDecimalHandler(OPEEncoder encoder, int fractionalWidth = 6)
    {
        _encoder = encoder;
        _fractionalWidth = fractionalWidth;
    }

    public override void SetValue(IDbDataParameter parameter, OpeDecimalValue value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = _encoder.EncodeDecimal(value.Value, _fractionalWidth);
    }

    public override OpeDecimalValue Parse(object value)
    {
        return new OpeDecimalValue(_encoder.DecodeDecimal(value.ToString()!, _fractionalWidth));
    }
}
