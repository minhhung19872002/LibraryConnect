import { mauBieuDo } from '@/lib/palette';
import type { AcquisitionStatRowDto } from './types';

/**
 * Dữ liệu cho biểu đồ của các báo cáo bổ sung (III.2, III.7).
 *
 * Màu lấy từ dải phân loại `MAU_BIEU_DO`, không phải màu ngữ nghĩa: xanh lá là "tốt", đỏ là
 * "hỏng", đem tô cho "Kho mở" và "Kho đóng" là gán nghĩa không có thật (bài học 18).
 */

export type ChartMeasure = 'itemCount' | 'titleCount' | 'value';

export interface ChartEntry {
  name: string;
  value: number;
  fill: string;
}

/** Mọi dòng của báo cáo, mỗi dòng một màu phân loại — không cắt bớt, biểu đồ cuộn được. */
export function toChartData(rows: AcquisitionStatRowDto[], measure: ChartMeasure): ChartEntry[] {
  return rows.map((row, index) => ({
    name: row.label,
    value: row[measure],
    fill: mauBieuDo(index),
  }));
}
