using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Đọc trạng thái thật của tài khoản cán bộ / thẻ bạn đọc đứng sau một thẻ đăng nhập.
///
/// Xem chú thích của <see cref="ISessionValidator"/> để biết vì sao câu hỏi này thuộc tầng xác thực.
/// </summary>
public class SessionValidator : ISessionValidator
{
    /// <summary>
    /// Đệm ngắn: đủ để một màn hình nạp mười lượt gọi chỉ tốn một lượt đọc, mà lệnh khoá vẫn có
    /// tác dụng gần như tức thì ngay cả khi máy chủ chạy nhiều tiến trình và đệm không xoá được ở
    /// tiến trình kia.
    /// </summary>
    private static readonly TimeSpan Dem = TimeSpan.FromSeconds(30);

    private const string KhoaCanBo = "session:user:";
    private const string KhoaBanDoc = "session:reader:";

    private readonly IApplicationDbContext _db;
    private readonly ICacheService _cache;
    private readonly IDateTimeProvider _clock;

    public SessionValidator(IApplicationDbContext db, ICacheService cache, IDateTimeProvider clock)
    {
        _db = db;
        _cache = cache;
        _clock = clock;
    }

    public async Task<string?> RejectionReasonAsync(
        Guid? userId, Guid? readerId, CancellationToken ct = default)
    {
        if (userId is { } user)
        {
            return await _cache.GetOrCreateAsync(
                KhoaCanBo + user, token => LyDoCanBoAsync(user, token), Dem, ct);
        }

        if (readerId is { } reader)
        {
            return await _cache.GetOrCreateAsync(
                KhoaBanDoc + reader, token => LyDoBanDocAsync(reader, token), Dem, ct);
        }

        return null;
    }

    public Task ForgetUserAsync(Guid userId, CancellationToken ct = default) =>
        _cache.RemoveAsync(KhoaCanBo + userId, ct);

    public Task ForgetReaderAsync(Guid readerId, CancellationToken ct = default) =>
        _cache.RemoveAsync(KhoaBanDoc + readerId, ct);

    private async Task<string?> LyDoCanBoAsync(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(entity => entity.Id == userId)
            .Select(entity => new { entity.IsActive, entity.LockedUntil, entity.DeletedAt })
            .FirstOrDefaultAsync(ct);

        if (user is null || user.DeletedAt is not null)
        {
            return "Tài khoản không còn tồn tại. Vui lòng đăng nhập lại.";
        }

        if (!user.IsActive)
        {
            return "Tài khoản đã bị ngừng hoạt động. Vui lòng liên hệ quản trị viên.";
        }

        if (user.LockedUntil is { } den && den > _clock.Now)
        {
            return $"Tài khoản đang bị khóa đến {den.ToLocalTime():HH:mm dd/MM/yyyy}. "
                   + "Vui lòng liên hệ quản trị viên.";
        }

        return null;
    }

    private async Task<string?> LyDoBanDocAsync(Guid readerId, CancellationToken ct)
    {
        var reader = await _db.Readers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(entity => entity.Id == readerId)
            .Select(entity => new { entity.Status, entity.CardExpireDate, entity.DeletedAt })
            .FirstOrDefaultAsync(ct);

        if (reader is null || reader.DeletedAt is not null)
        {
            return "Hồ sơ bạn đọc không còn tồn tại. Vui lòng liên hệ thư viện.";
        }

        // Thẻ hết hạn thì bạn đọc vẫn xem được thông tin của mình để biết mà đi gia hạn — đó là
        // trạng thái "Hết hạn". Còn "Tạm khóa" và "Khóa" là lệnh của thư viện, phải có tác dụng ngay.
        return reader.Status switch
        {
            ReaderStatus.Suspended => "Thẻ bạn đọc đang bị tạm khóa. Vui lòng liên hệ thư viện.",
            ReaderStatus.Locked => "Thẻ bạn đọc đang bị khóa. Vui lòng liên hệ thư viện.",
            _ => null
        };
    }
}
