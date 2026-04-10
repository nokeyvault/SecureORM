using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SecureORM.Core.Encoding;

namespace SecureORM.EntityFrameworkCore.Converters;

public class OpeStringValueConverter : ValueConverter<string, string>
{
    public OpeStringValueConverter(OPEEncoder encoder)
        : base(
            v => encoder.EncodeString(v),
            v => encoder.DecodeString(v))
    { }
}
