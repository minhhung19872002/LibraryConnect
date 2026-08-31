namespace LibraryConnect.Application.Common.Security;

/// <summary>
/// Every permission code in the product, shaped MODULE.ENTITY.ACTION. Endpoints reference these
/// constants through <c>[RequirePermission]</c>; the seeder turns <see cref="All"/> into rows in
/// sys.permissions and builds the permission tree the UI renders.
/// </summary>
public static class PermissionCodes
{
    // ---------------- Phân hệ I — Quản trị hệ thống ----------------
    public const string SystemGroupView = "SYSTEM.GROUP.VIEW";
    public const string SystemGroupCreate = "SYSTEM.GROUP.CREATE";
    public const string SystemGroupUpdate = "SYSTEM.GROUP.UPDATE";
    public const string SystemGroupDelete = "SYSTEM.GROUP.DELETE";
    public const string SystemGroupPermission = "SYSTEM.GROUP.PERMISSION";

    public const string SystemUserView = "SYSTEM.USER.VIEW";
    public const string SystemUserCreate = "SYSTEM.USER.CREATE";
    public const string SystemUserUpdate = "SYSTEM.USER.UPDATE";
    public const string SystemUserDelete = "SYSTEM.USER.DELETE";
    public const string SystemUserResetPassword = "SYSTEM.USER.RESET_PASSWORD";
    public const string SystemUserLock = "SYSTEM.USER.LOCK";
    public const string SystemUserImport = "SYSTEM.USER.IMPORT";

    public const string SystemParameterView = "SYSTEM.PARAMETER.VIEW";
    public const string SystemParameterUpdate = "SYSTEM.PARAMETER.UPDATE";

    public const string SystemAuditView = "SYSTEM.AUDIT.VIEW";
    public const string SystemAuditExport = "SYSTEM.AUDIT.EXPORT";
    public const string SystemAuditSetting = "SYSTEM.AUDIT.SETTING";

    public const string SystemBackupView = "SYSTEM.BACKUP.VIEW";
    public const string SystemBackupCreate = "SYSTEM.BACKUP.CREATE";
    public const string SystemBackupRestore = "SYSTEM.BACKUP.RESTORE";
    public const string SystemBackupDelete = "SYSTEM.BACKUP.DELETE";
    public const string SystemBackupSetting = "SYSTEM.BACKUP.SETTING";

    public const string SystemJobView = "SYSTEM.JOB.VIEW";
    public const string SystemJobManage = "SYSTEM.JOB.MANAGE";

    // ---------------- Danh mục ----------------
    public const string CatalogListView = "CATALOG.LIST.VIEW";
    public const string CatalogListCreate = "CATALOG.LIST.CREATE";
    public const string CatalogListUpdate = "CATALOG.LIST.UPDATE";
    public const string CatalogListDelete = "CATALOG.LIST.DELETE";
    public const string CatalogListImport = "CATALOG.LIST.IMPORT";
    public const string CatalogListExport = "CATALOG.LIST.EXPORT";
    public const string CatalogListMerge = "CATALOG.LIST.MERGE";
    public const string CatalogCustomIndexManage = "CATALOG.CUSTOM_INDEX.MANAGE";

    // ---------------- Phân hệ II — Biên mục ----------------
    public const string CatalogBibView = "CATALOG.BIB.VIEW";
    public const string CatalogBibCreate = "CATALOG.BIB.CREATE";
    public const string CatalogBibUpdate = "CATALOG.BIB.UPDATE";
    public const string CatalogBibDelete = "CATALOG.BIB.DELETE";
    public const string CatalogBibApprove = "CATALOG.BIB.APPROVE";
    public const string CatalogBibExport = "CATALOG.BIB.EXPORT";
    public const string CatalogBibImport = "CATALOG.BIB.IMPORT";
    public const string CatalogBibVersionRestore = "CATALOG.BIB.VERSION_RESTORE";

    public const string CatalogMarcDefinitionView = "CATALOG.MARC_DEFINITION.VIEW";
    public const string CatalogMarcDefinitionManage = "CATALOG.MARC_DEFINITION.MANAGE";
    public const string CatalogTemplateView = "CATALOG.TEMPLATE.VIEW";
    public const string CatalogTemplateManage = "CATALOG.TEMPLATE.MANAGE";
    public const string CatalogDefaultValueManage = "CATALOG.DEFAULT_VALUE.MANAGE";

