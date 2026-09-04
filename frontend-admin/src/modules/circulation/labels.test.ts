import { describe, expect, it } from 'vitest';
import {
  bucketLoansByDue,
  buildLockerGrid,
  channelLabels,
  closedWarehouseNotice,
  countLoansBy,
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
import type { FineType, HoldStatus, LoanRowDto, LoanStatus, LockerRowDto, LockerStatus } from './types';

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

function locker(code: string, mapRow: number | null, mapColumn: number | null): LockerRowDto {
  return {
    id: code,
    code,
    libraryId: null,
    libraryName: null,
    area: 'A',
    size: null,
    status: 'Free',
    mapRow,
    mapColumn,
    note: null,
    usageId: null,
    readerId: null,
    readerName: null,
    readerCardNumber: null,
    checkinAt: null,
    keyNumber: null,
    minutesInUse: null,
    overdue: false,
  };
}

describe('Sơ đồ tủ gửi đồ theo hàng/cột (VII.3)', () => {
  it('đặt tủ đúng ô và tính đủ số hàng, số cột', () => {
    const grid = buildLockerGrid([locker('A01', 1, 1), locker('A05', 1, 5), locker('B03', 2, 3)]);

    expect(grid.rows).toBe(2);
    expect(grid.columns).toBe(5);
    expect(grid.placed.get('1-5')?.code).toBe('A05');
    expect(grid.placed.get('2-3')?.code).toBe('B03');
    expect(grid.unplaced).toHaveLength(0);
  });

  it('tủ chưa khai toạ độ hoặc khai trùng ô không bị mất — rơi xuống dải chưa xếp', () => {
    const grid = buildLockerGrid([locker('A01', 1, 1), locker('A02', 1, 1), locker('C09', null, null)]);

    expect(grid.placed.size).toBe(1);
    expect(grid.unplaced.map((item) => item.code)).toEqual(['A02', 'C09']);
  });
});

function loan(dueDate: string, extra: Partial<LoanRowDto> = {}): LoanRowDto {
  return {
    id: dueDate + Math.random(),
    code: 'PM',
    readerId: 'r',
    readerCardNumber: 'TV',
    readerName: 'A',
    readerTypeName: 'Sinh viên',
    facultyName: null,
    className: null,
    itemId: 'i',
    barcode: null,
    title: null,
    callNumber: null,
    warehouseName: 'Kho mở',
    loanDate: '2026-08-01',
    dueDate,
    returnDate: null,
    renewedCount: 0,
    maxRenewals: 2,
    status: 'Active',
    loanType: 'TakeHome',
    channel: 'Desk',
    loanByName: null,
    returnByName: null,
    fineAmount: 0,
    fineOutstanding: 0,
    overdueDays: 0,
    estimatedFine: 0,
    note: null,
    ...extra,
  };
}

describe('Biểu đồ báo cáo đang mượn (VII.5.2)', () => {
  const today = new Date(2026, 8, 4); // 04/09/2026

  it('gom phiếu theo số ngày tới hạn, tính theo ngày chứ không theo giờ', () => {
    const buckets = bucketLoansByDue(
      [
        loan('2026-09-01'),
        loan('2026-09-04T23:00:00'),
        loan('2026-09-06'),
        loan('2026-09-11'),
        loan('2026-10-01'),
        loan('2026-09-07'),
      ],
      today,
    );

    expect(buckets.map((bucket) => [bucket.key, bucket.count])).toEqual([
      ['overdue', 1],
      ['today', 1],
      ['soon', 2],
      ['week', 1],
      ['later', 1],
    ]);
  });

  it('đếm theo một chiều, sắp giảm dần và gộp đuôi thành "Khác"', () => {
    const loans = [
      loan('2026-09-10', { readerTypeName: 'Sinh viên' }),
      loan('2026-09-10', { readerTypeName: 'Sinh viên' }),
      loan('2026-09-10', { readerTypeName: 'Giảng viên' }),
      loan('2026-09-10', { readerTypeName: null }),
      loan('2026-09-10', { readerTypeName: 'Học viên' }),
    ];

    const rows = countLoansBy(loans, (item) => item.readerTypeName, 3);

    expect(rows[0]).toEqual({ name: 'Sinh viên', count: 2 });
    expect(rows).toHaveLength(3);
    expect(rows[2]).toEqual({ name: 'Khác', count: 2 });
    expect(countLoansBy(loans, (item) => item.readerTypeName)).toContainEqual({ name: 'Chưa rõ', count: 1 });
  });
});
