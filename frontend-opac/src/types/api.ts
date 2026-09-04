/**
 * Các kiểu dữ liệu dùng chung với máy chủ.
 *
 * Chép đúng hình dạng của lớp bao ở mục 11 đặc tả, để một thay đổi ở máy chủ bị trình biên dịch bắt
 * ngay thay vì tới lúc chạy mới hỏng màn hình.
 */

export interface ApiError {
  field: string;
  message: string;
  code?: string;
}

export interface ApiResponse<T = unknown> {
  success: boolean;
  data?: T;
  message: string;
  errors: ApiError[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  /** Đúng khi totalCount là chặn trên: kết quả nhiều hơn con số đó, máy chủ dừng đếm cho nhanh. */
  totalCountCapped?: boolean;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface AuthUser {
  id: string;
  username: string;
  fullName: string;
  email?: string;
  isReader: boolean;
  groups: string[];
  permissions: string[];
}

export interface AuthResult {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: AuthUser;
  mustChangePassword: boolean;
}

/** Thông tin thư viện và tùy chọn hiển thị, lấy một lần khi mở trang. */
/** Một cơ sở của thư viện. */
export interface PublicBranch {
  id: string;
  name: string;
  address?: string | null;
  phone?: string | null;
  openingHours?: string | null;
  isHeadquarters: boolean;
  latitude?: number | null;
  longitude?: number | null;
}

export interface SiteSettings {
  libraryName: string;
  libraryNameEn?: string;
  address?: string;
  phone?: string;
  email?: string;
  website?: string;
  logoUrl?: string;
  showPoweredBy: boolean;
  opacPageSize: number;
  allowHold: boolean;
  allowReview: boolean;
  slogan?: string;
  faviconUrl?: string;
  heroImageUrl?: string;
  footerText?: string;
  openingHours?: string;
  /** Từng cơ sở kèm giờ mở cửa của chính nó, do màn hình Thư viện nhập (VIII.1). */
  branches: PublicBranch[];
  contactNote?: string;
  mapEmbedUrl?: string;
  facebook?: string;
  youtube?: string;
  zalo?: string;
  newsPerPage: number;
  showNewBooks: boolean;
  showPopularBooks: boolean;
  showInterlibrary: boolean;
}

export interface MenuItem {
  id: string;
  name: string;
  url?: string;
  parentId?: string;
  sortOrder: number;
  target?: string;
  icon?: string;
  isActive: boolean;
  children: MenuItem[];
}

export type SearchScope =
  | 'All'
  | 'Title'
  | 'Author'
  | 'Subject'
  | 'Keyword'
  | 'Publisher'
  | 'Isbn'
  | 'CallNumber';

export type SortOrder = 'Relevance' | 'Newest' | 'Title' | 'Author' | 'Popular';

export type Connector = 'And' | 'Or' | 'Not';

export interface SearchFilter {
  publishYearFrom?: number;
  publishYearTo?: number;
  languageId?: string;
  documentTypeId?: string;
  authorId?: string;
  subjectId?: string;
  collectionId?: string;
  publisherId?: string;
  warehouseId?: string;
  courseId?: string;
  ddc?: string;
  hasDigital?: boolean;
  availableOnly?: boolean;
}

export interface SearchClause {
  connector: Connector;
  field: SearchScope;
  term: string;
}

export interface SearchResult {
  id: string;
  controlNumber: string;
  title: string;
  subtitle?: string;
  authorMain?: string;
  publisherName?: string;
  publishYear?: number;
  isbn?: string;
  ddc?: string;
  documentTypeName?: string;
  languageName?: string;
  coverImageUrl?: string;
  abstract?: string;
  itemCount: number;
  availableItemCount: number;
  digitalDocumentCount: number;
  loanCount: number;
}

export interface FacetValue {
  id?: string;
  label: string;
  count: number;
}

export interface FacetGroup {
  code: string;
  name: string;
  values: FacetValue[];
}

export interface Suggestion {
  text: string;
  type: string;
  count: number;
}

export interface LinkedTerm {
  id?: string;
  name: string;
  note?: string;
}

export interface BibItem {
  id: string;
  barcode: string;
  registerNumber: string;
  callNumber?: string;
  libraryName: string;
  warehouseName: string;
  shelfName?: string;
  statusLabel: string;
  isAvailable: boolean;
  dueDate?: string;
}

export interface DigitalDocumentSummary {
  id: string;
  title: string;
  fileName: string;
  mimeType?: string;
  fileSize: number;
  pageCount?: number;
  accessLevelLabel: string;
  requiresRequest: boolean;
  allowDownload: boolean;
}

export interface BibReview {
  id: string;
  readerName: string;
  rating: number;
  comment?: string;
  createdAt: string;
}

/** Bản toàn văn nằm ở máy chủ khác, lấy từ trường MARC 856. */
export interface BibExternalLink {
  url: string;
  label?: string | null;
  note?: string | null;
  mimeType?: string | null;
}

export interface BibDetail {
  id: string;
  controlNumber: string;
  title: string;
  subtitle?: string;
  statementOfResponsibility?: string;
  authorMain?: string;
  authors: LinkedTerm[];
  subjects: LinkedTerm[];
  keywords: LinkedTerm[];
  classifications: LinkedTerm[];
  publisherName?: string;
  publishPlace?: string;
  publishYear?: number;
  edition?: string;
  pages?: string;
  dimensions?: string;
  isbn?: string;
  issn?: string;
  ddc?: string;
  seriesName?: string;
  languageName?: string;
  documentTypeName?: string;
  abstract?: string;
  coverImageUrl?: string;
  isbd: string;
  marcJson: string;
  itemCount: number;
  availableItemCount: number;
  items: BibItem[];
  digitalDocuments: DigitalDocumentSummary[];
  externalLinks: BibExternalLink[];
  reviews: BibReview[];
  averageRating?: number;
  related: SearchResult[];
}

export interface BrowseEntry {
  id?: string;
  code: string;
  name: string;
  bibCount: number;
  parentId?: string;
  hasChildren: boolean;
}

export interface CourseDocument {
  relationLabel: string;
  note?: string;
  bib: SearchResult;
}

export interface SerialSummary {
  id: string;
  bibId?: string;
  title: string;
  issn?: string;
  publisherName?: string;
  frequencyLabel: string;
  warehouseName?: string;
  receivedIssueCount: number;
  latestIssueDate?: string;
  latestIssueNo?: string;
}

export interface NewsSummary {
  id: string;
  title: string;
  slug: string;
  summary?: string;
  thumbnailUrl?: string;
  categoryName?: string;
  isFeatured: boolean;
  publishedAt?: string;
}

export interface NewsDetail extends NewsSummary {
  content?: string;
  categoryId?: string;
  tags?: string;
  author?: string;
  viewCount: number;
  related: NewsSummary[];
}

export interface NewsCategory {
  id: string;
  code: string;
  name: string;
  newsCount: number;
}

export interface StaticPage {
  id: string;
  slug: string;
  title: string;
  content?: string;
  metaDescription?: string;
  isPublished: boolean;
  publishedAt?: string;
  viewCount: number;
  sortOrder: number;
  parentId?: string;
}

export interface HomeBanner {
  id: string;
  title: string;
  imageUrl: string;
  link?: string;
}

export interface HomeLink {
  id: string;
  name: string;
  url: string;
  logoUrl?: string;
  groupName?: string;
}

export interface HomeStatistics {
  bibCount: number;
  itemCount: number;
  digitalCount: number;
  readerCount: number;
}

export interface HomePayload {
  newBooks: SearchResult[];
  popularBooks: SearchResult[];
  news: NewsSummary[];
  /** Bản tin thuộc chuyên mục "Thông báo", mới nhất trước (IX.1). */
  announcements: NewsSummary[];
  banners: HomeBanner[];
  links: HomeLink[];
  statistics: HomeStatistics;
}

/** Một ảnh trong album sự kiện (VIII.2). */
export interface GalleryImage {
  id: string;
  imageUrl: string;
  caption?: string;
  sortOrder: number;
}

/** Album ảnh sự kiện đã đăng, xem ở trang Thư viện ảnh. */
export interface Gallery {
  id: string;
  title: string;
  description?: string;
  coverUrl?: string;
  eventDate?: string;
  isPublished: boolean;
  images: GalleryImage[];
}

export interface ReaderProfile {
  id: string;
  cardNumber: string;
  studentCode?: string;
  fullName: string;
  gender?: string;
  dateOfBirth?: string;
  email?: string;
  phone?: string;
  address?: string;
  photoUrl?: string;
  readerTypeName: string;
  facultyName?: string;
  majorName?: string;
  className?: string;
  courseYear?: string;
  cardIssueDate: string;
  cardExpireDate: string;
  statusLabel: string;
  mustChangePassword: boolean;
  currentLoanCount: number;
  debtAmount: number;
}

/** Một yêu cầu đọc tài liệu hạn chế của chính bạn đọc đang đăng nhập (V.2, IX.3). */
export interface DigitalRequestRow {
  id: string;
  documentId: string;
  documentTitle: string;
  requestDate: string;
  reason?: string | null;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Expired';
  approvedAt?: string | null;
  expireAt?: string | null;
  rejectReason?: string | null;
  maxViews?: number | null;
  viewCount: number;
  allowDownload: boolean;
}

export interface LoanRow {
  id: string;
  code: string;
  itemId: string;
  barcode?: string;
  title?: string;
  callNumber?: string;
  warehouseName?: string;
  loanDate: string;
  dueDate: string;
  returnDate?: string;
  renewedCount: number;
  maxRenewals: number;
  status: 'Active' | 'Returned' | 'Overdue' | 'Lost' | 'Damaged';
  loanType: string;
  channel: string;
  fineAmount: number;
  fineOutstanding: number;
  overdueDays: number;
  estimatedFine: number;
  /** Yêu cầu gia hạn đã gửi, đang chờ cán bộ duyệt; hạn trả vẫn là hạn cũ. */
  renewalPending?: boolean;
  note?: string;
}

export interface HoldRow {
  id: string;
  bibId: string;
  title?: string;
  itemId?: string;
  barcode?: string;
  holdDate: string;
  expireDate?: string;
  pickupWarehouseId?: string;
  pickupWarehouseName?: string;
  status: 'Waiting' | 'Ready' | 'Fulfilled' | 'Expired' | 'Cancelled';
  queuePosition: number;
  notifiedAt?: string;
}

export interface FineRow {
  id: string;
  code: string;
  loanId?: string;
  loanCode?: string;
  title?: string;
  barcode?: string;
  type: 'Overdue' | 'Lost' | 'Damaged' | 'Other';
  amount: number;
  paidAmount: number;
  outstanding: number;
  waived: boolean;
  waiveReason?: string;
  paidAt?: string;
  createdAt: string;
  note?: string;
}

/** Tổng hợp tiền phạt của một bạn đọc, kèm danh sách khoản phạt. */
export interface FineSummary {
  readerId: string;
  cardNumber: string;
  fullName: string;
  totalOutstanding: number;
  totalPaid: number;
  totalWaived: number;
  fines: FineRow[];
}

export interface NotificationRow {
  id: string;
  type: string;
  title: string;
  body?: string;
  link?: string;
  isRead: boolean;
  createdAt: string;
}

export interface CardRenewalRow {
  id: string;
  requestDate: string;
  reason?: string;
  statusLabel: string;
  processedAt?: string;
  newExpireDate?: string;
  rejectReason?: string;
}

export interface SavedSearch {
  id: string;
  name: string;
  query: string;
  alertEnabled: boolean;
  createdAt: string;
}

export interface CirculationWarning {
  code: string;
  message: string;
  blocking: boolean;
}

export interface CardInfo {
  readerId: string;
  cardNumber: string;
  fullName: string;
  studentCode?: string;
  readerTypeName?: string;
  facultyName?: string;
  className?: string;
  cardIssueDate: string;
  cardExpireDate: string;
  status: string;
  canBorrow: boolean;
  barcodeValue: string;
  currentLoanCount: number;
  outstandingFines: number;
  warnings: CirculationWarning[];
}

/** Một tài liệu số bạn đọc nhìn thấy trên trang tra cứu. */
export interface DigitalDocumentRow {
  id: string;
  title: string;
  fileName: string;
  mimeType: string;
  fileSize: number;
  pageCount?: number;
  collectionId?: string;
  collectionName?: string;
  bibId?: string;
  bibTitle?: string;
  accessLevel: 'Public' | 'Internal' | 'Restricted' | 'Forbidden';
  allowDownload: boolean;
  allowPrint: boolean;
  previewPages: number;
  viewCount: number;
  downloadCount: number;
}

/** Quyền đọc của chính bạn đọc đối với một tài liệu số. */
export interface DigitalPermission {
  canRead: boolean;
  canDownload: boolean;
  canPrint: boolean;
  /** Số trang được xem; bỏ trống nghĩa là xem hết. */
  readablePages?: number;
  needsRequest: boolean;
  requestStatus?: 'Pending' | 'Approved' | 'Rejected' | 'Expired' | 'Revoked';
  accessExpireAt?: string;
  reason: string;
}

export interface DigitalDocumentDetail {
  document: DigitalDocumentRow;
  description?: string;
  checksumSha256?: string;
  files: { type: string; path: string; size: number }[];
  permission: DigitalPermission;
}

export interface DigitalReaderSession {
  documentId: string;
  title: string;
  pageCount?: number;
  readablePages?: number;
  canDownload: boolean;
  canPrint: boolean;
  watermarkEnabled: boolean;
  mimeType: string;
  reason: string;
}

export interface RemoteSearchRecord {
  title?: string;
  author?: string;
  publisher?: string;
  publishYear?: string;
  isbn?: string;
  marcJson: string;
  existingBibId?: string;
  existingBibTitle?: string;
}

export interface RemoteTargetResult {
  targetId: string;
  targetName: string;
  success: boolean;
  message?: string;
  totalHits: number;
  durationMs: number;
  records: RemoteSearchRecord[];
}

export interface RemoteSearchResult {
  targets: RemoteTargetResult[];
  totalHits: number;
  fetchedCount: number;
}

/** Bộ lọc của trang Tài liệu số (IX.4). */
export interface DigitalFilter {
  collectionId?: string;
  formatGroup?: string;
  accessLevel?: string;
  fullText?: boolean;
}

/** Một nhánh của cây bộ sưu tập tài liệu số. */
export interface DigitalCollectionNode {
  id: string;
  code: string;
  name: string;
  parentId: string | null;
  documentCount: number;
  children: DigitalCollectionNode[];
}
