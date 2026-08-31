import { api } from '@/api/client';
import type { PagedResult } from '@/types/api';
import type {
  CreateClaimsResultDto,
  GenerateArticleRecordsResultDto,
  GenerateIssuesResultDto,
  ImportArticlesResultDto,
  IssueGridYearDto,
  IssuePreviewDto,
  ReceiveIssuesResultDto,
  SerialArticleDto,
  SerialBindingDto,
  SerialClaimDto,
  SerialClaimStatus,
  SerialDetailDto,
  SerialDto,
  SerialIssueDto,
  SerialReportFilter,
  SerialStatReportDto,
  SerialSummaryRowDto,
} from './types';

/** Phân hệ IV — Ấn phẩm định kỳ. */
export const serialsApi = {
  search: (params: Record<string, unknown>) =>
    api.get<PagedResult<SerialDto>>('/serials', { params }),

  get: (id: string) => api.get<SerialDetailDto>(`/serials/${id}`),

  save: (id: string | null, payload: Record<string, unknown>) =>
    id ? api.put<string>(`/serials/${id}`, payload) : api.post<string>('/serials', payload),

  remove: (id: string) => api.delete<null>(`/serials/${id}`),

  previewIssues: (id: string, from?: string | null, to?: string | null) =>
    api.get<IssuePreviewDto[]>(`/serials/${id}/issues/preview`, { params: { from, to } }),

  generateIssues: (payload: Record<string, unknown>) =>
    api.post<GenerateIssuesResultDto>('/serials/issues/generate', payload),

  issues: (params: Record<string, unknown>) =>
    api.get<PagedResult<SerialIssueDto>>('/serials/issues', { params }),

  grid: (id: string, fromYear?: number | null, toYear?: number | null) =>
    api.get<IssueGridYearDto[]>(`/serials/${id}/grid`, { params: { fromYear, toYear } }),

  summary: (id: string) => api.get<SerialSummaryRowDto[]>(`/serials/${id}/summary`),

  receive: (payload: Record<string, unknown>) =>
    api.post<ReceiveIssuesResultDto>('/serials/issues/receive', payload),

  markMissing: (payload: Record<string, unknown>) =>
    api.post<number>('/serials/issues/mark-missing', payload),

  claims: (serialId?: string | null, status?: SerialClaimStatus | null) =>
    api.get<SerialClaimDto[]>('/serials/claims', { params: { serialId, status } }),

  createClaims: (payload: Record<string, unknown>) =>
    api.post<CreateClaimsResultDto>('/serials/claims', payload),

  respondClaim: (id: string, response: string, status: SerialClaimStatus) =>
    api.post<null>(`/serials/claims/${id}/respond`, { response, status }),

  articles: (issueId: string) => api.get<SerialArticleDto[]>(`/serials/issues/${issueId}/articles`),

  saveArticles: (issueId: string, articles: Record<string, unknown>[]) =>
    api.put<number>(`/serials/issues/${issueId}/articles`, { articles }),

  generateArticleRecords: (issueId: string, articleIds: string[]) =>
    api.post<GenerateArticleRecordsResultDto>(
      `/serials/issues/${issueId}/articles/generate-records`,
      { articleIds },
    ),

  articleTemplate: () => api.download('/serials/articles/excel-template'),

  importArticles: (issueId: string, file: File) => {
    const form = new FormData();
    form.append('file', file);

    return api.post<ImportArticlesResultDto>(`/serials/issues/${issueId}/articles/import`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  bindings: (serialId?: string | null, year?: number | null) =>
    api.get<SerialBindingDto[]>('/serials/bindings', { params: { serialId, year } }),

  bind: (payload: Record<string, unknown>) =>
    api.post<SerialBindingDto>('/serials/bindings', payload),

  dimensions: () => api.get<Record<string, string>>('/serials/reports/dimensions'),

  statistics: (dimension: string, filter: SerialReportFilter) =>
    api.post<SerialStatReportDto>(`/serials/reports/statistics?dimension=${dimension}`, filter),

  exportReport: (dimension: string, format: string, filter: SerialReportFilter) =>
    api.downloadPost(
      `/serials/reports/export?dimension=${dimension}&format=${format}`,
      filter,
      'thong-ke-an-pham-dinh-ky',
    ),
};
