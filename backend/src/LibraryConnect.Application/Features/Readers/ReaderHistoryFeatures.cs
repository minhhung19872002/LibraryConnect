using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Entities.Rdr;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Readers;

// ---------------------------------------------------------------------------------------------
// VI.1 — Tab lịch sử của hồ sơ bạn đọc: sách đang mượn, lịch sử mượn trả, tiền phạt, vi phạm,
// lượt vào thư viện và tài liệu số đã truy cập.
// ---------------------------------------------------------------------------------------------

public class ReaderLoanDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? Title { get; set; }
    public DateTimeOffset LoanDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateTimeOffset? ReturnDate { get; set; }
    public int RenewedCount { get; set; }
    public LoanStatus Status { get; set; }
    public decimal FineAmount { get; set; }
    /// <summary>Số ngày quá hạn tính tới hôm nay, hoặc tới ngày trả nếu đã trả.</summary>
    public int OverdueDays { get; set; }
}

public class ReaderFineDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public FineType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Outstanding { get; set; }
    public bool Waived { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? Note { get; set; }
}

public class ReaderVisitDto
{
    public Guid Id { get; set; }
    public DateTimeOffset CheckinAt { get; set; }
    public DateTimeOffset? CheckoutAt { get; set; }
    public string? Gate { get; set; }
    public string? Purpose { get; set; }
    public int? Minutes { get; set; }
}

public class ReaderDigitalAccessDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string? DocumentTitle { get; set; }
    public DigitalAccessAction Action { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public int? DurationSeconds { get; set; }
    public string? Ip { get; set; }
}

public class ReaderViolationDto
{
    public Guid Id { get; set; }
    public Guid ReaderId { get; set; }
    public Guid? ViolationTypeId { get; set; }
    public string? ViolationTypeName { get; set; }
    public string? Description { get; set; }
    public decimal FineAmount { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? Resolution { get; set; }
}

/// <summary>Sách đang mượn và lịch sử mượn trả của một bạn đọc.</summary>
public record GetReaderLoansQuery(Guid ReaderId, bool CurrentOnly, PagedRequestDefault Request)
    : IRequest<PagedResult<ReaderLoanDto>>;

public class GetReaderLoansQueryHandler : IRequestHandler<GetReaderLoansQuery, PagedResult<ReaderLoanDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetReaderLoansQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PagedResult<ReaderLoanDto>> Handle(GetReaderLoansQuery query, CancellationToken ct)
    {
        var today = _clock.Today;

        var loans = _db.Loans
            .AsNoTracking()
            .Where(loan => loan.ReaderId == query.ReaderId)
            .WhereIf(query.CurrentOnly,
                loan => loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue)
            .OrderByDescending(loan => loan.LoanDate);

        var result = await loans
            .Select(loan => new ReaderLoanDto
            {
                Id = loan.Id,
                Code = loan.Code,
                Barcode = loan.Barcode,
                Title = loan.BibTitle,
                LoanDate = loan.LoanDate,
                DueDate = loan.DueDate,
                ReturnDate = loan.ReturnDate,
                RenewedCount = loan.RenewedCount,
                Status = loan.Status,
                FineAmount = loan.FineAmount
            })
            .ToPagedResultAsync(query.Request, ct);

        foreach (var row in result.Items)
        {
            var end = row.ReturnDate is null
                ? today
                : DateOnly.FromDateTime(row.ReturnDate.Value.LocalDateTime);

            row.OverdueDays = Math.Max(0, end.DayNumber - row.DueDate.DayNumber);
        }

        return result;
    }
}

/// <summary>Tiền phạt của một bạn đọc.</summary>
public record GetReaderFinesQuery(Guid ReaderId, bool OutstandingOnly, PagedRequestDefault Request)
    : IRequest<PagedResult<ReaderFineDto>>;

public class GetReaderFinesQueryHandler : IRequestHandler<GetReaderFinesQuery, PagedResult<ReaderFineDto>>
{
    private readonly IApplicationDbContext _db;

