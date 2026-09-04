/** Kiểu dữ liệu của Phân hệ III — Bổ sung và Kho. */

export type WarehouseType = 'OpenStack' | 'ClosedStack' | 'ReadingRoom' | 'DiscardStore';

export type ItemStatus =
  | 'PendingInspection'
  | 'InStock'
  | 'OnLoan'
  | 'OnHoldShelf'
  | 'Lost'
  | 'Damaged'
  | 'Discarded'
  | 'UnderInventory';

export type AcquisitionType = 'Purchase' | 'Donation' | 'Exchange' | 'LegalDeposit';

export type PurchaseRequestType = 'Monograph' | 'Serial';

export type PurchaseRequestStatus =
  | 'Draft'
  | 'Submitted'
  | 'Approved'
  | 'PartiallyApproved'
  | 'Rejected'
  | 'Cancelled';

export type PurchaseOrderStatus = 'New' | 'Ordered' | 'PartiallyReceived' | 'Received' | 'Cancelled';

export type InventoryPeriodStatus = 'Preparing' | 'InProgress' | 'Closed';

export type InventoryResultType = 'Match' | 'Missing' | 'Unexpected' | 'WrongWarehouse';

export type BarcodeType = 'Code39' | 'Code128' | 'QrCode';

// ---------------------------------------------------------------------------------------------
// III.3 — Thư viện, kho, giá
// ---------------------------------------------------------------------------------------------

export interface LibraryDto {
  id: string;
  code: string;
  name: string;
  address?: string | null;
  isHeadquarters: boolean;
  isActive: boolean;
}

export interface LibraryDetailDto extends LibraryDto {
  nameEn?: string | null;
  description?: string | null;
  phone?: string | null;
  email?: string | null;
  manager?: string | null;
  openingHours?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  sortOrder: number;
  warehouseCount: number;
  itemCount: number;
}

export interface WarehouseDto {
  id: string;
  code: string;
  name: string;
  libraryId: string;
  libraryName?: string | null;
  type: WarehouseType;
  capacity?: number | null;
  location?: string | null;
  callNumberRule?: string | null;
  isClosedForInventory: boolean;
  isActive: boolean;
  itemCount: number;
}

export interface WarehouseDetailDto extends WarehouseDto {
  nameEn?: string | null;
  description?: string | null;
  sortOrder: number;
  shelfCount: number;
  usagePercent?: number | null;
}

export interface ShelfDto {
  id: string;
  code: string;
  name: string;
  warehouseId: string;
  warehouseName?: string | null;
  capacity?: number | null;
  currentCount: number;
  mapRow?: number | null;
  mapColumn?: number | null;
  callNumberFrom?: string | null;
  callNumberTo?: string | null;
  isActive: boolean;
}

export interface ShelfMapCellDto {
  shelfId: string;
  code: string;
  name: string;
  row: number;
  column: number;
  capacity?: number | null;
  currentCount: number;
  usagePercent?: number | null;
  callNumberFrom?: string | null;
  callNumberTo?: string | null;
  isActive: boolean;
}

export interface ShelfMapDto {
  warehouseId: string;
  warehouseName: string;
  capacity?: number | null;
  itemCount: number;
  rows: number;
  columns: number;
  cells: ShelfMapCellDto[];
  unplaced: ShelfMapCellDto[];
}

// ---------------------------------------------------------------------------------------------
// III.5 — Ấn phẩm trong kho
// ---------------------------------------------------------------------------------------------

export interface StockItemFilter {
  keyword?: string;
  libraryId?: string | null;
  warehouseId?: string | null;
  shelfId?: string | null;
  unshelved?: boolean | null;
  status?: ItemStatus | null;
  isLocked?: boolean | null;
  documentTypeId?: string | null;
  fundingSourceId?: string | null;
  acquisitionType?: AcquisitionType | null;
  orderId?: string | null;
  bibId?: string | null;
  acquiredFrom?: string | null;
  acquiredTo?: string | null;
  registerFrom?: string | null;
  registerTo?: string | null;
  barcodeFrom?: string | null;
  barcodeTo?: string | null;
}

export interface StockItemDto {
  id: string;
  barcode: string;
  registerNumber: string;
  bibId: string;
  title: string;
  authorMain?: string | null;
  isbn?: string | null;
  documentTypeName?: string | null;
  warehouseId: string;
  warehouseName: string;
  shelfId?: string | null;
  shelfName?: string | null;
  callNumber?: string | null;
  price: number;
  fundingSourceName?: string | null;
  acquisitionDate: string;
  acquisitionType: AcquisitionType;
  status: ItemStatus;
  isLocked: boolean;
  lockReason?: string | null;
  condition?: string | null;
  inspectedAt?: string | null;
  copyNumber: number;
  loanCount: number;
  orderCode?: string | null;
  note?: string | null;
}

