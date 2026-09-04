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
        new(ParameterKeys.LibraryMarcOrgCode, "LIBRARY", "Thông tin thư viện", "Mã cơ quan MARC",
            "Dạng VN-XXXXX, do Thư viện Quốc hội Mỹ cấp. Đi vào trường 003 và 040$a của mọi biểu ghi "
            + "thư viện phát ra — đây là cách nơi khác nhận ra biểu ghi do ai biên mục. Chưa đăng ký "
            + "được thì đặt tạm tên miền của thư viện. Bỏ trống thì biểu ghi xuất ra thiếu xuất xứ.",
            ParameterDataType.Text, ""),

        // ---- Biên mục ----
        new(ParameterKeys.MarcCatalogingSource, "CATALOG", "Cấu hình biên mục", "Nguồn biên mục (MARC 040$a)",
            "Giá trị tự động điền vào trường 040$a khi tạo biểu ghi mới.", ParameterDataType.Text, "Thư viện"),
        new(ParameterKeys.GoogleBooksApiKey, "CATALOG", "Cấu hình biên mục", "Khóa API Google Books",
            "Dùng khi tra ảnh bìa theo ISBN. Bỏ trống thì Google tính vào hạn mức dùng chung của mọi "
            + "người và hầu như lúc nào cũng trả lỗi 429 (hết hạn mức) — khi đó hệ thống chỉ còn tra "
            + "được ở Open Library. Lấy khóa miễn phí tại console.cloud.google.com, bật Books API.",
            ParameterDataType.Password, "", IsSecret: true),
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
        new("CODE.TRANSFER_PREFIX", "CODE", "Quy tắc sinh mã", "Tiền tố số phiếu chuyển kho", null, ParameterDataType.Text, "PCK"),
        new("CODE.TRANSFER_LENGTH", "CODE", "Quy tắc sinh mã", "Độ dài số phiếu chuyển kho", null, ParameterDataType.Number, "5"),
        new("CODE.TRANSFER_RESET_YEARLY", "CODE", "Quy tắc sinh mã", "Đánh lại số phiếu chuyển kho theo năm", null, ParameterDataType.Boolean, "true"),
        new("CODE.DISPOSAL_PREFIX", "CODE", "Quy tắc sinh mã", "Tiền tố số quyết định thanh lý", null, ParameterDataType.Text, "QĐTL"),
        new("CODE.DISPOSAL_LENGTH", "CODE", "Quy tắc sinh mã", "Độ dài số quyết định thanh lý", null, ParameterDataType.Number, "4"),
        new("CODE.DISPOSAL_RESET_YEARLY", "CODE", "Quy tắc sinh mã", "Đánh lại số quyết định thanh lý theo năm", null, ParameterDataType.Boolean, "true"),
        new("CODE.BINDING_PREFIX", "CODE", "Quy tắc sinh mã", "Tiền tố mã tập đóng", null, ParameterDataType.Text, "DT"),
        new("CODE.BINDING_LENGTH", "CODE", "Quy tắc sinh mã", "Độ dài mã tập đóng", null, ParameterDataType.Number, "5"),
        new("CODE.CLAIM_PREFIX", "CODE", "Quy tắc sinh mã", "Tiền tố mã khiếu nại", null, ParameterDataType.Text, "KN"),
        new("CODE.CLAIM_LENGTH", "CODE", "Quy tắc sinh mã", "Độ dài mã khiếu nại", null, ParameterDataType.Number, "5"),

        // Hậu tố (I.3 nêu "prefix/suffix/độ dài/reset theo năm"). Bộ sinh mã đọc khoá `_SUFFIX` từ
        // đầu, nhưng không có dòng tham số nào nên màn hình không hiện ô để nhập — thư viện muốn mã
        // dạng "ĐKCB000123/TV" phải sửa cấu hình bằng tay. Mặc định rỗng: mã cũ không đổi.
        new("CODE.BARCODE_SUFFIX", "CODE", "Quy tắc sinh mã", "Hậu tố mã vạch ĐKCB",
            "Nối vào sau phần số, ví dụ \"/TV\". Bỏ trống là không có hậu tố.", ParameterDataType.Text, ""),
        new("CODE.REGISTER_SUFFIX", "CODE", "Quy tắc sinh mã", "Hậu tố số đăng ký cá biệt", null, ParameterDataType.Text, ""),
        new("CODE.CARD_SUFFIX", "CODE", "Quy tắc sinh mã", "Hậu tố số thẻ bạn đọc", null, ParameterDataType.Text, ""),
        new("CODE.ORDER_SUFFIX", "CODE", "Quy tắc sinh mã", "Hậu tố mã đơn đặt",
            "Nhiều đơn vị ghi mã đơn kèm phòng ban, ví dụ \"/TV-ĐHM\".", ParameterDataType.Text, ""),
        new("CODE.REQUEST_SUFFIX", "CODE", "Quy tắc sinh mã", "Hậu tố mã yêu cầu mua", null, ParameterDataType.Text, ""),
        new("CODE.HANDOVER_SUFFIX", "CODE", "Quy tắc sinh mã", "Hậu tố mã biên bản bàn giao", null, ParameterDataType.Text, ""),
        new("CODE.TRANSFER_SUFFIX", "CODE", "Quy tắc sinh mã", "Hậu tố số phiếu chuyển kho", null, ParameterDataType.Text, ""),
        new("CODE.DISPOSAL_SUFFIX", "CODE", "Quy tắc sinh mã", "Hậu tố số quyết định thanh lý",
            "Ví dụ \"/QĐ-TV\" theo thể thức văn bản hành chính.", ParameterDataType.Text, ""),

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

        // ---- Bổ sung ----
        new("ACQ.APPROVAL_LEVELS", "ACQ", "Cấu hình bổ sung", "Số cấp duyệt yêu cầu đặt mua",
            "Yêu cầu phải đi qua đủ số cấp này mới thành Đã duyệt. Đặt 1 nếu chỉ một người duyệt.",
            ParameterDataType.Number, "1"),
        new("ACQ.APPROVAL_GROUPS", "ACQ", "Cấu hình bổ sung", "Nhóm duyệt từng cấp",
            "Mã nhóm người dùng duyệt từng cấp, ngăn cách bằng dấu phẩy theo đúng thứ tự cấp — ví dụ "
            + "ACQUISITION,LIBRARIAN nghĩa là cấp 1 do Cán bộ bổ sung duyệt, cấp 2 do Thủ thư. Bỏ trống "
            + "một vị trí thì cấp ấy ai có quyền duyệt cũng được. Một người không duyệt hai cấp liên tiếp.",
            ParameterDataType.Text, ""),
        new("ACQ.DUPLICATE_WARNING", "ACQ", "Cấu hình bổ sung", "Cảnh báo khi tài liệu đã có trong thư viện",
            "Khi bật, form đề nghị mua tự tra ISBN và nhan đề rồi báo nếu thư viện đã có.",
            ParameterDataType.Boolean, "true"),
        new("ACQ.ORDER_OVERDUE_DAYS", "ACQ", "Cấu hình bổ sung", "Số ngày quá hạn giao thì cảnh báo",
            "Đơn đặt quá ngày dự kiến giao quá số ngày này sẽ hiện cảnh báo trên danh sách đơn.",
            ParameterDataType.Number, "0"),

        // ---- Lưu thông ----
        // ---------------- Bạn đọc (Phân hệ VI) ----------------
        new("READER.CARD_EXPIRING_DAYS", "READER", "Cấu hình bạn đọc", "Cảnh báo thẻ sắp hết hạn trước số ngày",
            "Dùng cho cột cảnh báo trên danh sách bạn đọc và báo cáo thẻ sắp hết hạn.",
            ParameterDataType.Number, "30"),
        new("READER.IMPORT_MAPPING", "READER", "Cấu hình bạn đọc", "Ánh xạ cột tệp nhập bạn đọc",
            "Hồ sơ ánh xạ trường dữ liệu sang tiêu đề cột của tệp Excel, lưu từ màn hình Nhập bạn đọc.",
            ParameterDataType.Json, ""),
        new("READER.SYNC_MAPPING", "READER", "Cấu hình bạn đọc", "Ánh xạ trường đồng bộ từ hệ thống đào tạo",
            "Hồ sơ ánh xạ trường dữ liệu sang tên thuộc tính mà hệ thống quản lý đào tạo gửi sang.",
            ParameterDataType.Json, ""),

        new(ParameterKeys.CirculationDefaultLoanDays, "CIRCULATION", "Cấu hình lưu thông", "Số ngày mượn mặc định",
            "Áp dụng khi không có chính sách nào khớp.", ParameterDataType.Number, "14"),
        new(ParameterKeys.CirculationDefaultMaxItems, "CIRCULATION", "Cấu hình lưu thông", "Số tài liệu mượn tối đa mặc định", null, ParameterDataType.Number, "5"),
        new(ParameterKeys.CirculationFinePerDay, "CIRCULATION", "Cấu hình lưu thông", "Tiền phạt quá hạn mỗi ngày (VNĐ)", null, ParameterDataType.Number, "2000"),
        new(ParameterKeys.CirculationDueSoonDays, "CIRCULATION", "Cấu hình lưu thông", "Nhắc hạn trả trước số ngày", null, ParameterDataType.Number, "3"),
        new("CIRCULATION.WEEKLY_CLOSED_DAYS", "CIRCULATION", "Cấu hình lưu thông", "Thứ nghỉ hằng tuần",
            "Đếm theo cách người Việt đếm: thứ Hai là 1, Chủ nhật là 7. Nhiều ngày thì ngăn bằng dấu phẩy.",
            ParameterDataType.Text, "7"),
        new("CIRCULATION.DEBT_BLOCK_THRESHOLD", "CIRCULATION", "Cấu hình lưu thông",
            "Ngưỡng nợ phí chặn mượn (VNĐ)",
            "Nợ vượt mức này thì không cho mượn tiếp. Để 0 nghĩa là không chặn vì nợ phí.",
            ParameterDataType.Number, "50000"),
        new("CIRCULATION.BLOCK_ON_OVERDUE", "CIRCULATION", "Cấu hình lưu thông",
            "Chặn mượn khi đang giữ tài liệu quá hạn", null, ParameterDataType.Boolean, "true"),
        new("CIRCULATION.CLAMP_DUE_TO_CARD", "CIRCULATION", "Cấu hình lưu thông",
            "Cắt hạn trả theo ngày hết hạn thẻ",
            "Khi bật, hạn trả không bao giờ vượt quá ngày hết hạn thẻ bạn đọc.",
            ParameterDataType.Boolean, "true"),
        new("CIRCULATION.ALLOW_RENEW_OVERDUE", "CIRCULATION", "Cấu hình lưu thông",
            "Cho gia hạn tài liệu đã quá hạn", null, ParameterDataType.Boolean, "false"),
        new("CIRCULATION.LOST_COMPENSATION_FACTOR", "CIRCULATION", "Cấu hình lưu thông",
            "Hệ số bồi thường tài liệu mất",
            "Tiền bồi thường = giá ghi trên ĐKCB nhân hệ số này; tài liệu hỏng tính bằng một nửa.",
            ParameterDataType.Number, "2"),
        new("CIRCULATION.LOCKER_OVERDUE_MINUTES", "CIRCULATION", "Cấu hình lưu thông",
            "Cảnh báo giữ tủ quá số phút", null, ParameterDataType.Number, "240"),
        new("CIRCULATION.SELF_CHECKOUT_ENABLED", "CIRCULATION", "Cấu hình lưu thông",
            "Mở chức năng mượn tự phục vụ",
            "Cho phép bạn đọc tự quét mã vạch để mượn qua trang tra cứu hoặc ứng dụng.",
            ParameterDataType.Boolean, "false"),
        new("CIRCULATION.SELF_CHECKOUT_TOKENS", "CIRCULATION", "Cấu hình lưu thông",
            "Mã điểm mượn tự phục vụ",
            "Nội dung các mã QR dán tại kho, ngăn nhau bằng dấu phẩy. Để trống thì không kiểm tra vị trí.",
            ParameterDataType.Text, ""),
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

        // ---- Tài liệu số (Phân hệ V) ----
        new("DIGITAL.CHUNK_SIZE_MB", "DIGITAL", "Cấu hình tài liệu số", "Kích thước mỗi mảnh khi tải tệp lớn (MB)",
            "Tệp lớn hơn mức này được cắt thành nhiều mảnh, gián đoạn thì tải tiếp chứ không làm lại từ đầu.",
            ParameterDataType.Number, "8"),
        new("DIGITAL.UPLOAD_SESSION_HOURS", "DIGITAL", "Cấu hình tài liệu số", "Giữ phiên tải dở trong bao nhiêu giờ",
            "Quá thời gian này mà chưa tải xong thì các mảnh dở dang bị dọn đi.",
            ParameterDataType.Number, "24"),
        new("DIGITAL.PREVIEW_PAGES", "DIGITAL", "Cấu hình tài liệu số", "Số trang xem thử mặc định",
            "Áp dụng cho tài liệu mới tải lên; từng tài liệu vẫn sửa lại được.",
            ParameterDataType.Number, "10"),
        new("DIGITAL.WATERMARK_ENABLED", "DIGITAL", "Cấu hình tài liệu số", "Đóng chữ chìm khi đọc trực tuyến",
            "Chữ chìm gồm tên bạn đọc, thời điểm xem và địa chỉ IP, đóng lên từng trang.",
            ParameterDataType.Boolean, "true"),
        new("DIGITAL.READ_DPI", "DIGITAL", "Cấu hình tài liệu số", "Độ phân giải ảnh trang khi đọc trực tuyến (DPI)",
            "Cao thì nét nhưng nặng; 110 vừa đủ đọc trên màn hình.",
            ParameterDataType.Number, "110"),
        new("DIGITAL.THUMBNAIL_DPI", "DIGITAL", "Cấu hình tài liệu số", "Độ phân giải ảnh bìa (DPI)",
            null, ParameterDataType.Number, "60"),
        new("DIGITAL.OCR_ENABLED", "DIGITAL", "Cấu hình tài liệu số", "Nhận dạng ký tự cho tài liệu bản quét",
            "Tài liệu quét không có lớp chữ sẽ được nhận dạng để tìm kiếm được toàn văn.",
            ParameterDataType.Boolean, "true"),
        new("DIGITAL.OCR_MAX_PAGES", "DIGITAL", "Cấu hình tài liệu số", "Số trang nhận dạng tối đa mỗi tài liệu",
            "Giới hạn để một cuốn dày không chiếm máy chủ hàng giờ. Để 0 là nhận dạng hết.",
            ParameterDataType.Number, "50"),
        new("DIGITAL.OCR_LANGUAGE", "DIGITAL", "Cấu hình tài liệu số", "Ngôn ngữ nhận dạng ký tự",
            "Mã ngôn ngữ của Tesseract, ví dụ vie hoặc vie+eng.",
            ParameterDataType.Text, "vie"),
        new("DIGITAL.OCR_COMMAND", "DIGITAL", "Cấu hình tài liệu số", "Lệnh gọi công cụ nhận dạng ký tự",
            "Đường dẫn tới tesseract trong máy chủ. Để nguyên nếu tesseract nằm trong PATH.",
            ParameterDataType.Text, "tesseract"),
        new("DIGITAL.REQUEST_DEFAULT_DAYS", "DIGITAL", "Cấu hình tài liệu số", "Thời hạn mặc định khi duyệt yêu cầu đọc (ngày)",
            "Cán bộ vẫn sửa lại được cho từng yêu cầu.",
            ParameterDataType.Number, "30"),
        new("DIGITAL.REQUEST_DEFAULT_MAX_VIEWS", "DIGITAL", "Cấu hình tài liệu số", "Số lần xem tối đa mặc định khi duyệt",
            "Để 0 nghĩa là không giới hạn số lần xem.",
            ParameterDataType.Number, "0"),

        // ---- Liên thư viện (mục 3.3 và 3.4) ----
        new("ILL.Z3950_SERVER_ENABLED", "ILL", "Cấu hình liên thư viện", "Bật máy chủ Z39.50",
            "Cho phép thư viện khác tra cứu vào kho của mình qua giao thức Z39.50 (docker-compose công bố cổng 210).",
            ParameterDataType.Boolean, "true"),
        new("ILL.Z3950_SERVER_PORT", "ILL", "Cấu hình liên thư viện", "Cổng máy chủ Z39.50",
            "Chuẩn quy định cổng 210, nhưng cổng dưới 1024 cần quyền đặc biệt trên Linux nên có thể đổi.",
            ParameterDataType.Number, "2100"),
        new("ILL.Z3950_DATABASE_NAME", "ILL", "Cấu hình liên thư viện", "Tên cơ sở dữ liệu công bố",
            "Tên mà thư viện khác khai vào phần mềm của họ khi tra sang.",
            ParameterDataType.Text, "LibraryConnect"),
        new("ILL.Z3950_ALLOWED_IPS", "ILL", "Cấu hình liên thư viện", "Dải địa chỉ được phép tra Z39.50",
            "Danh sách địa chỉ hoặc tiền tố, ngăn nhau bằng dấu phẩy. Để trống là mở cho mọi nơi.",
            ParameterDataType.Text, ""),
        new("ILL.OAI_ENABLED", "ILL", "Cấu hình liên thư viện", "Mở kho OAI-PMH của mình",
            "Cho phép nơi khác thu hoạch metadata thư mục qua địa chỉ /oai.",
            ParameterDataType.Boolean, "true"),
        new("ILL.HARVEST_CRON", "ILL", "Cấu hình liên thư viện", "Lịch thu hoạch OAI-PMH",
            "Biểu thức cron của tác vụ nền chạy thu hoạch mọi kho đang bật.",
            ParameterDataType.Text, "0 2 * * *"),

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
            null, ParameterDataType.Password, "", IsSecret: true),

        // ---- Phase 15: ứng dụng di động ----
        new("CIRCULATION.SELF_CHECKOUT_VERIFY_MODE", "CIRCULATION", "Cấu hình lưu thông",
            "Cách xác thực vị trí khi mượn tự phục vụ",
            "NONE: không kiểm; WIFI_SSID: thiết bị phải nối Wi-Fi thư viện; QR_STATION: phải quét mã QR trạm mượn dán tại kho.",
            ParameterDataType.Text, "NONE"),
        new("CIRCULATION.SELF_CHECKOUT_QR_TTL_MINUTES", "CIRCULATION", "Cấu hình lưu thông",
            "Phiếu xác thực vị trí có hiệu lực (phút)",
            "Sau khi quét mã trạm hoặc kiểm Wi-Fi, bạn đọc có bấy nhiêu phút để quét sách.",
            ParameterDataType.Number, "15"),
        new("DIGITAL.OFFLINE_PACKAGE_DAYS", "DIGITAL", "Cấu hình tài liệu số",
            "Gói tài liệu tải về đọc ngoại tuyến dùng được (ngày)",
            "Hết hạn thì ứng dụng tự xoá và máy chủ không phát tệp nữa.",
            ParameterDataType.Number, "7"),
        new("MOBILE.APP_MIN_VERSION", "MOBILE", "Cấu hình ứng dụng di động", "Phiên bản ứng dụng thấp nhất còn dùng được",
            "Ứng dụng thấp hơn phiên bản này bị chặn và yêu cầu cập nhật.", ParameterDataType.Text, "1.0.0"),
        new("MOBILE.APP_LATEST_VERSION", "MOBILE", "Cấu hình ứng dụng di động", "Phiên bản ứng dụng mới nhất",
            null, ParameterDataType.Text, "1.0.0"),
        new("MOBILE.APP_UPDATE_URL_ANDROID", "MOBILE", "Cấu hình ứng dụng di động", "Địa chỉ tải bản Android",
            "Liên kết Google Play hoặc tệp APK.", ParameterDataType.Text, ""),
        new("MOBILE.APP_UPDATE_URL_IOS", "MOBILE", "Cấu hình ứng dụng di động", "Địa chỉ tải bản iOS",
            "Liên kết App Store hoặc TestFlight.", ParameterDataType.Text, ""),
        new("MOBILE.APP_FORCE_UPDATE", "MOBILE", "Cấu hình ứng dụng di động", "Bắt buộc cập nhật",
            "Bật thì ứng dụng cũ hơn phiên bản mới nhất cũng bị nhắc cập nhật ngay.", ParameterDataType.Boolean, "false"),
        new("MOBILE.NEWS_PUSH_ENABLED", "MOBILE", "Cấu hình ứng dụng di động", "Đẩy thông báo khi đăng tin mới",
            "Tin đăng lên trang thư viện thì gửi thông báo đẩy tới bạn đọc chưa tắt loại 'Tin tức mới'.",
            ParameterDataType.Boolean, "true")
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
