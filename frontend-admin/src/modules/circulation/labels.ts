import type {
  FineType,
  HoldStatus,
  LoanChannel,
  LoanStatus,
  LoanType,
  LockerStatus,
} from './types';

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
