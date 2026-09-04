using System.Text.Json;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Domain.Entities.Rdr;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Application.Features.Readers;

// ---------------------------------------------------------------------------------------------
// VI.4 — Nhập bạn đọc từ Excel: tệp mẫu, kiểm tra, nhập chạy nền, nhật ký lỗi.
// ---------------------------------------------------------------------------------------------

/// <summary>Tải tệp mẫu Excel kèm sheet hướng dẫn từng cột.</summary>
public record GetReaderImportTemplateQuery : IRequest<PrintedFileDto>;

public class GetReaderImportTemplateQueryHandler : IRequestHandler<GetReaderImportTemplateQuery, PrintedFileDto>
{
    private readonly IExcelService _excel;

    public GetReaderImportTemplateQueryHandler(IExcelService excel) => _excel = excel;

    public Task<PrintedFileDto> Handle(GetReaderImportTemplateQuery query, CancellationToken ct)
    {
        var columns = ReaderImportFields.DefaultHeaders
            .Select(pair => new ExcelTemplateColumn(
                pair.Value,
                ReaderImportFields.Descriptions[pair.Key],
                Required: pair.Key == ReaderImportFields.FullName,
                Example: SampleOf(pair.Key)))
            .ToList();

        var sample = new List<IReadOnlyDictionary<string, string>>
        {
            ReaderImportFields.DefaultHeaders.ToDictionary(
                pair => pair.Value,
                pair => SampleOf(pair.Key) ?? string.Empty)
        };

        var content = _excel.WriteTemplate("Bạn đọc", columns, sample);

        return Task.FromResult(new PrintedFileDto(content, "mau-nhap-ban-doc.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
    }

    private static string? SampleOf(string field) => field switch
    {
        ReaderImportFields.CardNumber => "TV000101",
        ReaderImportFields.StudentCode => "2151010101",
        ReaderImportFields.FullName => "Nguyễn Văn An",
        ReaderImportFields.Gender => "Nam",
        ReaderImportFields.DateOfBirth => "05/09/2005",
        ReaderImportFields.IdCardNumber => "079205001234",
        ReaderImportFields.Email => "an.nv@sinhvien.edu.vn",
        ReaderImportFields.Phone => "0901234567",
        ReaderImportFields.Address => "12 Nguyễn Huệ, Quận 1, TP.HCM",
        ReaderImportFields.ReaderType => "Sinh viên",
        ReaderImportFields.Faculty => "Công nghệ thông tin",
        ReaderImportFields.Major => "Kỹ thuật phần mềm",
        ReaderImportFields.ClassName => "DH21TH1",
        ReaderImportFields.CourseYear => "K21",
        ReaderImportFields.CardIssueDate => "01/09/2025",
        ReaderImportFields.CardExpireDate => "31/08/2029",
        _ => null
    };
}

/// <summary>Kiểm tra tệp trước khi nhập: trả bảng lỗi và vài dòng xem trước, không ghi gì.</summary>
public record ValidateReaderImportQuery(Stream FileStream, string FileName, ReaderImportOptions Options)
    : IRequest<ReaderImportPreviewDto>;

public class ValidateReaderImportQueryHandler
    : IRequestHandler<ValidateReaderImportQuery, ReaderImportPreviewDto>
{
    private readonly IExcelService _excel;
    private readonly ReaderImportProcessor _processor;

    public ValidateReaderImportQueryHandler(IExcelService excel, ReaderImportProcessor processor)
    {
        _excel = excel;
        _processor = processor;
    }

    public async Task<ReaderImportPreviewDto> Handle(
        ValidateReaderImportQuery query, CancellationToken ct)
    {
        var sheet = _excel.Read(query.FileStream);
        var outcome = await _processor.ProcessAsync(sheet, query.Options, dryRun: true, ct);

        return new ReaderImportPreviewDto
        {
            FileName = query.FileName,
            TotalRows = outcome.TotalRows,
            ValidRows = outcome.ValidRows,
            ErrorRows = outcome.ErrorRows,
            Headers = sheet.Headers.ToList(),
            Errors = outcome.Errors,
            Sample = outcome.Rows,
            ErrorRowCells = ReaderImportRows.ErrorCells(sheet, outcome)
        };
    }
}

/// <summary>Chuyển qua lại giữa dòng thô của giao diện và sheet mà bộ xử lý nhập đọc.</summary>
public static class ReaderImportRows
{
    /// <summary>Nguyên ô của những dòng có lỗi, giữ đúng số dòng của tệp.</summary>
    public static List<ReaderImportRawRowDto> ErrorCells(ExcelSheet sheet, ReaderImportOutcome outcome)
    {
        var failed = outcome.Errors.Select(error => error.Row).ToHashSet();

        return sheet.Rows
            .Where(row => failed.Contains(row.RowNumber))
            .Select(row => new ReaderImportRawRowDto
            {
                Row = row.RowNumber,
                Cells = row.Cells.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)
            })
            .ToList();
    }

    /// <summary>Dựng sheet ảo từ các dòng cán bộ đã sửa trên lưới; tiêu đề là hợp của mọi ô đã gửi.</summary>
    public static ExcelSheet ToSheet(IReadOnlyList<ReaderImportRawRowDto> rows)
    {
        var headers = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in rows.SelectMany(row => row.Cells.Keys))
        {
            if (seen.Add(header))
            {
                headers.Add(header);
            }
        }

        return new ExcelSheet
        {
            Name = "sua-tai-cho",
            Headers = headers,
            Rows = rows.Select(row => new ExcelRow
            {
                RowNumber = row.Row,
                Cells = row.Cells.ToDictionary(
                    pair => pair.Key, pair => pair.Value?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            }).ToList()
        };
    }
}

/// <summary>
/// Kiểm tra lại hoặc nhập thật những dòng cán bộ đã sửa ngay trên bảng lỗi (VI.4), không phải mở lại
/// Excel. Đi qua đúng bộ xử lý của nhập tệp nên luật kiểm tra chỉ có một.
/// </summary>
public class ImportReaderRowsCommand : IRequest<ReaderImportRowsResultDto>
{
    public List<ReaderImportRawRowDto> Rows { get; set; } = new();
    public ReaderImportOptions Options { get; set; } = new();
    /// <summary>Chỉ kiểm tra, không ghi.</summary>
    public bool DryRun { get; set; }
    /// <summary>Tên tệp gốc, để đợt nhập tại chỗ ghi vào sổ kèm tên tệp ấy.</summary>
    public string? FileName { get; set; }
}

public class ImportReaderRowsCommandHandler : IRequestHandler<ImportReaderRowsCommand, ReaderImportRowsResultDto>
{
    /// <summary>Lưới sửa tại chỗ là để sửa vài chục dòng lỗi; nhiều hơn thì sửa trong tệp rồi nhập lại.</summary>
    public const int MaxRows = 500;

