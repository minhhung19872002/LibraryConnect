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
// VII.2 — Màn hình ghi mượn / ghi trả.
// ---------------------------------------------------------------------------------------------

/// <summary>Quét thẻ ở quầy: nhận số thẻ hoặc mã sinh viên, trả về toàn bộ thứ cần biết.</summary>
public record GetDeskReaderQuery(string CardNumber) : IRequest<DeskReaderDto>;

public class GetDeskReaderQueryHandler : IRequestHandler<GetDeskReaderQuery, DeskReaderDto>
{
    private readonly ICirculationDeskService _desk;

    public GetDeskReaderQueryHandler(ICirculationDeskService desk) => _desk = desk;

    public async Task<DeskReaderDto> Handle(GetDeskReaderQuery query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.CardNumber))
        {
            throw new Common.Exceptions.ValidationException("cardNumber", "Chưa quét thẻ bạn đọc.");
        }

        var reader = await _desk.FindReaderByCardAsync(query.CardNumber, ct);
        return await _desk.GetReaderAsync(reader.Id, ct);
    }
}

public record GetDeskReaderByIdQuery(Guid ReaderId) : IRequest<DeskReaderDto>;

public class GetDeskReaderByIdQueryHandler : IRequestHandler<GetDeskReaderByIdQuery, DeskReaderDto>
{
    private readonly ICirculationDeskService _desk;

    public GetDeskReaderByIdQueryHandler(ICirculationDeskService desk) => _desk = desk;

    public Task<DeskReaderDto> Handle(GetDeskReaderByIdQuery query, CancellationToken ct) =>
        _desk.GetReaderAsync(query.ReaderId, ct);
}

/// <summary>
/// Quét một mã vạch ở màn hình ghi mượn.
///
/// Máy chủ quyết định cho mượn hay không và tính luôn hạn trả; màn hình chỉ hiển thị và phát âm
/// thanh. Nhờ vậy quét bằng máy quét ở quầy, quét bằng điện thoại hay gọi bằng Postman đều ra cùng
/// một kết quả.
/// </summary>
public record ScanForLoanQuery(Guid ReaderId, string Barcode, List<string>? Pending)
    : IRequest<ScanForLoanDto>;

public class ScanForLoanQueryHandler : IRequestHandler<ScanForLoanQuery, ScanForLoanDto>
{
    private readonly ICirculationDeskService _desk;

    public ScanForLoanQueryHandler(ICirculationDeskService desk) => _desk = desk;

    public Task<ScanForLoanDto> Handle(ScanForLoanQuery query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Barcode))
        {
            throw new Common.Exceptions.ValidationException("barcode", "Chưa quét mã vạch ấn phẩm.");
        }

        return _desk.ScanForLoanAsync(query.ReaderId, query.Barcode, query.Pending, ct);
    }
}

public class CheckoutCommand : IRequest<CheckoutResultDto>
{
    public Guid ReaderId { get; set; }
    public List<string> Barcodes { get; set; } = new();
    public LoanType LoanType { get; set; } = LoanType.TakeHome;
    public string? Note { get; set; }
    /// <summary>Bỏ qua các cảnh báo chặn được phép bỏ qua, kèm ghi chú lý do.</summary>
    public bool Force { get; set; }
}

public class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutCommandValidator()
    {
        RuleFor(command => command.ReaderId).NotEmpty().WithMessage("Chưa chọn bạn đọc.");
        RuleFor(command => command.Barcodes)
            .NotEmpty().WithMessage("Chưa quét mã vạch nào.");
        RuleFor(command => command.Note).MaximumLength(1000);
    }
}

public class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, CheckoutResultDto>
{
    private readonly ICirculationDeskService _desk;

    public CheckoutCommandHandler(ICirculationDeskService desk) => _desk = desk;

