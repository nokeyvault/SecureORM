using BenchmarkDotNet.Attributes;
using SecureORM.Core.Encoding;

namespace SecureORM.Benchmarks;

[MemoryDiagnoser]
public class EncodeBenchmarks
{
    private OPEEncoder _encoder = null!;
    private string _short = null!;
    private string _medium = null!;
    private string _long = null!;
    private string _encodedShort = null!;
    private string _encodedMedium = null!;
    private string _encodedLong = null!;

    [GlobalSetup]
    public void Setup()
    {
        _encoder = new OPEEncoder("benchmark-key-2024");
        _short = "hello";                                              // 5 chars
        _medium = new string('a', 50);                                 // 50 chars
        _long = new string('z', 200);                                  // 200 chars
        _encodedShort = _encoder.EncodeString(_short);
        _encodedMedium = _encoder.EncodeString(_medium);
        _encodedLong = _encoder.EncodeString(_long);
    }

    [Benchmark] public string EncodeString_5chars() => _encoder.EncodeString(_short);
    [Benchmark] public string EncodeString_50chars() => _encoder.EncodeString(_medium);
    [Benchmark] public string EncodeString_200chars() => _encoder.EncodeString(_long);

    [Benchmark] public string DecodeString_5chars() => _encoder.DecodeString(_encodedShort);
    [Benchmark] public string DecodeString_50chars() => _encoder.DecodeString(_encodedMedium);
    [Benchmark] public string DecodeString_200chars() => _encoder.DecodeString(_encodedLong);

    [Benchmark] public string EncodeInteger() => _encoder.EncodeInteger(123456789);
    [Benchmark] public long DecodeInteger() => _encoder.DecodeInteger(_encoder.EncodeInteger(123456789));

    [Benchmark] public string EncodeDecimal() => _encoder.EncodeDecimal(12345.67m, 2);
    [Benchmark] public decimal DecodeDecimal() => _encoder.DecodeDecimal(_encoder.EncodeDecimal(12345.67m, 2), 2);
}
