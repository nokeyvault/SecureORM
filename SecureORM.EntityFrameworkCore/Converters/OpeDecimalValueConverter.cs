using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SecureORM.Core.Encoding;

namespace SecureORM.EntityFrameworkCore.Converters;

public class OpeDecimalValueConverter : ValueConverter<decimal, string>
{
    public OpeDecimalValueConverter(OPEEncoder encoder, int fractionalWidth = 6)
        : base(
            v => encoder.EncodeDecimal(v, fractionalWidth),
            v => encoder.DecodeDecimal(v, fractionalWidth))
    { }
}
