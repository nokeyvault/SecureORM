# SecureORM

[![NuGet](https://img.shields.io/nuget/v/SecureORM.Core.svg?label=SecureORM.Core)](https://www.nuget.org/packages/SecureORM.Core)
[![NuGet](https://img.shields.io/nuget/v/SecureORM.EntityFrameworkCore.svg?label=SecureORM.EntityFrameworkCore)](https://www.nuget.org/packages/SecureORM.EntityFrameworkCore)
[![NuGet](https://img.shields.io/nuget/v/SecureORM.Dapper.svg?label=SecureORM.Dapper)](https://www.nuget.org/packages/SecureORM.Dapper)
[![NuGet](https://img.shields.io/nuget/v/SecureORM.AspNetCore.svg?label=SecureORM.AspNetCore)](https://www.nuget.org/packages/SecureORM.AspNetCore)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0%2B-blue.svg)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/tests-46%20passing-brightgreen.svg)]()

**Query encrypted data without decryption.** Order-Preserving Encoding extensions for Entity Framework Core, Dapper, and ASP.NET Core.

SecureORM lets you store encoded data in your database that is completely useless without your secret key — while still supporting `ORDER BY`, exact match, range (`BETWEEN`), and prefix (`LIKE`) queries directly on the encoded columns.

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

| Feature | Description |
|---|---|
| **Transparent encoding** | Decorate properties with `[OpeEncoded]`, `[OpeInteger]`, `[OpeDecimal]` — encoding is invisible |
| **Full type support** | `string`, `long`, `int`, `short`, `decimal`, `float`, `double` |
| **Negative numbers** | Opt-in support with preserved sort order across negative/zero/positive |
| **Unicode normalization** | Pluggable normalizer for accented characters, case folding, transliteration |
| **Native LINQ queries** | `OpeStartsWith()` and `OpeInRange()` translate directly to SQL |
| **Column sizing** | Auto-calculate database column sizes with `OpeColumnSizing` helpers |
| **Key rotation** | Batch re-encode all data when rotating encryption keys |
| **Dapper support** | Wrapper types and type handlers for Dapper |
| **Multi-tenant** | ASP.NET Core middleware resolves per-tenant keys from headers, claims, or routes |
| **Benchmarks** | BenchmarkDotNet project for encode/decode throughput and query performance |
| **Per-tenant isolation** | Different client keys produce completely different encodings |
| **Provider-agnostic** | Works with SQL Server, PostgreSQL, SQLite, MySQL, and any EF Core provider |

---

## Packages

```bash
dotnet add package SecureORM.Core
dotnet add package SecureORM.EntityFrameworkCore
dotnet add package SecureORM.Dapper
dotnet add package SecureORM.AspNetCore
```

| Package | Description |
|---|---|
| **SecureORM.Core** | Standalone OPE encoding engine. Zero dependencies. |
| **SecureORM.EntityFrameworkCore** | EF Core integration — attributes, value converters, LINQ translator, DI, key rotation. |
| **SecureORM.Dapper** | Dapper integration — wrapper types, type handlers, query builder. |
| **SecureORM.AspNetCore** | Multi-tenant middleware — per-request key resolution from headers, claims, or routes. |

> `SecureORM.EntityFrameworkCore` and `SecureORM.Dapper` both pull in `SecureORM.Core` automatically.

---

## Quick Start (EF Core)

### 1. Define Your Entity

```csharp
using SecureORM.EntityFrameworkCore.Attributes;

public class User
{
    public int Id { get; set; }

    [OpeEncoded(MaxLength = 100)]    // auto-sizes DB column to 600 chars
    public string Name { get; set; } = string.Empty;

    [OpeInteger]
    public int Age { get; set; }     // int, short, and long all supported

    [OpeDecimal(2)]
    public decimal Salary { get; set; }

    public string InternalNotes { get; set; } = string.Empty;  // not encoded
}
```

### 2. Register Services

```csharp
using SecureORM.EntityFrameworkCore.Extensions;

builder.Services.AddSecureOrm(options =>
{
    options.ClientKey = builder.Configuration["SecureOrm:ClientKey"]!;
    options.NumberPadWidth = 12;
    options.SupportNegatives = true;  // enable negative number encoding
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.UseSecureOrmTranslations(/* encoder injected via DI */);
});
```

### 3. Configure DbContext

```csharp
using SecureORM.Core.Encoding;
using SecureORM.EntityFrameworkCore.Extensions;

public class AppDbContext : DbContext
{
    private readonly OPEEncoder _encoder;
    public DbSet<User> Users => Set<User>();

    public AppDbContext(DbContextOptions<AppDbContext> options, OPEEncoder encoder)
        : base(options) => _encoder = encoder;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyOpeEncodings(_encoder);
    }
}
```

That's it. Your data is now encoded transparently.

---

## Querying Encoded Data

### Exact Match — just works

```csharp
var user = db.Users.FirstOrDefault(u => u.Name == "alice");
var young = db.Users.Where(u => u.Age == 25).ToList();
```

### ORDER BY — just works

```csharp
var sorted = db.Users.OrderBy(u => u.Name).ToList();
var byAge = db.Users.OrderByDescending(u => u.Age).ToList();
```

### Native LINQ — Prefix Search

```csharp
using SecureORM.EntityFrameworkCore.Linq;

// Translates to: WHERE Name LIKE 'encoded_prefix%'
var results = db.Users.Where(u => u.Name.OpeStartsWith("jo")).ToList();
```

### Native LINQ — Range Query

```csharp
using SecureORM.EntityFrameworkCore.Linq;

// Translates to: WHERE Age >= encoded_20 AND Age <= encoded_30
var results = db.Users.Where(u => u.Age.OpeInRange(20, 30)).ToList();
```

### Raw SQL Alternative (works with any provider)

```csharp
var helper = new OpeQueryHelper(encoder);

// Prefix
var pattern = helper.EncodePrefixLike("jo");
var results = db.Users
    .FromSqlRaw("SELECT * FROM Users WHERE Name LIKE {0}", pattern)
    .ToList();

// Range
var (low, high) = helper.EncodeIntegerRange(20, 30);
var results = db.Users
    .FromSqlRaw("SELECT * FROM Users WHERE Age >= {0} AND Age <= {1}", low, high)
    .ToList();
```

---

## Negative Number Support

Enable negative numbers in the encoder to support values like temperatures, account balances, and offsets:

```csharp
builder.Services.AddSecureOrm(options =>
{
    options.ClientKey = "your-key";
    options.SupportNegatives = true;   // enables negative int/decimal encoding
});
```

Sort order is fully preserved across negatives:

```
encoded(-100) < encoded(-1) < encoded(0) < encoded(1) < encoded(100)
```

```csharp
var encoder = new OPEEncoder("key", supportNegatives: true);

encoder.EncodeInteger(-42);         // works
encoder.EncodeDecimal(-99.50m, 2);  // works

var (low, high) = encoder.EncodeIntegerRange(-10, 10);  // range across zero
```

---

## Unicode Normalization

Handle accented characters, case folding, and non-ASCII input:

```csharp
using SecureORM.Core.Normalization;

var normalizer = new UnicodeNormalizer(new UnicodeNormalizerOptions
{
    Transliterate = true,      // cafe with accent to "cafe"
    ToLowerCase = false,       // optional case folding
    ReplacementChar = '?'      // fallback for untransliterable chars (null = throw)
});

builder.Services.AddSecureOrm(options =>
{
    options.ClientKey = "your-key";
    options.Normalizer = normalizer;
});
```

```csharp
var encoder = new OPEEncoder("key", normalizer: normalizer);

encoder.EncodeString("cafe\u0301");   // "cafe" (accent stripped)
encoder.EncodeString("Espa\u00F1a");  // "Espana" (n tilde transliterated)
```

> **Note:** Decoding returns the normalized form, not the original input. Store originals separately if exact round-trip is needed.

---

## Column Sizing

Calculate exact database column sizes for your encoded data:

```csharp
using SecureORM.Core.Encoding;

OpeColumnSizing.EncodedStringLength(100);     // 600 chars
OpeColumnSizing.EncodedIntegerLength(12);     // 72 chars
OpeColumnSizing.EncodedDecimalLength(12, 2);  // 84 chars

// With negative number support (adds 1 sign-prefix character)
OpeColumnSizing.EncodedIntegerLength(12, supportNegatives: true);  // 78 chars
```

Or use the `MaxLength` attribute for automatic column sizing:

```csharp
[OpeEncoded(MaxLength = 100)]   // DB column auto-sized to nvarchar(600)
public string Name { get; set; }
```

---

## Key Rotation

Re-encode all data when rotating encryption keys:

```csharp
using SecureORM.EntityFrameworkCore.Migration;

var oldEncoder = new OPEEncoder("old-key-2024");
var newEncoder = new OPEEncoder("new-key-2025");
var rotator = new OpeKeyRotator();

await rotator.ReEncodeAllAsync<User>(
    dbContext,
    oldEncoder,
    newEncoder,
    new Dictionary<string, ColumnEncodingType>
    {
        ["Name"] = ColumnEncodingType.String,
        ["Age"] = ColumnEncodingType.Integer,
        ["Salary"] = ColumnEncodingType.Decimal2
    },
    batchSize: 1000,
    progress: new Progress<KeyRotationProgress>(p =>
        Console.WriteLine($"Rotated {p.ProcessedCount}/{p.TotalCount} rows ({p.PercentComplete:F0}%)"))
);
```

---

## Dapper Integration

Use SecureORM with Dapper via wrapper types:

```csharp
using SecureORM.Dapper.Types;
using SecureORM.Dapper.Extensions;

// Register handlers at startup
DapperSecureOrmExtensions.AddSecureOrmDapper(encoder, decimalFractionalWidth: 2);

// Define your model with wrapper types
public class Employee
{
    public int Id { get; set; }
    public OpeString Name { get; set; }     // auto-encodes/decodes
    public OpeInt64 Age { get; set; }       // auto-encodes/decodes
}

// Insert — values are encoded automatically
connection.Execute(
    "INSERT INTO Employees (Name, Age) VALUES (@Name, @Age)",
    new { Name = (OpeString)"alice", Age = (OpeInt64)30L });

// Query — values are decoded automatically
var employee = connection.QueryFirst<Employee>("SELECT * FROM Employees WHERE Id = 1");
Console.WriteLine(employee.Name);  // "alice"

// Prefix and range queries
var qb = new OpeQueryBuilder(encoder);
var pattern = qb.EncodePrefixLike("ali");
var results = connection.Query<Employee>(
    "SELECT * FROM Employees WHERE Name LIKE @pattern", new { pattern });
```

---

## Multi-Tenant (ASP.NET Core)

Resolve encryption keys per-tenant from HTTP requests:

```csharp
using SecureORM.AspNetCore.Extensions;
using SecureORM.AspNetCore.Tenancy;

// In Program.cs
builder.Services.AddSecureOrmMultiTenant(options =>
{
    options.NumberPadWidth = 12;
    options.SupportNegatives = true;
    options.DefaultTenantKey = "";   // leave empty to require tenant key
});

// Pick a resolver (or combine multiple)
builder.Services.AddTenantKeyResolver(
    new HeaderTenantKeyResolver("X-Tenant-Key"));    // from header

// Or from JWT claims
builder.Services.AddTenantKeyResolver(
    new ClaimTenantKeyResolver("tenant_key"));       // from auth claim

// Or from route
builder.Services.AddTenantKeyResolver(
    new RouteTenantKeyResolver("tenantId"));         // from route data

// Or combine them (tries each in order)
builder.Services.AddTenantKeyResolver(
    new CompositeTenantKeyResolver(
        new HeaderTenantKeyResolver(),
        new ClaimTenantKeyResolver(),
        new RouteTenantKeyResolver()));

// Add middleware
app.UseAuthentication();
app.UseSecureOrmMultiTenant();   // after auth, before controllers

// In your DbContext or services, inject the per-request encoder:
public class TenantDbContext : DbContext
{
    private readonly TenantOpeEncoderAccessor _accessor;

    public TenantDbContext(
        DbContextOptions options,
        TenantOpeEncoderAccessor accessor) : base(options)
    {
        _accessor = accessor;
    }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        if (_accessor.Encoder != null)
            mb.ApplyOpeEncodings(_accessor.Encoder);
    }
}
```

---

## Fluent API (Alternative to Attributes)

Configure encoding in `OnModelCreating` instead of using attributes:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>(entity =>
    {
        entity.Property(u => u.Name).HasOpeEncoding(_encoder);
        entity.Property(u => u.Age).HasOpeIntegerEncoding(_encoder);      // works with int, short, long
        entity.Property(u => u.Salary).HasOpeDecimalEncoding(_encoder, fractionalWidth: 2);  // works with decimal, float, double
    });
}
```

---

## Standalone Encoder (Without any ORM)

Install just `SecureORM.Core` for the encoding engine alone:

```csharp
using SecureORM.Core.Encoding;

var encoder = new OPEEncoder("your-secret-key", supportNegatives: true);

// Strings
string encoded = encoder.EncodeString("alice");
string decoded = encoder.DecodeString(encoded);  // "alice"

// Integers (including negatives)
encoder.EncodeInteger(42);
encoder.EncodeInteger(-100);

// Decimals
encoder.EncodeDecimal(55000.50m, fractionalWidth: 2);

// Prefix for LIKE queries
string prefix = encoder.EncodePrefix("ali");

// Range bounds for BETWEEN queries
var (low, high) = encoder.EncodeIntegerRange(-10, 50);
```

---

## Supported Data Types

| Attribute | .NET Types | DB Column | Encoding |
|---|---|---|---|
| `[OpeEncoded]` | `string` | `TEXT` / `NVARCHAR` | Each character becomes a 6-digit code |
| `[OpeInteger]` | `long`, `int`, `short` | `TEXT` / `NVARCHAR` | Zero-padded, then character-encoded |
| `[OpeDecimal(n)]` | `decimal`, `float`, `double` | `TEXT` / `NVARCHAR` | Scaled by 10^n, zero-padded, then character-encoded |

### Character Support

The encoder supports 95 printable ASCII characters:
- Space
- Punctuation: `` !"#$%&'()*+,-./:;<=>?@[\]^_`{|}~ ``
- Digits: `0-9`
- Uppercase: `A-Z`
- Lowercase: `a-z`

Use `UnicodeNormalizer` to handle characters outside this set.

### Number Limits

- **Integers:** Up to `numberPadWidth` digits (default 12, max 18). Default range: -999,999,999,999 to 999,999,999,999.
- **Decimals:** Integer part up to `numberPadWidth` digits + `fractionalWidth` decimal places.
- **Negative numbers:** Supported when `supportNegatives: true`. Defaults to off for backward compatibility.

---

## How It Works

### The Encoding Algorithm

1. **Character Universe** — 95 printable ASCII characters, each mapped to a 6-digit numeric code
2. **Order Preservation** — codes assigned in strict ascending order: `'a' < 'b'` implies `code('a') < code('b')`
3. **Key-Derived Offset** — SHA-256 of client key produces a deterministic offset, making encodings unique per key
4. **Number Handling** — integers/decimals are zero-padded to fixed width before encoding
5. **Negative Numbers** — sign prefix (`0` = negative, `1` = positive) + nines' complement preserves order across zero

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

Keep your key in a secure vault (Azure Key Vault, AWS KMS, HashiCorp Vault, etc.), separate from your database.

---

## Project Structure

```
SecureORM/
├── SecureORM.Core/                          # Core encoding engine (zero dependencies)
│   ├── Encoding/
│   │   ├── OPEEncoder.cs                    # The OPE algorithm
│   │   └── OpeColumnSizing.cs               # Column size calculators
│   └── Normalization/
│       ├── IInputNormalizer.cs               # Normalizer interface
│       ├── UnicodeNormalizer.cs              # NFC + ASCII transliteration
│       └── UnicodeNormalizerOptions.cs       # Normalizer config
│
├── SecureORM.EntityFrameworkCore/           # EF Core integration
│   ├── Attributes/                          # [OpeEncoded], [OpeInteger], [OpeDecimal]
│   ├── Converters/                          # ValueConverters for all supported types
│   ├── Configuration/                       # OpeEncodingOptions, OpeEncoderFactory
│   ├── Extensions/                          # DI, ModelBuilder, PropertyBuilder, DbContextOptions
│   ├── Linq/                                # OpeStartsWith/OpeInRange LINQ translator
│   ├── Migration/                           # OpeKeyRotator for key rotation
│   └── OpeQueryHelper.cs                    # Pre-encode helpers for raw SQL queries
│
├── SecureORM.Dapper/                        # Dapper integration
│   ├── Types/                               # OpeString, OpeInt64, OpeDecimalValue wrappers
│   ├── Handlers/                            # SqlMapper.TypeHandler implementations
│   ├── Extensions/                          # AddSecureOrmDapper() registration
│   └── OpeQueryBuilder.cs                   # Dapper query parameter encoder
│
├── SecureORM.AspNetCore/                    # Multi-tenant middleware
│   ├── Tenancy/                             # ITenantKeyResolver + Header/Claim/Route/Composite
│   ├── Middleware/                           # OpeMultiTenantMiddleware
│   ├── Configuration/                       # MultiTenantOpeOptions
│   └── Extensions/                          # AddSecureOrmMultiTenant(), UseSecureOrmMultiTenant()
│
├── SecureORM.Benchmarks/                    # BenchmarkDotNet performance tests
│   ├── EncodeBenchmarks.cs                  # Encode/decode throughput
│   ├── QueryBenchmarks.cs                   # Query performance (encoded vs plaintext)
│   └── BulkInsertBenchmarks.cs              # Bulk insert overhead measurement
│
└── SecureORM.Tests/                         # 46 integration tests (xunit + SQLite)
```

---

## Requirements

- .NET 8.0+
- Entity Framework Core 8.0+ (for `SecureORM.EntityFrameworkCore`)
- Dapper 2.1+ (for `SecureORM.Dapper`)
- ASP.NET Core 8.0+ (for `SecureORM.AspNetCore`)

---

## Running Tests

```bash
dotnet test
```

46 integration tests covering:
- Round-trip encoding/decoding for all 7 data types
- Negative number ordering and range queries
- Unicode normalization and transliteration
- Exact match, ORDER BY, prefix, and range queries via EF Core
- Key rotation with batch processing
- Dapper type handler round-trips
- Column size calculation accuracy
- Cross-key isolation and stored value verification

## Running Benchmarks

```bash
dotnet run -c Release --project SecureORM.Benchmarks
```

---

## Contributing

Contributions are welcome! Here are areas where the community can help:

**Core:**
- Stronger key derivation (HKDF, per-position variation)
- Configurable character universe
- Additional normalization strategies

**EF Core:**
- Provider-specific LINQ translator optimizations (SQL Server, PostgreSQL)
- Nullable property edge cases
- Migration-friendly column type detection

**Ecosystem:**
- NHibernate extensions
- MongoDB integration
- Admin dashboard for key management
- Performance optimization for large batch operations

### Development Setup

```bash
git clone https://github.com/nokeyvault/SecureORM.git
cd SecureORM
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
- Built with [Entity Framework Core](https://github.com/dotnet/efcore) and [Dapper](https://github.com/DapperLib/Dapper)
