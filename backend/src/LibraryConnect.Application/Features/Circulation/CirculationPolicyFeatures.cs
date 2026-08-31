using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Cat;
using LibraryConnect.Domain.Entities.Cir;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Circulation;

// ---------------------------------------------------------------------------------------------
// VII.1 — Chính sách lưu thông và lịch nghỉ.
// ---------------------------------------------------------------------------------------------

public class CirculationPolicyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ReaderTypeId { get; set; }
    public string? ReaderTypeName { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public string? DocumentTypeName { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }

    public int MaxItems { get; set; }
    public int LoanDays { get; set; }
    public int MaxRenewals { get; set; }
    public int RenewalDays { get; set; }
    public decimal FinePerDay { get; set; }
    public int GraceDays { get; set; }
    public int MaxHolds { get; set; }
    public int HoldExpireDays { get; set; }
    public bool AllowLoan { get; set; }
    public bool AllowRenew { get; set; }
    public bool AllowHold { get; set; }
    public bool AllowTakeHome { get; set; }
    public bool RequireRenewalApproval { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
}

public record GetCirculationPoliciesQuery(bool IncludeInactive = false)
    : IRequest<IReadOnlyList<CirculationPolicyDto>>;

public class GetCirculationPoliciesQueryHandler
    : IRequestHandler<GetCirculationPoliciesQuery, IReadOnlyList<CirculationPolicyDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCirculationPoliciesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<CirculationPolicyDto>> Handle(
        GetCirculationPoliciesQuery query, CancellationToken ct) =>
        await _db.CirculationPolicies
            .AsNoTracking()
            .Where(policy => query.IncludeInactive || policy.IsActive)
            .OrderByDescending(policy => policy.Priority)
            .ThenBy(policy => policy.Name)
            .Select(policy => new CirculationPolicyDto
            {
                Id = policy.Id,
                Name = policy.Name,
                ReaderTypeId = policy.ReaderTypeId,
                ReaderTypeName = policy.ReaderType!.Name,
                DocumentTypeId = policy.DocumentTypeId,
                DocumentTypeName = policy.DocumentType!.Name,
                WarehouseId = policy.WarehouseId,
                WarehouseName = policy.Warehouse!.Name,
                MaxItems = policy.MaxItems,
                LoanDays = policy.LoanDays,
                MaxRenewals = policy.MaxRenewals,
                RenewalDays = policy.RenewalDays,
                FinePerDay = policy.FinePerDay,
                GraceDays = policy.GraceDays,
                MaxHolds = policy.MaxHolds,
                HoldExpireDays = policy.HoldExpireDays,
                AllowLoan = policy.AllowLoan,
                AllowRenew = policy.AllowRenew,
                AllowHold = policy.AllowHold,
                AllowTakeHome = policy.AllowTakeHome,
                RequireRenewalApproval = policy.RequireRenewalApproval,
                Priority = policy.Priority,
                IsActive = policy.IsActive
            })
            .ToListAsync(ct);
}

public class SaveCirculationPolicyCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ReaderTypeId { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public Guid? WarehouseId { get; set; }
    public int MaxItems { get; set; } = 5;
    public int LoanDays { get; set; } = 14;
    public int MaxRenewals { get; set; } = 2;
    public int RenewalDays { get; set; } = 7;
    public decimal FinePerDay { get; set; }
    public int GraceDays { get; set; }
    public int MaxHolds { get; set; } = 3;
    public int HoldExpireDays { get; set; } = 3;
    public bool AllowLoan { get; set; } = true;
    public bool AllowRenew { get; set; } = true;
    public bool AllowHold { get; set; } = true;
    public bool AllowTakeHome { get; set; } = true;
    public bool RequireRenewalApproval { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsActive { get; set; } = true;
}

public class SaveCirculationPolicyCommandValidator : AbstractValidator<SaveCirculationPolicyCommand>
{
    public SaveCirculationPolicyCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Chưa đặt tên chính sách.").MaximumLength(300);

        RuleFor(command => command.MaxItems)
            .InclusiveBetween(0, 200).WithMessage("Số tài liệu mượn tối đa phải từ 0 đến 200.");
        RuleFor(command => command.LoanDays)
            .InclusiveBetween(1, 730).WithMessage("Số ngày mượn phải từ 1 đến 730.");
        RuleFor(command => command.MaxRenewals)
            .InclusiveBetween(0, 20).WithMessage("Số lần gia hạn tối đa phải từ 0 đến 20.");
        RuleFor(command => command.RenewalDays)
            .InclusiveBetween(1, 365).WithMessage("Số ngày mỗi lần gia hạn phải từ 1 đến 365.");
        RuleFor(command => command.FinePerDay)
            .GreaterThanOrEqualTo(0).WithMessage("Tiền phạt mỗi ngày không được âm.");
        RuleFor(command => command.GraceDays)
            .InclusiveBetween(0, 60).WithMessage("Số ngày ân hạn phải từ 0 đến 60.");
        RuleFor(command => command.MaxHolds)
            .InclusiveBetween(0, 50).WithMessage("Số đặt giữ tối đa phải từ 0 đến 50.");
        RuleFor(command => command.HoldExpireDays)
            .InclusiveBetween(1, 60).WithMessage("Số ngày giữ chỗ phải từ 1 đến 60.");
        RuleFor(command => command.Priority)
            .InclusiveBetween(0, 1000).WithMessage("Độ ưu tiên phải từ 0 đến 1000.");
    }
}

