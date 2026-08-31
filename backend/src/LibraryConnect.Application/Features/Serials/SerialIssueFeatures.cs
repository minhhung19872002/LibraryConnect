using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Entities.Ser;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Serials;

// ---------------------------------------------------------------------------------------------
// IV.3 và IV.4 — Sinh số dự kiến, ghi nhận số đến, kiểm tra và khiếu nại.
// ---------------------------------------------------------------------------------------------

public class SerialIssueDto
{
    public Guid Id { get; set; }
    public Guid SerialId { get; set; }
    public string SerialTitle { get; set; } = string.Empty;
    public string IssueNo { get; set; } = string.Empty;
    public string? Volume { get; set; }
    public int Year { get; set; }
    public string? Caption { get; set; }
    public DateOnly ExpectedDate { get; set; }
    public DateOnly? ReceivedDate { get; set; }
    public string? ReceivedByName { get; set; }
    public int Quantity { get; set; }
    public SerialIssueStatus Status { get; set; }
    public string? Barcode { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public Guid? BindingId { get; set; }
    public string? Note { get; set; }
    public int ArticleCount { get; set; }
    /// <summary>Quá hạn phát hành mà chưa nhận — dòng cần cán bộ để mắt tới.</summary>
    public bool IsOverdue { get; set; }
    public bool HasOpenClaim { get; set; }
}

public class SerialIssueListRequest : PagedRequest
{
    public Guid? SerialId { get; set; }
    public int? Year { get; set; }
    public SerialIssueStatus? Status { get; set; }
    public DateOnly? ExpectedFrom { get; set; }
    public DateOnly? ExpectedTo { get; set; }
    /// <summary>Chỉ các số đã đến hạn phát hành mà chưa ghi nhận — màn hình ghi nhận hàng loạt.</summary>
    public bool? DueOnly { get; set; }
    public bool? OverdueOnly { get; set; }
}

public record SearchSerialIssuesQuery(SerialIssueListRequest Request)
    : IRequest<PagedResult<SerialIssueDto>>;

public class SearchSerialIssuesQueryHandler
    : IRequestHandler<SearchSerialIssuesQuery, PagedResult<SerialIssueDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public SearchSerialIssuesQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PagedResult<SerialIssueDto>> Handle(
        SearchSerialIssuesQuery query, CancellationToken ct)
    {
        var request = query.Request;
        var today = _clock.Today;

        var issues = _db.SerialIssues
            .AsNoTracking()
            .WhereIf(request.SerialId is not null, issue => issue.SerialId == request.SerialId)
            .WhereIf(request.Year is not null, issue => issue.Year == request.Year)
            .WhereIf(request.Status is not null, issue => issue.Status == request.Status)
            .WhereIf(request.ExpectedFrom is not null, issue => issue.ExpectedDate >= request.ExpectedFrom)
            .WhereIf(request.ExpectedTo is not null, issue => issue.ExpectedDate <= request.ExpectedTo)
            .WhereIf(request.DueOnly == true,
                issue => issue.Status == SerialIssueStatus.Expected && issue.ExpectedDate <= today)
            .WhereIf(request.OverdueOnly == true,
                issue => issue.Status == SerialIssueStatus.Expected && issue.ExpectedDate < today);

        if (request.HasKeyword())
        {
            var keyword = request.Keyword!.Trim().ToLowerInvariant();

            issues = issues.Where(issue =>
                issue.IssueNo.ToLower().Contains(keyword)
                || (issue.Barcode != null && issue.Barcode.ToLower().Contains(keyword))
                || DatabaseFunctions.Unaccent(issue.Serial!.Title).Contains(
                    Common.Text.VietnameseText.RemoveDiacritics(keyword)));
        }

        var total = await issues.CountAsync(ct);

        var page = await issues
            .OrderBy(issue => issue.ExpectedDate)
            .ThenBy(issue => issue.Serial!.Title)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(issue => new SerialIssueDto
            {
                Id = issue.Id,
                SerialId = issue.SerialId,
                SerialTitle = issue.Serial!.Title,
                IssueNo = issue.IssueNo,
                Volume = issue.Volume,
                Year = issue.Year,
                Caption = issue.Caption,
                ExpectedDate = issue.ExpectedDate,
                ReceivedDate = issue.ReceivedDate,
                ReceivedByName = issue.ReceivedByName,
                Quantity = issue.Quantity,
                Status = issue.Status,
                Barcode = issue.Barcode,
                WarehouseId = issue.WarehouseId,
                BindingId = issue.BindingId,
                Note = issue.Note,
                ArticleCount = issue.Articles.Count,
                HasOpenClaim = _db.SerialClaims.Any(claim =>
                    claim.IssueId == issue.Id && claim.Status == SerialClaimStatus.Open)
            })
            .ToListAsync(ct);

        foreach (var row in page)
        {
            row.IsOverdue = row.Status == SerialIssueStatus.Expected && row.ExpectedDate < today;
        }

        return new PagedResult<SerialIssueDto>(page, total, request.Page, request.PageSize);
    }
}

