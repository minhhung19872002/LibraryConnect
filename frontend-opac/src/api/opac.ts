import { api, http, tokenStorage } from '@/api/client';
import { buildReadingTimeRequest } from '@/lib/readingTime';
import type {
  AuthResult,
  BibDetail,
  BrowseEntry,
  CardInfo,
  CardRenewalRow,
  CourseDocument,
  FacetGroup,
  DigitalDocumentDetail,
  DigitalDocumentRow,
  DigitalReaderSession,
  FineSummary,
  Gallery,
  HoldRow,
  HomePayload,
  LoanRow,
  MenuItem,
  NewsCategory,
  NewsDetail,
  NewsSummary,
  NotificationRow,
  PagedResult,
  ReaderProfile,
  SavedSearch,
  SearchClause,
  SearchFilter,
  SearchResult,
  SearchScope,
  SerialSummary,
  SiteSettings,
  SortOrder,
  StaticPage,
  Suggestion,
  DigitalFilter,
  DigitalCollectionNode,
} from '@/types/api';

/** Tham số của một lần tra cứu cơ bản. */
export interface SearchParams {
  keyword?: string;
  scope?: SearchScope;
  sort?: SortOrder;
  page?: number;
  pageSize?: number;
  filter?: SearchFilter;
}

/**
 * Trải bộ lọc thành tham số phẳng.
 *
 * Máy chủ nhận bộ lọc dưới dạng đối tượng lồng (Filter.LanguageId), mà chuỗi truy vấn thì phẳng —
 * nên phải viết tên khóa đúng dạng "filter.languageId". Bỏ hẳn giá trị rỗng để địa chỉ trên thanh
 * trình duyệt còn đọc được.
 */
export function toQuery(params: SearchParams): Record<string, string | number | boolean> {
  const query: Record<string, string | number | boolean> = {};

  if (params.keyword) query.keyword = params.keyword;
  if (params.scope) query.scope = params.scope;
  if (params.sort) query.sort = params.sort;
  if (params.page) query.page = params.page;
  if (params.pageSize) query.pageSize = params.pageSize;

  Object.entries(params.filter ?? {}).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      query[`filter.${key}`] = value as string | number | boolean;
    }
  });

  return query;
}

