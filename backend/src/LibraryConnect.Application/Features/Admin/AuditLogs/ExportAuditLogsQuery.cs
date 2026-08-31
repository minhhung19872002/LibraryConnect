using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Admin.AuditLogs;

public enum ExportFormat { Excel, Pdf }

/// <summary>File xuất ra kèm tên và kiểu nội dung, dùng chung cho mọi chức năng xuất báo cáo.</summary>
public record ExportedFile(byte[] Content, string FileName, string ContentType)
{
    public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string PdfContentType = "application/pdf";
}

/// <summary>
/// Xuất nhật ký hệ thống ra Excel hoặc PDF (I.4).
///
/// The filter is the same object the grid uses, so what the operator exports is exactly what they
/// were looking at. The row count is capped because the audit table is retained forever and an
/// unbounded export would exhaust memory.
/// </summary>
public record ExportAuditLogsQuery(AuditLogListRequest Filter, ExportFormat Format) : IRequest<ExportedFile>;

public class ExportAuditLogsQueryHandler : IRequestHandler<ExportAuditLogsQuery, ExportedFile>
{
    private const int MaxExportRows = 50_000;

    private readonly IApplicationDbContext _db;
    private readonly IExcelService _excel;
    private readonly IPdfReportService _pdf;
    private readonly ISystemParameterService _parameters;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public ExportAuditLogsQueryHandler(
        IApplicationDbContext db,
        IExcelService excel,
        IPdfReportService pdf,
        ISystemParameterService parameters,
        ICurrentUser currentUser,
        IAuditService audit)
    {
        _db = db;
        _excel = excel;
        _pdf = pdf;
        _parameters = parameters;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<ExportedFile> Handle(ExportAuditLogsQuery request, CancellationToken ct)
    {
        var rows = await GetAuditLogsQueryHandler.BuildQuery(_db, request.Filter)
            .Take(MaxExportRows)
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            row.ActionLabel = AuditLabels.Action(row.Action);
            row.EntityLabel = AuditLabels.Entity(row.Entity);
        }

        var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmm");

        var file = request.Format == ExportFormat.Excel
            ? BuildExcel(rows, timestamp)
            : await BuildPdfAsync(rows, request.Filter, timestamp, ct);

        // Exporting the audit trail is itself an auditable event (section 6.2).
        await _audit.LogAsync(AuditAction.Export, "AuditLog", null, file.FileName,
            message: $"Xuất {rows.Count} bản ghi nhật ký ra {request.Format}", ct: ct);

        return file;
    }

    private ExportedFile BuildExcel(IReadOnlyList<AuditLogListItemDto> rows, string timestamp)
    {
        var columns = new List<ExcelColumn<AuditLogListItemDto>>
        {
            new("Thời điểm", r => r.OccurredAt, 18, "dd/MM/yyyy HH:mm:ss"),
            new("Người dùng", r => r.Username ?? "-", 18),
            new("Hành động", r => r.ActionLabel, 18),
            new("Đối tượng", r => r.EntityLabel, 22),
            new("Bản ghi", r => r.EntityDisplay ?? r.EntityId ?? "-", 34),
            new("Kết quả", r => r.Result ? "Thành công" : "Thất bại", 12),
            new("Ghi chú", r => r.Message ?? string.Empty, 40),
            new("Địa chỉ IP", r => r.Ip ?? string.Empty, 16)
        };

        var content = _excel.Write("Nhật ký hệ thống", columns, rows, "NHẬT KÝ HỆ THỐNG");
        return new ExportedFile(content, $"nhat-ky-he-thong-{timestamp}.xlsx", ExportedFile.ExcelContentType);
    }

    private async Task<ExportedFile> BuildPdfAsync(
        IReadOnlyList<AuditLogListItemDto> rows, AuditLogListRequest filter, string timestamp, CancellationToken ct)
    {
        var header = new PdfReportHeader
        {
            LibraryName = await _parameters.GetAsync("LIBRARY.NAME", "Thư viện", ct),
            LibraryAddress = await _parameters.GetAsync("LIBRARY.ADDRESS", string.Empty, ct),
            Title = "Nhật ký hệ thống",
            Criteria = DescribeFilter(filter),
            PreparedBy = _currentUser.FullName ?? _currentUser.Username,
            Landscape = true
        };

        var columns = new List<PdfColumn<AuditLogListItemDto>>
        {
            new("Thời điểm", r => r.OccurredAt.ToLocalTime().ToString("HH:mm:ss dd/MM/yyyy"), 1.4f),
            new("Người dùng", r => r.Username ?? "-", 1.1f),
            new("Hành động", r => r.ActionLabel, 1.1f),
            new("Đối tượng", r => r.EntityLabel, 1.2f),
            new("Bản ghi", r => r.EntityDisplay ?? r.EntityId ?? "-", 2f),
            new("Kết quả", r => r.Result ? "Thành công" : "Thất bại", 0.9f, PdfAlign.Center),
            new("IP", r => r.Ip ?? string.Empty, 1f)
        };

        var content = _pdf.RenderTable(header, columns, rows);
        return new ExportedFile(content, $"nhat-ky-he-thong-{timestamp}.pdf", ExportedFile.PdfContentType);
    }

    /// <summary>Prints the applied filter on the report so a signed printout is self-describing.</summary>
    private static List<string> DescribeFilter(AuditLogListRequest filter)
    {
        var criteria = new List<string>();

        if (filter.FromDate.HasValue || filter.ToDate.HasValue)
        {
            var from = filter.FromDate?.ToString("dd/MM/yyyy") ?? "đầu kỳ";
            var to = filter.ToDate?.ToString("dd/MM/yyyy") ?? "nay";
            criteria.Add($"Khoảng thời gian: từ {from} đến {to}");
        }

        if (!string.IsNullOrWhiteSpace(filter.Username))
        {
            criteria.Add($"Người dùng: {filter.Username}");
        }

        if (filter.Action.HasValue)
        {
            criteria.Add($"Hành động: {AuditLabels.Action(filter.Action.Value)}");
        }

        if (!string.IsNullOrWhiteSpace(filter.Entity))
        {
            criteria.Add($"Đối tượng: {AuditLabels.Entity(filter.Entity)}");
        }

        if (filter.Result.HasValue)
        {
            criteria.Add($"Kết quả: {(filter.Result.Value ? "Thành công" : "Thất bại")}");
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            criteria.Add($"Từ khóa: {filter.Keyword}");
        }

        return criteria;
    }
}
