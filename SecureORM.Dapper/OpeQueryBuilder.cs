using SecureORM.Core.Encoding;

namespace SecureORM.Dapper;

/// <summary>
/// Query parameter encoder for Dapper queries against OPE-encoded columns.
/// </summary>
public class OpeQueryBuilder
{
    private readonly OPEEncoder _encoder;

    public OpeQueryBuilder(OPEEncoder encoder) => _encoder = encoder;

    public string EncodeString(string value) => _encoder.EncodeString(value);
    public string EncodeInteger(long value) => _encoder.EncodeInteger(value);
    public string EncodeDecimal(decimal value, int fw = 6) => _encoder.EncodeDecimal(value, fw);

    public string EncodePrefixLike(string prefix) => _encoder.EncodePrefix(prefix) + "%";

    public (string Low, string High) EncodeStringRange(string low, string high)
        => _encoder.EncodeStringRange(low, high);

    public (string Low, string High) EncodeIntegerRange(long low, long high)
        => _encoder.EncodeIntegerRange(low, high);

    public (string Low, string High) EncodeDecimalRange(decimal low, decimal high, int fw = 6)
        => _encoder.EncodeDecimalRange(low, high, fw);
}
