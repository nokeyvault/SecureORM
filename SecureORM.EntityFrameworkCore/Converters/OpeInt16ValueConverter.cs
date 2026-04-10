using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SecureORM.Core.Encoding;

namespace SecureORM.EntityFrameworkCore.Converters;

public class OpeInt16ValueConverter : ValueConverter<short, string>
{
    public OpeInt16ValueConverter(OPEEncoder encoder)
        : base(
            v => encoder.EncodeInteger(v),
            v => (short)encoder.DecodeInteger(v))
    { }
}
