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
                if (property.GetCustomAttribute<OpeEncodedAttribute>() is not null)
                {
                    modelBuilder.Entity(clrType)
                        .Property(property.Name)
                        .HasConversion(new OpeStringValueConverter(encoder))
                        .HasColumnType("TEXT");
                }
                else if (property.GetCustomAttribute<OpeIntegerAttribute>() is not null)
                {
                    modelBuilder.Entity(clrType)
                        .Property(property.Name)
                        .HasConversion(new OpeIntegerValueConverter(encoder))
                        .HasColumnType("TEXT");
                }
                else if (property.GetCustomAttribute<OpeDecimalAttribute>() is { } decAttr)
                {
                    modelBuilder.Entity(clrType)
                        .Property(property.Name)
                        .HasConversion(new OpeDecimalValueConverter(encoder, decAttr.FractionalWidth))
                        .HasColumnType("TEXT");
                }
            }
        }

        return modelBuilder;
    }
}
