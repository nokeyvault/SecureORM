using Microsoft.EntityFrameworkCore;
using SecureORM.Core.Encoding;
using SecureORM.EntityFrameworkCore;
using SecureORM.EntityFrameworkCore.Attributes;
using SecureORM.EntityFrameworkCore.Extensions;
using Xunit;

namespace SecureORM.Tests;

// ─── Test Entity ───────────────────────────────────────────────────
public class Employee
{
    public int Id { get; set; }

    [OpeEncoded]
    public string Name { get; set; } = string.Empty;

    [OpeInteger]
    public long Age { get; set; }

    [OpeDecimal(2)]
    public decimal Salary { get; set; }
}

// ─── Test DbContext ────────────────────────────────────────────────
public class TestDbContext : DbContext
{
    private readonly OPEEncoder _encoder;

    public DbSet<Employee> Employees => Set<Employee>();

    public TestDbContext(DbContextOptions<TestDbContext> options, OPEEncoder encoder)
        : base(options)
    {
        _encoder = encoder;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyOpeEncodings(_encoder);
    }
}

// ─── Tests ─────────────────────────────────────────────────────────
public class OpeIntegrationTests : IDisposable
{
    private const string ClientKey = "test-client-key-2024";
    private readonly OPEEncoder _encoder;
    private readonly OpeQueryHelper _queryHelper;
    private readonly TestDbContext _db;

    public OpeIntegrationTests()
    {
        _encoder = new OPEEncoder(ClientKey);
        _queryHelper = new OpeQueryHelper(_encoder);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _db = new TestDbContext(options, _encoder);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();

        SeedData();
    }

    private void SeedData()
    {
        _db.Employees.AddRange(
            new Employee { Name = "alice", Age = 30, Salary = 55000.50m },
            new Employee { Name = "bob", Age = 25, Salary = 62000.00m },
            new Employee { Name = "charlie", Age = 35, Salary = 48000.75m },
            new Employee { Name = "diana", Age = 28, Salary = 71000.25m },
            new Employee { Name = "eve", Age = 32, Salary = 59500.00m }
        );
        _db.SaveChanges();
    }

    [Fact]
    public void RoundTrip_StringProperty_DecodesCorrectly()
    {
        var employees = _db.Employees.ToList();

        var names = employees.Select(e => e.Name).OrderBy(n => n).ToList();
        Assert.Equal(new[] { "alice", "bob", "charlie", "diana", "eve" }, names);
    }

    [Fact]
    public void RoundTrip_IntegerProperty_DecodesCorrectly()
    {
        var bob = _db.Employees.First(e => e.Name == "bob");
        Assert.Equal(25, bob.Age);
    }

    [Fact]
    public void RoundTrip_DecimalProperty_DecodesCorrectly()
    {
        var alice = _db.Employees.First(e => e.Name == "alice");
        Assert.Equal(55000.50m, alice.Salary);
    }

    [Fact]
    public void ExactMatch_StringProperty_FindsCorrectEntity()
    {
        // EF Core auto-encodes "charlie" via the value converter
        var result = _db.Employees.FirstOrDefault(e => e.Name == "charlie");

        Assert.NotNull(result);
        Assert.Equal("charlie", result.Name);
        Assert.Equal(35, result.Age);
    }

    [Fact]
    public void ExactMatch_IntegerProperty_FindsCorrectEntity()
    {
        var result = _db.Employees.FirstOrDefault(e => e.Age == 28);

        Assert.NotNull(result);
        Assert.Equal("diana", result.Name);
    }

    [Fact]
    public void OrderBy_StringProperty_PreservesOrder()
    {
        // ORDER BY on encoded column should produce correct lexicographic order
        var ordered = _db.Employees
            .OrderBy(e => e.Name)
            .Select(e => e.Name)
            .ToList();

        Assert.Equal(new[] { "alice", "bob", "charlie", "diana", "eve" }, ordered);
    }

