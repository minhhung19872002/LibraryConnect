using System.Text.Json;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Marc;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Marc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Thực hiện một tác vụ nhập biểu ghi từ Excel đã xếp hàng đợi (II.8).</summary>
public interface IBibExcelImportRunner
{
    Task RunAsync(Guid jobId, CancellationToken ct);
}

/// <summary>
/// Nhập biểu ghi từ bảng tính, chạy nền.
///
/// Each row becomes a MARC record built from the column mapping, then goes down exactly the same
/// path as a record typed by hand or read out of an exchange file: the same validation, the same
/// authority linking, the same duplicate handling. A spreadsheet is a way of typing quickly, not a
/// second kind of catalogue.
/// </summary>
public class BibExcelImportRunner : IBibExcelImportRunner
{
    private const int BatchSize = 50;
    private const int MaxRecordedErrors = 500;

    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IExcelService _excel;
    private readonly IBibRecordWriter _writer;
    private readonly IBibDuplicateFinder _duplicates;
    private readonly IMarcRuleProvider _rules;
    private readonly ISystemParameterService _parameters;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<BibExcelImportRunner> _logger;

    public BibExcelImportRunner(
        IApplicationDbContext db,
        IFileStorage storage,
        IExcelService excel,
        IBibRecordWriter writer,
        IBibDuplicateFinder duplicates,
        IMarcRuleProvider rules,
        ISystemParameterService parameters,
        IDateTimeProvider clock,
        ILogger<BibExcelImportRunner> logger)
    {
        _db = db;
        _storage = storage;
        _excel = excel;
        _writer = writer;
        _duplicates = duplicates;
        _rules = rules;
        _parameters = parameters;
        _clock = clock;
        _logger = logger;
    }

