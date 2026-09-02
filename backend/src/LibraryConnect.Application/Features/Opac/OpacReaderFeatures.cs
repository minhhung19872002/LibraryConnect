using System.Text;
using System.Text.Json;
using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Circulation;
using LibraryConnect.Domain.Entities.Rdr;
using LibraryConnect.Domain.Entities.Sys;
using LibraryConnect.Domain.Entities.Web;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Opac;

// ---------------------------------------------------------------------------------------------
// IX.3 — Trang cá nhân của bạn đọc trên OPAC, và phần còn lại của nhóm /api/reader/* ở mục XI.4:
// hồ sơ, yêu cầu gia hạn thẻ, thông báo, thiết bị nhận thông báo đẩy, yêu thích, tìm kiếm đã lưu,
// nhận xét và gửi danh sách tài liệu qua email.
// ---------------------------------------------------------------------------------------------

public record GetMyProfileQuery : IRequest<ReaderProfileDto>;

public record ReaderProfileDto(
    Guid Id,
    string CardNumber,
    string? StudentCode,
    string FullName,
    string? Gender,
    DateOnly? DateOfBirth,
    string? Email,
    string? Phone,
    string? Address,
    string? PhotoUrl,
    string ReaderTypeName,
    string? FacultyName,
    string? MajorName,
    string? ClassName,
    string? CourseYear,
    DateOnly CardIssueDate,
    DateOnly CardExpireDate,
    string StatusLabel,
    bool MustChangePassword,
    int CurrentLoanCount,
    decimal DebtAmount);

public class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, ReaderProfileDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyProfileQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ReaderProfileDto> Handle(GetMyProfileQuery query, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        var reader = await _db.Readers.AsNoTracking()
                         .Include(entity => entity.ReaderType)
                         .Include(entity => entity.Faculty)
                         .Include(entity => entity.Major)
                         .FirstOrDefaultAsync(entity => entity.Id == readerId, ct)
                     ?? throw new NotFoundException("bạn đọc", readerId);

        return new ReaderProfileDto(
            reader.Id,
            reader.CardNumber,
            reader.StudentCode,
            reader.FullName,
            reader.Gender,
            reader.DateOfBirth,
            reader.Email,
            reader.Phone,
            reader.Address,
            reader.PhotoUrl,
            reader.ReaderType?.Name ?? string.Empty,
            reader.Faculty?.Name,
            reader.Major?.Name,
            reader.ClassName,
            reader.CourseYear,
            reader.CardIssueDate,
            reader.CardExpireDate,
            ReaderStatusLabels.Describe(reader.Status),
            reader.MustChangePassword,
            reader.CurrentLoanCount,
            reader.DebtAmount);
    }
}

/// <summary>
/// Bạn đọc tự cập nhật thông tin liên hệ.
///
/// Chỉ ba trường liên hệ. Họ tên, mã sinh viên, khoa, loại bạn đọc là dữ liệu do nhà trường quản
/// lý — cho tự sửa thì hồ sơ thư viện lệch khỏi hồ sơ đào tạo ngay hôm sau.
/// </summary>
public class UpdateMyProfileCommand : IRequest
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(command => command.Email)
            .EmailAddress().When(command => !string.IsNullOrWhiteSpace(command.Email))
            .WithMessage("Địa chỉ email không hợp lệ.")
            .MaximumLength(200).WithMessage("Email tối đa 200 ký tự.");

        RuleFor(command => command.Phone)
            .MaximumLength(50).WithMessage("Số điện thoại tối đa 50 ký tự.");

        RuleFor(command => command.Address)
            .MaximumLength(500).WithMessage("Địa chỉ tối đa 500 ký tự.");
    }
}

public class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UpdateMyProfileCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateMyProfileCommand command, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        var reader = await _db.Readers.FirstOrDefaultAsync(entity => entity.Id == readerId, ct)
                     ?? throw new NotFoundException("bạn đọc", readerId);

        reader.Email = string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim();
        reader.Phone = string.IsNullOrWhiteSpace(command.Phone) ? null : command.Phone.Trim();
        reader.Address = string.IsNullOrWhiteSpace(command.Address) ? null : command.Address.Trim();

        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Gửi yêu cầu gia hạn thẻ thư viện (XI.4).</summary>
