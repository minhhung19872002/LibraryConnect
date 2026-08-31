import { describe, expect, it } from 'vitest';
import {
  describeFineType,
  describeHoldStatus,
  describeLoanStatus,
  describeResultCount,
} from '@/labels';

/**
 * Không để tên hằng số tiếng Anh của máy chủ lọt ra màn hình bạn đọc.
 *
 * "Waiting" hay "Overdue" là tên trong mã nguồn; bạn đọc nhìn thấy chúng thì trang tra cứu trông
 * như bản dịch dở dang. Phép thử duyệt hết mọi giá trị để một trạng thái mới thêm sau này không
 * lặng lẽ hiện ra bằng tiếng Anh.
 */
describe('Chữ hiển thị cho trạng thái nghiệp vụ', () => {
  const asciiOnly = /^[\x20-\x7E]+$/;

  it('dịch đủ mọi trạng thái đặt giữ', () => {
    const statuses = ['Waiting', 'Ready', 'Fulfilled', 'Expired', 'Cancelled'] as const;

    statuses.forEach((status) => {
      const label = describeHoldStatus(status);
      expect(label).not.toBe(status);
      expect(asciiOnly.test(label)).toBe(false);
    });

    expect(describeHoldStatus('Ready')).toBe('Sách đã sẵn sàng');
  });

  it('dịch đủ mọi trạng thái mượn trả', () => {
    const statuses = ['Active', 'Returned', 'Overdue', 'Lost', 'Damaged'] as const;

    statuses.forEach((status) => {
      expect(asciiOnly.test(describeLoanStatus(status))).toBe(false);
    });

    expect(describeLoanStatus('Overdue')).toBe('Quá hạn');
  });

  it('dịch đủ mọi loại tiền phạt', () => {
    const types = ['Overdue', 'Lost', 'Damaged', 'Other'] as const;

    types.forEach((type) => {
      expect(asciiOnly.test(describeFineType(type))).toBe(false);
    });

    expect(describeFineType('Lost')).toBe('Làm mất');
  });
});

describe('Số kết quả tra cứu', () => {
  it('nói rõ là "hơn" khi máy chủ dừng đếm giữa chừng', () => {
    expect(describeResultCount(10000, true)).toBe('Tìm thấy hơn 10.000 tài liệu');
  });

  it('nói con số chính xác khi đếm hết', () => {
    expect(describeResultCount(1234)).toBe('Tìm thấy 1.234 tài liệu');
  });

  it('không có kết quả nào thì vẫn là một câu đọc được', () => {
    expect(describeResultCount(0)).toBe('Tìm thấy 0 tài liệu');
  });
});
