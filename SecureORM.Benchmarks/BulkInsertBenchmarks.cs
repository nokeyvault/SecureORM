using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SecureORM.Core.Encoding;

namespace SecureORM.Benchmarks;

// Plain entity without OPE
public class PlainEmployee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long Age { get; set; }
}

public class PlainDbContext : DbContext
{
    public DbSet<PlainEmployee> Employees => Set<PlainEmployee>();
    public PlainDbContext(DbContextOptions<PlainDbContext> options) : base(options) { }
}

[MemoryDiagnoser]
public class BulkInsertBenchmarks
{
    private const int BatchSize = 1000;
    private OPEEncoder _encoder = null!;

    [GlobalSetup]
    public void Setup()
    {
        _encoder = new OPEEncoder("bench-key");
    }

    [Benchmark(Baseline = true)]
    public int InsertWithoutEncoding()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<PlainDbContext>().UseSqlite(conn).Options;
        using var db = new PlainDbContext(options);
        db.Database.EnsureCreated();

        for (int i = 0; i < BatchSize; i++)
            db.Employees.Add(new PlainEmployee { Name = "user" + i, Age = 20 + (i % 50) });

        return db.SaveChanges();
    }

    [Benchmark]
    public int InsertWithEncoding()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<BenchDbContext>().UseSqlite(conn).Options;
        using var db = new BenchDbContext(options, _encoder);
        db.Database.EnsureCreated();

        for (int i = 0; i < BatchSize; i++)
            db.Employees.Add(new BenchEmployee { Name = "user" + i, Age = 20 + (i % 50) });

        return db.SaveChanges();
    }
}
