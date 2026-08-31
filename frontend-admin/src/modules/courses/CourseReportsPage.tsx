import { useState } from 'react';
import { Button, Card, Col, Empty, Row, Select, Space, Statistic, Table, Tabs, Tag } from 'antd';
import { DownloadOutlined, FilePdfOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  ResponsiveContainer,
  Tooltip as ChartTooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { PageHeader } from '@/components/PageHeader';
import { api } from '@/api/client';
import type { PagedResult } from '@/types/api';
import type { CatalogItem } from '@/modules/catalogs/types';
import { coursesApi } from './api';

/**
 * X.3 — Ba báo cáo của phân hệ tài liệu môn học.
 *
 * Cả ba nhìn cùng một tập dữ liệu từ ba phía, nên đặt trên một màn hình có chung bộ lọc ngành:
 * cán bộ bổ sung đọc chúng cạnh nhau để quyết định mua gì.
 */
export function CourseReportsPage() {
  const [majorId, setMajorId] = useState<string | undefined>();

  const majors = useQuery({
    queryKey: ['catalog', 'majors'],
    queryFn: () =>
      api.get<PagedResult<CatalogItem>>('/catalogs/majors/items', {
        params: { page: 1, pageSize: 200, isActive: true },
      }),
  });

  const report = useQuery({
    queryKey: ['course-report', majorId],
    queryFn: () => coursesApi.report(majorId, 20),
  });

  const chartData = (report.data?.coverage ?? []).map((row) => ({
    name: row.code,
    fullName: row.name,
    percent: row.coveragePercent,
  }));

  const exportUrl = (format: string) =>
    `/api/courses/reports/export?format=${format}${majorId ? `&majorId=${majorId}` : ''}`;

  return (
    <>
      <PageHeader
        title="Báo cáo tài liệu môn học"
        description="Môn học chưa có tài liệu, tài liệu dùng chung nhiều môn và mức độ đáp ứng theo ngành đào tạo."
        actions={
          <Space>
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
        <Space wrap>
          <Select
            allowClear
            placeholder="Mọi ngành đào tạo"
            style={{ width: 320 }}
            value={majorId}
            onChange={setMajorId}
            options={(majors.data?.items ?? []).map((item) => ({
              value: item.id,
              label: `${item.code} — ${item.name}`,
            }))}
          />
          <Statistic title="Tổng số môn học" value={report.data?.totalCourses ?? 0} />
          <Statistic title="Môn đã có tài liệu" value={report.data?.coveredCourses ?? 0} />
          <Statistic title="Tổng số liên kết" value={report.data?.totalLinks ?? 0} />
        </Space>
      </Card>

      <Card loading={report.isLoading}>
        <Tabs
          items={[
            {
              key: 'coverage',
              label: `Đáp ứng theo ngành (${report.data?.coverage.length ?? 0})`,
              children: (
                <>
                  {chartData.length > 0 ? (
                    <div style={{ height: 300, marginBottom: 16 }}>
                      <ResponsiveContainer width="100%" height="100%">
                        <BarChart data={chartData}>
                          <CartesianGrid strokeDasharray="3 3" />
                          <XAxis dataKey="name" />
                          <YAxis unit="%" domain={[0, 100]} />
                          <ChartTooltip
                            formatter={(value: number) => [`${value}%`, 'Tỷ lệ môn có tài liệu']}
                            labelFormatter={(label: string) =>
                              chartData.find((row) => row.name === label)?.fullName ?? label
                            }
                          />
                          <Legend formatter={() => 'Tỷ lệ môn học đã có tài liệu'} />
                          <Bar dataKey="percent" name="Tỷ lệ">
                            {chartData.map((row) => (
                              <Cell
                                key={row.name}
                                // Dưới một nửa số môn có tài liệu là mức cần bổ sung gấp; đỏ để cán
                                // bộ nhìn biểu đồ là thấy ngay ngành nào đang thiếu.
                                fill={row.percent >= 80 ? '#52c41a' : row.percent >= 50 ? '#faad14' : '#ff4d4f'}
                              />
                            ))}
                          </Bar>
                        </BarChart>
                      </ResponsiveContainer>
                    </div>
                  ) : null}

                  <Table
                    rowKey="majorId"
                    size="small"
                    dataSource={report.data?.coverage ?? []}
                    pagination={false}
                    scroll={{ x: 900 }}
                    locale={{ emptyText: <Empty description="Chưa có ngành đào tạo nào." /> }}
                    columns={[
                      { title: 'Mã ngành', dataIndex: 'code', width: 130 },
                      { title: 'Tên ngành', dataIndex: 'name', width: 260 },
                      { title: 'Khoa quản lý', dataIndex: 'facultyName', width: 220 },
                      { title: 'Số môn', dataIndex: 'courseCount', width: 100, align: 'right' },
                      {
                        title: 'Môn có tài liệu',
                        dataIndex: 'coveredCourseCount',
                        width: 140,
                        align: 'right',
                      },
                      {
                        title: 'Tỷ lệ đáp ứng',
                        dataIndex: 'coveragePercent',
                        width: 140,
                        align: 'right',
                        render: (percent: number) => (
                          <Tag color={percent >= 80 ? 'green' : percent >= 50 ? 'orange' : 'red'}>
                            {percent}%
                          </Tag>
                        ),
                      },
                      {
                        title: 'Liên kết tài liệu',
                        dataIndex: 'documentCount',
                        width: 140,
                        align: 'right',
                      },
                    ]}
                  />
                </>
              ),
            },
            {
              key: 'missing',
              label: `Môn chưa có tài liệu (${report.data?.withoutDocuments.length ?? 0})`,
              children: (
                <Table
                  rowKey="courseId"
                  size="small"
                  dataSource={report.data?.withoutDocuments ?? []}
                  pagination={{ pageSize: 20 }}
                  scroll={{ x: 900 }}
                  locale={{
                    emptyText: <Empty description="Mọi môn học đều đã có tài liệu." />,
                  }}
                  columns={[
                    { title: 'Mã môn', dataIndex: 'code', width: 120 },
                    { title: 'Tên môn học', dataIndex: 'name', width: 300 },
                    { title: 'Số tín chỉ', dataIndex: 'credits', width: 110, align: 'right' },
                    { title: 'Học kỳ', dataIndex: 'semester', width: 130 },
                    { title: 'Ngành đào tạo', dataIndex: 'majors', width: 340 },
                  ]}
                />
              ),
            },
            {
              key: 'shared',
              label: `Tài liệu dùng nhiều môn (${report.data?.sharedDocuments.length ?? 0})`,
              children: (
                <Table
                  rowKey="bibId"
                  size="small"
                  dataSource={report.data?.sharedDocuments ?? []}
                  pagination={{ pageSize: 20 }}
                  scroll={{ x: 1000 }}
                  locale={{
                    emptyText: <Empty description="Chưa có tài liệu nào được dùng cho nhiều môn." />,
                  }}
                  columns={[
                    { title: 'Nhan đề', dataIndex: 'title', width: 320 },
                    { title: 'Tác giả', dataIndex: 'authorMain', width: 200 },
                    {
                      title: 'Số môn dùng',
                      dataIndex: 'courseCount',
                      width: 130,
                      align: 'right',
                      render: (count: number) => <Tag color="blue">{count} môn</Tag>,
                    },
                    {
                      title: 'Bản rảnh',
                      dataIndex: 'availableItemCount',
                      width: 120,
                      align: 'right',
                      render: (available: number, row) => (
                        <Tag color={available >= row.courseCount ? 'green' : 'red'}>
                          {available} bản
                        </Tag>
                      ),
                    },
                    { title: 'Các môn học', dataIndex: 'courses', width: 380 },
                  ]}
                />
              ),
            },
          ]}
        />
      </Card>

      <Row style={{ marginTop: 16 }}>
        <Col span={24}>
          <Card size="small">
            <Space direction="vertical" size={4}>
              <span style={{ color: '#888' }}>
                Cột <b>Bản rảnh</b> ở thẻ cuối tô đỏ khi số bản còn rảnh ít hơn số môn đang dùng
                chung cuốn đó — đây là chỗ thư viện cần bổ sung thêm bản.
              </span>
            </Space>
          </Card>
        </Col>
      </Row>
    </>
  );
}
