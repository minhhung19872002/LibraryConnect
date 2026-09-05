using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Entities.Cir;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Circulation;

// ---------------------------------------------------------------------------------------------
// VII.2 — Đặt giữ chỗ.
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Đặt giữ một tài liệu.
///
/// Đặt theo biểu ghi là cách thường dùng: bạn đọc cần nội dung chứ không cần đúng bản nào, nên bản
/// nào về trước thì bản đó được giữ. Đặt theo một ĐKCB cụ thể chỉ dùng khi các bản khác nhau thật sự
/// (bản có đĩa kèm, bản ở cơ sở khác).
/// </summary>
public class PlaceHoldCommand : IRequest<HoldRowDto>
{
    public Guid ReaderId { get; set; }
    public Guid BibId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? PickupWarehouseId { get; set; }
    public LoanChannel Channel { get; set; } = LoanChannel.Desk;
}

public class PlaceHoldCommandValidator : AbstractValidator<PlaceHoldCommand>
{
    public PlaceHoldCommandValidator()
    {
        RuleFor(command => command.ReaderId).NotEmpty().WithMessage("Chưa chọn bạn đọc.");
        RuleFor(command => command.BibId).NotEmpty().WithMessage("Chưa chọn tài liệu.");
    }
}

public class PlaceHoldCommandHandler : IRequestHandler<PlaceHoldCommand, HoldRowDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICirculationPolicyResolver _policies;
    private readonly IDateTimeProvider _clock;

    public PlaceHoldCommandHandler(
        IApplicationDbContext db, ICirculationPolicyResolver policies, IDateTimeProvider clock)
    {
        _db = db;
        _policies = policies;
        _clock = clock;
    }

    public async Task<HoldRowDto> Handle(PlaceHoldCommand command, CancellationToken ct)
    {
        var today = _clock.Today;

        var reader = await _db.Readers
            .FirstOrDefaultAsync(entity => entity.Id == command.ReaderId, ct)
            ?? throw new NotFoundException("bạn đọc", command.ReaderId);

        if (!reader.CanBorrow(today))
        {
            throw new ConflictException(
                "Thẻ bạn đọc đang hết hạn hoặc bị khóa nên không đặt giữ được.");
        }

        var bib = await _db.BibRecords
            .AsNoTracking()
            .Where(entity => entity.Id == command.BibId)
            .Select(entity => new { entity.Id, entity.Title, entity.DocumentTypeId })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("biểu ghi", command.BibId);

        // A hold queues for a physical copy. A record that has none — thousands of harvested records
        // have only metadata, and a record whose every copy is lost or discarded is the same thing —
        // would keep the reader waiting for a book that never arrives.
        var hasCopies = await _db.Items.AnyAsync(item =>
            item.BibId == command.BibId
            && item.Status != ItemStatus.Lost
            && item.Status != ItemStatus.Discarded, ct);

        if (!hasCopies)
        {
            throw new ConflictException(
                "Tài liệu này chưa có bản in nào trong kho nên không đặt giữ được.");
        }

        Guid? warehouseId = command.PickupWarehouseId;

        if (command.ItemId is not null)
        {
            var item = await _db.Items
                .AsNoTracking()
                .Where(entity => entity.Id == command.ItemId)
                .Select(entity => new { entity.Id, entity.BibId, entity.WarehouseId })
                .FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException("ấn phẩm", command.ItemId);

            if (item.BibId != command.BibId)
            {
                throw new Common.Exceptions.ValidationException(
                    "itemId", "Ấn phẩm được chọn không thuộc biểu ghi này.");
            }

            warehouseId ??= item.WarehouseId;
        }

        var policy = await _policies.ResolveAsync(
            reader.ReaderTypeId, bib.DocumentTypeId, warehouseId, ct);

        if (!policy.AllowHold)
        {
            throw new ConflictException(
                $"Chính sách \"{policy.Name}\" không cho đặt giữ loại tài liệu này.");
        }

        var active = await _db.Holds.CountAsync(hold =>
            hold.ReaderId == reader.Id
            && (hold.Status == HoldStatus.Waiting || hold.Status == HoldStatus.Ready), ct);

        if (active >= policy.MaxHolds)
        {
            throw new ConflictException(
                $"Bạn đọc đã đặt giữ đủ {policy.MaxHolds} tài liệu theo chính sách \"{policy.Name}\".");
        }

        var duplicated = await _db.Holds.AnyAsync(hold =>
            hold.ReaderId == reader.Id
            && hold.BibId == command.BibId
            && (hold.Status == HoldStatus.Waiting || hold.Status == HoldStatus.Ready), ct);

        if (duplicated)
        {
            throw new ConflictException("Bạn đọc đã đặt giữ tài liệu này rồi.");
        }

        var borrowing = await _db.Loans.AnyAsync(loan =>
            loan.ReaderId == reader.Id
            && loan.BibId == command.BibId
            && (loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue), ct);

        if (borrowing)
        {
            throw new ConflictException("Bạn đọc đang giữ tài liệu này nên không cần đặt giữ.");
        }

        var queue = await _db.Holds.CountAsync(hold =>
            hold.BibId == command.BibId && hold.Status == HoldStatus.Waiting, ct);

        var hold = new Hold
        {
            ReaderId = reader.Id,
            BibId = command.BibId,
            ItemId = command.ItemId,
            HoldDate = _clock.Now,
            PickupWarehouseId = warehouseId,
            Status = HoldStatus.Waiting,
            QueuePosition = queue + 1,
            Channel = command.Channel
        };

        _db.Holds.Add(hold);
        await _db.SaveChangesAsync(ct);

        return await HoldReader.LoadAsync(_db, hold.Id, ct);
    }
}