/// <summary>Một số dự kiến trong bản xem trước, trước khi cán bộ chốt.</summary>
public record IssuePreviewDto(string IssueNo, string? Volume, int Year, DateOnly ExpectedDate, string Caption);

/// <summary>
/// Xem trước danh sách số sẽ sinh, chưa ghi vào cơ sở dữ liệu (IV.4).
///
/// Đặc tả yêu cầu "cho phép sửa tay từng số trước khi chốt", nên bước xem trước là bắt buộc chứ
/// không phải tiện nghi: sinh thẳng vào bảng rồi mới cho sửa nghĩa là mọi lần thử đều để lại rác.
/// </summary>
public record PreviewSerialIssuesQuery(Guid SerialId, DateOnly? From, DateOnly? To)
    : IRequest<IReadOnlyList<IssuePreviewDto>>;

public class PreviewSerialIssuesQueryHandler
    : IRequestHandler<PreviewSerialIssuesQuery, IReadOnlyList<IssuePreviewDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public PreviewSerialIssuesQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<IssuePreviewDto>> Handle(
        PreviewSerialIssuesQuery query, CancellationToken ct)
    {
        var serial = await _db.Serials.AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == query.SerialId, ct)
            ?? throw new NotFoundException("ấn phẩm định kỳ", query.SerialId);

        var (from, to) = Range(serial, query.From, query.To, _clock.Today);

        return SerialIssuePredictor
            .Predict(serial.Frequency, SerialPatternDto.Read(serial.FrequencyConfig), from, to)
            .Select(issue => new IssuePreviewDto(
                issue.IssueNo, issue.Volume, issue.Year, issue.ExpectedDate, issue.Caption))
            .ToList();
    }

    /// <summary>
    /// Khoảng sinh số: lấy theo tham số truyền vào, không có thì lấy thời gian đặt mua, vẫn không có
    /// thì lấy trọn năm hiện tại — cán bộ mở màn hình ra là đã có sẵn một khoảng dùng được.
    /// </summary>
    internal static (DateOnly From, DateOnly To) Range(
        Serial serial, DateOnly? from, DateOnly? to, DateOnly today)
    {
        var start = from ?? serial.SubscriptionStart ?? new DateOnly(today.Year, 1, 1);
        var end = to ?? serial.SubscriptionEnd ?? new DateOnly(today.Year, 12, 31);

        return (start, end);
    }
}

public class GenerateIssuesResultDto
{
    public int Created { get; set; }
    /// <summary>Số đã tồn tại nên bỏ qua — sinh lại cho một khoảng đã sinh không được nhân đôi.</summary>
    public int Skipped { get; set; }
    public List<string> Captions { get; set; } = new();
}

