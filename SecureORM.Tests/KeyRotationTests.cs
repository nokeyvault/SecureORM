using Microsoft.EntityFrameworkCore;
using SecureORM.Core.Encoding;
using SecureORM.EntityFrameworkCore.Attributes;
using SecureORM.EntityFrameworkCore.Extensions;
using SecureORM.EntityFrameworkCore.Migration;
using Xunit;

namespace SecureORM.Tests;

public class RotEntity
{
    public int Id { get; set; }
    [OpeEncoded] public string Name { get; set; } = string.Empty;
    [OpeInteger] public long Age { get; set; }
}

public class RotDbContext : DbContext
{
    private readonly OPEEncoder _encoder;
    public DbSet<RotEntity> Items => Set<RotEntity>();

    public RotDbContext(DbContextOptions<RotDbContext> options, OPEEncoder encoder)
        : base(options) => _encoder = encoder;

    protected override void OnModelCreating(ModelBuilder mb) => mb.ApplyOpeEncodings(_encoder);
}

public class KeyRotationTests : IDisposable
{
    private const string OldKey = "old-key-2024";
    private const string NewKey = "new-key-2025";
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;

    public KeyRotationTests()
    {
        _connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task ReEncodeAll_RotatesKeys()
    {
        var oldEncoder = new OPEEncoder(OldKey);
        var newEncoder = new OPEEncoder(NewKey);

        // Seed with old key
        var oldOptions = new DbContextOptionsBuilder<RotDbContext>()
            .UseSqlite(_connection).Options;

        using (var db = new RotDbContext(oldOptions, oldEncoder))
        {
            db.Database.EnsureCreated();
            db.Items.AddRange(
                new RotEntity { Name = "alice", Age = 30 },
                new RotEntity { Name = "bob", Age = 25 }
            );
            db.SaveChanges();
        }

        // Rotate keys
        using (var db = new RotDbContext(oldOptions, oldEncoder))
        {
            var rotator = new OpeKeyRotator();
            await rotator.ReEncodeAllAsync<RotEntity>(
                db, oldEncoder, newEncoder,
                new Dictionary<string, ColumnEncodingType>
                {
                    ["Name"] = ColumnEncodingType.String,
                    ["Age"] = ColumnEncodingType.Integer
                },
                batchSize: 100);
        }

        // Verify by reading raw encoded values and decoding with new key
        using var readCmd = _connection.CreateCommand();
        readCmd.CommandText = "SELECT Name, Age FROM Items ORDER BY Name";
        using var reader = readCmd.ExecuteReader();

        var results = new List<(string Name, long Age)>();
        while (reader.Read())
        {
            var name = newEncoder.DecodeString(reader.GetString(0));
            var age = newEncoder.DecodeInteger(reader.GetString(1));
            results.Add((name, age));
        }

        Assert.Equal(2, results.Count);
        Assert.Equal("alice", results[0].Name);
        Assert.Equal(30, results[0].Age);
        Assert.Equal("bob", results[1].Name);
        Assert.Equal(25, results[1].Age);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