    public const string CatalogQueueView = "CATALOG.QUEUE.VIEW";
    public const string CatalogQueueAssign = "CATALOG.QUEUE.ASSIGN";
    public const string CatalogQueueProcess = "CATALOG.QUEUE.PROCESS";
    public const string CatalogQueueApprove = "CATALOG.QUEUE.APPROVE";

    public const string CatalogCardTemplateManage = "CATALOG.CARD_TEMPLATE.MANAGE";
    public const string CatalogCardPrint = "CATALOG.CARD.PRINT";

    public const string CatalogZ3950Search = "CATALOG.Z3950.SEARCH";
    public const string CatalogZ3950TargetManage = "CATALOG.Z3950.TARGET_MANAGE";
    public const string CatalogOaiManage = "CATALOG.OAI.MANAGE";
    public const string CatalogOaiHarvest = "CATALOG.OAI.HARVEST";

    // ---------------- Phân hệ III — Bổ sung & Kho ----------------
    public const string AcqRequestView = "ACQ.REQUEST.VIEW";
    public const string AcqRequestCreate = "ACQ.REQUEST.CREATE";
    public const string AcqRequestUpdate = "ACQ.REQUEST.UPDATE";
    public const string AcqRequestDelete = "ACQ.REQUEST.DELETE";
    public const string AcqRequestSubmit = "ACQ.REQUEST.SUBMIT";
    public const string AcqRequestApprove = "ACQ.REQUEST.APPROVE";
    public const string AcqRequestImport = "ACQ.REQUEST.IMPORT";

    public const string AcqOrderView = "ACQ.ORDER.VIEW";
    public const string AcqOrderCreate = "ACQ.ORDER.CREATE";
    public const string AcqOrderUpdate = "ACQ.ORDER.UPDATE";
    public const string AcqOrderDelete = "ACQ.ORDER.DELETE";
    public const string AcqOrderApprove = "ACQ.ORDER.APPROVE";
    public const string AcqOrderReceive = "ACQ.ORDER.RECEIVE";
    public const string AcqOrderPrint = "ACQ.ORDER.PRINT";

    public const string AcqHandoverView = "ACQ.HANDOVER.VIEW";
    public const string AcqHandoverManage = "ACQ.HANDOVER.MANAGE";

    public const string AcqItemView = "ACQ.ITEM.VIEW";
    public const string AcqItemCreate = "ACQ.ITEM.CREATE";
    public const string AcqItemUpdate = "ACQ.ITEM.UPDATE";
    public const string AcqItemDelete = "ACQ.ITEM.DELETE";
    public const string AcqItemInspect = "ACQ.ITEM.INSPECT";
    public const string AcqItemLock = "ACQ.ITEM.LOCK";
    public const string AcqItemMove = "ACQ.ITEM.MOVE";
    public const string AcqItemDispose = "ACQ.ITEM.DISPOSE";
    public const string AcqItemPrintBarcode = "ACQ.ITEM.PRINT_BARCODE";
    public const string AcqItemPrintLabel = "ACQ.ITEM.PRINT_LABEL";
    public const string AcqItemExport = "ACQ.ITEM.EXPORT";

    public const string AcqWarehouseView = "ACQ.WAREHOUSE.VIEW";
    public const string AcqWarehouseManage = "ACQ.WAREHOUSE.MANAGE";
    public const string AcqLibraryManage = "ACQ.LIBRARY.MANAGE";

    public const string AcqInventoryView = "ACQ.INVENTORY.VIEW";
    public const string AcqInventoryCreate = "ACQ.INVENTORY.CREATE";
    public const string AcqInventoryScan = "ACQ.INVENTORY.SCAN";
    public const string AcqInventoryClose = "ACQ.INVENTORY.CLOSE";
    public const string AcqInventoryReport = "ACQ.INVENTORY.REPORT";

    public const string AcqFormTemplateManage = "ACQ.FORM_TEMPLATE.MANAGE";
    public const string AcqReportView = "ACQ.REPORT.VIEW";
    public const string AcqReportExport = "ACQ.REPORT.EXPORT";

