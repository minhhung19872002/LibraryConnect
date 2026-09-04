import { describe, expect, it } from 'vitest';
import { MAU_BIEU_DO } from '@/lib/palette';
import { toChartData } from './chartData';
import type { AcquisitionStatRowDto } from './types';

function row(label: string, itemCount: number, value = 0): AcquisitionStatRowDto {
  return { label, itemCount, titleCount: 0, value, percent: 0 };
}

describe('Dữ liệu biểu đồ báo cáo bổ sung (III.7)', () => {
  it('giữ đủ mọi dòng — không cắt còn mười dòng đầu như thanh tiến độ cũ', () => {
    const rows = Array.from({ length: 15 }, (_, index) => row(`Kho ${index + 1}`, index + 1));

    expect(toChartData(rows, 'itemCount')).toHaveLength(15);
  });

  it('tô màu phân loại theo dải biểu đồ, quay vòng khi hết dải', () => {
    const rows = Array.from({ length: MAU_BIEU_DO.length + 2 }, (_, index) => row(`Nhóm ${index}`, 1));
    const data = toChartData(rows, 'itemCount');

    expect(data[0]!.fill).toBe(MAU_BIEU_DO[0]);
    expect(data[MAU_BIEU_DO.length]!.fill).toBe(MAU_BIEU_DO[0]);
    expect(data[MAU_BIEU_DO.length + 1]!.fill).toBe(MAU_BIEU_DO[1]);
  });

  it('lấy đúng chỉ tiêu được chọn', () => {
    const rows = [row('A', 3, 90000), row('B', 5, 10000)];

    expect(toChartData(rows, 'itemCount').map((entry) => entry.value)).toEqual([3, 5]);
    expect(toChartData(rows, 'value').map((entry) => entry.value)).toEqual([90000, 10000]);
  });
});
