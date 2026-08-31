import type { FineRow, HoldRow, LoanRow } from '@/types/api';

/**
 * Chữ tiếng Việt cho các trạng thái nghiệp vụ.
 *
 * Máy chủ trả về tên hằng số bằng tiếng Anh ("Waiting", "Overdue") vì đó là tên trong mã nguồn.
 * Bạn đọc không được nhìn thấy những chữ đó — toàn bộ giao diện phải bằng tiếng Việt. Dịch ở một
 * chỗ duy nhất để mọi màn hình gọi cùng một tên cho cùng một trạng thái.
 */
export function describeHoldStatus(status: HoldRow['status']): string {
  switch (status) {
    case 'Waiting':
      return 'Đang chờ';
    case 'Ready':
      return 'Sách đã sẵn sàng';
    case 'Fulfilled':
      return 'Đã nhận sách';
    case 'Expired':
      return 'Hết hạn giữ';
    default:
      return 'Đã hủy';
  }
}

export function describeLoanStatus(status: LoanRow['status']): string {
  switch (status) {
    case 'Active':
      return 'Đang mượn';
    case 'Returned':
      return 'Đã trả';
    case 'Overdue':
      return 'Quá hạn';
    case 'Lost':
      return 'Báo mất';
    default:
      return 'Báo hỏng';
  }
}

export function describeFineType(type: FineRow['type']): string {
  switch (type) {
    case 'Overdue':
      return 'Quá hạn';
    case 'Lost':
      return 'Làm mất';
    case 'Damaged':
      return 'Làm hỏng';
    default:
      return 'Khác';
  }
}

/** Màu của thẻ trạng thái đặt giữ: xanh khi sách đã sẵn sàng để bạn đọc tới lấy. */
export function holdStatusColor(status: HoldRow['status']): string | undefined {
  switch (status) {
    case 'Ready':
      return 'green';
    case 'Waiting':
      return 'blue';
    case 'Fulfilled':
      return undefined;
    default:
      return 'default';
  }
}
