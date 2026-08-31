namespace LibraryConnect.Domain.Enums;

// Enum members are named in English (code convention, section 11 of the spec) and persisted as
// strings so the database stays readable. The Vietnamese labels shown to users live in the shared
// i18n dictionary (Application/Localization/EnumLabels.cs and the frontend i18n files) so that a
// wording change never requires a data migration.

public enum RecordStatus { Draft, Queued, Approved, Published }

public enum BibSource { Manual, Iso2709, Z3950, Excel, Oai, MarcXml }

public enum ItemStatus
{
    /// <summary>CHƯA KIỂM NHẬN — received but not yet inspected, cannot circulate.</summary>
    PendingInspection,
    /// <summary>TRONG KHO — on the shelf, available.</summary>
    InStock,
    /// <summary>ĐANG MƯỢN</summary>
    OnLoan,
    /// <summary>ĐẶT GIỮ — held for a reader waiting at the desk.</summary>
    OnHoldShelf,
    /// <summary>MẤT</summary>
    Lost,
    /// <summary>HỎNG</summary>
    Damaged,
    /// <summary>THANH LÝ</summary>
    Discarded,
    /// <summary>ĐANG KIỂM KÊ</summary>
    UnderInventory
}

public enum AcquisitionType { Purchase, Donation, Exchange, LegalDeposit }

public enum WarehouseType { OpenStack, ClosedStack, ReadingRoom, DiscardStore }

public enum PurchaseRequestType { Monograph, Serial }

public enum PurchaseRequestStatus { Draft, Submitted, Approved, PartiallyApproved, Rejected, Cancelled }

public enum PurchaseOrderStatus { New, Ordered, PartiallyReceived, Received, Cancelled }

public enum InventoryPeriodStatus { Preparing, InProgress, Closed }

public enum InventoryResultType { Match, Missing, Unexpected, WrongWarehouse }

public enum SerialFrequency { Daily, Weekly, Biweekly, SemiMonthly, Monthly, Bimonthly, Quarterly, SemiAnnual, Annual, Irregular }

public enum SerialIssueStatus { Expected, Received, Missing, Claimed, Bound }

public enum SerialClaimStatus { Open, Responded, Resolved, Cancelled }

public enum DigitalAccessLevel
{
    /// <summary>CÔNG KHAI — anyone, including anonymous OPAC visitors.</summary>
    Public,
    /// <summary>NỘI BỘ — any authenticated reader.</summary>
    Internal,
    /// <summary>HẠN CHẾ — requires an approved access request.</summary>
    Restricted,
    /// <summary>CẤM — metadata only, content never served.</summary>
    Forbidden
}

public enum DigitalFileType { Original, Preview, Thumbnail, OcrText }

public enum DigitalAccessAction { View, Download, Print }

public enum AccessRequestStatus { Pending, Approved, Rejected, Expired, Revoked }

public enum ReaderStatus { Active, Expired, Suspended, Locked, Graduated }

public enum LoanStatus { Active, Returned, Overdue, Lost, Damaged }

public enum LoanType { InHouse, TakeHome, SelfCheckout }

public enum LoanChannel { Desk, Opac, Mobile }

public enum HoldStatus { Waiting, Ready, Fulfilled, Expired, Cancelled }

public enum FineType { Overdue, Lost, Damaged, Other }

public enum LockerStatus { Free, InUse, Broken, Locked }

public enum BackupType { Full, DataOnly, Incremental }

public enum BackupStatus { Running, Success, Failed, Restored }

public enum AuditAction { Create, Update, Delete, Read, Login, LoginFailed, Logout, Export, Import, Approve, Restore, Backup, PermissionChange, ParameterChange }

public enum DataScopeType { Library, Warehouse, DocumentType }

public enum ParameterDataType { Text, Number, Boolean, Date, Json, File, Password, Cron }

public enum ImportExportJobType { Iso2709In, Iso2709Out, ExcelIn, ExcelOut, MarcXmlIn, MarcXmlOut, ReaderExcelIn, DigitalBulkIn, FullSystemExport }

public enum JobStatus { Pending, Running, Completed, Failed, Cancelled }

public enum CourseRelationType { MainTextbook, RequiredReference, AdditionalReference }

public enum BarcodeType { Code39, Code128, QrCode }

public enum CatalogQueueStatus { Pending, InProgress, WaitingApproval, Completed, Returned }
