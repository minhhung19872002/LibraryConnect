import type { MarcValidationResult } from '@/modules/marc/types';

export type RecordStatus = 'Draft' | 'Pending' | 'Published' | 'Archived';
export type BibSource = 'Manual' | 'Iso2709' | 'Z3950' | 'Excel' | 'Oai' | 'Quick';

export interface BibListItem {
  id: string;
  controlNumber: string;
  title: string;
  subtitle?: string | null;
  authorMain?: string | null;
  publisherName?: string | null;
  publishYear?: number | null;
  isbn?: string | null;
  ddc?: string | null;
  documentTypeName?: string | null;
  languageName?: string | null;
  status: RecordStatus;
  source: BibSource;
  itemCount: number;
  availableItemCount: number;
  digitalDocumentCount: number;
  coverImageUrl?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface IsbdArea {
  label: string;
  content: string;
}

export interface BibAuthorLink {
  authorId: string;
  name: string;
  role?: string | null;
  isMain: boolean;
}

export interface BibClassificationLink {
  classificationId: string;
  code: string;
  scheme: string;
}

export interface BibDetail extends BibListItem {
  marcJson: string;
  isbd: IsbdArea[];
  statementOfResponsibility?: string | null;
  uniformTitle?: string | null;
  issn?: string | null;
  publishPlace?: string | null;
  edition?: string | null;
  pages?: string | null;
  dimensions?: string | null;
  abstract?: string | null;
  seriesTitle?: string | null;
  seriesVolume?: string | null;
  sourceRef?: string | null;
  documentTypeId?: string | null;
  carrierTypeId?: string | null;
  languageId?: string | null;
  countryId?: string | null;
  authors: BibAuthorLink[];
  subjects: string[];
  keywords: string[];
  classifications: BibClassificationLink[];
  collectionIds: string[];
  coverImageUrl?: string | null;
  coverImageSource?: string | null;
  versionCount: number;
  loanCount: number;
  viewCount: number;
  createdByName?: string | null;
  updatedByName?: string | null;
}

export interface NewBibRecord {
  marcJson: string;
  templateId?: string | null;
  templateName?: string | null;
  documentTypeId?: string | null;
  appliedDefaults: number;
}

export interface SaveBibResult {
  id: string;
  controlNumber: string;
  title: string;
  validation: MarcValidationResult;
}

export interface BibVersion {
  id: string;
  versionNumber: number;
  changeNote?: string | null;
  changedByName?: string | null;
  changedAt: string;
}

export interface MarcDiffLine {
  kind: 'Added' | 'Removed' | 'Changed' | 'Unchanged';
  tag: string;
  before?: string | null;
  after?: string | null;
}

export type ItemStatus =
  | 'PendingInspection'
  | 'InStock'
  | 'OnLoan'
  | 'Reserved'
  | 'Lost'
  | 'Damaged'
  | 'Discarded'
  | 'Transferring'
  | 'Binding';

export type AcquisitionType = 'Purchase' | 'Donation' | 'Exchange' | 'Deposit' | 'Transfer' | 'Other';

export interface Item {
  id: string;
  bibId: string;
  barcode: string;
  registerNumber: string;
  warehouseId: string;
  warehouseName?: string | null;
  shelfId?: string | null;
  shelfName?: string | null;
  callNumber?: string | null;
  price: number;
  fundingSourceId?: string | null;
  fundingSourceName?: string | null;
  acquisitionDate: string;
  acquisitionType: AcquisitionType;
  status: ItemStatus;
  condition?: string | null;
  isLocked: boolean;
  lockReason?: string | null;
  volumeNumber?: string | null;
  copyNumber: number;
  note?: string | null;
  loanCount: number;
  lastLoanAt?: string | null;
}

export interface CreateItemsResult {
  created: number;
  barcodes: string[];
  callNumber?: string | null;
}

export interface Warehouse {
  id: string;
  code: string;
  name: string;
  libraryId: string;
  libraryName?: string | null;
  type: string;
  capacity?: number | null;
  location?: string | null;
  callNumberRule?: string | null;
  isClosedForInventory: boolean;
  isActive: boolean;
  itemCount: number;
}

export interface Shelf {
  id: string;
  code: string;
  name: string;
  warehouseId: string;
  warehouseName?: string | null;
  capacity?: number | null;
  currentCount: number;
  isActive: boolean;
}

export interface MarcFieldDefault {
  id: string;
  documentTypeId?: string | null;
  documentTypeName?: string | null;
  tag: string;
  ind1?: string | null;
  ind2?: string | null;
  subfield?: string | null;
  defaultValue?: string | null;
  position?: number | null;
  length?: number | null;
  parameterKey?: string | null;
  isActive: boolean;
  sortOrder: number;
  fieldName?: string | null;
}

/** Một lượt mượn của một bản thuộc biểu ghi — tab "Lịch sử lưu thông" (II.3). */
export interface BibLoan {
  id: string;
  code: string;
  barcode?: string | null;
  readerName: string;
  readerCardNumber: string;
  loanDate: string;
  dueDate: string;
  returnDate?: string | null;
  renewedCount: number;
  status: 'Active' | 'Returned' | 'Overdue' | 'Lost' | 'Damaged';
  loanType: 'InHouse' | 'TakeHome' | 'SelfCheckout';
}

export const BIB_LOAN_STATUS_LABELS: Record<BibLoan['status'], string> = {
  Active: 'Đang mượn',
  Returned: 'Đã trả',
  Overdue: 'Quá hạn',
  Lost: 'Mất',
  Damaged: 'Hỏng',
};

export interface MarcTemplate {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  documentTypeId?: string | null;
  documentTypeName?: string | null;
  isDefault: boolean;
  isActive: boolean;
  fields: string;
  fieldCount: number;
}

/** Nhãn tiếng Việt của các trạng thái, dùng chung cho bảng và bộ lọc. */
export const RECORD_STATUS_LABELS: Record<RecordStatus, string> = {
  Draft: 'Nháp',
  Pending: 'Chờ duyệt',
  Published: 'Đã xuất bản',
  Archived: 'Lưu trữ',
};

export const BIB_SOURCE_LABELS: Record<BibSource, string> = {
  Manual: 'Nhập tay',
  Iso2709: 'Nhập từ tệp ISO 2709',
  Z3950: 'Lấy từ Z39.50',
  Excel: 'Nhập từ Excel',
  Oai: 'Thu hoạch OAI-PMH',
  Quick: 'Biên mục sơ lược',
};

export const ITEM_STATUS_LABELS: Record<ItemStatus, string> = {
  PendingInspection: 'Chờ kiểm nhận',
  InStock: 'Trong kho',
  OnLoan: 'Đang cho mượn',
  Reserved: 'Đang giữ chỗ',
  Lost: 'Mất',
  Damaged: 'Hỏng',
  Discarded: 'Đã thanh lý',
  Transferring: 'Đang chuyển kho',
  Binding: 'Đang đóng tập',
};

export const ACQUISITION_TYPE_LABELS: Record<AcquisitionType, string> = {
  Purchase: 'Mua',
  Donation: 'Biếu tặng',
  Exchange: 'Trao đổi',
  Deposit: 'Nộp lưu chiểu',
  Transfer: 'Điều chuyển',
  Other: 'Khác',
};

/** Kết quả tra ảnh bìa ở nguồn ngoài. */
export interface CoverLookupOutcome {
  found: boolean;
  source?: string | null;
  url?: string | null;
  reason?: string | null;
}

/** Tên nguồn ảnh bìa hiện cho cán bộ đọc. */
export const COVER_SOURCE_LABELS: Record<string, string> = {
  Manual: 'Cán bộ tải lên',
  Marc856: 'Trường 856 của biểu ghi',
  GoogleBooks: 'Google Books',
  OpenLibrary: 'Open Library',
};