    private readonly IApplicationDbContext _db;
    private readonly ReaderImportProcessor _processor;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public ImportReaderRowsCommandHandler(
        IApplicationDbContext db, ReaderImportProcessor processor, IDateTimeProvider clock, IAuditService audit)
    {
        _db = db;
        _processor = processor;
        _clock = clock;
        _audit = audit;
    }

    public async Task<ReaderImportRowsResultDto> Handle(ImportReaderRowsCommand command, CancellationToken ct)
    {
        if (command.Rows.Count == 0)
        {
            throw new ValidationException("rows", "Chưa có dòng nào để kiểm tra.");
        }

        if (command.Rows.Count > MaxRows)
        {
            throw new ValidationException(
                "rows", $"Sửa tại chỗ tối đa {MaxRows} dòng một lần; nhiều hơn thì sửa trong tệp rồi nhập lại.");
        }

        var sheet = ReaderImportRows.ToSheet(command.Rows);
        var outcome = await _processor.ProcessAsync(sheet, command.Options, command.DryRun, ct);

        if (!command.DryRun)
        {
            await _db.SaveChangesAsync(ct);

            // Vào sổ như một đợt nhập đã xong, để danh sách "các đợt nhập gần đây" không thiếu dòng nào.
            _db.ReaderImportBatches.Add(new ReaderImportBatch
            {
                FileName = string.IsNullOrWhiteSpace(command.FileName)
                    ? "Sửa tại chỗ trên bảng lỗi"
                    : $"{command.FileName.Trim()} (sửa tại chỗ)",
                TotalRows = outcome.TotalRows,
                SuccessRows = outcome.Created + outcome.Updated,
                ErrorRows = outcome.ErrorRows,
                Status = JobStatus.Completed,
                FinishedAt = _clock.Now,
                Errors = JsonSerializer.Serialize(outcome.Errors, ReaderImportJson.Options)
            });

            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(AuditAction.Create, "Reader", null,
                message: $"Nhập {outcome.Created + outcome.Updated} dòng bạn đọc sửa tại chỗ" +
                         $"{(outcome.ErrorRows > 0 ? $", còn {outcome.ErrorRows} dòng lỗi" : string.Empty)}",
                ct: ct);
        }

        return new ReaderImportRowsResultDto
        {
            DryRun = command.DryRun,
            TotalRows = outcome.TotalRows,
            Created = outcome.Created,
            Updated = outcome.Updated,
            Skipped = outcome.Skipped,
            ErrorRows = outcome.ErrorRows,
            Errors = outcome.Errors,
            ErrorRowCells = ReaderImportRows.ErrorCells(sheet, outcome)
        };
    }
}

/// <summary>Xếp hàng một đợt nhập bạn đọc để chạy nền.</summary>
public record StartReaderImportCommand(byte[] Content, string FileName, ReaderImportOptions Options)
    : IRequest<Guid>;

public class StartReaderImportCommandHandler : IRequestHandler<StartReaderImportCommand, Guid>
{
    public const string Bucket = "imports";

    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IBackgroundJobService _jobs;

