import { describe, expect, it } from 'vitest';
import {
  channelLabels,
  closedWarehouseNotice,
  describeDue,
  fineTypeLabels,
  formatDate,
  holdStatusColors,
  holdStatusLabels,
  loanStatusColors,
  loanStatusLabels,
  loanTypeLabels,
  lockerStatusColors,
  lockerStatusLabels,
  money,
} from './labels';
import type { FineType, HoldStatus, LoanStatus, LockerStatus } from './types';

describe('Nhãn tiếng Việt của Phân hệ VII', () => {
  it('phủ hết trạng thái phiếu mượn, kèm màu để tô bảng', () => {
    const statuses: LoanStatus[] = ['Active', 'Returned', 'Overdue', 'Lost', 'Damaged'];

    statuses.forEach((status) => {
      expect(loanStatusLabels[status]).toBeTruthy();
      expect(loanStatusColors[status]).toBeTruthy();
    });
  });

  it('phủ hết trạng thái đặt giữ và tủ gửi đồ', () => {
    const holds: HoldStatus[] = ['Waiting', 'Ready', 'Fulfilled', 'Expired', 'Cancelled'];
    const lockers: LockerStatus[] = ['Free', 'InUse', 'Broken', 'Locked'];

    holds.forEach((status) => {
      expect(holdStatusLabels[status]).toBeTruthy();
      expect(holdStatusColors[status]).toBeTruthy();
    });

    lockers.forEach((status) => {
      expect(lockerStatusLabels[status]).toBeTruthy();
      expect(lockerStatusColors[status]).toMatch(/^#/);
    });
  });

  it('phủ hết hình thức mượn, kênh giao dịch và loại phạt', () => {
    Object.values(loanTypeLabels).forEach((label) => expect(label).toBeTruthy());
    Object.values(channelLabels).forEach((label) => expect(label).toBeTruthy());

    const fines: FineType[] = ['Overdue', 'Lost', 'Damaged', 'Other'];
    fines.forEach((type) => expect(fineTypeLabels[type]).toBeTruthy());
  });
});

describe('Diễn giải hạn trả ở quầy', () => {
  const today = new Date(2026, 8, 15);

  it('nói rõ còn mấy ngày thay vì bắt cán bộ tự trừ ngày', () => {
    expect(describeDue('2026-09-22', today)).toBe('Còn 7 ngày');
  });

  it('gọi đúng tên ngày đến hạn', () => {
    expect(describeDue('2026-09-15', today)).toBe('Đến hạn hôm nay');
  });

  it('nói rõ quá hạn bao nhiêu ngày — đây là thứ quyết định có thu phạt hay không', () => {
    expect(describeDue('2026-09-10', today)).toBe('Quá hạn 5 ngày');
  });

  it('không đoán bừa khi giá trị không phải ngày', () => {
    expect(describeDue('hôm nào đó', today)).toBe('');
  });
});

describe('Hiển thị ngày và tiền', () => {
  it('để trống thay vì hiện Invalid Date', () => {
    expect(formatDate(null)).toBe('');
    expect(formatDate('không phải ngày')).toBe('');
  });

  it('viết số tiền theo cách người Việt đọc', () => {
    expect(money(2_500_000)).toBe('2.500.000');
    expect(money(null)).toBe('0');
  });
});

describe('Cảnh báo kho đang đóng để kiểm kê trên quầy', () => {
  it('không kho nào đóng thì không có cảnh báo', () => {
    expect(
      closedWarehouseNotice([
        { name: 'Kho mở', isClosedForInventory: false },
        { name: 'Kho đóng', isClosedForInventory: false },
      ]),
    ).toBeNull();
  });

  it('một kho đóng thì gọi tên kho và nói rõ hai việc: không mượn, trả thì giữ ở quầy', () => {
    const notice = closedWarehouseNotice([
      { name: 'Kho mở', isClosedForInventory: false },
      { name: 'Kho đọc Nhà Bè', isClosedForInventory: true },
    ]);

    expect(notice).toContain('Kho đọc Nhà Bè');
    expect(notice).toContain('kiểm kê');
    expect(notice).toContain('không ghi mượn');
    expect(notice).toContain('giữ ở quầy');
    expect(notice).not.toContain('Kho mở');
  });

  it('nhiều kho đóng thì đếm và liệt kê đủ tên', () => {
    const notice = closedWarehouseNotice([
      { name: 'Kho A', isClosedForInventory: true },
      { name: 'Kho B', isClosedForInventory: true },
    ]);

    expect(notice).toContain('2 kho');
    expect(notice).toContain('Kho A, Kho B');
  });
});

