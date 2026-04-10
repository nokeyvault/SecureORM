using Dapper;
using Microsoft.Data.Sqlite;
using SecureORM.Core.Encoding;
using SecureORM.Dapper.Extensions;
using SecureORM.Dapper.Types;
using Xunit;

namespace SecureORM.Tests;

public class DapperEmployee
{
    public int Id { get; set; }
    public OpeString Name { get; set; }
    public OpeInt64 Age { get; set; }
}

public class DapperIntegrationTests : IDisposable
{
    private readonly OPEEncoder _encoder = new("dapper-test-key");
    private readonly SqliteConnection _connection;

    public DapperIntegrationTests()
    {
        DapperSecureOrmExtensions.AddSecureOrmDapper(_encoder);

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _connection.Execute(@"
            CREATE TABLE DapperEmployees (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Age TEXT NOT NULL
            )");
    }

    [Fact]
    public void Insert_And_Query_RoundTrip()
    {
        _connection.Execute(
            "INSERT INTO DapperEmployees (Name, Age) VALUES (@Name, @Age)",
            new { Name = (OpeString)"alice", Age = (OpeInt64)30L });

        var result = _connection.QueryFirst<DapperEmployee>(
            "SELECT * FROM DapperEmployees WHERE Id = 1");

        Assert.Equal("alice", (string)result.Name);
        Assert.Equal(30L, (long)result.Age);
    }

    [Fact]
    public void StoredValues_AreEncoded()
    {
        _connection.Execute(
            "INSERT INTO DapperEmployees (Name, Age) VALUES (@Name, @Age)",
            new { Name = (OpeString)"bob", Age = (OpeInt64)25L });

        // Read raw to verify encoding
        var raw = _connection.QueryFirst<dynamic>(
            "SELECT Name, Age FROM DapperEmployees WHERE Id = 1");

        Assert.NotEqual("bob", (string)raw.Name);
        Assert.NotEqual("25", (string)raw.Age);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
