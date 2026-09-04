import type { DigitalRequestRow, FineRow, HoldRow, LoanRow } from '@/types/api';

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

/**
 * Câu "tìm thấy bao nhiêu tài liệu" ở đầu trang kết quả.
 *
 * Với câu hỏi rộng trên kho lớn, máy chủ dừng đếm ở một ngưỡng và báo lại bằng cờ capped: hiện con
 * số ấy như thể chính xác là nói sai với bạn đọc, mà bỏ trống thì họ không biết nhiều hay ít.
 */
export function describeResultCount(total: number, capped?: boolean): string {
  const number = total.toLocaleString('vi-VN');
  return capped ? `Tìm thấy hơn ${number} tài liệu` : `Tìm thấy ${number} tài liệu`;
}

/** Trạng thái một yêu cầu đọc tài liệu hạn chế, nhìn từ phía bạn đọc (IX.3). */
export function describeDigitalRequest(
  status: DigitalRequestRow['status'],
): { label: string; color: string } {
  switch (status) {
    case 'Approved':
      return { label: 'Đã duyệt', color: 'green' };
    case 'Rejected':
      return { label: 'Từ chối', color: 'red' };
    case 'Expired':
      return { label: 'Hết hạn', color: 'default' };
    default:
      return { label: 'Chờ duyệt', color: 'blue' };
  }
}
