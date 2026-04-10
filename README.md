# SecureORM

**Order-Preserving Encoding (OPE) extensions for Entity Framework Core.**

SecureORM lets you store encoded data in your database that is completely useless without your secret key — while still supporting `ORDER BY`, exact match, range (`BETWEEN`), and prefix (`LIKE`) queries directly on the encoded columns. No decryption needed at the database layer.

```
  Your App                    Database
  ────────                    ────────
  "alice"        ──────►      "583291742016618241"
  "bob"          ──────►      "583291673450618241"
  Age: 30        ──────►      "531108531108531108..."

  ORDER BY name  ──────►      ORDER BY name  ✅  (same result)
  WHERE age > 25 ──────►      WHERE age > "encoded_25"  ✅
```

An attacker with database access sees fixed-width numeric tokens. Without your client key, they **cannot** reconstruct the original values.

---

## Why SecureORM?

Most encryption schemes (AES, etc.) destroy the ability to query data. You encrypt a name, and now you can't sort by it, search by prefix, or do range queries without decrypting everything first.

**SecureORM solves this.** It uses Order-Preserving Encoding so that:

- `plaintext_a < plaintext_b` implies `encoded_a < encoded_b`
- The database engine can evaluate `ORDER BY`, `>`, `<`, `BETWEEN`, `LIKE 'prefix%'` on encoded values
- Your application decodes transparently — developers write normal LINQ, and the encoding is invisible

**Trade-off:** OPE leaks ordering information by design. This is a known, documented trade-off in academic literature (Boldyreva et al.). If you need zero-leakage encryption, use AES-GCM — but you lose queryability. SecureORM is for scenarios where ordering leakage is acceptable but plaintext exposure is not.

---

## Features

- **Transparent encoding** — decorate properties with `[OpeEncoded]`, `[OpeInteger]`, or `[OpeDecimal]` and forget about it
- **EF Core Value Converters** — automatic encode-on-write, decode-on-read
- **Query support** — exact match, ORDER BY, prefix search, range queries all work on encoded data
- **Per-tenant isolation** — different client keys produce completely different encodings
- **Deterministic** — same key + same input = same output (enables exact match queries)
- **Type support** — strings, integers (`long`), and decimals
- **Fluent API + Attributes** — configure via attributes or `ModelBuilder` fluent API
- **DI-friendly** — one-line setup with `AddSecureOrm()`
- **Provider-agnostic** — works with SQL Server, PostgreSQL, SQLite, and any EF Core provider

---

## Getting Started

### 1. Install

```bash
# From source (until NuGet package is published)
dotnet add reference path/to/SecureORM.Core.csproj
dotnet add reference path/to/SecureORM.EntityFrameworkCore.csproj
```

### 2. Define Your Entity

```csharp
using SecureORM.EntityFrameworkCore.Attributes;

public class User
{
    public int Id { get; set; }

    [OpeEncoded]
    public string Name { get; set; } = string.Empty;

    [OpeInteger]
    public long Age { get; set; }

    [OpeDecimal(2)]  // 2 decimal places
    public decimal Salary { get; set; }

    // Properties without attributes are stored as-is
    public string InternalNotes { get; set; } = string.Empty;
}
```

### 3. Register Services

```csharp
// Program.cs or Startup.cs
using SecureORM.EntityFrameworkCore.Extensions;

builder.Services.AddSecureOrm(options =>
{
    options.ClientKey = builder.Configuration["SecureOrm:ClientKey"]!;
    options.NumberPadWidth = 12; // supports integers up to 12 digits
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));  // or UseSqlite, UseNpgsql, etc.
```

### 4. Configure DbContext

```csharp
using SecureORM.Core.Encoding;
using SecureORM.EntityFrameworkCore.Extensions;

public class AppDbContext : DbContext
{
    private readonly OPEEncoder _encoder;

    public DbSet<User> Users => Set<User>();

    public AppDbContext(DbContextOptions<AppDbContext> options, OPEEncoder encoder)
        : base(options)
    {
        _encoder = encoder;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyOpeEncodings(_encoder);
    }
}
```

That's it. Your data is now encoded transparently.

---

## Querying Encoded Data

### Exact Match (just works)

EF Core auto-encodes the comparison value through the value converter:

```csharp
// Natural LINQ — no special handling needed
var user = db.Users.FirstOrDefault(u => u.Name == "alice");
var young = db.Users.Where(u => u.Age == 25).ToList();
```

