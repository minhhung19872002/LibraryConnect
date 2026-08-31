using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Admin.Users;
using LibraryConnect.Domain.Entities.Rdr;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Readers;

// ---------------------------------------------------------------------------------------------
// VI.1 — Thao tác trên hồ sơ: gia hạn thẻ, tạm khóa, cấp lại thẻ, chuyển trạng thái ra trường.
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Dựng danh sách bạn đọc cho một thao tác hàng loạt.
///
/// Khi cán bộ chọn "áp dụng cho toàn bộ kết quả lọc", danh sách được dựng lại ở máy chủ từ chính bộ
/// lọc đó chứ không tin vào danh sách định danh do trình duyệt gửi lên — trình duyệt chỉ nhìn thấy
/// trang đang mở, còn bộ lọc có thể ra vài nghìn người.
/// </summary>
internal static class ReaderSelectionResolver
{
    /// <summary>Chặn trên số hồ sơ xử lý một lần, đủ cho cả một khóa tuyển sinh.</summary>
    public const int MaxBulkSize = 5000;

    public static async Task<List<Reader>> ResolveAsync(
        IApplicationDbContext db, ReaderSelectionDto selection, DateOnly today, CancellationToken ct)
    {
        if (selection.UseFilter)
        {
            var filter = selection.Filter ?? new ReaderListRequest();

            var readers = await ReaderQuery.Apply(db, filter, today)
                .AsTracking()
                .OrderBy(reader => reader.FullName)
                .Take(MaxBulkSize + 1)
                .ToListAsync(ct);

            if (readers.Count > MaxBulkSize)
            {
                throw new Common.Exceptions.ValidationException(
                    "filter",
                    $"Bộ lọc đang chọn hơn {MaxBulkSize:#,##0} bạn đọc. Hãy thu hẹp bộ lọc rồi làm thành nhiều đợt.");
            }

            return readers;
        }

        var ids = selection.ReaderIds.Distinct().ToList();

        if (ids.Count == 0)
        {
            throw new Common.Exceptions.ValidationException("readerIds", "Chưa chọn bạn đọc nào.");
        }

        if (ids.Count > MaxBulkSize)
        {
            throw new Common.Exceptions.ValidationException(
                "readerIds", $"Mỗi lần xử lý tối đa {MaxBulkSize:#,##0} bạn đọc.");
        }

        return await db.Readers
            .Where(reader => ids.Contains(reader.Id))
            .OrderBy(reader => reader.FullName)
            .ToListAsync(ct);
    }
}

/// <summary>Gia hạn thẻ, một người hoặc cả một khóa theo bộ lọc (VI.1).</summary>
public class ExtendReaderCardsCommand : IRequest<BulkResultDto>
{
    public ReaderSelectionDto Selection { get; set; } = new();
    /// <summary>Số tháng gia hạn. Bỏ trống thì lấy hạn thẻ của loại bạn đọc.</summary>
    public int? Months { get; set; }
    /// <summary>Đặt thẳng ngày hết hạn mới, dùng khi nhà trường chốt một mốc chung.</summary>
    public DateOnly? NewExpireDate { get; set; }
    public string? Note { get; set; }
}

public class ExtendReaderCardsCommandValidator : AbstractValidator<ExtendReaderCardsCommand>
{
    public ExtendReaderCardsCommandValidator()
    {
        RuleFor(command => command.Months)
            .InclusiveBetween(1, 120).When(command => command.Months is not null)
            .WithMessage("Số tháng gia hạn phải từ 1 đến 120.");

        RuleFor(command => command)
            .Must(command => command.Months is not null || command.NewExpireDate is not null)
            .WithMessage("Chọn số tháng gia hạn hoặc nhập thẳng ngày hết hạn mới.")
            .OverridePropertyName(nameof(ExtendReaderCardsCommand.Months));
    }
}

