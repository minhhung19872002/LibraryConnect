using System.Globalization;
using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Entities.Ser;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Serials;

// ---------------------------------------------------------------------------------------------
// IV.4 — Đóng tập và bảng tổng hợp tình hình nhận số.
// ---------------------------------------------------------------------------------------------

public class SerialBindingDto
{
    public Guid Id { get; set; }
    public Guid SerialId { get; set; }
    public string SerialTitle { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? FromIssue { get; set; }
    public string? ToIssue { get; set; }
    public int Year { get; set; }
    public DateOnly BindingDate { get; set; }
    public int IssueCount { get; set; }
    public Guid? ItemId { get; set; }
    public string? Barcode { get; set; }
    public string? CallNumber { get; set; }
    public string? Note { get; set; }
}

public record GetSerialBindingsQuery(Guid? SerialId, int? Year) : IRequest<IReadOnlyList<SerialBindingDto>>;

public class GetSerialBindingsQueryHandler
    : IRequestHandler<GetSerialBindingsQuery, IReadOnlyList<SerialBindingDto>>
{
    private readonly IApplicationDbContext _db;

    public GetSerialBindingsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<SerialBindingDto>> Handle(
        GetSerialBindingsQuery query, CancellationToken ct) =>
        await _db.SerialBindings
            .AsNoTracking()
            .WhereIf(query.SerialId is not null, binding => binding.SerialId == query.SerialId)
            .WhereIf(query.Year is not null, binding => binding.Year == query.Year)
            .OrderByDescending(binding => binding.Year)
            .ThenByDescending(binding => binding.BindingDate)
            .Select(binding => new SerialBindingDto
            {
                Id = binding.Id,
                SerialId = binding.SerialId,
                SerialTitle = binding.Serial!.Title,
                Code = binding.Code,
                FromIssue = binding.FromIssue,
                ToIssue = binding.ToIssue,
                Year = binding.Year,
                BindingDate = binding.BindingDate,
                IssueCount = binding.IssueCount,
                ItemId = binding.ItemId,
                Barcode = binding.Item!.Barcode,
                CallNumber = binding.Item!.CallNumber,
                Note = binding.Note
            })
            .ToListAsync(ct);
}

/// <summary>
/// Đóng tập: gộp một khoảng số thành một tập đóng bìa (IV.4).
///
/// Tập đóng là một ấn phẩm mới trong kho — nó có mã vạch riêng, nằm trên giá và được mượn như một
/// cuốn sách. Các số lẻ thì chuyển sang trạng thái "đã đóng tập" chứ không bị xóa: sổ nhận số phải
/// còn nguyên để đối chiếu khi kiểm kê.
/// </summary>
public class BindSerialIssuesCommand : IRequest<SerialBindingDto>
{
    public Guid SerialId { get; set; }
    /// <summary>Danh sách số cần đóng; bỏ trống thì dùng khoảng số theo năm bên dưới.</summary>
    public List<Guid> IssueIds { get; set; } = new();
    public int? Year { get; set; }
    public string? FromIssue { get; set; }
    public string? ToIssue { get; set; }
    public DateOnly? BindingDate { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? ShelfId { get; set; }
    public string? CallNumber { get; set; }
    public decimal? Price { get; set; }
    public string? Note { get; set; }
}

public class BindSerialIssuesCommandValidator : AbstractValidator<BindSerialIssuesCommand>
{
    public BindSerialIssuesCommandValidator()
    {
        RuleFor(command => command.SerialId).NotEmpty().WithMessage("Chưa chọn đầu báo.");

        RuleFor(command => command)
            .Must(command => command.IssueIds.Count > 0 || command.Year is not null)
            .WithMessage("Phải chọn danh sách số hoặc chỉ định năm cần đóng tập.");
    }
}

public class BindSerialIssuesCommandHandler : IRequestHandler<BindSerialIssuesCommand, SerialBindingDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICodeGenerator _codes;
    private readonly IDateTimeProvider _clock;

    public BindSerialIssuesCommandHandler(
        IApplicationDbContext db, ICodeGenerator codes, IDateTimeProvider clock)
    {
        _db = db;
        _codes = codes;
        _clock = clock;
    }

