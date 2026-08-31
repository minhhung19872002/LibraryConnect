import type {
  SerialClaimStatus,
  SerialFrequency,
  SerialIssueStatus,
  SerialNumbering,
} from './types';

/** Nhãn tiếng Việt của Phân hệ IV, đặt một chỗ để không màn hình nào lỡ hiện tiếng Anh. */

export const frequencyLabels: Record<SerialFrequency, string> = {
  Daily: 'Nhật báo',
  Weekly: 'Tuần',
  Biweekly: 'Hai tuần một kỳ',
  SemiMonthly: 'Nửa tháng',
  Monthly: 'Tháng',
  Bimonthly: 'Hai tháng một kỳ',
  Quarterly: 'Quý',
  SemiAnnual: 'Nửa năm',
  Annual: 'Năm',
  Irregular: 'Không định kỳ',
};

/** Số kỳ trong năm suy ra từ kỳ hạn — hiện làm gợi ý trên form khai kỳ hạn. */
export const issuesPerYear: Record<SerialFrequency, number> = {
  Daily: 365,
  Weekly: 52,
  Biweekly: 26,
  SemiMonthly: 24,
  Monthly: 12,
  Bimonthly: 6,
  Quarterly: 4,
  SemiAnnual: 2,
  Annual: 1,
  Irregular: 0,
};

/** Kỳ hạn cần khai thứ trong tuần thay vì ngày trong tháng. */
export const weeklyFrequencies: SerialFrequency[] = ['Daily', 'Weekly', 'Biweekly'];

export const issueStatusLabels: Record<SerialIssueStatus, string> = {
  Expected: 'Dự kiến',
  Received: 'Đã nhận',
  Missing: 'Thiếu',
  Claimed: 'Đang khiếu nại',
  Bound: 'Đã đóng tập',
};

/** Màu ô trên lưới nhận số: xanh là đã về, đỏ là thiếu, xám là chưa tới hạn. */
export const issueStatusColors: Record<SerialIssueStatus, string> = {
  Expected: 'default',
  Received: 'green',
  Missing: 'red',
  Claimed: 'orange',
  Bound: 'blue',
};

export const claimStatusLabels: Record<SerialClaimStatus, string> = {
  Open: 'Đang chờ',
  Responded: 'Đã phản hồi',
  Resolved: 'Đã giải quyết',
  Cancelled: 'Đã hủy',
};

export const claimStatusColors: Record<SerialClaimStatus, string> = {
  Open: 'processing',
  Responded: 'warning',
  Resolved: 'success',
  Cancelled: 'default',
};

export const numberingLabels: Record<SerialNumbering, string> = {
  Continuous: 'Số liên tục, không đặt lại theo năm',
  RestartEachYear: 'Đánh lại số từ 1 mỗi năm',
  VolumeAndIssue: 'Có tập và số (Tập 12, Số 3)',
};

export const weekdays = [
  { value: 1, label: 'Thứ Hai' },
  { value: 2, label: 'Thứ Ba' },
  { value: 3, label: 'Thứ Tư' },
  { value: 4, label: 'Thứ Năm' },
  { value: 5, label: 'Thứ Sáu' },
  { value: 6, label: 'Thứ Bảy' },
  { value: 7, label: 'Chủ nhật' },
];

export const months = Array.from({ length: 12 }, (_, index) => ({
  value: index + 1,
  label: `Tháng ${index + 1}`,
}));

/**
 * ISSN theo cách viết chuẩn: bốn chữ số, dấu gạch nối, bốn ký tự.
 *
 * Máy chủ lưu dạng đã bỏ dấu gạch để tra cứu khớp dù người dùng gõ kiểu nào, nhưng dấu gạch là một
 * phần của chuẩn ISSN nên màn hình phải hiện lại — cán bộ đối chiếu với bìa tạp chí.
 */
export function formatIssn(issn: string | null | undefined): string {
  if (!issn) return '';

  const cleaned = issn.replace(/[^0-9Xx]/g, '').toUpperCase();

  return cleaned.length === 8 ? `${cleaned.slice(0, 4)}-${cleaned.slice(4)}` : issn;
}
