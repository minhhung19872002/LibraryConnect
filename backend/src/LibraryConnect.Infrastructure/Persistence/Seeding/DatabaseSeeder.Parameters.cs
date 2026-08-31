using LibraryConnect.Application.Common.Security;
using LibraryConnect.Domain.Entities.Sys;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

public partial class DatabaseSeeder
{
    /// <summary>
    /// The configurable surface of the product. Nothing customer-specific is compiled in: the
    /// library name, the numbering rules, the circulation defaults and the OPAC branding all live
    /// here and are editable from screen I.3.
    /// </summary>
    private static readonly IReadOnlyList<ParameterSeed> ParameterSeeds = new List<ParameterSeed>
    {
        // ---- Thông tin thư viện ----
        new(ParameterKeys.LibraryName, "LIBRARY", "Thông tin thư viện", "Tên thư viện",
            "Tên hiển thị trên giao diện quản trị, OPAC và các biểu mẫu in.", ParameterDataType.Text, "Thư viện"),
        new(ParameterKeys.LibraryNameEn, "LIBRARY", "Thông tin thư viện", "Tên thư viện (tiếng Anh)",
            null, ParameterDataType.Text, "Library"),
        new(ParameterKeys.LibraryAddress, "LIBRARY", "Thông tin thư viện", "Địa chỉ", null, ParameterDataType.Text, ""),
        new(ParameterKeys.LibraryPhone, "LIBRARY", "Thông tin thư viện", "Điện thoại", null, ParameterDataType.Text, ""),
        new(ParameterKeys.LibraryEmail, "LIBRARY", "Thông tin thư viện", "Email", null, ParameterDataType.Text, ""),
        new(ParameterKeys.LibraryWebsite, "LIBRARY", "Thông tin thư viện", "Website", null, ParameterDataType.Text, ""),
        new(ParameterKeys.LibraryLogoUrl, "LIBRARY", "Thông tin thư viện", "Logo thư viện",
            "Ảnh hiển thị ở đầu trang quản trị và trên các biểu mẫu in.", ParameterDataType.File, ""),

        // ---- Biên mục ----
        new(ParameterKeys.MarcCatalogingSource, "CATALOG", "Cấu hình biên mục", "Nguồn biên mục (MARC 040$a)",
            "Giá trị tự động điền vào trường 040$a khi tạo biểu ghi mới.", ParameterDataType.Text, "Thư viện"),
        new(ParameterKeys.MarcDefaultLanguage, "CATALOG", "Cấu hình biên mục", "Mã ngôn ngữ mặc định",
            "Mã ISO 639-2 điền vào 041$a và 008/35-37.", ParameterDataType.Text, "vie"),
        new(ParameterKeys.MarcDefaultCountry, "CATALOG", "Cấu hình biên mục", "Mã nước xuất bản mặc định",
            "Mã MARC điền vào 008/15-17 và 044$a.", ParameterDataType.Text, "vm"),
        new(ParameterKeys.MarcControlNumberPrefix, "CATALOG", "Cấu hình biên mục", "Tiền tố số kiểm soát (001)",
            null, ParameterDataType.Text, "LC"),
        new(ParameterKeys.CallNumberPattern, "CATALOG", "Cấu hình biên mục", "Quy tắc sinh ký hiệu xếp giá",
            "Các ô thay thế: {DDC}, {DDC:3}, {AUTHOR:3}, {AUTHOR_LAST:3}, {TITLE:3}, {YEAR}, {COPY}. " +
            "Ví dụ \"{DDC} {AUTHOR:3}\" cho ra 005.74 NGU.", ParameterDataType.Text, "{DDC} {AUTHOR:3}"),

        // ---- Quy tắc sinh mã ----
        new(ParameterKeys.BarcodePrefix, "CODE", "Quy tắc sinh mã", "Tiền tố mã vạch ĐKCB", null, ParameterDataType.Text, "LC"),
        new(ParameterKeys.BarcodeLength, "CODE", "Quy tắc sinh mã", "Độ dài phần số của mã vạch", null, ParameterDataType.Number, "8"),
        new(ParameterKeys.BarcodeResetYearly, "CODE", "Quy tắc sinh mã", "Đánh lại số mã vạch theo năm",
            "Khi bật, mã vạch chứa năm và số thứ tự bắt đầu lại từ 1 mỗi năm.", ParameterDataType.Boolean, "false"),
        new(ParameterKeys.RegisterNumberPrefix, "CODE", "Quy tắc sinh mã", "Tiền tố số đăng ký cá biệt", null, ParameterDataType.Text, "ĐKCB"),
        new(ParameterKeys.RegisterNumberLength, "CODE", "Quy tắc sinh mã", "Độ dài số đăng ký cá biệt", null, ParameterDataType.Number, "8"),
        new("CODE.REGISTER_RESET_YEARLY", "CODE", "Quy tắc sinh mã", "Đánh lại số ĐKCB theo năm", null, ParameterDataType.Boolean, "false"),
        new(ParameterKeys.CardNumberPrefix, "CODE", "Quy tắc sinh mã", "Tiền tố số thẻ bạn đọc", null, ParameterDataType.Text, "TV"),
        new(ParameterKeys.CardNumberLength, "CODE", "Quy tắc sinh mã", "Độ dài số thẻ bạn đọc", null, ParameterDataType.Number, "6"),
        new("CODE.CARD_RESET_YEARLY", "CODE", "Quy tắc sinh mã", "Đánh lại số thẻ theo năm", null, ParameterDataType.Boolean, "true"),
        new(ParameterKeys.OrderCodePrefix, "CODE", "Quy tắc sinh mã", "Tiền tố mã đơn đặt", null, ParameterDataType.Text, "DH"),
        new("CODE.ORDER_LENGTH", "CODE", "Quy tắc sinh mã", "Độ dài mã đơn đặt", null, ParameterDataType.Number, "5"),
        new("CODE.ORDER_RESET_YEARLY", "CODE", "Quy tắc sinh mã", "Đánh lại mã đơn đặt theo năm", null, ParameterDataType.Boolean, "true"),
        new(ParameterKeys.RequestCodePrefix, "CODE", "Quy tắc sinh mã", "Tiền tố mã yêu cầu mua", null, ParameterDataType.Text, "YC"),
        new("CODE.REQUEST_LENGTH", "CODE", "Quy tắc sinh mã", "Độ dài mã yêu cầu mua", null, ParameterDataType.Number, "5"),
        new("CODE.REQUEST_RESET_YEARLY", "CODE", "Quy tắc sinh mã", "Đánh lại mã yêu cầu theo năm", null, ParameterDataType.Boolean, "true"),
        new(ParameterKeys.LoanCodePrefix, "CODE", "Quy tắc sinh mã", "Tiền tố mã phiếu mượn", null, ParameterDataType.Text, "PM"),
        new("CODE.LOAN_LENGTH", "CODE", "Quy tắc sinh mã", "Độ dài mã phiếu mượn", null, ParameterDataType.Number, "8"),
        new(ParameterKeys.FineCodePrefix, "CODE", "Quy tắc sinh mã", "Tiền tố mã biên lai phạt", null, ParameterDataType.Text, "BL"),
        new("CODE.FINE_LENGTH", "CODE", "Quy tắc sinh mã", "Độ dài mã biên lai phạt", null, ParameterDataType.Number, "6"),
        new("CODE.CONTROL_LENGTH", "CODE", "Quy tắc sinh mã", "Độ dài số kiểm soát biểu ghi", null, ParameterDataType.Number, "8"),
        new("CODE.INVENTORY_LENGTH", "CODE", "Quy tắc sinh mã", "Độ dài mã kỳ kiểm kê", null, ParameterDataType.Number, "4"),
        new("CODE.INVENTORY_PREFIX", "CODE", "Quy tắc sinh mã", "Tiền tố mã kỳ kiểm kê", null, ParameterDataType.Text, "KK"),
        new("CODE.HANDOVER_PREFIX", "CODE", "Quy tắc sinh mã", "Tiền tố mã biên bản bàn giao", null, ParameterDataType.Text, "BB"),
        new("CODE.HANDOVER_LENGTH", "CODE", "Quy tắc sinh mã", "Độ dài mã biên bản bàn giao", null, ParameterDataType.Number, "5"),
        new("CODE.BINDING_PREFIX", "CODE", "Quy tắc sinh mã", "Tiền tố mã tập đóng", null, ParameterDataType.Text, "DT"),
        new("CODE.BINDING_LENGTH", "CODE", "Quy tắc sinh mã", "Độ dài mã tập đóng", null, ParameterDataType.Number, "5"),
        new("CODE.CLAIM_PREFIX", "CODE", "Quy tắc sinh mã", "Tiền tố mã khiếu nại", null, ParameterDataType.Text, "KN"),
        new("CODE.CLAIM_LENGTH", "CODE", "Quy tắc sinh mã", "Độ dài mã khiếu nại", null, ParameterDataType.Number, "5"),

        // ---- Bảo mật ----
        new(PasswordPolicyKeys.MinLength, "SECURITY", "Chính sách mật khẩu", "Độ dài mật khẩu tối thiểu", null, ParameterDataType.Number, "8"),
        new(PasswordPolicyKeys.RequireUppercase, "SECURITY", "Chính sách mật khẩu", "Bắt buộc có chữ hoa", null, ParameterDataType.Boolean, "false"),
        new(PasswordPolicyKeys.RequireLowercase, "SECURITY", "Chính sách mật khẩu", "Bắt buộc có chữ thường", null, ParameterDataType.Boolean, "true"),
        new(PasswordPolicyKeys.RequireDigit, "SECURITY", "Chính sách mật khẩu", "Bắt buộc có chữ số", null, ParameterDataType.Boolean, "true"),
        new(PasswordPolicyKeys.RequireSpecial, "SECURITY", "Chính sách mật khẩu", "Bắt buộc có ký tự đặc biệt", null, ParameterDataType.Boolean, "false"),
        new(PasswordPolicyKeys.ExpiryDays, "SECURITY", "Chính sách mật khẩu", "Số ngày buộc đổi mật khẩu",
            "Đặt 0 để không bắt buộc đổi định kỳ.", ParameterDataType.Number, "0"),
        new(PasswordPolicyKeys.MaxFailedLogin, "SECURITY", "Chính sách mật khẩu", "Khóa tài khoản sau số lần đăng nhập sai",
            "Đặt 0 để không tự khóa.", ParameterDataType.Number, "5"),
        new(PasswordPolicyKeys.LockMinutes, "SECURITY", "Chính sách mật khẩu", "Thời gian khóa tài khoản (phút)", null, ParameterDataType.Number, "15"),

        // ---- Lưu thông ----
        new(ParameterKeys.CirculationDefaultLoanDays, "CIRCULATION", "Cấu hình lưu thông", "Số ngày mượn mặc định",
            "Áp dụng khi không có chính sách nào khớp.", ParameterDataType.Number, "14"),
        new(ParameterKeys.CirculationDefaultMaxItems, "CIRCULATION", "Cấu hình lưu thông", "Số tài liệu mượn tối đa mặc định", null, ParameterDataType.Number, "5"),
        new(ParameterKeys.CirculationFinePerDay, "CIRCULATION", "Cấu hình lưu thông", "Tiền phạt quá hạn mỗi ngày (VNĐ)", null, ParameterDataType.Number, "2000"),
        new(ParameterKeys.CirculationDueSoonDays, "CIRCULATION", "Cấu hình lưu thông", "Nhắc hạn trả trước số ngày", null, ParameterDataType.Number, "3"),
        new("CIRCULATION.SKIP_HOLIDAY", "CIRCULATION", "Cấu hình lưu thông", "Dời hạn trả khỏi ngày nghỉ",
            "Khi bật, hạn trả rơi vào ngày nghỉ sẽ chuyển sang ngày làm việc kế tiếp và ngày nghỉ không tính phạt.",
            ParameterDataType.Boolean, "true"),

        // ---- OPAC ----
        new(ParameterKeys.OpacPageSize, "OPAC", "Cấu hình OPAC", "Số kết quả mỗi trang", null, ParameterDataType.Number, "20"),
        new(ParameterKeys.OpacShowPoweredBy, "OPAC", "Cấu hình OPAC", "Hiển thị dòng Powered by LibraryConnect", null, ParameterDataType.Boolean, "true"),
        new(ParameterKeys.OpacAllowHold, "OPAC", "Cấu hình OPAC", "Cho phép bạn đọc đặt giữ chỗ từ OPAC", null, ParameterDataType.Boolean, "true"),
        new(ParameterKeys.OpacMaxHoldPerReader, "OPAC", "Cấu hình OPAC", "Số đăng ký mượn đồng thời tối đa", null, ParameterDataType.Number, "3"),
        new(ParameterKeys.OpacAllowReview, "OPAC", "Cấu hình OPAC", "Cho phép bạn đọc nhận xét tài liệu", null, ParameterDataType.Boolean, "false"),

        // ---- Tải lên ----
        new(ParameterKeys.UploadMaxSizeMb, "UPLOAD", "Giới hạn tải lên", "Dung lượng tối đa mỗi tệp (MB)", null, ParameterDataType.Number, "500"),
        new(ParameterKeys.UploadAllowedExtensions, "UPLOAD", "Giới hạn tải lên", "Phần mở rộng được phép",
            "Danh sách ngăn cách bằng dấu phẩy.", ParameterDataType.Text,
            "pdf,docx,doc,epub,mp4,mp3,jpg,jpeg,png,gif,zip,iso,mrc,xlsx,xls,xml"),

        // ---- Email ----
        new("SMTP.ENABLED", "SMTP", "Cấu hình email", "Bật gửi email", null, ParameterDataType.Boolean, "false"),
        new("SMTP.HOST", "SMTP", "Cấu hình email", "Máy chủ SMTP", null, ParameterDataType.Text, ""),
        new("SMTP.PORT", "SMTP", "Cấu hình email", "Cổng SMTP", null, ParameterDataType.Number, "587"),
        new("SMTP.USE_SSL", "SMTP", "Cấu hình email", "Dùng SSL/TLS", null, ParameterDataType.Boolean, "true"),
        new("SMTP.USERNAME", "SMTP", "Cấu hình email", "Tài khoản SMTP", null, ParameterDataType.Text, ""),
        new("SMTP.PASSWORD", "SMTP", "Cấu hình email", "Mật khẩu SMTP", null, ParameterDataType.Password, "", IsSecret: true),
        new("SMTP.FROM_ADDRESS", "SMTP", "Cấu hình email", "Địa chỉ gửi", null, ParameterDataType.Text, ""),
        new("SMTP.FROM_NAME", "SMTP", "Cấu hình email", "Tên người gửi", null, ParameterDataType.Text, "Thư viện"),

        // ---- Sao lưu ----
        new("BACKUP.AUTO_ENABLED", "BACKUP", "Cấu hình sao lưu", "Bật sao lưu tự động", null, ParameterDataType.Boolean, "true"),
        new("BACKUP.SCHEDULE_CRON", "BACKUP", "Cấu hình sao lưu", "Lịch sao lưu (cron)",
            "Ví dụ 0 2 * * * là 2 giờ sáng hằng ngày.", ParameterDataType.Cron, "0 2 * * *"),
        new("BACKUP.KEEP_COUNT", "BACKUP", "Cấu hình sao lưu", "Số bản sao lưu giữ lại", null, ParameterDataType.Number, "30"),
        new("BACKUP.INCLUDE_FILES", "BACKUP", "Cấu hình sao lưu", "Sao lưu kèm tệp tài liệu số", null, ParameterDataType.Boolean, "true"),
        new("BACKUP.NOTIFY_EMAIL", "BACKUP", "Cấu hình sao lưu", "Email nhận cảnh báo khi sao lưu lỗi", null, ParameterDataType.Text, ""),

        // ---- Mobile (chuẩn bị cho đợt sau) ----
        new(ParameterKeys.MobileSelfCheckoutEnabled, "MOBILE", "Cấu hình ứng dụng di động", "Cho phép mượn tự phục vụ", null, ParameterDataType.Boolean, "false"),
        new(ParameterKeys.MobileSelfCheckoutWifiSsid, "MOBILE", "Cấu hình ứng dụng di động", "Tên Wi-Fi xác thực vị trí",
            "App chỉ cho mượn tự phục vụ khi thiết bị đang kết nối Wi-Fi này.", ParameterDataType.Text, ""),
        new(ParameterKeys.MobileSelfCheckoutQrSecret, "MOBILE", "Cấu hình ứng dụng di động", "Khóa bí mật mã QR đặt tại kho",
            null, ParameterDataType.Password, "", IsSecret: true)
    };

