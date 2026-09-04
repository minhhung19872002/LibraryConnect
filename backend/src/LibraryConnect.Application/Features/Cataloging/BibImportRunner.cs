using System.Text.Json;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Marc;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Marc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Thực hiện một tác vụ nhập biểu ghi đã xếp hàng đợi.</summary>
public interface IBibImportRunner
{
    Task RunAsync(Guid jobId, CancellationToken ct);
}

/// <summary>
/// Nhập biểu ghi từ tệp trao đổi, chạy nền (II.6 bước 4).
///
/// The job runs record by record and commits in batches, so a file of twenty thousand records does
/// not hold one transaction open for minutes and a failure part-way through does not throw away the
/// records that already went in. Every record that fails is recorded with its number and the reason,
/// which is what the librarian downloads as the error log.
/// </summary>
public class BibImportRunner : IBibImportRunner
{
    /// <summary>Số biểu ghi ghi xuống cơ sở dữ liệu mỗi lần.</summary>
    private const int BatchSize = 50;

    /// <summary>Số lỗi giữ lại trong bản ghi tác vụ; nhiều hơn nữa thì tệp đó cần sửa chứ không cần đọc tiếp.</summary>
    private const int MaxRecordedErrors = 500;

    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IBibRecordWriter _writer;
    private readonly IBibDuplicateFinder _duplicates;
    private readonly IMarcRuleProvider _rules;
    private readonly ICodeGenerator _codes;
    private readonly ISystemParameterService _parameters;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<BibImportRunner> _logger;

    public BibImportRunner(
        IApplicationDbContext db,
        IFileStorage storage,
        IBibRecordWriter writer,
        IBibDuplicateFinder duplicates,
        IMarcRuleProvider rules,
        ICodeGenerator codes,
        ISystemParameterService parameters,
        IDateTimeProvider clock,
        ILogger<BibImportRunner> logger)
    {
        _db = db;
        _storage = storage;
        _writer = writer;
        _duplicates = duplicates;
        _rules = rules;
        _codes = codes;
        _parameters = parameters;
        _clock = clock;
        _logger = logger;
    }