    public StartReaderImportCommandHandler(
        IApplicationDbContext db, IFileStorage storage, IBackgroundJobService jobs)
    {
        _db = db;
        _storage = storage;
        _jobs = jobs;
    }

    public async Task<Guid> Handle(StartReaderImportCommand command, CancellationToken ct)
    {
        if (command.Content.Length == 0)
        {
            throw new Common.Exceptions.ValidationException("file", "Tệp rỗng.");
        }

        var batchId = Guid.NewGuid();
        var objectName = $"readers/{batchId:N}.xlsx";

        await _storage.EnsureBucketAsync(Bucket, ct);

        using (var stream = new MemoryStream(command.Content))
        {
            await _storage.UploadAsync(Bucket, objectName, stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ct);
        }

        _db.ReaderImportBatches.Add(new ReaderImportBatch
        {
            Id = batchId,
            FileName = command.FileName,
            Status = JobStatus.Pending,
            // Đường dẫn tệp và tùy chọn đi cùng bản ghi đợt nhập, để tác vụ nền chạy sau vẫn dựng lại
            // được đúng ngữ cảnh mà không cần giữ gì trong bộ nhớ tiến trình.
            Errors = JsonSerializer.Serialize(new ReaderImportState(objectName, command.Options))
        });

        await _db.SaveChangesAsync(ct);

        _jobs.Enqueue<IReaderImportRunner>(runner => runner.RunAsync(batchId, CancellationToken.None));

        return batchId;
    }
}

/// <summary>Thông tin đợt nhập lưu kèm bản ghi để tác vụ nền dựng lại ngữ cảnh.</summary>
public record ReaderImportState(string ObjectName, ReaderImportOptions Options);

public interface IReaderImportRunner
{
    Task RunAsync(Guid batchId, CancellationToken ct);
}

/// <summary>
/// Thực hiện một đợt nhập bạn đọc đã xếp hàng (VI.4).
///
/// Chạy nền vì một danh sách sinh viên toàn trường là vài chục nghìn dòng: giữ trình duyệt chờ chừng
/// đó là cầm chắc hết thời gian chờ của proxy, còn cán bộ thì không biết tệp đã vào được bao nhiêu.
/// </summary>
public class ReaderImportRunner : IReaderImportRunner
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IExcelService _excel;
    private readonly ReaderImportProcessor _processor;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<ReaderImportRunner> _logger;

    public ReaderImportRunner(
        IApplicationDbContext db,
        IFileStorage storage,
        IExcelService excel,
        ReaderImportProcessor processor,
        IDateTimeProvider clock,
        ILogger<ReaderImportRunner> logger)
    {
        _db = db;
        _storage = storage;
        _excel = excel;
        _processor = processor;
        _clock = clock;
        _logger = logger;
    }

