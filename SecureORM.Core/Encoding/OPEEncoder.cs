using System.Security.Cryptography;

namespace SecureORM.Core.Encoding;

/// <summary>
/// Order-Preserving Encoder (OPE)
///
/// Every character in the supported universe maps to a fixed-width numeric code.
/// Codes are assigned in strict ascending order — smaller input always produces
/// a smaller code, guaranteeing database-level ORDER BY, range queries, and
/// prefix LIKE queries work correctly on the encoded ciphertext without decryption.
///
/// A client-supplied key derives a deterministic visual offset that shifts
/// all codes by a key-specific amount.
/// </summary>
public sealed class OPEEncoder
{
    private static readonly char[] StringUniverse = BuildStringUniverse();

    private static char[] BuildStringUniverse()
    {
        var universe = new List<char>();
        universe.Add(' ');
        string specials = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";
        universe.AddRange(specials);
        universe.AddRange("0123456789");
        universe.AddRange("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        universe.AddRange("abcdefghijklmnopqrstuvwxyz");
        return universe.ToArray();
    }

    /// <summary>Width of each encoded token in decimal digits.</summary>
    internal const int CODE_WIDTH = 6;

    private static readonly long MAX_CODE = (long)Math.Pow(10, CODE_WIDTH) - 1;

    /// <summary>Default maximum digit width for numeric values (supports up to 999,999,999,999).</summary>
    public const int DEFAULT_NUMBER_PAD_WIDTH = 12;

    private readonly Dictionary<char, string> _encodeMap;
    private readonly Dictionary<string, char> _decodeMap;
    private readonly int _numberPadWidth;
    private readonly bool _supportNegatives;
    private readonly Normalization.IInputNormalizer? _normalizer;

    /// <summary>Whether this encoder supports negative numbers.</summary>
    public bool SupportsNegatives => _supportNegatives;

    /// <summary>The configured number pad width.</summary>
    public int NumberPadWidth => _numberPadWidth;

    /// <summary>
    /// Initialise the encoder for a specific client.
    /// </summary>
    /// <param name="clientKey">A secret string unique to this client/tenant.</param>
    /// <param name="numberPadWidth">Maximum digit width for numeric columns (1-18).</param>
    /// <param name="supportNegatives">
    /// When true, negative integers and decimals are supported via a sign-prefix scheme.
    /// Encoded data is NOT compatible between supportNegatives=true and false.
    /// </param>
    /// <param name="normalizer">Optional input normalizer for Unicode handling.</param>
    public OPEEncoder(
        string clientKey,
        int numberPadWidth = DEFAULT_NUMBER_PAD_WIDTH,
        bool supportNegatives = false,
        Normalization.IInputNormalizer? normalizer = null)
    {
        if (string.IsNullOrEmpty(clientKey))
            throw new ArgumentException("Client key must not be null or empty.", nameof(clientKey));

        if (numberPadWidth < 1 || numberPadWidth > 18)
            throw new ArgumentOutOfRangeException(nameof(numberPadWidth),
                "Number pad width must be between 1 and 18.");

        _numberPadWidth = numberPadWidth;
        _supportNegatives = supportNegatives;
        _normalizer = normalizer;

        var (enc, dec) = BuildMappingTable(clientKey);
        _encodeMap = enc;
        _decodeMap = dec;
    }

    private static (Dictionary<char, string> encode, Dictionary<string, char> decode)
        BuildMappingTable(string clientKey)
    {
        byte[] keyHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(clientKey));

        int charCount = StringUniverse.Length;
        long spacing = MAX_CODE / (charCount + 1);
        long rawOffset = BitConverter.ToUInt32(keyHash, 0);
        long visualOffset = rawOffset % spacing;

        var encode = new Dictionary<char, string>(charCount);
        var decode = new Dictionary<string, char>(charCount);

        for (int i = 0; i < charCount; i++)
        {
            long baseCode = (i + 1) * spacing;
            long finalCode = baseCode + visualOffset;
            finalCode = Math.Min(finalCode, MAX_CODE);

            string codeStr = finalCode.ToString().PadLeft(CODE_WIDTH, '0');

            encode[StringUniverse[i]] = codeStr;
            decode[codeStr] = StringUniverse[i];
        }

        return (encode, decode);
    }

