using System.Text.RegularExpressions;
using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Entities.Cat;
using LibraryConnect.Domain.Entities.Rdr;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Readers;

// ---------------------------------------------------------------------------------------------
// VI.1 — Quản lý hồ sơ bạn đọc: danh sách, thêm, sửa, xóa.
// ---------------------------------------------------------------------------------------------

/// <summary>Tra cứu bạn đọc theo số thẻ, mã sinh viên, họ tên, CCCD, email, điện thoại (VI.1).</summary>
public record SearchReadersQuery(ReaderListRequest Request) : IRequest<PagedResult<ReaderDto>>;

public class SearchReadersQueryHandler : IRequestHandler<SearchReadersQuery, PagedResult<ReaderDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ISystemParameterService _parameters;

    public SearchReadersQueryHandler(
        IApplicationDbContext db, IDateTimeProvider clock, ISystemParameterService parameters)
    {
        _db = db;
        _clock = clock;
        _parameters = parameters;
    }

    public async Task<PagedResult<ReaderDto>> Handle(SearchReadersQuery query, CancellationToken ct)
    {
        var today = _clock.Today;

        // Khoảng cảnh báo thẻ sắp hết hạn là tham số của từng thư viện, không viết cứng trong code.
        var expiringDays = await _parameters.GetAsync(
            ReaderQuery.ExpiringDaysParameter, ReaderQuery.DefaultExpiringDays, ct);

        return await ReaderQuery.Apply(_db, query.Request, today)
            .ApplySort(query.Request, ReaderQuery.Sorts, reader => reader.FullName)
            .Select(ReaderQuery.Projection(_db, today, expiringDays))
            .ToPagedResultAsync(query.Request, ct);
    }
}

/// <summary>Hồ sơ chi tiết kèm lịch sử cấp thẻ.</summary>
public record GetReaderQuery(Guid Id) : IRequest<ReaderDetailDto>;

public class GetReaderQueryHandler : IRequestHandler<GetReaderQuery, ReaderDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ISystemParameterService _parameters;

    public GetReaderQueryHandler(
        IApplicationDbContext db, IDateTimeProvider clock, ISystemParameterService parameters)
    {
        _db = db;
        _clock = clock;
        _parameters = parameters;
    }

    public async Task<ReaderDetailDto> Handle(GetReaderQuery query, CancellationToken ct)
    {
        var today = _clock.Today;

        var expiringDays = await _parameters.GetAsync(
            ReaderQuery.ExpiringDaysParameter, ReaderQuery.DefaultExpiringDays, ct);

        var reader = await _db.Readers
            .AsNoTracking()
            .Include(entity => entity.ReaderType)
            .Include(entity => entity.Faculty)
            .Include(entity => entity.Major)
            .FirstOrDefaultAsync(entity => entity.Id == query.Id, ct)
            ?? throw new NotFoundException("bạn đọc", query.Id);

        var outstanding = await _db.Fines
            .Where(fine => fine.ReaderId == reader.Id && !fine.Waived)
            .SumAsync(fine => (decimal?)(fine.Amount - fine.PaidAmount), ct) ?? 0;

        var currentLoans = await _db.Loans.CountAsync(loan =>
            loan.ReaderId == reader.Id
            && (loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue), ct);

        var totalLoans = await _db.Loans.CountAsync(loan => loan.ReaderId == reader.Id, ct);

        var cards = await _db.ReaderCards
            .AsNoTracking()
            .Where(card => card.ReaderId == reader.Id)
            .OrderByDescending(card => card.IsCurrent)
            .ThenByDescending(card => card.IssueDate)
            .Select(card => new ReaderCardDto
            {
                Id = card.Id,
                CardNumber = card.CardNumber,
                IssueDate = card.IssueDate,
                ExpireDate = card.ExpireDate,
                PrintCount = card.PrintCount,
                IsCurrent = card.IsCurrent,
                ReissueReason = card.ReissueReason
            })
            .ToListAsync(ct);

        return new ReaderDetailDto
        {
            Id = reader.Id,
            CardNumber = reader.CardNumber,
            StudentCode = reader.StudentCode,
            FullName = reader.FullName,
            Gender = reader.Gender,
            DateOfBirth = reader.DateOfBirth,
            IdCardNumber = reader.IdCardNumber,
            Email = reader.Email,
            Phone = reader.Phone,
            Address = reader.Address,
            PhotoUrl = reader.PhotoUrl,
            ReaderTypeId = reader.ReaderTypeId,
            ReaderTypeName = reader.ReaderType?.Name,
            FacultyId = reader.FacultyId,
            FacultyName = reader.Faculty?.Name,
            MajorId = reader.MajorId,
            MajorName = reader.Major?.Name,
            ClassName = reader.ClassName,
            CourseYear = reader.CourseYear,
            CardIssueDate = reader.CardIssueDate,
            CardExpireDate = reader.CardExpireDate,
            Status = reader.Status,
            StatusReason = reader.StatusReason,
            DepositAmount = reader.DepositAmount,
            DebtAmount = outstanding,
            CurrentLoanCount = currentLoans,
            TotalLoanCount = totalLoans,
            IsExpired = reader.CardExpireDate < today,
            IsExpiringSoon = reader.CardExpireDate >= today
                             && reader.CardExpireDate <= today.AddDays(expiringDays),
            CanBorrow = reader.CanBorrow(today),
            Note = reader.Note,
            HasPassword = !string.IsNullOrEmpty(reader.PasswordHash),
            MustChangePassword = reader.MustChangePassword,
            LastLoginAt = reader.LastLoginAt,
            LockedUntil = reader.LockedUntil,
            CreatedAt = reader.CreatedAt,
            Cards = cards
        };
    }
}