    public GetReaderFinesQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<PagedResult<ReaderFineDto>> Handle(GetReaderFinesQuery query, CancellationToken ct) =>
        _db.Fines
            .AsNoTracking()
            .Where(fine => fine.ReaderId == query.ReaderId)
            .WhereIf(query.OutstandingOnly, fine => !fine.Waived && fine.Amount > fine.PaidAmount)
            .OrderByDescending(fine => fine.CreatedAt)
            .Select(fine => new ReaderFineDto
            {
                Id = fine.Id,
                Code = fine.Code,
                Type = fine.Type,
                Amount = fine.Amount,
                PaidAmount = fine.PaidAmount,
                Outstanding = fine.Waived ? 0 : fine.Amount - fine.PaidAmount,
                Waived = fine.Waived,
                PaidAt = fine.PaidAt,
                CreatedAt = fine.CreatedAt,
                Note = fine.Note
            })
            .ToPagedResultAsync(query.Request, ct);
}

/// <summary>Lượt vào thư viện của một bạn đọc.</summary>
public record GetReaderVisitsQuery(Guid ReaderId, PagedRequestDefault Request)
    : IRequest<PagedResult<ReaderVisitDto>>;

public class GetReaderVisitsQueryHandler : IRequestHandler<GetReaderVisitsQuery, PagedResult<ReaderVisitDto>>
{
    private readonly IApplicationDbContext _db;

    public GetReaderVisitsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<ReaderVisitDto>> Handle(GetReaderVisitsQuery query, CancellationToken ct)
    {
        var result = await _db.LibraryVisits
            .AsNoTracking()
            .Where(visit => visit.ReaderId == query.ReaderId)
            .OrderByDescending(visit => visit.CheckinAt)
            .Select(visit => new ReaderVisitDto
            {
                Id = visit.Id,
                CheckinAt = visit.CheckinAt,
                CheckoutAt = visit.CheckoutAt,
                Gate = visit.Gate,
                Purpose = visit.Purpose
            })
            .ToPagedResultAsync(query.Request, ct);

        foreach (var row in result.Items)
        {
            row.Minutes = row.CheckoutAt is null
                ? null
                : (int)Math.Round((row.CheckoutAt.Value - row.CheckinAt).TotalMinutes);
        }

        return result;
    }
}

/// <summary>Tài liệu số bạn đọc đã xem hoặc tải.</summary>
public record GetReaderDigitalAccessQuery(Guid ReaderId, PagedRequestDefault Request)
    : IRequest<PagedResult<ReaderDigitalAccessDto>>;

public class GetReaderDigitalAccessQueryHandler
    : IRequestHandler<GetReaderDigitalAccessQuery, PagedResult<ReaderDigitalAccessDto>>
{
    private readonly IApplicationDbContext _db;

    public GetReaderDigitalAccessQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<PagedResult<ReaderDigitalAccessDto>> Handle(
        GetReaderDigitalAccessQuery query, CancellationToken ct) =>
        _db.DigitalAccessLogs
            .AsNoTracking()
            .Where(log => log.ReaderId == query.ReaderId)
            .OrderByDescending(log => log.OccurredAt)
            .Select(log => new ReaderDigitalAccessDto
            {
                Id = log.Id,
                DocumentId = log.DocumentId,
                DocumentTitle = log.Document!.Title,
                Action = log.Action,
                OccurredAt = log.OccurredAt,
                DurationSeconds = log.DurationSeconds,
                Ip = log.Ip
            })
            .ToPagedResultAsync(query.Request, ct);
}

// ---------------------------------------------------------------------------------------------
// Vi phạm của bạn đọc — danh sách, ghi nhận và xử lý.
// ---------------------------------------------------------------------------------------------

public record GetReaderViolationsQuery(Guid ReaderId, PagedRequestDefault Request)
    : IRequest<PagedResult<ReaderViolationDto>>;

public class GetReaderViolationsQueryHandler
    : IRequestHandler<GetReaderViolationsQuery, PagedResult<ReaderViolationDto>>
{
    private readonly IApplicationDbContext _db;

    public GetReaderViolationsQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<PagedResult<ReaderViolationDto>> Handle(
        GetReaderViolationsQuery query, CancellationToken ct) =>
        _db.ReaderViolations
            .AsNoTracking()
            .Where(violation => violation.ReaderId == query.ReaderId)
            .OrderByDescending(violation => violation.OccurredAt)
            .Select(violation => new ReaderViolationDto
            {
                Id = violation.Id,
                ReaderId = violation.ReaderId,
                ViolationTypeId = violation.ViolationTypeId,
                ViolationTypeName = violation.ViolationType!.Name,
                Description = violation.Description,
                FineAmount = violation.FineAmount,
                OccurredAt = violation.OccurredAt,
                ResolvedAt = violation.ResolvedAt,
                Resolution = violation.Resolution
            })
            .ToPagedResultAsync(query.Request, ct);
}

