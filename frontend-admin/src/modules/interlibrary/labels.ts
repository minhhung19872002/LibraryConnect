import type { RemoteSearchField } from './types';

// Cách viết ngày giờ nằm ở lib/datetime để mọi màn hình ra cùng một dạng dd/MM/yyyy;
// trước đây mỗi phân hệ tự viết một hàm nên có chỗ in 5/9/2029, chỗ in 05/09/2029.
export { formatDate, formatDateTime } from '@/lib/datetime';

/** Nhãn tiếng Việt của phân hệ liên thư viện. */

export const searchFieldLabels: Record<RemoteSearchField, string> = {
  Any: 'Bất kỳ',
  Title: 'Nhan đề',
  Author: 'Tác giả',
  Isbn: 'ISBN',
  Issn: 'ISSN',
  Subject: 'Chủ đề',
  Publisher: 'Nhà xuất bản',
};

/**
 * Mã tiêu chí Bib-1 tương ứng, hiện kèm nhãn để cán bộ đối chiếu với tài liệu của thư viện bạn.
 *
 * Những con số này là chuẩn quốc tế: thư viện nào cũng dùng 4 cho nhan đề, 7 cho ISBN.
 */
export const bib1UseCodes: Record<RemoteSearchField, number | null> = {
  Any: 1016,
  Title: 4,
  Author: 1,
  Isbn: 7,
  Issn: 8,
  Subject: 21,
  Publisher: 1018,
};

export const charsetOptions = ['UTF-8', 'MARC-8', 'ISO-8859-1'].map((value) => ({
  value,
  label: value,
}));

export const recordSyntaxOptions = [
  { value: 'USMARC', label: 'USMARC / MARC 21' },
  { value: 'UNIMARC', label: 'UNIMARC' },
  { value: 'XML', label: 'MARCXML' },
];

export const metadataPrefixOptions = [
  { value: 'oai_dc', label: 'Dublin Core (oai_dc)' },
  { value: 'marc21', label: 'MARC 21 (marc21)' },
];

export const harvestStatusLabels: Record<string, string> = {
  Pending: 'Chờ chạy',
  Running: 'Đang chạy',
  Completed: 'Hoàn thành',
  Failed: 'Thất bại',
  Cancelled: 'Đã hủy',
};

export const harvestStatusColors: Record<string, string> = {
  Pending: 'default',
  Running: 'processing',
  Completed: 'green',
  Failed: 'red',
  Cancelled: 'default',
};

/** Mô tả một máy chủ đích bằng một dòng, đúng cách cán bộ đọc trong tài liệu của thư viện bạn. */
export function describeTarget(target: {
  useSru: boolean;
  sruBaseUrl: string | null;
  host: string;
  port: number;
  databaseName: string;
}): string {
  return target.useSru
    ? target.sruBaseUrl ?? ''
    : `${target.host}:${target.port}/${target.databaseName}`;
}

/** Đổi mili giây sang cách nói quen thuộc, vì máy chủ ở nước ngoài hay mất vài giây. */
export function formatDuration(ms: number | null | undefined): string {
  if (ms === null || ms === undefined) return '';
  return ms < 1000 ? `${ms} ms` : `${(ms / 1000).toFixed(1)} giây`;
}

/**
 * Bao lâu hỏi lại nhật ký thu hoạch một lần.
 *
 * Thu hoạch chạy ở tiến trình nền nên màn hình phải tự hỏi lại mới thấy tiến độ; hết việc thì thôi
 * hỏi, tránh gõ cửa máy chủ suốt ngày khi không có gì chạy.
 */
export function harvestPollInterval(
  logs: { status: string }[] | undefined,
): number | false {
  const dangChay = (logs ?? []).some(
    (log) => log.status === 'Running' || log.status === 'Pending',
  );

  return dangChay ? 3000 : false;
}