    public async Task<SerialBindingDto> Handle(BindSerialIssuesCommand command, CancellationToken ct)
    {
        var serial = await _db.Serials.FirstOrDefaultAsync(entity => entity.Id == command.SerialId, ct)
            ?? throw new NotFoundException("ấn phẩm định kỳ", command.SerialId);

        var candidates = await _db.SerialIssues
            .Where(issue => issue.SerialId == serial.Id)
            .WhereIf(command.IssueIds.Count > 0, issue => command.IssueIds.Contains(issue.Id))
            .WhereIf(command.IssueIds.Count == 0 && command.Year is not null,
                issue => issue.Year == command.Year)
            .OrderBy(issue => issue.ExpectedDate)
            .ToListAsync(ct);

        candidates = SelectIssueRange(candidates, command);

        // Chỉ số đã nhận mới đóng được thành tập: đóng một số chưa về là đóng một tập thiếu ruột.
        var issues = candidates
            .Where(issue => issue.Status == SerialIssueStatus.Received)
            .OrderBy(issue => issue.ExpectedDate)
            .ToList();

        if (issues.Count == 0)
        {
            var alreadyBound = candidates.Count(issue => issue.Status == SerialIssueStatus.Bound);

            throw new ConflictException(
                alreadyBound > 0
                    ? $"Các số trong phạm vi này đã đóng tập từ trước ({alreadyBound} số)."
                    : "Không có số nào đã nhận trong phạm vi đã chọn để đóng tập.");
        }

        var year = command.Year ?? issues[0].Year;
        var warehouseId = command.WarehouseId ?? serial.WarehouseId;

        if (warehouseId is null)
        {
            throw new Common.Exceptions.ValidationException(
                "warehouseId",
                "Tập đóng là một ấn phẩm trong kho nên phải chọn kho. Hãy phân kho cho đầu báo hoặc chọn kho ở đây.");
        }

        var code = await _codes.NextAsync("BINDING", ct);
        var barcode = await _codes.NextAsync("BARCODE", ct);
        var registerNumber = await _codes.NextAsync("REGISTER", ct);

        var lastCopy = await _db.Items
            .Where(item => item.BibId == serial.BibId)
            .MaxAsync(item => (int?)item.CopyNumber, ct) ?? 0;

        var caption = $"Tập đóng {issues[0].IssueNo}–{issues[^1].IssueNo} ({year})";

        var item = new Item
        {
            Id = Guid.NewGuid(),
            BibId = serial.BibId,
            Barcode = barcode,
            RegisterNumber = registerNumber,
            WarehouseId = warehouseId.Value,
            ShelfId = command.ShelfId ?? serial.ShelfId,
            CallNumber = command.CallNumber?.Trim() ?? serial.CallNumber,
            Price = command.Price ?? (serial.PricePerIssue ?? 0) * issues.Count,
            AcquisitionDate = command.BindingDate ?? _clock.Today,
            AcquisitionType = AcquisitionType.Purchase,
            SupplierId = serial.SupplierId,
            Status = ItemStatus.InStock,
            IsLocked = false,
            VolumeNumber = caption,
            CopyNumber = lastCopy + 1,
            Note = $"Đóng tập {code}: {issues.Count} số"
        };

        _db.Items.Add(item);

        var binding = new SerialBinding
        {
            Id = Guid.NewGuid(),
            SerialId = serial.Id,
            Code = code,
            FromIssue = command.FromIssue?.Trim() ?? issues[0].IssueNo,
            ToIssue = command.ToIssue?.Trim() ?? issues[^1].IssueNo,
            Year = year,
            BindingDate = command.BindingDate ?? _clock.Today,
            ItemId = item.Id,
            IssueCount = issues.Count,
            Note = command.Note?.Trim()
        };

        _db.SerialBindings.Add(binding);

        foreach (var issue in issues)
        {
            issue.Status = SerialIssueStatus.Bound;
            issue.BindingId = binding.Id;
        }

        await _db.SaveChangesAsync(ct);
        await RefreshBibCountsAsync(serial.BibId, ct);

        return new SerialBindingDto
        {
            Id = binding.Id,
            SerialId = serial.Id,
            SerialTitle = serial.Title,
            Code = binding.Code,
            FromIssue = binding.FromIssue,
            ToIssue = binding.ToIssue,
            Year = binding.Year,
            BindingDate = binding.BindingDate,
            IssueCount = binding.IssueCount,
            ItemId = item.Id,
            Barcode = item.Barcode,
            CallNumber = item.CallNumber,
            Note = binding.Note
        };
    }

