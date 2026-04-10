using System.Globalization;
using System.Text;

namespace SecureORM.Core.Normalization;

/// <summary>
/// Normalizes Unicode input to ASCII-safe strings suitable for OPE encoding.
/// Applies NFC normalization, optional transliteration, and optional lowercasing.
/// </summary>
public class UnicodeNormalizer : IInputNormalizer
{
    private readonly UnicodeNormalizerOptions _options;

    // Common accented character → ASCII mapping
    private static readonly Dictionary<char, char> TransliterationMap = BuildTransliterationMap();

    public UnicodeNormalizer(UnicodeNormalizerOptions? options = null)
    {
        _options = options ?? new UnicodeNormalizerOptions();
    }

    public string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Step 1: Unicode normalization
        string normalized = input.Normalize(_options.NormalizationForm);

        // Step 2: Optional lowercase
        if (_options.ToLowerCase)
            normalized = normalized.ToLowerInvariant();

        // Step 3: Transliterate if enabled
        if (_options.Transliterate)
            normalized = Transliterate(normalized);

        return normalized;
    }

    private string Transliterate(string input)
    {
        var sb = new StringBuilder(input.Length);

        foreach (char c in input)
        {
            if (c <= 127) // ASCII — pass through
            {
                sb.Append(c);
                continue;
            }

            if (TransliterationMap.TryGetValue(c, out char replacement))
            {
                sb.Append(replacement);
                continue;
            }

            // Try Unicode decomposition: accented char → base char + combining mark
            string decomposed = c.ToString().Normalize(NormalizationForm.FormD);
            if (decomposed.Length > 0 && decomposed[0] <= 127)
            {
                sb.Append(decomposed[0]); // Take the base character
                continue;
            }

            // Cannot transliterate
            if (_options.ReplacementChar.HasValue)
            {
                sb.Append(_options.ReplacementChar.Value);
            }
            else
            {
                throw new ArgumentException(
                    $"Character '{c}' (U+{(int)c:X4}) cannot be transliterated to ASCII " +
                    "and no replacement character is configured.");
            }
        }

        return sb.ToString();
    }

    private static Dictionary<char, char> BuildTransliterationMap()
    {
        return new Dictionary<char, char>
        {
            // Latin accented vowels
            ['\u00C0'] = 'A', ['\u00C1'] = 'A', ['\u00C2'] = 'A', ['\u00C3'] = 'A', ['\u00C4'] = 'A', ['\u00C5'] = 'A',
            ['\u00E0'] = 'a', ['\u00E1'] = 'a', ['\u00E2'] = 'a', ['\u00E3'] = 'a', ['\u00E4'] = 'a', ['\u00E5'] = 'a',
            ['\u00C8'] = 'E', ['\u00C9'] = 'E', ['\u00CA'] = 'E', ['\u00CB'] = 'E',
            ['\u00E8'] = 'e', ['\u00E9'] = 'e', ['\u00EA'] = 'e', ['\u00EB'] = 'e',
            ['\u00CC'] = 'I', ['\u00CD'] = 'I', ['\u00CE'] = 'I', ['\u00CF'] = 'I',
            ['\u00EC'] = 'i', ['\u00ED'] = 'i', ['\u00EE'] = 'i', ['\u00EF'] = 'i',
            ['\u00D2'] = 'O', ['\u00D3'] = 'O', ['\u00D4'] = 'O', ['\u00D5'] = 'O', ['\u00D6'] = 'O',
            ['\u00F2'] = 'o', ['\u00F3'] = 'o', ['\u00F4'] = 'o', ['\u00F5'] = 'o', ['\u00F6'] = 'o',
            ['\u00D9'] = 'U', ['\u00DA'] = 'U', ['\u00DB'] = 'U', ['\u00DC'] = 'U',
            ['\u00F9'] = 'u', ['\u00FA'] = 'u', ['\u00FB'] = 'u', ['\u00FC'] = 'u',
            // Other common Latin
            ['\u00C7'] = 'C', ['\u00E7'] = 'c', // C/c cedilla
            ['\u00D1'] = 'N', ['\u00F1'] = 'n', // N/n tilde
            ['\u00DD'] = 'Y', ['\u00FD'] = 'y', ['\u00FF'] = 'y', // Y/y accented
            ['\u00DF'] = 's', // sharp s → ss (simplified to s)
            ['\u00D0'] = 'D', ['\u00F0'] = 'd', // Eth
            ['\u00DE'] = 'T', ['\u00FE'] = 't', // Thorn
            ['\u00C6'] = 'A', ['\u00E6'] = 'a', // Ae ligature
            ['\u0152'] = 'O', ['\u0153'] = 'o', // Oe ligature
            ['\u0160'] = 'S', ['\u0161'] = 's', // S/s caron
            ['\u017D'] = 'Z', ['\u017E'] = 'z', // Z/z caron
        };
    }
}