public record RequestCardRenewalCommand(string? Reason) : IRequest<Guid>;

public class RequestCardRenewalCommandHandler : IRequestHandler<RequestCardRenewalCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public RequestCardRenewalCommandHandler(
        IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Guid> Handle(RequestCardRenewalCommand command, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        var pending = await _db.CardRenewalRequests
            .AnyAsync(request => request.ReaderId == readerId
                                 && request.Status == AccessRequestStatus.Pending, ct);

        if (pending)
        {
            throw new ConflictException(
                "Bạn đã có một yêu cầu gia hạn thẻ đang chờ xử lý.");
        }

        var request = new CardRenewalRequest
        {
            ReaderId = readerId,
            RequestDate = _clock.Now,
            Reason = command.Reason?.Trim(),
            Status = AccessRequestStatus.Pending
        };

        _db.CardRenewalRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        return request.Id;
    }
}

/// <summary>Trạng thái các yêu cầu gia hạn thẻ bạn đọc đã gửi.</summary>
public record GetMyCardRenewalsQuery : IRequest<IReadOnlyList<CardRenewalRowDto>>;

public record CardRenewalRowDto(
    Guid Id,
    DateTimeOffset RequestDate,
    string? Reason,
    string StatusLabel,
    DateTimeOffset? ProcessedAt,
    DateOnly? NewExpireDate,
    string? RejectReason);

public class GetMyCardRenewalsQueryHandler
    : IRequestHandler<GetMyCardRenewalsQuery, IReadOnlyList<CardRenewalRowDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyCardRenewalsQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CardRenewalRowDto>> Handle(
        GetMyCardRenewalsQuery query, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        var rows = await _db.CardRenewalRequests.AsNoTracking()
            .Where(request => request.ReaderId == readerId)
            .OrderByDescending(request => request.RequestDate)
            .ToListAsync(ct);

        return rows
            .Select(request => new CardRenewalRowDto(
                request.Id,
                request.RequestDate,
                request.Reason,
                AccessRequestLabels.Describe(request.Status),
                request.ProcessedAt,
                request.NewExpireDate,
                request.RejectReason))
            .ToList();
    }
}

/// <summary>Thông báo gửi tới bạn đọc (XI.4).</summary>
public record GetMyNotificationsQuery(PagedRequestDefault Request, bool UnreadOnly = false)
    : IRequest<PagedResult<ReaderNotificationDto>>;

public record ReaderNotificationDto(
    Guid Id,
    string Type,
    string Title,
    string? Body,
    string? Link,
    bool IsRead,
    DateTimeOffset CreatedAt);

public class GetMyNotificationsQueryHandler
    : IRequestHandler<GetMyNotificationsQuery, PagedResult<ReaderNotificationDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyNotificationsQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<ReaderNotificationDto>> Handle(
        GetMyNotificationsQuery query, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        return await _db.Notifications.AsNoTracking()
            .Where(notification => notification.ReaderId == readerId)
            .WhereIf(query.UnreadOnly, notification => !notification.IsRead)
            .WhereIf(query.Request.UpdatedSince is not null, notification => notification.CreatedAt >= query.Request.UpdatedSince)
            .OrderByDescending(notification => notification.CreatedAt)
            .Select(notification => new ReaderNotificationDto(
                notification.Id,
                notification.Type,
                notification.Title,
                notification.Body,
                notification.Link,
                notification.IsRead,
                notification.CreatedAt))
            .ToPagedResultAsync(query.Request, ct);
    }
}

