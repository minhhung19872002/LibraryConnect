import type { SerialIssueDto } from './types';

/**
 * Bổ sung tổng thể (IV.3): xử lý số đến và số thiếu của nhiều đầu báo cùng lúc.
 *
 * Hai hàm thuần này là phần có thể tính sai — gán nhầm số lượng cho số khác, đếm sai số thiếu của
 * một đầu báo — nên tách khỏi màn hình để kiểm được bằng phép thử.
 */

export interface ReceiveLine {
  issueId: string;
  quantity: number;
  receivedDate: string;
}

/** Dòng gửi máy chủ cho từng số đã tick: số lượng và ngày nhận riêng, thiếu thì lấy mặc định. */
export function buildReceiveLines(
  selectedIds: string[],
  quantities: Record<string, number | null | undefined>,
  receivedDates: Record<string, string | null | undefined>,
  defaultDate: string,
): ReceiveLine[] {
  return selectedIds.map((issueId) => {
    const quantity = quantities[issueId];

    return {
      issueId,
      quantity: quantity && quantity > 0 ? quantity : 1,
      receivedDate: receivedDates[issueId] || defaultDate,
    };
  });
}

export interface UnresolvedGroup {
  serialId: string;
  serialTitle: string;
  count: number;
  /** Trong đó bao nhiêu số đã có phiếu khiếu nại đang mở. */
  claimed: number;
  oldestExpectedDate: string;
}

/** Gom số chưa về theo đầu báo, thứ tự theo tên, để nhìn ra đầu nào thiếu nhiều nhất. */
export function groupUnresolvedBySerial(issues: SerialIssueDto[]): UnresolvedGroup[] {
  const groups = new Map<string, UnresolvedGroup>();

  for (const issue of issues) {
    const current = groups.get(issue.serialId);

    if (!current) {
      groups.set(issue.serialId, {
        serialId: issue.serialId,
        serialTitle: issue.serialTitle,
        count: 1,
        claimed: issue.hasOpenClaim ? 1 : 0,
        oldestExpectedDate: issue.expectedDate,
      });
      continue;
    }

    current.count += 1;
    current.claimed += issue.hasOpenClaim ? 1 : 0;

    if (issue.expectedDate < current.oldestExpectedDate) {
      current.oldestExpectedDate = issue.expectedDate;
    }
  }

  return Array.from(groups.values()).sort((a, b) => a.serialTitle.localeCompare(b.serialTitle, 'vi'));
}