### ORDER BY (just works)

Encoded values sort in the same order as the originals:

```csharp
// Lexicographic order on encoded column = correct order
var sorted = db.Users.OrderBy(u => u.Name).ToList();
var byAge = db.Users.OrderByDescending(u => u.Age).ToList();
```

### Prefix Search

Use `OpeQueryHelper` to pre-encode the prefix, then query with raw SQL:

```csharp
public class UserService
{
    private readonly AppDbContext _db;
    private readonly OpeQueryHelper _query;

    public UserService(AppDbContext db, OpeQueryHelper query)
    {
        _db = db;
        _query = query;
    }

    public List<User> SearchByNamePrefix(string prefix)
    {
        var pattern = _query.EncodePrefixLike(prefix); // "encoded_prefix%"

        return _db.Users
            .FromSqlRaw("SELECT * FROM Users WHERE Name LIKE {0}", pattern)
            .ToList();
    }
}
```

### Range Query (BETWEEN)

```csharp
public List<User> GetByAgeRange(long minAge, long maxAge)
{
    var (low, high) = _query.EncodeIntegerRange(minAge, maxAge);

    return _db.Users
        .FromSqlRaw("SELECT * FROM Users WHERE Age >= {0} AND Age <= {1}", low, high)
        .OrderBy(u => u.Age)
        .ToList();
}

public List<User> GetBySalaryRange(decimal minSalary, decimal maxSalary)
{
    var (low, high) = _query.EncodeDecimalRange(minSalary, maxSalary, fractionalWidth: 2);

    return _db.Users
        .FromSqlRaw("SELECT * FROM Users WHERE Salary >= {0} AND Salary <= {1}", low, high)
        .OrderBy(u => u.Salary)
        .ToList();
}
```

---

## Fluent API (Alternative to Attributes)

If you prefer configuring via `OnModelCreating` instead of attributes:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>(entity =>
    {
        entity.Property(u => u.Name).HasOpeEncoding(_encoder);
        entity.Property(u => u.Age).HasOpeIntegerEncoding(_encoder);
        entity.Property(u => u.Salary).HasOpeDecimalEncoding(_encoder, fractionalWidth: 2);
    });
}
```

---

## How It Works

### The Encoding Algorithm

1. **Character Universe** — 95 printable ASCII characters (space, punctuation, digits, A-Z, a-z), each mapped to a 6-digit numeric code
2. **Order Preservation** — codes are assigned in strict ascending order, so `'a' < 'b'` implies `code('a') < code('b')`
3. **Key-Derived Offset** — SHA-256 of your client key produces a deterministic offset that shifts all codes, making encodings unique per key
4. **Number Handling** — integers and decimals are zero-padded to fixed width before encoding, so numeric order equals lexicographic order

### What the Database Sees

```
┌─────────────────────────────────────────────────────┐
│ Your Application         │  Database Column          │
├──────────────────────────┼───────────────────────────┤
│ "alice"                  │  "583291742016618241..."   │
│ "bob"                    │  "583291673450618241..."   │
│ Age: 30                  │  "531108531108531108..."   │
│ Salary: 55000.50         │  "531108531108476974..."   │
└──────────────────────────┴───────────────────────────┘

Without the client key, these are just meaningless digit strings.
```

### Security Model

| What an attacker has | What they can learn |
|---|---|
| Database access only | Nothing — just numeric tokens |
| Database + knowledge of OPE scheme | Relative ordering of values (inherent to any OPE) |
| Database + client key | Everything — **protect your keys** |

**Key point:** The encoded data is useless unless the attacker gets both the key and the data at the same time. Keep your key in a secure vault (Azure Key Vault, AWS KMS, HashiCorp Vault, etc.), separate from your database.

---

## Project Structure

```
src/
├── SecureORM.Core/                          # Core encoding engine (no EF dependency)
│   └── Encoding/
│       └── OPEEncoder.cs                    # The OPE algorithm
│
├── SecureORM.EntityFrameworkCore/           # EF Core integration
│   ├── Attributes/
│   │   ├── OpeEncodedAttribute.cs           # [OpeEncoded] for strings
│   │   ├── OpeIntegerAttribute.cs           # [OpeInteger] for long/int
│   │   └── OpeDecimalAttribute.cs           # [OpeDecimal(fw)] for decimals
│   ├── Converters/
│   │   ├── OpeStringValueConverter.cs       # ValueConverter<string, string>
│   │   ├── OpeIntegerValueConverter.cs      # ValueConverter<long, string>
│   │   └── OpeDecimalValueConverter.cs      # ValueConverter<decimal, string>
│   ├── Configuration/
│   │   ├── OpeEncodingOptions.cs            # ClientKey, NumberPadWidth config
│   │   └── OpeEncoderFactory.cs             # Singleton factory for OPEEncoder
│   ├── Extensions/
│   │   ├── ServiceCollectionExtensions.cs   # AddSecureOrm() for DI
│   │   ├── ModelBuilderExtensions.cs        # ApplyOpeEncodings() attribute scanner
│   │   └── PropertyBuilderExtensions.cs     # HasOpeEncoding() fluent API
│   └── OpeQueryHelper.cs                   # Pre-encode helpers for queries
│
└── SecureORM.Tests/                         # Integration tests
    └── OpeIntegrationTests.cs               # 14 tests with SQLite in-memory
