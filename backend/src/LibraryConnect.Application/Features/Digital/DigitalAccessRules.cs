using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Domain.Entities.Dig;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Digital;

/// <summary>
/// Nơi duy nhất quyết định ai được đọc, tải và in tài liệu số (mục V.1 và V.2).
///
/// Mọi endpoint đọc, xem trang, tải về đều đi qua đây; giao diện chỉ hiển thị lại kết quả chứ không
/// tự suy. Gọi thẳng API bằng công cụ khác cũng nhận đúng quyết định này.
///
/// Thứ tự xét, dừng ngay ở điều kiện đầu tiên khớp:
/// <list type="number">
/// <item>Cán bộ có quyền xem tài liệu số thì đọc được tất cả, kể cả tài liệu cấm.</item>
/// <item>Tài liệu mức CẤM thì không ai ngoài cán bộ mở được nội dung.</item>
/// <item>Tài liệu mức HẠN CHẾ đòi một yêu cầu đã duyệt còn hiệu lực.</item>
/// <item>Tài liệu mức NỘI BỘ đòi bạn đọc đã đăng nhập.</item>
/// <item>Tài liệu CÔNG KHAI thì khách vãng lai cũng đọc được.</item>
/// </list>
///
/// Khách chưa đăng nhập vẫn được xem số trang xem thử của tài liệu nội bộ và hạn chế — đó là cách
/// bạn đọc quyết định có xin quyền đọc hay không.
/// </summary>
public interface IDigitalAccessEvaluator
{
    Task<DigitalPermissionDto> EvaluateAsync(DigitalDocument document, CancellationToken ct = default);
}

public class DigitalAccessEvaluator : IDigitalAccessEvaluator
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public DigitalAccessEvaluator(IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<DigitalPermissionDto> EvaluateAsync(
        DigitalDocument document, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        var now = _clock.Now;

        // 1. Cán bộ thư viện.
        if (_currentUser.UserId is not null && _currentUser.HasPermission(PermissionCodes.DigitalView))
        {
            return new DigitalPermissionDto(
                CanRead: true,
                CanDownload: _currentUser.HasPermission(PermissionCodes.DigitalDownload),
                CanPrint: true,
                ReadablePages: null,
                NeedsRequest: false,
                RequestStatus: null,
                AccessExpireAt: null,
                Reason: "Cán bộ thư viện xem toàn văn.");
        }

        var readerId = _currentUser.ReaderId;

        // 2. Mức CẤM: chỉ còn thấy thông tin thư mục.
        if (document.AccessLevel == DigitalAccessLevel.Forbidden)
        {
            return Denied("Tài liệu này không phục vụ đọc trực tuyến.");
        }

        // 3. Mức HẠN CHẾ: phải có yêu cầu đã duyệt còn hiệu lực.
        if (document.AccessLevel == DigitalAccessLevel.Restricted)
        {
            if (readerId is null)
            {
                return Preview(
                    document,
                    needsRequest: true,
                    status: null,
                    "Tài liệu hạn chế — đăng nhập rồi gửi yêu cầu để đọc toàn văn.");
            }

            var request = await _db.DigitalAccessRequests
                .AsNoTracking()
                .Where(row => row.DocumentId == document.Id && row.ReaderId == readerId)
                .OrderByDescending(row => row.RequestDate)
                .FirstOrDefaultAsync(ct);

            if (request is not null && request.IsUsable(now))
            {
                // Cán bộ duyệt là người quyết định cho riêng bạn đọc này tải hay không, nên lời
                // duyệt cộng thêm quyền chứ không bị chính sách chung của tài liệu bác đi — nếu
                // không thì ô "cho tải về" trên màn hình duyệt chẳng có tác dụng gì.
                return new DigitalPermissionDto(
                    CanRead: true,
                    CanDownload: request.AllowDownload || document.AllowDownload,
                    CanPrint: document.AllowPrint,
                    ReadablePages: null,
                    NeedsRequest: false,
                    RequestStatus: request.Status,
                    AccessExpireAt: request.ExpireAt,
                    Reason: "Yêu cầu đọc đã được duyệt.");
            }

            var reason = request?.Status switch
            {
                AccessRequestStatus.Pending => "Yêu cầu đọc đang chờ thư viện duyệt.",
                AccessRequestStatus.Rejected => "Yêu cầu đọc đã bị từ chối.",
                AccessRequestStatus.Revoked => "Quyền đọc đã bị thu hồi.",
                _ when request is not null => "Quyền đọc đã hết hạn hoặc hết lượt xem.",
                _ => "Tài liệu hạn chế — gửi yêu cầu để đọc toàn văn.",
            };

            return Preview(document, needsRequest: true, request?.Status, reason);
        }

        // 4. Mức NỘI BỘ: bạn đọc đã đăng nhập.
        if (document.AccessLevel == DigitalAccessLevel.Internal && readerId is null)
        {
            return Preview(document, needsRequest: false, null, "Đăng nhập để đọc toàn văn tài liệu này.");
        }

        // 5. Công khai, hoặc nội bộ và đã đăng nhập.
        return new DigitalPermissionDto(
            CanRead: true,
            CanDownload: document.AllowDownload,
            CanPrint: document.AllowPrint,
            ReadablePages: null,
            NeedsRequest: false,
            RequestStatus: null,
            AccessExpireAt: null,
            Reason: document.AccessLevel == DigitalAccessLevel.Public
                ? "Tài liệu công khai."
                : "Tài liệu nội bộ, bạn đọc đã đăng nhập.");
    }

    /// <summary>Chỉ được xem mấy trang đầu — vừa đủ để biết tài liệu có đúng thứ mình cần không.</summary>
    private static DigitalPermissionDto Preview(
        DigitalDocument document, bool needsRequest, AccessRequestStatus? status, string reason) =>
        new(
            CanRead: document.PreviewPages > 0,
            CanDownload: false,
            CanPrint: false,
            ReadablePages: Math.Max(0, document.PreviewPages),
            NeedsRequest: needsRequest,
            RequestStatus: status,
            AccessExpireAt: null,
            Reason: reason);

    private static DigitalPermissionDto Denied(string reason) =>
        new(false, false, false, 0, false, null, null, reason);
}