    public async Task RunAsync(Guid jobId, CancellationToken ct)
    {
        var job = await _db.ImportExportJobs.FirstOrDefaultAsync(item => item.Id == jobId, ct);

        if (job is null)
        {
            _logger.LogWarning("Không tìm thấy tác vụ nhập Excel {JobId}", jobId);
            return;
        }

        job.Status = JobStatus.Running;
        job.StartedAt = _clock.Now;
        await _db.SaveChangesAsync(ct);

        var errors = new List<ImportJobErrorDto>();

        try
        {
            var options = JsonSerializer.Deserialize<ExcelImportOptions>(
                                job.Options ?? "{}",
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                            ?? new ExcelImportOptions();

            var content = await ReadFileAsync(job.FilePath!, ct);

            ExcelSheet sheet;

            using (var stream = new MemoryStream(content))
            {
                sheet = _excel.Read(stream);
            }

            var rows = sheet.Rows.Where(row => !row.IsEmpty).ToList();

            job.Total = rows.Count;
            await _db.SaveChangesAsync(ct);

            var validator = await _rules.GetValidatorAsync(ct);
            var language = await _parameters.GetAsync("CATALOG.DEFAULT_LANGUAGE", "vie", ct);
            var country = await _parameters.GetAsync("CATALOG.DEFAULT_COUNTRY", "vm", ct);

            var processed = 0;

            foreach (var row in rows)
            {
                ct.ThrowIfCancellationRequested();
                processed++;

                try
                {
                    var marc = BuildRecord(row, options, language, country);
                    var outcome = await ImportOneAsync(marc, options, validator, ct);

                    switch (outcome)
                    {
                        case ImportOutcomeKind.Created:
                        case ImportOutcomeKind.Updated:
                            job.Success++;
                            break;

                        case ImportOutcomeKind.Skipped:
                            job.Skipped++;
                            break;
                    }
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    job.Failed++;
                    Record(errors, row.RowNumber, null, exception.Message);
                    _db.ChangeTracker.Clear();
                }

                if (processed % BatchSize == 0)
                {
                    job.Errors = JsonSerializer.Serialize(errors);
                    await _db.SaveChangesAsync(ct);
                }
            }

            job.Status = JobStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            job.Status = JobStatus.Cancelled;
        }
        catch (Exception exception)
        {
            job.Status = JobStatus.Failed;

            // Lỗi kiểm tra dữ liệu mang câu giải thích ở danh sách lỗi từng trường, còn Message chỉ
            // là "Dữ liệu không hợp lệ." Ghi câu chung chung ấy vào nhật ký thì người dùng mở ra
            // xem cũng không biết tệp của mình sai chỗ nào.
            Record(errors, 0, null, MoTaLoi(exception));
            _logger.LogError(exception, "Tác vụ nhập Excel {JobId} thất bại", jobId);
        }

        job.FinishedAt = _clock.Now;
        job.Errors = JsonSerializer.Serialize(errors);

        _db.ChangeTracker.Clear();
        _db.ImportExportJobs.Update(job);
        await _db.SaveChangesAsync(CancellationToken.None);

        _logger.LogInformation(
            "Tác vụ nhập Excel {JobId} kết thúc: {Success} thành công, {Skipped} bỏ qua, {Failed} lỗi",
            jobId, job.Success, job.Skipped, job.Failed);
    }

    private enum ImportOutcomeKind { Created, Updated, Skipped }

    /// <summary>
    /// Dựng biểu ghi MARC từ một dòng bảng tính theo bảng ánh xạ.
    ///
    /// Fields are grouped by tag so that several columns feeding the same field — place, publisher
    /// and year all going to 260 — land in one field rather than three. A cell holding several values
    /// separated by the mapping's separator becomes several repetitions of the field, which is how a
    /// list of subject headings has to end up.
    /// </summary>
    private MarcRecord BuildRecord(ExcelRow row, ExcelImportOptions options, string language, string country)
    {
        var record = new MarcRecord();
        record.Leader.RecordStatus = 'n';
        record.Leader.RecordType = 'a';
        record.Leader.BibliographicLevel = 'm';

        var today = _clock.Today;
        var fixedField = new string(' ', 40).ToCharArray();

        void Write(int start, string text)
        {
            for (var index = 0; index < text.Length && start + index < fixedField.Length; index++)
            {
                fixedField[start + index] = text[index];
            }
        }

        Write(0, $"{today.Year % 100:00}{today.Month:00}{today.Day:00}");
        fixedField[6] = 's';
        Write(15, country.PadRight(3)[..3]);
        Write(35, language.PadRight(3)[..3]);
        fixedField[39] = 'd';

        foreach (var group in options.Mapping.GroupBy(mapping => mapping.Tag))
        {
            var repeated = group
                .Select(mapping => new
                {
                    Mapping = mapping,
                    Values = Split(row.Get(mapping.Column), mapping.Separator)
                })
                .Where(item => item.Values.Count > 0)
                .ToList();

            if (repeated.Count == 0)
            {
                continue;
            }

            if (MarcConstants.IsControlFieldTag(group.Key))
            {
                var value = repeated[0].Values[0];

                if (group.Key == "008")
                {
                    Write(0, value);
                }
                else
                {
                    record.SetControlField(group.Key, value);
                }

                continue;
            }

            // How many repetitions this field needs: the longest list among the columns feeding it.
            var occurrences = repeated.Max(item => item.Values.Count);

            for (var index = 0; index < occurrences; index++)
            {
                var first = repeated[0].Mapping;

                var field = record.AddField(
                    group.Key,
                    string.IsNullOrEmpty(first.Ind1) ? ' ' : first.Ind1[0],
                    string.IsNullOrEmpty(first.Ind2) ? ' ' : first.Ind2[0]);

                foreach (var item in repeated)
                {
                    // A column with a single value applies to the first repetition only, which is
                    // what "Hà Nội" plus three subject headings has to mean.
                    if (index >= item.Values.Count)
                    {
                        continue;
                    }

                    var code = string.IsNullOrEmpty(item.Mapping.Subfield) ? 'a' : item.Mapping.Subfield[0];
                    field.AddSubfield(code, item.Values[index]);
                }

                if (field.Subfields.Count == 0)
                {
                    record.DataFields.Remove(field);
                }
            }
        }

        record.SetControlField("008", new string(fixedField));

        record.DataFields = record.DataFields
            .OrderBy(field => field.Tag, StringComparer.Ordinal)
            .ToList();

        return record;
    }

    private static List<string> Split(string value, string? separator)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new List<string>();
        }

