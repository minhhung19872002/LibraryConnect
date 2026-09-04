import { describe, expect, it } from 'vitest';
import {
  describeExportCounts,
  exportProgressPercent,
  exportStatusColors,
  exportStatusLabels,
  formatPackageSize,
  isExportOpen,
} from './fullExportView';
import type { ExportJobStatus } from './types';

describe('Xuất toàn bộ dữ liệu hệ thống — cách hiện một lượt', () => {
  it('phủ hết năm trạng thái tác vụ bằng nhãn tiếng Việt và màu', () => {
    const statuses: ExportJobStatus[] = ['Pending', 'Running', 'Completed', 'Failed', 'Cancelled'];

    statuses.forEach((status) => {
      expect(exportStatusLabels[status]).toBeTruthy();
      expect(exportStatusColors[status]).toBeTruthy();
    });
  });

  it('chỉ lượt đang chờ hoặc đang chạy mới là lượt còn mở', () => {
    expect(isExportOpen({ status: 'Pending' })).toBe(true);
    expect(isExportOpen({ status: 'Running' })).toBe(true);
    expect(isExportOpen({ status: 'Completed' })).toBe(false);
    expect(isExportOpen({ status: 'Failed' })).toBe(false);
  });

  it('tiến độ: xong là 100, chưa bước nào là 0, đang chạy không bao giờ chạm 100', () => {
    expect(exportProgressPercent({ status: 'Completed', stepsDone: 3, stepsTotal: 8 })).toBe(100);
    expect(exportProgressPercent({ status: 'Pending', stepsDone: 0, stepsTotal: 0 })).toBe(0);
    expect(exportProgressPercent({ status: 'Running', stepsDone: 4, stepsTotal: 8 })).toBe(50);
    // Bước cuối đã xong nhưng tệp còn đang đóng: thanh không được báo 100 khi trạng thái chưa xong.
    expect(exportProgressPercent({ status: 'Running', stepsDone: 8, stepsTotal: 8 })).toBe(99);
  });

  it('dòng tổng kết ghi đủ năm phần và chỉ nhắc số bỏ qua khi có', () => {
    const clean = describeExportCounts({
      bibCount: 7675,
      bibSkipped: 0,
      digitalCount: 16,
      digitalFailed: 0,
      readerCount: 351,
      itemCount: 9502,
      loanCount: 1603,
    });

    expect(clean).toContain('7.675 biểu ghi');
    expect(clean).toContain('16 tài liệu số');
    expect(clean).toContain('351 bạn đọc');
    expect(clean).toContain('9.502 ĐKCB');
    expect(clean).toContain('1.603 lượt mượn');
    expect(clean).not.toContain('bỏ qua');

    const withSkips = describeExportCounts({
      bibCount: 7674,
      bibSkipped: 1,
      digitalCount: 15,
      digitalFailed: 1,
      readerCount: 0,
      itemCount: 0,
      loanCount: 0,
    });

    expect(withSkips).toContain('bỏ qua 1 ở ISO 2709');
    expect(withSkips).toContain('1 tệp không đọc được');
  });

  it('dung lượng gói đọc được theo đơn vị', () => {
    expect(formatPackageSize(null)).toBe('—');
    expect(formatPackageSize(0)).toBe('—');
    expect(formatPackageSize(512)).toBe('512 B');
    expect(formatPackageSize(1536)).toBe('1,5 KB');
    expect(formatPackageSize(1288490188)).toBe('1,2 GB');
  });
});
