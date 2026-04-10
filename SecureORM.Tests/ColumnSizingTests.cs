using SecureORM.Core.Encoding;
using Xunit;

namespace SecureORM.Tests;

public class ColumnSizingTests
{
    [Theory]
    [InlineData(10, 60)]
    [InlineData(50, 300)]
    [InlineData(100, 600)]
    [InlineData(255, 1530)]
    public void EncodedStringLength_Correct(int maxPlaintext, int expectedEncoded)
    {
        Assert.Equal(expectedEncoded, OpeColumnSizing.EncodedStringLength(maxPlaintext));
    }

    [Fact]
    public void EncodedIntegerLength_Default()
    {
        Assert.Equal(72, OpeColumnSizing.EncodedIntegerLength()); // 12 * 6
    }

    [Fact]
    public void EncodedIntegerLength_WithNegatives()
    {
        Assert.Equal(78, OpeColumnSizing.EncodedIntegerLength(supportNegatives: true)); // (12+1) * 6
    }

    [Theory]
    [InlineData(12, 2, false, 84)]  // (12+2) * 6
    [InlineData(12, 6, false, 108)] // (12+6) * 6
    [InlineData(12, 2, true, 90)]   // (12+2+1) * 6
    public void EncodedDecimalLength_Correct(int padWidth, int fractionalWidth, bool negatives, int expected)
    {
        Assert.Equal(expected, OpeColumnSizing.EncodedDecimalLength(padWidth, fractionalWidth, negatives));
    }

    [Fact]
    public void ActualEncodedLength_MatchesPrediction()
    {
        var encoder = new OPEEncoder("test-key");

        var encoded = encoder.EncodeString("hello"); // 5 chars
        Assert.Equal(OpeColumnSizing.EncodedStringLength(5), encoded.Length);

        var encodedInt = encoder.EncodeInteger(42); // padWidth=12
        Assert.Equal(OpeColumnSizing.EncodedIntegerLength(12), encodedInt.Length);
    }
}