/// <summary>Đánh dấu một thông báo đã đọc, hoặc đánh dấu tất cả.</summary>
public record MarkNotificationReadCommand(Guid? Id) : IRequest;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public MarkNotificationReadCommandHandler(
        IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task Handle(MarkNotificationReadCommand command, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        // Điều kiện chủ sở hữu nằm ngay trong câu lệnh: gửi mã thông báo của người khác lên thì
        // không có dòng nào khớp, chứ không phải đọc lên rồi mới kiểm.
        var notifications = _db.Notifications
            .Where(notification => notification.ReaderId == readerId && !notification.IsRead);

        if (command.Id is not null)
        {
            notifications = notifications.Where(notification => notification.Id == command.Id);
        }

        var now = _clock.Now;

        foreach (var notification in await notifications.ToListAsync(ct))
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Đăng ký thiết bị nhận thông báo đẩy.
///
/// Đợt web chưa gửi thông báo đẩy, nhưng endpoint và bảng lưu đã sẵn sàng theo mục 0.2 để ứng dụng
/// di động đợt sau cắm vào mà không phải đổi lược đồ dữ liệu. Cùng một chuỗi thiết bị gửi lại thì
/// cập nhật chứ không thêm dòng mới — người dùng cài lại ứng dụng là gửi lại chuỗi cũ.
/// </summary>
public record RegisterDeviceTokenCommand(
    string Token, string Platform, string? DeviceName, string? AppVersion) : IRequest;

public class RegisterDeviceTokenCommandValidator : AbstractValidator<RegisterDeviceTokenCommand>
{
    private static readonly string[] Platforms = { "android", "ios", "web" };

    public RegisterDeviceTokenCommandValidator()
    {
        RuleFor(command => command.Token)
            .NotEmpty().WithMessage("Chưa có chuỗi thiết bị.")
            .MaximumLength(500).WithMessage("Chuỗi thiết bị tối đa 500 ký tự.");

        RuleFor(command => command.Platform)
            .Must(platform => Platforms.Contains((platform ?? string.Empty).ToLowerInvariant()))
            .WithMessage("Nền tảng phải là android, ios hoặc web.");
    }
}

public class RegisterDeviceTokenCommandHandler : IRequestHandler<RegisterDeviceTokenCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public RegisterDeviceTokenCommandHandler(
        IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task Handle(RegisterDeviceTokenCommand command, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);
        var token = command.Token.Trim();

        var device = await _db.DeviceTokens.FirstOrDefaultAsync(entity => entity.Token == token, ct);

        if (device is null)
        {
            device = new DeviceToken { Token = token };
            _db.DeviceTokens.Add(device);
        }

        device.ReaderId = readerId;
        device.UserId = null;
        device.Platform = command.Platform.ToLowerInvariant();
        device.DeviceName = command.DeviceName?.Trim();
        device.AppVersion = command.AppVersion?.Trim();
        device.LastSeenAt = _clock.Now;
        device.IsActive = true;

        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Gỡ đăng ký thiết bị khi bạn đọc đăng xuất khỏi ứng dụng.</summary>
public record RemoveDeviceTokenCommand(string Token) : IRequest;

public class RemoveDeviceTokenCommandHandler : IRequestHandler<RemoveDeviceTokenCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public RemoveDeviceTokenCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(RemoveDeviceTokenCommand command, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);
        var token = (command.Token ?? string.Empty).Trim();

        var device = await _db.DeviceTokens
            .FirstOrDefaultAsync(entity => entity.Token == token && entity.ReaderId == readerId, ct);

        if (device is null)
        {
            return;
        }

        device.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }
}

// ---------------------------------------------------------------------------------------------
// Yêu thích và tìm kiếm đã lưu (IX.2)
// ---------------------------------------------------------------------------------------------

public record GetMyFavoritesQuery(PagedRequestDefault Request) : IRequest<PagedResult<OpacResultDto>>;

public class GetMyFavoritesQueryHandler
    : IRequestHandler<GetMyFavoritesQuery, PagedResult<OpacResultDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyFavoritesQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<OpacResultDto>> Handle(
        GetMyFavoritesQuery query, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        return await _db.OpacFavorites.AsNoTracking()
            .Where(favorite => favorite.ReaderId == readerId)
            .OrderByDescending(favorite => favorite.CreatedAt)
            .Select(favorite => favorite.Bib!)
            .Select(OpacQueryBuilder.ToResult())
            .ToPagedResultAsync(query.Request, ct);
    }
}