public class ExtendReaderCardsCommandHandler : IRequestHandler<ExtendReaderCardsCommand, BulkResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public ExtendReaderCardsCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<BulkResultDto> Handle(ExtendReaderCardsCommand command, CancellationToken ct)
    {
        var today = _clock.Today;
        var readers = await ReaderSelectionResolver.ResolveAsync(_db, command.Selection, today, ct);
        var result = new BulkResultDto { Total = readers.Count };

        var validityByType = await _db.ReaderTypes
            .AsNoTracking()
            .ToDictionaryAsync(type => type.Id, type => type.CardValidMonths, ct);

        var readerIds = readers.Select(reader => reader.Id).ToList();

        var currentCards = await _db.ReaderCards
            .Where(card => readerIds.Contains(card.ReaderId) && card.IsCurrent)
            .ToListAsync(ct);

        var cardsByReader = currentCards.ToDictionary(card => card.ReaderId);

        foreach (var reader in readers)
        {
            if (reader.Status == ReaderStatus.Graduated)
            {
                result.Skipped++;
                result.Skips.Add(new BulkSkipDto(reader.Id, reader.CardNumber, reader.FullName,
                    "Bạn đọc đã ra trường — cần mở lại hồ sơ trước khi gia hạn thẻ."));
                continue;
            }

            DateOnly newExpiry;

            if (command.NewExpireDate is not null)
            {
                newExpiry = command.NewExpireDate.Value;
            }
            else
            {
                var months = command.Months
                             ?? (validityByType.TryGetValue(reader.ReaderTypeId, out var configured)
                                 ? configured
                                 : 12);

                // Thẻ đã hết hạn thì tính từ hôm nay, thẻ còn hạn thì nối tiếp hạn cũ — gia hạn sớm
                // không được làm mất phần thời gian bạn đọc chưa dùng đến.
                var basis = reader.CardExpireDate > today ? reader.CardExpireDate : today;
                newExpiry = basis.AddMonths(Math.Max(1, months));
            }

            if (newExpiry <= reader.CardExpireDate)
            {
                result.Skipped++;
                result.Skips.Add(new BulkSkipDto(reader.Id, reader.CardNumber, reader.FullName,
                    $"Hạn thẻ hiện tại ({reader.CardExpireDate:dd/MM/yyyy}) đã dài hơn hạn mới."));
                continue;
            }

            reader.CardExpireDate = newExpiry;

            // Thẻ hết hạn tự chuyển về Hoạt động khi được gia hạn; thẻ đang bị khóa thì giữ nguyên
            // trạng thái khóa vì lý do khóa không liên quan gì tới hạn thẻ.
            if (reader.Status == ReaderStatus.Expired)
            {
                reader.Status = ReaderStatus.Active;
                reader.StatusReason = null;
            }

            if (cardsByReader.TryGetValue(reader.Id, out var card))
            {
                card.ExpireDate = newExpiry;
            }

            if (!string.IsNullOrWhiteSpace(command.Note))
            {
                reader.Note = $"{reader.Note}\n{today:dd/MM/yyyy} gia hạn thẻ: {command.Note.Trim()}".Trim();
            }

            result.Succeeded++;
        }

        await _db.SaveChangesAsync(ct);
        return result;
    }
}

/// <summary>Tạm khóa hoặc mở khóa thẻ, kèm lý do bắt buộc (VI.1).</summary>
public class SetReaderLockCommand : IRequest<BulkResultDto>
{
    public ReaderSelectionDto Selection { get; set; } = new();
    public bool Locked { get; set; }
    public string? Reason { get; set; }
    /// <summary>Khóa có thời hạn; bỏ trống là khóa cho tới khi cán bộ mở lại.</summary>
    public DateOnly? LockedUntil { get; set; }
}

public class SetReaderLockCommandValidator : AbstractValidator<SetReaderLockCommand>
{
    public SetReaderLockCommandValidator()
    {
        RuleFor(command => command.Reason)
            .NotEmpty().When(command => command.Locked)
            .WithMessage("Phải ghi lý do tạm khóa thẻ.")
            .MaximumLength(500);
    }
}

