using System.Data;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Rút giá trị duy nhất của một trường con MARC bằng chính PostgreSQL (II.9).
///
/// The MARC record is stored as jsonb, so the scan is a single SQL statement that unnests the data
/// fields and their subfields. Doing it in the database rather than in .NET is what makes this
/// usable on a catalogue of a hundred thousand records: nothing but the distinct values crosses the
/// wire, and the grouping happens where the data already is.
///
/// The values are trimmed of trailing ISBD punctuation before grouping, otherwise "Hà Nội :" and
/// "Hà Nội" would appear as two separate places — which is exactly the mess this feature exists to
/// clean up.
/// </summary>
public class CustomIndexHarvester : ICustomIndexHarvester
{
    /// <summary>Số giá trị tối đa trả về một lần quét.</summary>
    private const int MaxValues = 20_000;

    private readonly LibraryConnectDbContext _db;

    public CustomIndexHarvester(LibraryConnectDbContext db) => _db = db;

    public async Task<IReadOnlyList<HarvestedValue>> HarvestAsync(
        string tag, string subfield, CancellationToken ct = default)
    {
        const string sql = """
            SELECT extracted.value AS name, COUNT(DISTINCT extracted.id)::int AS count
            FROM (
                SELECT b.id,
                       btrim(btrim(sf ->> 'value'), ' /:;,=+') AS value
                FROM bib.bib_records b
                CROSS JOIN LATERAL jsonb_array_elements(b.marc_data -> 'dataFields') AS df
                CROSS JOIN LATERAL jsonb_array_elements(df -> 'subfields') AS sf
                WHERE b.deleted_at IS NULL
                  AND df ->> 'tag' = @tag
                  AND sf ->> 'code' = @subfield
            ) AS extracted
            WHERE extracted.value <> ''
            GROUP BY extracted.value
            ORDER BY count DESC, name
            LIMIT @limit
            """;

        var connection = _db.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();

        AddParameter(command, "tag", tag);
        AddParameter(command, "subfield", subfield);
        AddParameter(command, "limit", MaxValues);

        var results = new List<HarvestedValue>();

        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            results.Add(new HarvestedValue(reader.GetString(0), reader.GetInt32(1)));
        }

        return results;
    }

    public async Task<int> RebuildLinksAsync(
        Guid indexId, string tag, string subfield, CancellationToken ct = default)
    {
        // One statement per step, all inside the caller's transaction. Matching goes through
        // bib.vn_unaccent so a value written with different diacritics or capitalisation still lands
        // on the same entry, and through the alias list so a merge the librarian made survives.
        const string deleteSql = """
            DELETE FROM cat.custom_index_links l
            USING cat.custom_index_values v
            WHERE l.custom_index_value_id = v.id
              AND v.custom_index_id = @indexId
            """;

        const string insertSql = """
            INSERT INTO cat.custom_index_links (custom_index_value_id, bib_id)
            SELECT DISTINCT v.id, e.bib_id
            FROM (
                SELECT b.id AS bib_id,
                       btrim(btrim(sf ->> 'value'), ' /:;,=+') AS value
                FROM bib.bib_records b
                CROSS JOIN LATERAL jsonb_array_elements(b.marc_data -> 'dataFields') AS df
                CROSS JOIN LATERAL jsonb_array_elements(df -> 'subfields') AS sf
                WHERE b.deleted_at IS NULL
                  AND df ->> 'tag' = @tag
                  AND sf ->> 'code' = @subfield
            ) AS e
            JOIN cat.custom_index_values v
              ON v.custom_index_id = @indexId
             AND v.deleted_at IS NULL
             AND (
                   bib.vn_unaccent(v.name) = bib.vn_unaccent(e.value)
                   OR EXISTS (
                       SELECT 1 FROM jsonb_array_elements_text(v.aliases) AS alias
                       WHERE bib.vn_unaccent(alias) = bib.vn_unaccent(e.value)
                   )
                 )
            WHERE e.value <> ''
            ON CONFLICT DO NOTHING
            """;

        const string countSql = """
            UPDATE cat.custom_index_values v
            SET record_count = COALESCE(c.total, 0)
            FROM (
                SELECT v2.id, COUNT(l.bib_id)::int AS total
                FROM cat.custom_index_values v2
                LEFT JOIN cat.custom_index_links l ON l.custom_index_value_id = v2.id
                WHERE v2.custom_index_id = @indexId
                GROUP BY v2.id
            ) AS c
            WHERE v.id = c.id
            """;

        var connection = _db.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        await ExecuteAsync(connection, deleteSql, indexId, tag, subfield, ct);
        var inserted = await ExecuteAsync(connection, insertSql, indexId, tag, subfield, ct);
        await ExecuteAsync(connection, countSql, indexId, tag, subfield, ct);

        return inserted;
    }

    private async Task<int> ExecuteAsync(
        System.Data.Common.DbConnection connection,
        string sql,
        Guid indexId,
        string tag,
        string subfield,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();

        AddParameter(command, "indexId", indexId);
        AddParameter(command, "tag", tag);
        AddParameter(command, "subfield", subfield);

        return await command.ExecuteNonQueryAsync(ct);
    }

    private static void AddParameter(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
