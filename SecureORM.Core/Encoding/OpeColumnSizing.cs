namespace SecureORM.Core.Encoding;

/// <summary>
/// Provides column size calculations for OPE-encoded values.
/// Use these to configure database column lengths (e.g., nvarchar(N)).
/// </summary>
public static class OpeColumnSizing
{
    /// <summary>Width of each encoded character token (6 digits).</summary>
    public const int CodeWidth = OPEEncoder.CODE_WIDTH;

    /// <summary>
    /// Calculates the encoded string length for a given maximum plaintext length.
    /// Example: MaxLength=100 → EncodedLength=600.
    /// </summary>
    public static int EncodedStringLength(int maxPlaintextLength)
        => maxPlaintextLength * CodeWidth;

    /// <summary>
    /// Calculates the encoded integer column length.
    /// </summary>
    public static int EncodedIntegerLength(
        int numberPadWidth = OPEEncoder.DEFAULT_NUMBER_PAD_WIDTH,
        bool supportNegatives = false)
        => (numberPadWidth + (supportNegatives ? 1 : 0)) * CodeWidth;

    /// <summary>
    /// Calculates the encoded decimal column length.
    /// </summary>
    public static int EncodedDecimalLength(
        int numberPadWidth = OPEEncoder.DEFAULT_NUMBER_PAD_WIDTH,
        int fractionalWidth = 6,
        bool supportNegatives = false)
        => (numberPadWidth + fractionalWidth + (supportNegatives ? 1 : 0)) * CodeWidth;
}