    // ---------------- Phân hệ IV — Ấn phẩm định kỳ ----------------
    public const string SerialView = "SERIAL.TITLE.VIEW";
    public const string SerialCreate = "SERIAL.TITLE.CREATE";
    public const string SerialUpdate = "SERIAL.TITLE.UPDATE";
    public const string SerialDelete = "SERIAL.TITLE.DELETE";
    public const string SerialPredict = "SERIAL.ISSUE.PREDICT";
    public const string SerialReceive = "SERIAL.ISSUE.RECEIVE";
    public const string SerialClaim = "SERIAL.ISSUE.CLAIM";
    public const string SerialBind = "SERIAL.ISSUE.BIND";
    public const string SerialArticleManage = "SERIAL.ARTICLE.MANAGE";
    public const string SerialReportView = "SERIAL.REPORT.VIEW";

    // ---------------- Phân hệ V — Tài liệu số ----------------
    public const string DigitalView = "DIGITAL.DOCUMENT.VIEW";
    public const string DigitalUpload = "DIGITAL.DOCUMENT.UPLOAD";
    public const string DigitalUpdate = "DIGITAL.DOCUMENT.UPDATE";
    public const string DigitalDelete = "DIGITAL.DOCUMENT.DELETE";
    public const string DigitalDownload = "DIGITAL.DOCUMENT.DOWNLOAD";
    public const string DigitalCollectionManage = "DIGITAL.COLLECTION.MANAGE";
    public const string DigitalRequestView = "DIGITAL.REQUEST.VIEW";
    public const string DigitalRequestApprove = "DIGITAL.REQUEST.APPROVE";
    public const string DigitalImport = "DIGITAL.DOCUMENT.IMPORT";
    public const string DigitalExport = "DIGITAL.DOCUMENT.EXPORT";
    public const string DigitalReportView = "DIGITAL.REPORT.VIEW";
    public const string DigitalAccessLogView = "DIGITAL.ACCESS_LOG.VIEW";

    // ---------------- Phân hệ VI — Bạn đọc ----------------
    public const string ReaderView = "READER.PROFILE.VIEW";
    public const string ReaderCreate = "READER.PROFILE.CREATE";
    public const string ReaderUpdate = "READER.PROFILE.UPDATE";
    public const string ReaderDelete = "READER.PROFILE.DELETE";
    public const string ReaderLock = "READER.PROFILE.LOCK";
    public const string ReaderExtendCard = "READER.CARD.EXTEND";
    public const string ReaderPrintCard = "READER.CARD.PRINT";
    public const string ReaderCardTemplateManage = "READER.CARD_TEMPLATE.MANAGE";
    public const string ReaderImport = "READER.PROFILE.IMPORT";
    public const string ReaderExport = "READER.PROFILE.EXPORT";
    public const string ReaderResetPassword = "READER.PROFILE.RESET_PASSWORD";
    public const string ReaderViolationManage = "READER.VIOLATION.MANAGE";
    public const string ReaderReportView = "READER.REPORT.VIEW";

    // ---------------- Phân hệ VII — Lưu thông ----------------
    public const string CirculationPolicyView = "CIRCULATION.POLICY.VIEW";
    public const string CirculationPolicyManage = "CIRCULATION.POLICY.MANAGE";
    public const string CirculationLoanCreate = "CIRCULATION.LOAN.CREATE";
    public const string CirculationLoanReturn = "CIRCULATION.LOAN.RETURN";
    public const string CirculationLoanRenew = "CIRCULATION.LOAN.RENEW";
    public const string CirculationLoanView = "CIRCULATION.LOAN.VIEW";
    public const string CirculationHoldManage = "CIRCULATION.HOLD.MANAGE";
    public const string CirculationFineView = "CIRCULATION.FINE.VIEW";
    public const string CirculationFineCollect = "CIRCULATION.FINE.COLLECT";
    public const string CirculationFineWaive = "CIRCULATION.FINE.WAIVE";
    public const string CirculationLockerManage = "CIRCULATION.LOCKER.MANAGE";
    public const string CirculationVisitManage = "CIRCULATION.VISIT.MANAGE";
    public const string CirculationTemplateManage = "CIRCULATION.TEMPLATE.MANAGE";
    public const string CirculationReportView = "CIRCULATION.REPORT.VIEW";
    public const string CirculationReportExport = "CIRCULATION.REPORT.EXPORT";

