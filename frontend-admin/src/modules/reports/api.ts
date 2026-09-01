import { api } from '@/api/client';
import type { SystemOverview } from './types';

/** Báo cáo thống kê toàn hệ thống. */
export const reportsApi = {
  overview: (from?: string, to?: string) =>
    api.get<SystemOverview>('/reports/overview', { params: { from, to } }),
};
