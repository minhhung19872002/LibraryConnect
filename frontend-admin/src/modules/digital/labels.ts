import type {
  AccessRequestStatus,
  DigitalAccessAction,
  DigitalAccessLevel,
  DigitalFileType,
} from './types';

// Cách viết ngày giờ nằm ở lib/datetime để mọi màn hình ra cùng một dạng dd/MM/yyyy;
// trước đây mỗi phân hệ tự viết một hàm nên có chỗ in 5/9/2029, chỗ in 05/09/2029.
export { formatDate, formatDateTime } from '@/lib/datetime';

/** Nhãn tiếng Việt của Phân hệ V. Mọi chuỗi hiển thị gom về đây. */

export const accessLevelLabels: Record<DigitalAccessLevel, string> = {
  Public: 'Công khai',
  Internal: 'Nội bộ',
  Restricted: 'Hạn chế',
  Forbidden: 'Cấm',
};

export const accessLevelColors: Record<DigitalAccessLevel, string> = {
  Public: 'green',
  Internal: 'blue',
  Restricted: 'orange',
  Forbidden: 'red',
};

export const accessLevelHints: Record<DigitalAccessLevel, string> = {
  Public: 'Ai cũng đọc được, kể cả khách chưa đăng nhập.',
  Internal: 'Bạn đọc đã đăng nhập mới đọc được toàn văn.',
  Restricted: 'Phải gửi yêu cầu và được cán bộ duyệt mới đọc được.',
  Forbidden: 'Chỉ hiện thông tin thư mục, không phục vụ nội dung.',
};

export const requestStatusLabels: Record<AccessRequestStatus, string> = {
  Pending: 'Chờ duyệt',
  Approved: 'Đã duyệt',
  Rejected: 'Từ chối',
  Expired: 'Hết hạn',
  Revoked: 'Đã thu hồi',
};

export const requestStatusColors: Record<AccessRequestStatus, string> = {
  Pending: 'gold',
  Approved: 'green',
  Rejected: 'red',
  Expired: 'default',
  Revoked: 'volcano',
};

export const accessActionLabels: Record<DigitalAccessAction, string> = {
  View: 'Xem',
  Download: 'Tải về',
  Print: 'In',
};

export const fileTypeLabels: Record<DigitalFileType, string> = {
  Original: 'Bản gốc',
  Preview: 'Bản xem thử',
  Thumbnail: 'Ảnh bìa',
  OcrText: 'Văn bản nhận dạng',
};

export const formatGroupLabels: Record<string, string> = {
  PDF: 'PDF',
  VIDEO: 'Video',
  AUDIO: 'Âm thanh',
  IMAGE: 'Ảnh',
  EPUB: 'EPUB',
  OFFICE: 'Tài liệu Office',
  OTHER: 'Khác',
};

/**
 * Đổi số byte sang đơn vị đọc được.
 *
 * Dùng bội số 1024 giống cách hệ điều hành hiển thị, để con số trên màn hình khớp với thứ cán bộ
 * nhìn thấy khi mở thư mục tệp.
 */
export function formatSize(bytes: number | null | undefined): string {
  if (bytes === null || bytes === undefined || Number.isNaN(bytes)) {
    return '';
  }

  if (bytes < 1024) {
    return `${bytes} byte`;
  }

  const units = ['KB', 'MB', 'GB', 'TB'];
  let value = bytes / 1024;
  let unit = 0;

  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024;
    unit += 1;
  }

  return `${value.toLocaleString('vi-VN', { maximumFractionDigits: 1 })} ${units[unit]}`;
}

/** Nhóm định dạng suy từ kiểu MIME, khớp với cách máy chủ phân nhóm. */
export function formatGroupOf(mimeType: string): string {
  if (mimeType === 'application/pdf') return 'PDF';
  if (mimeType.startsWith('video/')) return 'VIDEO';
  if (mimeType.startsWith('audio/')) return 'AUDIO';
  if (mimeType.startsWith('image/')) return 'IMAGE';
  if (mimeType === 'application/epub+zip') return 'EPUB';
  if (
    mimeType.includes('word') ||
    mimeType.includes('excel') ||
    mimeType.includes('powerpoint') ||
    mimeType.includes('officedocument')
  ) {
    return 'OFFICE';
  }
  return 'OTHER';
}

/**
 * Diễn giải quyền đọc để cán bộ biết bạn đọc sẽ thấy gì.
 *
 * So lỏng với null vì máy chủ bỏ hẳn trường này khỏi JSON khi không giới hạn số trang — so
 * chặt thì rơi vào nhánh sai và nhãn hiện ra chữ "undefined".
 */
export function describeReadable(readablePages: number | null | undefined): string {
  if (readablePages == null) return 'Đọc toàn văn';
  if (readablePages <= 0) return 'Không mở được nội dung';
  return `Chỉ xem thử ${readablePages} trang đầu`;
}
