using SecureORM.Core.Encoding;

namespace SecureORM.EntityFrameworkCore.Configuration;

public class OpeEncodingOptions
{
    public string ClientKey { get; set; } = string.Empty;
    public int NumberPadWidth { get; set; } = OPEEncoder.DEFAULT_NUMBER_PAD_WIDTH;
}
