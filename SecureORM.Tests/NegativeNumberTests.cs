using SecureORM.Core.Encoding;
using Xunit;

namespace SecureORM.Tests;

public class NegativeNumberTests
{
    private readonly OPEEncoder _encoder = new("test-key", supportNegatives: true);

    [Fact]
    public void NegativeInteger_RoundTrip()
    {
        var encoded = _encoder.EncodeInteger(-42);
        var decoded = _encoder.DecodeInteger(encoded);
        Assert.Equal(-42, decoded);
    }

    [Fact]
    public void NegativeInteger_OrderPreservation()
    {
        var encNeg100 = _encoder.EncodeInteger(-100);
        var encNeg1 = _encoder.EncodeInteger(-1);
        var encZero = _encoder.EncodeInteger(0);
        var encPos1 = _encoder.EncodeInteger(1);
        var encPos100 = _encoder.EncodeInteger(100);

        // Lexicographic order should match numeric order
        Assert.True(string.Compare(encNeg100, encNeg1, StringComparison.Ordinal) < 0);
        Assert.True(string.Compare(encNeg1, encZero, StringComparison.Ordinal) < 0);
        Assert.True(string.Compare(encZero, encPos1, StringComparison.Ordinal) < 0);
        Assert.True(string.Compare(encPos1, encPos100, StringComparison.Ordinal) < 0);
    }

    [Fact]
    public void NegativeDecimal_RoundTrip()
    {
        var encoded = _encoder.EncodeDecimal(-99.50m, 2);
        var decoded = _encoder.DecodeDecimal(encoded, 2);
        Assert.Equal(-99.50m, decoded);
    }

    [Fact]
    public void NegativeDecimal_OrderPreservation()
    {
        var encNeg50 = _encoder.EncodeDecimal(-50.25m, 2);
        var encNeg1 = _encoder.EncodeDecimal(-1.00m, 2);
        var encZero = _encoder.EncodeDecimal(0.00m, 2);
        var encPos50 = _encoder.EncodeDecimal(50.25m, 2);

        Assert.True(string.Compare(encNeg50, encNeg1, StringComparison.Ordinal) < 0);
        Assert.True(string.Compare(encNeg1, encZero, StringComparison.Ordinal) < 0);
        Assert.True(string.Compare(encZero, encPos50, StringComparison.Ordinal) < 0);
    }

    [Fact]
    public void NegativeInteger_RangeQuery()
    {
        var (low, high) = _encoder.EncodeIntegerRange(-10, 10);
        Assert.True(string.Compare(low, high, StringComparison.Ordinal) < 0);
    }

    [Fact]
    public void BackwardCompat_PositiveOnlyEncoder_Throws()
    {
        var positiveOnly = new OPEEncoder("test-key", supportNegatives: false);
        Assert.Throws<ArgumentOutOfRangeException>(() => positiveOnly.EncodeInteger(-1));
    }

    [Fact]
    public void SupportsNegatives_Property()
    {
        Assert.True(_encoder.SupportsNegatives);

        var positiveOnly = new OPEEncoder("test-key", supportNegatives: false);
        Assert.False(positiveOnly.SupportsNegatives);
    }
}