export interface ItemMovementDto {
  id: string;
  batchCode: string;
  fromWarehouseName?: string | null;
  toWarehouseName?: string | null;
  fromShelfName?: string | null;
  toShelfName?: string | null;
  movementDate: string;
  reason?: string | null;
  decisionNo?: string | null;
  performedByName?: string | null;
}

export interface ItemDisposalDto {
  id: string;
  disposalDate: string;
  disposalType: string;
  reason?: string | null;
  decisionNo?: string | null;
  approvedByName?: string | null;
  value: number;
}

export interface StockItemDetailDto extends StockItemDto {
  libraryName?: string | null;
  controlNumber?: string | null;
  publisherName?: string | null;
  publishYear?: number | null;
  volumeNumber?: string | null;
  lastLoanAt?: string | null;
  movements: ItemMovementDto[];
  disposal?: ItemDisposalDto | null;
}

export interface StockSummaryDto {
  pendingInspection: number;
  inStock: number;
  onLoan: number;
  discarded: number;
  lost: number;
  damaged: number;
  locked: number;
  unshelved: number;
  total: number;
}

export interface BulkItemSkipDto {
  barcode: string;
  reason: string;
}

export interface BulkItemResultDto {
  affected: number;
  skipped: BulkItemSkipDto[];
  documentCode?: string | null;
}

export interface TransferSlipLineDto {
  barcode: string;
  registerNumber: string;
  title: string;
  authorMain?: string | null;
  callNumber?: string | null;
  price: number;
  condition?: string | null;
}

export interface TransferSlipDto {
  batchCode: string;
  movementDate: string;
  fromWarehouseName?: string | null;
  toWarehouseName?: string | null;
  reason?: string | null;
  decisionNo?: string | null;
  performedByName?: string | null;
  itemCount: number;
  totalValue: number;
  lines: TransferSlipLineDto[];
}

// ---------------------------------------------------------------------------------------------
// III.2 — Mẫu tem mã vạch và nhãn gáy
// ---------------------------------------------------------------------------------------------

export interface LabelBoxDto {
  x: number;
  y: number;
  width: number;
  height: number;
  source: string;
  fontSize: number;
  bold: boolean;
  italic: boolean;
  align: 'left' | 'center' | 'right';
  border: boolean;
  prefix?: string | null;
}

export interface LabelBarcodeDto {
  x: number;
  y: number;
  width: number;
  height: number;
  showText: boolean;
  fontSize: number;
  type?: BarcodeType | null;
  source: string;
}

export interface LabelLayoutDto {
  boxes: LabelBoxDto[];
  barcode?: LabelBarcodeDto | null;
  padding: number;
  showBorder: boolean;
}

export interface BarcodeTemplateDto {
  id: string;
  code: string;
  name: string;
  widthMm: number;
  heightMm: number;
  barcodeType: BarcodeType;
  columnsPerPage: number;
  rowsPerPage: number;
  marginTopMm: number;
  marginLeftMm: number;
  isDefault: boolean;
  isActive: boolean;
  layout: LabelLayoutDto;
}

export interface LabelTemplateDto {
  id: string;
  code: string;
  name: string;
  widthMm: number;
  heightMm: number;
  columnsPerPage: number;
  rowsPerPage: number;
  marginTopMm: number;
  marginLeftMm: number;
  isDefault: boolean;
  isActive: boolean;
  layout: LabelLayoutDto;
}

// ---------------------------------------------------------------------------------------------
// III.1 — Yêu cầu đặt mua và đơn đặt
// ---------------------------------------------------------------------------------------------

export interface PurchaseRequestDto {
  id: string;
  code: string;
  type: PurchaseRequestType;
  requesterName: string;
  department?: string | null;
  requestDate: string;
  reason?: string | null;
  fundingSourceId?: string | null;
  fundingSourceName?: string | null;
  status: PurchaseRequestStatus;
  approvalLevel: number;
  requiredLevels: number;
  submittedAt?: string | null;
  approvedByName?: string | null;
  approvedAt?: string | null;
  rejectReason?: string | null;
  totalAmount: number;
  approvedAmount: number;
  lineCount: number;
  totalQuantity: number;
  duplicateCount: number;
}

export interface PurchaseRequestItemDto {
  id: string;
  title: string;
  author?: string | null;
  publisherName?: string | null;
  publishYear?: number | null;
  isbn?: string | null;
  issn?: string | null;
  quantity: number;
  approvedQuantity: number;
  unitPrice: number;
  estimatedAmount: number;
  supplierId?: string | null;
  supplierName?: string | null;
  bibId?: string | null;
  isDuplicate: boolean;
  note?: string | null;
  frequency?: string | null;
  issuesPerYear?: number | null;
  subscriptionFrom?: string | null;
  subscriptionTo?: string | null;
  /** Số kỳ trong thời gian đặt; tài liệu đơn bản luôn là 1. */
  issueCount: number;
  existingCopies: number;
}

