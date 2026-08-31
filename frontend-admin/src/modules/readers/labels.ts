import type {
  LoanStatus,
  ReaderReportDimension,
  ReaderStatus,
  ReaderTimeGrouping,
} from './types';

/** Nhãn tiếng Việt của Phân hệ VI, đặt một chỗ để không màn hình nào lỡ hiện tiếng Anh. */

export const readerStatusLabels: Record<ReaderStatus, string> = {
  Active: 'Hoạt động',
  Expired: 'Hết hạn',
  Suspended: 'Tạm khóa',
  Locked: 'Khóa',
  Graduated: 'Đã ra trường',
};

export const readerStatusColors: Record<ReaderStatus, string> = {
  Active: 'green',
  Expired: 'orange',
  Suspended: 'red',
  Locked: 'red',
  Graduated: 'default',
};

export const loanStatusLabels: Record<LoanStatus, string> = {
  Active: 'Đang mượn',
  Returned: 'Đã trả',
  Overdue: 'Quá hạn',
  Lost: 'Mất',
  Damaged: 'Hỏng',
};

export const loanStatusColors: Record<LoanStatus, string> = {
  Active: 'processing',
  Returned: 'default',
  Overdue: 'red',
  Lost: 'red',
  Damaged: 'orange',
};

export const dimensionLabels: Record<ReaderReportDimension, string> = {
  ReaderType: 'Loại bạn đọc',
  Faculty: 'Khoa',
  Major: 'Ngành đào tạo',
  Cohort: 'Khóa',
  Class: 'Lớp',
  Status: 'Trạng thái thẻ',
  Gender: 'Giới tính',
};

export const groupingLabels: Record<ReaderTimeGrouping, string> = {
  Day: 'Theo ngày',
  Month: 'Theo tháng',
  Quarter: 'Theo quý',
  Year: 'Theo năm',
};

export const genderOptions = ['Nam', 'Nữ', 'Khác'].map((value) => ({ value, label: value }));

export const duplicateActionOptions = [
  { value: 0, label: 'Báo lỗi dòng trùng (an toàn nhất)' },
  { value: 1, label: 'Bỏ qua, giữ nguyên hồ sơ đang có' },
  { value: 2, label: 'Cập nhật hồ sơ đang có' },
];

/** Ngày dạng người Việt đọc; chuỗi rỗng khi không có giá trị, để ô bảng không hiện "Invalid Date". */
export function formatDate(value: string | null | undefined): string {
  if (!value) return '';

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '' : date.toLocaleDateString('vi-VN');
}

export function formatDateTime(value: string | null | undefined): string {
  if (!value) return '';

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '' : date.toLocaleString('vi-VN');
}

export function money(value: number | null | undefined): string {
  return (value ?? 0).toLocaleString('vi-VN');
}

/**
 * Diễn giải hạn thẻ thành câu người đọc hiểu ngay.
 *
 * Cán bộ ở quầy không có thời gian trừ ngày trong đầu; câu "còn 12 ngày" hay "quá hạn 5 ngày" mới là
 * thứ quyết định có cho mượn hay không.
 */
export function describeExpiry(expireDate: string, today = new Date()): string {
  const expiry = new Date(expireDate);

  if (Number.isNaN(expiry.getTime())) {
    return '';
  }

  const start = Date.UTC(today.getFullYear(), today.getMonth(), today.getDate());
  const end = Date.UTC(expiry.getFullYear(), expiry.getMonth(), expiry.getDate());
  const days = Math.round((end - start) / 86_400_000);

  if (days < 0) return `Quá hạn ${Math.abs(days)} ngày`;
  if (days === 0) return 'Hết hạn hôm nay';

  return `Còn ${days} ngày`;
}

/** Chữ cái đầu của họ tên, dùng làm ảnh đại diện thay thế khi bạn đọc chưa có ảnh. */
export function initials(fullName: string): string {
  const parts = fullName.trim().split(/\s+/).filter(Boolean);

  if (parts.length === 0) return '?';

  // Người Việt gọi nhau bằng tên chứ không bằng họ, nên lấy chữ cái của tên trước.
  const last = parts[parts.length - 1]?.[0] ?? '';
  const first = parts.length > 1 ? (parts[0]?.[0] ?? '') : '';

  return `${last}${first}`.toUpperCase();
}

/**
 * Tên tiếng Việt của từng trường dữ liệu bạn đọc khi nhập từ tệp ngoài.
 *
 * Khóa là tên trường mà máy chủ dùng; màn hình ánh xạ cột phải hiện tên tiếng Việt, vì người khai
 * ánh xạ là cán bộ thư viện chứ không phải người viết phần mềm.
 */
export const importFieldLabels: Record<string, string> = {
  cardNumber: 'Số thẻ',
  studentCode: 'Mã sinh viên',
  fullName: 'Họ và tên',
  gender: 'Giới tính',
  dateOfBirth: 'Ngày sinh',
  idCardNumber: 'Số CCCD',
  email: 'Email',
  phone: 'Điện thoại',
  address: 'Địa chỉ',
  readerType: 'Loại bạn đọc',
  faculty: 'Khoa',
  major: 'Ngành đào tạo',
  className: 'Lớp',
  courseYear: 'Khóa',
  cardIssueDate: 'Ngày cấp thẻ',
  cardExpireDate: 'Ngày hết hạn',
  note: 'Ghi chú',
};
