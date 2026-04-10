using Microsoft.EntityFrameworkCore;
using SecureORM.Core.Encoding;
using SecureORM.EntityFrameworkCore.Attributes;
using SecureORM.EntityFrameworkCore.Extensions;
using Xunit;

namespace SecureORM.Tests;

public class TypedEntity
{
    public int Id { get; set; }
    [OpeInteger] public int IntAge { get; set; }
    [OpeInteger] public short ShortRank { get; set; }
    [OpeDecimal(2)] public float FloatScore { get; set; }
    [OpeDecimal(4)] public double DoubleValue { get; set; }
}

public class TypedDbContext : DbContext
{
    private readonly OPEEncoder _encoder;
    public DbSet<TypedEntity> Entities => Set<TypedEntity>();

    public TypedDbContext(DbContextOptions<TypedDbContext> options, OPEEncoder encoder)
        : base(options) => _encoder = encoder;

    protected override void OnModelCreating(ModelBuilder mb) => mb.ApplyOpeEncodings(_encoder);
}

public class AdditionalTypeTests : IDisposable
{
    private readonly OPEEncoder _encoder = new("type-test-key");
    private readonly TypedDbContext _db;

    public AdditionalTypeTests()
    {
        var options = new DbContextOptionsBuilder<TypedDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _db = new TypedDbContext(options, _encoder);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
    }

    [Fact]
    public void Int32_RoundTrip()
    {
        _db.Entities.Add(new TypedEntity { IntAge = 42, ShortRank = 1, FloatScore = 1.0f, DoubleValue = 1.0 });
        _db.SaveChanges();

        var result = _db.Entities.First();
        Assert.Equal(42, result.IntAge);
    }

    [Fact]
    public void Int16_RoundTrip()
    {
        _db.Entities.Add(new TypedEntity { IntAge = 1, ShortRank = 7, FloatScore = 1.0f, DoubleValue = 1.0 });
        _db.SaveChanges();

        var result = _db.Entities.First();
        Assert.Equal(7, result.ShortRank);
    }

    [Fact]
    public void Float_RoundTrip()
    {
        _db.Entities.Add(new TypedEntity { IntAge = 1, ShortRank = 1, FloatScore = 95.75f, DoubleValue = 1.0 });
        _db.SaveChanges();

        var result = _db.Entities.First();
        Assert.Equal(95.75f, result.FloatScore);
    }

    [Fact]
    public void Double_RoundTrip()
    {
        _db.Entities.Add(new TypedEntity { IntAge = 1, ShortRank = 1, FloatScore = 1.0f, DoubleValue = 123.4567 });
        _db.SaveChanges();

        var result = _db.Entities.First();
        Assert.Equal(123.4567, result.DoubleValue, 4);
    }

    [Fact]
    public void Int32_OrderPreservation()
    {
        _db.Entities.AddRange(
            new TypedEntity { IntAge = 30, ShortRank = 1, FloatScore = 1.0f, DoubleValue = 1.0 },
            new TypedEntity { IntAge = 20, ShortRank = 1, FloatScore = 1.0f, DoubleValue = 1.0 },
            new TypedEntity { IntAge = 40, ShortRank = 1, FloatScore = 1.0f, DoubleValue = 1.0 }
        );
        _db.SaveChanges();

        var ordered = _db.Entities.OrderBy(e => e.IntAge).Select(e => e.IntAge).ToList();
        Assert.Equal(new[] { 20, 30, 40 }, ordered);
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }
}
