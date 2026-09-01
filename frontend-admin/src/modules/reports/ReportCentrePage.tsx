import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Button, Card, Col, DatePicker, Empty, List, Row, Space, Statistic, Table, Tabs, Tag, Tooltip } from 'antd';
import { DownloadOutlined, FilePdfOutlined, RightOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip as ChartTooltip,
  XAxis,
  YAxis,
} from 'recharts';
import dayjs, { type Dayjs } from 'dayjs';
import type { ColumnsType } from 'antd/es/table';
import { PageHeader } from '@/components/PageHeader';
import { useAuthStore } from '@/stores/authStore';
import { reportsApi } from './api';
import { REPORT_CATALOGUE } from './reportCatalogue';
import { formatMetric, type OverviewBreakdownRow, type OverviewMetric } from './types';

const { RangePicker } = DatePicker;

/**
 * Báo cáo thống kê — trang trả lời câu hỏi "thư viện đang thế nào".
 *
 * Bảy phân hệ đều có báo cáo riêng, nhưng người phụ trách thư viện cần một chỗ nhìn thấy toàn cảnh
 * mà không phải mở bảy màn hình rồi tự cộng lại. Trang này gộp các con số ấy, vẽ xu hướng mười hai
 * tháng, và dẫn thẳng tới từng báo cáo chi tiết khi cần đào sâu.
 */
