import type { SerialIssueDto } from './types';

/**
 * Đóng tập theo khoảng số (IV.4).
 *
 * Số hiệu là chuỗi nên không so sánh số học; thứ tự lấy theo ngày phát hành dự kiến, cùng cách với
 * máy chủ. Màn hình dùng hai hàm này để hiện trước "sẽ đóng N số" và cảnh báo khoảng rỗng trước
 * khi gửi.
 */

/** Số đã nhận của một năm, xếp theo ngày phát hành — chỉ những số này đóng được thành tập. */
export function bindableIssues(issues: SerialIssueDto[], year: number): SerialIssueDto[] {
  return issues
    .filter((issue) => issue.year === year && issue.status === 'Received')
    .sort((a, b) => a.expectedDate.localeCompare(b.expectedDate));
}

/** Đoạn từ số → đến số trong các số đóng được; bỏ trống một đầu thì lấy tới hết về phía ấy. */
export function issuesInRange(
  issues: SerialIssueDto[],
  year: number,
  fromIssue: string | null | undefined,
  toIssue: string | null | undefined,
): SerialIssueDto[] {
  const ordered = bindableIssues(issues, year);
  const start = fromIssue ? ordered.findIndex((issue) => issue.issueNo === fromIssue) : 0;
  const end = toIssue ? ordered.findIndex((issue) => issue.issueNo === toIssue) : ordered.length - 1;

  if (start < 0 || end < 0 || end < start) return [];

  return ordered.slice(start, end + 1);
}
