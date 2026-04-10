using System.Text;

namespace SecureORM.Core.Normalization;

public class UnicodeNormalizerOptions
{
    /// <summary>Unicode normalization form (default: NFC).</summary>
    public NormalizationForm NormalizationForm { get; set; } = NormalizationForm.FormC;

    /// <summary>When true, transliterate accented characters to ASCII equivalents.</summary>
    public bool Transliterate { get; set; } = true;

    /// <summary>
    /// Replacement character for characters that cannot be transliterated.
    /// Set to null to throw on untransliterable characters.
    /// </summary>
    public char? ReplacementChar { get; set; } = '?';

    /// <summary>When true, convert all characters to lowercase before encoding.</summary>
    public bool ToLowerCase { get; set; } = false;
}