/// <summary>Chốt danh sách số dự kiến vào cơ sở dữ liệu (IV.3 và IV.4).</summary>
public class GenerateSerialIssuesCommand : IRequest<GenerateIssuesResultDto>
{
    /// <summary>Nhiều đầu báo cùng lúc — đó là cách dùng ở màn hình bổ sung tổng thể (IV.3).</summary>
    public List<Guid> SerialIds { get; set; } = new();
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    /// <summary>Danh sách đã sửa tay ở bước xem trước; bỏ trống thì sinh theo đúng dự đoán.</summary>
    public List<IssuePreviewDto>? Issues { get; set; }
}

public class GenerateSerialIssuesCommandValidator : AbstractValidator<GenerateSerialIssuesCommand>
{
    public GenerateSerialIssuesCommandValidator()
    {
        RuleFor(command => command.SerialIds)
            .NotEmpty().WithMessage("Chưa chọn đầu báo nào để sinh số.");

        RuleFor(command => command)
            .Must(command => command.Issues is null || command.SerialIds.Count == 1)
            .WithMessage("Danh sách số sửa tay chỉ áp dụng khi sinh cho một đầu báo.");
    }
}

public class GenerateSerialIssuesCommandHandler
    : IRequestHandler<GenerateSerialIssuesCommand, GenerateIssuesResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GenerateSerialIssuesCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<GenerateIssuesResultDto> Handle(
        GenerateSerialIssuesCommand command, CancellationToken ct)
    {
        var serials = await _db.Serials
            .Where(serial => command.SerialIds.Contains(serial.Id))
            .ToListAsync(ct);

        if (serials.Count == 0)
        {
            throw new NotFoundException("Không tìm thấy đầu báo nào trong danh sách đã chọn.");
        }

        var result = new GenerateIssuesResultDto();

        foreach (var serial in serials)
        {
            var (from, to) = PreviewSerialIssuesQueryHandler.Range(
                serial, command.From, command.To, _clock.Today);

            var predicted = command.Issues is { Count: > 0 }
                ? command.Issues.Select(issue => new PredictedIssue(
                    issue.IssueNo, issue.Volume, issue.Year, issue.ExpectedDate, issue.Caption)).ToList()
                : SerialIssuePredictor
                    .Predict(serial.Frequency, SerialPatternDto.Read(serial.FrequencyConfig), from, to)
                    .ToList();

            // Số đã có trong kỳ đó thì bỏ qua: cán bộ sinh lại cho một khoảng đã sinh là chuyện
            // thường xuyên, và nhân đôi số sẽ làm hỏng cả lưới theo dõi.
            var existing = await _db.SerialIssues
                .Where(issue => issue.SerialId == serial.Id)
                .Select(issue => new { issue.Year, issue.IssueNo })
                .ToListAsync(ct);

            var seen = existing
                .Select(issue => $"{issue.Year}/{issue.IssueNo}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var issue in predicted)
            {
                var key = $"{issue.Year}/{issue.IssueNo}";

                if (!seen.Add(key))
                {
                    result.Skipped++;
                    continue;
                }

                _db.SerialIssues.Add(new SerialIssue
                {
                    Id = Guid.NewGuid(),
                    SerialId = serial.Id,
                    IssueNo = issue.IssueNo,
                    Volume = issue.Volume,
                    Year = issue.Year,
                    Caption = SerialIssuePredictor.Caption(null, issue.Volume, issue.IssueNo, issue.Year),
                    ExpectedDate = issue.ExpectedDate,
                    Quantity = 0,
                    Status = SerialIssueStatus.Expected,
                    WarehouseId = serial.WarehouseId
                });

                result.Created++;

                if (result.Captions.Count < 20)
                {
                    result.Captions.Add($"{serial.Title} — {issue.Caption}");
                }
            }
        }

        if (result.Created == 0)
        {
            throw new ConflictException(
                result.Skipped > 0
                    ? $"Toàn bộ {result.Skipped} số trong khoảng này đã được sinh trước đó."
                    : "Kỳ hạn hiện tại không sinh ra số nào trong khoảng đã chọn.");
        }

        await _db.SaveChangesAsync(ct);
        return result;
    }
}

