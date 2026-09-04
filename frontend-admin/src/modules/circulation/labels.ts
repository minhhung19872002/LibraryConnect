import type {
  FineType,
  HoldStatus,
  LoanChannel,
  LoanRowDto,
  LoanStatus,
  LoanType,
  LockerRowDto,
  LockerStatus,
} from './types';

// Cách viết ngày giờ nằm ở lib/datetime để mọi màn hình ra cùng một dạng dd/MM/yyyy;
// trước đây mỗi phân hệ tự viết một hàm nên có chỗ in 5/9/2029, chỗ in 05/09/2029.
export { formatDate, formatDateTime } from '@/lib/datetime';


/** Nhãn tiếng Việt của Phân hệ VII, đặt một chỗ để mọi màn hình nói cùng một thứ tiếng. */

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

export const loanTypeLabels: Record<LoanType, string> = {
  InHouse: 'Đọc tại chỗ',
  TakeHome: 'Mượn về nhà',
  SelfCheckout: 'Tự phục vụ',
};

export const channelLabels: Record<LoanChannel, string> = {
  Desk: 'Quầy',
  Opac: 'Trang tra cứu',
  Mobile: 'Ứng dụng di động',
};

export const holdStatusLabels: Record<HoldStatus, string> = {
  Waiting: 'Đang chờ',
  Ready: 'Sẵn sàng nhận',
  Fulfilled: 'Đã nhận',
  Expired: 'Hết hạn giữ',
  Cancelled: 'Đã hủy',
};

export const holdStatusColors: Record<HoldStatus, string> = {
  Waiting: 'processing',
  Ready: 'green',
  Fulfilled: 'default',
  Expired: 'orange',
  Cancelled: 'default',
};

export const fineTypeLabels: Record<FineType, string> = {
  Overdue: 'Quá hạn',
  Lost: 'Làm mất',
  Damaged: 'Làm hỏng',
  Other: 'Khác',
};

export const lockerStatusLabels: Record<LockerStatus, string> = {
  Free: 'Trống',
  InUse: 'Đang dùng',
  Broken: 'Hỏng',
  Locked: 'Khóa',
};

/** Màu ô tủ trên sơ đồ: xanh là trống, cam là đang dùng, đỏ là hỏng. */
export const lockerStatusColors: Record<LockerStatus, string> = {
  Free: '#52c41a',
  InUse: '#faad14',
  Broken: '#f5222d',
  Locked: '#8c8c8c',
};

export function money(value: number | null | undefined): string {
  return (value ?? 0).toLocaleString('vi-VN');
}

/**
 * Diễn giải hạn trả thành câu cán bộ đọc được ngay ở quầy.
 *
 * Ở quầy không ai có thời gian trừ ngày trong đầu; "quá hạn 3 ngày" mới là thứ quyết định có thu
 * tiền phạt hay không.
 */
export function describeDue(dueDate: string, today = new Date()): string {
  const due = new Date(dueDate);

  if (Number.isNaN(due.getTime())) return '';

  const start = Date.UTC(today.getFullYear(), today.getMonth(), today.getDate());
  const end = Date.UTC(due.getFullYear(), due.getMonth(), due.getDate());
  const days = Math.round((end - start) / 86_400_000);

  if (days < 0) return `Quá hạn ${Math.abs(days)} ngày`;
  if (days === 0) return 'Đến hạn hôm nay';

  return `Còn ${days} ngày`;
}

/**
 * Phản hồi bằng âm thanh ở màn hình quét (VII.2).
 *
 * Cán bộ ở quầy nhìn vào chồng sách và máy quét chứ không nhìn màn hình, nên tiếng bíp mới là thứ
 * báo quét được hay không. Dùng bộ tạo âm sẵn có của trình duyệt để khỏi phải tải tệp âm thanh —
 * quầy thư viện thường không có loa ngoài và mạng thì chậm.
 */
