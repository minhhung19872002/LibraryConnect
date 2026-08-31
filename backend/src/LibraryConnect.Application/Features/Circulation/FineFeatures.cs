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
// VII.2 — Thu tiền phạt và miễn giảm.
// ---------------------------------------------------------------------------------------------

public record SearchFinesQuery(FineListRequest Request) : IRequest<PagedResult<FineRowDto>>;

public class SearchFinesQueryHandler : IRequestHandler<SearchFinesQuery, PagedResult<FineRowDto>>
{
    private readonly IApplicationDbContext _db;

    public SearchFinesQueryHandler(IApplicationDbContext db) => _db = db;

    public Task<PagedResult<FineRowDto>> Handle(SearchFinesQuery query, CancellationToken ct)
    {
        var request = query.Request;

        var fines = FineQuery.Base(_db)
            .WhereIf(request.ReaderId is not null, fine => fine.ReaderId == request.ReaderId)
            .WhereIf(request.Type is not null, fine => fine.Type == request.Type)
            .WhereIf(request.OutstandingOnly == true,
                fine => !fine.Waived && fine.Amount > fine.PaidAmount)
            .WhereIf(request.FromDate is not null,
                fine => fine.CreatedAt >= request.FromDate!.Value.ToDateTime(TimeOnly.MinValue))
            .WhereIf(request.ToDate is not null,
                fine => fine.CreatedAt < request.ToDate!.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));

        if (request.HasKeyword())
        {
            var keyword = request.Keyword!.Trim().ToLowerInvariant();
            var unaccented = Common.Text.VietnameseText.RemoveDiacritics(keyword);

            fines = fines.Where(fine =>
                fine.Code.ToLower().Contains(keyword)
                || fine.Reader!.CardNumber.ToLower().Contains(keyword)
                || DatabaseFunctions.Unaccent(fine.Reader!.FullName).Contains(unaccented));
        }

        return fines
            .OrderByDescending(fine => fine.CreatedAt)
            .Select(FineQuery.Projection)
            .ToPagedResultAsync(request, ct);
    }
}

/// <summary>Tổng nợ của một bạn đọc, hiện ở màn hình thu tiền.</summary>
public record GetReaderFineSummaryQuery(Guid ReaderId) : IRequest<ReaderFineSummaryDto>;

public class ReaderFineSummaryDto
{
    public Guid ReaderId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public decimal TotalOutstanding { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalWaived { get; set; }
    public List<FineRowDto> Fines { get; set; } = new();
}

public class GetReaderFineSummaryQueryHandler
    : IRequestHandler<GetReaderFineSummaryQuery, ReaderFineSummaryDto>
{
    private readonly IApplicationDbContext _db;

    public GetReaderFineSummaryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<ReaderFineSummaryDto> Handle(
        GetReaderFineSummaryQuery query, CancellationToken ct)
    {
        var reader = await _db.Readers
            .AsNoTracking()
            .Where(entity => entity.Id == query.ReaderId)
            .Select(entity => new { entity.Id, entity.CardNumber, entity.FullName })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("bạn đọc", query.ReaderId);

        var fines = await FineQuery.Base(_db)
            .Where(fine => fine.ReaderId == query.ReaderId)
            .OrderByDescending(fine => fine.CreatedAt)
            .Select(FineQuery.Projection)
            .ToListAsync(ct);

        return new ReaderFineSummaryDto
        {
            ReaderId = reader.Id,
            CardNumber = reader.CardNumber,
            FullName = reader.FullName,
            TotalOutstanding = fines.Sum(fine => fine.Outstanding),
            TotalPaid = fines.Sum(fine => fine.PaidAmount),
            TotalWaived = fines.Where(fine => fine.Waived).Sum(fine => fine.Amount - fine.PaidAmount),
            Fines = fines
        };
    }
}

/// <summary>Ghi một khoản phạt thủ công (làm mất thẻ, vi phạm nội quy…).</summary>
public class CreateFineCommand : IRequest<FineRowDto>
{
    public Guid ReaderId { get; set; }
    public FineType Type { get; set; } = FineType.Other;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public class CreateFineCommandValidator : AbstractValidator<CreateFineCommand>
{
    public CreateFineCommandValidator()
    {
        RuleFor(command => command.ReaderId).NotEmpty().WithMessage("Chưa chọn bạn đọc.");
        RuleFor(command => command.Amount)
            .GreaterThan(0).WithMessage("Số tiền phạt phải lớn hơn 0.");
        RuleFor(command => command.Note).MaximumLength(1000);
    }
}

public class CreateFineCommandHandler : IRequestHandler<CreateFineCommand, FineRowDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICodeGenerator _codes;

    public CreateFineCommandHandler(IApplicationDbContext db, ICodeGenerator codes)
    {
        _db = db;
        _codes = codes;
    }

