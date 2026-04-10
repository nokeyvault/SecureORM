using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SecureORM.Core.Encoding;

namespace SecureORM.EntityFrameworkCore.Converters;

public class OpeInt32ValueConverter : ValueConverter<int, string>
{
    public OpeInt32ValueConverter(OPEEncoder encoder)
        : base(
            v => encoder.EncodeInteger(v),
            v => (int)encoder.DecodeInteger(v))
    { }
}
