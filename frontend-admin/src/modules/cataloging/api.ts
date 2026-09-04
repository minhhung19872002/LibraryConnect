import { api, http } from '@/api/client';
import type { PagedResult } from '@/types/api';
import type { MarcRecord } from '@/modules/marc/types';
import type { BibImportOptions, BibImportPreview, DuplicateMatchBy, ImportJob } from './importTypes';
import type { CustomIndex, CustomIndexValue, HarvestResult } from './customIndexTypes';
import type { CardTemplate } from './cardTypes';
import type {
  ExcelFailedRows,
  ExcelImportOptions,
  ExcelPreview,
  ExcelRetryRow,
  ImportMappingProfile,
} from './excelTypes';
import type {
  CatalogProductivity,
  CatalogQueueItem,
  CatalogQueueStatus,
  CatalogQueueSummary,
} from './queueTypes';
import type {
  BibDetail,
  BibListItem,
  BibVersion,
  CoverLookupOutcome,
  CreateItemsResult,
  Item,
  MarcDiffLine,
  MarcFieldDefault,
  MarcTemplate,
  NewBibRecord,
  RecordStatus,
  SaveBibResult,
  Shelf,
  Warehouse,
} from './types';

/** Bộ lọc của màn hình danh sách biểu ghi. */
export interface BibListParams {
  page?: number;
  pageSize?: number;
  keyword?: string;
  sortBy?: string;
  sortDescending?: boolean;
  documentTypeId?: string;
  languageId?: string;
  publisherId?: string;
  authorId?: string;
  subjectId?: string;
  classificationId?: string;
  collectionId?: string;
  /** Giá trị của một danh mục tự tạo từ trường MARC (II.9). */
  customIndexValueId?: string;
  status?: RecordStatus;
  publishYearFrom?: number;
  publishYearTo?: number;
  withoutItems?: boolean;
  availableOnly?: boolean;
}

export interface SaveBibPayload {
  marcJson: string;
  documentTypeId?: string | null;
  carrierTypeId?: string | null;
  collectionIds?: string[];
  coverImageUrl?: string | null;
  status: RecordStatus;
  changeNote?: string | null;
}

export interface CreateItemsPayload {
  quantity: number;
  warehouseId: string;
  shelfId?: string | null;
  price: number;
  fundingSourceId?: string | null;
  acquisitionDate?: string | null;
  acquisitionType: string;
  volumeNumber?: string | null;
  condition?: string | null;
  note?: string | null;
  callNumber?: string | null;
  unlockImmediately: boolean;
}