public class SaveCirculationPolicyCommandHandler : IRequestHandler<SaveCirculationPolicyCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveCirculationPolicyCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveCirculationPolicyCommand command, CancellationToken ct)
    {
        CirculationPolicy policy;

        if (command.Id is null)
        {
            policy = new CirculationPolicy();
            _db.CirculationPolicies.Add(policy);
        }
        else
        {
            policy = await _db.CirculationPolicies
                .FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
                ?? throw new NotFoundException("chính sách lưu thông", command.Id);
        }

        // Hai ô cùng tọa độ trong ma trận là nguồn gốc của những lần "sao hôm nay cho mượn 3 quyển
        // mà hôm qua 5 quyển"; chặn ngay lúc lưu.
        var duplicate = await _db.CirculationPolicies.AnyAsync(other =>
            other.Id != policy.Id
            && other.ReaderTypeId == command.ReaderTypeId
            && other.DocumentTypeId == command.DocumentTypeId
            && other.WarehouseId == command.WarehouseId
            && other.Priority == command.Priority, ct);

        if (duplicate)
        {
            throw new Common.Exceptions.ValidationException("priority",
                "Đã có chính sách khác cùng loại bạn đọc, dạng tài liệu, kho và độ ưu tiên. " +
                "Hãy đổi độ ưu tiên để biết ô nào thắng.");
        }

        policy.Name = command.Name.Trim();
        policy.ReaderTypeId = command.ReaderTypeId;
        policy.DocumentTypeId = command.DocumentTypeId;
        policy.WarehouseId = command.WarehouseId;
        policy.MaxItems = command.MaxItems;
        policy.LoanDays = command.LoanDays;
        policy.MaxRenewals = command.MaxRenewals;
        policy.RenewalDays = command.RenewalDays;
        policy.FinePerDay = command.FinePerDay;
        policy.GraceDays = command.GraceDays;
        policy.MaxHolds = command.MaxHolds;
        policy.HoldExpireDays = command.HoldExpireDays;
        policy.AllowLoan = command.AllowLoan;
        policy.AllowRenew = command.AllowRenew;
        policy.AllowHold = command.AllowHold;
        policy.AllowTakeHome = command.AllowTakeHome;
        policy.RequireRenewalApproval = command.RequireRenewalApproval;
        policy.Priority = command.Priority;
        policy.IsActive = command.IsActive;

        await _db.SaveChangesAsync(ct);
        return policy.Id;
    }
}

public record DeleteCirculationPolicyCommand(Guid Id) : IRequest;

public class DeleteCirculationPolicyCommandHandler : IRequestHandler<DeleteCirculationPolicyCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteCirculationPolicyCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteCirculationPolicyCommand command, CancellationToken ct)
    {
        var policy = await _db.CirculationPolicies
            .FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
            ?? throw new NotFoundException("chính sách lưu thông", command.Id);

        _db.CirculationPolicies.Remove(policy);
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Thử một chính sách trước khi áp dụng: chọn bạn đọc, dạng tài liệu và kho rồi xem ô nào thắng.
/// </summary>
public record PreviewPolicyQuery(Guid? ReaderTypeId, Guid? DocumentTypeId, Guid? WarehouseId)
    : IRequest<EffectivePolicy>;

public class PreviewPolicyQueryHandler : IRequestHandler<PreviewPolicyQuery, EffectivePolicy>
{
    private readonly ICirculationPolicyResolver _resolver;

    public PreviewPolicyQueryHandler(ICirculationPolicyResolver resolver) => _resolver = resolver;

    public Task<EffectivePolicy> Handle(PreviewPolicyQuery query, CancellationToken ct) =>
        _resolver.ResolveAsync(query.ReaderTypeId, query.DocumentTypeId, query.WarehouseId, ct);
}

// ---------------------------------------------------------------------------------------------
// Lịch nghỉ
// ---------------------------------------------------------------------------------------------

public class HolidayDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public bool IsRecurringYearly { get; set; }
    public Guid? LibraryId { get; set; }
    public string? LibraryName { get; set; }
    public bool IsActive { get; set; }
}

