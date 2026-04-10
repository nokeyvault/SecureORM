using SecureORM.Core.Encoding;

namespace SecureORM.AspNetCore.Configuration;

public class MultiTenantOpeOptions
{
    /// <summary>Number pad width for the OPE encoder.</summary>
    public int NumberPadWidth { get; set; } = OPEEncoder.DEFAULT_NUMBER_PAD_WIDTH;

    /// <summary>Enable negative number support.</summary>
    public bool SupportNegatives { get; set; } = false;

    /// <summary>
    /// Fallback tenant key when no resolver finds a key.
    /// Leave empty to throw on missing tenant key.
    /// </summary>
    public string DefaultTenantKey { get; set; } = string.Empty;
}