/// <summary>Ghi nhận một số đã đến.</summary>
public record ReceiveIssueInput(Guid IssueId, int Quantity, DateOnly? ReceivedDate, string? Note);

/// <summary>
/// Ghi nhận số đến, một số hoặc cả loạt (IV.3 và IV.4).
///
/// Mỗi bản nhận về được sinh một ĐKCB riêng khi đầu báo đã phân kho: đó là thứ cán bộ dán mã vạch
/// lên và là thứ bạn đọc mượn, nên nó phải là một ấn phẩm thật trong kho chứ không phải một dòng
/// ghi chú.
/// </summary>
public class ReceiveSerialIssuesCommand : IRequest<ReceiveIssuesResultDto>
{
    public List<ReceiveIssueInput> Issues { get; set; } = new();
    /// <summary>Sinh ĐKCB và mã vạch cho từng bản nhận về.</summary>
    public bool CreateItems { get; set; } = true;
    public Guid? WarehouseId { get; set; }
    public Guid? ShelfId { get; set; }
}

public class ReceiveIssuesResultDto
{
    public int Received { get; set; }
    public int CreatedItems { get; set; }
    public List<string> Barcodes { get; set; } = new();
    public List<string> Skipped { get; set; } = new();
}

public class ReceiveSerialIssuesCommandValidator : AbstractValidator<ReceiveSerialIssuesCommand>
{
    public ReceiveSerialIssuesCommandValidator()
    {
        RuleFor(command => command.Issues).NotEmpty().WithMessage("Chưa chọn số nào để ghi nhận.");

        RuleForEach(command => command.Issues)
            .Must(issue => issue.Quantity is > 0 and <= 100)
            .WithMessage("Số lượng thực nhận của mỗi kỳ phải từ 1 đến 100.");
    }
}

