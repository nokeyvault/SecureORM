using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SecureORM.Core.Encoding;

namespace SecureORM.EntityFrameworkCore.Converters;

public class OpeDoubleValueConverter : ValueConverter<double, string>
{
    public OpeDoubleValueConverter(OPEEncoder encoder, int fractionalWidth = 6)
        : base(
            v => encoder.EncodeDecimal((decimal)v, fractionalWidth),
            v => (double)encoder.DecodeDecimal(v, fractionalWidth))
    { }
}