export const opacApi = {
  // ---- Thông tin trang ----
  settings: () => api.get<SiteSettings>('/public/settings'),
  home: () => api.get<HomePayload>('/public/home'),
  menus: () => api.get<MenuItem[]>('/public/menus'),
  galleries: () => api.get<Gallery[]>('/public/galleries'),

  // ---- Tra cứu ----
  search: (params: SearchParams) =>
    api.get<PagedResult<SearchResult>>('/search', { params: toQuery(params) }),

  advancedSearch: (body: {
    clauses: SearchClause[];
    sort?: SortOrder;
    page?: number;
    pageSize?: number;
    filter?: SearchFilter;
  }) => api.post<PagedResult<SearchResult>>('/search/advanced', body),

  suggest: (term: string) =>
    api.get<Suggestion[]>('/search/suggest', { params: { term, limit: 8 } }),

  facets: (params: SearchParams) =>
    api.get<FacetGroup[]>('/search/facets', { params: toQuery(params) }),

  bib: (id: string) => api.get<BibDetail>(`/bib/${id}`),

  citation: (id: string, style: string) =>
    api.get<{ style: string; content: string; fileName?: string; contentType: string }>(
      `/bib/${id}/citation`,
      { params: { style } },
    ),

  // ---- Duyệt danh mục ----
  // Mọi nhánh duyệt đều nhận `letter` để lọc theo chữ cái đầu (IX.2 — "dạng cây và A-Z").
  browseSubjects: (parentId?: string, letter?: string) =>
    api.get<BrowseEntry[]>('/browse/subjects', { params: { ...(parentId ? { parentId } : {}), ...(letter ? { letter } : {}) } }),
  browseAuthors: (letter?: string) =>
    api.get<BrowseEntry[]>('/browse/authors', { params: letter ? { letter } : {} }),
  browseClassifications: (parentId?: string, letter?: string) =>
    api.get<BrowseEntry[]>('/browse/classifications', { params: { ...(parentId ? { parentId } : {}), ...(letter ? { letter } : {}) } }),
  browseCollections: (letter?: string) =>
    api.get<BrowseEntry[]>('/browse/collections', { params: letter ? { letter } : {} }),
  browseMajors: (letter?: string) =>
    api.get<BrowseEntry[]>('/browse/majors', { params: letter ? { letter } : {} }),
  browseCourses: (majorId?: string, letter?: string) =>
    api.get<BrowseEntry[]>('/browse/courses', { params: { ...(majorId ? { majorId } : {}), ...(letter ? { letter } : {}) } }),
  courseDocuments: (majorId: string, courseId: string, page = 1) =>
    api.get<PagedResult<CourseDocument>>(
      `/browse/majors/${majorId}/courses/${courseId}/documents`,
      { params: { page, pageSize: 20 } },
    ),
  theses: (params: SearchParams) =>
    api.get<PagedResult<SearchResult>>('/browse/theses', { params: toQuery(params) }),
  serials: (page = 1, keyword?: string) =>
    api.get<PagedResult<SerialSummary>>('/browse/serials', {
      params: { page, pageSize: 20, ...(keyword ? { keyword } : {}) },
    }),

  // ---- Nội dung ----
  news: (params: { page?: number; pageSize?: number; categoryId?: string; keyword?: string }) =>
    api.get<PagedResult<NewsSummary>>('/public/news', { params }),
  newsCategories: () => api.get<NewsCategory[]>('/public/news/categories'),
  newsDetail: (slug: string) => api.get<NewsDetail>(`/public/news/${slug}`),
  pages: () => api.get<StaticPage[]>('/public/pages'),
  page: (slug: string) => api.get<StaticPage>(`/public/pages/${slug}`),

  // ---- Liên thư viện ----
  remoteTargets: () => api.get<{ id: string; name: string }[]>('/public/interlibrary/targets'),
  remoteSearch: (body: { term: string; field: string; targetIds: string[] }) =>
    api.post<{
      targets: {
        targetId: string;
        targetName: string;
        success: boolean;
        message?: string;
        totalHits: number;
        durationMs: number;
        records: {
          sourceName: string;
          title?: string;
          author?: string;
          publisher?: string;
          publishYear?: string;
          isbn?: string;
          edition?: string;
          pages?: string;
          existingBibId?: string;
          existingBibTitle?: string;
        }[];
      }[];
      totalHits: number;
      fetchedCount: number;
    }>('/public/interlibrary/search', body),
};

