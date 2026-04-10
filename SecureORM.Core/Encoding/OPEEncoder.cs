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

        // Whitespace
        universe.Add(' ');

        // Special / punctuation
        string specials = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";
        universe.AddRange(specials);

        // Digits
        universe.AddRange("0123456789");

        // Uppercase
        universe.AddRange("ABCDEFGHIJKLMNOPQRSTUVWXYZ");

        // Lowercase
        universe.AddRange("abcdefghijklmnopqrstuvwxyz");

        return universe.ToArray();
    }

    private const int CODE_WIDTH = 6;
    private static readonly long MAX_CODE = (long)Math.Pow(10, CODE_WIDTH) - 1;

    public const int DEFAULT_NUMBER_PAD_WIDTH = 12;

    private readonly Dictionary<char, string> _encodeMap;
    private readonly Dictionary<string, char> _decodeMap;
    private readonly int _numberPadWidth;

    public OPEEncoder(string clientKey, int numberPadWidth = DEFAULT_NUMBER_PAD_WIDTH)
    {
        if (string.IsNullOrEmpty(clientKey))
            throw new ArgumentException("Client key must not be null or empty.", nameof(clientKey));

        if (numberPadWidth < 1 || numberPadWidth > 18)
            throw new ArgumentOutOfRangeException(nameof(numberPadWidth),
                "Number pad width must be between 1 and 18.");

        _numberPadWidth = numberPadWidth;

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

    public string EncodeString(string plaintext)
    {
        if (plaintext == null) throw new ArgumentNullException(nameof(plaintext));

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

    public string EncodeInteger(long value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value),
                "Negative integers are not supported.");

        string padded = value.ToString().PadLeft(_numberPadWidth, '0');

        if (padded.Length > _numberPadWidth)
            throw new OverflowException(
                $"Value {value} exceeds the configured number pad width of {_numberPadWidth} digits.");

        return EncodeString(padded);
    }

    public long DecodeInteger(string ciphertext)
    {
        string padded = DecodeString(ciphertext);
        return long.Parse(padded);
    }

    public string EncodeDecimal(decimal value, int fractionalWidth = 6)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value),
                "Negative decimals are not supported.");

        decimal scale = (decimal)Math.Pow(10, fractionalWidth);
        long scaled = (long)Math.Round(value * scale);

        int totalWidth = _numberPadWidth + fractionalWidth;
        string padded = scaled.ToString().PadLeft(totalWidth, '0');

        return EncodeString(padded);
    }

    public decimal DecodeDecimal(string ciphertext, int fractionalWidth = 6)
    {
        string padded = DecodeString(ciphertext);
        long scaled = long.Parse(padded);
        decimal scale = (decimal)Math.Pow(10, fractionalWidth);
        return scaled / scale;
    }

    public string EncodePrefix(string plaintextPrefix)
        => EncodeString(plaintextPrefix);

    public (string encodedLow, string encodedHigh) EncodeStringRange(
        string lowInclusive, string highInclusive)
        => (EncodeString(lowInclusive), EncodeString(highInclusive));

    public (string encodedLow, string encodedHigh) EncodeIntegerRange(
        long lowInclusive, long highInclusive)
        => (EncodeInteger(lowInclusive), EncodeInteger(highInclusive));

    public (string encodedLow, string encodedHigh) EncodeDecimalRange(
        decimal lowInclusive, decimal highInclusive, int fractionalWidth = 6)
        => (EncodeDecimal(lowInclusive, fractionalWidth),
            EncodeDecimal(highInclusive, fractionalWidth));

    public IReadOnlyDictionary<char, string> GetEncodingTableUnsafe()
        => _encodeMap;

    public int UniverseSize => StringUniverse.Length;
}