/// <summary>Bật / tắt đánh dấu yêu thích cho một tài liệu.</summary>
public record ToggleFavoriteCommand(Guid BibId) : IRequest<bool>;

public class ToggleFavoriteCommandHandler : IRequestHandler<ToggleFavoriteCommand, bool>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ToggleFavoriteCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(ToggleFavoriteCommand command, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        var existing = await _db.OpacFavorites
            .FirstOrDefaultAsync(
                favorite => favorite.ReaderId == readerId && favorite.BibId == command.BibId, ct);

        if (existing is not null)
        {
            _db.OpacFavorites.Remove(existing);
            await _db.SaveChangesAsync(ct);
            return false;
        }

        var exists = await _db.BibRecords.AnyAsync(
            bib => bib.Id == command.BibId && bib.Status == RecordStatus.Published, ct);

        if (!exists)
        {
            throw new NotFoundException("Không tìm thấy tài liệu.");
        }

        _db.OpacFavorites.Add(new OpacFavorite { ReaderId = readerId, BibId = command.BibId });
        await _db.SaveChangesAsync(ct);

        return true;
    }
}

public record GetMySavedSearchesQuery : IRequest<IReadOnlyList<SavedSearchDto>>;

public record SavedSearchDto(
    Guid Id, string Name, string Query, bool AlertEnabled, DateTimeOffset CreatedAt);

public class GetMySavedSearchesQueryHandler
    : IRequestHandler<GetMySavedSearchesQuery, IReadOnlyList<SavedSearchDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMySavedSearchesQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<SavedSearchDto>> Handle(
        GetMySavedSearchesQuery query, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        return await _db.OpacSavedSearches.AsNoTracking()
            .Where(search => search.ReaderId == readerId)
            .OrderByDescending(search => search.CreatedAt)
            .Select(search => new SavedSearchDto(
                search.Id, search.Name, search.Query, search.AlertEnabled, search.CreatedAt))
            .ToListAsync(ct);
    }
}

/// <summary>Lưu lại một lần tra cứu để lần sau chạy lại y nguyên.</summary>
public record SaveSearchCommand(string Name, string Query, bool AlertEnabled) : IRequest<Guid>;

public class SaveSearchCommandValidator : AbstractValidator<SaveSearchCommand>
{
    public SaveSearchCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Chưa đặt tên cho tìm kiếm.")
            .MaximumLength(200).WithMessage("Tên tối đa 200 ký tự.");