    /// <summary>
    /// Thu hẹp danh sách số theo khoảng "từ số → đến số" (IV.4: "chọn khoảng số, ví dụ số 1–12").
    ///
    /// Số hiệu là chuỗi ("1", "12", "Xuân") nên không so sánh bằng số học: thứ tự lấy theo ngày
    /// phát hành dự kiến, và khoảng là đoạn giữa hai số đã nêu. Số không có trong năm thì báo rõ
    /// thay vì lặng lẽ đóng cả năm — đó chính là lỗi cũ: hai ô này chỉ được dùng làm nhãn.
    /// </summary>
    private static List<SerialIssue> SelectIssueRange(List<SerialIssue> ordered, BindSerialIssuesCommand command)
    {
        if (command.IssueIds.Count > 0
            || (string.IsNullOrWhiteSpace(command.FromIssue) && string.IsNullOrWhiteSpace(command.ToIssue)))
        {
            return ordered;
        }

        var start = 0;
        var end = ordered.Count - 1;

        if (!string.IsNullOrWhiteSpace(command.FromIssue))
        {
            start = ordered.FindIndex(issue => issue.IssueNo == command.FromIssue.Trim());

            if (start < 0)
            {
                throw new Common.Exceptions.ValidationException(
                    "fromIssue", $"Không có số \"{command.FromIssue}\" trong năm đã chọn.");
            }
        }

        if (!string.IsNullOrWhiteSpace(command.ToIssue))
        {
            end = ordered.FindIndex(issue => issue.IssueNo == command.ToIssue.Trim());

            if (end < 0)
            {
                throw new Common.Exceptions.ValidationException(
                    "toIssue", $"Không có số \"{command.ToIssue}\" trong năm đã chọn.");
            }
        }

        if (end < start)
        {
            throw new Common.Exceptions.ValidationException(
                "toIssue", "Số kết thúc phát hành trước số bắt đầu — hãy đảo lại khoảng số.");
        }

        return ordered.GetRange(start, end - start + 1);
    }

    private async Task RefreshBibCountsAsync(Guid bibId, CancellationToken ct)
    {
        var total = await _db.Items.CountAsync(item => item.BibId == bibId, ct);

        var available = await _db.Items.CountAsync(
            item => item.BibId == bibId && !item.IsLocked && item.Status == ItemStatus.InStock, ct);

        await _db.BibRecords
            .Where(record => record.Id == bibId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(record => record.ItemCount, total)
                    .SetProperty(record => record.AvailableItemCount, available),
                ct);
    }
}

// ---------------------------------------------------------------------------------------------
// IV.1 và IV.4 — Lưới tình trạng nhận số.
// ---------------------------------------------------------------------------------------------

/// <summary>Một ô trên lưới nhận số.</summary>
public record IssueGridCellDto(
    Guid IssueId,
    string IssueNo,
    string? Volume,
    DateOnly ExpectedDate,
    DateOnly? ReceivedDate,
    SerialIssueStatus Status,
    bool IsOverdue);

/// <summary>Một năm trên lưới nhận số.</summary>
public class IssueGridYearDto
{
    public int Year { get; set; }
    public int Expected { get; set; }
    public int Received { get; set; }
    public int Missing { get; set; }
    public int Bound { get; set; }
    public IReadOnlyList<IssueGridCellDto> Cells { get; set; } = Array.Empty<IssueGridCellDto>();
}

/// <summary>
/// Lưới các số theo năm, tô màu theo trạng thái (IV.1 và IV.4).
///
/// Đây là màn hình cán bộ nhìn nhiều nhất ở phân hệ này: một cái liếc mắt phải trả lời được "năm nay
/// còn thiếu số nào", nên dữ liệu trả về đã nhóm sẵn theo năm và kèm số đếm từng trạng thái.
/// </summary>
public record GetIssueGridQuery(Guid SerialId, int? FromYear, int? ToYear)
    : IRequest<IReadOnlyList<IssueGridYearDto>>;

public class GetIssueGridQueryHandler
    : IRequestHandler<GetIssueGridQuery, IReadOnlyList<IssueGridYearDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetIssueGridQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<IssueGridYearDto>> Handle(
        GetIssueGridQuery query, CancellationToken ct)
    {
        var today = _clock.Today;

        var issues = await _db.SerialIssues
            .AsNoTracking()
            .Where(issue => issue.SerialId == query.SerialId)
            .WhereIf(query.FromYear is not null, issue => issue.Year >= query.FromYear)
            .WhereIf(query.ToYear is not null, issue => issue.Year <= query.ToYear)
            .OrderBy(issue => issue.Year)
            .ThenBy(issue => issue.ExpectedDate)
            .Select(issue => new
            {
                issue.Id,
                issue.IssueNo,
                issue.Volume,
                issue.Year,
                issue.ExpectedDate,
                issue.ReceivedDate,
                issue.Status
            })
            .ToListAsync(ct);

        return issues
            .GroupBy(issue => issue.Year)
            .Select(group => new IssueGridYearDto
            {
                Year = group.Key,
                Expected = group.Count(issue => issue.Status == SerialIssueStatus.Expected),
                Received = group.Count(issue => issue.Status == SerialIssueStatus.Received),
                Missing = group.Count(issue =>
                    issue.Status is SerialIssueStatus.Missing or SerialIssueStatus.Claimed),
                Bound = group.Count(issue => issue.Status == SerialIssueStatus.Bound),
                Cells = group
                    .Select(issue => new IssueGridCellDto(
                        issue.Id,
                        issue.IssueNo,
                        issue.Volume,
                        issue.ExpectedDate,
                        issue.ReceivedDate,
                        issue.Status,
                        issue.Status == SerialIssueStatus.Expected && issue.ExpectedDate < today))
                    .ToList()
            })
            .OrderByDescending(year => year.Year)
            .ToList();
    }
}