    [Fact]
    public void OrderBy_IntegerProperty_PreservesNumericOrder()
    {
        var ordered = _db.Employees
            .OrderBy(e => e.Age)
            .Select(e => e.Age)
            .ToList();

        Assert.Equal(new long[] { 25, 28, 30, 32, 35 }, ordered);
    }

    [Fact]
    public void OrderBy_DecimalProperty_PreservesNumericOrder()
    {
        var ordered = _db.Employees
            .OrderBy(e => e.Salary)
            .Select(e => e.Salary)
            .ToList();

        Assert.Equal(
            new[] { 48000.75m, 55000.50m, 59500.00m, 62000.00m, 71000.25m },
            ordered);
    }

    [Fact]
    public void PrefixSearch_EncodedColumn_FindsMatches()
    {
        var prefix = _queryHelper.EncodePrefix("ch");
        var likePattern = prefix + "%";

        var results = _db.Employees
            .FromSqlRaw("SELECT * FROM Employees WHERE Name LIKE {0}", likePattern)
            .ToList();

        Assert.Single(results);
        Assert.Equal("charlie", results[0].Name);
    }

    [Fact]
    public void RangeQuery_IntegerColumn_FindsMatchesInRange()
    {
        // Find employees aged 28-32 using raw SQL for range on encoded column
        var (low, high) = _queryHelper.EncodeIntegerRange(28, 32);

        var results = _db.Employees
            .FromSqlRaw("SELECT * FROM Employees WHERE Age >= {0} AND Age <= {1}", low, high)
            .OrderBy(e => e.Age)
            .ToList();

        Assert.Equal(3, results.Count);
        Assert.Equal(new long[] { 28, 30, 32 }, results.Select(r => r.Age).ToArray());
    }

    [Fact]
    public void RangeQuery_DecimalColumn_FindsMatchesInRange()
    {
        // Find employees with salary between 50k-65k
        var (low, high) = _queryHelper.EncodeDecimalRange(50000.00m, 65000.00m, 2);

        var results = _db.Employees
            .FromSqlRaw("SELECT * FROM Employees WHERE Salary >= {0} AND Salary <= {1}", low, high)
            .OrderBy(e => e.Salary)
            .ToList();

        Assert.Equal(3, results.Count);
        Assert.Equal(
            new[] { 55000.50m, 59500.00m, 62000.00m },
            results.Select(r => r.Salary).ToArray());
    }

    [Fact]
    public void StoredValues_AreEncoded_NotPlaintext()
    {
        // Verify that the database stores encoded values, not plaintext
        using var cmd = _db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = "SELECT Name, Age, Salary FROM Employees WHERE Id = 1";
        using var reader = cmd.ExecuteReader();
        reader.Read();

        var storedName = reader.GetString(0);
        var storedAge = reader.GetString(1);
        var storedSalary = reader.GetString(2);

        // Stored values should be numeric digit strings, not "alice", "30", "55000.50"
        Assert.NotEqual("alice", storedName);
        Assert.NotEqual("30", storedAge);
        Assert.All(storedName, c => Assert.True(char.IsDigit(c)));
        Assert.All(storedAge, c => Assert.True(char.IsDigit(c)));
        Assert.All(storedSalary, c => Assert.True(char.IsDigit(c)));
    }

    [Fact]
    public void DifferentKeys_ProduceDifferentEncodings()
    {
        var encoder2 = new OPEEncoder("different-key-999");

        var encoded1 = _encoder.EncodeString("hello");
        var encoded2 = encoder2.EncodeString("hello");

        Assert.NotEqual(encoded1, encoded2);
    }

    [Fact]
    public void Update_Entity_RoundTripsCorrectly()
    {
        var bob = _db.Employees.First(e => e.Name == "bob");
        bob.Age = 26;
        bob.Salary = 65000.00m;
        _db.SaveChanges();

        // Re-query to verify
        var updated = _db.Employees.First(e => e.Name == "bob");
        Assert.Equal(26, updated.Age);
        Assert.Equal(65000.00m, updated.Salary);
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }
}
