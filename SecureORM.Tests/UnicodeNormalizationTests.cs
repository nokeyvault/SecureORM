using SecureORM.Core.Encoding;
using SecureORM.Core.Normalization;
using Xunit;

namespace SecureORM.Tests;

public class UnicodeNormalizationTests
{
    [Fact]
    public void AccentedChars_Transliterated()
    {
        var normalizer = new UnicodeNormalizer();
        var encoder = new OPEEncoder("test-key", normalizer: normalizer);

        // "cafe" with accent should encode without throwing
        var encoded = encoder.EncodeString("caf\u00E9"); // café
        var decoded = encoder.DecodeString(encoded);

        Assert.Equal("cafe", decoded); // é transliterated to e
    }

    [Fact]
    public void NTilde_Transliterated()
    {
        var normalizer = new UnicodeNormalizer();

        var result = normalizer.Normalize("Espa\u00F1a"); // España
        Assert.Equal("Espana", result);
    }

    [Fact]
    public void LowercaseOption()
    {
        var normalizer = new UnicodeNormalizer(new UnicodeNormalizerOptions { ToLowerCase = true });
        var encoder = new OPEEncoder("test-key", normalizer: normalizer);

        var encoded = encoder.EncodeString("HELLO");
        var decoded = encoder.DecodeString(encoded);

        Assert.Equal("hello", decoded);
    }

    [Fact]
    public void ReplacementChar_ForUnknown()
    {
        var normalizer = new UnicodeNormalizer(new UnicodeNormalizerOptions
        {
            ReplacementChar = '?'
        });

        // Chinese character — can't transliterate to ASCII
        var result = normalizer.Normalize("hello\u4E16"); // 世
        Assert.Equal("hello?", result);
    }

    [Fact]
    public void NoReplacementChar_Throws()
    {
        var normalizer = new UnicodeNormalizer(new UnicodeNormalizerOptions
        {
            ReplacementChar = null
        });

        Assert.Throws<ArgumentException>(() => normalizer.Normalize("hello\u4E16"));
    }

    [Fact]
    public void AsciiInput_PassesThrough()
    {
        var normalizer = new UnicodeNormalizer();
        var result = normalizer.Normalize("hello world 123");
        Assert.Equal("hello world 123", result);
    }

    [Fact]
    public void WithoutNormalizer_ThrowsOnAccent()
    {
        var encoder = new OPEEncoder("test-key");
        Assert.Throws<ArgumentException>(() => encoder.EncodeString("caf\u00E9"));
    }
}
