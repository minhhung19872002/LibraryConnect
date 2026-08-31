using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Digital;

// ---------------------------------------------------------------------------------------------
// V.4 — Bốn báo cáo thống kê tài liệu số. Mỗi báo cáo đều có bảng, số liệu vẽ biểu đồ và xuất được
// ra PDF lẫn Excel.
// ---------------------------------------------------------------------------------------------

public class DigitalReportFilter
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public Guid? CollectionId { get; set; }
    /// <summary>Số dòng của các bảng xếp hạng.</summary>
    public int Top { get; set; } = 10;
    /// <summary>Gộp theo NGAY | THANG | QUY | NAM.</summary>
    public string GroupBy { get; set; } = "THANG";
}

public record DigitalCountRowDto(string Label, int Count, long TotalSize);

public record DigitalUsageRowDto(string Label, int Views, int Downloads);

public record DigitalTopDocumentRowDto(
    Guid DocumentId, string Title, string? CollectionName, int Views, int Downloads);

public record DigitalTopReaderRowDto(
    Guid ReaderId, string ReaderName, string CardNumber, int Views, int Downloads);

/// <summary>Báo cáo 1: số lượng tài liệu theo bộ sưu tập, định dạng và mức truy cập.</summary>
public record DigitalInventoryReportDto(
    int TotalDocuments,
    long TotalSize,
    int WithText,
    int OcrProcessed,
    IReadOnlyList<DigitalCountRowDto> ByCollection,
    IReadOnlyList<DigitalCountRowDto> ByFormat,
    IReadOnlyList<DigitalCountRowDto> ByAccessLevel);

/// <summary>Báo cáo 2: lượt xem và lượt tải theo thời gian, theo tài liệu và theo bạn đọc.</summary>
public record DigitalUsageReportDto(
    int TotalViews,
    int TotalDownloads,
    IReadOnlyList<DigitalUsageRowDto> ByPeriod,
    IReadOnlyList<DigitalTopDocumentRowDto> TopDocuments,
    IReadOnlyList<DigitalTopReaderRowDto> TopReaders);

/// <summary>Báo cáo 3: dung lượng đã dùng.</summary>
public record DigitalStorageReportDto(
    long TotalSize,
    long OriginalSize,
    long DerivedSize,
    int FileCount,
    IReadOnlyList<DigitalCountRowDto> ByFormat);

/// <summary>Báo cáo 4: yêu cầu truy cập tài liệu hạn chế.</summary>
public record DigitalRequestReportDto(
    int Total,
    int Pending,
    int Approved,
    int Rejected,
    int Expired,
    double AverageProcessingHours,
    IReadOnlyList<DigitalUsageRowDto> ByPeriod);

public record GetDigitalInventoryReportQuery(DigitalReportFilter Filter)
    : IRequest<DigitalInventoryReportDto>;

