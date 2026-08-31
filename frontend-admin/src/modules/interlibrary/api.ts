import { api } from '@/api/client';
import type { PagedResult } from '@/types/api';
import type {
  OaiHarvestLogDto,
  OaiIdentifyDto,
  OaiRepositoryDto,
  RemoteSearchResultDto,
  Z3950CheckResultDto,
  Z3950SearchLogDto,
  Z3950TargetDto,
} from './types';

/** Phân hệ liên thư viện — Z39.50, SRU, OAI-PMH. */
export const interLibraryApi = {
  // --- Máy chủ thư viện bạn -----------------------------------------------
  targets: (includeInactive = false) =>
    api.get<Z3950TargetDto[]>('/interlibrary/targets', { params: { includeInactive } }),

  saveTarget: (payload: Record<string, unknown>, id?: string) =>
    id
      ? api.put<string>(`/interlibrary/targets/${id}`, payload)
      : api.post<string>('/interlibrary/targets', payload),

  deleteTarget: (id: string) => api.delete<null>(`/interlibrary/targets/${id}`),

  checkTarget: (id: string) =>
    api.post<Z3950CheckResultDto>(`/interlibrary/targets/${id}/check`),

  search: (payload: Record<string, unknown>) =>
    api.post<RemoteSearchResultDto>('/interlibrary/search', payload),

  prepareRecord: (targetId: string, marcJson: string) =>
    api.post<string>(`/interlibrary/targets/${targetId}/prepare`, { marcJson }),

  searchLogs: (payload: Record<string, unknown>) =>
    api.post<PagedResult<Z3950SearchLogDto>>('/interlibrary/search-logs', payload),

  // --- Kho OAI-PMH ---------------------------------------------------------
  repositories: (includeInactive = false) =>
    api.get<OaiRepositoryDto[]>('/interlibrary/oai/repositories', {
      params: { includeInactive },
    }),

  saveRepository: (payload: Record<string, unknown>, id?: string) =>
    id
      ? api.put<string>(`/interlibrary/oai/repositories/${id}`, payload)
      : api.post<string>('/interlibrary/oai/repositories', payload),

  deleteRepository: (id: string) => api.delete<null>(`/interlibrary/oai/repositories/${id}`),

  identify: (baseUrl: string) =>
    api.get<OaiIdentifyDto>('/interlibrary/oai/identify', { params: { baseUrl } }),

  harvest: (id: string, fullReload = false) =>
    api.post<OaiHarvestLogDto>(
      `/interlibrary/oai/repositories/${id}/harvest`,
      undefined,
      { params: { fullReload } },
    ),

  harvestLogs: (params: Record<string, unknown>) =>
    api.get<PagedResult<OaiHarvestLogDto>>('/interlibrary/oai/harvest-logs', { params }),
};
