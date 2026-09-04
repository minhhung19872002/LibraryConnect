import type {
  ReaderCardDto,
  ReaderClearanceDto,
  LoanStatus,
  ReaderReportDimension,
  ReaderStatus,
  ReaderTimeGrouping,
} from './types';

// Cách viết ngày giờ nằm ở lib/datetime để mọi màn hình ra cùng một dạng dd/MM/yyyy;
// trước đây mỗi phân hệ tự viết một hàm nên có chỗ in 5/9/2029, chỗ in 05/09/2029.
export { formatDate, formatDateTime } from '@/lib/datetime';
import { formatDate } from '@/lib/datetime';


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

/** Câu báo sau khi cấp lại thẻ: nói rõ số cũ thu hồi hay giữ, số mới là gì, hạn tới ngày nào. */
export function reissueSummary(previousCardNumber: string, card: ReaderCardDto): string {
  const expiry = formatDate(card.expireDate);

  return card.cardNumber === previousCardNumber
    ? `Đã cấp lại thẻ, giữ nguyên số ${card.cardNumber}; hạn thẻ đến ${expiry}.`
    : `Đã cấp thẻ mới số ${card.cardNumber} (thẻ cũ ${previousCardNumber} thu hồi); hạn thẻ đến ${expiry}.`;
}

/**
 * Nút "In giấy xác nhận trả sách" chỉ bấm được khi bạn đọc không còn nợ gì; còn nợ thì khóa nút và
 * nói rõ vì sao, thay vì in ra một tờ giấy ghi "còn nợ".
 */
export function clearancePrintState(
  clearance: ReaderClearanceDto | undefined,
): { disabled: boolean; reason: string | null } {
  if (!clearance) {
    return { disabled: true, reason: 'Đang kiểm tra công nợ…' };
  }

  if (clearance.cleared) {
    return { disabled: false, reason: null };
  }

  const reasons = clearance.blockers.length > 0
    ? clearance.blockers.join(' ')
    : 'Bạn đọc còn tài liệu chưa trả hoặc còn nợ phí.';

  return { disabled: true, reason: `Chưa in được: ${reasons}` };
}

/**
 * Thân yêu cầu in thẻ. Xem trước gửi `preview: true` để máy chủ không tính thêm một lần in — số lần
 * in là căn cứ khi bạn đọc xin cấp lại thẻ, nên không được đội lên vì cán bộ mở ra nhìn.
 */
export function cardPrintRequest(
  selection: Record<string, unknown>,
  values: { templateId?: string | null; multiplePerPage?: boolean },
  preview: boolean,
): Record<string, unknown> {
  return {
    selection,
    templateId: values.templateId ?? undefined,
    multiplePerPage: values.multiplePerPage ?? true,
    preview,
  };
}

export type ReaderSyncItem = Record<string, string | null>;

/**
 * Đọc dữ liệu dán từ hệ thống đào tạo: nhận một mảng JSON, hoặc một đối tượng có `items`, mỗi phần
 * tử là túi khóa–giá trị. Trả về lỗi tiếng Việt nói rõ chỗ sai thay vì ném ngoại lệ JSON.
 */
export function parseSyncItems(text: string): { items: ReaderSyncItem[]; error: string | null } {
  const trimmed = text.trim();

  if (!trimmed) {
    return { items: [], error: 'Chưa dán dữ liệu nào.' };
  }

  let parsed: unknown;

  try {
    parsed = JSON.parse(trimmed);
  } catch {
    return { items: [], error: 'Dữ liệu không phải JSON hợp lệ.' };
  }

  const list = Array.isArray(parsed)
    ? parsed
    : parsed && typeof parsed === 'object' && Array.isArray((parsed as { items?: unknown }).items)
      ? ((parsed as { items: unknown[] }).items)
      : null;

  if (!list) {
    return { items: [], error: 'Cần một mảng JSON các bản ghi sinh viên, hoặc đối tượng có trường "items".' };
  }

  const items: ReaderSyncItem[] = [];

  for (let index = 0; index < list.length; index += 1) {
    const entry = list[index];

    if (!entry || typeof entry !== 'object' || Array.isArray(entry)) {
      return { items: [], error: `Bản ghi thứ ${index + 1} không phải đối tượng khóa–giá trị.` };
    }

    const item: ReaderSyncItem = {};

    Object.entries(entry as Record<string, unknown>).forEach(([key, value]) => {
      item[key] = value === null || value === undefined ? null : String(value);
    });

    items.push(item);
  }

  if (items.length === 0) {
    return { items: [], error: 'Mảng dữ liệu rỗng.' };
  }

  return { items, error: null };
}

/** Câu tóm tắt kết quả đồng bộ, phân biệt rõ lần thử với lần ghi thật. */
export function syncSummary(result: {
  dryRun: boolean;
  totalItems: number;
  created: number;
  updated: number;
  skipped: number;
  errorItems: number;
}): string {
  const body = `${result.totalItems} bản ghi: thêm ${result.created}, cập nhật ${result.updated}` +
    (result.skipped > 0 ? `, bỏ qua ${result.skipped}` : '') +
    (result.errorItems > 0 ? `, lỗi ${result.errorItems}` : '');

  return result.dryRun ? `Thử (chưa ghi) — ${body}.` : `Đã đồng bộ — ${body}.`;
}
