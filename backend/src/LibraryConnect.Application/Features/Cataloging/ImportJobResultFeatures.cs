using System.Text;
using System.Text.Json;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Marc;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>
/// Giới hạn dung lượng tệp nhập biểu ghi, đọc từ tham số hệ thống thay vì một hằng số trong mã.
///
/// Trước đây hai endpoint nhập ghim cứng 100 MB và 50 MB; thư viện có tệp trao đổi lớn hơn thì
/// phải sửa mã nguồn. Tham số <c>UPLOAD.IMPORT_MAX_SIZE_MB</c> là chỗ đổi.
/// </summary>
public static class ImportFileLimit
{
    public const string ParameterKey = "UPLOAD.IMPORT_MAX_SIZE_MB";
    public const int DefaultMaxMb = 100;

    /// <summary>Ném lỗi kiểm tra dữ liệu, bằng tiếng Việt, khi tệp vượt giới hạn.</summary>
    public static void EnsureWithinLimit(long lengthBytes, int maxMb, string fileName)
    {
        var limit = Math.Max(1, maxMb);

        if (lengthBytes > limit * 1024L * 1024L)
        {
            throw new ValidationException("file",
                $"Tệp \"{fileName}\" nặng {lengthBytes / 1024.0 / 1024.0:0.#} MB, vượt giới hạn {limit} MB. " +
                $"Tách tệp nhỏ hơn, hoặc nâng tham số {ParameterKey} trong Tham số hệ thống.");
        }
    }

    public static async Task EnsureWithinLimitAsync(
        ISystemParameterService parameters, long lengthBytes, string fileName, CancellationToken ct)
    {
        var maxMb = await parameters.GetAsync(ParameterKey, DefaultMaxMb, ct);
        EnsureWithinLimit(lengthBytes, maxMb, fileName);
    }
}

// ---------------------------------------------------------------------------
// Tệp kết quả của một lượt nhập (II.6 bước 4, II.8): các dòng lỗi kèm tổng kết
// ---------------------------------------------------------------------------

public record GetImportJobResultFileQuery(Guid JobId, string Format) : IRequest<MarcExportFileDto>;