export interface PurchaseRequestDetailDto extends PurchaseRequestDto {
  items: PurchaseRequestItemDto[];
  orderCodes: string[];
}

export interface PurchaseDuplicateDto {
  bibId: string;
  controlNumber: string;
  title: string;
  authorMain?: string | null;
  isbn?: string | null;
  publishYear?: number | null;
  itemCount: number;
  availableItemCount: number;
  matchedBy: string;
}

export interface ImportPurchaseLinesResultDto {
  requestId: string;
  imported: number;
  duplicateWarnings: number;
  totalAmount: number;
  errors: { rowNumber: number; message: string }[];
}

export interface PurchaseOrderDto {
  id: string;
  code: string;
  supplierId: string;
  supplierName: string;
  orderDate: string;
  expectedDate?: string | null;
  fundingSourceId?: string | null;
  fundingSourceName?: string | null;
  contractNo?: string | null;
  totalAmount: number;
  status: PurchaseOrderStatus;
  note?: string | null;
  lineCount: number;
  orderedQuantity: number;
  receivedQuantity: number;
  isOverdue: boolean;
  overdueDays: number;
  itemCount: number;
}

export interface PurchaseOrderItemDto {
  id: string;
  requestItemId?: string | null;
  requestCode?: string | null;
  bibId?: string | null;
  controlNumber?: string | null;
  title: string;
  author?: string | null;
  isbn?: string | null;
  quantity: number;
  unitPrice: number;
  receivedQuantity: number;
  note?: string | null;
  createdItemCount: number;
}

export interface HandoverSummaryDto {
  id: string;
  code: string;
  handoverDate: string;
  totalItems: number;
  totalAmount: number;
}

export interface PurchaseOrderDetailDto extends PurchaseOrderDto {
  items: PurchaseOrderItemDto[];
  handovers: HandoverSummaryDto[];
}

export interface HandoverDto {
  id: string;
  code: string;
  orderId?: string | null;
  orderCode?: string | null;
  supplierName?: string | null;
  handoverDate: string;
  partyA: string;
  partyB: string;
  content?: string | null;
  totalItems: number;
  totalAmount: number;
  hasScan: boolean;
  note?: string | null;
}

export interface QuickCatalogResultDto {
  bibId: string;
  controlNumber: string;
  title: string;
  reusedExisting: boolean;
  createdItems: number;
  barcodes: string[];
}

export interface CreateItemsFromOrderResultDto {
  createdItems: number;
  barcodes: string[];
  pendingCataloging: string[];
}

// ---------------------------------------------------------------------------------------------
// III.4 — Kiểm kê
// ---------------------------------------------------------------------------------------------

export interface InventoryPeriodDto {
  id: string;
  code: string;
  name: string;
  warehouseId: string;
  warehouseName: string;
  scopeType: 'ALL' | 'RANGE' | 'DOCTYPE';
  scopeFrom?: string | null;
  scopeTo?: string | null;
  scopeDocumentTypeId?: string | null;
  scopeDocumentTypeName?: string | null;
  startDate: string;
  endDate?: string | null;
  status: InventoryPeriodStatus;
  assignedStaff?: string | null;
  expectedCount: number;
  scannedCount: number;
  closedAt?: string | null;
  note?: string | null;
  warehouseClosed: boolean;
}

export interface InventoryScanResultDto {
  barcode: string;
  outcome: InventoryResultType;
  outcomeName: string;
  title?: string | null;
  registerNumber?: string | null;
  actualWarehouseName?: string | null;
  alreadyScanned: boolean;
  scannedCount: number;
  expectedCount: number;
  message: string;
}

export interface InventorySummaryDto {
  periodId: string;
  code: string;
  name: string;
  warehouseName: string;
  status: InventoryPeriodStatus;
  expectedCount: number;
  scannedCount: number;
  matchCount: number;
  missingCount: number;
  unexpectedCount: number;
  wrongWarehouseCount: number;
  missingValue: number;
  progressPercent: number;
}

export interface InventoryResultRowDto {
  id: string;
  itemId?: string | null;
  barcode: string;
  registerNumber?: string | null;
  title?: string | null;
  authorMain?: string | null;
  callNumber?: string | null;
  price: number;
  result: InventoryResultType;
  resultName: string;
  expectedWarehouseName?: string | null;
  actualWarehouseName?: string | null;
  isResolved: boolean;
  note?: string | null;
}

export interface ImportInventoryScansResultDto {
  total: number;
  match: number;
  unexpected: number;
  wrongWarehouse: number;
  duplicate: number;
  scannedCount: number;
  expectedCount: number;
}