    private async Task SeedSystemParametersAsync(CancellationToken ct)
    {
        var existing = await _db.SystemParameters.ToDictionaryAsync(p => p.Key, ct);
        var added = 0;
        var order = 0;

        foreach (var seed in ParameterSeeds)
        {
            order += 10;

            if (existing.TryGetValue(seed.Key, out var parameter))
            {
                // Labels and defaults may improve between releases; the operator's own value is kept.
                parameter.GroupName = seed.GroupName;
                parameter.Name = seed.Name;
                parameter.Description = seed.Description;
                parameter.DefaultValue = seed.DefaultValue;
                continue;
            }

            _db.SystemParameters.Add(new SystemParameter
            {
                Key = seed.Key,
                GroupCode = seed.GroupCode,
                GroupName = seed.GroupName,
                Name = seed.Name,
                Description = seed.Description,
                DataType = seed.DataType,
                DefaultValue = seed.DefaultValue,
                Value = seed.DefaultValue,
                IsSecret = seed.IsSecret,
                SortOrder = order,
                CreatedAt = _clock.Now
            });

            added++;
        }

        if (await _db.SaveChangesAsync(ct) > 0 && added > 0)
        {
            _logger.LogInformation("Đã bổ sung {Count} tham số hệ thống", added);
        }
    }

