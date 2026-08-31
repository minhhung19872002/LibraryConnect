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
// VII.3 — Quản lý tủ gửi đồ.
// ---------------------------------------------------------------------------------------------

/// <summary>Sơ đồ tủ theo khu vực, kèm lượt đang mở của từng tủ.</summary>
public record GetLockerMapQuery(Guid? LibraryId, string? Area) : IRequest<LockerMapDto>;

public class LockerMapDto
{
    public List<string> Areas { get; set; } = new();
    public int Free { get; set; }
    public int InUse { get; set; }
    public int Broken { get; set; }
    public int Overdue { get; set; }
    public List<LockerRowDto> Lockers { get; set; } = new();
}

public class GetLockerMapQueryHandler : IRequestHandler<GetLockerMapQuery, LockerMapDto>
{
    /// <summary>Giữ tủ quá số phút này thì cảnh báo cho cán bộ đi kiểm tra.</summary>
    public const string OverdueMinutesParameter = "CIRCULATION.LOCKER_OVERDUE_MINUTES";

    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameters;
    private readonly IDateTimeProvider _clock;

    public GetLockerMapQueryHandler(
        IApplicationDbContext db, ISystemParameterService parameters, IDateTimeProvider clock)
    {
        _db = db;
        _parameters = parameters;
        _clock = clock;
    }

    public async Task<LockerMapDto> Handle(GetLockerMapQuery query, CancellationToken ct)
    {
        var now = _clock.Now;
        var overdueMinutes = await _parameters.GetAsync(OverdueMinutesParameter, 240, ct);

        var lockers = await _db.Lockers
            .AsNoTracking()
            .WhereIf(query.LibraryId is not null, locker => locker.LibraryId == query.LibraryId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Area), locker => locker.Area == query.Area)
            .OrderBy(locker => locker.Area)
            .ThenBy(locker => locker.MapRow)
            .ThenBy(locker => locker.MapColumn)
            .ThenBy(locker => locker.Code)
            .Select(locker => new LockerRowDto
            {
                Id = locker.Id,
                Code = locker.Code,
                LibraryId = locker.LibraryId,
                Area = locker.Area,
                Size = locker.Size,
                Status = locker.Status,
                MapRow = locker.MapRow,
                MapColumn = locker.MapColumn,
                Note = locker.Note
            })
            .ToListAsync(ct);

        var ids = lockers.Select(locker => locker.Id).ToList();

        var usages = await _db.LockerUsages
            .AsNoTracking()
            .Where(usage => ids.Contains(usage.LockerId) && usage.CheckoutAt == null)
            .Select(usage => new
            {
                usage.Id,
                usage.LockerId,
                usage.ReaderId,
                ReaderName = usage.Reader!.FullName,
                ReaderCard = usage.Reader!.CardNumber,
                usage.CheckinAt,
                usage.KeyNumber
            })
            .ToListAsync(ct);

        var byLocker = usages.ToDictionary(usage => usage.LockerId);

        foreach (var locker in lockers)
        {
            if (!byLocker.TryGetValue(locker.Id, out var usage))
            {
                continue;
            }

            locker.UsageId = usage.Id;
            locker.ReaderId = usage.ReaderId;
            locker.ReaderName = usage.ReaderName;
            locker.ReaderCardNumber = usage.ReaderCard;
            locker.CheckinAt = usage.CheckinAt;
            locker.KeyNumber = usage.KeyNumber;
            locker.MinutesInUse = (int)Math.Round((now - usage.CheckinAt).TotalMinutes);
            locker.Overdue = overdueMinutes > 0 && locker.MinutesInUse > overdueMinutes;
        }

        return new LockerMapDto
        {
            Areas = lockers
                .Where(locker => !string.IsNullOrWhiteSpace(locker.Area))
                .Select(locker => locker.Area!)
                .Distinct()
                .OrderBy(area => area, StringComparer.CurrentCulture)
                .ToList(),
            Free = lockers.Count(locker => locker.Status == LockerStatus.Free),
            InUse = lockers.Count(locker => locker.Status == LockerStatus.InUse),
            Broken = lockers.Count(locker =>
                locker.Status is LockerStatus.Broken or LockerStatus.Locked),
            Overdue = lockers.Count(locker => locker.Overdue),
            Lockers = lockers
        };
    }
}

public class SaveLockerCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid? LibraryId { get; set; }
    public string? Area { get; set; }
    public string? Size { get; set; }
    public LockerStatus Status { get; set; } = LockerStatus.Free;
    public int? MapRow { get; set; }
    public int? MapColumn { get; set; }
    public string? Note { get; set; }
}

public class SaveLockerCommandValidator : AbstractValidator<SaveLockerCommand>
{
    public SaveLockerCommandValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty().WithMessage("Chưa nhập số tủ.").MaximumLength(50);
        RuleFor(command => command.Area).MaximumLength(100);
        RuleFor(command => command.Size).MaximumLength(50);
    }
}

