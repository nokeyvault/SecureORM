using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SecureORM.Core.Encoding;

namespace SecureORM.EntityFrameworkCore.Converters;

public class OpeIntegerValueConverter : ValueConverter<long, string>
{
    public OpeIntegerValueConverter(OPEEncoder encoder)
        : base(
            v => encoder.EncodeInteger(v),
            v => encoder.DecodeInteger(v))
    { }
}
