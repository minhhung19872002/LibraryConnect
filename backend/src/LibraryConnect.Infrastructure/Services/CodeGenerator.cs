using System.Globalization;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Produces the business codes described in I.3: barcode, register number, card number, order and
/// request codes. The prefix, length and yearly-reset behaviour of each sequence are parameters, so
/// a customer can match their existing numbering without a code change.
///
/// Sequence state is kept in a PostgreSQL sequence table updated inside the caller's transaction,
/// which is what makes concurrent issuing safe when two librarians register copies at the same time.
/// </summary>
public class CodeGenerator : ICodeGenerator
{
    private readonly LibraryConnectDbContext _db;
    private readonly ISystemParameterService _parameters;
    private readonly IDateTimeProvider _clock;

    public CodeGenerator(LibraryConnectDbContext db, ISystemParameterService parameters, IDateTimeProvider clock)
    {
        _db = db;
        _parameters = parameters;
        _clock = clock;
    }

    public async Task<string> NextAsync(string sequenceKey, CancellationToken ct = default)
    {
        var codes = await NextBatchAsync(sequenceKey, 1, ct);
        return codes[0];
    }

    public async Task<IReadOnlyList<string>> NextBatchAsync(string sequenceKey, int count, CancellationToken ct = default)
    {
        if (count <= 0)
        {
            return Array.Empty<string>();
        }

        var prefix = await _parameters.GetAsync($"CODE.{sequenceKey}_PREFIX", string.Empty, ct);
        var suffix = await _parameters.GetAsync($"CODE.{sequenceKey}_SUFFIX", string.Empty, ct);
        var length = await _parameters.GetAsync($"CODE.{sequenceKey}_LENGTH", 6, ct);
        var resetYearly = await _parameters.GetAsync($"CODE.{sequenceKey}_RESET_YEARLY", false, ct);

        var year = _clock.Today.Year;
        var scope = resetYearly ? year.ToString(CultureInfo.InvariantCulture) : "ALL";

        var start = await ReserveAsync(sequenceKey, scope, count, ct);

        var results = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var number = (start + i).ToString(CultureInfo.InvariantCulture).PadLeft(length, '0');
            var yearPart = resetYearly ? year.ToString(CultureInfo.InvariantCulture) : string.Empty;
            results.Add($"{prefix}{yearPart}{number}{suffix}");
        }

        return results;
    }

    /// <summary>
    /// Atomically reserves <paramref name="count"/> values and returns the first one. The UPDATE ...
    /// RETURNING runs as a single statement so two concurrent callers can never receive the same
    /// number.
    /// </summary>
    private async Task<long> ReserveAsync(string sequenceKey, string scope, int count, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO sys.code_sequences (key, scope, current_value)
            VALUES ({0}, {1}, {2})
            ON CONFLICT (key, scope)
            DO UPDATE SET current_value = sys.code_sequences.current_value + {2}
            RETURNING current_value - {2} + 1
            """;

        await using var command = _db.Database.GetDbConnection().CreateCommand();

        var connection = _db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        command.CommandText = string.Format(CultureInfo.InvariantCulture, sql, "@key", "@scope", "@count");
        command.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();

        var keyParam = command.CreateParameter();
        keyParam.ParameterName = "key";
        keyParam.Value = sequenceKey;
        command.Parameters.Add(keyParam);

        var scopeParam = command.CreateParameter();
        scopeParam.ParameterName = "scope";
        scopeParam.Value = scope;
        command.Parameters.Add(scopeParam);

        var countParam = command.CreateParameter();
        countParam.ParameterName = "count";
        countParam.Value = (long)count;
        command.Parameters.Add(countParam);

        var result = await command.ExecuteScalarAsync(ct);
        return System.Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }
}

/// <summary>Sequence keys used across the product.</summary>
public static class SequenceKeys
{
    public const string Barcode = "BARCODE";
    public const string RegisterNumber = "REGISTER";
    public const string CardNumber = "CARD";
    public const string PurchaseRequest = "REQUEST";
    public const string PurchaseOrder = "ORDER";
    public const string Handover = "HANDOVER";
    public const string Loan = "LOAN";
    public const string Fine = "FINE";
    public const string ControlNumber = "CONTROL";
    public const string InventoryPeriod = "INVENTORY";
    public const string SerialBinding = "BINDING";
    public const string SerialClaim = "CLAIM";
}