public class SaveLockerCommandHandler : IRequestHandler<SaveLockerCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveLockerCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveLockerCommand command, CancellationToken ct)
    {
        var code = command.Code.Trim();

        Locker locker;

        if (command.Id is null)
        {
            locker = new Locker();
            _db.Lockers.Add(locker);
        }
        else
        {
            locker = await _db.Lockers.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
                ?? throw new NotFoundException("tủ gửi đồ", command.Id);
        }

        if (await _db.Lockers.AnyAsync(other => other.Id != locker.Id && other.Code == code, ct))
        {
            throw new Common.Exceptions.ValidationException("code", $"Tủ số {code} đã có.");
        }

        if (locker.Status == LockerStatus.InUse && command.Status != LockerStatus.InUse)
        {
            var busy = await _db.LockerUsages.AnyAsync(
                usage => usage.LockerId == locker.Id && usage.CheckoutAt == null, ct);

            if (busy)
            {
                throw new ConflictException(
                    "Tủ đang có bạn đọc sử dụng; phải trả tủ trước khi đổi trạng thái.");
            }
        }

        locker.Code = code;
        locker.LibraryId = command.LibraryId;
        locker.Area = command.Area?.Trim();
        locker.Size = command.Size?.Trim();
        locker.Status = command.Status;
        locker.MapRow = command.MapRow;
        locker.MapColumn = command.MapColumn;
        locker.Note = command.Note?.Trim();

        await _db.SaveChangesAsync(ct);
        return locker.Id;
    }
}

public record DeleteLockerCommand(Guid Id) : IRequest;

public class DeleteLockerCommandHandler : IRequestHandler<DeleteLockerCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteLockerCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteLockerCommand command, CancellationToken ct)
    {
        var locker = await _db.Lockers.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
            ?? throw new NotFoundException("tủ gửi đồ", command.Id);

        var used = await _db.LockerUsages.AnyAsync(usage => usage.LockerId == locker.Id, ct);

        if (used)
        {
            throw new ConflictException(
                $"Tủ {locker.Code} đã có lịch sử sử dụng nên chỉ ngừng dùng được, không xóa được.");
        }

        _db.Lockers.Remove(locker);
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Giao tủ cho bạn đọc: quét thẻ, chọn tủ trống, ghi số chìa.</summary>
public class AssignLockerCommand : IRequest<LockerRowDto>
{
    public Guid LockerId { get; set; }
    /// <summary>Số thẻ hoặc mã sinh viên quét ở quầy.</summary>
    public string CardNumber { get; set; } = string.Empty;
    public string? KeyNumber { get; set; }
    public string? Note { get; set; }
}

public class AssignLockerCommandHandler : IRequestHandler<AssignLockerCommand, LockerRowDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICirculationDeskService _desk;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ISender _mediator;

    public AssignLockerCommandHandler(
        IApplicationDbContext db,
        ICirculationDeskService desk,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        ISender mediator)
    {
        _db = db;
        _desk = desk;
        _currentUser = currentUser;
        _clock = clock;
        _mediator = mediator;
    }

    public async Task<LockerRowDto> Handle(AssignLockerCommand command, CancellationToken ct)
    {
        var reader = await _desk.FindReaderByCardAsync(command.CardNumber, ct);

        var locker = await _db.Lockers.FirstOrDefaultAsync(entity => entity.Id == command.LockerId, ct)
            ?? throw new NotFoundException("tủ gửi đồ", command.LockerId);

        if (locker.Status is LockerStatus.Broken or LockerStatus.Locked)
        {
            throw new ConflictException($"Tủ {locker.Code} đang hỏng hoặc bị khóa.");
        }

        var busy = await _db.LockerUsages.AnyAsync(
            usage => usage.LockerId == locker.Id && usage.CheckoutAt == null, ct);

        if (busy)
        {
            throw new ConflictException($"Tủ {locker.Code} đang có người dùng.");
        }

        // Một bạn đọc chỉ giữ một tủ: giữ hai tủ là chiếm chỗ của người khác.
        var holding = await _db.LockerUsages
            .Where(usage => usage.ReaderId == reader.Id && usage.CheckoutAt == null)
            .Select(usage => usage.Locker!.Code)
            .FirstOrDefaultAsync(ct);

        if (holding is not null)
        {
            throw new ConflictException(
                $"Bạn đọc {reader.FullName} đang giữ tủ {holding}, phải trả tủ đó trước.");
        }

        _db.LockerUsages.Add(new LockerUsage
        {
            LockerId = locker.Id,
            ReaderId = reader.Id,
            CheckinAt = _clock.Now,
            KeyNumber = command.KeyNumber?.Trim(),
            IssuedBy = _currentUser.UserId,
            Note = command.Note?.Trim()
        });

        locker.Status = LockerStatus.InUse;
        await _db.SaveChangesAsync(ct);

        var map = await _mediator.Send(new GetLockerMapQuery(locker.LibraryId, locker.Area), ct);
        return map.Lockers.First(row => row.Id == locker.Id);
    }
}

