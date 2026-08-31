/** Phân hệ V — Tài liệu số. Các kiểu dữ liệu khớp với DTO của máy chủ. */

export type DigitalAccessLevel = 'Public' | 'Internal' | 'Restricted' | 'Forbidden';

export type DigitalFileType = 'Original' | 'Preview' | 'Thumbnail' | 'OcrText';

export type DigitalAccessAction = 'View' | 'Download' | 'Print';

export type AccessRequestStatus = 'Pending' | 'Approved' | 'Rejected' | 'Expired' | 'Revoked';

export interface DigitalCollectionDto {
  id: string;
  code: string;
  name: string;
  nameEn: string | null;
  parentId: string | null;
  parentName: string | null;
  description: string | null;
  defaultAccessLevel: DigitalAccessLevel;
  sortOrder: number;
  isActive: boolean;
  documentCount: number;
  children: DigitalCollectionDto[];
}

export interface DigitalDocumentRowDto {
  id: string;
  title: string;
  fileName: string;
  mimeType: string;
  fileSize: number;
  pageCount: number | null;
  collectionId: string | null;
  collectionName: string | null;
  bibId: string | null;
  bibTitle: string | null;
  accessLevel: DigitalAccessLevel;
  allowDownload: boolean;
  allowPrint: boolean;
  watermarkEnabled: boolean;
  previewPages: number;
  hasThumbnail: boolean;
  hasText: boolean;
  ocrProcessed: boolean;
  viewCount: number;
  downloadCount: number;
  uploadByName: string | null;
  uploadAt: string;
  snippet: string | null;
}

export interface DigitalPermissionDto {
  canRead: boolean;
  canDownload: boolean;
  canPrint: boolean;
  readablePages: number | null;
  needsRequest: boolean;
  requestStatus: AccessRequestStatus | null;
  accessExpireAt: string | null;
  reason: string;
}

export interface DigitalDocumentFileDto {
  id: string;
  type: DigitalFileType;
  path: string;
  size: number;
  mimeType: string | null;
  pageNumber: number | null;
}

export interface DigitalDocumentDetailDto {
  document: DigitalDocumentRowDto;
  description: string | null;
  checksumSha256: string | null;
  files: DigitalDocumentFileDto[];
  permission: DigitalPermissionDto;
}

export interface DigitalDocumentFilter {
  collectionId?: string;
  includeDescendants?: boolean;
  bibId?: string;
  accessLevel?: DigitalAccessLevel;
  formatGroup?: string;
  hasText?: boolean;
  fullText?: boolean;
  uploadedFrom?: string;
  uploadedTo?: string;
}

export interface DigitalReaderSessionDto {
  documentId: string;
  title: string;
  pageCount: number | null;
  readablePages: number | null;
  canDownload: boolean;
  canPrint: boolean;
  watermarkEnabled: boolean;
  mimeType: string;
  reason: string;
}

export interface DigitalUploadSessionDto {
  id: string;
  fileName: string;
  totalSize: number;
  chunkSize: number;
  totalChunks: number;
  receivedChunks: number[];
  missingChunks: number[];
  isCompleted: boolean;
  documentId: string | null;
  expiresAt: string;
}

export interface DigitalAccessRequestRowDto {
  id: string;
  documentId: string;
  documentTitle: string;
  readerId: string;
  readerName: string;
  readerCardNumber: string;
  readerTypeName: string | null;
  facultyName: string | null;
  requestDate: string;
  reason: string | null;
  status: AccessRequestStatus;
  approvedByName: string | null;
  approvedAt: string | null;
  expireAt: string | null;
  rejectReason: string | null;
  maxViews: number | null;
  viewCount: number;
  allowDownload: boolean;
  processingHours: number | null;
}

export interface DigitalAccessLogRowDto {
  id: string;
  documentId: string;
  documentTitle: string;
  readerId: string | null;
  readerName: string | null;
  readerCardNumber: string | null;
  userName: string | null;
  action: DigitalAccessAction;
  ip: string | null;
  device: string | null;
  pageFrom: number | null;
  pageTo: number | null;
  durationSeconds: number | null;
  occurredAt: string;
}

export interface DigitalImportRowDto {
  fileName: string;
  success: boolean;
  message: string;
  documentId: string | null;
}

export interface DigitalImportResultDto {
  total: number;
  success: number;
  failed: number;
  rows: DigitalImportRowDto[];
}

export interface DigitalReportFilter {
  fromDate?: string;
  toDate?: string;
  collectionId?: string;
  top?: number;
  groupBy?: string;
}

export interface DigitalCountRowDto {
  label: string;
  count: number;
  totalSize: number;
}

export interface DigitalUsageRowDto {
  label: string;
  views: number;
  downloads: number;
}

export interface DigitalTopDocumentRowDto {
  documentId: string;
  title: string;
  collectionName: string | null;
  views: number;
  downloads: number;
}

export interface DigitalTopReaderRowDto {
  readerId: string;
  readerName: string;
  cardNumber: string;
  views: number;
  downloads: number;
}

export interface DigitalInventoryReportDto {
  totalDocuments: number;
  totalSize: number;
  withText: number;
  ocrProcessed: number;
  byCollection: DigitalCountRowDto[];
  byFormat: DigitalCountRowDto[];
  byAccessLevel: DigitalCountRowDto[];
}

export interface DigitalUsageReportDto {
  totalViews: number;
  totalDownloads: number;
  byPeriod: DigitalUsageRowDto[];
  topDocuments: DigitalTopDocumentRowDto[];
  topReaders: DigitalTopReaderRowDto[];
}

export interface DigitalStorageReportDto {
  totalSize: number;
  originalSize: number;
  derivedSize: number;
  fileCount: number;
  byFormat: DigitalCountRowDto[];
}

export interface DigitalRequestReportDto {
  total: number;
  pending: number;
  approved: number;
  rejected: number;
  expired: number;
  averageProcessingHours: number;
  byPeriod: DigitalUsageRowDto[];
}