public class SetReaderLockCommandHandler : IRequestHandler<SetReaderLockCommand, BulkResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public SetReaderLockCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<BulkResultDto> Handle(SetReaderLockCommand command, CancellationToken ct)
    {
        var today = _clock.Today;
        var readers = await ReaderSelectionResolver.ResolveAsync(_db, command.Selection, today, ct);
        var result = new BulkResultDto { Total = readers.Count };

        foreach (var reader in readers)
        {
            if (command.Locked)
            {
                reader.Status = ReaderStatus.Suspended;
                reader.StatusReason = command.Reason?.Trim();
                reader.LockedUntil = command.LockedUntil?.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Local);
            }
            else
            {
                if (reader.Status == ReaderStatus.Graduated)
                {
                    result.Skipped++;
                    result.Skips.Add(new BulkSkipDto(reader.Id, reader.CardNumber, reader.FullName,
                        "Bạn đọc đã ra trường, không thuộc diện mở khóa thẻ."));
                    continue;
                }

                reader.Status = reader.CardExpireDate < today ? ReaderStatus.Expired : ReaderStatus.Active;
                reader.StatusReason = string.IsNullOrWhiteSpace(command.Reason) ? null : command.Reason.Trim();
                reader.LockedUntil = null;
                reader.FailedLoginCount = 0;
            }

            result.Succeeded++;
        }

        await _db.SaveChangesAsync(ct);
        return result;
    }
}

/// <summary>
/// Cấp lại thẻ khi bạn đọc làm mất hoặc thẻ hỏng (VI.1).
///
/// Thẻ cũ không bị xóa mà chỉ thôi là thẻ hiện hành: sổ mượn trả cũ ghi theo số thẻ cũ, mất dòng đó
/// là mất luôn đường truy vết.
/// </summary>
public class ReissueReaderCardCommand : IRequest<ReaderCardDto>
{
    public Guid ReaderId { get; set; }
    public string? Reason { get; set; }
    /// <summary>Giữ nguyên số thẻ cũ (thẻ hỏng) thay vì cấp số mới (thẻ mất).</summary>
    public bool KeepCardNumber { get; set; }
    /// <summary>Số thẻ mới do cán bộ tự đặt; bỏ trống thì hệ thống sinh.</summary>
    public string? NewCardNumber { get; set; }
    public int? ValidMonths { get; set; }
}

public class ReissueReaderCardCommandValidator : AbstractValidator<ReissueReaderCardCommand>
{
    public ReissueReaderCardCommandValidator()
    {
        RuleFor(command => command.ReaderId).NotEmpty();
        RuleFor(command => command.Reason)
            .NotEmpty().WithMessage("Phải ghi lý do cấp lại thẻ.")
            .MaximumLength(500);
        RuleFor(command => command.NewCardNumber).MaximumLength(50);
        RuleFor(command => command.ValidMonths)
            .InclusiveBetween(1, 120).When(command => command.ValidMonths is not null)
            .WithMessage("Hạn thẻ phải từ 1 đến 120 tháng.");
    }
}

public class ReissueReaderCardCommandHandler : IRequestHandler<ReissueReaderCardCommand, ReaderCardDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICodeGenerator _codes;
    private readonly IDateTimeProvider _clock;

    public ReissueReaderCardCommandHandler(
        IApplicationDbContext db, ICodeGenerator codes, IDateTimeProvider clock)
    {
        _db = db;
        _codes = codes;
        _clock = clock;
    }

    public async Task<ReaderCardDto> Handle(ReissueReaderCardCommand command, CancellationToken ct)
    {
        var reader = await _db.Readers
            .Include(entity => entity.ReaderType)
            .FirstOrDefaultAsync(entity => entity.Id == command.ReaderId, ct)
            ?? throw new NotFoundException("bạn đọc", command.ReaderId);

        var today = _clock.Today;

        var cardNumber = command.KeepCardNumber
            ? reader.CardNumber
            : string.IsNullOrWhiteSpace(command.NewCardNumber)
                ? await _codes.NextAsync("CARD", ct)
                : command.NewCardNumber.Trim();

        if (!command.KeepCardNumber
            && await _db.Readers.AnyAsync(other => other.Id != reader.Id && other.CardNumber == cardNumber, ct))
        {
            throw new Common.Exceptions.ValidationException(
                nameof(command.NewCardNumber), $"Số thẻ {cardNumber} đã được cấp cho bạn đọc khác.");
        }

        var months = command.ValidMonths ?? reader.ReaderType?.CardValidMonths ?? 12;

        // Cấp lại thì thẻ mới chạy hạn từ hôm nay, trừ khi hạn cũ còn dài hơn — bạn đọc đánh mất thẻ
        // không vì thế mà mất phần thời gian đã đóng phí.
        var expire = reader.CardExpireDate > today.AddMonths(months)
            ? reader.CardExpireDate
            : today.AddMonths(Math.Max(1, months));

        foreach (var previous in await _db.ReaderCards
                     .Where(card => card.ReaderId == reader.Id && card.IsCurrent)
                     .ToListAsync(ct))
        {
            previous.IsCurrent = false;
        }

        var newCard = new ReaderCard
        {
            ReaderId = reader.Id,
            CardNumber = cardNumber,
            IssueDate = today,
            ExpireDate = expire,
            IsCurrent = true,
            ReissueReason = command.Reason?.Trim()
        };

        _db.ReaderCards.Add(newCard);

        reader.CardNumber = cardNumber;
        reader.CardIssueDate = today;
        reader.CardExpireDate = expire;

        if (reader.Status == ReaderStatus.Expired)
        {
            reader.Status = ReaderStatus.Active;
            reader.StatusReason = null;
        }

        await _db.SaveChangesAsync(ct);

        return new ReaderCardDto
        {
            Id = newCard.Id,
            CardNumber = newCard.CardNumber,
            IssueDate = newCard.IssueDate,
            ExpireDate = newCard.ExpireDate,
            PrintCount = newCard.PrintCount,
            IsCurrent = true,
            ReissueReason = newCard.ReissueReason
        };
    }
}