    public Task<CheckoutResultDto> Handle(CheckoutCommand command, CancellationToken ct) =>
        _desk.CheckoutAsync(new CheckoutRequest
        {
            ReaderId = command.ReaderId,
            Barcodes = command.Barcodes,
            LoanType = command.LoanType,
            Channel = LoanChannel.Desk,
            Note = command.Note,
            Force = command.Force
        }, ct);
}

public class ReturnCommand : IRequest<ReturnResultDto>
{
    public List<string> Barcodes { get; set; } = new();
    public string? Note { get; set; }
}

public class ReturnCommandValidator : AbstractValidator<ReturnCommand>
{
    public ReturnCommandValidator()
    {
        RuleFor(command => command.Barcodes).NotEmpty().WithMessage("Chưa quét mã vạch nào.");
        RuleFor(command => command.Note).MaximumLength(1000);
    }
}

public class ReturnCommandHandler : IRequestHandler<ReturnCommand, ReturnResultDto>
{
    private readonly ICirculationDeskService _desk;

    public ReturnCommandHandler(ICirculationDeskService desk) => _desk = desk;

    public Task<ReturnResultDto> Handle(ReturnCommand command, CancellationToken ct) =>
        _desk.ReturnAsync(command.Barcodes, command.Note, ct);
}

/// <summary>Gia hạn tại quầy, theo mã lượt mượn.</summary>
public record RenewLoanCommand(Guid LoanId) : IRequest<LoanRowDto>;

public class RenewLoanCommandHandler : IRequestHandler<RenewLoanCommand, LoanRowDto>
{
    private readonly ICirculationDeskService _desk;

    public RenewLoanCommandHandler(ICirculationDeskService desk) => _desk = desk;

    public Task<LoanRowDto> Handle(RenewLoanCommand command, CancellationToken ct) =>
        _desk.RenewAsync(command.LoanId, LoanChannel.Desk, null, ct);
}

/// <summary>Gia hạn bằng mã vạch ấn phẩm — cán bộ chỉ cần quét, không phải tra mã lượt mượn.</summary>
public record RenewByBarcodeCommand(string Barcode) : IRequest<LoanRowDto>;

public class RenewByBarcodeCommandHandler : IRequestHandler<RenewByBarcodeCommand, LoanRowDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICirculationDeskService _desk;

    public RenewByBarcodeCommandHandler(IApplicationDbContext db, ICirculationDeskService desk)
    {
        _db = db;
        _desk = desk;
    }

    public async Task<LoanRowDto> Handle(RenewByBarcodeCommand command, CancellationToken ct)
    {
        var code = command.Barcode.Trim();

        var loanId = await _db.Loans
            .AsNoTracking()
            .Where(loan => (loan.Barcode == code || loan.Item!.RegisterNumber == code)
                           && (loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue))
            .OrderByDescending(loan => loan.LoanDate)
            .Select(loan => (Guid?)loan.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Không có lượt mượn nào đang mở cho mã vạch '{code}'.");

        return await _desk.RenewAsync(loanId, LoanChannel.Desk, null, ct);
    }
}

/// <summary>
/// Ghi nhận tài liệu mất hoặc hỏng (trạng thái MẤT / HỎNG của mục 4.8).
///
/// Tiền bồi thường mặc định lấy theo giá ghi trên ĐKCB nhân hệ số khai trong tham số, vì đó là căn
/// cứ duy nhất có sẵn; cán bộ sửa lại được cho từng trường hợp.
/// </summary>
public class CloseLoanAsLostCommand : IRequest<FineRowDto>
{
    public Guid LoanId { get; set; }
    public bool Damaged { get; set; }
    public decimal? Amount { get; set; }
    public string? Note { get; set; }
}

public class CloseLoanAsLostCommandHandler : IRequestHandler<CloseLoanAsLostCommand, FineRowDto>
{
    public const string CompensationFactorParameter = "CIRCULATION.LOST_COMPENSATION_FACTOR";

    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameters;
    private readonly ICodeGenerator _codes;
    private readonly IDateTimeProvider _clock;

