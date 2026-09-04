namespace LibraryConnect.Application.Features.Acquisition;

/// <summary>
/// Các loại biểu mẫu in dùng chung một trình thiết kế (III.6).
///
/// Mã loại nằm ở đây chứ không rải trong mã nguồn, vì màn hình thiết kế, bộ dựng dữ liệu và endpoint
/// in đều phải nói cùng một thứ tiếng; một chuỗi gõ lệch là biểu mẫu không bao giờ được tìm thấy.
/// </summary>
public static class FormTypes
{
    /// <summary>Phiếu nhập kho.</summary>
    public const string GoodsReceipt = "RECEIPT";
    /// <summary>Biên bản bàn giao.</summary>
    public const string Handover = "HANDOVER";
    /// <summary>Phiếu chuyển kho.</summary>
    public const string Transfer = "TRANSFER";
    /// <summary>Biên bản kiểm kê.</summary>
    public const string Inventory = "INVENTORY";
    /// <summary>Quyết định thanh lý.</summary>
    public const string Disposal = "DISPOSAL";
    /// <summary>Đơn đặt hàng gửi nhà cung cấp.</summary>
    public const string PurchaseOrder = "ORDER";
    /// <summary>Phiếu mượn.</summary>
    public const string LoanSlip = "LOAN_SLIP";
    /// <summary>Phiếu trả.</summary>
    public const string ReturnSlip = "RETURN_SLIP";
    /// <summary>Biên lai thu tiền phạt.</summary>
    public const string FineReceipt = "FINE_RECEIPT";
    /// <summary>Giấy xác nhận đã trả hết tài liệu, cấp cho sinh viên ra trường.</summary>
    public const string Clearance = "CLEARANCE";

    /// <summary>Phiếu khiếu nại số báo thiếu, gửi nhà cung cấp (IV.3).</summary>
    public const string SerialClaim = "SERIAL_CLAIM";

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [GoodsReceipt] = "Phiếu nhập kho",
        [Handover] = "Biên bản bàn giao",
        [Transfer] = "Phiếu chuyển kho",
        [Inventory] = "Biên bản kiểm kê",
        [Disposal] = "Quyết định thanh lý",
        [PurchaseOrder] = "Đơn đặt hàng",
        [LoanSlip] = "Phiếu mượn",
        [ReturnSlip] = "Phiếu trả",
        [FineReceipt] = "Biên lai thu tiền phạt",
        [Clearance] = "Giấy xác nhận trả sách",
        [SerialClaim] = "Phiếu khiếu nại số báo thiếu"
    };

    /// <summary>
    /// Quyền cần có để in từng loại mẫu. Trước đây cả mười loại đều đòi "In đơn đặt hàng" của phân hệ
    /// Bổ sung, nên cán bộ quầy không in được phiếu mượn của chính mình. Quyền đi theo nghiệp vụ sinh
    /// ra chứng từ: ai được ghi trả thì in được phiếu mượn/trả, ai thu phạt thì in biên lai, ai xem hồ
    /// sơ thì in giấy xác nhận trả sách.
    /// </summary>
    public static IReadOnlyList<string> PermissionsToPrint(string formType) => formType.ToUpperInvariant() switch
    {
        LoanSlip or ReturnSlip => new[]
        {
            Common.Security.PermissionCodes.CirculationLoanReturn,
            Common.Security.PermissionCodes.CirculationLoanView,
        },
        FineReceipt => new[]
        {
            Common.Security.PermissionCodes.CirculationFineCollect,
            Common.Security.PermissionCodes.CirculationFineView,
        },
        Clearance => new[] { Common.Security.PermissionCodes.ReaderView },
        SerialClaim => new[]
        {
            Common.Security.PermissionCodes.SerialClaim,
            Common.Security.PermissionCodes.SerialView,
        },
        Transfer => new[] { Common.Security.PermissionCodes.AcqItemMove, Common.Security.PermissionCodes.AcqItemView },
        Disposal => new[] { Common.Security.PermissionCodes.AcqItemDispose, Common.Security.PermissionCodes.AcqItemView },
        Inventory => new[] { Common.Security.PermissionCodes.AcqInventoryReport, Common.Security.PermissionCodes.AcqInventoryView },
        _ => new[] { Common.Security.PermissionCodes.AcqOrderPrint },
    };

    /// <summary>Mọi quyền có thể in một mẫu nào đó — cổng thô ở controller, cổng tinh ở handler.</summary>
    public static IReadOnlyList<string> AnyPrintPermission { get; } = new[]
    {
        Common.Security.PermissionCodes.AcqOrderPrint,
        Common.Security.PermissionCodes.CirculationLoanReturn,
        Common.Security.PermissionCodes.CirculationLoanView,
        Common.Security.PermissionCodes.CirculationFineCollect,
        Common.Security.PermissionCodes.CirculationFineView,
        Common.Security.PermissionCodes.ReaderView,
        Common.Security.PermissionCodes.SerialClaim,
        Common.Security.PermissionCodes.SerialView,
        Common.Security.PermissionCodes.AcqItemMove,
        Common.Security.PermissionCodes.AcqItemView,
        Common.Security.PermissionCodes.AcqItemDispose,
        Common.Security.PermissionCodes.AcqInventoryReport,
        Common.Security.PermissionCodes.AcqInventoryView,
    };
}