/// <summary>
/// Chuyển trạng thái ra trường hàng loạt (VI.1).
///
/// Kiểm tra công nợ trước khi chuyển: bạn đọc còn sách hoặc còn nợ phí thì bị giữ lại kèm lý do, để
/// phòng đào tạo còn căn cứ mà giữ bằng tốt nghiệp.
/// </summary>
public class GraduateReadersCommand : IRequest<BulkResultDto>
{
    public ReaderSelectionDto Selection { get; set; } = new();
    public string? Note { get; set; }
}

public class GraduateReadersCommandHandler : IRequestHandler<GraduateReadersCommand, BulkResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GraduateReadersCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<BulkResultDto> Handle(GraduateReadersCommand command, CancellationToken ct)
    {
        var today = _clock.Today;
        var readers = await ReaderSelectionResolver.ResolveAsync(_db, command.Selection, today, ct);
        var result = new BulkResultDto { Total = readers.Count };

        var readerIds = readers.Select(reader => reader.Id).ToList();

        var loanCounts = await _db.Loans
            .Where(loan => readerIds.Contains(loan.ReaderId)
                           && (loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue))
            .GroupBy(loan => loan.ReaderId)
            .Select(group => new { ReaderId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.ReaderId, row => row.Count, ct);

        var debts = await _db.Fines
            .Where(fine => readerIds.Contains(fine.ReaderId) && !fine.Waived && fine.Amount > fine.PaidAmount)
            .GroupBy(fine => fine.ReaderId)
            .Select(group => new { ReaderId = group.Key, Amount = group.Sum(fine => fine.Amount - fine.PaidAmount) })
            .ToDictionaryAsync(row => row.ReaderId, row => row.Amount, ct);

        foreach (var reader in readers)
        {
            var reasons = new List<string>();

            if (loanCounts.TryGetValue(reader.Id, out var loans) && loans > 0)
            {
                reasons.Add($"còn giữ {loans} tài liệu chưa trả");
            }

            if (debts.TryGetValue(reader.Id, out var debt) && debt > 0)
            {
                reasons.Add($"còn nợ {debt:#,##0} đ tiền phạt");
            }

            if (reasons.Count > 0)
            {
                result.Skipped++;
                result.Skips.Add(new BulkSkipDto(reader.Id, reader.CardNumber, reader.FullName,
                    $"Bạn đọc {string.Join(" và ", reasons)}."));
                continue;
            }

            reader.Status = ReaderStatus.Graduated;
            reader.StatusReason = string.IsNullOrWhiteSpace(command.Note)
                ? $"Chuyển trạng thái ra trường ngày {today:dd/MM/yyyy}."
                : command.Note.Trim();
            reader.LockedUntil = null;

            result.Succeeded++;
        }

        await _db.SaveChangesAsync(ct);
        return result;
    }
}

/// <summary>
/// Xác nhận bạn đọc đã thanh toán hết công nợ — giấy xác nhận trả sách cho sinh viên ra trường.
/// </summary>
public record GetReaderClearanceQuery(Guid ReaderId) : IRequest<ReaderClearanceDto>;

