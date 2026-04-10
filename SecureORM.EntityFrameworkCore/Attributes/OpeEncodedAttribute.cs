namespace SecureORM.EntityFrameworkCore.Attributes;

/// <summary>
/// Marks a string property for OPE encoding. The value is stored as an
/// order-preserving encoded string in the database column.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class OpeEncodedAttribute : Attribute { }
