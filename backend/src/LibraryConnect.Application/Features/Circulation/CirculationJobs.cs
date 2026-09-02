using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Application.Features.Circulation;

/// <summary>Các việc lưu thông chạy hằng ngày (mục 6.5).</summary>
public interface ICirculationDailyJobs
{
    Task MarkOverdueAsync();
    Task SendDueSoonRemindersAsync();
    Task ExpireHoldsAsync();
}

/// <summary>
/// Ba việc phải làm mỗi ngày mà không ai bấm nút: đánh dấu quá hạn, nhắc sắp đến hạn và thu hồi các
/// bản giữ ở quầy quá lâu không có người tới nhận.
///
/// Chạy nền vì chúng phải xảy ra kể cả những ngày không ai đăng nhập vào hệ thống — và vì bản giữ
/// nằm chết ở quầy là một bản sách bị khóa khỏi lưu thông.
/// </summary>
public class CirculationDailyJobs : ICirculationDailyJobs
{
    /// <summary>Nhắc trước hạn trả bao nhiêu ngày.</summary>
    public const string DueSoonDaysParameter = "CIRCULATION.DUE_SOON_DAYS";

    private readonly IApplicationDbContext _db;
    private readonly ISender _mediator;
    private readonly ISystemParameterService _parameters;
    private readonly INotificationSender _notifications;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<CirculationDailyJobs> _logger;

    public CirculationDailyJobs(
        IApplicationDbContext db,
        ISender mediator,
        ISystemParameterService parameters,
        INotificationSender notifications,
        IDateTimeProvider clock,
        ILogger<CirculationDailyJobs> logger)
    {
        _db = db;
        _mediator = mediator;
        _parameters = parameters;
        _notifications = notifications;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>Chuyển các lượt mượn đã qua hạn sang trạng thái quá hạn.</summary>
    public async Task MarkOverdueAsync()
    {
        var today = _clock.Today;

        var loans = await _db.Loans
            .Where(loan => loan.Status == LoanStatus.Active && loan.DueDate < today)
            .ToListAsync();

        foreach (var loan in loans)
        {
            loan.Status = LoanStatus.Overdue;
        }

        if (loans.Count > 0)
        {
            await _db.SaveChangesAsync();
        }

        // Phase 15: báo ngay ngày đầu quá hạn, gộp theo bạn đọc. Ứng dụng bấm vào là mở "Sách của tôi".
        foreach (var group in loans.GroupBy(loan => loan.ReaderId))
        {
            var titles = group.Select(loan => $"• {loan.BibTitle} — hạn trả {loan.DueDate:dd/MM/yyyy}");

            await _notifications.SendAsync(group.Key,
                NotificationKinds.Overdue,
                "Tài liệu đã quá hạn trả",
                $"Bạn có {group.Count()} tài liệu đã quá hạn trả:\n{string.Join("\n", titles)}\n" +
                "Vui lòng mang tới trả sớm để không bị tính thêm phí.",
                "/tai-khoan",
                null);
        }

        _logger.LogInformation("Đánh dấu quá hạn: {Count} lượt mượn", loans.Count);
    }

    /// <summary>Nhắc bạn đọc sắp đến hạn trả.</summary>
    public async Task SendDueSoonRemindersAsync()
    {
        var today = _clock.Today;
        var days = await _parameters.GetAsync(DueSoonDaysParameter, 3);
        var limit = today.AddDays(Math.Max(1, days));

        var loans = await _db.Loans
            .AsNoTracking()
            .Where(loan => loan.Status == LoanStatus.Active
                           && loan.DueDate >= today
                           && loan.DueDate <= limit)
            .Select(loan => new
            {
                loan.ReaderId,
                ReaderName = loan.Reader!.FullName,
                loan.BibTitle,
                loan.Barcode,
                loan.DueDate
            })
            .ToListAsync();

        // Gộp theo bạn đọc: ba quyển sắp hết hạn là một thư, không phải ba thư.
        foreach (var group in loans.GroupBy(loan => loan.ReaderId))
        {
            var lines = group
                .OrderBy(loan => loan.DueDate)
                .Select(loan => $"• {loan.BibTitle} (mã vạch {loan.Barcode}) — hạn trả {loan.DueDate:dd/MM/yyyy}");

            await _notifications.SendAsync(group.Key,
                NotificationKinds.DueSoon,
                "Nhắc hạn trả tài liệu",
                $"Thư viện xin nhắc {group.First().ReaderName} có {group.Count()} tài liệu sắp đến hạn trả:\n" +
                string.Join("\n", lines) +
                "\nBạn đọc có thể mang tài liệu tới trả hoặc gia hạn trên trang tra cứu.",
                "/tai-khoan",
                null);
        }

        _logger.LogInformation(
            "Nhắc hạn trả: {Readers} bạn đọc, {Loans} tài liệu",
            loans.Select(loan => loan.ReaderId).Distinct().Count(), loans.Count);
    }

    /// <summary>
    /// Thu hồi các bản giữ ở quầy đã quá hạn nhận và chuyển cho người kế tiếp trong hàng đợi.
    /// </summary>
    public async Task ExpireHoldsAsync()
    {
        var now = _clock.Now;

        var expired = await _db.Holds
            .Where(hold => hold.Status == HoldStatus.Ready
                           && hold.ExpireDate != null
                           && hold.ExpireDate < now)
            .ToListAsync();

        foreach (var hold in expired)
        {
            hold.Status = HoldStatus.Expired;

            await _notifications.SendAsync(hold.ReaderId,
                "Phiếu đặt giữ đã hết hạn nhận",
                "Tài liệu bạn đặt giữ đã quá hạn nhận nên được chuyển cho bạn đọc kế tiếp. " +
                "Bạn có thể đặt giữ lại nếu vẫn cần.");

            if (hold.ItemId is not null)
            {
                await HoldReader.PassToNextAsync(_db, hold.ItemId.Value, hold.BibId, now, default);
            }

            await HoldReader.ResequenceAsync(_db, hold.BibId, default);
        }

        // Phiếu chờ quá lâu mà chưa bao giờ có sách cũng nên đóng lại, nếu thư viện có khai hạn.
        if (expired.Count > 0)
        {
            await _db.SaveChangesAsync();
        }

        _logger.LogInformation("Hết hạn giữ chỗ: {Count} phiếu", expired.Count);

        // Đóng các lượt vào thư viện còn bỏ ngỏ của hôm qua.
        var closed = await _mediator.Send(new CloseOpenVisitsCommand(_clock.Today.AddDays(-1)));

        if (closed > 0)
        {
            _logger.LogInformation("Đóng {Count} lượt vào thư viện còn bỏ ngỏ", closed);
        }
    }
}
