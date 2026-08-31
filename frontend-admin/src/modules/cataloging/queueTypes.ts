import type { BibSource } from './types';

export type CatalogQueueStatus = 'Pending' | 'InProgress' | 'WaitingApproval' | 'Completed' | 'Returned';

export interface CatalogQueueItem {
  id: string;
  bibId: string;
  controlNumber: string;
  title: string;
  authorMain?: string | null;
  documentTypeName?: string | null;
  source: BibSource;
  status: CatalogQueueStatus;
  priority: number;
  assignedTo?: string | null;
  assignedToName?: string | null;
  deadline?: string | null;
  note?: string | null;
  returnReason?: string | null;
  createdAt: string;
  startedAt?: string | null;
  completedAt?: string | null;
  /** Quá hạn xử lý. */
  isOverdue: boolean;
}

export interface CatalogQueueSummary {
  pending: number;
  inProgress: number;
  waitingApproval: number;
  completed: number;
  returned: number;
  overdue: number;
}

export interface CatalogProductivity {
  userId?: string | null;
  userName: string;
  assigned: number;
  completed: number;
  returned: number;
  averageDays?: number | null;
}

export const QUEUE_STATUS_LABELS: Record<CatalogQueueStatus, string> = {
  Pending: 'Chờ xử lý',
  InProgress: 'Đang biên mục',
  WaitingApproval: 'Chờ duyệt',
  Completed: 'Đã hoàn thành',
  Returned: 'Bị trả lại',
};

/** Năm mức ưu tiên, 1 là cao nhất — cách các thư viện quen đánh số việc. */
export const PRIORITY_LABELS: Record<number, string> = {
  1: '1 — Rất gấp',
  2: '2 — Gấp',
  3: '3 — Bình thường',
  4: '4 — Thấp',
  5: '5 — Rất thấp',
};
