/** Kiểu dữ liệu của Báo cáo thống kê toàn hệ thống. */

export interface OverviewMetric {
  key: string;
  label: string;
  value: number;
  unit?: string | null;
  hint?: string | null;
}

export interface OverviewSection {
  key: string;
  title: string;
  metrics: OverviewMetric[];
}

export interface OverviewTrendPoint {
  period: string;
  loans: number;
  acquisitions: number;
  newReaders: number;
}

export interface OverviewBreakdownRow {
  label: string;
  count: number;
  percent: number;
}

export interface SystemOverview {
  from: string;
  to: string;
  sections: OverviewSection[];
  trend: OverviewTrendPoint[];
  documentTypes: OverviewBreakdownRow[];
  warehouses: OverviewBreakdownRow[];
}

/**
 * Định dạng một con số cho người Việt đọc.
 *
 * Tiền và số lượng đọc bằng dấu chấm phân nhóm nghìn; tỷ lệ giữ một chữ số thập phân. Không tự ý
 * rút gọn thành "1,2 tr" — cán bộ đối chiếu số này với sổ sách nên phải thấy đủ chữ số.
 */
export function formatMetric(metric: OverviewMetric): string {
  const fractionDigits = metric.unit === '%' || metric.unit === 'MB' ? 1 : 0;

  const number = metric.value.toLocaleString('vi-VN', {
    minimumFractionDigits: 0,
    maximumFractionDigits: fractionDigits,
  });

  return metric.unit ? `${number} ${metric.unit}` : number;
}
