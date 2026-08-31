import { describe, expect, it } from 'vitest';
import {
  describeExpiry,
  dimensionLabels,
  formatDate,
  groupingLabels,
  initials,
  loanStatusLabels,
  money,
  readerStatusColors,
  readerStatusLabels,
} from './labels';
import type { LoanStatus, ReaderStatus } from './types';

describe('Nhãn tiếng Việt của Phân hệ VI', () => {
  it('phủ hết mọi trạng thái thẻ bạn đọc, kèm màu để tô bảng', () => {
    const statuses: ReaderStatus[] = ['Active', 'Expired', 'Suspended', 'Locked', 'Graduated'];

    statuses.forEach((status) => {
      expect(readerStatusLabels[status]).toBeTruthy();
      expect(readerStatusColors[status]).toBeTruthy();
    });
  });

  it('phủ hết mọi trạng thái phiếu mượn hiện trên tab lịch sử', () => {
    const statuses: LoanStatus[] = ['Active', 'Returned', 'Overdue', 'Lost', 'Damaged'];

    statuses.forEach((status) => expect(loanStatusLabels[status]).toBeTruthy());
  });

  it('phủ hết chiều thống kê và bước thời gian của báo cáo', () => {
    Object.values(dimensionLabels).forEach((label) => expect(label).toBeTruthy());
    Object.values(groupingLabels).forEach((label) => expect(label).toBeTruthy());
  });
});

describe('Diễn giải hạn thẻ', () => {
  const today = new Date(2026, 7, 31);

  it('nói rõ còn bao nhiêu ngày thay vì bắt cán bộ tự trừ ngày', () => {
    expect(describeExpiry('2026-09-12', today)).toBe('Còn 12 ngày');
  });

  it('nói rõ đã quá hạn bao nhiêu ngày', () => {
    expect(describeExpiry('2026-08-26', today)).toBe('Quá hạn 5 ngày');
  });

  it('gọi đúng tên ngày hết hạn rơi vào hôm nay', () => {
    expect(describeExpiry('2026-08-31', today)).toBe('Hết hạn hôm nay');
  });

  it('không đoán bừa khi giá trị không phải ngày', () => {
    expect(describeExpiry('không rõ', today)).toBe('');
  });
});

describe('Hiển thị ngày và tiền', () => {
  it('để trống thay vì hiện Invalid Date', () => {
    expect(formatDate(null)).toBe('');
    expect(formatDate('')).toBe('');
    expect(formatDate('không phải ngày')).toBe('');
  });

  it('viết số tiền theo cách người Việt đọc', () => {
    expect(money(1250000)).toBe('1.250.000');
    expect(money(null)).toBe('0');
  });
});

describe('Chữ cái đại diện khi bạn đọc chưa có ảnh', () => {
  it('lấy chữ đầu của tên rồi tới họ, vì người Việt gọi nhau bằng tên', () => {
    expect(initials('Nguyễn Văn An')).toBe('AN');
    expect(initials('Trần Bình')).toBe('BT');
  });

  it('chịu được tên một chữ và tên rỗng', () => {
    expect(initials('An')).toBe('A');
    expect(initials('   ')).toBe('?');
  });
});
