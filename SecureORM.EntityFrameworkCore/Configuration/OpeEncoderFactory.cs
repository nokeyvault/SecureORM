using SecureORM.Core.Encoding;

namespace SecureORM.EntityFrameworkCore.Configuration;

public class OpeEncoderFactory
{
    private readonly OPEEncoder _encoder;

    public OpeEncoderFactory(OpeEncodingOptions options)
    {
        if (string.IsNullOrEmpty(options.ClientKey))
            throw new InvalidOperationException(
                "OPE client key is not configured. Call AddSecureOrm(o => o.ClientKey = \"...\").");

        _encoder = new OPEEncoder(
            options.ClientKey,
            options.NumberPadWidth,
            options.SupportNegatives,
            options.Normalizer);
    }

    public OPEEncoder Encoder => _encoder;
}