/// <summary>Thêm mới hoặc sửa hồ sơ bạn đọc (VI.1).</summary>
public class SaveReaderCommand : IRequest<Guid>
{
    public Guid? Id { get; set; }
    /// <summary>Bỏ trống khi thêm mới thì hệ thống sinh theo quy tắc khai trong Tham số hệ thống.</summary>
    public string? CardNumber { get; set; }
    public string? StudentCode { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? IdCardNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public Guid ReaderTypeId { get; set; }
    public Guid? FacultyId { get; set; }
    public Guid? MajorId { get; set; }
    public string? ClassName { get; set; }
    public string? CourseYear { get; set; }
    public DateOnly? CardIssueDate { get; set; }
    /// <summary>Bỏ trống thì tính từ hạn thẻ của loại bạn đọc.</summary>
    public DateOnly? CardExpireDate { get; set; }
    public ReaderStatus? Status { get; set; }
    public decimal DepositAmount { get; set; }
    public string? Note { get; set; }
}

public class SaveReaderCommandValidator : AbstractValidator<SaveReaderCommand>
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"^[0-9+().\s-]{6,20}$", RegexOptions.Compiled);

    public SaveReaderCommandValidator()
    {
        RuleFor(command => command.FullName)
            .NotEmpty().WithMessage("Chưa nhập họ và tên bạn đọc.")
            .MaximumLength(300);

        RuleFor(command => command.CardNumber).MaximumLength(50);
        RuleFor(command => command.StudentCode).MaximumLength(50);
        RuleFor(command => command.IdCardNumber).MaximumLength(50);
        RuleFor(command => command.ClassName).MaximumLength(100);
        RuleFor(command => command.CourseYear).MaximumLength(50);

        RuleFor(command => command.ReaderTypeId)
            .NotEmpty().WithMessage("Chưa chọn loại bạn đọc.");

        RuleFor(command => command.Email)
            .Must(email => string.IsNullOrWhiteSpace(email) || EmailPattern.IsMatch(email))
            .WithMessage("Địa chỉ email không hợp lệ.")
            .MaximumLength(200);

        RuleFor(command => command.Phone)
            .Must(phone => string.IsNullOrWhiteSpace(phone) || PhonePattern.IsMatch(phone))
            .WithMessage("Số điện thoại chỉ gồm chữ số và các ký tự + ( ) . -")
            .MaximumLength(50);

        RuleFor(command => command.DepositAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Tiền đặt cọc không được âm.");

        RuleFor(command => command)
            .Must(command => command.CardExpireDate is null
                             || command.CardIssueDate is null
                             || command.CardExpireDate >= command.CardIssueDate)
            .WithMessage("Ngày hết hạn thẻ phải sau ngày cấp thẻ.")
            .OverridePropertyName(nameof(SaveReaderCommand.CardExpireDate));

        // Ngày sinh gõ nhầm là chuyện thường: 2099 thay vì 1999, 2005 thay vì 1905. Không chặn thì
        // hồ sơ sai nằm im trong kho, và mọi báo cáo theo độ tuổi đều lệch theo.
        RuleFor(command => command.DateOfBirth)
            .Must(date => date is null || date.Value < DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Ngày sinh phải là một ngày đã qua.")
            .Must(date => date is null || date.Value.Year >= 1900)
            .WithMessage("Ngày sinh trước năm 1900 nhiều khả năng là gõ nhầm.");
    }
}

public class SaveReaderCommandHandler : IRequestHandler<SaveReaderCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICodeGenerator _codes;
    private readonly IDateTimeProvider _clock;

    public SaveReaderCommandHandler(IApplicationDbContext db, ICodeGenerator codes, IDateTimeProvider clock)
    {
        _db = db;
        _codes = codes;
        _clock = clock;
    }

    public async Task<Guid> Handle(SaveReaderCommand command, CancellationToken ct)
    {
        var readerType = await _db.ReaderTypes
            .FirstOrDefaultAsync(type => type.Id == command.ReaderTypeId, ct)
            ?? throw new Common.Exceptions.ValidationException(
                nameof(command.ReaderTypeId), "Loại bạn đọc không tồn tại.");

        await EnsureRelatedExistAsync(command, ct);

        var reader = command.Id is null
            ? await CreateAsync(command, readerType, ct)
            : await LoadForUpdateAsync(command.Id.Value, ct);

        await EnsureUniqueAsync(command, reader.Id, ct);

        reader.FullName = command.FullName.Trim();
        reader.StudentCode = Normalize(command.StudentCode);
        reader.Gender = Normalize(command.Gender);
        reader.DateOfBirth = command.DateOfBirth;
        reader.IdCardNumber = Normalize(command.IdCardNumber);
        reader.Email = Normalize(command.Email)?.ToLowerInvariant();
        reader.Phone = Normalize(command.Phone);
        reader.Address = Normalize(command.Address);
        reader.ReaderTypeId = command.ReaderTypeId;
        reader.FacultyId = command.FacultyId;
        reader.MajorId = command.MajorId;
        reader.ClassName = Normalize(command.ClassName);
        reader.CourseYear = Normalize(command.CourseYear);
        reader.DepositAmount = command.DepositAmount;
        reader.Note = Normalize(command.Note);

        if (command.CardIssueDate is not null)
        {
            reader.CardIssueDate = command.CardIssueDate.Value;
        }

        if (command.CardExpireDate is not null)
        {
            reader.CardExpireDate = command.CardExpireDate.Value;
        }

        if (command.Status is not null && command.Id is not null)
        {
            reader.Status = command.Status.Value;
        }

        // Thẻ hiện hành đi theo hồ sơ: sửa hạn thẻ trên hồ sơ mà sổ cấp thẻ vẫn giữ hạn cũ thì đến
        // lúc in ra sẽ là hai con số khác nhau trên cùng một tấm thẻ.
        if (command.Id is not null)
        {
            var current = await _db.ReaderCards
                .FirstOrDefaultAsync(card => card.ReaderId == reader.Id && card.IsCurrent, ct);

            if (current is not null)
            {
                current.CardNumber = reader.CardNumber;
                current.IssueDate = reader.CardIssueDate;
                current.ExpireDate = reader.CardExpireDate;
            }
        }

        await _db.SaveChangesAsync(ct);
        return reader.Id;
    }

    private async Task<Reader> CreateAsync(SaveReaderCommand command, ReaderType readerType, CancellationToken ct)
    {
        var today = _clock.Today;

        var cardNumber = string.IsNullOrWhiteSpace(command.CardNumber)
            ? await _codes.NextAsync("CARD", ct)
            : command.CardNumber.Trim();

        var issueDate = command.CardIssueDate ?? today;

        var reader = new Reader
        {
            CardNumber = cardNumber,
            CardIssueDate = issueDate,
            CardExpireDate = command.CardExpireDate
                             ?? issueDate.AddMonths(Math.Max(1, readerType.CardValidMonths)),
            Status = command.Status ?? ReaderStatus.Active,
            DepositAmount = command.DepositAmount > 0 ? command.DepositAmount : readerType.DepositAmount
        };

        _db.Readers.Add(reader);

        _db.ReaderCards.Add(new ReaderCard
        {
            ReaderId = reader.Id,
            CardNumber = reader.CardNumber,
            IssueDate = reader.CardIssueDate,
            ExpireDate = reader.CardExpireDate,
            IsCurrent = true
        });

        return reader;
    }

    private async Task<Reader> LoadForUpdateAsync(Guid id, CancellationToken ct) =>
        await _db.Readers.FirstOrDefaultAsync(entity => entity.Id == id, ct)
        ?? throw new NotFoundException("bạn đọc", id);

    private async Task EnsureRelatedExistAsync(SaveReaderCommand command, CancellationToken ct)
    {
        if (command.FacultyId is not null
            && !await _db.Faculties.AnyAsync(faculty => faculty.Id == command.FacultyId, ct))
        {
            throw new Common.Exceptions.ValidationException(nameof(command.FacultyId), "Khoa không tồn tại.");
        }

        if (command.MajorId is not null
            && !await _db.Majors.AnyAsync(major => major.Id == command.MajorId, ct))
        {
            throw new Common.Exceptions.ValidationException(
                nameof(command.MajorId), "Ngành đào tạo không tồn tại.");
        }
    }

    private async Task EnsureUniqueAsync(SaveReaderCommand command, Guid readerId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(command.CardNumber))
        {
            var cardNumber = command.CardNumber.Trim();

            if (await _db.Readers.AnyAsync(other => other.Id != readerId && other.CardNumber == cardNumber, ct))
            {
                throw new Common.Exceptions.ValidationException(
                    nameof(command.CardNumber), $"Số thẻ {cardNumber} đã được cấp cho bạn đọc khác.");
            }
        }

        if (!string.IsNullOrWhiteSpace(command.StudentCode))
        {
            var studentCode = command.StudentCode.Trim();

            if (await _db.Readers.AnyAsync(other => other.Id != readerId && other.StudentCode == studentCode, ct))
            {
                throw new Common.Exceptions.ValidationException(
                    nameof(command.StudentCode), $"Mã sinh viên {studentCode} đã có trong hệ thống.");
            }
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>
/// Xóa hồ sơ bạn đọc — xóa mềm, và chỉ khi bạn đọc không còn ràng buộc gì với thư viện.
/// </summary>
public record DeleteReaderCommand(Guid Id, string? Reason = null) : IRequest;

public class DeleteReaderCommandHandler : IRequestHandler<DeleteReaderCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteReaderCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteReaderCommand command, CancellationToken ct)
    {
        var reader = await _db.Readers.FirstOrDefaultAsync(entity => entity.Id == command.Id, ct)
            ?? throw new NotFoundException("bạn đọc", command.Id);

        var activeLoans = await _db.Loans.CountAsync(loan =>
            loan.ReaderId == reader.Id
            && (loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue), ct);

        if (activeLoans > 0)
        {
            throw new ConflictException(
                $"Bạn đọc {reader.FullName} còn giữ {activeLoans} tài liệu chưa trả nên chưa xóa được hồ sơ.");
        }

        var debt = await _db.Fines
            .Where(fine => fine.ReaderId == reader.Id && !fine.Waived)
            .SumAsync(fine => (decimal?)(fine.Amount - fine.PaidAmount), ct) ?? 0;

        if (debt > 0)
        {
            throw new ConflictException(
                $"Bạn đọc {reader.FullName} còn nợ {debt:#,##0} đ tiền phạt nên chưa xóa được hồ sơ.");
        }

        reader.Note = string.IsNullOrWhiteSpace(command.Reason)
            ? reader.Note
            : $"{reader.Note}\nLý do xóa hồ sơ: {command.Reason.Trim()}".Trim();

        _db.Readers.Remove(reader);
        await _db.SaveChangesAsync(ct);
    }
}
