/** Phân hệ VI — Bạn đọc. Các kiểu dữ liệu khớp với DTO của máy chủ. */

export type ReaderStatus = 'Active' | 'Expired' | 'Suspended' | 'Locked' | 'Graduated';

export interface ReaderDto {
  id: string;
  cardNumber: string;
  studentCode: string | null;
  fullName: string;
  gender: string | null;
  dateOfBirth: string | null;
  email: string | null;
  phone: string | null;
  photoUrl: string | null;
  readerTypeId: string;
  readerTypeName: string | null;
  facultyId: string | null;
  facultyName: string | null;
  majorId: string | null;
  majorName: string | null;
  className: string | null;
  courseYear: string | null;
  cardIssueDate: string;
  cardExpireDate: string;
  status: ReaderStatus;
  statusReason: string | null;
  depositAmount: number;
  debtAmount: number;
  currentLoanCount: number;
  totalLoanCount: number;
  isExpired: boolean;
  isExpiringSoon: boolean;
  canBorrow: boolean;
}

export interface ReaderCardDto {
  id: string;
  cardNumber: string;
  issueDate: string;
  expireDate: string;
  printCount: number;
  isCurrent: boolean;
  reissueReason: string | null;
}

export interface ReaderDetailDto extends ReaderDto {
  idCardNumber: string | null;
  address: string | null;
  note: string | null;
  hasPassword: boolean;
  mustChangePassword: boolean;
  lastLoginAt: string | null;
  lockedUntil: string | null;
  createdAt: string;
  cards: ReaderCardDto[];
}

export interface ReaderFilter {
  keyword?: string;
  readerTypeId?: string;
  facultyId?: string;
  majorId?: string;
  className?: string;
  courseYear?: string;
  status?: ReaderStatus;
  expired?: boolean;
  expiringInDays?: number;
  hasDebt?: boolean;
  borrowing?: boolean;
  neverBorrowed?: boolean;
  createdFrom?: string;
  createdTo?: string;
}

export interface ReaderSelectionDto {
  readerIds?: string[];
  filter?: ReaderFilter;
  useFilter?: boolean;
}

export interface BulkSkipDto {
  readerId: string;
  cardNumber: string;
  fullName: string;
  reason: string;
}

export interface BulkResultDto {
  total: number;
  succeeded: number;
  skipped: number;
  skips: BulkSkipDto[];
}

export interface ReaderClearanceDto {
  readerId: string;
  cardNumber: string;
  fullName: string;
  studentCode: string | null;
  className: string | null;
  facultyName: string | null;
  outstandingLoans: number;
  outstandingFines: number;
  cleared: boolean;
  blockers: string[];
}

// ---------------------------------------------------------------------------------------------
// Lịch sử
// ---------------------------------------------------------------------------------------------

export type LoanStatus = 'Active' | 'Returned' | 'Overdue' | 'Lost' | 'Damaged';

export interface ReaderLoanDto {
  id: string;
  code: string;
  barcode: string | null;
  title: string | null;
  loanDate: string;
  dueDate: string;
  returnDate: string | null;
  renewedCount: number;
  status: LoanStatus;
  fineAmount: number;
  overdueDays: number;
}

export interface ReaderFineDto {
  id: string;
  code: string;
  type: string;
  amount: number;
  paidAmount: number;
  outstanding: number;
  waived: boolean;
  paidAt: string | null;
  createdAt: string;
  note: string | null;
}

export interface ReaderVisitDto {
  id: string;
  checkinAt: string;
  checkoutAt: string | null;
  gate: string | null;
  purpose: string | null;
  minutes: number | null;
}

export interface ReaderDigitalAccessDto {
  id: string;
  documentId: string;
  documentTitle: string | null;
  action: string;
  occurredAt: string;
  durationSeconds: number | null;
  ip: string | null;
}

export interface ReaderViolationDto {
  id: string;
  readerId: string;
  violationTypeId: string | null;
  violationTypeName: string | null;
  description: string | null;
  fineAmount: number;
  occurredAt: string;
  resolvedAt: string | null;
  resolution: string | null;
}

// ---------------------------------------------------------------------------------------------
// VI.2 — Mẫu thẻ
// ---------------------------------------------------------------------------------------------

export type BarcodeType = 'Code39' | 'Code128' | 'Ean13' | 'QrCode';

export interface CardBoxDto {
  x: number;
  y: number;
  width: number;
  height: number;
  source: string;
  prefix?: string | null;
  fontSize: number;
  bold?: boolean;
  italic?: boolean;
  uppercase?: boolean;
  align: 'left' | 'center' | 'right';
  color?: string | null;
  border?: boolean;
}

export interface CardImageDto {
  x: number;
  y: number;
  width: number;
  height: number;
  /** photo — ảnh bạn đọc; logo — logo thư viện. */
  kind: 'photo' | 'logo';
  border?: boolean;
}

export interface CardBarcodeDto {
  x: number;
  y: number;
  width: number;
  height: number;
  type: BarcodeType;
  showText?: boolean;
  fontSize?: number;
}