export function ReportCentrePage() {
  const [range, setRange] = useState<[Dayjs, Dayjs]>(() => [dayjs().startOf('year'), dayjs()]);
  const hasAnyPermission = useAuthStore((state) => state.hasAnyPermission);

  const from = range[0].format('YYYY-MM-DD');
  const to = range[1].format('YYYY-MM-DD');

  const overview = useQuery({
    queryKey: ['report-overview', from, to],
    queryFn: () => reportsApi.overview(from, to),
  });

  const exportUrl = (format: string) =>
    `/api/reports/overview/export?format=${format}&from=${from}&to=${to}`;

  const breakdownColumns: ColumnsType<OverviewBreakdownRow> = [
    { title: 'Nhóm', dataIndex: 'label' },
    {
      title: 'Số lượng',
      dataIndex: 'count',
      width: 120,
      align: 'right',
      render: (value: number) => value.toLocaleString('vi-VN'),
    },
    {
      title: 'Tỷ lệ',
      dataIndex: 'percent',
      width: 110,
      align: 'right',
      render: (value: number) => <Tag color="blue">{value}%</Tag>,
    },
  ];

  const trend = overview.data?.trend ?? [];

  return (
    <>
      <PageHeader
        title="Báo cáo thống kê"
        description="Toàn cảnh hoạt động của thư viện và lối vào mọi báo cáo chi tiết của từng phân hệ."
        actions={
          <Space wrap>
            <Button icon={<DownloadOutlined />} href={exportUrl('excel')} target="_blank" rel="noopener noreferrer">
              Xuất Excel
            </Button>
            <Button icon={<FilePdfOutlined />} href={exportUrl('pdf')} target="_blank" rel="noopener noreferrer">
              Xuất PDF
            </Button>
          </Space>
        }
      />

      <Card style={{ marginBottom: 16 }}>
        <Space wrap align="center">
          <span>Kỳ báo cáo</span>
          <RangePicker
            value={range}
            allowClear={false}
            format="DD/MM/YYYY"
            onChange={(value) => {
              if (value?.[0] && value?.[1]) {
                setRange([value[0], value[1]]);
              }
            }}
          />
          <Button size="small" onClick={() => setRange([dayjs().startOf('month'), dayjs()])}>
            Tháng này
          </Button>
          <Button size="small" onClick={() => setRange([dayjs().startOf('year'), dayjs()])}>
            Năm nay
          </Button>
          <Button size="small" onClick={() => setRange([dayjs().subtract(1, 'year'), dayjs()])}>
            12 tháng gần nhất
          </Button>
        </Space>
      </Card>

      <Tabs
        defaultActiveKey="overview"
        items={[
          {
            key: 'overview',
            label: 'Tổng quan',
            children: (
              <>
                {(overview.data?.sections ?? []).map((section) => (
                  <Card
                    key={section.key}
                    title={section.title}
                    loading={overview.isLoading}
                    style={{ marginBottom: 16 }}
                  >
                    <Row gutter={[16, 16]}>
                      {section.metrics.map((metric) => (
                        <Col key={metric.key} xs={12} sm={12} md={8} lg={6} xl={4}>
                          <MetricCard metric={metric} />
                        </Col>
                      ))}
                    </Row>
                  </Card>
                ))}

                {!overview.isLoading && (overview.data?.sections.length ?? 0) === 0 && (
                  <Card>
                    <Empty description="Chưa có số liệu trong kỳ đã chọn." />
                  </Card>
                )}
              </>
            ),
          },
          {
            key: 'trend',
            label: 'Xu hướng 12 tháng',
            children: (
              <Card loading={overview.isLoading}>
                {trend.length > 0 ? (
                  <div style={{ height: 340 }}>
                    <ResponsiveContainer width="100%" height="100%">
                      <LineChart data={trend}>
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis dataKey="period" />
                        <YAxis allowDecimals={false} />
                        <ChartTooltip />
                        <Legend />
                        <Line
                          type="monotone"
                          dataKey="loans"
                          name="Lượt mượn"
                          stroke="#1668dc"
                          strokeWidth={2}
                        />
                        <Line
                          type="monotone"
                          dataKey="acquisitions"
                          name="Bản nhập kho"
                          stroke="#52c41a"
                          strokeWidth={2}
                        />
                        <Line
                          type="monotone"
                          dataKey="newReaders"
                          name="Thẻ mới"
                          stroke="#faad14"
                          strokeWidth={2}
                        />
                      </LineChart>
                    </ResponsiveContainer>
                  </div>
                ) : (
                  <Empty description="Chưa có dữ liệu để vẽ." />
                )}
              </Card>
            ),
          },
          {
            key: 'breakdown',
            label: 'Phân bố kho',
            children: (
              <Row gutter={[16, 16]}>
                <Col xs={24} lg={12}>
                  <Card title="Biểu ghi theo dạng tài liệu" loading={overview.isLoading}>
                    <Table
                      rowKey="label"
                      size="small"
                      pagination={false}
                      columns={breakdownColumns}
                      dataSource={overview.data?.documentTypes ?? []}
                      locale={{ emptyText: 'Chưa có biểu ghi nào.' }}
                    />
                  </Card>
                </Col>

                <Col xs={24} lg={12}>
                  <Card title="Bản in theo kho" loading={overview.isLoading}>
                    {(overview.data?.warehouses.length ?? 0) > 0 ? (
                      <div style={{ height: 280 }}>
                        <ResponsiveContainer width="100%" height="100%">
                          <BarChart data={overview.data?.warehouses ?? []}>
                            <CartesianGrid strokeDasharray="3 3" />
                            <XAxis dataKey="label" />
                            <YAxis allowDecimals={false} />
                            <ChartTooltip formatter={(value: number) => [value.toLocaleString('vi-VN'), 'Bản in']} />
                            <Bar dataKey="count" name="Bản in" fill="#1668dc" />
                          </BarChart>
                        </ResponsiveContainer>
                      </div>
                    ) : (
                      <Empty description="Chưa có bản in nào trong kho." />
                    )}
                  </Card>
                </Col>
              </Row>
            ),
          },
          {
            key: 'catalogue',
            label: 'Mục lục báo cáo',
            children: (
              <Row gutter={[16, 16]}>
                {REPORT_CATALOGUE.map((group) => {
                  // Chỉ hiện những báo cáo tài khoản này mở được; nhóm rỗng thì bỏ hẳn khỏi trang.
                  const links = group.links.filter((link) => hasAnyPermission([link.permission]));

                  if (links.length === 0) {
                    return null;
                  }

                  return (
                    <Col key={group.key} xs={24} md={12} xl={8}>
                      <Card title={group.title} size="small" style={{ height: '100%' }}>
                        <List
                          dataSource={links}
                          renderItem={(link) => (
                            <List.Item>
                              <List.Item.Meta
                                title={
                                  <Link to={link.path}>
                                    {link.title} <RightOutlined style={{ fontSize: 11 }} />
                                  </Link>
                                }
                                description={link.description}
                              />
                            </List.Item>
                          )}
                        />
                      </Card>
                    </Col>
                  );
                })}
              </Row>
            ),
          },
        ]}
      />
    </>
  );
}

function MetricCard({ metric }: { metric: OverviewMetric }) {
  const card = (
    <Statistic
      title={metric.label}
      value={formatMetric(metric)}
      valueStyle={{ fontSize: 22, fontWeight: 600 }}
    />
  );

  return metric.hint ? <Tooltip title={metric.hint}>{card}</Tooltip> : card;
}