/// <summary>Một dòng thông tin ở phần đầu biểu mẫu, ví dụ "Bên giao: Công ty ...".</summary>
public class FormFieldDto
{
    public string Label { get; set; } = string.Empty;
    /// <summary>Khóa dữ liệu, xem <see cref="FormFieldCatalog"/>.</summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>Chiếm cả chiều rộng thay vì hai cột.</summary>
    public bool FullWidth { get; set; }
}

/// <summary>Một cột của bảng chi tiết trên biểu mẫu.</summary>
public class FormColumnDto
{
    public string Header { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    /// <summary>Trọng số chiều rộng so với các cột khác.</summary>
    public double Width { get; set; } = 1;
    /// <summary>left | center | right</summary>
    public string Align { get; set; } = "left";
    /// <summary>Cộng tổng cột này ở dòng cuối bảng.</summary>
    public bool Sum { get; set; }
}

/// <summary>Một ô ký tên ở cuối biểu mẫu.</summary>
public class FormSignatureDto
{
    public string Role { get; set; } = string.Empty;
    /// <summary>Dòng nhỏ dưới chức danh, thường là "(Ký, ghi rõ họ tên)".</summary>
    public string? Note { get; set; }
}

/// <summary>Bố cục một biểu mẫu in.</summary>
public class FormLayoutDto
{
    /// <summary>In logo thư viện ở góc trên bên trái.</summary>
    public bool ShowLogo { get; set; }
    /// <summary>Các dòng tên cơ quan ở góc trên bên trái, có thể chèn ô thay thế.</summary>
    public List<string> OrganisationLines { get; set; } = new();
    /// <summary>In quốc hiệu và tiêu ngữ ở góc trên bên phải — bắt buộc với văn bản hành chính.</summary>
    public bool ShowNationalHeading { get; set; } = true;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    /// <summary>Các câu dẫn trước phần thông tin, ví dụ "Hôm nay, ngày ..., chúng tôi gồm:".</summary>
    public List<string> IntroLines { get; set; } = new();
    public List<FormFieldDto> Fields { get; set; } = new();
    public List<FormColumnDto> Columns { get; set; } = new();
    /// <summary>In dòng tổng cộng cuối bảng cho các cột đã bật cộng tổng.</summary>
    public bool ShowTotals { get; set; } = true;
    public List<string> ClosingLines { get; set; } = new();
    public List<FormSignatureDto> Signatures { get; set; } = new();
    public string? Footer { get; set; }
    /// <summary>Cỡ chữ cơ sở của biểu mẫu.</summary>
    public double FontSize { get; set; } = 10;
}

public class FormTemplateDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FormType { get; set; } = string.Empty;
    public string FormTypeName { get; set; } = string.Empty;
    /// <summary>A4 | A5</summary>
    public string PaperSize { get; set; } = "A4";
    public bool IsLandscape { get; set; }
    public double? CustomWidthMm { get; set; }
    public double? CustomHeightMm { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public FormLayoutDto Layout { get; set; } = new();
}

/// <summary>
/// Dữ liệu đổ vào một biểu mẫu.
///
/// Cố tình để dạng từ điển chứ không phải một lớp cho mỗi loại chứng từ: trình thiết kế cho phép
/// người dùng chọn trường theo tên, nên bộ kết xuất phải tra được theo tên chứ không theo thuộc tính
/// biên dịch sẵn.
/// </summary>
public class FormDataDto
{
    public Dictionary<string, string> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<Dictionary<string, string>> Rows { get; set; } = new();
}

/// <summary>Các trường có thể chọn trên trình thiết kế, theo từng loại biểu mẫu.</summary>
public record FormFieldOption(string Key, string Label, bool IsRow = false);

public static class FormFieldCatalog
{
    /// <summary>Trường có ở mọi biểu mẫu, lấy từ tham số hệ thống và từ thời điểm in.</summary>
    private static readonly FormFieldOption[] Common =
    {
        new("libraryName", "Tên thư viện"),
        new("libraryAddress", "Địa chỉ thư viện"),
        new("libraryPhone", "Điện thoại thư viện"),
        new("printedAt", "Ngày in"),
        new("printedBy", "Người in"),
        new("day", "Ngày (số)"),
        new("month", "Tháng (số)"),
        new("year", "Năm (số)")
    };

