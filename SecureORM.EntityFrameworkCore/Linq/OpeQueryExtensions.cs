namespace SecureORM.EntityFrameworkCore.Linq;

/// <summary>
/// LINQ marker methods for OPE queries. These translate to SQL via the
/// OPE IMethodCallTranslator — they throw if called directly in C#.
/// Register with UseSecureOrmTranslations() on DbContextOptionsBuilder.
/// </summary>
public static class OpeQueryExtensions
{
    /// <summary>
    /// Translates to SQL: WHERE column LIKE 'encoded_prefix%'
    /// </summary>
    public static bool OpeStartsWith(this string property, string prefix)
        => throw new InvalidOperationException(
            "OpeStartsWith can only be used in EF Core LINQ queries. " +
            "Register with UseSecureOrmTranslations().");

    /// <summary>
    /// Translates to SQL: WHERE column >= encoded_low AND column &lt;= encoded_high
    /// </summary>
    public static bool OpeInRange(this string property, string low, string high)
        => throw new InvalidOperationException(
            "OpeInRange can only be used in EF Core LINQ queries. " +
            "Register with UseSecureOrmTranslations().");

    /// <summary>
    /// Translates to SQL: WHERE column >= encoded_low AND column &lt;= encoded_high
    /// </summary>
    public static bool OpeInRange(this long property, long low, long high)
        => throw new InvalidOperationException(
            "OpeInRange can only be used in EF Core LINQ queries. " +
            "Register with UseSecureOrmTranslations().");

    /// <summary>
    /// Translates to SQL: WHERE column >= encoded_low AND column &lt;= encoded_high
    /// </summary>
    public static bool OpeInRange(this decimal property, decimal low, decimal high)
        => throw new InvalidOperationException(
            "OpeInRange can only be used in EF Core LINQ queries. " +
            "Register with UseSecureOrmTranslations().");

    /// <summary>
    /// Translates to SQL: WHERE column >= encoded_low AND column &lt;= encoded_high
    /// </summary>
    public static bool OpeInRange(this int property, int low, int high)
        => throw new InvalidOperationException(
            "OpeInRange can only be used in EF Core LINQ queries. " +
            "Register with UseSecureOrmTranslations().");
}
