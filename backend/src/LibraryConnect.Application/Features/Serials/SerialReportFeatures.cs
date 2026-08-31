using System.Globalization;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Admin.AuditLogs;
using LibraryConnect.Domain.Entities.Ser;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Serials;

// ---------------------------------------------------------------------------------------------
// IV.5 — Báo cáo thống kê ấn phẩm định kỳ.
// ---------------------------------------------------------------------------------------------

public class SerialReportFilter
{
    public int? Year { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? SupplierId { get; set; }
    public SerialFrequency? Frequency { get; set; }
    public bool? ActiveOnly { get; set; }
}

/// <summary>Một dòng của báo cáo thống kê ấn phẩm định kỳ.</summary>
public class SerialStatRowDto
{
    public string Label { get; set; } = string.Empty;
    /// <summary>Số đầu báo trong nhóm.</summary>
    public int TitleCount { get; set; }
    /// <summary>Số kỳ đã nhận, tính cả các kỳ đã đóng tập.</summary>
    public int ReceivedIssues { get; set; }
    public int MissingIssues { get; set; }
    /// <summary>Số bản thực nhận — một kỳ có thể đặt nhiều bản.</summary>
    public int Copies { get; set; }
    public decimal Value { get; set; }
    public double Percent { get; set; }
}

public class SerialStatReportDto
{
    public string Title { get; set; } = string.Empty;
    public string DimensionName { get; set; } = string.Empty;
    public IReadOnlyList<SerialStatRowDto> Rows { get; set; } = Array.Empty<SerialStatRowDto>();
    public int TotalTitles { get; set; }
    public int TotalReceivedIssues { get; set; }
    public int TotalMissingIssues { get; set; }
    public decimal TotalValue { get; set; }
}

/// <summary>Bốn chiều thống kê mà đặc tả IV.5 yêu cầu.</summary>
public static class SerialDimensions
{
    /// <summary>Tổng hợp: một dòng cho toàn bộ, để đọc nhanh con số chung.</summary>
    public const string Overall = "OVERALL";
    /// <summary>Theo môn loại — chỉ số DDC của biểu ghi đầu báo.</summary>
    public const string Classification = "DDC";
    public const string Frequency = "FREQUENCY";
    public const string Language = "LANGUAGE";
    public const string Supplier = "SUPPLIER";
    public const string Warehouse = "WAREHOUSE";

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [Overall] = "Tổng hợp",
        [Classification] = "Môn loại (DDC)",
        [Frequency] = "Mức định kỳ",
        [Language] = "Ngôn ngữ",
        [Supplier] = "Nhà cung cấp",
        [Warehouse] = "Kho"
    };
}

public record GetSerialStatsQuery(string Dimension, SerialReportFilter Filter)
    : IRequest<SerialStatReportDto>;

public class GetSerialStatsQueryHandler : IRequestHandler<GetSerialStatsQuery, SerialStatReportDto>
{
    private readonly IApplicationDbContext _db;