    public async Task RunAsync(Guid batchId, CancellationToken ct)
    {
        var batch = await _db.ReaderImportBatches.FirstOrDefaultAsync(entity => entity.Id == batchId, ct);

        if (batch is null)
        {
            _logger.LogWarning("Không tìm thấy đợt nhập bạn đọc {BatchId}", batchId);
            return;
        }

        var state = ReadState(batch.Errors);

        if (state is null)
        {
            batch.Status = JobStatus.Failed;
            batch.FinishedAt = _clock.Now;
            batch.Errors = WriteErrors(new[]
            {
                new ReaderImportErrorDto(0, null, null, "Không đọc được thông tin của đợt nhập.")
            });

            await _db.SaveChangesAsync(ct);
            return;
        }

        batch.Status = JobStatus.Running;
        await _db.SaveChangesAsync(ct);

        try
        {
            await using var stream = await _storage.DownloadAsync(Bucket(), state.ObjectName, ct);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            buffer.Position = 0;

            var sheet = _excel.Read(buffer);
            var outcome = await _processor.ProcessAsync(sheet, state.Options, dryRun: false, ct);

            await _db.SaveChangesAsync(ct);

            batch.TotalRows = outcome.TotalRows;
            batch.SuccessRows = outcome.Created + outcome.Updated;
            batch.ErrorRows = outcome.ErrorRows;
            batch.Status = JobStatus.Completed;
            batch.FinishedAt = _clock.Now;
            batch.Errors = WriteErrors(outcome.Errors);

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Nhập bạn đọc {BatchId}: {Total} dòng, thêm {Created}, cập nhật {Updated}, lỗi {Errors}",
                batchId, outcome.TotalRows, outcome.Created, outcome.Updated, outcome.ErrorRows);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Đợt nhập bạn đọc {BatchId} thất bại", batchId);

            batch.Status = JobStatus.Failed;
            batch.FinishedAt = _clock.Now;
            batch.Errors = WriteErrors(new[]
            {
                new ReaderImportErrorDto(0, null, null, exception.Message)
            });

            await _db.SaveChangesAsync(ct);
        }
    }

    private static string Bucket() => StartReaderImportCommandHandler.Bucket;

    private static ReaderImportState? ReadState(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ReaderImportState>(json, ReaderImportJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string WriteErrors(IReadOnlyCollection<ReaderImportErrorDto> errors) =>
        JsonSerializer.Serialize(errors, ReaderImportJson.Options);
}

internal static class ReaderImportJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Cột lỗi mang hai loại nội dung theo từng giai đoạn: trước khi chạy là thông tin đợt nhập, sau
    /// khi chạy là danh sách lỗi. Đọc mềm để màn hình không vỡ khi đợt nhập còn đang xếp hàng.
    /// </summary>
    public static List<ReaderImportErrorDto> ReadErrors(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith('['))
        {
            return new List<ReaderImportErrorDto>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<ReaderImportErrorDto>>(json, Options)
                   ?? new List<ReaderImportErrorDto>();
        }
        catch (JsonException)
        {
            return new List<ReaderImportErrorDto>();
        }
    }
}

/// <summary>Danh sách các đợt nhập, mới nhất trước.</summary>
public record GetReaderImportBatchesQuery(PagedRequestDefault Request)
    : IRequest<PagedResult<ReaderImportBatchDto>>;

public class GetReaderImportBatchesQueryHandler
    : IRequestHandler<GetReaderImportBatchesQuery, PagedResult<ReaderImportBatchDto>>
{
    private readonly IApplicationDbContext _db;

    public GetReaderImportBatchesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<ReaderImportBatchDto>> Handle(
        GetReaderImportBatchesQuery query, CancellationToken ct)
    {
        var batches = await _db.ReaderImportBatches
            .AsNoTracking()
            .OrderByDescending(batch => batch.CreatedAt)
            .Skip(query.Request.Skip)
            .Take(query.Request.PageSize)
            .ToListAsync(ct);

        var total = await _db.ReaderImportBatches.CountAsync(ct);

        var items = batches.Select(Map).ToList();

        return new PagedResult<ReaderImportBatchDto>(
            items, total, query.Request.Page, query.Request.PageSize);
    }

    internal static ReaderImportBatchDto Map(ReaderImportBatch batch) => new()
    {
        Id = batch.Id,
        FileName = batch.FileName,
        TotalRows = batch.TotalRows,
        SuccessRows = batch.SuccessRows,
        ErrorRows = batch.ErrorRows,
        Status = batch.Status,
        CreatedAt = batch.CreatedAt,
        FinishedAt = batch.FinishedAt,
        Errors = ReaderImportJson.ReadErrors(batch.Errors)
    };
}

public record GetReaderImportBatchQuery(Guid Id) : IRequest<ReaderImportBatchDto>;