    public CloseLoanAsLostCommandHandler(
        IApplicationDbContext db,
        ISystemParameterService parameters,
        ICodeGenerator codes,
        IDateTimeProvider clock)
    {
        _db = db;
        _parameters = parameters;
        _codes = codes;
        _clock = clock;
    }

    public async Task<FineRowDto> Handle(CloseLoanAsLostCommand command, CancellationToken ct)
    {
        var loan = await _db.Loans
            .Include(entity => entity.Item)
            .Include(entity => entity.Reader)
            .FirstOrDefaultAsync(entity => entity.Id == command.LoanId, ct)
            ?? throw new NotFoundException("lượt mượn", command.LoanId);

        if (loan.Status is LoanStatus.Returned)
        {
            throw new ConflictException("Lượt mượn đã ghi trả nên không ghi nhận mất hay hỏng được.");
        }

        var factor = await _parameters.GetAsync(CompensationFactorParameter, 2m, ct);
        var price = loan.Item?.Price ?? 0;

        var amount = command.Amount
                     ?? Math.Round(price * (command.Damaged ? factor / 2 : factor), 0,
                         MidpointRounding.AwayFromZero);

        loan.Status = command.Damaged ? LoanStatus.Damaged : LoanStatus.Lost;
        loan.ReturnDate = _clock.Now;
        loan.Note = string.IsNullOrWhiteSpace(command.Note)
            ? loan.Note
            : $"{loan.Note}\n{command.Note}".Trim();

        if (loan.Item is not null)
        {
            loan.Item.Status = command.Damaged ? ItemStatus.Damaged : ItemStatus.Lost;
            loan.Item.IsLocked = true;
            loan.Item.LockReason = command.Damaged
                ? "Bạn đọc làm hỏng, chờ xử lý"
                : "Bạn đọc làm mất, chờ xử lý";
        }

        if (loan.Reader is not null && loan.Reader.CurrentLoanCount > 0)
        {
            loan.Reader.CurrentLoanCount--;
        }

        var fine = new Fine
        {
            Code = await _codes.NextAsync("FINE", ct),
            ReaderId = loan.ReaderId,
            LoanId = loan.Id,
            Type = command.Damaged ? FineType.Damaged : FineType.Lost,
            Amount = amount,
            Note = command.Note
                   ?? (command.Damaged
                       ? $"Bồi thường tài liệu hỏng {loan.Barcode}."
                       : $"Bồi thường tài liệu mất {loan.Barcode}.")
        };

        _db.Fines.Add(fine);
        loan.FineAmount += amount;

        await _db.SaveChangesAsync(ct);

        return await FineQuery.Base(_db)
            .Where(entity => entity.Id == fine.Id)
            .Select(FineQuery.Projection)
            .FirstAsync(ct);
    }
}

// ---------------------------------------------------------------------------------------------
// Danh sách mượn trả
// ---------------------------------------------------------------------------------------------

public record SearchLoansQuery(LoanListRequest Request) : IRequest<PagedResult<LoanRowDto>>;

