import type { RecordStatus } from './types';

export type { RecordStatus };
export { RECORD_STATUS_LABELS } from './types';

export type DuplicateMatchBy = 'Isbn' | 'ControlNumber' | 'TitleAndAuthor';
export type DuplicateAction = 'Skip' | 'Overwrite' | 'CreateNew' | 'Merge';
export type JobStatus = 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Cancelled';

export interface BibImportOptions {
  matchBy: DuplicateMatchBy;
  onDuplicate: DuplicateAction;
  documentTypeId?: string;
  status: RecordStatus;
  addToCatalogQueue: boolean;
  createItems: boolean;
  itemQuantity: number;
  warehouseId?: string;
  fundingSourceId?: string;
}

export interface BibImportPreviewRow {
  recordNumber: number;
  title: string;
  author?: string | null;
  isbn?: string | null;
  publishYear?: number | null;
  controlNumber?: string | null;
  marcJson: string;
  duplicateOfId?: string | null;
  duplicateOfTitle?: string | null;
  duplicateOfControlNumber?: string | null;
  hasErrors: boolean;
  errors: string[];
  warnings: string[];
}

export interface MarcFileError {
  recordNumber: number;
  position: number;
  message: string;
}

export interface BibImportPreview {
  format: string;
  totalRecords: number;
  duplicateCount: number;
  invalidCount: number;
  records: BibImportPreviewRow[];
  fileErrors: MarcFileError[];
}

export interface ImportJobError {
  row: number;
  identifier?: string | null;
  message: string;
}

export interface ImportJob {
  id: string;
  type: string;
  fileName?: string | null;
  status: JobStatus;
  total: number;
  success: number;
  failed: number;
  skipped: number;
  createdByName?: string | null;
  createdAt: string;
  startedAt?: string | null;
  finishedAt?: string | null;
  errors: ImportJobError[];
  hasResultFile: boolean;
}

/** Nhãn tiếng Việt cho ba cách đối chiếu trùng. */
export const MATCH_BY_LABELS: Record<DuplicateMatchBy, string> = {
  Isbn: 'Số ISBN',
  ControlNumber: 'Số kiểm soát (001)',
  TitleAndAuthor: 'Nhan đề và tác giả',
};

/**
 * Bốn cách xử lý biểu ghi trùng, kèm giải thích hậu quả — vì đây là quyết định khó lấy lại nhất
 * trong toàn bộ luồng nhập.
 */
export const DUPLICATE_ACTION_LABELS: Record<DuplicateAction, { title: string; hint: string }> = {
  Skip: {
    title: 'Bỏ qua',
    hint: 'Giữ nguyên biểu ghi đã có, không lấy gì từ tệp. An toàn nhất.',
  },
  Merge: {
    title: 'Gộp',
    hint: 'Chỉ bổ sung những trường biểu ghi đã có còn thiếu; không đụng vào trường đã có nội dung.',
  },
  Overwrite: {
    title: 'Ghi đè',
    hint: 'Thay toàn bộ biểu ghi đã có bằng biểu ghi trong tệp. Bản cũ vẫn còn trong lịch sử phiên bản.',
  },
  CreateNew: {
    title: 'Tạo mới',
    hint: 'Chấp nhận có hai biểu ghi cho cùng một tài liệu. Chỉ dùng khi biết chắc đây là hai ấn bản khác nhau.',
  },
};

export const JOB_STATUS_LABELS: Record<JobStatus, string> = {
  Pending: 'Chờ chạy',
  Running: 'Đang chạy',
  Completed: 'Hoàn thành',
  Failed: 'Thất bại',
  Cancelled: 'Đã hủy',
};
