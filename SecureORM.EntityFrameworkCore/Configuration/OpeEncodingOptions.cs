using SecureORM.Core.Encoding;
using SecureORM.Core.Normalization;

namespace SecureORM.EntityFrameworkCore.Configuration;

public class OpeEncodingOptions
{
    public string ClientKey { get; set; } = string.Empty;
    public int NumberPadWidth { get; set; } = OPEEncoder.DEFAULT_NUMBER_PAD_WIDTH;
    public bool SupportNegatives { get; set; } = false;
    public IInputNormalizer? Normalizer { get; set; } = null;
}
