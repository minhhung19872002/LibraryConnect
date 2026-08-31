import { api } from '@/api/client';
import type { PagedResult } from '@/types/api';
import type {
  DigitalAccessLogRowDto,
  DigitalAccessRequestRowDto,
  DigitalCollectionDto,
  DigitalDocumentDetailDto,
  DigitalDocumentRowDto,
  DigitalImportResultDto,
  DigitalInventoryReportDto,
  DigitalReaderSessionDto,
  DigitalReportFilter,
  DigitalRequestReportDto,
  DigitalStorageReportDto,
  DigitalUploadSessionDto,
  DigitalUsageReportDto,
} from './types';

/** Phân hệ V — Tài liệu số. */
export const digitalApi = {
  // --- Bộ sưu tập ---------------------------------------------------------
  collections: (includeInactive = false) =>
    api.get<DigitalCollectionDto[]>('/digital/collections', { params: { includeInactive } }),

  saveCollection: (payload: Record<string, unknown>, id?: string) =>
    id
      ? api.put<string>(`/digital/collections/${id}`, payload)
      : api.post<string>('/digital/collections', payload),

  deleteCollection: (id: string) => api.delete<null>(`/digital/collections/${id}`),

  // --- Tài liệu -----------------------------------------------------------
  search: (payload: Record<string, unknown>) =>
    api.post<PagedResult<DigitalDocumentRowDto>>('/digital/documents/search', payload),

  detail: (id: string) => api.get<DigitalDocumentDetailDto>(`/digital/documents/${id}`),

  update: (id: string, payload: Record<string, unknown>) =>
    api.put<null>(`/digital/documents/${id}`, payload),

  remove: (id: string, reason: string) =>
    api.delete<null>(`/digital/documents/${id}`, { params: { reason } }),

  runOcr: (id: string) => api.post<null>(`/digital/documents/${id}/ocr`),

  /** Tải tệp nhỏ trong một lần gọi. */
  upload: (file: File, fields: Record<string, string | undefined>) => {
    const form = new FormData();
    form.append('file', file);

    Object.entries(fields).forEach(([key, value]) => {
      if (value !== undefined && value !== '') form.append(key, value);
    });

    return api.post<string>('/digital/documents/upload', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  // --- Tải theo mảnh ------------------------------------------------------
  startUpload: (payload: Record<string, unknown>) =>
    api.post<DigitalUploadSessionDto>('/digital/uploads', payload),

  uploadSession: (id: string) => api.get<DigitalUploadSessionDto>(`/digital/uploads/${id}`),

  uploadChunk: (id: string, index: number, chunk: Blob) => {
    const form = new FormData();
    form.append('file', chunk, `${index}.part`);

    return api.post<DigitalUploadSessionDto>(`/digital/uploads/${id}/chunks/${index}`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  completeUpload: (id: string, payload: Record<string, unknown>) =>
    api.post<string>(`/digital/uploads/${id}/complete`, payload),

  // --- Đọc trực tuyến -----------------------------------------------------
  openReader: (id: string) =>
    api.get<DigitalReaderSessionDto>(`/digital/documents/${id}/reader`),

  download: (id: string) => api.download(`/digital/documents/${id}/download`),

  // --- Yêu cầu đọc và nhật ký --------------------------------------------
  requests: (payload: Record<string, unknown>) =>
    api.post<PagedResult<DigitalAccessRequestRowDto>>('/digital/requests/search', payload),

  approveRequest: (id: string, payload: Record<string, unknown>) =>
    api.post<DigitalAccessRequestRowDto>(`/digital/requests/${id}/approve`, payload),

  rejectRequest: (id: string, reason: string) =>
    api.post<null>(`/digital/requests/${id}/reject`, { reason }),

  revokeRequest: (id: string, reason: string) =>
    api.post<null>(`/digital/requests/${id}/revoke`, { reason }),

  logs: (payload: Record<string, unknown>) =>
    api.post<PagedResult<DigitalAccessLogRowDto>>('/digital/logs/search', payload),

  // --- Nhập xuất ----------------------------------------------------------
  importArchive: (file: File, fields: Record<string, string | undefined>) => {
    const form = new FormData();
    form.append('file', file);

    Object.entries(fields).forEach(([key, value]) => {
      if (value !== undefined && value !== '') form.append(key, value);
    });

    return api.post<DigitalImportResultDto>('/digital/import', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  exportArchive: (payload: Record<string, unknown>) =>
    api.downloadPost('/digital/export', payload, 'tai-lieu-so.zip'),

  // --- Báo cáo ------------------------------------------------------------
  inventoryReport: (filter: DigitalReportFilter) =>
    api.post<DigitalInventoryReportDto>('/digital/reports/inventory', filter),

  usageReport: (filter: DigitalReportFilter) =>
    api.post<DigitalUsageReportDto>('/digital/reports/usage', filter),

  storageReport: () => api.get<DigitalStorageReportDto>('/digital/reports/storage'),

  requestReport: (filter: DigitalReportFilter) =>
    api.post<DigitalRequestReportDto>('/digital/reports/requests', filter),

  exportReport: (payload: Record<string, unknown>) =>
    api.downloadPost('/digital/reports/export', payload, 'bao-cao-tai-lieu-so'),
};