/// <summary>Một dòng của bảng tổng hợp tình hình nhận số theo năm (IV.4).</summary>
public record SerialSummaryRowDto(
    int Year,
    int Planned,
    int Received,
    int Missing,
    int Bound,
    decimal Value,
    double ReceivedPercent);

public record GetSerialSummaryQuery(Guid SerialId) : IRequest<IReadOnlyList<SerialSummaryRowDto>>;

public class GetSerialSummaryQueryHandler
    : IRequestHandler<GetSerialSummaryQuery, IReadOnlyList<SerialSummaryRowDto>>
{
    private readonly IApplicationDbContext _db;

    public GetSerialSummaryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<SerialSummaryRowDto>> Handle(
        GetSerialSummaryQuery query, CancellationToken ct)
    {
        var serial = await _db.Serials.AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == query.SerialId, ct)
            ?? throw new NotFoundException("ấn phẩm định kỳ", query.SerialId);

        var rows = await _db.SerialIssues
            .AsNoTracking()
            .Where(issue => issue.SerialId == query.SerialId)
            .GroupBy(issue => issue.Year)
            .Select(group => new
            {
                Year = group.Key,
                Planned = group.Count(),
                Received = group.Count(issue => issue.Status == SerialIssueStatus.Received),
                Bound = group.Count(issue => issue.Status == SerialIssueStatus.Bound),
                Missing = group.Count(issue =>
                    issue.Status == SerialIssueStatus.Missing || issue.Status == SerialIssueStatus.Claimed),
                Copies = group.Sum(issue => issue.Quantity)
            })
            .ToListAsync(ct);

        var price = serial.PricePerIssue ?? 0;

        return rows
            .Select(row =>
            {
                var received = row.Received + row.Bound;

                return new SerialSummaryRowDto(
                    row.Year,
                    row.Planned,
                    received,
                    row.Missing,
                    row.Bound,
                    row.Copies * price,
                    row.Planned == 0 ? 0 : Math.Round(received * 100.0 / row.Planned, 1));
            })
            .OrderByDescending(row => row.Year)
            .ToList();
    }
}

/// <summary>Nhãn tiếng Việt của trạng thái một số báo.</summary>
public static class SerialIssueStatusLabels
{
    public static readonly IReadOnlyDictionary<SerialIssueStatus, string> All =
        new Dictionary<SerialIssueStatus, string>
        {
            [SerialIssueStatus.Expected] = "Dự kiến",
            [SerialIssueStatus.Received] = "Đã nhận",
            [SerialIssueStatus.Missing] = "Thiếu",
            [SerialIssueStatus.Claimed] = "Đang khiếu nại",
            [SerialIssueStatus.Bound] = "Đã đóng tập"
        };

    public static string Of(SerialIssueStatus status) =>
        All.TryGetValue(status, out var label) ? label : status.ToString();

    public static string Of(string status) =>
        Enum.TryParse<SerialIssueStatus>(status, out var parsed) ? Of(parsed) : status;

    /// <summary>Tên năm dùng làm nhãn cột trên báo cáo.</summary>
    public static string Year(int year) => year.ToString(CultureInfo.InvariantCulture);
}