/// <summary>Ghi nhận hoặc sửa một vi phạm của bạn đọc.</summary>
public class SaveReaderViolationCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    public Guid ReaderId { get; set; }
    public Guid? ViolationTypeId { get; set; }
    public string? Description { get; set; }
    public decimal? FineAmount { get; set; }
    public DateTimeOffset? OccurredAt { get; set; }
    public string? Resolution { get; set; }
    public bool Resolved { get; set; }
}

public class SaveReaderViolationCommandValidator : AbstractValidator<SaveReaderViolationCommand>
{
    public SaveReaderViolationCommandValidator()
    {
        RuleFor(command => command.ReaderId).NotEmpty();
        RuleFor(command => command.Description).MaximumLength(1000);
        RuleFor(command => command.FineAmount)
            .GreaterThanOrEqualTo(0).When(command => command.FineAmount is not null)
            .WithMessage("Mức phạt không được âm.");

        RuleFor(command => command)
            .Must(command => command.ViolationTypeId is not null
                             || !string.IsNullOrWhiteSpace(command.Description))
            .WithMessage("Chọn loại vi phạm hoặc mô tả vi phạm.")
            .OverridePropertyName(nameof(SaveReaderViolationCommand.ViolationTypeId));
    }
}

public class SaveReaderViolationCommandHandler : IRequestHandler<SaveReaderViolationCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public SaveReaderViolationCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Guid> Handle(SaveReaderViolationCommand command, CancellationToken ct)
    {
        if (!await _db.Readers.AnyAsync(reader => reader.Id == command.ReaderId, ct))
        {
            throw new NotFoundException("bạn đọc", command.ReaderId);
        }

        ReaderViolation violation;

        if (command.Id is null)
        {
            violation = new ReaderViolation
            {
                ReaderId = command.ReaderId,
                OccurredAt = command.OccurredAt ?? _clock.Now
            };

            _db.ReaderViolations.Add(violation);
        }
        else
        {
            violation = await _db.ReaderViolations
                .FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
                ?? throw new NotFoundException("vi phạm", command.Id);

            if (command.OccurredAt is not null)
            {
                violation.OccurredAt = command.OccurredAt.Value;
            }
        }

        violation.ViolationTypeId = command.ViolationTypeId;
        violation.Description = string.IsNullOrWhiteSpace(command.Description)
            ? null
            : command.Description.Trim();

        // Mức phạt bỏ trống thì lấy mức mặc định của loại vi phạm — cán bộ vẫn sửa đè được cho từng
        // trường hợp cụ thể.
        if (command.FineAmount is not null)
        {
            violation.FineAmount = command.FineAmount.Value;
        }
        else if (command.ViolationTypeId is not null && command.Id is null)
        {
            violation.FineAmount = await _db.ViolationTypes
                .Where(type => type.Id == command.ViolationTypeId)
                .Select(type => type.DefaultFine)
                .FirstOrDefaultAsync(ct);
        }

        violation.Resolution = string.IsNullOrWhiteSpace(command.Resolution)
            ? null
            : command.Resolution.Trim();

        violation.ResolvedAt = command.Resolved
            ? violation.ResolvedAt ?? _clock.Now
            : null;

        await _db.SaveChangesAsync(ct);
        return violation.Id;
    }
}

public record DeleteReaderViolationCommand(Guid Id) : IRequest;

public class DeleteReaderViolationCommandHandler : IRequestHandler<DeleteReaderViolationCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteReaderViolationCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteReaderViolationCommand command, CancellationToken ct)
    {
        var violation = await _db.ReaderViolations
            .FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
            ?? throw new NotFoundException("vi phạm", command.Id);

        _db.ReaderViolations.Remove(violation);
        await _db.SaveChangesAsync(ct);
    }
}