public class SearchLoansQueryHandler : IRequestHandler<SearchLoansQuery, PagedResult<LoanRowDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICirculationPolicyResolver _policies;
    private readonly ICirculationCalendarProvider _calendars;
    private readonly IDateTimeProvider _clock;

    public SearchLoansQueryHandler(
        IApplicationDbContext db,
        ICirculationPolicyResolver policies,
        ICirculationCalendarProvider calendars,
        IDateTimeProvider clock)
    {
        _db = db;
        _policies = policies;
        _calendars = calendars;
        _clock = clock;
    }

    public async Task<PagedResult<LoanRowDto>> Handle(SearchLoansQuery query, CancellationToken ct)
    {
        var today = _clock.Today;
        var request = query.Request;

        var loans = LoanQuery.Base(_db)
            .WhereIf(request.ReaderId is not null, loan => loan.ReaderId == request.ReaderId)
            .WhereIf(request.ItemId is not null, loan => loan.ItemId == request.ItemId)
            .WhereIf(request.WarehouseId is not null, loan => loan.Item!.WarehouseId == request.WarehouseId)
            .WhereIf(request.ReaderTypeId is not null,
                loan => loan.Reader!.ReaderTypeId == request.ReaderTypeId)
            .WhereIf(request.FacultyId is not null, loan => loan.Reader!.FacultyId == request.FacultyId)
            .WhereIf(request.Status is not null, loan => loan.Status == request.Status)
            .WhereIf(request.LoanType is not null, loan => loan.LoanType == request.LoanType)
            .WhereIf(request.Channel is not null, loan => loan.Channel == request.Channel)
            .WhereIf(request.ActiveOnly == true,
                loan => loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue)
            .WhereIf(request.OverdueOnly == true,
                loan => (loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue)
                        && loan.DueDate < today)
            .WhereIf(request.FromDate is not null,
                loan => loan.LoanDate >= request.FromDate!.Value.ToDateTime(TimeOnly.MinValue))
            .WhereIf(request.ToDate is not null,
                loan => loan.LoanDate < request.ToDate!.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));

        if (request.HasKeyword())
        {
            var keyword = request.Keyword!.Trim().ToLowerInvariant();
            var unaccented = Common.Text.VietnameseText.RemoveDiacritics(keyword);

            loans = loans.Where(loan =>
                loan.Code.ToLower().Contains(keyword)
                || (loan.Barcode != null && loan.Barcode.ToLower().Contains(keyword))
                || loan.Reader!.CardNumber.ToLower().Contains(keyword)
                || (loan.Reader!.StudentCode != null && loan.Reader!.StudentCode.ToLower().Contains(keyword))
                || DatabaseFunctions.Unaccent(loan.Reader!.FullName).Contains(unaccented)
                || (loan.BibTitle != null && DatabaseFunctions.Unaccent(loan.BibTitle).Contains(unaccented)));
        }

        var page = await loans
            .ApplySort(request, Sorts, loan => loan.LoanDate, defaultDescending: true)
            .Select(LoanQuery.Projection)
            .ToPagedResultAsync(request, ct);

        var calendar = await _calendars.GetAsync(null, ct);
        var cache = new Dictionary<Guid, EffectivePolicy>();

        foreach (var row in page.Items)
        {
            // Chính sách tra theo loại bạn đọc là đủ cho phần hiển thị; tra đúng từng ô ma trận cho
            // mỗi dòng sẽ thành hàng trăm truy vấn cho một trang danh sách.
            var policy = await ResolveCachedAsync(cache, row.ReaderId, ct);
            CirculationDeskService.Enrich(row, policy, calendar, today);
        }

        return page;
    }

    private async Task<EffectivePolicy> ResolveCachedAsync(
        Dictionary<Guid, EffectivePolicy> cache, Guid readerId, CancellationToken ct)
    {
        if (cache.TryGetValue(readerId, out var cached))
        {
            return cached;
        }

        var readerTypeId = await _db.Readers
            .AsNoTracking()
            .Where(reader => reader.Id == readerId)
            .Select(reader => (Guid?)reader.ReaderTypeId)
            .FirstOrDefaultAsync(ct);

        var policy = await _policies.ResolveAsync(readerTypeId, null, null, ct);
        cache[readerId] = policy;

        return policy;
    }

    private static readonly IReadOnlyDictionary<string, System.Linq.Expressions.Expression<Func<Loan, object?>>> Sorts =
        new Dictionary<string, System.Linq.Expressions.Expression<Func<Loan, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["code"] = loan => loan.Code,
            ["loanDate"] = loan => loan.LoanDate,
            ["dueDate"] = loan => loan.DueDate,
            ["returnDate"] = loan => loan.ReturnDate,
            ["readerName"] = loan => loan.Reader!.FullName,
            ["title"] = loan => loan.BibTitle,
            ["status"] = loan => loan.Status
        };
}