    public async Task<FineRowDto> Handle(CreateFineCommand command, CancellationToken ct)
    {
        if (!await _db.Readers.AnyAsync(reader => reader.Id == command.ReaderId, ct))
        {
            throw new NotFoundException("bạn đọc", command.ReaderId);
        }

        var fine = new Fine
        {
            Code = await _codes.NextAsync("FINE", ct),
            ReaderId = command.ReaderId,
            Type = command.Type,
            Amount = command.Amount,
            Note = command.Note
        };

        _db.Fines.Add(fine);
        await _db.SaveChangesAsync(ct);

        return await FineQuery.Base(_db)
            .Where(entity => entity.Id == fine.Id)
            .Select(FineQuery.Projection)
            .FirstAsync(ct);
    }
}

/// <summary>
/// Thu tiền phạt. Thu được từng phần: bạn đọc trả trước một nửa là chuyện bình thường ở quầy.
/// </summary>
public class PayFineCommand : IRequest<FineRowDto>
{
    public Guid FineId { get; set; }
    /// <summary>Bỏ trống là thu hết phần còn nợ.</summary>
    public decimal? Amount { get; set; }
    public string? Note { get; set; }
}

public class PayFineCommandHandler : IRequestHandler<PayFineCommand, FineRowDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public PayFineCommandHandler(
        IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<FineRowDto> Handle(PayFineCommand command, CancellationToken ct)
    {
        var fine = await _db.Fines
            .Include(entity => entity.Loan)
            .FirstOrDefaultAsync(entity => entity.Id == command.FineId, ct)
            ?? throw new NotFoundException("khoản phạt", command.FineId);

        if (fine.Waived)
        {
            throw new ConflictException("Khoản phạt này đã được miễn nên không thu tiền.");
        }

        var outstanding = fine.Amount - fine.PaidAmount;

        if (outstanding <= 0)
        {
            throw new ConflictException("Khoản phạt này đã thu đủ.");
        }

        var amount = command.Amount ?? outstanding;

        if (amount <= 0)
        {
            throw new Common.Exceptions.ValidationException("amount", "Số tiền thu phải lớn hơn 0.");
        }

        if (amount > outstanding)
        {
            throw new Common.Exceptions.ValidationException(
                "amount", $"Số tiền thu vượt quá phần còn nợ ({outstanding:#,##0} đ).");
        }

        fine.PaidAmount += amount;
        fine.PaidAt = _clock.Now;
        fine.PaidBy = _currentUser.UserId;
        fine.PaidByName = _currentUser.FullName;

        if (!string.IsNullOrWhiteSpace(command.Note))
        {
            fine.Note = string.IsNullOrWhiteSpace(fine.Note)
                ? command.Note
                : $"{fine.Note}\n{command.Note}";
        }

        // Cột nợ trên hồ sơ bạn đọc là bộ nhớ đệm để danh sách chạy nhanh; cập nhật cùng lúc để hai
        // chỗ không nói hai con số khác nhau.
        if (fine.Loan is not null)
        {
            fine.Loan.FinePaid += amount;
        }

        await SyncReaderDebtAsync(_db, fine.ReaderId, ct);
        await _db.SaveChangesAsync(ct);

        return await FineQuery.Base(_db)
            .Where(entity => entity.Id == fine.Id)
            .Select(FineQuery.Projection)
            .FirstAsync(ct);
    }

    internal static async Task SyncReaderDebtAsync(
        IApplicationDbContext db, Guid readerId, CancellationToken ct)
    {
        var reader = await db.Readers.FirstOrDefaultAsync(entity => entity.Id == readerId, ct);

        if (reader is null)
        {
            return;
        }

        reader.DebtAmount = await db.Fines
            .Where(fine => fine.ReaderId == readerId && !fine.Waived)
            .SumAsync(fine => (decimal?)(fine.Amount - fine.PaidAmount), ct) ?? 0;
    }
}

/// <summary>Miễn giảm tiền phạt — chỉ người có quyền riêng mới làm được, và phải ghi lý do.</summary>
public class WaiveFineCommand : IRequest<FineRowDto>
{
    public Guid FineId { get; set; }
    public string? Reason { get; set; }
}

public class WaiveFineCommandValidator : AbstractValidator<WaiveFineCommand>
{
    public WaiveFineCommandValidator()
    {
        RuleFor(command => command.Reason)
            .NotEmpty().WithMessage("Phải ghi lý do miễn giảm tiền phạt.")
            .MaximumLength(500);
    }
}

public class WaiveFineCommandHandler : IRequestHandler<WaiveFineCommand, FineRowDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public WaiveFineCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<FineRowDto> Handle(WaiveFineCommand command, CancellationToken ct)
    {
        var fine = await _db.Fines.FirstOrDefaultAsync(entity => entity.Id == command.FineId, ct)
            ?? throw new NotFoundException("khoản phạt", command.FineId);

        if (fine.Waived)
        {
            throw new ConflictException("Khoản phạt này đã được miễn trước đó.");
        }

        fine.Waived = true;
        fine.WaiveReason = command.Reason?.Trim();
        fine.WaivedBy = _currentUser.UserId;

        await PayFineCommandHandler.SyncReaderDebtAsync(_db, fine.ReaderId, ct);
        await _db.SaveChangesAsync(ct);

        return await FineQuery.Base(_db)
            .Where(entity => entity.Id == fine.Id)
            .Select(FineQuery.Projection)
            .FirstAsync(ct);
    }
}
