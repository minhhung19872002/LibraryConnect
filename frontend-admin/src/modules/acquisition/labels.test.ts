import { describe, expect, it } from 'vitest';
import {
  acquisitionTypeLabels,
  disposalTypes,
  formatDate,
  inventoryResultLabels,
  itemStatusColors,
  itemStatusLabels,
  money,
  orderStatusLabels,
  requestStatusLabels,
  warehouseTypeLabels,
} from './labels';
import type { ItemStatus } from './types';

describe('Nhãn tiếng Việt của Phân hệ III', () => {
  it('phủ hết mọi trạng thái ấn phẩm', () => {
    const statuses: ItemStatus[] = [
      'PendingInspection',
      'InStock',
      'OnLoan',
      'OnHoldShelf',
      'Lost',
      'Damaged',
      'Discarded',
      'UnderInventory',
    ];

    statuses.forEach((status) => {
      expect(itemStatusLabels[status]).toBeTruthy();
      expect(itemStatusColors[status]).toBeTruthy();
    });
  });

  it('không để sót nhãn ở các bảng trị liệt kê còn lại', () => {
    [
      warehouseTypeLabels,
      acquisitionTypeLabels,
      requestStatusLabels,
      orderStatusLabels,
      inventoryResultLabels,
    ].forEach((table) => {
      Object.values(table).forEach((label) => expect(label).toBeTruthy());
    });
  });

  it('chỉ nhận đúng ba hình thức đưa ấn phẩm ra khỏi kho', () => {
    // Máy chủ từ chối mọi giá trị khác, nên danh sách trên màn hình phải khớp đúng.
    expect([...disposalTypes]).toEqual(['Thanh lý', 'Mất', 'Hỏng không phục hồi']);
  });
});

describe('Định dạng số tiền', () => {
  it('nhóm hàng nghìn theo cách viết Việt Nam', () => {
    expect(money(1250000)).toBe('1.250.000');
    expect(money(0)).toBe('0');
  });

  it('coi giá trị trống là 0 thay vì in ra chữ', () => {
    expect(money(null)).toBe('0');
    expect(money(undefined)).toBe('0');
  });
});

describe('Định dạng ngày', () => {
  it('đổi ngày của máy chủ sang cách đọc Việt Nam', () => {
    expect(formatDate('2026-08-31')).toBe('31/08/2026');
  });

  it('cắt phần giờ khỏi dấu thời gian đầy đủ', () => {
    expect(formatDate('2026-08-31T16:20:00+07:00')).toBe('31/08/2026');
  });

  it('trả về chuỗi rỗng khi không có ngày', () => {
    expect(formatDate(null)).toBe('');
    expect(formatDate(undefined)).toBe('');
  });
});
