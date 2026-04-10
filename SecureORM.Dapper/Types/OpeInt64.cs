namespace SecureORM.Dapper.Types;

/// <summary>
/// Wrapper type for OPE-encoded integer columns in Dapper.
/// </summary>
public readonly struct OpeInt64 : IEquatable<OpeInt64>, IComparable<OpeInt64>
{
    public long Value { get; }

    public OpeInt64(long value) => Value = value;

    public static implicit operator OpeInt64(long value) => new(value);
    public static implicit operator long(OpeInt64 ope) => ope.Value;

    public bool Equals(OpeInt64 other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is OpeInt64 other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();
    public int CompareTo(OpeInt64 other) => Value.CompareTo(other.Value);

    public static bool operator ==(OpeInt64 left, OpeInt64 right) => left.Equals(right);
    public static bool operator !=(OpeInt64 left, OpeInt64 right) => !left.Equals(right);
}
