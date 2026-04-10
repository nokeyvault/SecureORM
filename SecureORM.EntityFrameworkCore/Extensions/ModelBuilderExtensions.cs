using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SecureORM.Core.Encoding;
using SecureORM.EntityFrameworkCore.Attributes;
using SecureORM.EntityFrameworkCore.Converters;

namespace SecureORM.EntityFrameworkCore.Extensions;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Scans all registered entity types for [OpeEncoded], [OpeInteger], and
    /// [OpeDecimal] attributes and wires up the appropriate value converters.
    /// Supports string, long, int, short, decimal, float, and double property types.
    /// Call this in OnModelCreating.
    /// </summary>
    public static ModelBuilder ApplyOpeEncodings(this ModelBuilder modelBuilder, OPEEncoder encoder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (clrType == null) continue;

            foreach (var property in clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetCustomAttribute<OpeEncodedAttribute>() is { } strAttr)
                {
                    var pb = modelBuilder.Entity(clrType)
                        .Property(property.Name)
                        .HasConversion(new OpeStringValueConverter(encoder))
                        .HasColumnType("TEXT");

                    if (strAttr.MaxLength > 0)
                        pb.HasMaxLength(OpeColumnSizing.EncodedStringLength(strAttr.MaxLength));
                }
                else if (property.GetCustomAttribute<OpeIntegerAttribute>() is not null)
                {
                    var propType = property.PropertyType;
                    var propBuilder = modelBuilder.Entity(clrType).Property(property.Name);

                    if (propType == typeof(long) || propType == typeof(long?))
                        propBuilder.HasConversion(new OpeIntegerValueConverter(encoder));
                    else if (propType == typeof(int) || propType == typeof(int?))
                        propBuilder.HasConversion(new OpeInt32ValueConverter(encoder));
                    else if (propType == typeof(short) || propType == typeof(short?))
                        propBuilder.HasConversion(new OpeInt16ValueConverter(encoder));
                    else
                        propBuilder.HasConversion(new OpeIntegerValueConverter(encoder));

                    propBuilder.HasColumnType("TEXT");
                }
                else if (property.GetCustomAttribute<OpeDecimalAttribute>() is { } decAttr)
                {
                    var propType = property.PropertyType;
                    var propBuilder = modelBuilder.Entity(clrType).Property(property.Name);

                    if (propType == typeof(decimal) || propType == typeof(decimal?))
                        propBuilder.HasConversion(new OpeDecimalValueConverter(encoder, decAttr.FractionalWidth));
                    else if (propType == typeof(float) || propType == typeof(float?))
                        propBuilder.HasConversion(new OpeFloatValueConverter(encoder, decAttr.FractionalWidth));
                    else if (propType == typeof(double) || propType == typeof(double?))
                        propBuilder.HasConversion(new OpeDoubleValueConverter(encoder, decAttr.FractionalWidth));
                    else
                        propBuilder.HasConversion(new OpeDecimalValueConverter(encoder, decAttr.FractionalWidth));

                    propBuilder.HasColumnType("TEXT");
                }
            }
        }

        return modelBuilder;
    }
}