public class ReceiveSerialIssuesCommandHandler
    : IRequestHandler<ReceiveSerialIssuesCommand, ReceiveIssuesResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICodeGenerator _codes;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public ReceiveSerialIssuesCommandHandler(
        IApplicationDbContext db, ICodeGenerator codes, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _codes = codes;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<ReceiveIssuesResultDto> Handle(
        ReceiveSerialIssuesCommand command, CancellationToken ct)
    {
        var ids = command.Issues.Select(issue => issue.IssueId).ToList();

        var issues = await _db.SerialIssues
            .Include(issue => issue.Serial)
            .Where(issue => ids.Contains(issue.Id))
            .ToListAsync(ct);

        var result = new ReceiveIssuesResultDto();
        var today = _clock.Today;

        foreach (var input in command.Issues)
        {
            var issue = issues.FirstOrDefault(entity => entity.Id == input.IssueId);

            if (issue is null)
            {
                continue;
            }

            if (issue.Status is SerialIssueStatus.Received or SerialIssueStatus.Bound)
            {
                result.Skipped.Add($"{issue.Serial?.Title} — {issue.Caption}: đã ghi nhận trước đó.");
                continue;
            }

            issue.Status = SerialIssueStatus.Received;
            issue.ReceivedDate = input.ReceivedDate ?? today;
            issue.ReceivedBy = _currentUser.UserId;
            issue.ReceivedByName = _currentUser.FullName;
            issue.Quantity = input.Quantity;
            issue.Note = input.Note?.Trim() ?? issue.Note;

            var warehouseId = command.WarehouseId ?? issue.WarehouseId ?? issue.Serial?.WarehouseId;

            if (warehouseId is not null)
            {
                issue.WarehouseId = warehouseId;
            }

            result.Received++;

            if (!command.CreateItems || warehouseId is null || issue.Serial is null)
            {
                continue;
            }

            var created = await CreateCopiesAsync(
                issue, issue.Serial, warehouseId.Value, command.ShelfId, input.Quantity, ct);

            result.CreatedItems += created.Count;
            result.Barcodes.AddRange(created);

            // Mã vạch của bản đầu tiên gắn lên chính số đó, để tra theo mã vạch ra ngay số báo.
            issue.Barcode ??= created.FirstOrDefault();
        }

        if (result.Received == 0)
        {
            throw new ConflictException(
                result.Skipped.Count > 0
                    ? "Các số đã chọn đều được ghi nhận từ trước."
                    : "Không tìm thấy số nào trong danh sách đã chọn.");
        }

        await _db.SaveChangesAsync(ct);
        await RefreshBibCountsAsync(issues.Select(issue => issue.Serial?.BibId).ToList(), ct);

        return result;
    }

    private async Task<List<string>> CreateCopiesAsync(
        SerialIssue issue, Serial serial, Guid warehouseId, Guid? shelfId, int quantity, CancellationToken ct)
    {
        var barcodes = await _codes.NextBatchAsync("BARCODE", quantity, ct);
        var registerNumbers = await _codes.NextBatchAsync("REGISTER", quantity, ct);

        var lastCopy = await _db.Items
            .Where(item => item.BibId == serial.BibId)
            .MaxAsync(item => (int?)item.CopyNumber, ct) ?? 0;

        for (var index = 0; index < quantity; index++)
        {
            _db.Items.Add(new Item
            {
                Id = Guid.NewGuid(),
                BibId = serial.BibId,
                Barcode = barcodes[index],
                RegisterNumber = registerNumbers[index],
                WarehouseId = warehouseId,
                ShelfId = shelfId ?? serial.ShelfId,
                CallNumber = serial.CallNumber,
                Price = serial.PricePerIssue ?? 0,
                AcquisitionDate = issue.ReceivedDate ?? _clock.Today,
                AcquisitionType = AcquisitionType.Purchase,
                SupplierId = serial.SupplierId,
                // Báo và tạp chí đọc được ngay khi về tới kho; chúng không đi qua bước kiểm nhận
                // như sách, vì một số báo để chờ kiểm nhận một tuần là một số báo đã cũ.
                Status = ItemStatus.InStock,
                IsLocked = false,
                // Số báo là ấn phẩm nhiều tập: ghi số kỳ vào cột tập để danh sách ĐKCB đọc được.
                VolumeNumber = issue.Caption,
                CopyNumber = lastCopy + index + 1,
                Note = $"Số {issue.IssueNo} năm {issue.Year}"
            });
        }

        return barcodes.ToList();
    }

    /// <summary>Cập nhật lại số bản trên biểu ghi của đầu báo sau khi nhận thêm số.</summary>
    private async Task RefreshBibCountsAsync(IReadOnlyList<Guid?> bibIds, CancellationToken ct)
    {
        foreach (var bibId in bibIds.Where(id => id is not null).Select(id => id!.Value).Distinct())
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
}

/// <summary>Đánh dấu các số quá hạn là thiếu, để chuyển sang bước khiếu nại (IV.3).</summary>
public class MarkIssuesMissingCommand : IRequest<int>
{
    public List<Guid> IssueIds { get; set; } = new();
    /// <summary>Bỏ trống danh sách thì đánh dấu mọi số quá hạn của đầu báo này.</summary>
    public Guid? SerialId { get; set; }
    public string? Note { get; set; }
}

public class MarkIssuesMissingCommandHandler : IRequestHandler<MarkIssuesMissingCommand, int>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public MarkIssuesMissingCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<int> Handle(MarkIssuesMissingCommand command, CancellationToken ct)
    {
        var today = _clock.Today;

        var issues = await _db.SerialIssues
            .WhereIf(command.IssueIds.Count > 0, issue => command.IssueIds.Contains(issue.Id))
            .WhereIf(command.IssueIds.Count == 0 && command.SerialId is not null,
                issue => issue.SerialId == command.SerialId
                         && issue.Status == SerialIssueStatus.Expected
                         && issue.ExpectedDate < today)
            .ToListAsync(ct);

        var affected = 0;

        foreach (var issue in issues.Where(issue => issue.Status == SerialIssueStatus.Expected))
        {
            issue.Status = SerialIssueStatus.Missing;

            if (!string.IsNullOrWhiteSpace(command.Note))
            {
                issue.Note = command.Note.Trim();
            }

            affected++;
        }

        if (affected == 0)
        {
            throw new ConflictException("Không có số nào ở trạng thái dự kiến để đánh dấu thiếu.");
        }

        await _db.SaveChangesAsync(ct);
        return affected;
    }
}

