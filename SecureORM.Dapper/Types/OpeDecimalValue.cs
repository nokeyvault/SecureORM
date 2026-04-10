namespace SecureORM.Dapper.Types;

/// <summary>
/// Wrapper type for OPE-encoded decimal columns in Dapper.
/// </summary>
public readonly struct OpeDecimalValue : IEquatable<OpeDecimalValue>, IComparable<OpeDecimalValue>
{
    public decimal Value { get; }

    public OpeDecimalValue(decimal value) => Value = value;

    public static implicit operator OpeDecimalValue(decimal value) => new(value);
    public static implicit operator decimal(OpeDecimalValue ope) => ope.Value;

    public bool Equals(OpeDecimalValue other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is OpeDecimalValue other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();
    public int CompareTo(OpeDecimalValue other) => Value.CompareTo(other.Value);

    public static bool operator ==(OpeDecimalValue left, OpeDecimalValue right) => left.Equals(right);
    public static bool operator !=(OpeDecimalValue left, OpeDecimalValue right) => !left.Equals(right);
}