    // ─────────────────────────────────────────────────────────────────────
    // String encoding
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Encodes a string value. The resulting ciphertext sorts lexicographically
    /// in the same order as the original plaintext.
    /// </summary>
    public string EncodeString(string plaintext)
    {
        if (plaintext == null) throw new ArgumentNullException(nameof(plaintext));

        // Apply normalization if configured
        if (_normalizer != null)
            plaintext = _normalizer.Normalize(plaintext);

        var sb = new System.Text.StringBuilder(plaintext.Length * CODE_WIDTH);

        foreach (char c in plaintext)
        {
            if (!_encodeMap.TryGetValue(c, out string? code))
                throw new ArgumentException(
                    $"Character '{c}' (U+{(int)c:X4}) is not in the supported universe.",
                    nameof(plaintext));

            sb.Append(code);
        }

        return sb.ToString();
    }

    /// <summary>Decodes a string ciphertext back to its plaintext.</summary>
    public string DecodeString(string ciphertext)
    {
        if (ciphertext == null) throw new ArgumentNullException(nameof(ciphertext));

        if (ciphertext.Length % CODE_WIDTH != 0)
            throw new ArgumentException(
                $"Ciphertext length {ciphertext.Length} is not a multiple of {CODE_WIDTH}.",
                nameof(ciphertext));

        var sb = new System.Text.StringBuilder(ciphertext.Length / CODE_WIDTH);

        for (int i = 0; i < ciphertext.Length; i += CODE_WIDTH)
        {
            string chunk = ciphertext.Substring(i, CODE_WIDTH);

            if (!_decodeMap.TryGetValue(chunk, out char c))
                throw new ArgumentException(
                    $"Unknown code token '{chunk}' at position {i}.", nameof(ciphertext));

            sb.Append(c);
        }

        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Integer encoding (with negative support)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Encodes an integer value. When supportNegatives is enabled, negative values
    /// use a sign-prefix + nines' complement scheme to preserve sort order.
    /// </summary>
    public string EncodeInteger(long value)
    {
        if (_supportNegatives)
        {
            return EncodeSignedInteger(value);
        }

        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value),
                "Negative integers are not supported. Enable supportNegatives in constructor.");

        string padded = value.ToString().PadLeft(_numberPadWidth, '0');

        if (padded.Length > _numberPadWidth)
            throw new OverflowException(
                $"Value {value} exceeds the configured number pad width of {_numberPadWidth} digits.");

