using FluentValidation;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Digital;

// ---------------------------------------------------------------------------------------------
// V.2 — Thời lượng đọc.
//
// Nhật ký truy cập có cột "thời lượng" từ phase 10 nhưng chưa bao giờ được ghi: máy chủ chỉ biết
// lúc mở tài liệu, không biết lúc đóng. Trình đọc giờ báo về số giây đã đọc — định kỳ trong lúc
// đọc và một lần cuối khi rời trang — và máy chủ ghi vào đúng dòng "Xem" đã mở lượt ấy.
// ---------------------------------------------------------------------------------------------

/// <summary>Báo số giây đã đọc của lượt mở tài liệu gần nhất.</summary>
public class RecordDigitalReadingTimeCommand : IRequest
{
    public Guid DocumentId { get; set; }

    /// <summary>Tổng số giây kể từ lúc mở trình đọc, không phải phần tăng thêm — gọi lặp cũng không cộng dồn sai.</summary>
    public int Seconds { get; set; }
}

public class RecordDigitalReadingTimeCommandValidator : AbstractValidator<RecordDigitalReadingTimeCommand>
{
    /// <summary>Một phiên đọc không thể dài hơn một ngày; hơn thế là đồng hồ phía trình duyệt sai.</summary>
    public const int MaxSeconds = 24 * 60 * 60;

    public RecordDigitalReadingTimeCommandValidator()
    {
        RuleFor(command => command.Seconds)
            .InclusiveBetween(0, MaxSeconds)
            .WithMessage($"Thời lượng đọc phải từ 0 tới {MaxSeconds:N0} giây.");
    }
}

public class RecordDigitalReadingTimeCommandHandler : IRequestHandler<RecordDigitalReadingTimeCommand>
{
    /// <summary>Chỉ nhận cho lượt mở trong vòng này; xa hơn là báo lạc của một phiên cũ.</summary>
    private static readonly TimeSpan Window = TimeSpan.FromHours(24);

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public RecordDigitalReadingTimeCommandHandler(
        IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task Handle(RecordDigitalReadingTimeCommand command, CancellationToken ct)
    {
        var since = _clock.Now - Window;

        var query = _db.DigitalAccessLogs
            .Where(log => log.DocumentId == command.DocumentId
                && log.Action == DigitalAccessAction.View
                && log.PageFrom == null
                && log.OccurredAt >= since);

        // Dòng "Xem" của chính người đang đọc: bạn đọc theo mã bạn đọc, cán bộ theo mã người dùng,
        // khách vãng lai theo địa chỉ IP — không có cách nào khác để nhận ra khách.
        query = _currentUser.ReaderId is { } readerId
            ? query.Where(log => log.ReaderId == readerId)
            : _currentUser.UserId is { } userId
                ? query.Where(log => log.UserId == userId)
                : query.Where(log => log.ReaderId == null && log.UserId == null && log.Ip == _currentUser.Ip);

        var log = await query
            .OrderByDescending(row => row.OccurredAt)
            .FirstOrDefaultAsync(ct);

        if (log is null)
        {
            // Không có lượt mở nào để gắn vào — báo lạc thì bỏ, không phải lỗi của người gọi.
            return;
        }

        // Trình đọc gửi tổng số giây, nên lấy số lớn nhất từng nhận; báo tới muộn hơn không làm giảm.
        if (command.Seconds > (log.DurationSeconds ?? 0))
        {
            log.DurationSeconds = command.Seconds;
            await _db.SaveChangesAsync(ct);
        }
    }
}