/// <summary>Chi tiết một lượt mượn kèm lịch sử gia hạn.</summary>
public record GetLoanQuery(Guid Id) : IRequest<LoanDetailDto>;

public class LoanDetailDto : LoanRowDto
{
    public List<LoanRenewalDto> Renewals { get; set; } = new();
    public List<FineRowDto> Fines { get; set; } = new();
}

public record LoanRenewalDto(
    Guid Id,
    DateTimeOffset RenewalDate,
    DateOnly OldDueDate,
    DateOnly NewDueDate,
    LoanChannel Channel,
    AccessRequestStatus Status,
    string? RejectReason);

public class GetLoanQueryHandler : IRequestHandler<GetLoanQuery, LoanDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICirculationPolicyResolver _policies;
    private readonly ICirculationCalendarProvider _calendars;
    private readonly IDateTimeProvider _clock;

    public GetLoanQueryHandler(
        IApplicationDbContext db,
        ICirculationPolicyResolver policies,
        ICirculationCalendarProvider calendars,
        IDateTimeProvider clock)
    {
        _db = db;
        _policies = policies;
        _calendars = calendars;
        _clock = clock;
    }

    public async Task<LoanDetailDto> Handle(GetLoanQuery query, CancellationToken ct)
    {
        var row = await LoanQuery.Base(_db)
            .Where(loan => loan.Id == query.Id)
            .Select(LoanQuery.Projection)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("lượt mượn", query.Id);

        var detail = new LoanDetailDto
        {
            Id = row.Id,
            Code = row.Code,
            ReaderId = row.ReaderId,
            ReaderCardNumber = row.ReaderCardNumber,
            ReaderName = row.ReaderName,
            ReaderTypeName = row.ReaderTypeName,
            FacultyName = row.FacultyName,
            ClassName = row.ClassName,
            ItemId = row.ItemId,
            Barcode = row.Barcode,
            Title = row.Title,
            CallNumber = row.CallNumber,
            WarehouseName = row.WarehouseName,
            LoanDate = row.LoanDate,
            DueDate = row.DueDate,
            ReturnDate = row.ReturnDate,
            RenewedCount = row.RenewedCount,
            Status = row.Status,
            LoanType = row.LoanType,
            Channel = row.Channel,
            LoanByName = row.LoanByName,
            ReturnByName = row.ReturnByName,
            FineAmount = row.FineAmount,
            Note = row.Note
        };

        var readerTypeId = await _db.Readers
            .AsNoTracking()
            .Where(reader => reader.Id == row.ReaderId)
            .Select(reader => (Guid?)reader.ReaderTypeId)
            .FirstOrDefaultAsync(ct);

        var policy = await _policies.ResolveAsync(readerTypeId, null, null, ct);
        var calendar = await _calendars.GetAsync(null, ct);

        CirculationDeskService.Enrich(detail, policy, calendar, _clock.Today);

        detail.Renewals = await _db.LoanRenewals
            .AsNoTracking()
            .Where(renewal => renewal.LoanId == query.Id)
            .OrderByDescending(renewal => renewal.RenewalDate)
            .Select(renewal => new LoanRenewalDto(
                renewal.Id,
                renewal.RenewalDate,
                renewal.OldDueDate,
                renewal.NewDueDate,
                renewal.Channel,
                renewal.Status,
                renewal.RejectReason))
            .ToListAsync(ct);

        detail.Fines = await FineQuery.Base(_db)
            .Where(fine => fine.LoanId == query.Id)
            .Select(FineQuery.Projection)
            .ToListAsync(ct);

        return detail;
    }
}

/// <summary>Duyệt hoặc từ chối một yêu cầu gia hạn gửi từ trang tra cứu hoặc ứng dụng.</summary>
public class ProcessRenewalRequestCommand : IRequest<LoanRowDto>
{
    public Guid RenewalId { get; set; }
    public bool Approve { get; set; }
    public string? RejectReason { get; set; }
}