/// <summary>Hủy một phiếu đặt giữ.</summary>
public record CancelHoldCommand(Guid Id, string? Reason, Guid? ReaderId = null) : IRequest;

public class CancelHoldCommandHandler : IRequestHandler<CancelHoldCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public CancelHoldCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task Handle(CancelHoldCommand command, CancellationToken ct)
    {
        var hold = await _db.Holds.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
            ?? throw new NotFoundException("phiếu đặt giữ", command.Id);

        if (command.ReaderId is not null && hold.ReaderId != command.ReaderId)
        {
            throw new ForbiddenException("Phiếu đặt giữ này không thuộc về bạn đọc đang đăng nhập.");
        }

        if (hold.Status is HoldStatus.Fulfilled or HoldStatus.Cancelled)
        {
            throw new ConflictException("Phiếu đặt giữ này đã kết thúc.");
        }

        var wasReady = hold.Status == HoldStatus.Ready;

        hold.Status = HoldStatus.Cancelled;
        hold.CancelReason = command.Reason;

        // Bản đang giữ ở quầy phải được chuyển cho người kế tiếp, không thì nó nằm chết ở đó.
        if (wasReady && hold.ItemId is not null)
        {
            await HoldReader.PassToNextAsync(_db, hold.ItemId.Value, hold.BibId, _clock.Now, ct);
        }

        await HoldReader.ResequenceAsync(_db, hold.BibId, ct);
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Danh sách đặt giữ.</summary>
public record SearchHoldsQuery(HoldListRequest Request) : IRequest<PagedResult<HoldRowDto>>;

public class SearchHoldsQueryHandler : IRequestHandler<SearchHoldsQuery, PagedResult<HoldRowDto>>
{
    private readonly IApplicationDbContext _db;

    public SearchHoldsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<HoldRowDto>> Handle(SearchHoldsQuery query, CancellationToken ct)
    {
        var request = query.Request;

        var holds = HoldQuery.Base(_db)
            .WhereIf(request.ReaderId is not null, hold => hold.ReaderId == request.ReaderId)
            .WhereIf(request.BibId is not null, hold => hold.BibId == request.BibId)
            .WhereIf(request.Status is not null, hold => hold.Status == request.Status)
            .WhereIf(request.PickupWarehouseId is not null,
                hold => hold.PickupWarehouseId == request.PickupWarehouseId)
            .WhereIf(request.ActiveOnly == true,
                hold => hold.Status == HoldStatus.Waiting || hold.Status == HoldStatus.Ready);

        if (request.HasKeyword())
        {
            var keyword = request.Keyword!.Trim().ToLowerInvariant();
            var unaccented = Common.Text.VietnameseText.RemoveDiacritics(keyword);

            holds = holds.Where(hold =>
                hold.Reader!.CardNumber.ToLower().Contains(keyword)
                || DatabaseFunctions.Unaccent(hold.Reader!.FullName).Contains(unaccented)
                || DatabaseFunctions.Unaccent(hold.Bib!.Title).Contains(unaccented));
        }

        var page = await holds
            .OrderBy(hold => hold.Status == HoldStatus.Ready ? 0 : 1)
            .ThenBy(hold => hold.QueuePosition)
            .ThenByDescending(hold => hold.HoldDate)
            .Select(HoldQuery.Projection)
            .ToPagedResultAsync(request, ct);

        await HoldReader.FillAvailabilityAsync(_db, page.Items, ct);
        return page;
    }
}

/// <summary>Hàng đợi của một biểu ghi, để cán bộ trả lời "còn bao nhiêu người trước tôi".</summary>
public record GetHoldQueueQuery(Guid BibId) : IRequest<IReadOnlyList<HoldRowDto>>;

public class GetHoldQueueQueryHandler : IRequestHandler<GetHoldQueueQuery, IReadOnlyList<HoldRowDto>>
{
    private readonly IApplicationDbContext _db;

    public GetHoldQueueQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<HoldRowDto>> Handle(GetHoldQueueQuery query, CancellationToken ct)
    {
        var rows = await HoldQuery.Base(_db)
            .Where(hold => hold.BibId == query.BibId
                           && (hold.Status == HoldStatus.Waiting || hold.Status == HoldStatus.Ready))
            .OrderBy(hold => hold.Status == HoldStatus.Ready ? 0 : 1)
            .ThenBy(hold => hold.QueuePosition)
            .Select(HoldQuery.Projection)
            .ToListAsync(ct);

        await HoldReader.FillAvailabilityAsync(_db, rows, ct);
        return rows;
    }
}

/// <summary>Thao tác dùng chung của đặt giữ, gọi từ nhiều handler.</summary>
internal static class HoldReader
{
    public static async Task<HoldRowDto> LoadAsync(
        IApplicationDbContext db, Guid holdId, CancellationToken ct)
    {
        var row = await HoldQuery.Base(db)
            .Where(hold => hold.Id == holdId)
            .Select(HoldQuery.Projection)
            .FirstAsync(ct);

        await FillAvailabilityAsync(db, new[] { row }, ct);
        return row;
    }

    /// <summary>Đếm số bản đang rảnh của từng biểu ghi trong danh sách.</summary>
    public static async Task FillAvailabilityAsync(
        IApplicationDbContext db, IReadOnlyList<HoldRowDto> rows, CancellationToken ct)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var bibIds = rows.Select(row => row.BibId).Distinct().ToList();

        var counts = await db.Items
            .AsNoTracking()
            .Where(item => bibIds.Contains(item.BibId)
                           && item.Status == ItemStatus.InStock
                           && !item.IsLocked)
            .GroupBy(item => item.BibId)
            .Select(group => new { BibId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.BibId, row => row.Count, ct);

        foreach (var row in rows)
        {
            row.AvailableCopies = counts.TryGetValue(row.BibId, out var count) ? count : 0;
        }
    }

    /// <summary>Chuyển một bản đang giữ ở quầy cho người kế tiếp trong hàng đợi.</summary>
    public static async Task PassToNextAsync(
        IApplicationDbContext db, Guid itemId, Guid bibId, DateTimeOffset now, CancellationToken ct)
    {
        var item = await db.Items.FirstOrDefaultAsync(entity => entity.Id == itemId, ct);

        if (item is null)
        {
            return;
        }

        var next = await db.Holds
            .Where(hold => hold.Status == HoldStatus.Waiting
                           && hold.BibId == bibId
                           && (hold.ItemId == null || hold.ItemId == itemId))
            .OrderBy(hold => hold.QueuePosition)
            .ThenBy(hold => hold.HoldDate)
            .FirstOrDefaultAsync(ct);

        if (next is null)
        {
            item.Status = ItemStatus.InStock;
            return;
        }

        next.Status = HoldStatus.Ready;
        next.ItemId = itemId;
        next.NotifiedAt = now;
        next.ExpireDate = now.AddDays(3);
        item.Status = ItemStatus.OnHoldShelf;
    }

    public static async Task ResequenceAsync(
        IApplicationDbContext db, Guid bibId, CancellationToken ct)
    {
        var rows = await db.Holds
            .Where(hold => hold.BibId == bibId && hold.Status == HoldStatus.Waiting)
            .OrderBy(hold => hold.HoldDate)
            .ToListAsync(ct);

        // Điều kiện lọc chạy dưới cơ sở dữ liệu nên nó nhìn thấy trạng thái cũ: phiếu vừa bị hủy hay
        // vừa được nhận trong chính phiên này vẫn còn là "đang chờ" ở đó. Lọc lại trên đối tượng đã
        // nạp — chỗ mang trạng thái mới — nếu không thì hủy phiếu đầu hàng xong người kế tiếp vẫn
        // mang số 2 và không bao giờ lên số 1.
        var waiting = rows.Where(hold => hold.Status == HoldStatus.Waiting).ToList();

        for (var index = 0; index < waiting.Count; index++)
        {
            waiting[index].QueuePosition = index + 1;
        }
    }
}