        return EncodeString(padded);
    }

    private string EncodeSignedInteger(long value)
    {
        string signPrefix;
        string padded;

        if (value >= 0)
        {
            signPrefix = "1";
            padded = value.ToString().PadLeft(_numberPadWidth, '0');
        }
        else
        {
            signPrefix = "0";
            // Nines' complement: map negative values so that -1 > -100 in encoded form
            long maxVal = (long)Math.Pow(10, _numberPadWidth) - 1;
            long complement = maxVal + value + 1; // e.g., -1 → 999999999999, -100 → 999999999900
            padded = complement.ToString().PadLeft(_numberPadWidth, '0');
        }

        if (padded.Length > _numberPadWidth)
            throw new OverflowException(
                $"Value {value} exceeds the configured number pad width of {_numberPadWidth} digits.");

        return EncodeString(signPrefix + padded);
    }

    /// <summary>Decodes an integer ciphertext back to its long value.</summary>
    public long DecodeInteger(string ciphertext)
    {
        string padded = DecodeString(ciphertext);

        if (_supportNegatives)
        {
            char sign = padded[0];
            string numberPart = padded.Substring(1);
            long number = long.Parse(numberPart);

            if (sign == '0')
            {
                // Reverse nines' complement
                long maxVal = (long)Math.Pow(10, _numberPadWidth) - 1;
                return number - maxVal - 1; // complement back to negative
            }

            return number; // positive
        }

        return long.Parse(padded);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Decimal encoding (with negative support)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Encodes a decimal/float value. When supportNegatives is enabled,
    /// negative values are supported with preserved sort order.
    /// </summary>
    public string EncodeDecimal(decimal value, int fractionalWidth = 6)
    {
        if (_supportNegatives)
        {
            return EncodeSignedDecimal(value, fractionalWidth);
        }

        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value),
                "Negative decimals are not supported. Enable supportNegatives in constructor.");

        decimal scale = (decimal)Math.Pow(10, fractionalWidth);
        long scaled = (long)Math.Round(value * scale);

        int totalWidth = _numberPadWidth + fractionalWidth;
        string padded = scaled.ToString().PadLeft(totalWidth, '0');

        return EncodeString(padded);
    }

    private string EncodeSignedDecimal(decimal value, int fractionalWidth)
    {
        decimal scale = (decimal)Math.Pow(10, fractionalWidth);
        long scaled = (long)Math.Round(Math.Abs(value) * scale);
        int totalWidth = _numberPadWidth + fractionalWidth;

        string signPrefix;
        string padded;

        if (value >= 0)
        {
            signPrefix = "1";
            padded = scaled.ToString().PadLeft(totalWidth, '0');
        }
        else
        {
            signPrefix = "0";
            long maxVal = (long)Math.Pow(10, totalWidth) - 1;
            long complement = maxVal - scaled;
            padded = complement.ToString().PadLeft(totalWidth, '0');
        }

        return EncodeString(signPrefix + padded);
    }

    /// <summary>Decodes a decimal ciphertext.</summary>
    public decimal DecodeDecimal(string ciphertext, int fractionalWidth = 6)
    {
        string padded = DecodeString(ciphertext);

        if (_supportNegatives)
        {
            char sign = padded[0];
            string numberPart = padded.Substring(1);
            long number = long.Parse(numberPart);
            decimal scale = (decimal)Math.Pow(10, fractionalWidth);
            int totalWidth = _numberPadWidth + fractionalWidth;

            if (sign == '0')
            {
                long maxVal = (long)Math.Pow(10, totalWidth) - 1;
                long absScaled = maxVal - number;
                return -(absScaled / scale);
            }

            return number / scale;
        }

        long scaled = long.Parse(padded);
        decimal sc = (decimal)Math.Pow(10, fractionalWidth);
        return scaled / sc;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Search helpers
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Returns the encoded prefix for a SQL LIKE '{prefix}%' query.</summary>
    public string EncodePrefix(string plaintextPrefix)
        => EncodeString(plaintextPrefix);

    /// <summary>Returns encoded bounds for a SQL BETWEEN query on a string column.</summary>
    public (string encodedLow, string encodedHigh) EncodeStringRange(
        string lowInclusive, string highInclusive)
        => (EncodeString(lowInclusive), EncodeString(highInclusive));

    /// <summary>Returns encoded bounds for a SQL BETWEEN query on an integer column.</summary>
    public (string encodedLow, string encodedHigh) EncodeIntegerRange(
        long lowInclusive, long highInclusive)
        => (EncodeInteger(lowInclusive), EncodeInteger(highInclusive));

    /// <summary>Returns encoded bounds for a SQL BETWEEN query on a decimal column.</summary>
    public (string encodedLow, string encodedHigh) EncodeDecimalRange(
        decimal lowInclusive, decimal highInclusive, int fractionalWidth = 6)
        => (EncodeDecimal(lowInclusive, fractionalWidth),
            EncodeDecimal(highInclusive, fractionalWidth));

    // ─────────────────────────────────────────────────────────────────────
    // Diagnostics
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Returns the full encoding table for inspection / debugging.</summary>
    public IReadOnlyDictionary<char, string> GetEncodingTableUnsafe()
        => _encodeMap;

    /// <summary>Returns the number of characters in the supported universe.</summary>
    public int UniverseSize => StringUniverse.Length;
}