public class GetDigitalInventoryReportQueryHandler
    : IRequestHandler<GetDigitalInventoryReportQuery, DigitalInventoryReportDto>
{
    private readonly IApplicationDbContext _db;

    public GetDigitalInventoryReportQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<DigitalInventoryReportDto> Handle(
        GetDigitalInventoryReportQuery query, CancellationToken ct)
    {
        var source = _db.DigitalDocuments.AsNoTracking().AsQueryable();

        if (query.Filter.CollectionId is { } collectionId)
        {
            source = source.Where(document => document.CollectionId == collectionId);
        }

        if (query.Filter.FromDate is { } from)
        {
            var start = from.ToDateTime(TimeOnly.MinValue);
            source = source.Where(document => document.UploadAt >= start);
        }

        if (query.Filter.ToDate is { } to)
        {
            var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
            source = source.Where(document => document.UploadAt < end);
        }

        var rows = await source
            .Select(document => new
            {
                document.CollectionId,
                CollectionName = document.Collection!.Name,
                document.MimeType,
                document.AccessLevel,
                document.FileSize,
                HasText = document.ExtractedText != null && document.ExtractedText != "",
                document.OcrProcessed,
            })
            .ToListAsync(ct);

        var byCollection = rows
            .GroupBy(row => row.CollectionName ?? "(Chưa xếp bộ sưu tập)")
            .Select(group => new DigitalCountRowDto(
                group.Key, group.Count(), group.Sum(row => row.FileSize)))
            .OrderByDescending(row => row.Count)
            .ToList();

        var byFormat = rows
            .GroupBy(row => DigitalStorage.FormatGroup(row.MimeType))
            .Select(group => new DigitalCountRowDto(
                group.Key, group.Count(), group.Sum(row => row.FileSize)))
            .OrderByDescending(row => row.Count)
            .ToList();

        var byAccess = rows
            .GroupBy(row => row.AccessLevel)
            .Select(group => new DigitalCountRowDto(
                DigitalReportLabels.AccessLevel(group.Key),
                group.Count(),
                group.Sum(row => row.FileSize)))
            .OrderByDescending(row => row.Count)
            .ToList();

        return new DigitalInventoryReportDto(
            rows.Count,
            rows.Sum(row => row.FileSize),
            rows.Count(row => row.HasText),
            rows.Count(row => row.OcrProcessed),
            byCollection,
            byFormat,
            byAccess);
    }
}

public record GetDigitalUsageReportQuery(DigitalReportFilter Filter) : IRequest<DigitalUsageReportDto>;

public class GetDigitalUsageReportQueryHandler
    : IRequestHandler<GetDigitalUsageReportQuery, DigitalUsageReportDto>
{
    private readonly IApplicationDbContext _db;

    public GetDigitalUsageReportQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<DigitalUsageReportDto> Handle(
        GetDigitalUsageReportQuery query, CancellationToken ct)
    {
        var filter = query.Filter;

        // Chỉ đếm lần mở tài liệu và lần tải, không đếm từng trang lật.
        var source = _db.DigitalAccessLogs
            .AsNoTracking()
            .Where(log => log.PageFrom == null);

        if (filter.CollectionId is { } collectionId)
        {
            source = source.Where(log => log.Document!.CollectionId == collectionId);
        }

        if (filter.FromDate is { } from)
        {
            var start = from.ToDateTime(TimeOnly.MinValue);
            source = source.Where(log => log.OccurredAt >= start);
        }

        if (filter.ToDate is { } to)
        {
            var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
            source = source.Where(log => log.OccurredAt < end);
        }

        var rows = await source
            .Select(log => new
            {
                log.DocumentId,
                DocumentTitle = log.Document!.Title,
                CollectionName = log.Document!.Collection!.Name,
                log.ReaderId,
                log.Action,
                log.OccurredAt,
            })
            .ToListAsync(ct);

        var byPeriod = rows
            .GroupBy(row => DigitalReportLabels.Period(row.OccurredAt, filter.GroupBy))
            .Select(group => new DigitalUsageRowDto(
                group.Key,
                group.Count(row => row.Action == DigitalAccessAction.View),
                group.Count(row => row.Action == DigitalAccessAction.Download)))
            .OrderBy(row => row.Label)
            .ToList();

        var top = Math.Clamp(filter.Top, 1, 100);

        var topDocuments = rows
            .GroupBy(row => new { row.DocumentId, row.DocumentTitle, row.CollectionName })
            .Select(group => new DigitalTopDocumentRowDto(
                group.Key.DocumentId,
                group.Key.DocumentTitle,
                group.Key.CollectionName,
                group.Count(row => row.Action == DigitalAccessAction.View),
                group.Count(row => row.Action == DigitalAccessAction.Download)))
            .OrderByDescending(row => row.Views + row.Downloads)
            .Take(top)
            .ToList();

        var readerIds = rows
            .Where(row => row.ReaderId is not null)
            .Select(row => row.ReaderId!.Value)
            .Distinct()
            .ToList();

        var readers = await _db.Readers
            .AsNoTracking()
            .Where(reader => readerIds.Contains(reader.Id))
            .Select(reader => new { reader.Id, reader.FullName, reader.CardNumber })
            .ToDictionaryAsync(reader => reader.Id, ct);

        var topReaders = rows
            .Where(row => row.ReaderId is not null)
            .GroupBy(row => row.ReaderId!.Value)
            .Select(group => new DigitalTopReaderRowDto(
                group.Key,
                readers.TryGetValue(group.Key, out var reader) ? reader.FullName : "(Không rõ)",
                readers.TryGetValue(group.Key, out var card) ? card.CardNumber : string.Empty,
                group.Count(row => row.Action == DigitalAccessAction.View),
                group.Count(row => row.Action == DigitalAccessAction.Download)))
            .OrderByDescending(row => row.Views + row.Downloads)
            .Take(top)
            .ToList();

        return new DigitalUsageReportDto(
            rows.Count(row => row.Action == DigitalAccessAction.View),
            rows.Count(row => row.Action == DigitalAccessAction.Download),
            byPeriod,
            topDocuments,
            topReaders);
    }
}

