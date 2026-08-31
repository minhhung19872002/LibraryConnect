using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Entities.Cir;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Circulation;

// ---------------------------------------------------------------------------------------------
// VII.2 — Ghi nhận bạn đọc ra vào thư viện (quét thẻ tại cổng).
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Một lần quét thẻ ở cổng.
///
/// Cổng chỉ có một máy quét nên cùng một thao tác phải hiểu được cả vào lẫn ra: bạn đọc chưa có lượt
/// nào đang mở thì là vào, đang mở thì là ra. Cán bộ không phải bấm chọn gì thêm.
/// </summary>
public class ScanGateCommand : IRequest<GateScanResultDto>
{
    public string CardNumber { get; set; } = string.Empty;
    public Guid? LibraryId { get; set; }
    public string? Gate { get; set; }
    public string? Purpose { get; set; }
}

public class GateScanResultDto
{
    /// <summary>true là vừa ghi nhận vào, false là vừa ghi nhận ra.</summary>
    public bool CheckedIn { get; set; }
    public VisitRowDto Visit { get; set; } = new();
    public DeskReaderDto Reader { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    /// <summary>Số bạn đọc đang ở trong thư viện sau lần quét này.</summary>
    public int InsideCount { get; set; }
}

public class ScanGateCommandHandler : IRequestHandler<ScanGateCommand, GateScanResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICirculationDeskService _desk;
    private readonly IDateTimeProvider _clock;

    public ScanGateCommandHandler(
        IApplicationDbContext db, ICirculationDeskService desk, IDateTimeProvider clock)
    {
        _db = db;
        _desk = desk;
        _clock = clock;
    }

    public async Task<GateScanResultDto> Handle(ScanGateCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.CardNumber))
        {
            throw new Common.Exceptions.ValidationException("cardNumber", "Chưa quét thẻ bạn đọc.");
        }

        var reader = await _desk.FindReaderByCardAsync(command.CardNumber, ct);
        var now = _clock.Now;

        var open = await _db.LibraryVisits
            .Where(visit => visit.ReaderId == reader.Id && visit.CheckoutAt == null)
            .OrderByDescending(visit => visit.CheckinAt)
            .FirstOrDefaultAsync(ct);

        Guid visitId;
        bool checkedIn;

        if (open is null)
        {
            var visit = new LibraryVisit
            {
                ReaderId = reader.Id,
                LibraryId = command.LibraryId,
                CheckinAt = now,
                Gate = command.Gate?.Trim(),
                Purpose = command.Purpose?.Trim()
            };

            _db.LibraryVisits.Add(visit);
            visitId = visit.Id;
            checkedIn = true;
        }
        else
        {
            open.CheckoutAt = now;

            if (!string.IsNullOrWhiteSpace(command.Gate))
            {
                open.Gate = command.Gate.Trim();
            }

            visitId = open.Id;
            checkedIn = false;
        }

        await _db.SaveChangesAsync(ct);

        var row = await VisitQuery.Base(_db)
            .Where(visit => visit.Id == visitId)
            .Select(VisitQuery.Projection)
            .FirstAsync(ct);

        row.Minutes = row.CheckoutAt is null
            ? null
            : (int)Math.Round((row.CheckoutAt.Value - row.CheckinAt).TotalMinutes);

        var inside = await _db.LibraryVisits.CountAsync(visit => visit.CheckoutAt == null, ct);
        var deskReader = await _desk.GetReaderAsync(reader.Id, ct);

        return new GateScanResultDto
        {
            CheckedIn = checkedIn,
            Visit = row,
            Reader = deskReader,
            InsideCount = inside,
            Message = checkedIn
                ? $"Chào {reader.FullName}, đã ghi nhận vào thư viện lúc {now:HH:mm}."
                : $"Tạm biệt {reader.FullName}, đã ghi nhận ra lúc {now:HH:mm}" +
                  (row.Minutes is null ? "." : $", ở lại {row.Minutes} phút.")
        };
    }
}

/// <summary>Danh sách lượt ra vào; mặc định hiện những người đang ở trong thư viện.</summary>
public record SearchVisitsQuery(VisitListRequest Request) : IRequest<PagedResult<VisitRowDto>>;

public class SearchVisitsQueryHandler : IRequestHandler<SearchVisitsQuery, PagedResult<VisitRowDto>>
{
    private readonly IApplicationDbContext _db;

    public SearchVisitsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<VisitRowDto>> Handle(SearchVisitsQuery query, CancellationToken ct)
    {
        var request = query.Request;

        var visits = VisitQuery.Base(_db)
            .WhereIf(request.ReaderId is not null, visit => visit.ReaderId == request.ReaderId)
            .WhereIf(request.LibraryId is not null, visit => visit.LibraryId == request.LibraryId)
            .WhereIf(request.InsideOnly == true, visit => visit.CheckoutAt == null)
            .WhereIf(request.FromDate is not null,
                visit => visit.CheckinAt >= request.FromDate!.Value.ToDateTime(TimeOnly.MinValue))
            .WhereIf(request.ToDate is not null,
                visit => visit.CheckinAt < request.ToDate!.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));

        if (request.HasKeyword())
        {
            var keyword = request.Keyword!.Trim().ToLowerInvariant();
            var unaccented = Common.Text.VietnameseText.RemoveDiacritics(keyword);

            visits = visits.Where(visit =>
                visit.Reader!.CardNumber.ToLower().Contains(keyword)
                || DatabaseFunctions.Unaccent(visit.Reader!.FullName).Contains(unaccented));
        }

        var page = await visits
            .OrderByDescending(visit => visit.CheckinAt)
            .Select(VisitQuery.Projection)
            .ToPagedResultAsync(request, ct);

        foreach (var row in page.Items)
        {
            row.Minutes = row.CheckoutAt is null
                ? null
                : (int)Math.Round((row.CheckoutAt.Value - row.CheckinAt).TotalMinutes);
        }

        return page;
    }
}

/// <summary>Đóng các lượt vào còn bỏ ngỏ khi thư viện đóng cửa.</summary>
public record CloseOpenVisitsCommand(DateOnly? Date) : IRequest<int>;

public class CloseOpenVisitsCommandHandler : IRequestHandler<CloseOpenVisitsCommand, int>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public CloseOpenVisitsCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<int> Handle(CloseOpenVisitsCommand command, CancellationToken ct)
    {
        var now = _clock.Now;
        var date = command.Date ?? _clock.Today;
        var upperBound = date.AddDays(1).ToDateTime(TimeOnly.MinValue);

        // Bạn đọc quên quét thẻ lúc ra là chuyện xảy ra hằng ngày; để lượt mở treo qua đêm thì báo
        // cáo "đang ở trong thư viện" mất hết ý nghĩa.
        var open = await _db.LibraryVisits
            .Where(visit => visit.CheckoutAt == null && visit.CheckinAt < upperBound)
            .ToListAsync(ct);

        foreach (var visit in open)
        {
            visit.CheckoutAt = now;
            visit.Purpose = string.IsNullOrWhiteSpace(visit.Purpose)
                ? "Hệ thống tự đóng lượt khi hết ngày"
                : visit.Purpose;
        }

        await _db.SaveChangesAsync(ct);
        return open.Count;
    }
}
