import type { ExportJobStatus, FullSystemExportJobDto } from './types';

/**
 * Cách hiện một lượt "Xuất toàn bộ dữ liệu hệ thống" (V.3): nhãn trạng thái, phần trăm tiến độ và
 * dòng tổng kết số lượng để cán bộ đối chiếu với bên nhận bàn giao. Tách khỏi trang để kiểm bằng
 * vitest mà không phải dựng cả AntD.
 */

export const exportStatusLabels: Record<ExportJobStatus, string> = {
  Pending: 'Đang chờ',
  Running: 'Đang chạy',
  Completed: 'Hoàn tất',
  Failed: 'Thất bại',
  Cancelled: 'Đã hủy',
};

export const exportStatusColors: Record<ExportJobStatus, string> = {
  Pending: 'default',
  Running: 'processing',
  Completed: 'success',
  Failed: 'error',
  Cancelled: 'default',
};

/** Lượt còn mở là lượt màn hình phải hỏi lại tiến độ, và là lý do nút "Xuất" bị khoá. */
export function isExportOpen(job: Pick<FullSystemExportJobDto, 'status'>): boolean {
  return job.status === 'Pending' || job.status === 'Running';
}

/** Phần trăm cho thanh tiến trình; lượt xong luôn là 100, lượt chưa có bước nào là 0. */
export function exportProgressPercent(
  job: Pick<FullSystemExportJobDto, 'status' | 'stepsDone' | 'stepsTotal'>,
): number {
  if (job.status === 'Completed') return 100;
  if (job.stepsTotal <= 0) return 0;

  return Math.min(99, Math.round((job.stepsDone / job.stepsTotal) * 100));
}

/** Dòng tổng kết: "1.234 biểu ghi · 56 tài liệu số · 351 bạn đọc · 9.502 ĐKCB · 1.603 lượt mượn". */
export function describeExportCounts(
  job: Pick<
    FullSystemExportJobDto,
    'bibCount' | 'bibSkipped' | 'digitalCount' | 'digitalFailed' | 'readerCount' | 'itemCount' | 'loanCount'
  >,
): string {
  const number = (value: number) => value.toLocaleString('vi-VN');
  const parts = [
    `${number(job.bibCount)} biểu ghi${job.bibSkipped > 0 ? ` (bỏ qua ${number(job.bibSkipped)} ở ISO 2709)` : ''}`,
    `${number(job.digitalCount)} tài liệu số${job.digitalFailed > 0 ? ` (${number(job.digitalFailed)} tệp không đọc được)` : ''}`,
    `${number(job.readerCount)} bạn đọc`,
    `${number(job.itemCount)} ĐKCB`,
    `${number(job.loanCount)} lượt mượn`,
  ];

  return parts.join(' · ');
}

/** Dung lượng gói đọc được: 1,2 GB thay vì 1288490188. */
export function formatPackageSize(bytes: number | null): string {
  if (bytes === null || bytes <= 0) return '—';

  const units = ['B', 'KB', 'MB', 'GB', 'TB'];
  let value = bytes;
  let index = 0;

  while (value >= 1024 && index < units.length - 1) {
    value /= 1024;
    index += 1;
  }

  return `${value.toLocaleString('vi-VN', { maximumFractionDigits: index === 0 ? 0 : 1 })} ${units[index]}`;
}