public record GetDigitalStorageReportQuery : IRequest<DigitalStorageReportDto>;

public class GetDigitalStorageReportQueryHandler
    : IRequestHandler<GetDigitalStorageReportQuery, DigitalStorageReportDto>
{
    private readonly IApplicationDbContext _db;

    public GetDigitalStorageReportQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<DigitalStorageReportDto> Handle(
        GetDigitalStorageReportQuery query, CancellationToken ct)
    {
        var files = await _db.DigitalDocumentFiles
            .AsNoTracking()
            .Select(file => new { file.Type, file.Size })
            .ToListAsync(ct);

        var documents = await _db.DigitalDocuments
            .AsNoTracking()
            .Select(document => new { document.MimeType, document.FileSize })
            .ToListAsync(ct);

        var byFormat = documents
            .GroupBy(document => DigitalStorage.FormatGroup(document.MimeType))
            .Select(group => new DigitalCountRowDto(
                group.Key, group.Count(), group.Sum(document => document.FileSize)))
            .OrderByDescending(row => row.TotalSize)
            .ToList();

        var original = files.Where(file => file.Type == DigitalFileType.Original).Sum(file => file.Size);
        var derived = files.Where(file => file.Type != DigitalFileType.Original).Sum(file => file.Size);

        return new DigitalStorageReportDto(
            original + derived, original, derived, files.Count, byFormat);
    }
}

public record GetDigitalRequestReportQuery(DigitalReportFilter Filter)
    : IRequest<DigitalRequestReportDto>;

public class GetDigitalRequestReportQueryHandler
    : IRequestHandler<GetDigitalRequestReportQuery, DigitalRequestReportDto>
{
    private readonly IApplicationDbContext _db;

    public GetDigitalRequestReportQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<DigitalRequestReportDto> Handle(
        GetDigitalRequestReportQuery query, CancellationToken ct)
    {
        var filter = query.Filter;
        var source = _db.DigitalAccessRequests.AsNoTracking().AsQueryable();

        if (filter.CollectionId is { } collectionId)
        {
            source = source.Where(request => request.Document!.CollectionId == collectionId);
        }

        if (filter.FromDate is { } from)
        {
            var start = from.ToDateTime(TimeOnly.MinValue);
            source = source.Where(request => request.RequestDate >= start);
        }

        if (filter.ToDate is { } to)
        {
            var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
            source = source.Where(request => request.RequestDate < end);
        }

        var rows = await source
            .Select(request => new { request.Status, request.RequestDate, request.ApprovedAt })
            .ToListAsync(ct);

        var processed = rows.Where(row => row.ApprovedAt is not null).ToList();

        var byPeriod = rows
            .GroupBy(row => DigitalReportLabels.Period(row.RequestDate, filter.GroupBy))
            .Select(group => new DigitalUsageRowDto(
                group.Key,
                group.Count(row => row.Status == AccessRequestStatus.Approved),
                group.Count(row => row.Status == AccessRequestStatus.Rejected)))
            .OrderBy(row => row.Label)
            .ToList();

        return new DigitalRequestReportDto(
            rows.Count,
            rows.Count(row => row.Status == AccessRequestStatus.Pending),
            rows.Count(row => row.Status == AccessRequestStatus.Approved),
            rows.Count(row => row.Status == AccessRequestStatus.Rejected),
            rows.Count(row => row.Status == AccessRequestStatus.Expired),
            processed.Count == 0
                ? 0
                : Math.Round(
                    processed.Average(row => (row.ApprovedAt!.Value - row.RequestDate).TotalHours), 1),
            byPeriod);
    }
}

