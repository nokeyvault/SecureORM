using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SecureORM.Core.Encoding;
using SecureORM.EntityFrameworkCore.Attributes;
using SecureORM.EntityFrameworkCore.Extensions;

namespace SecureORM.Benchmarks;

public class BenchEmployee
{
    public int Id { get; set; }
    [OpeEncoded] public string Name { get; set; } = string.Empty;
    [OpeInteger] public long Age { get; set; }
}

public class BenchDbContext : DbContext
{
    private readonly OPEEncoder _encoder;
    public DbSet<BenchEmployee> Employees => Set<BenchEmployee>();

    public BenchDbContext(DbContextOptions<BenchDbContext> options, OPEEncoder encoder)
        : base(options) => _encoder = encoder;

    protected override void OnModelCreating(ModelBuilder mb) => mb.ApplyOpeEncodings(_encoder);
}

[MemoryDiagnoser]
public class QueryBenchmarks
{
    private BenchDbContext _db = null!;
    private OPEEncoder _encoder = null!;
    private SqliteConnection _connection = null!;

    [Params(1000, 10000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _encoder = new OPEEncoder("bench-key");
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<BenchDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new BenchDbContext(options, _encoder);
        _db.Database.EnsureCreated();

        // Seed data
        var names = new[] { "alice", "bob", "charlie", "diana", "eve", "frank", "grace", "henry" };
        var random = new Random(42);

        for (int i = 0; i < RowCount; i++)
        {
            _db.Employees.Add(new BenchEmployee
            {
                Name = names[random.Next(names.Length)] + i,
                Age = random.Next(18, 65)
            });
        }
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }

    [Benchmark]
    public int ExactMatch() => _db.Employees.Count(e => e.Name == "alice50");

    [Benchmark]
    public List<BenchEmployee> OrderBy() => _db.Employees.OrderBy(e => e.Name).Take(10).ToList();

    [Benchmark]
    public List<BenchEmployee> RangeQuery()
    {
        var (low, high) = _encoder.EncodeIntegerRange(25, 40);
        return _db.Employees
            .FromSqlRaw("SELECT * FROM Employees WHERE Age >= {0} AND Age <= {1} LIMIT 100", low, high)
            .ToList();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
