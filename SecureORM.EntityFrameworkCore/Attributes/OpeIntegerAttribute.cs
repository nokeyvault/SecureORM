namespace SecureORM.EntityFrameworkCore.Attributes;

/// <summary>
/// Marks a long/int property for OPE encoding. The numeric value is
/// zero-padded and stored as an order-preserving encoded string.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class OpeIntegerAttribute : Attribute { }
