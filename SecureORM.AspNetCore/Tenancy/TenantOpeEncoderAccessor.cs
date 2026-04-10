using SecureORM.Core.Encoding;

namespace SecureORM.AspNetCore.Tenancy;

/// <summary>
/// Scoped service that holds the per-request OPEEncoder.
/// Set by the multi-tenant middleware, consumed by DbContext and services.
/// </summary>
public class TenantOpeEncoderAccessor
{
    public OPEEncoder? Encoder { get; set; }
}
