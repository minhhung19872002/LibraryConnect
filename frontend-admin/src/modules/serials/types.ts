/** Kiểu dữ liệu của Phân hệ IV — Ấn phẩm định kỳ. */

export type SerialFrequency =
  | 'Daily'
  | 'Weekly'
  | 'Biweekly'
  | 'SemiMonthly'
  | 'Monthly'
  | 'Bimonthly'
  | 'Quarterly'
  | 'SemiAnnual'
  | 'Annual'
  | 'Irregular';

export type SerialIssueStatus = 'Expected' | 'Received' | 'Missing' | 'Claimed' | 'Bound';

export type SerialClaimStatus = 'Open' | 'Responded' | 'Resolved' | 'Cancelled';

export type SerialNumbering = 'Continuous' | 'RestartEachYear' | 'VolumeAndIssue';

/** Khai báo kỳ hạn xuất bản — thứ quyết định hệ thống đoán ra những số nào sẽ đến. */
export interface SerialPatternDto {
  issuesPerYear?: number | null;
  dayOfWeek?: number | null;
  dayOfMonth?: number | null;
  secondDayOfMonth?: number | null;
  numbering: SerialNumbering;
  startIssueNumber: number;
  startVolume: number;
  startYear?: number | null;
  skipMonths: number[];
}

export interface SerialDto {
  id: string;
  bibId: string;
  controlNumber?: string | null;
  title: string;
  issn?: string | null;
  publisherId?: string | null;
  publisherName?: string | null;
  languageId?: string | null;
  languageName?: string | null;
  supplierId?: string | null;
  supplierName?: string | null;
  frequency: SerialFrequency;
  warehouseId?: string | null;
  warehouseName?: string | null;
  shelfId?: string | null;
  shelfName?: string | null;
  callNumber?: string | null;
  subscriptionStart?: string | null;
  subscriptionEnd?: string | null;
  pricePerIssue?: number | null;
  copiesPerIssue: number;
  isActive: boolean;
  note?: string | null;
  expectedCount: number;
  receivedCount: number;
  missingCount: number;
  subscriptionEndingSoon: boolean;
}

export interface SerialDetailDto extends SerialDto {
  pattern: SerialPatternDto;
}

export interface SerialIssueDto {
  id: string;
  serialId: string;
  serialTitle: string;
  issueNo: string;
  volume?: string | null;
  year: number;
  caption?: string | null;
  expectedDate: string;
  receivedDate?: string | null;
  receivedByName?: string | null;
  quantity: number;
  status: SerialIssueStatus;
  barcode?: string | null;
  warehouseId?: string | null;
  warehouseName?: string | null;
  bindingId?: string | null;
  /** Tình trạng vật lý lúc nhận (IV.4). */
  condition?: string | null;
  note?: string | null;
  articleCount: number;
  isOverdue: boolean;
  hasOpenClaim: boolean;
}

export interface IssuePreviewDto {
  issueNo: string;
  volume?: string | null;
  year: number;
  expectedDate: string;
  caption: string;
}

export interface GenerateIssuesResultDto {
  created: number;
  skipped: number;
  captions: string[];
}

export interface ReceiveIssuesResultDto {
  received: number;
  createdItems: number;
  barcodes: string[];
  skipped: string[];
}

export interface IssueGridCellDto {
  issueId: string;
  issueNo: string;
  volume?: string | null;
  expectedDate: string;
  receivedDate?: string | null;
  status: SerialIssueStatus;
  isOverdue: boolean;
}

export interface IssueGridYearDto {
  year: number;
  expected: number;
  received: number;
  missing: number;
  bound: number;
  cells: IssueGridCellDto[];
}

export interface SerialSummaryRowDto {
  year: number;
  planned: number;
  received: number;
  missing: number;
  bound: number;
  value: number;
  receivedPercent: number;
}

export interface SerialClaimDto {
  id: string;
  issueId: string;
  claimNo: string;
  claimDate: string;
  serialTitle: string;
  issueCaption?: string | null;
  supplierId?: string | null;
  supplierName?: string | null;
  content?: string | null;
  response?: string | null;
  responseDate?: string | null;
  status: SerialClaimStatus;
}

export interface CreateClaimsResultDto {
  created: number;
  claimNumbers: string[];
  skipped: string[];
}

export interface SerialArticleDto {
  id: string;
  issueId: string;
  title: string;
  authors?: string | null;
  pageFrom?: number | null;
  pageTo?: number | null;
  abstract?: string | null;
  keywords?: string | null;
  bibId?: string | null;
  controlNumber?: string | null;
}

export interface GenerateArticleRecordsResultDto {
  created: number;
  skipped: number;
  controlNumbers: string[];
}

export interface ImportArticlesResultDto {
  imported: number;
  errors: { rowNumber: number; message: string }[];
}

export interface SerialBindingDto {
  id: string;
  serialId: string;
  serialTitle: string;
  code: string;
  fromIssue?: string | null;
  toIssue?: string | null;
  year: number;
  bindingDate: string;
  issueCount: number;
  itemId?: string | null;
  barcode?: string | null;
  callNumber?: string | null;
  note?: string | null;
}

export interface SerialReportFilter {
  year?: number | null;
  warehouseId?: string | null;
  supplierId?: string | null;
  frequency?: SerialFrequency | null;
  activeOnly?: boolean | null;
}

export interface SerialStatRowDto {
  label: string;
  titleCount: number;
  receivedIssues: number;
  missingIssues: number;
  copies: number;
  value: number;
  percent: number;
}

export interface SerialStatReportDto {
  title: string;
  dimensionName: string;
  rows: SerialStatRowDto[];
  totalTitles: number;
  totalReceivedIssues: number;
  totalMissingIssues: number;
  totalValue: number;
}
