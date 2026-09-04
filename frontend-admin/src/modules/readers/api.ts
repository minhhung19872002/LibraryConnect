import { api } from '@/api/client';
import type { PagedResult } from '@/types/api';
import type {
  BulkResultDto,
  CardFieldOptionDto,
  ExpiringCardsReportDto,
  PhotoImportResultDto,
  ReaderActivityRowDto,
  ReaderCardDto,
  ReaderCardTemplateDto,
  ReaderClearanceDto,
  ReaderDetailDto,
  ReaderDigitalAccessDto,
  ReaderDto,
  ReaderFineDto,
  ReaderImportBatchDto,
  ReaderImportOptions,
  ReaderImportPreviewDto,
  ReaderImportRawRowDto,
  ReaderImportRowsResultDto,
  ReaderLoanDto,
  ReaderReportDimension,
  ReaderReportFilter,
  ReaderReportRowDto,
  ReaderSyncResultDto,
  ReaderTimeGrouping,
  ReaderTimeRowDto,
  ReaderViolationDto,
  ReaderVisitDto,
} from './types';

/** Phân hệ VI — Bạn đọc. */
export const readersApi = {
  search: (params: Record<string, unknown>) => api.get<PagedResult<ReaderDto>>('/readers', { params }),

  get: (id: string) => api.get<ReaderDetailDto>(`/readers/${id}`),

  save: (id: string | null, payload: Record<string, unknown>) =>
    id ? api.put<string>(`/readers/${id}`, payload) : api.post<string>('/readers', payload),

  remove: (id: string, reason?: string) =>
    api.delete<null>(`/readers/${id}`, { params: { reason } }),

  // --- Ảnh chân dung -------------------------------------------------------
  uploadPhoto: (id: string, file: Blob, fileName = 'anh.jpg') => {
    const form = new FormData();
    form.append('file', file, fileName);

    return api.post<string>(`/readers/${id}/photo`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  deletePhoto: (id: string) => api.delete<null>(`/readers/${id}/photo`),

  importPhotos: (file: File, dryRun: boolean) => {
    const form = new FormData();
    form.append('file', file, file.name);

    return api.post<PhotoImportResultDto>('/readers/photos/import', form, {
      params: { dryRun },
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  // --- Thẻ và trạng thái ---------------------------------------------------
  extendCards: (payload: Record<string, unknown>) =>
    api.post<BulkResultDto>('/readers/cards/extend', payload),

  setLock: (payload: Record<string, unknown>) => api.post<BulkResultDto>('/readers/lock', payload),

  reissueCard: (id: string, payload: Record<string, unknown>) =>
    api.post<ReaderCardDto>(`/readers/${id}/cards/reissue`, payload),

  graduate: (payload: Record<string, unknown>) =>
    api.post<BulkResultDto>('/readers/graduate', payload),

  clearance: (id: string) => api.get<ReaderClearanceDto>(`/readers/${id}/clearance`),

  /** Giấy xác nhận trả sách (VII.4) — mẫu biểu CLEARANCE in qua lối của phân hệ bạn đọc. */
  printClearance: (id: string) => api.download(`/readers/${id}/clearance/print`),

  resetPassword: (id: string, newPassword?: string) =>
    api.post<string>(`/readers/${id}/reset-password`, { newPassword }),

  // --- Lịch sử -------------------------------------------------------------
  loans: (id: string, params: Record<string, unknown>) =>
    api.get<PagedResult<ReaderLoanDto>>(`/readers/${id}/loans`, { params }),

  fines: (id: string, params: Record<string, unknown>) =>
    api.get<PagedResult<ReaderFineDto>>(`/readers/${id}/fines`, { params }),

  visits: (id: string, params: Record<string, unknown>) =>
    api.get<PagedResult<ReaderVisitDto>>(`/readers/${id}/visits`, { params }),

  digitalAccess: (id: string, params: Record<string, unknown>) =>
    api.get<PagedResult<ReaderDigitalAccessDto>>(`/readers/${id}/digital-access`, { params }),

  violations: (id: string, params: Record<string, unknown>) =>
    api.get<PagedResult<ReaderViolationDto>>(`/readers/${id}/violations`, { params }),

  saveViolation: (id: string, payload: Record<string, unknown>) =>
    api.post<string>(`/readers/${id}/violations`, payload),

  deleteViolation: (violationId: string) =>
    api.delete<null>(`/readers/violations/${violationId}`),

  // --- Mẫu thẻ và in thẻ ---------------------------------------------------
  cardTemplates: (includeInactive = false) =>
    api.get<ReaderCardTemplateDto[]>('/readers/card-templates', { params: { includeInactive } }),

  cardFields: () => api.get<CardFieldOptionDto[]>('/readers/card-templates/fields'),

  saveCardTemplate: (payload: Record<string, unknown>) =>
    api.post<string>('/readers/card-templates', payload),

  deleteCardTemplate: (id: string) => api.delete<null>(`/readers/card-templates/${id}`),

  printCards: (payload: Record<string, unknown>) =>
    api.downloadPost('/readers/cards/print', payload, 'the-ban-doc.pdf'),

  // --- Nhập, xuất, đồng bộ -------------------------------------------------
  importTemplate: () => api.download('/readers/import/template'),

  importMapping: () => api.get<Record<string, string>>('/readers/import/mapping'),

  saveImportMapping: (mapping: Record<string, string>) =>
    api.put<null>('/readers/import/mapping', mapping),

  validateImport: (file: File, options: ReaderImportOptions) => {
    const form = new FormData();
    form.append('file', file, file.name);
    form.append('options', JSON.stringify(options));

    return api.post<ReaderImportPreviewDto>('/readers/import/validate', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  startImport: (file: File, options: ReaderImportOptions) => {
    const form = new FormData();
    form.append('file', file, file.name);
    form.append('options', JSON.stringify(options));

    return api.post<string>('/readers/import', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  /** Kiểm tra lại (dryRun) hoặc nhập thật các dòng đã sửa ngay trên bảng lỗi. */
  importRows: (payload: {
    rows: ReaderImportRawRowDto[];
    options: ReaderImportOptions;
    dryRun: boolean;
    fileName?: string;
  }) => api.post<ReaderImportRowsResultDto>('/readers/import/rows', payload),

  importBatches: (params: Record<string, unknown>) =>
    api.get<PagedResult<ReaderImportBatchDto>>('/readers/import/batches', { params }),

  importBatch: (id: string) => api.get<ReaderImportBatchDto>(`/readers/import/batches/${id}`),

  importErrors: (id: string) => api.download(`/readers/import/batches/${id}/errors`),

  export: (filter: Record<string, unknown>) =>
    api.download('/readers/export', { params: filter }),

  syncMapping: () => api.get<Record<string, string>>('/readers/sync/mapping'),

  saveSyncMapping: (mapping: Record<string, string>) =>
    api.put<null>('/readers/sync/mapping', mapping),

  sync: (payload: Record<string, unknown>) =>
    api.post<ReaderSyncResultDto>('/readers/sync', payload),

  // --- Báo cáo -------------------------------------------------------------
  countReport: (dimension: ReaderReportDimension, filter: ReaderReportFilter) =>
    api.get<ReaderReportRowDto[]>('/readers/reports/count', { params: { dimension, ...filter } }),

  registrationReport: (grouping: ReaderTimeGrouping, filter: ReaderReportFilter) =>
    api.get<ReaderTimeRowDto[]>('/readers/reports/registrations', {
      params: { grouping, ...filter },
    }),

  expiringCards: (withinDays: number, filter: ReaderReportFilter) =>
    api.get<ExpiringCardsReportDto>('/readers/reports/expiring-cards', {
      params: { withinDays, ...filter },
    }),

  activityReport: (neverBorrowed: boolean, top: number, filter: ReaderReportFilter) =>
    api.get<ReaderActivityRowDto[]>('/readers/reports/activity', {
      params: { neverBorrowed, top, ...filter },
    }),

  exportReport: (payload: Record<string, unknown>) =>
    api.downloadPost('/readers/reports/export', payload, 'bao-cao-ban-doc'),
};
