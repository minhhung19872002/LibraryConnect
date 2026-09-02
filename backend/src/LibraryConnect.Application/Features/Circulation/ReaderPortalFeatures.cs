using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Circulation;

// ---------------------------------------------------------------------------------------------
// Nhóm /api/reader/* phần mượn trả (mục XI.4).
//
// Cùng một nghiệp vụ với quầy, chỉ khác ở chỗ danh tính lấy từ mã thông báo của bạn đọc chứ không
// phải do cán bộ chọn. Trang tra cứu và ứng dụng di động đợt sau dùng chung đúng nhóm này.
// ---------------------------------------------------------------------------------------------

/// <summary>Xác định bạn đọc đang đăng nhập; chặn sớm khi tài khoản không phải bạn đọc.</summary>
internal static class ReaderIdentity
{
    public static Guid Require(ICurrentUser currentUser) =>
        currentUser.ReaderId
        ?? throw new ForbiddenException("Chức năng này dành cho tài khoản bạn đọc.");
}

/// <summary>Sách bạn đọc đang mượn (/api/reader/loans/current).</summary>
public record GetMyLoansQuery(bool CurrentOnly, PagedRequestDefault Request)
    : IRequest<PagedResult<LoanRowDto>>;

public class GetMyLoansQueryHandler : IRequestHandler<GetMyLoansQuery, PagedResult<LoanRowDto>>
{
    private readonly ISender _mediator;
    private readonly ICurrentUser _currentUser;

    public GetMyLoansQueryHandler(ISender mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public Task<PagedResult<LoanRowDto>> Handle(GetMyLoansQuery query, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        return _mediator.Send(new SearchLoansQuery(new LoanListRequest
        {
            ReaderId = readerId,
            UpdatedSince = query.Request.UpdatedSince,
            ActiveOnly = query.CurrentOnly ? true : null,
            Page = query.Request.Page,
            PageSize = query.Request.PageSize,
            SortBy = query.CurrentOnly ? "dueDate" : "loanDate",
            SortDescending = !query.CurrentOnly
        }), ct);
    }
}

/// <summary>Bạn đọc tự gửi yêu cầu gia hạn (/api/reader/loans/{id}/renew).</summary>
public record RenewMyLoanCommand(Guid LoanId) : IRequest<LoanRowDto>;

public class RenewMyLoanCommandHandler : IRequestHandler<RenewMyLoanCommand, LoanRowDto>
{
    private readonly ICirculationDeskService _desk;
    private readonly ICurrentUser _currentUser;

    public RenewMyLoanCommandHandler(ICirculationDeskService desk, ICurrentUser currentUser)
    {
        _desk = desk;
        _currentUser = currentUser;
    }

    public Task<LoanRowDto> Handle(RenewMyLoanCommand command, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);
        return _desk.RenewAsync(command.LoanId, LoanChannel.Opac, readerId, ct);
    }
}

/// <summary>
/// Mượn tự phục vụ (/api/reader/loans/self-checkout).
///
/// Bạn đọc tự quét mã vạch tài liệu bằng điện thoại. Để không thành ra "mượn sách từ nhà", hệ thống
/// đòi thêm mã điểm quét — mã QR dán tại kho — và đối chiếu với danh sách khai trong tham số. Thư
/// viện chưa dán mã thì để trống tham số, khi đó tính năng vẫn chạy mà không kiểm tra vị trí.
/// </summary>
public class SelfCheckoutCommand : IRequest<CheckoutResultDto>
{
    public List<string> Barcodes { get; set; } = new();
    /// <summary>Nội dung mã QR dán tại kho (cách cũ: danh sách mã tĩnh trong tham số).</summary>
    public string? LocationToken { get; set; }

    /// <summary>Phiếu xác thực vị trí do <c>POST /api/reader/loans/self-checkout/verify</c> cấp.</summary>
    public string? VerificationToken { get; set; }
}

public class SelfCheckoutCommandValidator : AbstractValidator<SelfCheckoutCommand>
{
    public SelfCheckoutCommandValidator()
    {
        RuleFor(command => command.Barcodes)
            .NotEmpty().WithMessage("Chưa quét mã vạch tài liệu nào.");
    }
}

public class SelfCheckoutCommandHandler : IRequestHandler<SelfCheckoutCommand, CheckoutResultDto>
{
    public const string LocationTokensParameter = "CIRCULATION.SELF_CHECKOUT_TOKENS";
    public const string EnabledParameter = "CIRCULATION.SELF_CHECKOUT_ENABLED";

    private readonly ICirculationDeskService _desk;
    private readonly ISystemParameterService _parameters;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public SelfCheckoutCommandHandler(
        ICirculationDeskService desk,
        ISystemParameterService parameters,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _desk = desk;
        _parameters = parameters;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<CheckoutResultDto> Handle(SelfCheckoutCommand command, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        if (!await _parameters.GetAsync(EnabledParameter, false, ct))
        {
            throw new ConflictException(
                "Thư viện chưa mở chức năng mượn tự phục vụ.", SelfCheckoutErrorCodes.Disabled);
        }

        // Phase 15: chế độ xác thực vị trí (NONE / WIFI_SSID / QR_STATION) do máy chủ quyết; ứng dụng
        // nộp phiếu đã được cấp ở bước xác thực. Chế độ NONE thì rơi xuống cách cũ bên dưới.
        var place = await SelfCheckoutVerification.RequireAsync(
            _parameters, command.VerificationToken, readerId, _clock.Now, ct);

        var tokens = await _parameters.GetAsync(LocationTokensParameter, string.Empty, ct);

        if (!string.IsNullOrWhiteSpace(tokens))
        {
            var allowed = tokens
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(command.LocationToken)
                || !allowed.Contains(command.LocationToken.Trim()))
            {
                throw new ConflictException(
                    "Chưa quét mã điểm mượn tại kho, hoặc mã không đúng. " +
                    "Vui lòng quét mã QR dán tại kho rồi thử lại.");
            }
        }

        return await _desk.CheckoutAsync(new CheckoutRequest
        {
            ReaderId = readerId,
            Barcodes = command.Barcodes,
            LoanType = LoanType.SelfCheckout,
            Channel = LoanChannel.Mobile,
            Note = place is null ? "Mượn tự phục vụ" : $"Mượn tự phục vụ · xác thực tại {place}"
        }, ct);
    }
}