    public async Task RunAsync(Guid jobId, CancellationToken ct)
    {
        var job = await _db.ImportExportJobs.FirstOrDefaultAsync(item => item.Id == jobId, ct);

        if (job is null)
        {
            _logger.LogWarning("Không tìm thấy tác vụ nhập {JobId}", jobId);
            return;
        }

        job.Status = JobStatus.Running;
        job.StartedAt = _clock.Now;
        await _db.SaveChangesAsync(ct);

        var errors = new List<ImportJobErrorDto>();

        try
        {
            var options = JsonSerializer.Deserialize<BibImportOptions>(
                                job.Options ?? "{}",
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                            ?? new BibImportOptions();

            var content = await ReadFileAsync(job.FilePath!, ct);
            var read = Iso2709Reader.ReadAllTolerant(content);

            IReadOnlyList<MarcRecord> records = read.Records;

            if (LooksLikeXml(content))
            {
                records = MarcXml.ReadAll(content);
            }
            else
            {
                foreach (var error in read.Errors)
                {
                    Record(errors, error.RecordNumber, null, error.Message);
                }
            }

            job.Total = records.Count + read.Errors.Count;
            job.Failed = read.Errors.Count;
            await _db.SaveChangesAsync(ct);

            var validator = await _rules.GetValidatorAsync(ct);
            var pattern = await _parameters.GetAsync(
                "CATALOG.CALL_NUMBER_PATTERN", CallNumberBuilder.DefaultPattern, ct);

            for (var index = 0; index < records.Count; index++)
            {
                ct.ThrowIfCancellationRequested();

                var number = index + 1;
                var marc = records[index];

                try
                {
                    var outcome = await ImportOneAsync(marc, options, validator, pattern, ct);

                    switch (outcome.Kind)
                    {
                        case ImportOutcomeKind.Created:
                        case ImportOutcomeKind.Updated:
                            job.Success++;
                            break;

                        case ImportOutcomeKind.Skipped:
                            job.Skipped++;
                            break;

                        default:
                            job.Failed++;
                            Record(errors, number, marc.ControlNumber, outcome.Message ?? "Không nhập được biểu ghi.");
                            break;
                    }
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    job.Failed++;
                    Record(errors, number, marc.ControlNumber, exception.Message);

                    // The change tracker holds a half-built record after a failure; dropping it keeps
                    // the next record in the file from inheriting the problem.
                    _db.ChangeTracker.Clear();
                }

                if (number % BatchSize == 0)
                {
                    await SaveProgressAsync(job, errors, ct);
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
            _logger.LogError(exception, "Tác vụ nhập {JobId} thất bại", jobId);
        }

        job.FinishedAt = _clock.Now;
        job.Errors = JsonSerializer.Serialize(errors);

        _db.ChangeTracker.Clear();
        _db.ImportExportJobs.Update(job);
        await _db.SaveChangesAsync(CancellationToken.None);

        _logger.LogInformation(
            "Tác vụ nhập {JobId} kết thúc: {Success} thành công, {Skipped} bỏ qua, {Failed} lỗi",
            jobId, job.Success, job.Skipped, job.Failed);
    }

    private enum ImportOutcomeKind { Created, Updated, Skipped, Failed }

    private record ImportOutcome(ImportOutcomeKind Kind, string? Message = null);

    private async Task<ImportOutcome> ImportOneAsync(
        MarcRecord marc,
        BibImportOptions options,
        MarcValidator validator,
        string callNumberPattern,
        CancellationToken ct)
    {
        var projection = MarcProjection.Project(marc);

        if (string.IsNullOrWhiteSpace(projection.Title))
        {
            return new ImportOutcome(ImportOutcomeKind.Failed, "Biểu ghi không có nhan đề (trường 245$a).");
        }

        var duplicate = await _duplicates.FindAsync(projection, marc.ControlNumber, options.MatchBy, ct);
        BibRecord entity;
        bool isNew;

        if (duplicate is null || options.OnDuplicate == DuplicateAction.CreateNew)
        {
            entity = new BibRecord { Id = Guid.NewGuid(), Source = BibSource.Iso2709 };
            isNew = true;
        }
        else
        {
            switch (options.OnDuplicate)
            {
                case DuplicateAction.Skip:
                    return new ImportOutcome(ImportOutcomeKind.Skipped);

                case DuplicateAction.Merge:
                    entity = await LoadForWriteAsync(duplicate.Id, ct);
                    marc = MarcMerge.MergeInto(MarcJson.Deserialize(entity.MarcData), marc);
                    isNew = false;
                    break;

                default:
                    entity = await LoadForWriteAsync(duplicate.Id, ct);
                    isNew = false;
                    break;
            }
        }

        if (isNew)
        {
            // An incoming record keeps the control number of the library that made it only when this
            // library is not the one that issued it; otherwise two records would share a number.
            if (!string.IsNullOrWhiteSpace(marc.ControlNumber)
                && await _db.BibRecords.AnyAsync(bib => bib.ControlNumber == marc.ControlNumber, ct))
            {
                marc.ControlNumber = null;
            }

            entity.SourceRef = MarcProjection.Clean(marc.GetSubfield("035", 'a'));
            _db.BibRecords.Add(entity);
        }

        await _writer.PrepareAsync(entity, marc, ct);

        var issues = validator.Validate(marc);

        if (!MarcValidator.IsValid(issues))
        {
            var message = string.Join(" ", issues
                .Where(issue => issue.Severity == MarcIssueSeverity.Error)
                .Select(issue => issue.Message));

            return new ImportOutcome(ImportOutcomeKind.Failed, message);
        }

        entity.DocumentTypeId = options.DocumentTypeId ?? entity.DocumentTypeId;
        entity.Status = options.Status;

        await _writer.ApplyAsync(entity, marc, isNew, "Nhập từ tệp trao đổi", ct);
        await _db.SaveChangesAsync(ct);

        if (options.CreateItems && options.WarehouseId is not null)
        {
            await CreateItemsAsync(entity, options, callNumberPattern, ct);
        }

        if (options.AddToCatalogQueue)
        {
            await AddToQueueAsync(entity, ct);
        }

        return new ImportOutcome(isNew ? ImportOutcomeKind.Created : ImportOutcomeKind.Updated);
    }

    private Task<BibRecord> LoadForWriteAsync(Guid id, CancellationToken ct) =>
        _db.BibRecords
            .Include(record => record.Authors)
            .Include(record => record.Subjects)
            .Include(record => record.Keywords)
            .Include(record => record.Classifications)
            .Include(record => record.Collections)
            .FirstAsync(record => record.Id == id, ct);

    private async Task CreateItemsAsync(
        BibRecord bib,
        BibImportOptions options,
        string callNumberPattern,
        CancellationToken ct)
    {
        var quantity = Math.Clamp(options.ItemQuantity, 1, 100);
        var barcodes = await _codes.NextBatchAsync("BARCODE", quantity, ct);
        var registerNumbers = await _codes.NextBatchAsync("REGISTER", quantity, ct);

        var callNumber = CallNumberBuilder.Build(
            callNumberPattern,
            new CallNumberBuilder.Context(bib.Ddc, bib.AuthorMain, bib.Title, bib.PublishYear, 1));

        for (var index = 0; index < quantity; index++)
        {
            _db.Items.Add(new Item
            {
                Id = Guid.NewGuid(),
                BibId = bib.Id,
                Barcode = barcodes[index],
                RegisterNumber = registerNumbers[index],
                WarehouseId = options.WarehouseId!.Value,
                CallNumber = callNumber,
                FundingSourceId = options.FundingSourceId,
                AcquisitionDate = _clock.Today,
                AcquisitionType = AcquisitionType.Purchase,
                // Copies created by an import have not been seen by anyone yet, so they wait for the
                // inspection step rather than going straight onto the shelf.
                Status = ItemStatus.PendingInspection,
                IsLocked = true,
                LockReason = "Chờ kiểm nhận sau khi nhập từ tệp",
                CopyNumber = index + 1
            });
        }

        bib.ItemCount = quantity;
        bib.AvailableItemCount = 0;

        await _db.SaveChangesAsync(ct);
    }

    private async Task AddToQueueAsync(BibRecord bib, CancellationToken ct)
    {
        var already = await _db.CatalogQueue.AnyAsync(item => item.BibId == bib.Id, ct);

        if (already)
        {
            return;
        }

        _db.CatalogQueue.Add(new CatalogQueueItem
        {
            Id = Guid.NewGuid(),
            BibId = bib.Id,
            Status = CatalogQueueStatus.Pending,
            Priority = 3,
            Note = "Biểu ghi nhập từ tệp trao đổi, cần biên mục chi tiết"
        });

        await _db.SaveChangesAsync(ct);
    }

    private async Task<byte[]> ReadFileAsync(string objectName, CancellationToken ct)
    {
        await using var stream = await _storage.DownloadAsync(StartBibImportCommandHandler.Bucket, objectName, ct);
        using var buffer = new MemoryStream();

        await stream.CopyToAsync(buffer, ct);

        return buffer.ToArray();
    }

    private async Task SaveProgressAsync(
        Domain.Entities.Ill.ImportExportJob job,
        List<ImportJobErrorDto> errors,
        CancellationToken ct)
    {
        job.Errors = JsonSerializer.Serialize(errors);
        await _db.SaveChangesAsync(ct);
    }

    private static void Record(List<ImportJobErrorDto> errors, int row, string? identifier, string message)
    {
        if (errors.Count >= MaxRecordedErrors)
        {
            return;
        }

        errors.Add(new ImportJobErrorDto { Row = row, Identifier = identifier, Message = message });
    }

    private static bool LooksLikeXml(byte[] content)
    {
        var index = content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF ? 3 : 0;

        while (index < content.Length && content[index] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
        {
            index++;
        }

        return index < content.Length && content[index] == (byte)'<';
    }

    /// <summary>Câu giải thích đọc được của một lỗi, ưu tiên lỗi kiểm tra dữ liệu từng trường.</summary>
    private static string MoTaLoi(Exception exception) =>
        exception is Common.Exceptions.ValidationException validation && validation.Errors.Count > 0
            ? string.Join(" ", validation.Errors.Select(error => error.Message))
            : exception.Message;
}