```

---

## Supported Data Types

| Attribute | .NET Type | DB Column | Encoding |
|---|---|---|---|
| `[OpeEncoded]` | `string` | `TEXT` / `NVARCHAR` | Each character becomes a 6-digit code |
| `[OpeInteger]` | `long` | `TEXT` / `NVARCHAR` | Zero-padded, then character-encoded |
| `[OpeDecimal(n)]` | `decimal` | `TEXT` / `NVARCHAR` | Scaled by 10^n, zero-padded, then character-encoded |

### Character Support

The encoder supports 95 printable ASCII characters:
- Space
- Punctuation: `!"#$%&'()*+,-./:;<=>?@[\]^_`{|}~`
- Digits: `0-9`
- Uppercase: `A-Z`
- Lowercase: `a-z`

Characters outside this set (Unicode, accented characters, etc.) will throw at encoding time. Normalize your input before encoding.

### Number Limits

- **Integers:** Up to `numberPadWidth` digits (default 12, max 18). Supports 0 to 999,999,999,999 with default settings.
- **Decimals:** Integer part up to `numberPadWidth` digits + `fractionalWidth` decimal places.
- **Negative numbers:** Not supported in the current version. Apply a domain shift (e.g., `value + offset`) before encoding if needed.

---

## Requirements

- .NET 8.0+
- Entity Framework Core 8.0+
- Any EF Core database provider (SQL Server, PostgreSQL, SQLite, MySQL, etc.)

---

## Running Tests

```bash
cd src
dotnet test
```

The test suite includes 14 integration tests using SQLite in-memory:

- Round-trip encoding/decoding for all types
- Exact match queries via LINQ
- ORDER BY correctness for strings, integers, and decimals
- Prefix search with LIKE
- Range queries with BETWEEN
- Verification that stored values are encoded (not plaintext)
- Cross-key isolation
- Update operations

---

## Roadmap

- [ ] NuGet package publishing
- [ ] Negative number support
- [ ] Unicode / normalization layer
- [ ] Custom `IMethodCallTranslator` for native LINQ prefix/range queries (no raw SQL needed)
- [ ] Column size recommendations based on max input length
- [ ] Key rotation support
- [ ] Benchmarks and performance profiling
- [ ] Additional ORM support (Dapper extensions)

---

## Contributing

Contributions are welcome. Here are some areas where the community can help:

**Core improvements:**
- Stronger key derivation (HKDF, per-position variation instead of global offset)
- Negative number support
- Unicode normalization layer
- Configurable character universe

**EF Core integration:**
- Custom `IMethodCallTranslator` plugins for SQL Server, PostgreSQL, and SQLite so that prefix and range queries work with native LINQ instead of `FromSqlRaw`
- Support for `int`, `short`, `float`, `double` property types
- Nullable property handling improvements
- Migration-friendly column type detection

**Ecosystem:**
- Dapper extensions
- NHibernate extensions
- ASP.NET Core middleware for automatic key injection from request context (multi-tenant)
- Benchmarks comparing query performance on encoded vs. plaintext columns

### Development Setup

```bash
git clone https://github.com/your-org/SecureORM.git
cd SecureORM/src
dotnet restore
dotnet build
dotnet test
```

---

## License

[MIT](LICENSE)

---

## Acknowledgments

- Order-Preserving Encryption concept based on research by [Boldyreva et al.](https://link.springer.com/chapter/10.1007/978-3-642-01001-9_13)
- Built with [Entity Framework Core](https://github.com/dotnet/efcore)