/// <summary>Nhãn tiếng Việt và cách gộp kỳ, dùng chung cho cả bốn báo cáo.</summary>
internal static class DigitalReportLabels
{
    public static string AccessLevel(DigitalAccessLevel level) => level switch
    {
        DigitalAccessLevel.Public => "Công khai",
        DigitalAccessLevel.Internal => "Nội bộ",
        DigitalAccessLevel.Restricted => "Hạn chế",
        _ => "Cấm",
    };

    public static string Period(DateTimeOffset moment, string groupBy) => groupBy.ToUpperInvariant() switch
    {
        "NGAY" => moment.ToString("yyyy-MM-dd"),
        "QUY" => $"{moment.Year}-Q{(moment.Month - 1) / 3 + 1}",
        "NAM" => moment.Year.ToString(),
        _ => moment.ToString("yyyy-MM"),
    };
}

// ---------------------------------------------------------------------------------------------
// Xuất báo cáo
// ---------------------------------------------------------------------------------------------

public enum DigitalReportKind
{
    Inventory = 0,
    Usage = 1,
    Storage = 2,
    Requests = 3
}

public class ExportDigitalReportQuery : IRequest<PrintedFileDto>
{
    public DigitalReportKind Kind { get; set; }
    public bool AsPdf { get; set; }
    public DigitalReportFilter Filter { get; set; } = new();
}

public class ExportDigitalReportQueryHandler : IRequestHandler<ExportDigitalReportQuery, PrintedFileDto>
{
    private readonly ISender _mediator;
    private readonly IExcelService _excel;
    private readonly IPdfReportService _pdf;
    private readonly ISystemParameterService _parameters;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public ExportDigitalReportQueryHandler(
        ISender mediator,
        IExcelService excel,
        IPdfReportService pdf,
        ISystemParameterService parameters,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IAuditService audit)
    {
        _mediator = mediator;
        _excel = excel;
        _pdf = pdf;
        _parameters = parameters;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
    }

    public async Task<PrintedFileDto> Handle(ExportDigitalReportQuery query, CancellationToken ct)
    {
        var header = new PdfReportHeader
        {
            LibraryName = await _parameters.GetAsync("LIBRARY.NAME", "Thư viện", ct),
            LibraryAddress = await _parameters.GetAsync("LIBRARY.ADDRESS", ct),
            Criteria = BuildCriteria(query.Filter),
            PreparedBy = _currentUser.FullName,
            Landscape = true,
        };

        var file = query.Kind switch
        {
            DigitalReportKind.Inventory => await ExportInventoryAsync(query, header, ct),
            DigitalReportKind.Usage => await ExportUsageAsync(query, header, ct),
            DigitalReportKind.Storage => await ExportStorageAsync(query, header, ct),
            _ => await ExportRequestsAsync(query, header, ct),
        };

        await _audit.LogAsync(AuditAction.Export, "DigitalReport", null,
            message: $"Xuất báo cáo tài liệu số: {header.Title}", ct: ct);

        return file;
    }

    private static List<string> BuildCriteria(DigitalReportFilter filter)
    {
        var criteria = new List<string>();

        if (filter.FromDate is not null || filter.ToDate is not null)
        {
            criteria.Add(
                $"Thời gian: {filter.FromDate?.ToString("dd/MM/yyyy") ?? "…"} — "
                + $"{filter.ToDate?.ToString("dd/MM/yyyy") ?? "…"}");
        }

        return criteria;
    }