/// <summary>Đặt giữ của bạn đọc (/api/reader/holds).</summary>
public record GetMyHoldsQuery(PagedRequestDefault Request) : IRequest<PagedResult<HoldRowDto>>;

public class GetMyHoldsQueryHandler : IRequestHandler<GetMyHoldsQuery, PagedResult<HoldRowDto>>
{
    private readonly ISender _mediator;
    private readonly ICurrentUser _currentUser;

    public GetMyHoldsQueryHandler(ISender mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public Task<PagedResult<HoldRowDto>> Handle(GetMyHoldsQuery query, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        return _mediator.Send(new SearchHoldsQuery(new HoldListRequest
        {
            ReaderId = readerId,
            Page = query.Request.Page,
            PageSize = query.Request.PageSize
        }), ct);
    }
}

public class PlaceMyHoldCommand : IRequest<HoldRowDto>
{
    public Guid BibId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? PickupWarehouseId { get; set; }
}

public class PlaceMyHoldCommandHandler : IRequestHandler<PlaceMyHoldCommand, HoldRowDto>
{
    private readonly ISender _mediator;
    private readonly ICurrentUser _currentUser;

    public PlaceMyHoldCommandHandler(ISender mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public Task<HoldRowDto> Handle(PlaceMyHoldCommand command, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        return _mediator.Send(new PlaceHoldCommand
        {
            ReaderId = readerId,
            BibId = command.BibId,
            ItemId = command.ItemId,
            PickupWarehouseId = command.PickupWarehouseId,
            Channel = LoanChannel.Opac
        }, ct);
    }
}

public record CancelMyHoldCommand(Guid Id) : IRequest;

public class CancelMyHoldCommandHandler : IRequestHandler<CancelMyHoldCommand>
{
    private readonly ISender _mediator;
    private readonly ICurrentUser _currentUser;

    public CancelMyHoldCommandHandler(ISender mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public Task Handle(CancelMyHoldCommand command, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        return _mediator.Send(
            new CancelHoldCommand(command.Id, "Bạn đọc tự hủy", readerId), ct);
    }
}

/// <summary>Tiền phạt của bạn đọc (/api/reader/fines).</summary>
public record GetMyFinesQuery(PagedRequestDefault Request) : IRequest<ReaderFineSummaryDto>;

public class GetMyFinesQueryHandler : IRequestHandler<GetMyFinesQuery, ReaderFineSummaryDto>
{
    private readonly ISender _mediator;
    private readonly ICurrentUser _currentUser;

    public GetMyFinesQueryHandler(ISender mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public Task<ReaderFineSummaryDto> Handle(GetMyFinesQuery query, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);
        return _mediator.Send(new GetReaderFineSummaryQuery(readerId), ct);
    }
}

/// <summary>Thẻ thư viện điện tử (/api/reader/card).</summary>
public record GetMyCardQuery : IRequest<ReaderCardInfoDto>;

public class ReaderCardInfoDto
{
    public Guid ReaderId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? StudentCode { get; set; }
    public string? ReaderTypeName { get; set; }
    public string? FacultyName { get; set; }
    public string? ClassName { get; set; }
    public DateOnly CardIssueDate { get; set; }
    public DateOnly CardExpireDate { get; set; }
    public ReaderStatus Status { get; set; }
    public bool CanBorrow { get; set; }
    /// <summary>Chuỗi mã hóa vào mã vạch và mã QR của thẻ điện tử — chính là số thẻ.</summary>
    public string BarcodeValue { get; set; } = string.Empty;
    public int CurrentLoanCount { get; set; }
    public decimal OutstandingFines { get; set; }
    public List<CirculationWarningDto> Warnings { get; set; } = new();
}

public class GetMyCardQueryHandler : IRequestHandler<GetMyCardQuery, ReaderCardInfoDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICirculationDeskService _desk;
    private readonly ICurrentUser _currentUser;

    public GetMyCardQueryHandler(
        IApplicationDbContext db, ICirculationDeskService desk, ICurrentUser currentUser)
    {
        _db = db;
        _desk = desk;
        _currentUser = currentUser;
    }

    public async Task<ReaderCardInfoDto> Handle(GetMyCardQuery query, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);
        var desk = await _desk.GetReaderAsync(readerId, ct);

        var issue = await _db.Readers
            .AsNoTracking()
            .Where(reader => reader.Id == readerId)
            .Select(reader => reader.CardIssueDate)
            .FirstAsync(ct);

        return new ReaderCardInfoDto
        {
            ReaderId = desk.Id,
            CardNumber = desk.CardNumber,
            FullName = desk.FullName,
            StudentCode = desk.StudentCode,
            ReaderTypeName = desk.ReaderTypeName,
            FacultyName = desk.FacultyName,
            ClassName = desk.ClassName,
            CardIssueDate = issue,
            CardExpireDate = desk.CardExpireDate,
            Status = desk.Status,
            CanBorrow = desk.CanBorrow,
            BarcodeValue = desk.CardNumber,
            CurrentLoanCount = desk.CurrentLoanCount,
            OutstandingFines = desk.OutstandingFines,
            Warnings = desk.Warnings
        };
    }
}