    // ---------------- Phân hệ VIII — Quản trị nội dung ----------------
    public const string CmsPageManage = "CMS.PAGE.MANAGE";
    public const string CmsNewsView = "CMS.NEWS.VIEW";
    public const string CmsNewsManage = "CMS.NEWS.MANAGE";
    public const string CmsNewsPublish = "CMS.NEWS.PUBLISH";
    public const string CmsMenuManage = "CMS.MENU.MANAGE";
    public const string CmsBannerManage = "CMS.BANNER.MANAGE";
    public const string CmsSettingManage = "CMS.SETTING.MANAGE";
    public const string CmsGalleryManage = "CMS.GALLERY.MANAGE";
    public const string CmsLinkManage = "CMS.LINK.MANAGE";
    public const string CmsReviewModerate = "CMS.REVIEW.MODERATE";

    // ---------------- Phân hệ X — Tài liệu môn học ----------------
    public const string CourseMajorManage = "COURSE.MAJOR.MANAGE";
    public const string CourseManage = "COURSE.COURSE.MANAGE";
    public const string CourseDocumentLink = "COURSE.DOCUMENT.LINK";
    public const string CourseReportView = "COURSE.REPORT.VIEW";

    // ---------------- Trao đổi dữ liệu ----------------
    public const string ExchangeImport = "EXCHANGE.DATA.IMPORT";
    public const string ExchangeExport = "EXCHANGE.DATA.EXPORT";
    public const string ExchangeFullExport = "EXCHANGE.DATA.FULL_EXPORT";
    public const string ExchangeApiClientManage = "EXCHANGE.API_CLIENT.MANAGE";

    /// <summary>Metadata for one permission, used both for seeding and to render the permission tree.</summary>
    public readonly record struct Definition(string Code, string Module, string Group, string Name);

