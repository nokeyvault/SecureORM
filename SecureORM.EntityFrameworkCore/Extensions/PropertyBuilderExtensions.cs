using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureORM.Core.Encoding;
using SecureORM.EntityFrameworkCore.Converters;

namespace SecureORM.EntityFrameworkCore.Extensions;

public static class PropertyBuilderExtensions
{
    /// <summary>
    /// Configures the string property for OPE encoding (fluent API alternative to [OpeEncoded]).
    /// </summary>
    public static PropertyBuilder<string> HasOpeEncoding(
        this PropertyBuilder<string> builder, OPEEncoder encoder)
    {
        builder.HasConversion(new OpeStringValueConverter(encoder));
        builder.HasColumnType("TEXT");
        return builder;
    }

    /// <summary>
    /// Configures the long property for OPE integer encoding (fluent API alternative to [OpeInteger]).
    /// </summary>
    public static PropertyBuilder<long> HasOpeIntegerEncoding(
        this PropertyBuilder<long> builder, OPEEncoder encoder)
    {
        builder.HasConversion(new OpeIntegerValueConverter(encoder));
        builder.HasColumnType("TEXT");
        return builder;
    }

    /// <summary>
    /// Configures the decimal property for OPE decimal encoding (fluent API alternative to [OpeDecimal]).
    /// </summary>
    public static PropertyBuilder<decimal> HasOpeDecimalEncoding(
        this PropertyBuilder<decimal> builder, OPEEncoder encoder, int fractionalWidth = 6)
    {
        builder.HasConversion(new OpeDecimalValueConverter(encoder, fractionalWidth));
        builder.HasColumnType("TEXT");
        return builder;
    }
}
