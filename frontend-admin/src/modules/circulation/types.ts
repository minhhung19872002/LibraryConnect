/** Phân hệ VII — Lưu thông. Các kiểu dữ liệu khớp với DTO của máy chủ. */

export type LoanStatus = 'Active' | 'Returned' | 'Overdue' | 'Lost' | 'Damaged';
export type LoanType = 'InHouse' | 'TakeHome' | 'SelfCheckout';
export type LoanChannel = 'Desk' | 'Opac' | 'Mobile';
export type HoldStatus = 'Waiting' | 'Ready' | 'Fulfilled' | 'Expired' | 'Cancelled';
export type FineType = 'Overdue' | 'Lost' | 'Damaged' | 'Other';
export type LockerStatus = 'Free' | 'InUse' | 'Broken' | 'Locked';
export type ReaderStatus = 'Active' | 'Expired' | 'Suspended' | 'Locked' | 'Graduated';
export type ItemStatus =
  | 'PendingInspection'
  | 'InStock'
  | 'OnLoan'
  | 'OnHoldShelf'
  | 'Lost'
  | 'Damaged'
  | 'Discarded'
  | 'UnderInventory';

export interface CirculationWarningDto {
  code: string;
  message: string;
  blocking: boolean;
}

export interface LoanRowDto {
  id: string;
  code: string;
  readerId: string;
  readerCardNumber: string;
  readerName: string;
  readerTypeName: string | null;
  facultyName: string | null;
  className: string | null;
  itemId: string;
  barcode: string | null;
  title: string | null;
  callNumber: string | null;
  warehouseName: string | null;
  loanDate: string;
  dueDate: string;
  returnDate: string | null;
  renewedCount: number;
  maxRenewals: number;
  status: LoanStatus;
  loanType: LoanType;
  channel: LoanChannel;
  loanByName: string | null;
  returnByName: string | null;
  fineAmount: number;
  fineOutstanding: number;
  overdueDays: number;
  estimatedFine: number;
  note: string | null;
}

export interface LoanRenewalDto {
  id: string;
  renewalDate: string;
  oldDueDate: string;
  newDueDate: string;
  channel: LoanChannel;
  status: string;
  rejectReason: string | null;
}

export interface LoanDetailDto extends LoanRowDto {
  renewals: LoanRenewalDto[];
  fines: FineRowDto[];
}

export interface HoldRowDto {
  id: string;
  readerId: string;
  readerCardNumber: string;
  readerName: string;
  bibId: string;
  title: string | null;
  itemId: string | null;
  barcode: string | null;
  holdDate: string;
  expireDate: string | null;
  pickupWarehouseId: string | null;
  pickupWarehouseName: string | null;
  status: HoldStatus;
  queuePosition: number;
  notifiedAt: string | null;
  channel: LoanChannel;
  cancelReason: string | null;
  availableCopies: number;
}

export interface DeskReaderDto {
  id: string;
  cardNumber: string;
  studentCode: string | null;
  fullName: string;
  readerTypeId: string;
  readerTypeName: string | null;
  facultyName: string | null;
  className: string | null;
  hasPhoto: boolean;
  status: ReaderStatus;
  cardExpireDate: string;
  canBorrow: boolean;
  currentLoanCount: number;
  overdueCount: number;
  outstandingFines: number;
  maxItems: number;
  remainingQuota: number;
  warnings: CirculationWarningDto[];
  currentLoans: LoanRowDto[];
  readyHolds: HoldRowDto[];
}

export interface ScanForLoanDto {
  allowed: boolean;
  itemId: string | null;
  barcode: string;
  registerNumber: string | null;
  title: string | null;
  author: string | null;
  callNumber: string | null;
  warehouseName: string | null;
  documentTypeName: string | null;
  itemStatus: ItemStatus | null;
  dueDate: string | null;
  policyName: string;
  allowTakeHome: boolean;
  warnings: CirculationWarningDto[];
}

export interface CirculationFailureDto {
  barcode: string;
  message: string;
}

export interface CheckoutResultDto {
  readerId: string;
  readerName: string;
  loans: LoanRowDto[];
  failures: CirculationFailureDto[];
  slipCode: string | null;
}

export interface ReturnedItemDto {
  loanId: string;
  loanCode: string;
  barcode: string;
  title: string | null;
  readerId: string;
  readerName: string;
  readerCardNumber: string;
  dueDate: string;
  overdueDays: number;
  fine: number;
  fineCode: string | null;
  holdWaiting: boolean;
  holdForReaderName: string | null;
  holdPickupWarehouse: string | null;
  warnings: CirculationWarningDto[];
}

export interface ReturnResultDto {
  items: ReturnedItemDto[];
  failures: CirculationFailureDto[];
  totalFine: number;
  slipCode: string | null;
}

export interface FineRowDto {
  id: string;
  code: string;
  readerId: string;
  readerCardNumber: string;
  readerName: string;
  loanId: string | null;
  loanCode: string | null;
  title: string | null;
  barcode: string | null;
  type: FineType;
  amount: number;
  paidAmount: number;
  outstanding: number;
  waived: boolean;
  waiveReason: string | null;
  paidAt: string | null;
  paidByName: string | null;
  createdAt: string;
  note: string | null;
}

export interface ReaderFineSummaryDto {
  readerId: string;
  cardNumber: string;
  fullName: string;
  totalOutstanding: number;
  totalPaid: number;
  totalWaived: number;
  fines: FineRowDto[];
}

// ---------------------------------------------------------------------------------------------
// VII.1 — Chính sách và lịch nghỉ
// ---------------------------------------------------------------------------------------------

