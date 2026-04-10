using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureORM.Core.Encoding;
using SecureORM.EntityFrameworkCore.Converters;

namespace SecureORM.EntityFrameworkCore.Extensions;

public static class PropertyBuilderExtensions
{
    // ─── String ────────────────────────────────────────────────────────

    public static PropertyBuilder<string> HasOpeEncoding(
        this PropertyBuilder<string> builder, OPEEncoder encoder)
    {
        builder.HasConversion(new OpeStringValueConverter(encoder));
        builder.HasColumnType("TEXT");
        return builder;
    }

    // ─── Integer types ─────────────────────────────────────────────────

    public static PropertyBuilder<long> HasOpeIntegerEncoding(
        this PropertyBuilder<long> builder, OPEEncoder encoder)
    {
        builder.HasConversion(new OpeIntegerValueConverter(encoder));
        builder.HasColumnType("TEXT");
        return builder;
    }

    public static PropertyBuilder<int> HasOpeIntegerEncoding(
        this PropertyBuilder<int> builder, OPEEncoder encoder)
    {
        builder.HasConversion(new OpeInt32ValueConverter(encoder));
        builder.HasColumnType("TEXT");
        return builder;
    }

    public static PropertyBuilder<short> HasOpeIntegerEncoding(
        this PropertyBuilder<short> builder, OPEEncoder encoder)
    {
        builder.HasConversion(new OpeInt16ValueConverter(encoder));
        builder.HasColumnType("TEXT");
        return builder;
    }

    // ─── Decimal types ─────────────────────────────────────────────────

    public static PropertyBuilder<decimal> HasOpeDecimalEncoding(
        this PropertyBuilder<decimal> builder, OPEEncoder encoder, int fractionalWidth = 6)
    {
        builder.HasConversion(new OpeDecimalValueConverter(encoder, fractionalWidth));
        builder.HasColumnType("TEXT");
        return builder;
    }

    public static PropertyBuilder<float> HasOpeDecimalEncoding(
        this PropertyBuilder<float> builder, OPEEncoder encoder, int fractionalWidth = 6)
    {
        builder.HasConversion(new OpeFloatValueConverter(encoder, fractionalWidth));
        builder.HasColumnType("TEXT");
        return builder;
    }

    public static PropertyBuilder<double> HasOpeDecimalEncoding(
        this PropertyBuilder<double> builder, OPEEncoder encoder, int fractionalWidth = 6)
    {
        builder.HasConversion(new OpeDoubleValueConverter(encoder, fractionalWidth));
        builder.HasColumnType("TEXT");
        return builder;
    }
}