// ---------------------------------------------------------------------------------------------
// Khiếu nại nhà cung cấp
// ---------------------------------------------------------------------------------------------

public class SerialClaimDto
{
    public Guid Id { get; set; }
    public Guid IssueId { get; set; }
    public string ClaimNo { get; set; } = string.Empty;
    public DateOnly ClaimDate { get; set; }
    public string SerialTitle { get; set; } = string.Empty;
    public string? IssueCaption { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? Content { get; set; }
    public string? Response { get; set; }
    public DateOnly? ResponseDate { get; set; }
    public SerialClaimStatus Status { get; set; }
}

public record GetSerialClaimsQuery(Guid? SerialId, SerialClaimStatus? Status)
    : IRequest<IReadOnlyList<SerialClaimDto>>;

public class GetSerialClaimsQueryHandler
    : IRequestHandler<GetSerialClaimsQuery, IReadOnlyList<SerialClaimDto>>
{
    private readonly IApplicationDbContext _db;

    public GetSerialClaimsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<SerialClaimDto>> Handle(
        GetSerialClaimsQuery query, CancellationToken ct) =>
        await _db.SerialClaims
            .AsNoTracking()
            .WhereIf(query.SerialId is not null, claim => claim.Issue!.SerialId == query.SerialId)
            .WhereIf(query.Status is not null, claim => claim.Status == query.Status)
            .OrderByDescending(claim => claim.ClaimDate)
            .Select(claim => new SerialClaimDto
            {
                Id = claim.Id,
                IssueId = claim.IssueId,
                ClaimNo = claim.ClaimNo,
                ClaimDate = claim.ClaimDate,
                SerialTitle = claim.Issue!.Serial!.Title,
                IssueCaption = claim.Issue!.Caption,
                SupplierId = claim.SupplierId,
                SupplierName = claim.Supplier!.Name,
                Content = claim.Content,
                Response = claim.Response,
                ResponseDate = claim.ResponseDate,
                Status = claim.Status
            })
            .ToListAsync(ct);
}

/// <summary>Lập phiếu khiếu nại cho các số thiếu (IV.3 và IV.4).</summary>
public class CreateSerialClaimsCommand : IRequest<CreateClaimsResultDto>
{
    public List<Guid> IssueIds { get; set; } = new();
    public Guid? SupplierId { get; set; }
    public DateOnly? ClaimDate { get; set; }
    public string? Content { get; set; }
}

public class CreateClaimsResultDto
{
    public int Created { get; set; }
    public List<string> ClaimNumbers { get; set; } = new();
    public List<string> Skipped { get; set; } = new();
}

public class CreateSerialClaimsCommandValidator : AbstractValidator<CreateSerialClaimsCommand>
{
    public CreateSerialClaimsCommandValidator()
    {
        RuleFor(command => command.IssueIds).NotEmpty().WithMessage("Chưa chọn số nào để khiếu nại.");
    }
}

public class CreateSerialClaimsCommandHandler
    : IRequestHandler<CreateSerialClaimsCommand, CreateClaimsResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICodeGenerator _codes;
    private readonly IDateTimeProvider _clock;

    public CreateSerialClaimsCommandHandler(
        IApplicationDbContext db, ICodeGenerator codes, IDateTimeProvider clock)
    {
        _db = db;
        _codes = codes;
        _clock = clock;
    }

