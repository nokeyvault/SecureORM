using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SecureORM.Core.Encoding;

namespace SecureORM.EntityFrameworkCore.Converters;

public class OpeFloatValueConverter : ValueConverter<float, string>
{
    public OpeFloatValueConverter(OPEEncoder encoder, int fractionalWidth = 6)
        : base(
            v => encoder.EncodeDecimal((decimal)v, fractionalWidth),
            v => (float)encoder.DecodeDecimal(v, fractionalWidth))
    { }
}
