namespace SecureORM.EntityFrameworkCore.Attributes;

/// <summary>
/// Marks a string property for OPE encoding. The value is stored as an
/// order-preserving encoded string in the database column.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class OpeEncodedAttribute : Attribute
{
    /// <summary>
    /// Maximum plaintext length. When set, the database column is sized to
    /// MaxLength * 6 characters. Set to 0 (default) for unspecified.
    /// </summary>
    public int MaxLength { get; set; } = 0;
}