public class ProcessRenewalRequestCommandHandler
    : IRequestHandler<ProcessRenewalRequestCommand, LoanRowDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICirculationDeskService _desk;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationSender _notifications;

    public ProcessRenewalRequestCommandHandler(
        IApplicationDbContext db,
        ICirculationDeskService desk,
        ICurrentUser currentUser,
        INotificationSender notifications)
    {
        _db = db;
        _desk = desk;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<LoanRowDto> Handle(ProcessRenewalRequestCommand command, CancellationToken ct)
    {
        var renewal = await _db.LoanRenewals
            .Include(entity => entity.Loan)
            .FirstOrDefaultAsync(entity => entity.Id == command.RenewalId, ct)
            ?? throw new NotFoundException("yêu cầu gia hạn", command.RenewalId);

        if (renewal.Status != AccessRequestStatus.Pending)
        {
            throw new ConflictException("Yêu cầu gia hạn này đã được xử lý.");
        }

        var loanId = renewal.LoanId;

        if (!command.Approve)
        {
            renewal.Status = AccessRequestStatus.Rejected;
            renewal.RejectReason = command.RejectReason;
            renewal.ApprovedBy = _currentUser.UserId;

            await _db.SaveChangesAsync(ct);

            await _notifications.SendAsync(renewal.Loan!.ReaderId,
                "Yêu cầu gia hạn bị từ chối",
                $"Yêu cầu gia hạn tài liệu \"{renewal.Loan!.BibTitle}\" không được duyệt. " +
                $"Lý do: {command.RejectReason ?? "không ghi"}.", ct: ct);

            return await LoanQuery.Base(_db)
                .Where(loan => loan.Id == loanId)
                .Select(LoanQuery.Projection)
                .FirstAsync(ct);
        }

        // Duyệt xong thì chạy lại đúng đường gia hạn ở quầy để mọi luật vẫn được kiểm tra lần nữa.
        renewal.Status = AccessRequestStatus.Approved;
        renewal.ApprovedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(ct);

        var result = await _desk.RenewAsync(loanId, LoanChannel.Desk, null, ct);

        await _notifications.SendAsync(result.ReaderId,
            "Yêu cầu gia hạn đã được duyệt",
            $"Tài liệu \"{result.Title}\" được gia hạn tới ngày {result.DueDate:dd/MM/yyyy}.", ct: ct);

        return result;
    }
}

/// <summary>Danh sách yêu cầu gia hạn đang chờ duyệt.</summary>
public record GetPendingRenewalsQuery : IRequest<IReadOnlyList<PendingRenewalDto>>;

public record PendingRenewalDto(
    Guid Id,
    Guid LoanId,
    string LoanCode,
    string ReaderName,
    string ReaderCardNumber,
    string? Title,
    string? Barcode,
    DateOnly OldDueDate,
    DateOnly NewDueDate,
    DateTimeOffset RequestedAt,
    LoanChannel Channel);

public class GetPendingRenewalsQueryHandler
    : IRequestHandler<GetPendingRenewalsQuery, IReadOnlyList<PendingRenewalDto>>
{
    private readonly IApplicationDbContext _db;

    public GetPendingRenewalsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<PendingRenewalDto>> Handle(
        GetPendingRenewalsQuery query, CancellationToken ct) =>
        await _db.LoanRenewals
            .AsNoTracking()
            .Where(renewal => renewal.Status == AccessRequestStatus.Pending)
            .OrderBy(renewal => renewal.RenewalDate)
            .Select(renewal => new PendingRenewalDto(
                renewal.Id,
                renewal.LoanId,
                renewal.Loan!.Code,
                renewal.Loan!.Reader!.FullName,
                renewal.Loan!.Reader!.CardNumber,
                renewal.Loan!.BibTitle,
                renewal.Loan!.Barcode,
                renewal.OldDueDate,
                renewal.NewDueDate,
                renewal.RenewalDate,
                renewal.Channel))
            .ToListAsync(ct);
}
