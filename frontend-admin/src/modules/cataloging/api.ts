import { api, http } from '@/api/client';
import type { PagedResult } from '@/types/api';
import type { MarcRecord } from '@/modules/marc/types';
import type { BibImportOptions, BibImportPreview, DuplicateMatchBy, ImportJob } from './importTypes';
import type { CustomIndex, CustomIndexValue, HarvestResult } from './customIndexTypes';
import type {
  BibDetail,
  BibListItem,
  BibVersion,
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