export const catalogingApi = {
  list: (params: BibListParams) => api.get<PagedResult<BibListItem>>('/cataloging/bibs', { params }),

  get: (id: string) => api.get<BibDetail>(`/cataloging/bibs/${id}`),

  /** Khung biểu ghi mới, đã điền sẵn theo mẫu và bảng giá trị ngầm định. */
  blank: (documentTypeId?: string, templateId?: string) =>
    api.get<NewBibRecord>('/cataloging/bibs/new', { params: { documentTypeId, templateId } }),

  create: (payload: SaveBibPayload) => api.post<SaveBibResult>('/cataloging/bibs', payload),

  update: (id: string, payload: SaveBibPayload) =>
    api.put<SaveBibResult>(`/cataloging/bibs/${id}`, payload),

  remove: (id: string, reason: string) =>
    api.delete<null>(`/cataloging/bibs/${id}`, { data: { reason } }),

  versions: (id: string) => api.get<BibVersion[]>(`/cataloging/bibs/${id}/versions`),

  diff: (id: string, versionId: string, compareTo?: string) =>
    api.get<MarcDiffLine[]>(`/cataloging/bibs/${id}/versions/${versionId}/diff`, {
      params: { compareTo },
    }),

  restore: (id: string, versionId: string) =>
    api.post<SaveBibResult>(`/cataloging/bibs/${id}/versions/${versionId}/restore`),

  items: (id: string) => api.get<Item[]>(`/cataloging/bibs/${id}/items`),

  createItems: (id: string, payload: CreateItemsPayload) =>
    api.post<CreateItemsResult>(`/cataloging/bibs/${id}/items`, payload),

  updateItem: (itemId: string, payload: Partial<Item>) =>
    api.put<null>(`/cataloging/items/${itemId}`, payload),

  // ---- Ảnh bìa ----------------------------------------------------------------------------

  /**
   * Địa chỉ ảnh bìa của một biểu ghi.
   *
   * Một địa chỉ duy nhất cho mọi biểu ghi: máy chủ tự quyết trả ảnh thật hay bìa dựng sẵn. Để phía
   * gọi tự chọn theo cột coverImageUrl thì khi cột ấy trỏ sai chỗ, cả trang đầy ô ảnh hỏng.
   */
  coverUrl: (bibId: string) => `/api/public/covers/${bibId}`,

  /** Tra ảnh bìa thật ở nguồn ngoài cho một biểu ghi. */
  lookupCover: (id: string) =>
    api.post<CoverLookupOutcome>(`/cataloging/bibs/${id}/cover/lookup`),

  /** Cán bộ tự tải ảnh bìa lên; ảnh này không bao giờ bị lượt tra tự động ghi đè. */
  async uploadCover(id: string, file: File): Promise<string> {
    const form = new FormData();
    form.append('file', file);

    return api.post<string>(`/cataloging/bibs/${id}/cover`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  /**
   * Mở một lượt tra ảnh bìa hàng loạt cho biểu ghi chưa có ảnh.
   *
   * Lọc theo dạng tài liệu vì chỉ vài dạng mới có ISBN: toàn bộ bài giảng điện tử trong kho không
   * cuốn nào có, còn nhóm Sách và Giáo trình thì hơn một nửa có. Mỗi lượt gọi ra nguồn ngoài tốn
   * hơn một giây chờ nên tra dạng không thể có ISBN là phí thời gian.
   */
  lookupCoversBatch: (maxRecords: number, documentTypeCodes?: string[]) =>
    api.post<string>(
      `/cataloging/covers/lookup-batch?maxRecords=${maxRecords}`
        + (documentTypeCodes?.length ? `&documentTypeCodes=${documentTypeCodes.join(',')}` : ''),
    ),

  /** Nạp sách từ Open Library theo chủ đề — API mở, giấy phép CC0, có ảnh bìa kèm sẵn. */
  harvestOpenLibrary: (maxRecords: number, subjects?: string[]) =>
    api.post<string>(
      `/cataloging/open-library/harvest?maxRecords=${maxRecords}`
        + (subjects?.length ? `&subjects=${encodeURIComponent(subjects.join(','))}` : ''),
    ),

  deleteItem: (itemId: string, reason: string) =>
    api.delete<null>(`/cataloging/items/${itemId}`, { data: { reason } }),

  marcDefaults: (documentTypeId?: string, includeInactive = false) =>
    api.get<MarcFieldDefault[]>('/cataloging/marc-defaults', {
      params: { documentTypeId, includeInactive },
    }),

  saveMarcDefault: (id: string | null, payload: Partial<MarcFieldDefault>) =>
    id
      ? api.put<string>(`/cataloging/marc-defaults/${id}`, payload)
      : api.post<string>('/cataloging/marc-defaults', payload),

  deleteMarcDefault: (id: string) => api.delete<null>(`/cataloging/marc-defaults/${id}`),

  templates: (documentTypeId?: string, includeInactive = false) =>
    api.get<MarcTemplate[]>('/cataloging/templates', { params: { documentTypeId, includeInactive } }),

  saveTemplate: (id: string | null, payload: Record<string, unknown>) =>
    id
      ? api.put<string>(`/cataloging/templates/${id}`, payload)
      : api.post<string>('/cataloging/templates', payload),

  deleteTemplate: (id: string) => api.delete<null>(`/cataloging/templates/${id}`),

  customIndexes: (includeInactive = false) =>
    api.get<CustomIndex[]>('/cataloging/custom-indexes', { params: { includeInactive } }),

  customIndexValues: (id: string, keyword?: string) =>
    api.get<CustomIndexValue[]>(`/cataloging/custom-indexes/${id}/values`, { params: { keyword } }),

  saveCustomIndex: (id: string | null, payload: Record<string, unknown>) =>
    id
      ? api.put<string>(`/cataloging/custom-indexes/${id}`, payload)
      : api.post<string>('/cataloging/custom-indexes', payload),

  deleteCustomIndex: (id: string) => api.delete<null>(`/cataloging/custom-indexes/${id}`),

  harvestCustomIndex: (id: string) =>
    api.post<HarvestResult>(`/cataloging/custom-indexes/${id}/harvest`),

  mergeCustomIndexValues: (indexId: string, keepId: string, mergeIds: string[]) =>
    api.post<number>(`/cataloging/custom-indexes/${indexId}/merge`, { keepId, mergeIds }),
};

/** Nhập và xuất biểu ghi hàng loạt (II.6). */
export const importApi = {
  /** Bước xem trước: đọc tệp và đối chiếu trùng, không ghi gì. */
  async preview(file: File, matchBy: DuplicateMatchBy): Promise<BibImportPreview> {
    const form = new FormData();
    form.append('file', file);

    return api.post<BibImportPreview>(`/cataloging/import/preview?matchBy=${matchBy}`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  /** Bắt đầu nhập thật; trả về mã tác vụ để theo dõi tiến độ. */
  async start(file: File, options: BibImportOptions): Promise<string> {
    const form = new FormData();
    form.append('file', file);
    form.append('options', JSON.stringify(options));

    return api.post<string>('/cataloging/import', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  jobs: (take = 20) => api.get<ImportJob[]>('/cataloging/import/jobs', { params: { take } }),

  job: (id: string) => api.get<ImportJob>(`/cataloging/import/jobs/${id}`),

  /** Tệp kết quả của một lượt nhập: dòng tổng kết và các dòng lỗi, Excel hoặc CSV. */
  result: (id: string, format: 'xlsx' | 'csv' = 'xlsx') =>
    api.download(`/cataloging/import/jobs/${id}/result`, { params: { format } }),

  /**
   * Xuất biểu ghi ra tệp trao đổi: theo danh sách đã tick chọn, hoặc theo đúng bộ lọc đang dùng.
   *
   * The response is a file rather than the usual envelope, so this call goes through the raw client.
   */
  async export(
    ids: string[],
    filter: BibListParams | undefined,
    format: 'iso2709' | 'marcxml',
  ): Promise<{ blob: Blob; fileName: string }> {
    const response = await http.post<Blob>(
      '/cataloging/export',
      { ids, filter, format },
      { responseType: 'blob' },
    );

    const disposition = response.headers['content-disposition'] as string | undefined;
    const match = disposition?.match(/filename\*?=(?:UTF-8'')?"?([^";]+)"?/i);

    return {
      blob: response.data,
      fileName: match?.[1] ? decodeURIComponent(match[1]) : `bieu-ghi.${format === 'marcxml' ? 'xml' : 'mrc'}`,
    };
  },
};

/** Hàng đợi biên mục chi tiết (II.4). */
export const queueApi = {
  list: (params: {
    status?: CatalogQueueStatus;
    keyword?: string;
    assignedTo?: string;
    unassigned?: boolean;
    overdueOnly?: boolean;
    page?: number;
    pageSize?: number;
  }) => api.get<PagedResult<CatalogQueueItem>>('/cataloging/queue', { params }),

  summary: () => api.get<CatalogQueueSummary>('/cataloging/queue/summary'),

  productivity: (from?: string, to?: string) =>
    api.get<CatalogProductivity[]>('/cataloging/queue/productivity', { params: { from, to } }),

  enqueue: (bibId: string, payload: { priority?: number; assignedTo?: string; deadline?: string; note?: string }) =>
    api.post<string>('/cataloging/queue', { bibId, ...payload }),

  assign: (payload: {
    ids: string[];
    assignedTo?: string | null;
    priority?: number;
    deadline?: string;
    note?: string;
  }) => api.post<number>('/cataloging/queue/assign', payload),

  changeStatus: (id: string, status: CatalogQueueStatus, reason?: string) =>
    api.post<null>(`/cataloging/queue/${id}/status`, { status, reason }),

  /** Đổi trạng thái nhiều việc cùng lúc — duyệt cả một lượt thu hoạch. */
  changeStatusBatch: (ids: string[], status: CatalogQueueStatus, reason?: string) =>
    api.post<number>('/cataloging/queue/status', { ids, status, reason }),

  remove: (id: string) => api.delete<null>(`/cataloging/queue/${id}`),
};

/** Mẫu phích và in phích (II.10). */
export const cardApi = {
  templates: (includeInactive = false) =>
    api.get<CardTemplate[]>('/cataloging/card-templates', { params: { includeInactive } }),

  saveTemplate: (id: string | null, payload: Record<string, unknown>) =>
    id
      ? api.put<string>(`/cataloging/card-templates/${id}`, payload)
      : api.post<string>('/cataloging/card-templates', payload),

  deleteTemplate: (id: string) => api.delete<null>(`/cataloging/card-templates/${id}`),

  /** Dựng thử một phích từ biểu ghi đang soạn, chưa lưu (II.2) — PDF một trang. */
  previewRecord: (request: { marcJson: string; cardType?: string; templateId?: string; callNumber?: string }) =>
    api.downloadPost('/cataloging/cards/preview', request, 'xem-truoc-phich.pdf'),

  /** Kết xuất phích ra PDF; phản hồi là tệp nên đi qua client thô. */
  async print(request: {
    bibIds: string[];
    filter?: BibListParams;
    templateId?: string;
    cardTypes: string[];
    multiplePerPage: boolean;
    /** Chỉ dựng vài biểu ghi đầu để xem trước, không phải cả lượt in. */
    preview?: boolean;
    previewRecords?: number;
  }): Promise<{ blob: Blob; fileName: string }> {
    const response = await http.post<Blob>('/cataloging/cards/print', request, { responseType: 'blob' });

    const disposition = response.headers['content-disposition'] as string | undefined;
    const match = disposition?.match(/filename\*?=(?:UTF-8'')?"?([^";]+)"?/i);

    return {
      blob: response.data,
      fileName: match?.[1] ? decodeURIComponent(match[1]) : 'phich.pdf',
    };
  },
};

/** Nhập biểu ghi từ bảng tính Excel (II.8). */
export const excelApi = {
  /** Tệp mẫu có tiêu đề tiếng Việt và sheet hướng dẫn. */
  async template(): Promise<{ blob: Blob; fileName: string }> {
    return api.download('/cataloging/excel/template');
  },

  async preview(file: File): Promise<ExcelPreview> {
    const form = new FormData();
    form.append('file', file);

    return api.post<ExcelPreview>('/cataloging/excel/preview', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  async start(file: File, options: ExcelImportOptions): Promise<string> {
    const form = new FormData();
    form.append('file', file);
    form.append('options', JSON.stringify(options));

    return api.post<string>('/cataloging/excel/import', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  /** Các dòng đã lỗi của một lượt nhập, kèm nội dung ô để sửa tại chỗ. */
  failedRows: (jobId: string) => api.get<ExcelFailedRows>(`/cataloging/excel/jobs/${jobId}/failed-rows`),

  /** Nhập lại các dòng đã sửa; trả về mã tác vụ mới. */
  retry: (jobId: string, rows: ExcelRetryRow[]) =>
    api.post<string>(`/cataloging/excel/jobs/${jobId}/retry`, { rows }),

  profiles: () => api.get<ImportMappingProfile[]>('/cataloging/excel/mapping-profiles'),

  saveProfile: (id: string | null, payload: Record<string, unknown>) =>
    id
      ? api.put<string>(`/cataloging/excel/mapping-profiles/${id}`, payload)
      : api.post<string>('/cataloging/excel/mapping-profiles', payload),

  deleteProfile: (id: string) => api.delete<null>(`/cataloging/excel/mapping-profiles/${id}`),
};

export const locationsApi = {
  warehouses: (libraryId?: string) =>
    api.get<Warehouse[]>('/locations/warehouses', { params: { libraryId } }),

  shelves: (warehouseId?: string) =>
    api.get<Shelf[]>('/locations/shelves', { params: { warehouseId } }),
};

/** Đọc biểu ghi MARC từ chuỗi JSON máy chủ trả về. */
export function parseMarc(marcJson: string): MarcRecord {
  return JSON.parse(marcJson) as MarcRecord;
}
