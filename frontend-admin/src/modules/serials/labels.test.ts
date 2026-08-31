import { describe, expect, it } from 'vitest';
import {
  claimStatusLabels,
  formatIssn,
  frequencyLabels,
  issueStatusColors,
  issueStatusLabels,
  issuesPerYear,
  months,
  numberingLabels,
  weekdays,
  weeklyFrequencies,
} from './labels';
import type { SerialFrequency, SerialIssueStatus } from './types';

const allFrequencies: SerialFrequency[] = [
  'Daily',
  'Weekly',
  'Biweekly',
  'SemiMonthly',
  'Monthly',
  'Bimonthly',
  'Quarterly',
  'SemiAnnual',
  'Annual',
  'Irregular',
];

describe('Nhãn tiếng Việt của Phân hệ IV', () => {
  it('phủ hết mọi kỳ hạn xuất bản', () => {
    allFrequencies.forEach((frequency) => {
      expect(frequencyLabels[frequency]).toBeTruthy();
      expect(issuesPerYear[frequency]).toBeGreaterThanOrEqual(0);
    });
  });

  it('phủ hết mọi trạng thái số báo, kèm màu để tô lưới', () => {
    const statuses: SerialIssueStatus[] = ['Expected', 'Received', 'Missing', 'Claimed', 'Bound'];

    statuses.forEach((status) => {
      expect(issueStatusLabels[status]).toBeTruthy();
      expect(issueStatusColors[status]).toBeTruthy();
    });
  });

  it('phủ hết trạng thái phiếu khiếu nại và cách đánh số', () => {
    Object.values(claimStatusLabels).forEach((label) => expect(label).toBeTruthy());
    Object.values(numberingLabels).forEach((label) => expect(label).toBeTruthy());
  });
});

describe('Gợi ý số kỳ trong năm', () => {
  it('khớp với ý nghĩa của từng kỳ hạn', () => {
    expect(issuesPerYear.Monthly).toBe(12);
    expect(issuesPerYear.Weekly).toBe(52);
    expect(issuesPerYear.Quarterly).toBe(4);
    expect(issuesPerYear.Annual).toBe(1);
  });

  it('không đoán số kỳ cho ấn phẩm không định kỳ', () => {
    // Kỳ hạn này buộc cán bộ tự khai; hiện một con số gợi ý sẽ là đoán bừa.
    expect(issuesPerYear.Irregular).toBe(0);
  });
});

describe('Chọn thứ hay chọn ngày trong tháng', () => {
  it('kỳ hạn theo tuần khai thứ phát hành', () => {
    expect(weeklyFrequencies).toContain('Weekly');
    expect(weeklyFrequencies).toContain('Biweekly');
    expect(weeklyFrequencies).toContain('Daily');
  });

  it('kỳ hạn theo tháng trở lên thì không khai thứ', () => {
    expect(weeklyFrequencies).not.toContain('Monthly');
    expect(weeklyFrequencies).not.toContain('Quarterly');
  });
});

describe('Danh sách chọn của form khai kỳ hạn', () => {
  it('đếm tuần theo cách người Việt đếm: thứ Hai là 1, Chủ nhật là 7', () => {
    expect(weekdays).toHaveLength(7);
    expect(weekdays[0]).toEqual({ value: 1, label: 'Thứ Hai' });
    expect(weekdays[6]).toEqual({ value: 7, label: 'Chủ nhật' });
  });

  it('có đủ mười hai tháng để chọn kỳ nghỉ không xuất bản', () => {
    expect(months).toHaveLength(12);
    expect(months[6]).toEqual({ value: 7, label: 'Tháng 7' });
  });
});

describe('Hiển thị ISSN', () => {
  it('đặt lại dấu gạch nối của chuẩn ISSN', () => {
    // Máy chủ lưu dạng đã bỏ gạch để tra cứu khớp; màn hình phải hiện lại đúng cách viết trên bìa.
    expect(formatIssn('18591450')).toBe('1859-1450');
  });

  it('giữ nguyên giá trị đã có gạch', () => {
    expect(formatIssn('1859-1450')).toBe('1859-1450');
  });

  it('nhận chữ X ở vị trí kiểm tra', () => {
    expect(formatIssn('1050091x')).toBe('1050-091X');
  });

  it('trả nguyên chuỗi khi độ dài không phải tám ký tự, thay vì cắt bừa', () => {
    expect(formatIssn('12345')).toBe('12345');
    expect(formatIssn(null)).toBe('');
  });
});