// ---------------------------------------------------------------------------------------------
// III.6 — Mẫu biểu in
// ---------------------------------------------------------------------------------------------

export interface FormFieldOption {
  key: string;
  label: string;
  isRow: boolean;
}

export interface FormTypeMetadataDto {
  formType: string;
  name: string;
  headerFields: FormFieldOption[];
  rowFields: FormFieldOption[];
}

export interface FormFieldDto {
  label: string;
  key: string;
  fullWidth: boolean;
}

export interface FormColumnDto {
  header: string;
  key: string;
  width: number;
  align: 'left' | 'center' | 'right';
  sum: boolean;
}

export interface FormSignatureDto {
  role: string;
  note?: string | null;
}

export interface FormLayoutDto {
  showLogo: boolean;
  organisationLines: string[];
  showNationalHeading: boolean;
  title: string;
  subtitle?: string | null;
  introLines: string[];
  fields: FormFieldDto[];
  columns: FormColumnDto[];
  showTotals: boolean;
  closingLines: string[];
  signatures: FormSignatureDto[];
  footer?: string | null;
  fontSize: number;
}

export interface FormTemplateDto {
  id: string;
  code: string;
  name: string;
  formType: string;
  formTypeName: string;
  paperSize: string;
  isLandscape: boolean;
  customWidthMm?: number | null;
  customHeightMm?: number | null;
  isDefault: boolean;
  isActive: boolean;
  layout: FormLayoutDto;
}

// ---------------------------------------------------------------------------------------------
// III.2 và III.7 — Báo cáo
// ---------------------------------------------------------------------------------------------

export interface AcquisitionReportFilter {
  from?: string | null;
  to?: string | null;
  libraryId?: string | null;
  warehouseId?: string | null;
  fundingSourceId?: string | null;
  acquisitionType?: AcquisitionType | null;
  supplierId?: string | null;
  documentTypeId?: string | null;
}

export interface AcquisitionStatRowDto {
  label: string;
  itemCount: number;
  titleCount: number;
  value: number;
  percent: number;
}

export interface AcquisitionStatReportDto {
  title: string;
  dimensionName: string;
  rows: AcquisitionStatRowDto[];
  totalItems: number;
  totalTitles: number;
  totalValue: number;
}

export interface AcquisitionPivotRowDto {
  label: string;
  values: number[];
  total: number;
}

export interface AcquisitionPivotDto {
  rowDimensionName: string;
  columnDimensionName: string;
  measureName: string;
  columns: string[];
  rows: AcquisitionPivotRowDto[];
  columnTotals: number[];
  grandTotal: number;
}

export interface AcquisitionListRowDto {
  barcode: string;
  registerNumber: string;
  title: string;
  author?: string | null;
  isbn?: string | null;
  documentTypeName?: string | null;
  warehouseName: string;
  fundingSourceName?: string | null;
  supplierName?: string | null;
  orderCode?: string | null;
  acquisitionDate: string;
  acquisitionType: AcquisitionType;
  price: number;
}

export interface DisposalReportRowDto {
  barcode: string;
  registerNumber: string;
  title: string;
  callNumber?: string | null;
  warehouseName: string;
  disposalDate: string;
  disposalType: string;
  reason?: string | null;
  decisionNo?: string | null;
  approvedByName?: string | null;
  value: number;
}

export interface StockOverviewDto {
  totalBibs: number;
  totalItems: number;
  totalValue: number;
  availableItems: number;
  lockedItems: number;
  byWarehouse: AcquisitionStatRowDto[];
  byDocumentType: AcquisitionStatRowDto[];
  byStatus: AcquisitionStatRowDto[];
}

export interface PurchaseApprovalReportDto {
  totalRequests: number;
  approvedRequests: number;
  rejectedRequests: number;
  pendingRequests: number;
  requestedAmount: number;
  approvedAmount: number;
  approvalRate: number;
  byStatus: AcquisitionStatRowDto[];
  byDepartment: AcquisitionStatRowDto[];
  byMonth: AcquisitionStatRowDto[];
}

export interface SupplierOrderRowDto {
  code: string;
  orderDate: string;
  expectedDate?: string | null;
  status: PurchaseOrderStatus;
  orderedQuantity: number;
  receivedQuantity: number;
  totalAmount: number;
}

export interface SupplierHistoryDto {
  supplierId: string;
  supplierName: string;
  /** Đánh giá đã chấm ở danh mục nhà cung cấp, 0 là chưa chấm, tối đa 5 sao. */
  rating: number;
  orderCount: number;
  totalAmount: number;
  itemCount: number;
  fulfilmentRate: number;
  lateOrders: number;
  orders: SupplierOrderRowDto[];
}
