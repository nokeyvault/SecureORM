namespace SecureORM.EntityFrameworkCore.Migration;

/// <summary>Reports progress during key rotation.</summary>
public record KeyRotationProgress(int ProcessedCount, int TotalCount)
{
    public double PercentComplete => TotalCount > 0 ? (double)ProcessedCount / TotalCount * 100 : 0;
}
