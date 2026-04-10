namespace SecureORM.EntityFrameworkCore.Attributes;

/// <summary>
/// Marks a decimal property for OPE encoding. The value is scaled by
/// the specified fractional width and stored as an order-preserving encoded string.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class OpeDecimalAttribute : Attribute
{
    public int FractionalWidth { get; }

    public OpeDecimalAttribute(int fractionalWidth = 6)
    {
        FractionalWidth = fractionalWidth;
    }
}
