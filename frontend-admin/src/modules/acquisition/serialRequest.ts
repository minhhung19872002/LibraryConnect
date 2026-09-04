import type { PurchaseRequestType } from './types';

/**
 * Tính tiền cho dòng đề nghị đặt ấn phẩm định kỳ (III.1).
 *
 * Cùng công thức với `SerialSubscription` ở máy chủ: số kỳ suy ra từ số tháng đặt và số kỳ mỗi
 * năm, thành tiền là số bản × số kỳ × đơn giá một kỳ. Màn hình tính để hiện ngay khi gõ; máy chủ
 * tính lại khi lưu, và hai con số phải bằng nhau.
 */

/** Một dòng đang gõ trên form — mọi ô đều có thể còn trống. */
export interface RequestLineDraft {
  quantity?: number | null;
  unitPrice?: number | null;
  subscriptionFrom?: string | null;
  subscriptionTo?: string | null;
  issuesPerYear?: number | null;
}

function monthIndex(value: string): number | null {
  // Nhận cả "2026-01-15" lẫn "2026-01" của ô chọn tháng.
  const match = /^(\d{4})-(\d{2})/.exec(value);

  if (!match) return null;

  return Number(match[1]) * 12 + (Number(match[2]) - 1);
}

/** Số kỳ trong khoảng đặt; thiếu dữ liệu hoặc khoảng ngược thì là một kỳ. */
export function subscriptionIssueCount(
  from: string | null | undefined,
  to: string | null | undefined,
  issuesPerYear: number | null | undefined,
): number {
  if (!from || !to || !issuesPerYear || issuesPerYear <= 0) return 1;

  const start = monthIndex(from);
  const end = monthIndex(to);

  if (start === null || end === null || end < start) return 1;

  const months = end - start + 1;

  return Math.max(1, Math.round((months * issuesPerYear) / 12));
}

/** Thành tiền một dòng theo loại yêu cầu. */
export function requestLineAmount(type: PurchaseRequestType, line: RequestLineDraft): number {
  const quantity = line.quantity ?? 0;
  const unitPrice = line.unitPrice ?? 0;

  const issues =
    type === 'Serial'
      ? subscriptionIssueCount(line.subscriptionFrom, line.subscriptionTo, line.issuesPerYear)
      : 1;

  return quantity * issues * unitPrice;
}