public class ReaderClearanceDto
{
    public Guid ReaderId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? StudentCode { get; set; }
    public string? ClassName { get; set; }
    public string? FacultyName { get; set; }
    public int OutstandingLoans { get; set; }
    public decimal OutstandingFines { get; set; }
    public bool Cleared { get; set; }
    public List<string> Blockers { get; set; } = new();
}

public class GetReaderClearanceQueryHandler : IRequestHandler<GetReaderClearanceQuery, ReaderClearanceDto>
{
    private readonly IApplicationDbContext _db;

    public GetReaderClearanceQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<ReaderClearanceDto> Handle(GetReaderClearanceQuery query, CancellationToken ct)
    {
        var reader = await _db.Readers
            .AsNoTracking()
            .Include(entity => entity.Faculty)
            .FirstOrDefaultAsync(entity => entity.Id == query.ReaderId, ct)
            ?? throw new NotFoundException("bạn đọc", query.ReaderId);

        var loans = await _db.Loans.CountAsync(loan =>
            loan.ReaderId == reader.Id
            && (loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue), ct);

        var fines = await _db.Fines
            .Where(fine => fine.ReaderId == reader.Id && !fine.Waived)
            .SumAsync(fine => (decimal?)(fine.Amount - fine.PaidAmount), ct) ?? 0;

        var result = new ReaderClearanceDto
        {
            ReaderId = reader.Id,
            CardNumber = reader.CardNumber,
            FullName = reader.FullName,
            StudentCode = reader.StudentCode,
            ClassName = reader.ClassName,
            FacultyName = reader.Faculty?.Name,
            OutstandingLoans = loans,
            OutstandingFines = fines
        };

        if (loans > 0)
        {
            result.Blockers.Add($"Còn giữ {loans} tài liệu chưa trả.");
        }

        if (fines > 0)
        {
            result.Blockers.Add($"Còn nợ {fines:#,##0} đ tiền phạt.");
        }

        result.Cleared = result.Blockers.Count == 0;
        return result;
    }
}

/// <summary>Đặt lại mật khẩu tra cứu của bạn đọc (VI.1).</summary>
public record ResetReaderPasswordCommand(Guid Id, string? NewPassword) : IRequest<string>;

public class ResetReaderPasswordCommandHandler : IRequestHandler<ResetReaderPasswordCommand, string>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IPasswordPolicyProvider _policyProvider;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public ResetReaderPasswordCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher hasher,
        IPasswordPolicyProvider policyProvider,
        IDateTimeProvider clock,
        IAuditService audit)
    {
        _db = db;
        _hasher = hasher;
        _policyProvider = policyProvider;
        _clock = clock;
        _audit = audit;
    }

    public async Task<string> Handle(ResetReaderPasswordCommand command, CancellationToken ct)
    {
        var reader = await _db.Readers.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
            ?? throw new NotFoundException("bạn đọc", command.Id);

        var policy = await _policyProvider.GetAsync(ct);
        var password = command.NewPassword;

        if (string.IsNullOrWhiteSpace(password))
        {
            password = TemporaryPasswordGenerator.Generate(policy);
        }
        else
        {
            var errors = policy.Validate(password, "newPassword");

            if (errors.Count > 0)
            {
                throw new Common.Exceptions.ValidationException(errors);
            }
        }

        reader.PasswordHash = _hasher.Hash(password);
        reader.MustChangePassword = true;
        reader.FailedLoginCount = 0;
        reader.LockedUntil = null;

        // Phiên đăng nhập cũ trên điện thoại phải mất hiệu lực ngay: đổi mật khẩu mà token cũ vẫn
        // dùng được thì việc đặt lại mật khẩu chẳng bảo vệ được gì.
        foreach (var token in await _db.RefreshTokens
                     .Where(entity => entity.ReaderId == reader.Id && entity.RevokedAt == null)
                     .ToListAsync(ct))
        {
            token.RevokedAt = _clock.Now;
            token.RevokedReason = "Cán bộ thư viện đặt lại mật khẩu bạn đọc";
        }

        await _db.SaveChangesAsync(ct);

        // Chỉ ghi nhật ký việc đặt lại, không bao giờ ghi mật khẩu.
        await _audit.LogAsync(AuditAction.Update, nameof(Reader), reader.Id.ToString(), reader.CardNumber,
            message: "Đặt lại mật khẩu bạn đọc", ct: ct);

        return password;
    }
}
