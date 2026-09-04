import { describe, expect, it } from 'vitest';
import { requestLineAmount, subscriptionIssueCount } from './serialRequest';

describe('Yêu cầu đặt mua ấn phẩm định kỳ (III.1)', () => {
  it('đếm số kỳ theo tháng đặt và số kỳ mỗi năm', () => {
    expect(subscriptionIssueCount('2026-01-01', '2026-12-01', 12)).toBe(12);
    expect(subscriptionIssueCount('2026-01-01', '2026-03-01', 52)).toBe(13);
    expect(subscriptionIssueCount('2026-07-01', '2026-07-01', 4)).toBe(1);
    expect(subscriptionIssueCount('2026-09-01', '2027-02-01', 365)).toBe(183);
  });

  it('nhận cả dạng tháng-năm của ô chọn tháng', () => {
    expect(subscriptionIssueCount('2026-01', '2026-06', 12)).toBe(6);
  });

  it('thiếu dữ liệu hoặc khoảng ngược thì tính một kỳ, không ra số âm', () => {
    expect(subscriptionIssueCount(null, '2026-12-01', 12)).toBe(1);
    expect(subscriptionIssueCount('2026-01-01', null, 12)).toBe(1);
    expect(subscriptionIssueCount('2026-01-01', '2026-12-01', null)).toBe(1);
    expect(subscriptionIssueCount('2026-12-01', '2026-01-01', 12)).toBe(1);
  });

  it('thành tiền của dòng báo là số bản × số kỳ × đơn giá kỳ, của sách chỉ là số bản × đơn giá', () => {
    const line = {
      quantity: 2,
      unitPrice: 25000,
      subscriptionFrom: '2026-01-01',
      subscriptionTo: '2026-12-01',
      issuesPerYear: 12,
    };

    expect(requestLineAmount('Serial', line)).toBe(600000);
    expect(requestLineAmount('Monograph', line)).toBe(50000);
  });

  it('ô trống trên form chưa gõ xong thì thành tiền là 0 chứ không phải NaN', () => {
    expect(requestLineAmount('Serial', {})).toBe(0);
  });
});