    /// <summary>
    /// The complete catalogue. Module and Group are the two upper levels of the tri-state checkbox
    /// tree in the "Gán quyền cho nhóm" screen; Name is the Vietnamese leaf label.
    /// </summary>
    public static IReadOnlyList<Definition> All { get; } = new List<Definition>
    {
        new(SystemGroupView, "Quản trị hệ thống", "Nhóm người dùng", "Xem danh sách nhóm"),
        new(SystemGroupCreate, "Quản trị hệ thống", "Nhóm người dùng", "Thêm nhóm"),
        new(SystemGroupUpdate, "Quản trị hệ thống", "Nhóm người dùng", "Sửa nhóm"),
        new(SystemGroupDelete, "Quản trị hệ thống", "Nhóm người dùng", "Xóa nhóm"),
        new(SystemGroupPermission, "Quản trị hệ thống", "Nhóm người dùng", "Gán quyền cho nhóm"),

        new(SystemUserView, "Quản trị hệ thống", "Người dùng", "Xem danh sách người dùng"),
        new(SystemUserCreate, "Quản trị hệ thống", "Người dùng", "Thêm người dùng"),
        new(SystemUserUpdate, "Quản trị hệ thống", "Người dùng", "Sửa người dùng"),
        new(SystemUserDelete, "Quản trị hệ thống", "Người dùng", "Xóa người dùng"),
        new(SystemUserResetPassword, "Quản trị hệ thống", "Người dùng", "Đặt lại mật khẩu"),
        new(SystemUserLock, "Quản trị hệ thống", "Người dùng", "Khóa / mở khóa tài khoản"),
        new(SystemUserImport, "Quản trị hệ thống", "Người dùng", "Nhập người dùng từ Excel"),

        new(SystemParameterView, "Quản trị hệ thống", "Tham số hệ thống", "Xem tham số"),
        new(SystemParameterUpdate, "Quản trị hệ thống", "Tham số hệ thống", "Sửa tham số"),

        new(SystemAuditView, "Quản trị hệ thống", "Nhật ký", "Tra cứu nhật ký"),
        new(SystemAuditExport, "Quản trị hệ thống", "Nhật ký", "Xuất nhật ký"),
        new(SystemAuditSetting, "Quản trị hệ thống", "Nhật ký", "Cài đặt chế độ ghi nhận"),

        new(SystemBackupView, "Quản trị hệ thống", "Sao lưu", "Xem danh sách bản sao lưu"),
        new(SystemBackupCreate, "Quản trị hệ thống", "Sao lưu", "Sao lưu cơ sở dữ liệu"),
        new(SystemBackupRestore, "Quản trị hệ thống", "Sao lưu", "Phục hồi cơ sở dữ liệu"),
        new(SystemBackupDelete, "Quản trị hệ thống", "Sao lưu", "Xóa bản sao lưu"),
        new(SystemBackupSetting, "Quản trị hệ thống", "Sao lưu", "Cấu hình sao lưu tự động"),

        new(SystemJobView, "Quản trị hệ thống", "Tác vụ nền", "Xem tác vụ nền"),
        new(SystemJobManage, "Quản trị hệ thống", "Tác vụ nền", "Quản lý tác vụ nền"),

        new(CatalogListView, "Danh mục", "Danh mục nghiệp vụ", "Xem danh mục"),
        new(CatalogListCreate, "Danh mục", "Danh mục nghiệp vụ", "Thêm giá trị danh mục"),
        new(CatalogListUpdate, "Danh mục", "Danh mục nghiệp vụ", "Sửa giá trị danh mục"),
        new(CatalogListDelete, "Danh mục", "Danh mục nghiệp vụ", "Xóa giá trị danh mục"),
        new(CatalogListImport, "Danh mục", "Danh mục nghiệp vụ", "Nhập danh mục từ Excel"),
        new(CatalogListExport, "Danh mục", "Danh mục nghiệp vụ", "Xuất danh mục ra Excel"),
        new(CatalogListMerge, "Danh mục", "Danh mục nghiệp vụ", "Gộp giá trị trùng"),
        new(CatalogCustomIndexManage, "Danh mục", "Danh mục tự tạo", "Quản lý danh mục tự tạo từ trường MARC"),

        new(CatalogBibView, "Biên mục", "Biểu ghi", "Xem biểu ghi"),
        new(CatalogBibCreate, "Biên mục", "Biểu ghi", "Thêm biểu ghi"),
        new(CatalogBibUpdate, "Biên mục", "Biểu ghi", "Sửa biểu ghi"),
        new(CatalogBibDelete, "Biên mục", "Biểu ghi", "Xóa biểu ghi"),
        new(CatalogBibApprove, "Biên mục", "Biểu ghi", "Duyệt / xuất bản biểu ghi"),
        new(CatalogBibExport, "Biên mục", "Biểu ghi", "Xuất biểu ghi (ISO 2709 / MARCXML)"),
        new(CatalogBibImport, "Biên mục", "Biểu ghi", "Nhập biểu ghi (ISO 2709 / Excel / MARCXML)"),
        new(CatalogBibVersionRestore, "Biên mục", "Biểu ghi", "Khôi phục phiên bản cũ"),

        new(CatalogMarcDefinitionView, "Biên mục", "Định nghĩa MARC", "Xem định nghĩa trường MARC"),
        new(CatalogMarcDefinitionManage, "Biên mục", "Định nghĩa MARC", "Quản lý định nghĩa trường MARC"),
        new(CatalogTemplateView, "Biên mục", "Mẫu biên mục", "Xem mẫu biên mục"),
        new(CatalogTemplateManage, "Biên mục", "Mẫu biên mục", "Quản lý mẫu biên mục"),
        new(CatalogDefaultValueManage, "Biên mục", "Giá trị ngầm định", "Cài đặt giá trị ngầm định trường MARC"),

        new(CatalogQueueView, "Biên mục", "Hàng đợi biên mục", "Xem hàng đợi"),
        new(CatalogQueueAssign, "Biên mục", "Hàng đợi biên mục", "Phân công biên mục"),
        new(CatalogQueueProcess, "Biên mục", "Hàng đợi biên mục", "Xử lý biểu ghi trong hàng đợi"),
        new(CatalogQueueApprove, "Biên mục", "Hàng đợi biên mục", "Duyệt / trả lại biểu ghi"),

        new(CatalogCardTemplateManage, "Biên mục", "Phích mục lục", "Quản lý mẫu phích"),
        new(CatalogCardPrint, "Biên mục", "Phích mục lục", "In phích"),

        new(CatalogZ3950Search, "Biên mục", "Liên thư viện", "Tra cứu Z39.50 / SRU"),
        new(CatalogZ3950TargetManage, "Biên mục", "Liên thư viện", "Quản lý server Z39.50"),
        new(CatalogOaiManage, "Biên mục", "Liên thư viện", "Quản lý nguồn OAI-PMH"),
        new(CatalogOaiHarvest, "Biên mục", "Liên thư viện", "Chạy thu hoạch OAI-PMH"),

        new(AcqRequestView, "Bổ sung", "Yêu cầu đặt mua", "Xem yêu cầu"),
        new(AcqRequestCreate, "Bổ sung", "Yêu cầu đặt mua", "Tạo yêu cầu"),
        new(AcqRequestUpdate, "Bổ sung", "Yêu cầu đặt mua", "Sửa yêu cầu"),
        new(AcqRequestDelete, "Bổ sung", "Yêu cầu đặt mua", "Xóa yêu cầu"),
        new(AcqRequestSubmit, "Bổ sung", "Yêu cầu đặt mua", "Gửi duyệt"),
        new(AcqRequestApprove, "Bổ sung", "Yêu cầu đặt mua", "Duyệt / từ chối yêu cầu"),
        new(AcqRequestImport, "Bổ sung", "Yêu cầu đặt mua", "Nhập yêu cầu từ Excel"),

        new(AcqOrderView, "Bổ sung", "Đơn đặt", "Xem đơn đặt"),
        new(AcqOrderCreate, "Bổ sung", "Đơn đặt", "Tạo đơn đặt"),
        new(AcqOrderUpdate, "Bổ sung", "Đơn đặt", "Sửa đơn đặt"),
        new(AcqOrderDelete, "Bổ sung", "Đơn đặt", "Xóa đơn đặt"),
        new(AcqOrderApprove, "Bổ sung", "Đơn đặt", "Duyệt đơn đặt"),
        new(AcqOrderReceive, "Bổ sung", "Đơn đặt", "Ghi nhận giao hàng"),
        new(AcqOrderPrint, "Bổ sung", "Đơn đặt", "In đơn đặt hàng"),

        new(AcqHandoverView, "Bổ sung", "Biên bản bàn giao", "Xem biên bản"),
        new(AcqHandoverManage, "Bổ sung", "Biên bản bàn giao", "Lập / sửa biên bản"),

        new(AcqItemView, "Bổ sung", "Ấn phẩm (ĐKCB)", "Xem ĐKCB"),
        new(AcqItemCreate, "Bổ sung", "Ấn phẩm (ĐKCB)", "Thêm ĐKCB"),
        new(AcqItemUpdate, "Bổ sung", "Ấn phẩm (ĐKCB)", "Sửa ĐKCB"),
        new(AcqItemDelete, "Bổ sung", "Ấn phẩm (ĐKCB)", "Xóa ĐKCB"),
        new(AcqItemInspect, "Bổ sung", "Ấn phẩm (ĐKCB)", "Kiểm nhận ấn phẩm"),
        new(AcqItemLock, "Bổ sung", "Ấn phẩm (ĐKCB)", "Khóa / mở khóa ấn phẩm"),
        new(AcqItemMove, "Bổ sung", "Ấn phẩm (ĐKCB)", "Chuyển kho"),
        new(AcqItemDispose, "Bổ sung", "Ấn phẩm (ĐKCB)", "Thanh lý / ghi mất"),
        new(AcqItemPrintBarcode, "Bổ sung", "Ấn phẩm (ĐKCB)", "In mã vạch"),
        new(AcqItemPrintLabel, "Bổ sung", "Ấn phẩm (ĐKCB)", "In nhãn gáy"),
        new(AcqItemExport, "Bổ sung", "Ấn phẩm (ĐKCB)", "Xuất danh sách ĐKCB"),

        new(AcqWarehouseView, "Bổ sung", "Kho", "Xem kho và giá"),
        new(AcqWarehouseManage, "Bổ sung", "Kho", "Quản lý kho và giá"),
        new(AcqLibraryManage, "Bổ sung", "Kho", "Quản lý thư viện / cơ sở"),

        new(AcqInventoryView, "Bổ sung", "Kiểm kê", "Xem kỳ kiểm kê"),
        new(AcqInventoryCreate, "Bổ sung", "Kiểm kê", "Tạo kỳ kiểm kê"),
        new(AcqInventoryScan, "Bổ sung", "Kiểm kê", "Quét kiểm kê"),
        new(AcqInventoryClose, "Bổ sung", "Kiểm kê", "Đóng kỳ kiểm kê"),
        new(AcqInventoryReport, "Bổ sung", "Kiểm kê", "Báo cáo kiểm kê"),

        new(AcqFormTemplateManage, "Bổ sung", "Biểu mẫu", "Quản lý mẫu biểu in"),
        new(AcqReportView, "Bổ sung", "Báo cáo", "Xem báo cáo bổ sung"),
        new(AcqReportExport, "Bổ sung", "Báo cáo", "Xuất báo cáo bổ sung"),

        new(SerialView, "Ấn phẩm định kỳ", "Đầu báo / tạp chí", "Xem đầu báo"),
        new(SerialCreate, "Ấn phẩm định kỳ", "Đầu báo / tạp chí", "Thêm đầu báo"),
        new(SerialUpdate, "Ấn phẩm định kỳ", "Đầu báo / tạp chí", "Sửa đầu báo"),
        new(SerialDelete, "Ấn phẩm định kỳ", "Đầu báo / tạp chí", "Xóa đầu báo"),
        new(SerialPredict, "Ấn phẩm định kỳ", "Số ấn phẩm", "Sinh số dự kiến"),
        new(SerialReceive, "Ấn phẩm định kỳ", "Số ấn phẩm", "Ghi nhận số đã nhận"),
        new(SerialClaim, "Ấn phẩm định kỳ", "Số ấn phẩm", "Lập khiếu nại số thiếu"),
        new(SerialBind, "Ấn phẩm định kỳ", "Số ấn phẩm", "Đóng tập"),
        new(SerialArticleManage, "Ấn phẩm định kỳ", "Bài trích", "Quản lý mục lục bài trích"),
        new(SerialReportView, "Ấn phẩm định kỳ", "Báo cáo", "Xem báo cáo ấn phẩm định kỳ"),

        new(DigitalView, "Tài liệu số", "Tài liệu", "Xem tài liệu số"),
        new(DigitalUpload, "Tài liệu số", "Tài liệu", "Tải lên tài liệu số"),
        new(DigitalUpdate, "Tài liệu số", "Tài liệu", "Sửa thông tin tài liệu số"),
        new(DigitalDelete, "Tài liệu số", "Tài liệu", "Xóa tài liệu số"),
        new(DigitalDownload, "Tài liệu số", "Tài liệu", "Tải về tài liệu số"),
        new(DigitalCollectionManage, "Tài liệu số", "Bộ sưu tập", "Quản lý bộ sưu tập"),
        new(DigitalRequestView, "Tài liệu số", "Yêu cầu truy cập", "Xem yêu cầu truy cập"),
        new(DigitalRequestApprove, "Tài liệu số", "Yêu cầu truy cập", "Duyệt / từ chối yêu cầu"),
        new(DigitalImport, "Tài liệu số", "Xuất nhập", "Nhập hàng loạt tài liệu số"),
        new(DigitalExport, "Tài liệu số", "Xuất nhập", "Xuất tài liệu số kèm metadata"),
        new(DigitalReportView, "Tài liệu số", "Báo cáo", "Xem báo cáo tài liệu số"),
        new(DigitalAccessLogView, "Tài liệu số", "Báo cáo", "Xem nhật ký truy cập"),

        new(ReaderView, "Bạn đọc", "Hồ sơ", "Xem hồ sơ bạn đọc"),
        new(ReaderCreate, "Bạn đọc", "Hồ sơ", "Thêm bạn đọc"),
        new(ReaderUpdate, "Bạn đọc", "Hồ sơ", "Sửa bạn đọc"),
        new(ReaderDelete, "Bạn đọc", "Hồ sơ", "Xóa bạn đọc"),
        new(ReaderLock, "Bạn đọc", "Hồ sơ", "Tạm khóa / mở khóa thẻ"),
        new(ReaderExtendCard, "Bạn đọc", "Thẻ", "Gia hạn / cấp lại thẻ"),
        new(ReaderPrintCard, "Bạn đọc", "Thẻ", "In thẻ bạn đọc"),
        new(ReaderCardTemplateManage, "Bạn đọc", "Thẻ", "Quản lý mẫu thẻ"),
        new(ReaderImport, "Bạn đọc", "Xuất nhập", "Nhập bạn đọc từ Excel"),
        new(ReaderExport, "Bạn đọc", "Xuất nhập", "Xuất danh sách bạn đọc"),
        new(ReaderResetPassword, "Bạn đọc", "Hồ sơ", "Đặt lại mật khẩu bạn đọc"),
        new(ReaderViolationManage, "Bạn đọc", "Vi phạm", "Quản lý vi phạm"),
        new(ReaderReportView, "Bạn đọc", "Báo cáo", "Xem báo cáo bạn đọc"),

        new(CirculationPolicyView, "Lưu thông", "Chính sách", "Xem chính sách lưu thông"),
        new(CirculationPolicyManage, "Lưu thông", "Chính sách", "Quản lý chính sách lưu thông"),
        new(CirculationLoanCreate, "Lưu thông", "Mượn trả", "Ghi mượn"),
        new(CirculationLoanReturn, "Lưu thông", "Mượn trả", "Ghi trả"),
        new(CirculationLoanRenew, "Lưu thông", "Mượn trả", "Gia hạn"),
        new(CirculationLoanView, "Lưu thông", "Mượn trả", "Xem giao dịch mượn trả"),
        new(CirculationHoldManage, "Lưu thông", "Đặt giữ", "Quản lý đặt giữ chỗ"),
        new(CirculationFineView, "Lưu thông", "Tiền phạt", "Xem tiền phạt"),
        new(CirculationFineCollect, "Lưu thông", "Tiền phạt", "Thu tiền phạt"),
        new(CirculationFineWaive, "Lưu thông", "Tiền phạt", "Miễn giảm tiền phạt"),
        new(CirculationLockerManage, "Lưu thông", "Tủ gửi đồ", "Quản lý tủ gửi đồ"),
        new(CirculationVisitManage, "Lưu thông", "Ra vào thư viện", "Ghi nhận ra vào thư viện"),
        new(CirculationTemplateManage, "Lưu thông", "Biểu mẫu", "Quản lý mẫu phiếu mượn / trả"),
        new(CirculationReportView, "Lưu thông", "Báo cáo", "Xem báo cáo lưu thông"),
        new(CirculationReportExport, "Lưu thông", "Báo cáo", "Xuất báo cáo lưu thông"),

        new(CmsPageManage, "Quản trị nội dung", "Trang tĩnh", "Quản lý trang tĩnh"),
        new(CmsNewsView, "Quản trị nội dung", "Tin tức", "Xem tin tức"),
        new(CmsNewsManage, "Quản trị nội dung", "Tin tức", "Soạn / sửa tin tức"),
        new(CmsNewsPublish, "Quản trị nội dung", "Tin tức", "Xuất bản tin tức"),
        new(CmsMenuManage, "Quản trị nội dung", "Giao diện", "Quản lý menu"),
        new(CmsBannerManage, "Quản trị nội dung", "Giao diện", "Quản lý banner"),
        new(CmsSettingManage, "Quản trị nội dung", "Giao diện", "Cấu hình thông tin trang thư viện"),
        new(CmsGalleryManage, "Quản trị nội dung", "Thư viện ảnh", "Quản lý album ảnh"),
        new(CmsLinkManage, "Quản trị nội dung", "Giao diện", "Quản lý liên kết website"),
        new(CmsReviewModerate, "Quản trị nội dung", "Tương tác", "Kiểm duyệt nhận xét bạn đọc"),

        new(CourseMajorManage, "Tài liệu môn học", "Ngành", "Quản lý ngành đào tạo"),
        new(CourseManage, "Tài liệu môn học", "Môn học", "Quản lý môn học"),
        new(CourseDocumentLink, "Tài liệu môn học", "Liên kết tài liệu", "Gán tài liệu cho môn học"),
        new(CourseReportView, "Tài liệu môn học", "Báo cáo", "Xem báo cáo tài liệu môn học"),

        new(ExchangeImport, "Trao đổi dữ liệu", "Nhập xuất", "Nhập dữ liệu"),
        new(ExchangeExport, "Trao đổi dữ liệu", "Nhập xuất", "Xuất dữ liệu"),
        new(ExchangeFullExport, "Trao đổi dữ liệu", "Nhập xuất", "Xuất toàn bộ dữ liệu hệ thống"),
        new(ExchangeApiClientManage, "Trao đổi dữ liệu", "Tích hợp", "Quản lý ứng dụng tích hợp")
    };
}