export interface CardFaceLayoutDto {
  boxes: CardBoxDto[];
  images: CardImageDto[];
  barcode?: CardBarcodeDto | null;
  backgroundColor?: string | null;
  headerBandHeight?: number;
  headerBandColor?: string | null;
}

export interface ReaderCardTemplateDto {
  id: string;
  code: string;
  name: string;
  widthMm: number;
  heightMm: number;
  cardsPerPage: number;
  isDefault: boolean;
  isActive: boolean;
  printBack: boolean;
  front: CardFaceLayoutDto;
  back: CardFaceLayoutDto;
}

export interface CardFieldOptionDto {
  key: string;
  label: string;
}

// ---------------------------------------------------------------------------------------------
// VI.4 — Nhập, xuất, đồng bộ
// ---------------------------------------------------------------------------------------------

export type JobStatus = 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Cancelled';

/** 0 = báo lỗi, 1 = bỏ qua, 2 = cập nhật. */
export type ReaderImportDuplicateAction = 0 | 1 | 2;

export interface ReaderImportOptions {
  mapping?: Record<string, string>;
  onDuplicate?: ReaderImportDuplicateAction;
  defaultReaderTypeId?: string | null;
  createMissingCatalogs?: boolean;
  setInitialPassword?: boolean;
}

export interface ReaderImportErrorDto {
  row: number;
  column: string | null;
  value: string | null;
  message: string;
}

export interface ReaderImportRowDto {
  row: number;
  cardNumber: string | null;
  studentCode: string | null;
  fullName: string;
  gender: string | null;
  dateOfBirth: string | null;
  email: string | null;
  phone: string | null;
  readerType: string | null;
  faculty: string | null;
  major: string | null;
  className: string | null;
  courseYear: string | null;
  hasError: boolean;
  isExisting: boolean;
}

export interface ReaderImportPreviewDto {
  fileName: string;
  totalRows: number;
  validRows: number;
  errorRows: number;
  headers: string[];
  errors: ReaderImportErrorDto[];
  sample: ReaderImportRowDto[];
  /** Nguyên ô của các dòng lỗi, để sửa tại chỗ rồi gửi lại. */
  errorRowCells: ReaderImportRawRowDto[];
}

/** Một dòng thô của tệp: số dòng và các ô theo tiêu đề cột. */
export interface ReaderImportRawRowDto {
  row: number;
  cells: Record<string, string>;
}

export interface ReaderImportRowsResultDto {
  dryRun: boolean;
  totalRows: number;
  created: number;
  updated: number;
  skipped: number;
  errorRows: number;
  errors: ReaderImportErrorDto[];
  errorRowCells: ReaderImportRawRowDto[];
}

export interface ReaderImportBatchDto {
  id: string;
  fileName: string;
  totalRows: number;
  successRows: number;
  errorRows: number;
  status: JobStatus;
  createdAt: string;
  finishedAt: string | null;
  errors: ReaderImportErrorDto[];
}

export interface PhotoImportIssueDto {
  fileName: string;
  message: string;
}

export interface PhotoImportResultDto {
  totalFiles: number;
  matched: number;
  unmatched: number;
  invalid: number;
  issues: PhotoImportIssueDto[];
  matchedReaders: string[];
}

export interface ReaderSyncResultDto {
  totalItems: number;
  created: number;
  updated: number;
  skipped: number;
  errorItems: number;
  errors: ReaderImportErrorDto[];
  dryRun: boolean;
}

// ---------------------------------------------------------------------------------------------
// VI.5 — Báo cáo
// ---------------------------------------------------------------------------------------------

export type ReaderReportDimension =
  | 'ReaderType'
  | 'Faculty'
  | 'Major'
  | 'Cohort'
  | 'Class'
  | 'Status'
  | 'Gender';

export type ReaderTimeGrouping = 'Day' | 'Month' | 'Quarter' | 'Year';

export interface ReaderReportFilter {
  fromDate?: string;
  toDate?: string;
  readerTypeId?: string;
  facultyId?: string;
  majorId?: string;
  courseYear?: string;
  status?: ReaderStatus;
}

export interface ReaderReportRowDto {
  key: string;
  label: string;
  total: number;
  active: number;
  expired: number;
  suspended: number;
  graduated: number;
  everBorrowed: number;
  percentage: number;
}

export interface ReaderTimeRowDto {
  period: string;
  newReaders: number;
  cumulative: number;
}

export interface ExpiringCardRowDto {
  readerId: string;
  cardNumber: string;
  studentCode: string | null;
  fullName: string;
  readerTypeName: string | null;
  facultyName: string | null;
  className: string | null;
  email: string | null;
  phone: string | null;
  cardExpireDate: string;
  daysLeft: number;
}

export interface ExpiringCardsReportDto {
  expiredCount: number;
  expiringCount: number;
  validCount: number;
  rows: ExpiringCardRowDto[];
}

export interface ReaderActivityRowDto {
  readerId: string;
  cardNumber: string;
  studentCode: string | null;
  fullName: string;
  readerTypeName: string | null;
  facultyName: string | null;
  className: string | null;
  loanCount: number;
  lastLoanAt: string | null;
}