    /// <summary>
    /// Audit is on for writes on every entity by default. Reads are off except where the E-HSMT
    /// explicitly wants a read trail (digital documents and reader records).
    /// </summary>
    private async Task SeedAuditSettingsAsync(CancellationToken ct)
    {
        var defaults = new (string Entity, string DisplayName, bool LogRead)[]
        {
            ("User", "Người dùng", false),
            ("UserGroup", "Nhóm người dùng", false),
            ("GroupPermission", "Phân quyền nhóm", false),
            ("UserDataScope", "Phạm vi dữ liệu", false),
            ("SystemParameter", "Tham số hệ thống", false),
            ("BibRecord", "Biểu ghi thư mục", false),
            ("Item", "Ấn phẩm (ĐKCB)", false),
            ("Reader", "Bạn đọc", true),
            ("Loan", "Giao dịch mượn trả", false),
            ("Hold", "Đặt giữ chỗ", false),
            ("Fine", "Tiền phạt", false),
            ("DigitalDocument", "Tài liệu số", true),
            ("DigitalAccessRequest", "Yêu cầu truy cập tài liệu số", false),
            ("PurchaseRequest", "Yêu cầu đặt mua", false),
            ("PurchaseOrder", "Đơn đặt hàng", false),
            ("InventoryPeriod", "Kỳ kiểm kê", false),
            ("Serial", "Ấn phẩm định kỳ", false),
            ("CmsNews", "Tin tức", false),
            ("CmsPage", "Trang tĩnh", false),
            ("BackupJob", "Sao lưu / phục hồi", false)
        };

        var existing = await _db.AuditSettings.Select(s => s.Entity).ToListAsync(ct);
        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (entity, displayName, logRead) in defaults.Where(d => !existingSet.Contains(d.Entity)))
        {
            _db.AuditSettings.Add(new AuditSetting
            {
                Entity = entity,
                DisplayName = displayName,
                LogCreate = true,
                LogUpdate = true,
                LogDelete = true,
                LogRead = logRead,
                // Null means keep forever, which is what the E-HSMT requires.
                RetentionDays = null,
                CreatedAt = _clock.Now
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    private sealed record ParameterSeed(
        string Key,
        string GroupCode,
        string GroupName,
        string Name,
        string? Description,
        ParameterDataType DataType,
        string DefaultValue,
        bool IsSecret = false);
}
