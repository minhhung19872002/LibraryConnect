import { useState } from 'react';
import { Segmented, Space, Typography } from 'antd';
import { BarChartOutlined, PieChartOutlined } from '@ant-design/icons';
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { money } from './labels';
import { toChartData, type ChartMeasure } from './chartData';
import type { AcquisitionStatRowDto } from './types';

/**
 * Biểu đồ cột hoặc tròn cho một bảng thống kê của Phân hệ III.
 *
 * E-HSMT đòi mọi báo cáo có ba dạng đầu ra — bảng, đồ họa, tệp. Đây là dạng đồ họa: đủ mọi dòng
 * của bảng, đổi được giữa cột và tròn, và chỉ tiêu (số bản / số đầu / giá trị) chọn được.
 */
export function StatChart({
  rows,
  measure = 'itemCount',
  unit = 'bản',
  height = 300,
  defaultKind = 'bar',
}: {
  rows: AcquisitionStatRowDto[];
  measure?: ChartMeasure;
  unit?: string;
  height?: number;
  defaultKind?: 'bar' | 'pie';
}) {
  const [kind, setKind] = useState<'bar' | 'pie'>(defaultKind);

  if (rows.length === 0) {
    return <Typography.Text type="secondary">Chưa có số liệu trong phạm vi lọc.</Typography.Text>;
  }

  const data = toChartData(rows, measure);
  const format = (value: number) => (measure === 'value' ? `${money(value)} đ` : `${value} ${unit}`);

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={4}>
      <Segmented
        size="small"
        value={kind}
        onChange={(value) => setKind(value as 'bar' | 'pie')}
        options={[
          { value: 'bar', icon: <BarChartOutlined />, label: 'Cột' },
          { value: 'pie', icon: <PieChartOutlined />, label: 'Tròn' },
        ]}
      />
      <ResponsiveContainer width="100%" height={height}>
        {kind === 'bar' ? (
          <BarChart data={data} margin={{ top: 8, right: 8, left: 8, bottom: 8 }}>
            <CartesianGrid strokeDasharray="3 3" vertical={false} />
            <XAxis dataKey="name" tick={{ fontSize: 11 }} interval={0} angle={data.length > 6 ? -20 : 0} textAnchor={data.length > 6 ? 'end' : 'middle'} height={data.length > 6 ? 60 : 30} />
            <YAxis allowDecimals={false} tick={{ fontSize: 11 }} />
            <Tooltip formatter={(value) => format(Number(value))} />
            <Bar dataKey="value" name={unit}>
              {data.map((entry) => (
                <Cell key={entry.name} fill={entry.fill} />
              ))}
            </Bar>
          </BarChart>
        ) : (
          <PieChart>
            <Pie
              data={data}
              dataKey="value"
              nameKey="name"
              outerRadius={Math.min(110, height / 2 - 30)}
              label={(entry: { name?: string; value?: number }) => `${entry.name}: ${entry.value}`}
            >
              {data.map((entry) => (
                <Cell key={entry.name} fill={entry.fill} />
              ))}
            </Pie>
            <Legend />
            <Tooltip formatter={(value) => format(Number(value))} />
          </PieChart>
        )}
      </ResponsiveContainer>
    </Space>
  );
}