    private async Task<PrintedFileDto> ExportInventoryAsync(
        ExportDigitalReportQuery query, PdfReportHeader header, CancellationToken ct)
    {
        var report = await _mediator.Send(new GetDigitalInventoryReportQuery(query.Filter), ct);

        header.Title = "BÁO CÁO SỐ LƯỢNG TÀI LIỆU SỐ";
        header.Subtitle = $"{report.TotalDocuments:#,##0} tài liệu, "
            + $"{FormatSize(report.TotalSize)}, đã có văn bản tìm kiếm: {report.WithText:#,##0}";

        var rows = report.ByCollection
            .Select(row => new object[] { "Bộ sưu tập", row.Label, row.Count, row.TotalSize })
            .Concat(report.ByFormat
                .Select(row => new object[] { "Định dạng", row.Label, row.Count, row.TotalSize }))
            .Concat(report.ByAccessLevel
                .Select(row => new object[] { "Mức truy cập", row.Label, row.Count, row.TotalSize }))
            .ToList();

        if (query.AsPdf)
        {
            var columns = new List<PdfColumn<object[]>>
            {
                new("Chiều thống kê", row => (string)row[0], 1.2f),
                new("Giá trị", row => (string)row[1], 3f),
                new("Số tài liệu", row => Convert.ToInt32(row[2]).ToString("#,##0"), 1f, PdfAlign.Right),
                new("Dung lượng", row => FormatSize(Convert.ToInt64(row[3])), 1.2f, PdfAlign.Right),
            };

            return Pdf(_pdf.RenderTable(header, columns, rows), "bao-cao-tai-lieu-so");
        }

        var excelColumns = new List<ExcelColumn<object[]>>
        {
            new("Chiều thống kê", row => row[0], 18),
            new("Giá trị", row => row[1], 40),
            new("Số tài liệu", row => row[2], 14),
            new("Dung lượng (byte)", row => row[3], 18),
        };

        return Excel(
            _excel.Write("Tài liệu số", excelColumns, rows, $"{header.Title} — {header.Subtitle}"),
            "bao-cao-tai-lieu-so");
    }

    private async Task<PrintedFileDto> ExportUsageAsync(
        ExportDigitalReportQuery query, PdfReportHeader header, CancellationToken ct)
    {
        var report = await _mediator.Send(new GetDigitalUsageReportQuery(query.Filter), ct);

        header.Title = "BÁO CÁO LƯỢT XEM VÀ TẢI TÀI LIỆU SỐ";
        header.Subtitle = $"{report.TotalViews:#,##0} lượt xem, {report.TotalDownloads:#,##0} lượt tải";

        var rows = report.TopDocuments
            .Select(row => new object[]
            {
                row.Title, row.CollectionName ?? string.Empty, row.Views, row.Downloads,
            })
            .ToList();

        if (query.AsPdf)
        {
            var columns = new List<PdfColumn<object[]>>
            {
                new("Nhan đề", row => (string)row[0], 3.5f),
                new("Bộ sưu tập", row => (string)row[1], 2f),
                new("Lượt xem", row => Convert.ToInt32(row[2]).ToString("#,##0"), 1f, PdfAlign.Right),
                new("Lượt tải", row => Convert.ToInt32(row[3]).ToString("#,##0"), 1f, PdfAlign.Right),
            };

            return Pdf(_pdf.RenderTable(header, columns, rows), "bao-cao-luot-truy-cap");
        }

        var excelColumns = new List<ExcelColumn<object[]>>
        {
            new("Nhan đề", row => row[0], 50),
            new("Bộ sưu tập", row => row[1], 28),
            new("Lượt xem", row => row[2], 12),
            new("Lượt tải", row => row[3], 12),
        };

        return Excel(
            _excel.Write("Lượt truy cập", excelColumns, rows, $"{header.Title} — {header.Subtitle}"),
            "bao-cao-luot-truy-cap");
    }

