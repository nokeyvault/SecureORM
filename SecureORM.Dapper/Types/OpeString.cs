namespace SecureORM.Dapper.Types;

/// <summary>
/// Wrapper type for OPE-encoded string columns in Dapper.
/// Use this instead of string for properties that should be encoded.
/// Supports implicit conversion to/from string.
/// </summary>
public readonly struct OpeString : IEquatable<OpeString>, IComparable<OpeString>
{
    public string Value { get; }

    public OpeString(string value) => Value = value ?? string.Empty;

    public static implicit operator OpeString(string value) => new(value);
    public static implicit operator string(OpeString ope) => ope.Value;

    public bool Equals(OpeString other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is OpeString other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;
    public int CompareTo(OpeString other) => string.Compare(Value, other.Value, StringComparison.Ordinal);

    public static bool operator ==(OpeString left, OpeString right) => left.Equals(right);
    public static bool operator !=(OpeString left, OpeString right) => !left.Equals(right);
}