        RuleFor(command => command.Query)
            .NotEmpty().WithMessage("Chưa có nội dung tìm kiếm để lưu.")
            .Must(BeJson).WithMessage("Nội dung tìm kiếm không đúng định dạng.");
    }

    /// <summary>Nội dung lưu là chuỗi JSON để chạy lại đúng lần tra cũ; kiểm ngay khi nhận.</summary>
    private static bool BeJson(string query)
    {
        try
        {
            using var _ = JsonDocument.Parse(query);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public class SaveSearchCommandHandler : IRequestHandler<SaveSearchCommand, Guid>
{
    private const int MaxSavedSearches = 50;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public SaveSearchCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(SaveSearchCommand command, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        var count = await _db.OpacSavedSearches
            .CountAsync(search => search.ReaderId == readerId, ct);

        if (count >= MaxSavedSearches)
        {
            throw new ConflictException(
                $"Mỗi bạn đọc lưu tối đa {MaxSavedSearches} tìm kiếm. Hãy xóa bớt trước khi lưu thêm.");
        }

        var search = new OpacSavedSearch
        {
            ReaderId = readerId,
            Name = command.Name.Trim(),
            Query = command.Query,
            AlertEnabled = command.AlertEnabled
        };

        _db.OpacSavedSearches.Add(search);
        await _db.SaveChangesAsync(ct);

        return search.Id;
    }
}

public record DeleteSavedSearchCommand(Guid Id) : IRequest;

public class DeleteSavedSearchCommandHandler : IRequestHandler<DeleteSavedSearchCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DeleteSavedSearchCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteSavedSearchCommand command, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        var search = await _db.OpacSavedSearches
                         .FirstOrDefaultAsync(
                             entity => entity.Id == command.Id && entity.ReaderId == readerId, ct)
                     ?? throw new NotFoundException("tìm kiếm đã lưu", command.Id);

        _db.OpacSavedSearches.Remove(search);
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Bạn đọc gửi nhận xét về một tài liệu; nhận xét chờ cán bộ duyệt mới hiện công khai.</summary>
public record SubmitReviewCommand(Guid BibId, int Rating, string? Comment) : IRequest<Guid>;

public class SubmitReviewCommandValidator : AbstractValidator<SubmitReviewCommand>
{
    public SubmitReviewCommandValidator()
    {
        RuleFor(command => command.Rating)
            .InclusiveBetween(1, 5).WithMessage("Số sao đánh giá nằm trong khoảng 1 đến 5.");

        RuleFor(command => command.Comment)
            .MaximumLength(2000).WithMessage("Nhận xét tối đa 2000 ký tự.");
    }
}

public class SubmitReviewCommandHandler : IRequestHandler<SubmitReviewCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ISystemParameterService _parameters;

    public SubmitReviewCommandHandler(
        IApplicationDbContext db, ICurrentUser currentUser, ISystemParameterService parameters)
    {
        _db = db;
        _currentUser = currentUser;
        _parameters = parameters;
    }

    public async Task<Guid> Handle(SubmitReviewCommand command, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        if (!await _parameters.GetAsync("OPAC.ALLOW_REVIEW", false, ct))
        {
            throw new ConflictException("Thư viện đang tắt chức năng nhận xét tài liệu.");
        }

        if (!await _db.BibRecords.AnyAsync(
                bib => bib.Id == command.BibId && bib.Status == RecordStatus.Published, ct))
        {
            throw new NotFoundException("Không tìm thấy tài liệu.");
        }

        var review = await _db.OpacReviews.FirstOrDefaultAsync(
            entity => entity.BibId == command.BibId && entity.ReaderId == readerId, ct);

        if (review is null)
        {
            review = new OpacReview { BibId = command.BibId, ReaderId = readerId };
            _db.OpacReviews.Add(review);
        }

        review.Rating = command.Rating;
        review.Comment = command.Comment?.Trim();

        // Sửa lại nhận xét đã duyệt thì phải duyệt lại: nếu không, người ta gửi một câu vô hại để
        // được duyệt rồi sửa thành nội dung khác.
        review.IsApproved = false;
        review.ApprovedBy = null;

        await _db.SaveChangesAsync(ct);

        return review.Id;
    }
}

/// <summary>
/// Gửi danh sách tài liệu trong giỏ qua email (IX.2).
///
/// Chỉ gửi tới địa chỉ email đã ghi trong hồ sơ bạn đọc, không nhận địa chỉ tùy ý từ phía gọi —
/// nếu không thì trang tra cứu thành công cụ gửi thư rác mượn danh thư viện.
/// </summary>
public record EmailBibListCommand(IReadOnlyList<Guid> BibIds, string? Note) : IRequest<string>;

public class EmailBibListCommandHandler : IRequestHandler<EmailBibListCommand, string>
{
    private const int MaxItems = 100;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IEmailSender _email;
    private readonly ISystemParameterService _parameters;

    public EmailBibListCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IEmailSender email,
        ISystemParameterService parameters)
    {
        _db = db;
        _currentUser = currentUser;
        _email = email;
        _parameters = parameters;
    }

    public async Task<string> Handle(EmailBibListCommand command, CancellationToken ct)
    {
        var readerId = ReaderIdentity.Require(_currentUser);

        if (command.BibIds.Count == 0)
        {
            throw new Common.Exceptions.ValidationException("bibIds", "Giỏ tài liệu đang trống.");
        }

        if (command.BibIds.Count > MaxItems)
        {
            throw new Common.Exceptions.ValidationException(
                "bibIds", $"Mỗi lần gửi tối đa {MaxItems} tài liệu.");
        }

        var reader = await _db.Readers.AsNoTracking()
                         .FirstOrDefaultAsync(entity => entity.Id == readerId, ct)
                     ?? throw new NotFoundException("bạn đọc", readerId);

        if (string.IsNullOrWhiteSpace(reader.Email))
        {
            throw new ConflictException(
                "Hồ sơ của bạn chưa có địa chỉ email. Hãy cập nhật trong mục Thông tin cá nhân.");
        }

        var ids = command.BibIds.Distinct().ToList();

        var bibs = await OpacQueryBuilder.Published(_db.BibRecords.AsNoTracking())
            .Where(bib => ids.Contains(bib.Id))
            .OrderBy(bib => bib.Title)
            .Select(bib => new
            {
                bib.Title,
                bib.AuthorMain,
                bib.PublisherName,
                bib.PublishYear,
                bib.Isbn,
                bib.Ddc,
                bib.AvailableItemCount
            })
            .ToListAsync(ct);

        if (bibs.Count == 0)
        {
            throw new NotFoundException("Không còn tài liệu nào trong danh sách để gửi.");
        }

        var libraryName = await _parameters.GetAsync("LIBRARY.NAME", "Thư viện", ct);
        var body = new StringBuilder();

        body.Append("<p>Xin chào ").Append(System.Net.WebUtility.HtmlEncode(reader.FullName))
            .Append(",</p><p>Dưới đây là danh sách tài liệu bạn đã chọn trên trang tra cứu của ")
            .Append(System.Net.WebUtility.HtmlEncode(libraryName)).Append(".</p>");

        if (!string.IsNullOrWhiteSpace(command.Note))
        {
            body.Append("<p><i>").Append(System.Net.WebUtility.HtmlEncode(command.Note.Trim()))
                .Append("</i></p>");
        }

        body.Append("<ol>");

        foreach (var bib in bibs)
        {
            body.Append("<li><b>").Append(System.Net.WebUtility.HtmlEncode(bib.Title)).Append("</b>");

            if (!string.IsNullOrWhiteSpace(bib.AuthorMain))
            {
                body.Append(" — ").Append(System.Net.WebUtility.HtmlEncode(bib.AuthorMain));
            }

            var imprint = string.Join(", ", new[]
            {
                bib.PublisherName,
                bib.PublishYear?.ToString(),
                string.IsNullOrWhiteSpace(bib.Ddc) ? null : $"Phân loại {bib.Ddc}",
                string.IsNullOrWhiteSpace(bib.Isbn) ? null : $"ISBN {bib.Isbn}"
            }.Where(part => !string.IsNullOrWhiteSpace(part)));

            if (imprint.Length > 0)
            {
                body.Append("<br/><small>").Append(System.Net.WebUtility.HtmlEncode(imprint))
                    .Append("</small>");
            }

            body.Append("<br/><small>")
                .Append(bib.AvailableItemCount > 0
                    ? $"Còn {bib.AvailableItemCount} bản sẵn sàng"
                    : "Hiện chưa có bản rảnh")
                .Append("</small></li>");
        }

        body.Append("</ol>");

        await _email.SendAsync(
            new EmailMessage(
                new[] { reader.Email! },
                $"Danh sách tài liệu từ {libraryName}",
                body.ToString()),
            ct);

        return reader.Email!;
    }
}

/// <summary>Chữ tiếng Việt của trạng thái thẻ và trạng thái yêu cầu, dùng cho trang cá nhân.</summary>
public static class ReaderStatusLabels
{
    public static string Describe(ReaderStatus status) => status switch
    {
        ReaderStatus.Active => "Hoạt động",
        ReaderStatus.Expired => "Hết hạn",
        ReaderStatus.Suspended => "Tạm khóa",
        ReaderStatus.Locked => "Khóa",
        _ => "Đã ra trường"
    };
}

public static class AccessRequestLabels
{
    public static string Describe(AccessRequestStatus status) => status switch
    {
        AccessRequestStatus.Pending => "Chờ duyệt",
        AccessRequestStatus.Approved => "Đã duyệt",
        AccessRequestStatus.Rejected => "Từ chối",
        _ => "Hết hạn"
    };
}