    public GetSerialStatsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<SerialStatReportDto> Handle(GetSerialStatsQuery query, CancellationToken ct)
    {
        if (!SerialDimensions.Labels.TryGetValue(query.Dimension, out var dimensionName))
        {
            throw new Common.Exceptions.ValidationException("dimension", "Chiều thống kê không hợp lệ.");
        }

        var filter = query.Filter;

        // Kéo về từng đầu báo kèm số liệu kỳ, rồi mới nhóm: các chiều thống kê nằm ở đầu báo còn số
        // liệu nằm ở kỳ, nên gộp một lần ở đây gọn hơn là viết sáu câu truy vấn khác nhau.
        var rows = await _db.Serials
            .AsNoTracking()
            .WhereIf(filter.WarehouseId is not null, serial => serial.WarehouseId == filter.WarehouseId)
            .WhereIf(filter.SupplierId is not null, serial => serial.SupplierId == filter.SupplierId)
            .WhereIf(filter.Frequency is not null, serial => serial.Frequency == filter.Frequency)
            .WhereIf(filter.ActiveOnly == true, serial => serial.IsActive)
            .Select(serial => new SerialFact
            {
                Ddc = serial.Bib!.Ddc,
                Frequency = serial.Frequency,
                Language = serial.Language!.Name,
                Supplier = serial.Supplier!.Name,
                Warehouse = serial.Warehouse!.Name,
                Price = serial.PricePerIssue ?? 0,
                Received = serial.Issues.Count(issue =>
                    (issue.Status == SerialIssueStatus.Received || issue.Status == SerialIssueStatus.Bound)
                    && (filter.Year == null || issue.Year == filter.Year)),
                Missing = serial.Issues.Count(issue =>
                    (issue.Status == SerialIssueStatus.Missing || issue.Status == SerialIssueStatus.Claimed)
                    && (filter.Year == null || issue.Year == filter.Year)),
                Copies = serial.Issues
                    .Where(issue => filter.Year == null || issue.Year == filter.Year)
                    .Sum(issue => issue.Quantity)
            })
            .ToListAsync(ct);

        var grouped = rows
            .GroupBy(fact => Key(fact, query.Dimension))
            .Select(group => new SerialStatRowDto
            {
                Label = group.Key,
                TitleCount = group.Count(),
                ReceivedIssues = group.Sum(fact => fact.Received),
                MissingIssues = group.Sum(fact => fact.Missing),
                Copies = group.Sum(fact => fact.Copies),
                Value = group.Sum(fact => fact.Copies * fact.Price)
            })
            .OrderByDescending(row => row.ReceivedIssues)
            .ThenBy(row => row.Label, StringComparer.CurrentCulture)
            .ToList();

        var totalReceived = grouped.Sum(row => row.ReceivedIssues);

        foreach (var row in grouped)
        {
            row.Percent = totalReceived == 0
                ? 0
                : Math.Round(row.ReceivedIssues * 100.0 / totalReceived, 1);
        }

        return new SerialStatReportDto
        {
            Title = query.Dimension == SerialDimensions.Overall
                ? "Tổng hợp ấn phẩm định kỳ"
                : $"Thống kê ấn phẩm định kỳ theo {dimensionName.ToLowerInvariant()}",
            DimensionName = dimensionName,
            Rows = grouped,
            TotalTitles = rows.Count,
            TotalReceivedIssues = totalReceived,
            TotalMissingIssues = grouped.Sum(row => row.MissingIssues),
            TotalValue = grouped.Sum(row => row.Value)
        };
    }

    private static string Key(SerialFact fact, string dimension)
    {
        var value = dimension switch
        {
            SerialDimensions.Overall => "Toàn bộ ấn phẩm định kỳ",
            // Gộp theo lớp trăm của DDC: "005.74" và "005.13" cùng thuộc môn loại 000, và báo cáo
            // môn loại mà liệt kê từng chỉ số lẻ thì dài mà không nói lên điều gì.
            SerialDimensions.Classification => MainClass(fact.Ddc),
            SerialDimensions.Frequency => FrequencyLabels.Of(fact.Frequency),
            SerialDimensions.Language => fact.Language,
            SerialDimensions.Supplier => fact.Supplier,
            _ => fact.Warehouse
        };

        return string.IsNullOrWhiteSpace(value) ? "(Chưa xác định)" : value;
    }

    /// <summary>Lớp chính của một chỉ số DDC, ví dụ "005.74" → "000 — Tin học, thông tin".</summary>
    private static string MainClass(string? ddc)
    {
        if (string.IsNullOrWhiteSpace(ddc) || !char.IsDigit(ddc[0]))
        {
            return string.Empty;
        }

        var digit = ddc[0] - '0';

        var names = new[]
        {
            "Tin học, thông tin và tác phẩm tổng quát",
            "Triết học và tâm lý học",
            "Tôn giáo",
            "Khoa học xã hội",
            "Ngôn ngữ",
            "Khoa học tự nhiên và toán học",
            "Công nghệ",
            "Nghệ thuật và giải trí",
            "Văn học",
            "Lịch sử và địa lý"
        };

        return $"{digit}00 — {names[digit]}";
    }