/** Nhóm endpoint cần đăng nhập bằng tài khoản bạn đọc. */
export const readerApi = {
  login: (cardNumber: string, password: string) =>
    api.post<AuthResult>('/reader/auth/login', { cardNumber, password }),

  changePassword: (currentPassword: string, newPassword: string) =>
    api.post<void>('/reader/auth/change-password', { currentPassword, newPassword }),

  profile: () => api.get<ReaderProfile>('/reader/profile'),
  updateProfile: (body: { email?: string; phone?: string; address?: string }) =>
    api.put<void>('/reader/profile', body),

  card: () => api.get<CardInfo>('/reader/card'),
  requestCardRenewal: (reason?: string) =>
    api.post<string>('/reader/card/renew-request', { reason }),
  cardRenewals: () => api.get<CardRenewalRow[]>('/reader/card/renew-requests'),

  currentLoans: () => api.get<PagedResult<LoanRow>>('/reader/loans/current', {
    params: { page: 1, pageSize: 50 },
  }),
  loanHistory: (page = 1) =>
    api.get<PagedResult<LoanRow>>('/reader/loans/history', { params: { page, pageSize: 20 } }),
  renewLoan: (id: string) => api.post<LoanRow>(`/reader/loans/${id}/renew`),

  holds: () => api.get<PagedResult<HoldRow>>('/reader/holds', { params: { page: 1, pageSize: 50 } }),
  createHold: (body: { bibId: string; itemId?: string; pickupWarehouseId?: string }) =>
    api.post<HoldRow>('/reader/holds', body),
  cancelHold: (id: string) => api.delete<void>(`/reader/holds/${id}`),

  fines: () => api.get<FineSummary>('/reader/fines', { params: { page: 1, pageSize: 50 } }),

  notifications: (unreadOnly = false) =>
    api.get<PagedResult<NotificationRow>>('/reader/notifications', {
      params: { page: 1, pageSize: 50, unreadOnly },
    }),
  markNotificationRead: (id: string) => api.post<void>(`/reader/notifications/${id}/read`),
  markAllNotificationsRead: () => api.post<void>('/reader/notifications/read-all'),

  favorites: (page = 1) =>
    api.get<PagedResult<SearchResult>>('/reader/favorites', { params: { page, pageSize: 20 } }),
  toggleFavorite: (bibId: string) => api.post<boolean>(`/reader/favorites/${bibId}`),

  savedSearches: () => api.get<SavedSearch[]>('/reader/saved-searches'),
  saveSearch: (name: string, query: string) =>
    api.post<string>('/reader/saved-searches', { name, query, alertEnabled: false }),
  deleteSavedSearch: (id: string) => api.delete<void>(`/reader/saved-searches/${id}`),

  submitReview: (bibId: string, rating: number, comment?: string) =>
    api.post<string>('/reader/reviews', { bibId, rating, comment }),

  emailCart: (bibIds: string[], note?: string) =>
    api.post<string>('/reader/cart/email', { bibIds, note }),

  digitalDocuments: (page = 1, keyword?: string, filter?: DigitalFilter) =>
    api.post<PagedResult<DigitalDocumentRow>>('/reader/digital/search', {
      page,
      pageSize: 20,
      ...(keyword ? { keyword } : {}),
      filter: {
        ...(filter?.collectionId ? { collectionId: filter.collectionId } : {}),
        ...(filter?.formatGroup ? { formatGroup: filter.formatGroup } : {}),
        ...(filter?.accessLevel ? { accessLevel: filter.accessLevel } : {}),
        ...(filter?.fullText ? { fullText: true } : {}),
      },
    }),

  digitalCollections: () =>
    api.get<DigitalCollectionNode[]>('/reader/digital/collections'),

  digitalDocument: (id: string) => api.get<DigitalDocumentDetail>(`/reader/digital/${id}`),

  openDigital: (id: string) => api.get<DigitalReaderSession>(`/reader/digital/${id}/read`),

  /**
   * Một trang tài liệu dưới dạng ảnh.
   *
   * Phải lấy bằng lời gọi có mang mã đăng nhập rồi mới dựng thành địa chỉ tạm cho thẻ ảnh: đặt
   * thẳng đường dẫn vào thẻ ảnh thì trình duyệt gửi đi mà không kèm mã, và máy chủ từ chối.
   */
  async digitalPage(id: string, page: number): Promise<string> {
    const response = await http.get<Blob>(`/reader/digital/${id}/pages/${page}`, {
      responseType: 'blob',
    });

    return URL.createObjectURL(response.data);
  },

  async downloadDigital(id: string): Promise<Blob> {
    const response = await http.get<Blob>(`/reader/digital/${id}/download`, {
      responseType: 'blob',
    });

    return response.data;
  },

  requestDigitalAccess: (documentId: string, reason: string) =>
    api.post<string>(`/reader/digital/${documentId}/request`, { reason }),

  /**
   * Báo tổng số giây đã đọc của lượt mở gần nhất (V.2). Dùng fetch keepalive thay vì axios để lần
   * báo cuối lúc rời trang vẫn được gửi nốt; lỗi thì nuốt — không có gì để người đọc phải biết.
   */
  reportReadingTime(documentId: string, seconds: number): void {
    const request = buildReadingTimeRequest(
      http.defaults.baseURL ?? '/api',
      documentId,
      seconds,
      tokenStorage.getAccessToken(),
    );

    void fetch(request.url, request.init).catch(() => undefined);
  },
};
