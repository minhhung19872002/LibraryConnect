import type {
  AcquisitionType,
  InventoryPeriodStatus,
  InventoryResultType,
  ItemStatus,
  PurchaseOrderStatus,
  PurchaseRequestStatus,
  WarehouseType,
} from './types';

/**
 * Nhãn tiếng Việt của các trị liệt kê Phân hệ III.
 *
 * Máy chủ lưu trị liệt kê bằng tiếng Anh để cơ sở dữ liệu đọc được, còn màn hình luôn hiện tiếng
 * Việt; đặt bảng dịch ở một chỗ để không có màn hình nào lỡ hiện "PendingInspection".
 */

export const warehouseTypeLabels: Record<WarehouseType, string> = {
  OpenStack: 'Kho mở',
  ClosedStack: 'Kho đóng',
  ReadingRoom: 'Phòng đọc tại chỗ',
  DiscardStore: 'Kho thanh lý',
};

export const itemStatusLabels: Record<ItemStatus, string> = {
  PendingInspection: 'Chưa kiểm nhận',
  InStock: 'Trong kho',
  OnLoan: 'Đang mượn',
  OnHoldShelf: 'Đặt giữ',
  Lost: 'Mất',
  Damaged: 'Hỏng',
  Discarded: 'Thanh lý',
  UnderInventory: 'Đang kiểm kê',
};

/** Màu thẻ trạng thái: xanh là lưu thông được, đỏ là đã ra khỏi kho. */
export const itemStatusColors: Record<ItemStatus, string> = {
  PendingInspection: 'orange',
  InStock: 'green',
  OnLoan: 'blue',
  OnHoldShelf: 'purple',
  Lost: 'red',
  Damaged: 'volcano',
  Discarded: 'default',
  UnderInventory: 'gold',
};

export const acquisitionTypeLabels: Record<AcquisitionType, string> = {
  Purchase: 'Mua',
  Donation: 'Biếu tặng',
  Exchange: 'Trao đổi',
  LegalDeposit: 'Lưu chiểu',
};

export const requestStatusLabels: Record<PurchaseRequestStatus, string> = {
  Draft: 'Nháp',
  Submitted: 'Chờ duyệt',
  Approved: 'Đã duyệt',
  PartiallyApproved: 'Duyệt một phần',
  Rejected: 'Từ chối',
  Cancelled: 'Đã hủy',
};

export const requestStatusColors: Record<PurchaseRequestStatus, string> = {
  Draft: 'default',
  Submitted: 'processing',
  Approved: 'success',
  PartiallyApproved: 'warning',
  Rejected: 'error',
  Cancelled: 'default',
};

export const orderStatusLabels: Record<PurchaseOrderStatus, string> = {
  New: 'Mới lập',
  Ordered: 'Đã đặt',
  PartiallyReceived: 'Nhận một phần',
  Received: 'Đã nhận đủ',
  Cancelled: 'Đã hủy',
};

export const orderStatusColors: Record<PurchaseOrderStatus, string> = {
  New: 'default',
  Ordered: 'processing',
  PartiallyReceived: 'warning',
  Received: 'success',
  Cancelled: 'error',
};

export const inventoryStatusLabels: Record<InventoryPeriodStatus, string> = {
  Preparing: 'Đang chuẩn bị',
  InProgress: 'Đang kiểm kê',
  Closed: 'Đã chốt',
};

export const inventoryResultLabels: Record<InventoryResultType, string> = {
  Match: 'Khớp',
  Missing: 'Thiếu',
  Unexpected: 'Thừa',
  WrongWarehouse: 'Sai kho',
};

export const inventoryResultColors: Record<InventoryResultType, string> = {
  Match: 'green',
  Missing: 'red',
  Unexpected: 'orange',
  WrongWarehouse: 'purple',
};

/** Hình thức đưa ấn phẩm ra khỏi kho — đúng ba giá trị máy chủ chấp nhận. */
export const disposalTypes = ['Thanh lý', 'Mất', 'Hỏng không phục hồi'] as const;

const currency = new Intl.NumberFormat('vi-VN');

/** Số tiền theo cách viết Việt Nam, không kèm đơn vị vì cột đã ghi "(VNĐ)". */
export function money(value: number | null | undefined): string {
  return currency.format(value ?? 0);
}

/** Ngày theo dd/MM/yyyy; ô ngày của máy chủ là chuỗi yyyy-MM-dd. */
export function formatDate(value: string | null | undefined): string {
  if (!value) return '';

  const parts = value.slice(0, 10).split('-');
  return parts.length === 3 ? `${parts[2]}/${parts[1]}/${parts[0]}` : value;
}