        if (string.IsNullOrEmpty(separator))
        {
            return new List<string> { value.Trim() };
        }

        return value
            .Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private async Task<ImportOutcomeKind> ImportOneAsync(
        MarcRecord marc,
        ExcelImportOptions options,
        MarcValidator validator,
        CancellationToken ct)
    {
        var projection = MarcProjection.Project(marc);

        if (string.IsNullOrWhiteSpace(projection.Title))
        {
            throw new InvalidOperationException("Dòng này không có nhan đề (cột ánh xạ sang 245$a để trống).");
        }

        var duplicate = await _duplicates.FindAsync(projection, null, options.MatchBy, ct);
        BibRecord entity;
        bool isNew;

        if (duplicate is null || options.OnDuplicate == DuplicateAction.CreateNew)
        {
            entity = new BibRecord { Id = Guid.NewGuid(), Source = BibSource.Excel };
            isNew = true;
            _db.BibRecords.Add(entity);
        }
        else if (options.OnDuplicate == DuplicateAction.Skip)
        {
            return ImportOutcomeKind.Skipped;
        }
        else
        {
            entity = await _db.BibRecords
                .Include(record => record.Authors)
                .Include(record => record.Subjects)
                .Include(record => record.Keywords)
                .Include(record => record.Classifications)
                .Include(record => record.Collections)
                .FirstAsync(record => record.Id == duplicate.Id, ct);

            if (options.OnDuplicate == DuplicateAction.Merge)
            {
                // "Gộp" keeps everything the local record already has and only fills in what the
                // spreadsheet adds — the same rule the exchange-file importer applies.
                marc = MarcMerge.MergeInto(MarcJson.Deserialize(entity.MarcData), marc);
            }

            isNew = false;
        }

        await _writer.PrepareAsync(entity, marc, ct);

        var issues = validator.Validate(marc);

        if (!MarcValidator.IsValid(issues))
        {
            throw new InvalidOperationException(string.Join(" ", issues
                .Where(issue => issue.Severity == MarcIssueSeverity.Error)
                .Select(issue => issue.Message)));
        }

        entity.DocumentTypeId = options.DocumentTypeId ?? entity.DocumentTypeId;
        entity.Status = options.Status;

        await _writer.ApplyAsync(entity, marc, isNew, "Nhập từ bảng tính Excel", ct);
        await _db.SaveChangesAsync(ct);

        if (options.AddToCatalogQueue && !await _db.CatalogQueue.AnyAsync(item => item.BibId == entity.Id, ct))
        {
            _db.CatalogQueue.Add(new CatalogQueueItem
            {
                Id = Guid.NewGuid(),
                BibId = entity.Id,
                Status = CatalogQueueStatus.Pending,
                Priority = 3,
                Note = "Biểu ghi nhập từ bảng tính, cần biên mục chi tiết"
            });

            await _db.SaveChangesAsync(ct);
        }

        return isNew ? ImportOutcomeKind.Created : ImportOutcomeKind.Updated;
    }

    private async Task<byte[]> ReadFileAsync(string objectName, CancellationToken ct)
    {
        await using var stream = await _storage.DownloadAsync(StartBibImportCommandHandler.Bucket, objectName, ct);
        using var buffer = new MemoryStream();

        await stream.CopyToAsync(buffer, ct);

        return buffer.ToArray();
    }

    private static void Record(List<ImportJobErrorDto> errors, int row, string? identifier, string message)
    {
        if (errors.Count >= MaxRecordedErrors)
        {
            return;
        }

        errors.Add(new ImportJobErrorDto { Row = row, Identifier = identifier, Message = message });
    }

    /// <summary>Câu giải thích đọc được của một lỗi, ưu tiên lỗi kiểm tra dữ liệu từng trường.</summary>
    private static string MoTaLoi(Exception exception) =>
        exception is Common.Exceptions.ValidationException validation && validation.Errors.Count > 0
            ? string.Join(" ", validation.Errors.Select(error => error.Message))
            : exception.Message;
}