    public async Task<CreateClaimsResultDto> Handle(
        CreateSerialClaimsCommand command, CancellationToken ct)
    {
        var issues = await _db.SerialIssues
            .Include(issue => issue.Serial)
            .Where(issue => command.IssueIds.Contains(issue.Id))
            .ToListAsync(ct);

        var alreadyClaimed = await _db.SerialClaims
            .Where(claim => command.IssueIds.Contains(claim.IssueId)
                            && claim.Status == SerialClaimStatus.Open)
            .Select(claim => claim.IssueId)
            .ToListAsync(ct);

        var result = new CreateClaimsResultDto();
        var claimDate = command.ClaimDate ?? _clock.Today;

        var pending = issues
            .Where(issue => issue.Status is SerialIssueStatus.Expected or SerialIssueStatus.Missing)
            .Where(issue => !alreadyClaimed.Contains(issue.Id))
            .ToList();

        foreach (var issue in issues.Except(pending))
        {
            result.Skipped.Add($"{issue.Serial?.Title} — {issue.Caption}: đã nhận hoặc đang có khiếu nại mở.");
        }

        if (pending.Count == 0)
        {
            throw new ConflictException(
                "Không có số nào để khiếu nại: các số đã chọn hoặc đã nhận, hoặc đang có phiếu khiếu nại chưa đóng.");
        }

        var numbers = await _codes.NextBatchAsync("CLAIM", pending.Count, ct);
        var index = 0;

        foreach (var issue in pending)
        {
            var claimNo = numbers[index++];

            _db.SerialClaims.Add(new SerialClaim
            {
                Id = Guid.NewGuid(),
                IssueId = issue.Id,
                ClaimNo = claimNo,
                ClaimDate = claimDate,
                SupplierId = command.SupplierId ?? issue.Serial?.SupplierId,
                Content = string.IsNullOrWhiteSpace(command.Content)
                    ? $"Chưa nhận được {issue.Caption} của {issue.Serial?.Title}, " +
                      $"dự kiến phát hành ngày {issue.ExpectedDate:dd/MM/yyyy}."
                    : command.Content.Trim(),
                Status = SerialClaimStatus.Open
            });

            issue.Status = SerialIssueStatus.Claimed;

            result.Created++;
            result.ClaimNumbers.Add(claimNo);
        }

        await _db.SaveChangesAsync(ct);
        return result;
    }
}

/// <summary>Ghi nhận phản hồi của nhà cung cấp cho một phiếu khiếu nại.</summary>
public record RespondSerialClaimCommand(Guid Id, string Response, SerialClaimStatus Status) : IRequest;

public class RespondSerialClaimCommandValidator : AbstractValidator<RespondSerialClaimCommand>
{
    public RespondSerialClaimCommandValidator()
    {
        RuleFor(command => command.Response)
            .NotEmpty().WithMessage("Chưa nhập nội dung phản hồi.").MaximumLength(2000);

        RuleFor(command => command.Status)
            .Must(status => status != SerialClaimStatus.Open)
            .WithMessage("Ghi nhận phản hồi thì phiếu phải chuyển sang Đã phản hồi, Đã giải quyết hoặc Đã hủy.");
    }
}

public class RespondSerialClaimCommandHandler : IRequestHandler<RespondSerialClaimCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public RespondSerialClaimCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task Handle(RespondSerialClaimCommand command, CancellationToken ct)
    {
        var claim = await _db.SerialClaims
            .Include(entity => entity.Issue)
            .FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
            ?? throw new NotFoundException("phiếu khiếu nại", command.Id);

        claim.Response = command.Response.Trim();
        claim.ResponseDate = _clock.Today;
        claim.Status = command.Status;

        // Khiếu nại bị hủy thì số quay lại trạng thái thiếu, để nó vẫn nằm trong danh sách cần theo dõi.
        if (command.Status == SerialClaimStatus.Cancelled
            && claim.Issue is { Status: SerialIssueStatus.Claimed })
        {
            claim.Issue.Status = SerialIssueStatus.Missing;
        }

        await _db.SaveChangesAsync(ct);
    }
}