/// <summary>Trả tủ: quét thẻ bạn đọc hoặc nhập số tủ.</summary>
public class ReleaseLockerCommand : IRequest<LockerUsageRowDto>
{
    public Guid? LockerId { get; set; }
    public string? CardNumber { get; set; }
    public string? Note { get; set; }
}

public class ReleaseLockerCommandHandler : IRequestHandler<ReleaseLockerCommand, LockerUsageRowDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICirculationDeskService _desk;
    private readonly IDateTimeProvider _clock;

    public ReleaseLockerCommandHandler(
        IApplicationDbContext db, ICirculationDeskService desk, IDateTimeProvider clock)
    {
        _db = db;
        _desk = desk;
        _clock = clock;
    }

    public async Task<LockerUsageRowDto> Handle(ReleaseLockerCommand command, CancellationToken ct)
    {
        LockerUsage? usage = null;

        if (command.LockerId is not null)
        {
            usage = await _db.LockerUsages
                .Include(entity => entity.Locker)
                .FirstOrDefaultAsync(entity =>
                    entity.LockerId == command.LockerId && entity.CheckoutAt == null, ct);
        }
        else if (!string.IsNullOrWhiteSpace(command.CardNumber))
        {
            var reader = await _desk.FindReaderByCardAsync(command.CardNumber, ct);

            usage = await _db.LockerUsages
                .Include(entity => entity.Locker)
                .FirstOrDefaultAsync(entity =>
                    entity.ReaderId == reader.Id && entity.CheckoutAt == null, ct);
        }
        else
        {
            throw new Common.Exceptions.ValidationException(
                "lockerId", "Phải chọn tủ hoặc quét thẻ bạn đọc.");
        }

        if (usage is null)
        {
            throw new NotFoundException("Không tìm thấy lượt giữ tủ nào đang mở.");
        }

        usage.CheckoutAt = _clock.Now;

        if (!string.IsNullOrWhiteSpace(command.Note))
        {
            usage.Note = string.IsNullOrWhiteSpace(usage.Note)
                ? command.Note
                : $"{usage.Note}\n{command.Note}";
        }

        if (usage.Locker is not null && usage.Locker.Status == LockerStatus.InUse)
        {
            usage.Locker.Status = LockerStatus.Free;
        }

        await _db.SaveChangesAsync(ct);

        return await _db.LockerUsages
            .AsNoTracking()
            .Where(entity => entity.Id == usage.Id)
            .Select(LockerUsageProjection)
            .FirstAsync(ct);
    }

    internal static readonly System.Linq.Expressions.Expression<Func<LockerUsage, LockerUsageRowDto>>
        LockerUsageProjection = usage => new LockerUsageRowDto
        {
            Id = usage.Id,
            LockerId = usage.LockerId,
            LockerCode = usage.Locker!.Code,
            Area = usage.Locker!.Area,
            ReaderId = usage.ReaderId,
            ReaderName = usage.Reader!.FullName,
            ReaderCardNumber = usage.Reader!.CardNumber,
            CheckinAt = usage.CheckinAt,
            CheckoutAt = usage.CheckoutAt,
            KeyNumber = usage.KeyNumber,
            Note = usage.Note
        };
}

/// <summary>Lịch sử sử dụng tủ.</summary>
public class LockerUsageListRequest : PagedRequest
{
    public Guid? LockerId { get; set; }
    public Guid? ReaderId { get; set; }
    public bool? OpenOnly { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public record SearchLockerUsagesQuery(LockerUsageListRequest Request)
    : IRequest<PagedResult<LockerUsageRowDto>>;

public class SearchLockerUsagesQueryHandler
    : IRequestHandler<SearchLockerUsagesQuery, PagedResult<LockerUsageRowDto>>
{
    private readonly IApplicationDbContext _db;

    public SearchLockerUsagesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<LockerUsageRowDto>> Handle(
        SearchLockerUsagesQuery query, CancellationToken ct)
    {
        var request = query.Request;

        var usages = _db.LockerUsages
            .AsNoTracking()
            .WhereIf(request.LockerId is not null, usage => usage.LockerId == request.LockerId)
            .WhereIf(request.ReaderId is not null, usage => usage.ReaderId == request.ReaderId)
            .WhereIf(request.OpenOnly == true, usage => usage.CheckoutAt == null)
            .WhereIf(request.FromDate is not null,
                usage => usage.CheckinAt >= request.FromDate!.Value.ToDateTime(TimeOnly.MinValue))
            .WhereIf(request.ToDate is not null,
                usage => usage.CheckinAt < request.ToDate!.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));

        var page = await usages
            .OrderByDescending(usage => usage.CheckinAt)
            .Select(ReleaseLockerCommandHandler.LockerUsageProjection)
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