public class GetImportJobResultFileQueryHandler : IRequestHandler<GetImportJobResultFileQuery, MarcExportFileDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IExcelService _excel;

    public GetImportJobResultFileQueryHandler(IApplicationDbContext db, IExcelService excel)
    {
        _db = db;
        _excel = excel;
    }

    public async Task<MarcExportFileDto> Handle(GetImportJobResultFileQuery query, CancellationToken ct)
    {
        var job = await _db.ImportExportJobs.AsNoTracking().FirstOrDefaultAsync(item => item.Id == query.JobId, ct)
                  ?? throw new NotFoundException("Không tìm thấy tác vụ nhập dữ liệu.");

        var dto = ImportJobMapper.ToDto(job);
        var stem = Path.GetFileNameWithoutExtension(job.FileName ?? "nhap-du-lieu");
        var summary = $"Kết quả nhập \"{job.FileName}\": {dto.Total} dòng, {dto.Success} thành công, " +
                      $"{dto.Skipped} bỏ qua, {dto.Failed} lỗi.";

        if (string.Equals(query.Format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            return new MarcExportFileDto
            {
                Content = WriteCsv(summary, dto.Errors),
                FileName = $"ket-qua-{stem}.csv",
                ContentType = "text/csv; charset=utf-8"
            };
        }

        var columns = new List<ExcelColumn<ImportJobErrorDto>>
        {
            new("Dòng / biểu ghi số", error => error.Row, 18),
            new("Số kiểm soát", error => error.Identifier, 22),
            new("Lý do", error => error.Message, 90)
        };

        return new MarcExportFileDto
        {
            Content = _excel.Write("Dòng lỗi", columns, dto.Errors, summary),
            FileName = $"ket-qua-{stem}.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    /// <summary>CSV có BOM để Excel trên Windows mở đúng tiếng Việt.</summary>
    private static byte[] WriteCsv(string summary, IReadOnlyList<ImportJobErrorDto> errors)
    {
        var builder = new StringBuilder();
        builder.AppendLine(Quote(summary));
        builder.AppendLine("Dòng / biểu ghi số,Số kiểm soát,Lý do");

        foreach (var error in errors)
        {
            builder.AppendLine($"{error.Row},{Quote(error.Identifier ?? string.Empty)},{Quote(error.Message)}");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();

        static string Quote(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
    }
}

// ---------------------------------------------------------------------------
// Sửa các dòng lỗi ngay trên màn hình rồi nhập lại (II.8)
// ---------------------------------------------------------------------------

/// <summary>Một dòng bảng tính đã lỗi, kèm nội dung ô để sửa tại chỗ.</summary>
public class ExcelFailedRowDto
{
    public int RowNumber { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string> Cells { get; set; } = new();
}

public class ExcelFailedRowsDto
{
    public List<string> Headers { get; set; } = new();
    public List<ExcelFailedRowDto> Rows { get; set; } = new();
}

/// <summary>Các dòng đã lỗi của một lượt nhập Excel, đọc lại từ chính tệp đã tải lên.</summary>
public record GetExcelImportFailedRowsQuery(Guid JobId) : IRequest<ExcelFailedRowsDto>;

public class GetExcelImportFailedRowsQueryHandler : IRequestHandler<GetExcelImportFailedRowsQuery, ExcelFailedRowsDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IExcelService _excel;

    public GetExcelImportFailedRowsQueryHandler(IApplicationDbContext db, IFileStorage storage, IExcelService excel)
    {
        _db = db;
        _storage = storage;
        _excel = excel;
    }

    public async Task<ExcelFailedRowsDto> Handle(GetExcelImportFailedRowsQuery query, CancellationToken ct)
    {
        var job = await _db.ImportExportJobs.AsNoTracking().FirstOrDefaultAsync(item => item.Id == query.JobId, ct)
                  ?? throw new NotFoundException("Không tìm thấy tác vụ nhập dữ liệu.");

        if (job.Type != ImportExportJobType.ExcelIn || string.IsNullOrWhiteSpace(job.FilePath))
        {
            throw new ValidationException("jobId", "Chỉ lượt nhập từ bảng tính Excel mới sửa được dòng lỗi tại chỗ.");
        }

        var errors = ImportJobMapper.ToDto(job).Errors
            .Where(error => error.Row > 0)
            .GroupBy(error => error.Row)
            .ToDictionary(group => group.Key, group => string.Join(" ", group.Select(error => error.Message)));

        ExcelSheet sheet;

        await using (var stream = await _storage.DownloadAsync(StartBibImportCommandHandler.Bucket, job.FilePath, ct))
        using (var buffer = new MemoryStream())
        {
            await stream.CopyToAsync(buffer, ct);
            buffer.Position = 0;
            sheet = _excel.Read(buffer);
        }

        return new ExcelFailedRowsDto
        {
            Headers = sheet.Headers.ToList(),
            Rows = sheet.Rows
                .Where(row => errors.ContainsKey(row.RowNumber))
                .Select(row => new ExcelFailedRowDto
                {
                    RowNumber = row.RowNumber,
                    Message = errors[row.RowNumber],
                    Cells = sheet.Headers.ToDictionary(header => header, row.Get)
                })
                .ToList()
        };
    }
}

public class ExcelRetryRow
{
    public int RowNumber { get; set; }
    public Dictionary<string, string> Cells { get; set; } = new();
}

/// <summary>
/// Nhập lại những dòng cán bộ đã sửa trên màn hình: dựng một bảng tính mới chỉ gồm các dòng ấy,
/// rồi xếp vào hàng đợi với đúng ánh xạ và tùy chọn của lượt nhập gốc.
/// </summary>
public record RetryExcelImportRowsCommand(Guid JobId, List<ExcelRetryRow> Rows) : IRequest<Guid>;

public class RetryExcelImportRowsCommandHandler : IRequestHandler<RetryExcelImportRowsCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IExcelService _excel;
    private readonly ISender _sender;

    public RetryExcelImportRowsCommandHandler(IApplicationDbContext db, IExcelService excel, ISender sender)
    {
        _db = db;
        _excel = excel;
        _sender = sender;
    }

    public async Task<Guid> Handle(RetryExcelImportRowsCommand request, CancellationToken ct)
    {
        if (request.Rows.Count == 0)
        {
            throw new ValidationException("rows", "Chưa có dòng nào để nhập lại.");
        }

        var job = await _db.ImportExportJobs.AsNoTracking().FirstOrDefaultAsync(item => item.Id == request.JobId, ct)
                  ?? throw new NotFoundException("Không tìm thấy tác vụ nhập dữ liệu.");

        if (job.Type != ImportExportJobType.ExcelIn)
        {
            throw new ValidationException("jobId", "Chỉ lượt nhập từ bảng tính Excel mới nhập lại được dòng đã sửa.");
        }

        var options = JsonSerializer.Deserialize<ExcelImportOptions>(
                          job.Options ?? "{}",
                          new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                      ?? new ExcelImportOptions();

        // Columns follow the mapping so every mapped column is present even when a row omits it.
        var headers = request.Rows
            .SelectMany(row => row.Cells.Keys)
            .Concat(options.Mapping.Select(mapping => mapping.Column))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var columns = headers
            .Select(header => new ExcelColumn<ExcelRetryRow>(
                header,
                row => row.Cells.FirstOrDefault(cell => string.Equals(cell.Key, header, StringComparison.OrdinalIgnoreCase)).Value))
            .ToList();

        var content = _excel.Write("Biểu ghi", columns, request.Rows.OrderBy(row => row.RowNumber));
        var stem = Path.GetFileNameWithoutExtension(job.FileName ?? "bang-tinh");

        return await _sender.Send(
            new StartBibExcelImportCommand(content, $"{stem}-sua-{request.Rows.Count}-dong.xlsx", options), ct);
    }
}