export function beep(kind: 'ok' | 'error'): void {
  try {
    const AudioContextClass =
      window.AudioContext ??
      (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;

    if (!AudioContextClass) return;

    const context = new AudioContextClass();
    const oscillator = context.createOscillator();
    const gain = context.createGain();

    oscillator.type = 'sine';
    oscillator.frequency.value = kind === 'ok' ? 880 : 220;
    gain.gain.value = 0.08;

    oscillator.connect(gain);
    gain.connect(context.destination);

    oscillator.start();

    window.setTimeout(
      () => {
        oscillator.stop();
        void context.close();
      },
      kind === 'ok' ? 120 : 320,
    );
  } catch {
    // Trình duyệt chặn âm thanh khi chưa có tương tác của người dùng — không phải lỗi đáng báo.
  }
}

/** Mã cảnh báo máy chủ trả về khi bản in thuộc kho đang đóng để kiểm kê (III.4 bước 1). */
export const WAREHOUSE_CLOSED_WARNING = 'WAREHOUSE_CLOSED';

/**
 * Câu cảnh báo trên đầu màn hình quầy khi có kho đang đóng để kiểm kê. Trả về null khi không kho
 * nào đóng, để màn hình không phải tự đếm.
 */
export function closedWarehouseNotice(
  warehouses: ReadonlyArray<{ name: string; isClosedForInventory: boolean }>,
): string | null {
  const closed = warehouses.filter((warehouse) => warehouse.isClosedForInventory);

  if (closed.length === 0) return null;

  const names = closed.map((warehouse) => warehouse.name).join(', ');

  return closed.length === 1
    ? `Kho ${names} đang đóng để kiểm kê: không ghi mượn sách của kho này; sách trả về thì giữ ở quầy, chưa xếp lên giá.`
    : `${closed.length} kho đang đóng để kiểm kê (${names}): không ghi mượn sách của các kho này; sách trả về thì giữ ở quầy, chưa xếp lên giá.`;
}

/** Sơ đồ tủ dựng từ hàng/cột máy chủ trả về (VII.3). */
export interface LockerGrid {
  rows: number;
  columns: number;
  /** Tủ có toạ độ, tra theo khoá "hàng-cột". */
  placed: Map<string, LockerRowDto>;
  /** Tủ chưa khai hàng/cột — vẫn phải hiện để giao được, xếp thành dải riêng bên dưới. */
  unplaced: LockerRowDto[];
}

export function lockerGridKey(row: number, column: number): string {
  return `${row}-${column}`;
}

/**
 * Xếp tủ lên lưới theo `mapRow`/`mapColumn`. Hai tủ khai trùng ô thì tủ sau rơi xuống dải "chưa
 * xếp" thay vì đè lên nhau — đè là mất một tủ trên sơ đồ mà không ai biết.
 */
export function buildLockerGrid(lockers: ReadonlyArray<LockerRowDto>): LockerGrid {
  const placed = new Map<string, LockerRowDto>();
  const unplaced: LockerRowDto[] = [];
  let rows = 0;
  let columns = 0;

  lockers.forEach((locker) => {
    if (locker.mapRow == null || locker.mapColumn == null || locker.mapRow < 1 || locker.mapColumn < 1) {
      unplaced.push(locker);
      return;
    }

    const key = lockerGridKey(locker.mapRow, locker.mapColumn);

    if (placed.has(key)) {
      unplaced.push(locker);
      return;
    }

    placed.set(key, locker);
    rows = Math.max(rows, locker.mapRow);
    columns = Math.max(columns, locker.mapColumn);
  });

  return { rows, columns, placed, unplaced };
}

/** Một cột của biểu đồ "đang mượn theo hạn trả" (báo cáo VII.5.2). */
export interface DueBucket {
  key: 'overdue' | 'today' | 'soon' | 'week' | 'later';
  label: string;
  count: number;
}

/**
 * Gom các phiếu đang mượn theo còn bao nhiêu ngày tới hạn, để nhìn một cái là biết hôm nay quầy
 * sẽ bận thế nào. Ngày lấy theo đầu ngày, không tính giờ.
 */
export function bucketLoansByDue(loans: ReadonlyArray<LoanRowDto>, today = new Date()): DueBucket[] {
  const buckets: DueBucket[] = [
    { key: 'overdue', label: 'Đã quá hạn', count: 0 },
    { key: 'today', label: 'Hạn hôm nay', count: 0 },
    { key: 'soon', label: '1–3 ngày', count: 0 },
    { key: 'week', label: '4–7 ngày', count: 0 },
    { key: 'later', label: 'Trên 7 ngày', count: 0 },
  ];

  const startOfToday = new Date(today.getFullYear(), today.getMonth(), today.getDate()).getTime();
  const dayMs = 24 * 60 * 60 * 1000;

  loans.forEach((loan) => {
    const due = new Date(loan.dueDate);
    const startOfDue = new Date(due.getFullYear(), due.getMonth(), due.getDate()).getTime();
    const days = Math.round((startOfDue - startOfToday) / dayMs);

    const bucket = days < 0
      ? buckets[0]
      : days === 0
        ? buckets[1]
        : days <= 3
          ? buckets[2]
          : days <= 7
            ? buckets[3]
            : buckets[4];

    if (bucket) bucket.count += 1;
  });

  return buckets;
}

/** Đếm phiếu đang mượn theo một chiều (loại bạn đọc, kho…), sắp giảm dần, gộp phần đuôi thành "Khác". */
export function countLoansBy(
  loans: ReadonlyArray<LoanRowDto>,
  pick: (loan: LoanRowDto) => string | null | undefined,
  limit = 8,
): { name: string; count: number }[] {
  const counts = new Map<string, number>();

  loans.forEach((loan) => {
    const name = pick(loan)?.trim() || 'Chưa rõ';
    counts.set(name, (counts.get(name) ?? 0) + 1);
  });

  const sorted = Array.from(counts.entries())
    .map(([name, count]) => ({ name, count }))
    .sort((a, b) => b.count - a.count || a.name.localeCompare(b.name, 'vi'));

  if (sorted.length <= limit) return sorted;

  const head = sorted.slice(0, limit - 1);
  const tail = sorted.slice(limit - 1).reduce((sum, item) => sum + item.count, 0);

  return [...head, { name: 'Khác', count: tail }];
}