public class GetReaderImportBatchQueryHandler
    : IRequestHandler<GetReaderImportBatchQuery, ReaderImportBatchDto>
{
    private readonly IApplicationDbContext _db;

    public GetReaderImportBatchQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<ReaderImportBatchDto> Handle(GetReaderImportBatchQuery query, CancellationToken ct)
    {
        var batch = await _db.ReaderImportBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == query.Id, ct)
            ?? throw new NotFoundException("đợt nhập bạn đọc", query.Id);

        return GetReaderImportBatchesQueryHandler.Map(batch);
    }
}

/// <summary>Tải nhật ký lỗi của một đợt nhập ra Excel để sửa rồi nhập lại.</summary>
public record GetReaderImportErrorsQuery(Guid Id) : IRequest<PrintedFileDto>;

public class GetReaderImportErrorsQueryHandler : IRequestHandler<GetReaderImportErrorsQuery, PrintedFileDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IExcelService _excel;

    public GetReaderImportErrorsQueryHandler(IApplicationDbContext db, IExcelService excel)
    {
        _db = db;
        _excel = excel;
    }

    public async Task<PrintedFileDto> Handle(GetReaderImportErrorsQuery query, CancellationToken ct)
    {
        var batch = await _db.ReaderImportBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == query.Id, ct)
            ?? throw new NotFoundException("đợt nhập bạn đọc", query.Id);

        var errors = ReaderImportJson.ReadErrors(batch.Errors);

        var columns = new List<ExcelColumn<ReaderImportErrorDto>>
        {
            new("Dòng", error => error.Row, 8),
            new("Cột", error => error.Column, 25),
            new("Giá trị", error => error.Value, 30),
            new("Lỗi", error => error.Message, 70)
        };

        var content = _excel.Write("Lỗi nhập bạn đọc", columns, errors,
            $"NHẬT KÝ LỖI NHẬP BẠN ĐỌC — {batch.FileName}");

        return new PrintedFileDto(content, $"loi-nhap-ban-doc-{batch.Id:N}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}

/// <summary>Lưu và đọc lại hồ sơ ánh xạ cột để lần nhập sau khỏi làm lại từ đầu (VI.4).</summary>
public record SaveReaderImportMappingCommand(Dictionary<string, string> Mapping) : IRequest;

public class SaveReaderImportMappingCommandHandler : IRequestHandler<SaveReaderImportMappingCommand>
{
    public const string ParameterKey = "READER.IMPORT_MAPPING";

    private readonly ISystemParameterService _parameters;

    public SaveReaderImportMappingCommandHandler(ISystemParameterService parameters) =>
        _parameters = parameters;

    public async Task Handle(SaveReaderImportMappingCommand command, CancellationToken ct)
    {
        var known = command.Mapping
            .Where(pair => ReaderImportFields.DefaultHeaders.ContainsKey(pair.Key)
                           && !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value.Trim());

        await _parameters.SetAsync(ParameterKey, JsonSerializer.Serialize(known, ReaderImportJson.Options), ct);
    }
}

public record GetReaderImportMappingQuery : IRequest<Dictionary<string, string>>;

public class GetReaderImportMappingQueryHandler
    : IRequestHandler<GetReaderImportMappingQuery, Dictionary<string, string>>
{
    private readonly ISystemParameterService _parameters;

    public GetReaderImportMappingQueryHandler(ISystemParameterService parameters) =>
        _parameters = parameters;

    public async Task<Dictionary<string, string>> Handle(
        GetReaderImportMappingQuery query, CancellationToken ct)
    {
        var json = await _parameters.GetAsync(SaveReaderImportMappingCommandHandler.ParameterKey, ct);

        if (string.IsNullOrWhiteSpace(json))
        {
            return ReaderImportFields.DefaultHeaders.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        try
        {
            var saved = JsonSerializer.Deserialize<Dictionary<string, string>>(json, ReaderImportJson.Options)
                        ?? new Dictionary<string, string>();

            // Hồ sơ ánh xạ đã lưu có thể thiếu trường mới thêm về sau; bù bằng tiêu đề mặc định.
            foreach (var pair in ReaderImportFields.DefaultHeaders)
            {
                saved.TryAdd(pair.Key, pair.Value);
            }

            return saved;
        }
        catch (JsonException)
        {
            return ReaderImportFields.DefaultHeaders.ToDictionary(pair => pair.Key, pair => pair.Value);
        }
    }
}