    private async Task<PrintedFileDto> ExportStorageAsync(
        ExportDigitalReportQuery query, PdfReportHeader header, CancellationToken ct)
    {
        var report = await _mediator.Send(new GetDigitalStorageReportQuery(), ct);

        header.Title = "BÁO CÁO DUNG LƯỢNG LƯU TRỮ TÀI LIỆU SỐ";
        header.Subtitle = $"Tổng {FormatSize(report.TotalSize)} trong {report.FileCount:#,##0} tệp "
            + $"(bản gốc {FormatSize(report.OriginalSize)}, bản dẫn xuất {FormatSize(report.DerivedSize)})";

        var rows = report.ByFormat
            .Select(row => new object[] { row.Label, row.Count, row.TotalSize })
            .ToList();

        if (query.AsPdf)
        {
            var columns = new List<PdfColumn<object[]>>
            {
                new("Định dạng", row => (string)row[0], 2f),
                new("Số tài liệu", row => Convert.ToInt32(row[1]).ToString("#,##0"), 1f, PdfAlign.Right),
                new("Dung lượng", row => FormatSize(Convert.ToInt64(row[2])), 1.5f, PdfAlign.Right),
            };

            return Pdf(_pdf.RenderTable(header, columns, rows), "bao-cao-dung-luong");
        }

        var excelColumns = new List<ExcelColumn<object[]>>
        {
            new("Định dạng", row => row[0], 20),
            new("Số tài liệu", row => row[1], 14),
            new("Dung lượng (byte)", row => row[2], 20),
        };

        return Excel(
            _excel.Write("Dung lượng", excelColumns, rows, $"{header.Title} — {header.Subtitle}"),
            "bao-cao-dung-luong");
    }

    private async Task<PrintedFileDto> ExportRequestsAsync(
        ExportDigitalReportQuery query, PdfReportHeader header, CancellationToken ct)
    {
        var report = await _mediator.Send(new GetDigitalRequestReportQuery(query.Filter), ct);

        header.Title = "BÁO CÁO YÊU CẦU ĐỌC TÀI LIỆU HẠN CHẾ";
        header.Subtitle = $"{report.Total:#,##0} yêu cầu — chờ duyệt {report.Pending}, "
            + $"đã duyệt {report.Approved}, từ chối {report.Rejected}, "
            + $"xử lý trung bình {report.AverageProcessingHours} giờ";

        var rows = report.ByPeriod
            .Select(row => new object[] { row.Label, row.Views, row.Downloads })
            .ToList();

        if (query.AsPdf)
        {
            var columns = new List<PdfColumn<object[]>>
            {
                new("Kỳ", row => (string)row[0], 2f),
                new("Đã duyệt", row => Convert.ToInt32(row[1]).ToString("#,##0"), 1f, PdfAlign.Right),
                new("Từ chối", row => Convert.ToInt32(row[2]).ToString("#,##0"), 1f, PdfAlign.Right),
            };

            return Pdf(_pdf.RenderTable(header, columns, rows), "bao-cao-yeu-cau-doc");
        }

        var excelColumns = new List<ExcelColumn<object[]>>
        {
            new("Kỳ", row => row[0], 18),
            new("Đã duyệt", row => row[1], 14),
            new("Từ chối", row => row[2], 14),
        };

        return Excel(
            _excel.Write("Yêu cầu đọc", excelColumns, rows, $"{header.Title} — {header.Subtitle}"),
            "bao-cao-yeu-cau-doc");
    }

    /// <summary>Đổi số byte sang đơn vị người đọc hiểu ngay.</summary>
    internal static string FormatSize(long bytes)
    {
        string[] units = { "byte", "KB", "MB", "GB", "TB" };
        double value = bytes;
        var unit = 0;

        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0 ? $"{bytes:#,##0} byte" : $"{value:#,##0.0} {units[unit]}";
    }

    private PrintedFileDto Pdf(byte[] content, string name) =>
        new(content, $"{name}-{_clock.Today:yyyyMMdd}.pdf", "application/pdf");

    private PrintedFileDto Excel(byte[] content, string name) =>
        new(content, $"{name}-{_clock.Today:yyyyMMdd}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
}
