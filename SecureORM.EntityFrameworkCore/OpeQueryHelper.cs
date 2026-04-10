using SecureORM.Core.Encoding;

namespace SecureORM.EntityFrameworkCore;

/// <summary>
/// Pre-encodes query parameters for use in LINQ expressions against OPE-encoded columns.
/// Use this for prefix search and range queries where EF Core cannot auto-apply the converter.
/// </summary>
public class OpeQueryHelper
{
    private readonly OPEEncoder _encoder;

    public OpeQueryHelper(OPEEncoder encoder)
    {
        _encoder = encoder;
    }

    // Exact match helpers (can also just use natural LINQ — EF Core auto-encodes via converter)
    public string EncodeString(string value) => _encoder.EncodeString(value);
    public string EncodeInteger(long value) => _encoder.EncodeInteger(value);
    public string EncodeDecimal(decimal value, int fractionalWidth = 6) => _encoder.EncodeDecimal(value, fractionalWidth);

    // Prefix: use with FromSqlRaw("SELECT * FROM T WHERE Col LIKE {0}", EncodePrefixLike("jo"))
    public string EncodePrefix(string prefix) => _encoder.EncodePrefix(prefix);

    /// <summary>
    /// Returns the encoded prefix with a SQL LIKE wildcard appended (e.g. "encoded%").
    /// Use directly in FromSqlRaw LIKE clauses.
    /// </summary>
    public string EncodePrefixLike(string prefix) => _encoder.EncodePrefix(prefix) + "%";

    // Range: use (Low, High) with EF.Property<string> and CompareTo for BETWEEN-style queries
    public (string Low, string High) EncodeStringRange(string low, string high)
        => _encoder.EncodeStringRange(low, high);

    public (string Low, string High) EncodeIntegerRange(long low, long high)
        => _encoder.EncodeIntegerRange(low, high);

    public (string Low, string High) EncodeDecimalRange(decimal low, decimal high, int fractionalWidth = 6)
        => _encoder.EncodeDecimalRange(low, high, fractionalWidth);
}