export interface CirculationPolicyDto {
  id: string;
  name: string;
  readerTypeId: string | null;
  readerTypeName: string | null;
  documentTypeId: string | null;
  documentTypeName: string | null;
  warehouseId: string | null;
  warehouseName: string | null;
  maxItems: number;
  loanDays: number;
  maxRenewals: number;
  renewalDays: number;
  finePerDay: number;
  graceDays: number;
  maxHolds: number;
  holdExpireDays: number;
  allowLoan: boolean;
  allowRenew: boolean;
  allowHold: boolean;
  allowTakeHome: boolean;
  requireRenewalApproval: boolean;
  priority: number;
  isActive: boolean;
}

export interface EffectivePolicyDto {
  policyId: string | null;
  name: string;
  maxItems: number;
  loanDays: number;
  maxRenewals: number;
  renewalDays: number;
  finePerDay: number;
  graceDays: number;
  maxHolds: number;
  holdExpireDays: number;
  allowLoan: boolean;
  allowRenew: boolean;
  allowHold: boolean;
  allowTakeHome: boolean;
  requireRenewalApproval: boolean;
}

export interface HolidayDto {
  id: string;
  name: string;
  fromDate: string;
  toDate: string;
  isRecurringYearly: boolean;
  libraryId: string | null;
  libraryName: string | null;
  isActive: boolean;
}

export interface DueDatePreviewDto {
  rawDueDate: string;
  dueDate: string;
  shifted: boolean;
  explanation: string;
}

// ---------------------------------------------------------------------------------------------
// VII.2 — Ra vào thư viện
// ---------------------------------------------------------------------------------------------

export interface VisitRowDto {
  id: string;
  readerId: string;
  readerCardNumber: string;
  readerName: string;
  readerTypeName: string | null;
  facultyName: string | null;
  libraryId: string | null;
  libraryName: string | null;
  checkinAt: string;
  checkoutAt: string | null;
  minutes: number | null;
  gate: string | null;
  purpose: string | null;
}

export interface GateScanResultDto {
  checkedIn: boolean;
  visit: VisitRowDto;
  reader: DeskReaderDto;
  message: string;
  insideCount: number;
}

// ---------------------------------------------------------------------------------------------
// VII.3 — Tủ gửi đồ
// ---------------------------------------------------------------------------------------------

export interface LockerRowDto {
  id: string;
  code: string;
  libraryId: string | null;
  libraryName: string | null;
  area: string | null;
  size: string | null;
  status: LockerStatus;
  mapRow: number | null;
  mapColumn: number | null;
  note: string | null;
  usageId: string | null;
  readerId: string | null;
  readerName: string | null;
  readerCardNumber: string | null;
  checkinAt: string | null;
  keyNumber: string | null;
  minutesInUse: number | null;
  overdue: boolean;
}

export interface LockerMapDto {
  areas: string[];
  free: number;
  inUse: number;
  broken: number;
  overdue: number;
  lockers: LockerRowDto[];
}

export interface LockerUsageRowDto {
  id: string;
  lockerId: string;
  lockerCode: string;
  area: string | null;
  readerId: string;
  readerName: string;
  readerCardNumber: string;
  checkinAt: string;
  checkoutAt: string | null;
  minutes: number | null;
  keyNumber: string | null;
  note: string | null;
}

// ---------------------------------------------------------------------------------------------
// VII.5 — Báo cáo
// ---------------------------------------------------------------------------------------------

export interface CirculationReportFilter {
  fromDate?: string;
  toDate?: string;
  readerTypeId?: string;
  facultyId?: string;
  warehouseId?: string;
  documentTypeId?: string;
  readerId?: string;
  libraryId?: string;
  top?: number;
}

export interface ReportBucketDto {
  key: string;
  label: string;
  count: number;
  amount: number;
  percentage: number;
}

export interface VisitReportDto {
  totalVisits: number;
  uniqueReaders: number;
  insideNow: number;
  averageMinutes: number;
  byDay: ReportBucketDto[];
  byHour: ReportBucketDto[];
  byReaderType: ReportBucketDto[];
}

export interface LoanHistoryReportDto {
  totalLoans: number;
  returned: number;
  stillOut: number;
  overdueReturns: number;
  totalFine: number;
  byDay: ReportBucketDto[];
  rows: LoanRowDto[];
}

export interface OverdueReportDto {
  totalOverdue: number;
  readers: number;
  estimatedFine: number;
  byRange: ReportBucketDto[];
  rows: LoanRowDto[];
}

export interface LockerReportDto {
  totalLockers: number;
  totalUsages: number;
  averageMinutes: number;
  openNow: number;
  byArea: ReportBucketDto[];
  byDay: ReportBucketDto[];
  topLockers: ReportBucketDto[];
}

export interface TopReaderRowDto {
  readerId: string;
  cardNumber: string;
  fullName: string;
  readerTypeName: string | null;
  facultyName: string | null;
  className: string | null;
  loanCount: number;
  overdueCount: number;
  fineTotal: number;
  lastLoanAt: string | null;
}

export interface TopItemRowDto {
  bibId: string;
  title: string | null;
  author: string | null;
  isbn: string | null;
  documentTypeName: string | null;
  ddc: string | null;
  loanCount: number;
  copyCount: number;
  lastLoanAt: string | null;
}

export interface PendingRenewalDto {
  id: string;
  loanId: string;
  loanCode: string;
  readerName: string;
  readerCardNumber: string;
  title: string | null;
  barcode: string | null;
  oldDueDate: string;
  newDueDate: string;
  requestedAt: string;
  channel: LoanChannel;
}