    private class SerialFact
    {
        public string? Ddc { get; init; }
        public SerialFrequency Frequency { get; init; }
        public string? Language { get; init; }
        public string? Supplier { get; init; }
        public string? Warehouse { get; init; }
        public decimal Price { get; init; }
        public int Received { get; init; }
        public int Missing { get; init; }
        public int Copies { get; init; }
    }
}

/// <summary>Xuất báo cáo ấn phẩm định kỳ ra Excel hoặc PDF (IV.5).</summary>
public record ExportSerialReportQuery(string Dimension, SerialReportFilter Filter, ExportFormat Format)
    : IRequest<ExportedFile>;

public class ExportSerialReportQueryHandler : IRequestHandler<ExportSerialReportQuery, ExportedFile>
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    private readonly IMediator _mediator;
    private readonly IExcelService _excel;
    private readonly IPdfReportService _pdf;
    private readonly ISystemParameterService _parameters;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public ExportSerialReportQueryHandler(
        IMediator mediator,
        IExcelService excel,
        IPdfReportService pdf,
        ISystemParameterService parameters,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _mediator = mediator;
        _excel = excel;
        _pdf = pdf;
        _parameters = parameters;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<ExportedFile> Handle(ExportSerialReportQuery query, CancellationToken ct)
    {
        var report = await _mediator.Send(new GetSerialStatsQuery(query.Dimension, query.Filter), ct);

        var criteria = new List<string>();

        if (query.Filter.Year is not null)
        {
            criteria.Add($"Năm {query.Filter.Year}");
        }

        var header = new PdfReportHeader
        {
            LibraryName = await _parameters.GetAsync("LIBRARY.NAME", "Thư viện", ct),
            LibraryAddress = await _parameters.GetAsync("LIBRARY.ADDRESS", ct),
            Title = report.Title.ToUpperInvariant(),
            Criteria = criteria,
            PreparedBy = _currentUser.FullName
        };

        var name = $"thong-ke-an-pham-dinh-ky-{query.Dimension.ToLowerInvariant()}-{_clock.Today:yyyyMMdd}";

        if (query.Format == ExportFormat.Excel)
        {
            var columns = new List<ExcelColumn<SerialStatRowDto>>
            {
                new(report.DimensionName, row => row.Label, 40),
                new("Số đầu báo", row => row.TitleCount, 14),
                new("Số kỳ đã nhận", row => row.ReceivedIssues, 16),
                new("Số kỳ thiếu", row => row.MissingIssues, 14),
                new("Số bản", row => row.Copies, 12),
                new("Tỷ trọng (%)", row => row.Percent, 14, "#,##0.0"),
                new("Giá trị (VNĐ)", row => row.Value, 18, "#,##0")
            };

            return new ExportedFile(
                _excel.Write("Ấn phẩm định kỳ", columns, report.Rows, header.Title),
                $"{name}.xlsx",
                ExportedFile.ExcelContentType);
        }

        var pdfColumns = new List<PdfColumn<SerialStatRowDto>>
        {
            new(report.DimensionName, row => row.Label, 3f),
            new("Đầu báo", row => row.TitleCount.ToString("#,##0", Vietnamese), 1f, PdfAlign.Right),
            new("Kỳ đã nhận", row => row.ReceivedIssues.ToString("#,##0", Vietnamese), 1.1f, PdfAlign.Right),
            new("Kỳ thiếu", row => row.MissingIssues.ToString("#,##0", Vietnamese), 1f, PdfAlign.Right),
            new("Số bản", row => row.Copies.ToString("#,##0", Vietnamese), 1f, PdfAlign.Right),
            new("Giá trị", row => row.Value.ToString("#,##0", Vietnamese), 1.4f, PdfAlign.Right)
        };

        return new ExportedFile(
            _pdf.RenderTable(header, pdfColumns, report.Rows), $"{name}.pdf", ExportedFile.PdfContentType);
    }
}
