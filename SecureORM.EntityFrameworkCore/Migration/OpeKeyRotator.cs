using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using SecureORM.Core.Encoding;

namespace SecureORM.EntityFrameworkCore.Migration;

/// <summary>
/// Re-encodes all OPE-encoded data from one key to another.
/// Operates in batches within a transaction for safety.
/// </summary>
public class OpeKeyRotator
{
    /// <summary>
    /// Re-encodes all rows in a table, decoding with the old encoder
    /// and re-encoding with the new encoder.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="context">The DbContext with an open connection.</param>
    /// <param name="oldEncoder">Encoder initialized with the old key.</param>
    /// <param name="newEncoder">Encoder initialized with the new key.</param>
    /// <param name="columnMappings">
    /// Column names and their encoding types. Key = column name, Value = encoding type.
    /// </param>
    /// <param name="batchSize">Number of rows per batch (default 1000).</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ReEncodeAllAsync<TEntity>(
        DbContext context,
        OPEEncoder oldEncoder,
        OPEEncoder newEncoder,
        Dictionary<string, ColumnEncodingType> columnMappings,
        int batchSize = 1000,
        IProgress<KeyRotationProgress>? progress = null,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} not found in model.");

        var tableName = entityType.GetTableName()
            ?? throw new InvalidOperationException($"Could not determine table name for {typeof(TEntity).Name}.");

        var keyProperty = entityType.FindPrimaryKey()?.Properties.FirstOrDefault()
            ?? throw new InvalidOperationException($"Entity {typeof(TEntity).Name} has no primary key.");

        var keyColumn = keyProperty.GetColumnName();
        var columns = string.Join(", ", new[] { keyColumn }.Concat(columnMappings.Keys));

        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        // Count total rows
        int totalCount;
        using (var countCmd = connection.CreateCommand())
        {
            countCmd.CommandText = $"SELECT COUNT(*) FROM {tableName}";
            countCmd.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
            totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));
        }

        if (totalCount == 0) return;

        await using var transaction = context.Database.CurrentTransaction == null
            ? await context.Database.BeginTransactionAsync(cancellationToken)
            : null;

        int processed = 0;
        var lastKey = (object?)null;

        while (processed < totalCount)
        {
            // Read batch
            using var readCmd = connection.CreateCommand();
            readCmd.Transaction = transaction?.GetDbTransaction()
                ?? context.Database.CurrentTransaction?.GetDbTransaction();

            if (lastKey == null)
            {
                readCmd.CommandText = $"SELECT {columns} FROM {tableName} ORDER BY {keyColumn} LIMIT {batchSize}";
            }
            else
            {
                readCmd.CommandText = $"SELECT {columns} FROM {tableName} WHERE {keyColumn} > @lastKey ORDER BY {keyColumn} LIMIT {batchSize}";
                var param = readCmd.CreateParameter();
                param.ParameterName = "@lastKey";
                param.Value = lastKey;
                readCmd.Parameters.Add(param);
            }

            var rows = new List<Dictionary<string, string>>();
            var keys = new List<object>();

            using (var reader = await readCmd.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var keyValue = reader[keyColumn];
                    keys.Add(keyValue);

                    var row = new Dictionary<string, string>();
                    foreach (var col in columnMappings.Keys)
                    {
                        var rawValue = reader[col];
                        if (rawValue != DBNull.Value)
                            row[col] = rawValue.ToString()!;
                    }
                    rows.Add(row);
                }
            }

            if (rows.Count == 0) break;
            lastKey = keys.Last();

            // Re-encode and update each row
            for (int i = 0; i < rows.Count; i++)
            {
                using var updateCmd = connection.CreateCommand();
                updateCmd.Transaction = transaction?.GetDbTransaction()
                    ?? context.Database.CurrentTransaction?.GetDbTransaction();

                var setClauses = new List<string>();
                foreach (var (colName, encodingType) in columnMappings)
                {
                    if (!rows[i].ContainsKey(colName)) continue;

                    string oldEncoded = rows[i][colName];
                    string newEncoded = ReEncode(oldEncoder, newEncoder, oldEncoded, encodingType);

                    var paramName = $"@p_{colName}";
                    setClauses.Add($"{colName} = {paramName}");
                    var p = updateCmd.CreateParameter();
                    p.ParameterName = paramName;
                    p.Value = newEncoded;
                    updateCmd.Parameters.Add(p);
                }

                var keyParam = updateCmd.CreateParameter();
                keyParam.ParameterName = "@keyVal";
                keyParam.Value = keys[i];
                updateCmd.Parameters.Add(keyParam);

                updateCmd.CommandText = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE {keyColumn} = @keyVal";
                await updateCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            processed += rows.Count;
            progress?.Report(new KeyRotationProgress(processed, totalCount));
        }

        if (transaction != null)
            await transaction.CommitAsync(cancellationToken);
    }

    private static string ReEncode(
        OPEEncoder oldEncoder, OPEEncoder newEncoder,
        string oldEncoded, ColumnEncodingType encodingType)
    {
        return encodingType switch
        {
            ColumnEncodingType.String => newEncoder.EncodeString(oldEncoder.DecodeString(oldEncoded)),
            ColumnEncodingType.Integer => newEncoder.EncodeInteger(oldEncoder.DecodeInteger(oldEncoded)),
            ColumnEncodingType.Decimal2 => newEncoder.EncodeDecimal(oldEncoder.DecodeDecimal(oldEncoded, 2), 2),
            ColumnEncodingType.Decimal6 => newEncoder.EncodeDecimal(oldEncoder.DecodeDecimal(oldEncoded, 6), 6),
            _ => throw new ArgumentException($"Unknown encoding type: {encodingType}")
        };
    }
}

/// <summary>Encoding type for a column, used during key rotation.</summary>
public enum ColumnEncodingType
{
    String,
    Integer,
    Decimal2,
    Decimal6
}