    private static readonly Dictionary<string, FormFieldOption[]> ByType = new(StringComparer.OrdinalIgnoreCase)
    {
        [FormTypes.Handover] = new[]
        {
            new FormFieldOption("code", "Số biên bản"),
            new FormFieldOption("handoverDate", "Ngày bàn giao"),
            new FormFieldOption("partyA", "Bên giao"),
            new FormFieldOption("partyB", "Bên nhận"),
            new FormFieldOption("orderCode", "Mã đơn đặt"),
            new FormFieldOption("supplierName", "Nhà cung cấp"),
            new FormFieldOption("contractNo", "Số hợp đồng"),
            new FormFieldOption("content", "Nội dung"),
            new FormFieldOption("totalItems", "Tổng số bản"),
            new FormFieldOption("totalAmount", "Tổng giá trị"),
            new FormFieldOption("note", "Ghi chú"),
            new FormFieldOption("index", "Số thứ tự", IsRow: true),
            new FormFieldOption("title", "Nhan đề", IsRow: true),
            new FormFieldOption("author", "Tác giả", IsRow: true),
            new FormFieldOption("isbn", "ISBN", IsRow: true),
            new FormFieldOption("quantity", "Số lượng", IsRow: true),
            new FormFieldOption("unitPrice", "Đơn giá", IsRow: true),
            new FormFieldOption("amount", "Thành tiền", IsRow: true),
            new FormFieldOption("condition", "Tình trạng", IsRow: true),
            new FormFieldOption("note", "Ghi chú dòng", IsRow: true)
        },
        [FormTypes.PurchaseOrder] = new[]
        {
            new FormFieldOption("code", "Mã đơn đặt"),
            new FormFieldOption("orderDate", "Ngày đặt"),
            new FormFieldOption("expectedDate", "Ngày dự kiến giao"),
            new FormFieldOption("supplierName", "Nhà cung cấp"),
            new FormFieldOption("supplierAddress", "Địa chỉ nhà cung cấp"),
            new FormFieldOption("supplierPhone", "Điện thoại nhà cung cấp"),
            new FormFieldOption("supplierTaxCode", "Mã số thuế"),
            new FormFieldOption("contractNo", "Số hợp đồng"),
            new FormFieldOption("fundingSourceName", "Nguồn kinh phí"),
            new FormFieldOption("totalAmount", "Tổng giá trị"),
            new FormFieldOption("note", "Ghi chú"),
            new FormFieldOption("index", "Số thứ tự", IsRow: true),
            new FormFieldOption("title", "Nhan đề", IsRow: true),
            new FormFieldOption("author", "Tác giả", IsRow: true),
            new FormFieldOption("isbn", "ISBN", IsRow: true),
            new FormFieldOption("quantity", "Số lượng", IsRow: true),
            new FormFieldOption("unitPrice", "Đơn giá", IsRow: true),
            new FormFieldOption("amount", "Thành tiền", IsRow: true),
            new FormFieldOption("note", "Ghi chú dòng", IsRow: true)
        },
        [FormTypes.LoanSlip] = new[]
        {
            new FormFieldOption("code", "Số phiếu mượn"),
            new FormFieldOption("loanDate", "Ngày mượn"),
            new FormFieldOption("dueDate", "Hạn trả"),
            new FormFieldOption("readerName", "Họ tên bạn đọc"),
            new FormFieldOption("cardNumber", "Số thẻ"),
            new FormFieldOption("studentCode", "Mã sinh viên"),
            new FormFieldOption("readerType", "Loại bạn đọc"),
            new FormFieldOption("faculty", "Khoa"),
            new FormFieldOption("className", "Lớp"),
            new FormFieldOption("totalItems", "Số tài liệu"),
            new FormFieldOption("staffName", "Cán bộ ghi mượn"),
            new FormFieldOption("index", "Số thứ tự", IsRow: true),
            new FormFieldOption("barcode", "Mã vạch", IsRow: true),
            new FormFieldOption("registerNumber", "Số ĐKCB", IsRow: true),
            new FormFieldOption("title", "Nhan đề", IsRow: true),
            new FormFieldOption("author", "Tác giả", IsRow: true),
            new FormFieldOption("callNumber", "Ký hiệu xếp giá", IsRow: true),
            new FormFieldOption("dueDate", "Hạn trả", IsRow: true),
            new FormFieldOption("price", "Giá", IsRow: true)
        },

        [FormTypes.ReturnSlip] = new[]
        {
            new FormFieldOption("code", "Số phiếu trả"),
            new FormFieldOption("returnDate", "Ngày trả"),
            new FormFieldOption("readerName", "Họ tên bạn đọc"),
            new FormFieldOption("cardNumber", "Số thẻ"),
            new FormFieldOption("studentCode", "Mã sinh viên"),
            new FormFieldOption("faculty", "Khoa"),
            new FormFieldOption("className", "Lớp"),
            new FormFieldOption("totalItems", "Số tài liệu"),
            new FormFieldOption("totalFine", "Tổng tiền phạt"),
            new FormFieldOption("staffName", "Cán bộ ghi trả"),
            new FormFieldOption("index", "Số thứ tự", IsRow: true),
            new FormFieldOption("barcode", "Mã vạch", IsRow: true),
            new FormFieldOption("title", "Nhan đề", IsRow: true),
            new FormFieldOption("dueDate", "Hạn trả", IsRow: true),
            new FormFieldOption("returnDate", "Ngày trả", IsRow: true),
            new FormFieldOption("overdueDays", "Số ngày quá hạn", IsRow: true),
            new FormFieldOption("fine", "Tiền phạt", IsRow: true)
        },

        [FormTypes.FineReceipt] = new[]
        {
            new FormFieldOption("code", "Số biên lai"),
            new FormFieldOption("paidAt", "Ngày thu"),
            new FormFieldOption("readerName", "Họ tên bạn đọc"),
            new FormFieldOption("cardNumber", "Số thẻ"),
            new FormFieldOption("studentCode", "Mã sinh viên"),
            new FormFieldOption("faculty", "Khoa"),
            new FormFieldOption("className", "Lớp"),
            new FormFieldOption("fineType", "Loại phạt"),
            new FormFieldOption("amount", "Số tiền phạt"),
            new FormFieldOption("paidAmount", "Số tiền đã thu"),
            new FormFieldOption("outstanding", "Còn nợ"),
            new FormFieldOption("amountInWords", "Số tiền bằng chữ"),
            new FormFieldOption("reason", "Lý do phạt"),
            new FormFieldOption("staffName", "Cán bộ thu tiền"),
            new FormFieldOption("index", "Số thứ tự", IsRow: true),
            new FormFieldOption("title", "Nhan đề", IsRow: true),
            new FormFieldOption("barcode", "Mã vạch", IsRow: true),
            new FormFieldOption("fine", "Số tiền", IsRow: true)
        },

        [FormTypes.Clearance] = new[]
        {
            new FormFieldOption("readerName", "Họ tên bạn đọc"),
            new FormFieldOption("cardNumber", "Số thẻ"),
            new FormFieldOption("studentCode", "Mã sinh viên"),
            new FormFieldOption("dateOfBirth", "Ngày sinh"),
            new FormFieldOption("readerType", "Loại bạn đọc"),
            new FormFieldOption("faculty", "Khoa"),
            new FormFieldOption("className", "Lớp"),
            new FormFieldOption("courseYear", "Khóa"),
            new FormFieldOption("totalLoans", "Tổng lượt đã mượn"),
            new FormFieldOption("outstandingLoans", "Tài liệu chưa trả"),
            new FormFieldOption("outstandingFines", "Tiền phạt còn nợ"),
            new FormFieldOption("conclusion", "Kết luận"),
            new FormFieldOption("staffName", "Cán bộ xác nhận"),
            new FormFieldOption("index", "Số thứ tự", IsRow: true),
            new FormFieldOption("barcode", "Mã vạch", IsRow: true),
            new FormFieldOption("title", "Nhan đề", IsRow: true),
            new FormFieldOption("dueDate", "Hạn trả", IsRow: true)
        },

        [FormTypes.SerialClaim] = new[]
        {
            new FormFieldOption("claimNo", "Số phiếu khiếu nại"),
            new FormFieldOption("claimDate", "Ngày lập phiếu"),
            new FormFieldOption("supplierName", "Nhà cung cấp"),
            new FormFieldOption("supplierAddress", "Địa chỉ nhà cung cấp"),
            new FormFieldOption("supplierPhone", "Điện thoại nhà cung cấp"),
            new FormFieldOption("serialTitle", "Tên báo, tạp chí"),
            new FormFieldOption("issn", "ISSN"),
            new FormFieldOption("content", "Nội dung khiếu nại"),
            new FormFieldOption("staffName", "Cán bộ lập phiếu"),
            new FormFieldOption("index", "Số thứ tự", IsRow: true),
            new FormFieldOption("issueCaption", "Số báo", IsRow: true),
            new FormFieldOption("expectedDate", "Ngày dự kiến", IsRow: true),
            new FormFieldOption("status", "Tình trạng", IsRow: true)
        },

        [FormTypes.Transfer] = new[]
        {
            new FormFieldOption("code", "Số phiếu chuyển kho"),
            new FormFieldOption("movementDate", "Ngày chuyển"),
            new FormFieldOption("fromWarehouse", "Kho chuyển đi"),
            new FormFieldOption("toWarehouse", "Kho nhận"),
            new FormFieldOption("reason", "Lý do chuyển"),
            new FormFieldOption("decisionNo", "Số quyết định"),
            new FormFieldOption("performedBy", "Người thực hiện"),
            new FormFieldOption("totalItems", "Tổng số bản"),
            new FormFieldOption("totalAmount", "Tổng giá trị"),
            new FormFieldOption("index", "Số thứ tự", IsRow: true),
            new FormFieldOption("barcode", "Mã vạch", IsRow: true),
            new FormFieldOption("registerNumber", "Số ĐKCB", IsRow: true),
            new FormFieldOption("title", "Nhan đề", IsRow: true),
            new FormFieldOption("author", "Tác giả", IsRow: true),
            new FormFieldOption("callNumber", "Ký hiệu xếp giá", IsRow: true),
            new FormFieldOption("price", "Giá", IsRow: true),
            new FormFieldOption("condition", "Tình trạng", IsRow: true)
        },
        [FormTypes.Disposal] = new[]
        {
            new FormFieldOption("decisionNo", "Số quyết định"),
            new FormFieldOption("disposalDate", "Ngày quyết định"),
            new FormFieldOption("disposalType", "Hình thức"),
            new FormFieldOption("reason", "Lý do"),
            new FormFieldOption("approvedBy", "Người duyệt"),
            new FormFieldOption("totalItems", "Tổng số bản"),
            new FormFieldOption("totalAmount", "Tổng giá trị"),
            new FormFieldOption("index", "Số thứ tự", IsRow: true),
            new FormFieldOption("barcode", "Mã vạch", IsRow: true),
            new FormFieldOption("registerNumber", "Số ĐKCB", IsRow: true),
            new FormFieldOption("title", "Nhan đề", IsRow: true),
            new FormFieldOption("callNumber", "Ký hiệu xếp giá", IsRow: true),
            new FormFieldOption("price", "Giá", IsRow: true),
            new FormFieldOption("warehouseName", "Kho", IsRow: true)
        },
        [FormTypes.Inventory] = new[]
        {
            new FormFieldOption("code", "Mã kỳ kiểm kê"),
            new FormFieldOption("name", "Tên kỳ kiểm kê"),
            new FormFieldOption("warehouseName", "Kho kiểm kê"),
            new FormFieldOption("startDate", "Ngày bắt đầu"),
            new FormFieldOption("endDate", "Ngày kết thúc"),
            new FormFieldOption("assignedStaff", "Cán bộ kiểm kê"),
            new FormFieldOption("expectedCount", "Số bản theo sổ"),
            new FormFieldOption("matchCount", "Số bản khớp"),
            new FormFieldOption("missingCount", "Số bản thiếu"),
            new FormFieldOption("unexpectedCount", "Số bản thừa"),
            new FormFieldOption("wrongWarehouseCount", "Số bản sai kho"),
            new FormFieldOption("index", "Số thứ tự", IsRow: true),
            new FormFieldOption("barcode", "Mã vạch", IsRow: true),
            new FormFieldOption("registerNumber", "Số ĐKCB", IsRow: true),
            new FormFieldOption("title", "Nhan đề", IsRow: true),
            new FormFieldOption("result", "Kết quả", IsRow: true),
            new FormFieldOption("price", "Giá", IsRow: true),
            new FormFieldOption("note", "Ghi chú dòng", IsRow: true)
        },
        [FormTypes.GoodsReceipt] = new[]
        {
            new FormFieldOption("code", "Số phiếu nhập"),
            new FormFieldOption("receiptDate", "Ngày nhập"),
            new FormFieldOption("orderCode", "Mã đơn đặt"),
            new FormFieldOption("supplierName", "Nhà cung cấp"),
            new FormFieldOption("warehouseName", "Kho nhập"),
            new FormFieldOption("fundingSourceName", "Nguồn kinh phí"),
            new FormFieldOption("totalItems", "Tổng số bản"),
            new FormFieldOption("totalAmount", "Tổng giá trị"),
            new FormFieldOption("index", "Số thứ tự", IsRow: true),
            new FormFieldOption("barcode", "Mã vạch", IsRow: true),
            new FormFieldOption("registerNumber", "Số ĐKCB", IsRow: true),
            new FormFieldOption("title", "Nhan đề", IsRow: true),
            new FormFieldOption("author", "Tác giả", IsRow: true),
            new FormFieldOption("callNumber", "Ký hiệu xếp giá", IsRow: true),
            new FormFieldOption("price", "Giá", IsRow: true)
        }
    };

    /// <summary>Trường dùng được ở phần đầu biểu mẫu.</summary>
    public static IReadOnlyList<FormFieldOption> HeaderFields(string formType) =>
        Common.Concat(Fields(formType).Where(field => !field.IsRow)).ToList();

    /// <summary>Trường dùng được làm cột của bảng chi tiết.</summary>
    public static IReadOnlyList<FormFieldOption> RowFields(string formType) =>
        Fields(formType).Where(field => field.IsRow).ToList();

    private static IReadOnlyList<FormFieldOption> Fields(string formType) =>
        ByType.TryGetValue(formType, out var fields) ? fields : Array.Empty<FormFieldOption>();
}