public record GetHolidaysQuery(int? Year = null) : IRequest<IReadOnlyList<HolidayDto>>;

public class GetHolidaysQueryHandler : IRequestHandler<GetHolidaysQuery, IReadOnlyList<HolidayDto>>
{
    private readonly IApplicationDbContext _db;

    public GetHolidaysQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<HolidayDto>> Handle(GetHolidaysQuery query, CancellationToken ct)
    {
        var holidays = await _db.Holidays
            .AsNoTracking()
            .OrderBy(holiday => holiday.FromDate)
            .Select(holiday => new HolidayDto
            {
                Id = holiday.Id,
                Name = holiday.Name,
                FromDate = holiday.FromDate,
                ToDate = holiday.ToDate,
                IsRecurringYearly = holiday.IsRecurringYearly,
                LibraryId = holiday.LibraryId,
                IsActive = holiday.IsActive
            })
            .ToListAsync(ct);

        if (query.Year is null)
        {
            return holidays;
        }

        // Kỳ nghỉ lặp hằng năm luôn thuộc mọi năm, nên không lọc theo năm của bản ghi.
        return holidays
            .Where(holiday => holiday.IsRecurringYearly
                              || holiday.FromDate.Year == query.Year
                              || holiday.ToDate.Year == query.Year)
            .ToList();
    }
}

public class SaveHolidayCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public bool IsRecurringYearly { get; set; }
    public Guid? LibraryId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SaveHolidayCommandValidator : AbstractValidator<SaveHolidayCommand>
{
    public SaveHolidayCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Chưa nhập tên ngày nghỉ.").MaximumLength(300);

        RuleFor(command => command)
            .Must(command => command.IsRecurringYearly || command.ToDate >= command.FromDate)
            .WithMessage("Ngày kết thúc phải sau ngày bắt đầu.")
            .OverridePropertyName(nameof(SaveHolidayCommand.ToDate));
    }
}

public class SaveHolidayCommandHandler : IRequestHandler<SaveHolidayCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public SaveHolidayCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveHolidayCommand command, CancellationToken ct)
    {
        Holiday holiday;

        if (command.Id is null)
        {
            holiday = new Holiday();
            _db.Holidays.Add(holiday);
        }
        else
        {
            holiday = await _db.Holidays.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
                ?? throw new NotFoundException("ngày nghỉ", command.Id);
        }

        holiday.Name = command.Name.Trim();
        holiday.FromDate = command.FromDate;
        holiday.ToDate = command.ToDate;
        holiday.IsRecurringYearly = command.IsRecurringYearly;
        holiday.LibraryId = command.LibraryId;
        holiday.IsActive = command.IsActive;

        await _db.SaveChangesAsync(ct);
        return holiday.Id;
    }
}

public record DeleteHolidayCommand(Guid Id) : IRequest;

public class DeleteHolidayCommandHandler : IRequestHandler<DeleteHolidayCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteHolidayCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteHolidayCommand command, CancellationToken ct)
    {
        var holiday = await _db.Holidays.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
            ?? throw new NotFoundException("ngày nghỉ", command.Id);

        _db.Holidays.Remove(holiday);
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Thử lịch: đưa vào một ngày mượn và số ngày mượn, trả về hạn trả sau khi đã tránh ngày nghỉ.
/// Dùng để cán bộ kiểm chứng lịch nghỉ vừa khai có đúng ý không.
/// </summary>
public record PreviewDueDateQuery(DateOnly LoanDate, int LoanDays) : IRequest<DueDatePreviewDto>;

public record DueDatePreviewDto(DateOnly RawDueDate, DateOnly DueDate, bool Shifted, string Explanation);

public class PreviewDueDateQueryHandler : IRequestHandler<PreviewDueDateQuery, DueDatePreviewDto>
{
    private readonly ICirculationCalendarProvider _calendars;

    public PreviewDueDateQueryHandler(ICirculationCalendarProvider calendars) => _calendars = calendars;

    public async Task<DueDatePreviewDto> Handle(PreviewDueDateQuery query, CancellationToken ct)
    {
        var calendar = await _calendars.GetAsync(null, ct);

        var raw = query.LoanDate.AddDays(Math.Max(1, query.LoanDays));
        var due = CirculationRules.DueDate(query.LoanDate, query.LoanDays, calendar);

        return new DueDatePreviewDto(raw, due, due != raw,
            due == raw
                ? $"Hạn trả {due:dd/MM/yyyy} là ngày làm việc."
                : $"Hạn trả {raw:dd/MM/yyyy} rơi vào ngày thư viện đóng cửa nên dời sang {due:dd/MM/yyyy}.");
    }
}
