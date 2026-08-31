import { useState } from 'react';
import {
  App,
  Button,
  Card,
  Col,
  DatePicker,
  Empty,
  InputNumber,
  Radio,
  Row,
  Select,
  Space,
  Statistic,
  Table,
  Tag,
  Typography,
} from 'antd';
import { FileExcelOutlined, FilePdfOutlined } from '@ant-design/icons';
import { useMutation, useQuery } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import type { Dayjs } from 'dayjs';
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { saveBlob } from '@/modules/marc/api';
import { useCatalogOptions, toOptions } from '@/modules/cataloging/useCatalogOptions';
import { readersApi } from './api';
import { dimensionLabels, formatDate, groupingLabels, readerStatusLabels } from './labels';
import type {
  ExpiringCardRowDto,
  ReaderActivityRowDto,
  ReaderReportDimension,
  ReaderReportFilter,
  ReaderReportRowDto,
  ReaderTimeGrouping,
  ReaderTimeRowDto,
} from './types';

type ReportKind = 'count' | 'registration' | 'expiring' | 'activity';

const kindLabels: Record<ReportKind, string> = {
  count: 'Số lượng bạn đọc',
  registration: 'Đăng ký mới theo thời gian',
  expiring: 'Thẻ sắp hết hạn',
  activity: 'Mức độ sử dụng',
};

/** Mã báo cáo gửi cho máy chủ khi xuất tệp, khớp với enum ReaderReportKind. */
const kindCodes: Record<ReportKind, number> = {
  count: 0,
  registration: 1,
  expiring: 2,
  activity: 3,
};

const chartColors = ['#1677ff', '#52c41a', '#faad14', '#f5222d', '#722ed1', '#13c2c2', '#eb2f96'];

/**
 * VI.5 — Báo cáo thống kê bạn đọc.
 *
 * Bốn báo cáo đặc tả yêu cầu, mỗi báo cáo đủ ba dạng đầu ra: bảng số liệu, biểu đồ và tệp xuất ra
 * (Excel hoặc PDF) đúng bằng bộ lọc đang xem trên màn hình.
 */
export function ReaderReportsPage() {
  const { message } = App.useApp();

  const [kind, setKind] = useState<ReportKind>('count');
  const [dimension, setDimension] = useState<ReaderReportDimension>('ReaderType');
  const [grouping, setGrouping] = useState<ReaderTimeGrouping>('Month');
  const [withinDays, setWithinDays] = useState(30);
  const [neverBorrowed, setNeverBorrowed] = useState(false);
  const [top, setTop] = useState(20);
  const [filter, setFilter] = useState<ReaderReportFilter>({});

  const readerTypes = useCatalogOptions('reader-types');
  const faculties = useCatalogOptions('faculties');
  const cohorts = useCatalogOptions('cohorts');

  const count = useQuery({
    queryKey: ['reader-report-count', dimension, filter],
    queryFn: () => readersApi.countReport(dimension, filter),
    enabled: kind === 'count',
  });

  const registrations = useQuery({
    queryKey: ['reader-report-registrations', grouping, filter],
    queryFn: () => readersApi.registrationReport(grouping, filter),
    enabled: kind === 'registration',
  });

  const expiring = useQuery({
    queryKey: ['reader-report-expiring', withinDays, filter],
    queryFn: () => readersApi.expiringCards(withinDays, filter),
    enabled: kind === 'expiring',
  });

  const activity = useQuery({
    queryKey: ['reader-report-activity', neverBorrowed, top, filter],
    queryFn: () => readersApi.activityReport(neverBorrowed, top, filter),
    enabled: kind === 'activity',
  });

  const exportReport = useMutation({
    mutationFn: (asPdf: boolean) =>
      readersApi.exportReport({
        kind: kindCodes[kind],
        asPdf,
        dimension,
        grouping,
        withinDays,
        neverBorrowed,
        top,
        filter,
      }),
    onSuccess: ({ blob, fileName }) => {
      saveBlob(blob, fileName);
      message.success('Đã xuất báo cáo.');
    },
    onError: (error: Error) => message.error(error.message),
  });

  const countColumns: ColumnsType<ReaderReportRowDto> = [
    { title: dimensionLabels[dimension], dataIndex: 'label' },
    { title: 'Tổng', dataIndex: 'total', width: 90, align: 'right' },
    { title: 'Hoạt động', dataIndex: 'active', width: 100, align: 'right' },
    { title: 'Hết hạn', dataIndex: 'expired', width: 100, align: 'right' },
    { title: 'Tạm khóa', dataIndex: 'suspended', width: 100, align: 'right' },
    { title: 'Ra trường', dataIndex: 'graduated', width: 100, align: 'right' },
    { title: 'Đã từng mượn', dataIndex: 'everBorrowed', width: 120, align: 'right' },
    {
      title: 'Tỷ lệ',
      dataIndex: 'percentage',
      width: 90,
      align: 'right',
      render: (value: number) => `${value.toLocaleString('vi-VN')}%`,
    },
  ];

  const expiringColumns: ColumnsType<ExpiringCardRowDto> = [
    { title: 'Số thẻ', dataIndex: 'cardNumber', width: 140 },
    { title: 'Mã sinh viên', dataIndex: 'studentCode', width: 130 },
    { title: 'Họ và tên', dataIndex: 'fullName' },
    { title: 'Loại bạn đọc', dataIndex: 'readerTypeName', width: 130 },
    { title: 'Khoa', dataIndex: 'facultyName', width: 200, ellipsis: true },
    { title: 'Lớp', dataIndex: 'className', width: 110 },
    { title: 'Hết hạn', dataIndex: 'cardExpireDate', width: 120, render: formatDate },
    {
      title: 'Còn lại',
      dataIndex: 'daysLeft',
      width: 120,
      align: 'right',
      render: (days: number) =>
        days < 0 ? (
          <Tag color="red">Quá hạn {Math.abs(days)} ngày</Tag>
        ) : (
          <Tag color="orange">Còn {days} ngày</Tag>
        ),
    },
  ];

  const activityColumns: ColumnsType<ReaderActivityRowDto> = [
    { title: 'Số thẻ', dataIndex: 'cardNumber', width: 140 },
    { title: 'Mã sinh viên', dataIndex: 'studentCode', width: 130 },
    { title: 'Họ và tên', dataIndex: 'fullName' },
    { title: 'Loại bạn đọc', dataIndex: 'readerTypeName', width: 130 },
    { title: 'Khoa', dataIndex: 'facultyName', width: 200, ellipsis: true },
    { title: 'Lớp', dataIndex: 'className', width: 110 },
    { title: 'Lượt mượn', dataIndex: 'loanCount', width: 110, align: 'right' },
    {
      title: 'Lần mượn gần nhất',
      dataIndex: 'lastLoanAt',
      width: 160,
      render: formatDate,
    },
  ];

  const registrationColumns: ColumnsType<ReaderTimeRowDto> = [
    { title: 'Kỳ', dataIndex: 'period' },
    { title: 'Đăng ký mới', dataIndex: 'newReaders', width: 140, align: 'right' },
    { title: 'Cộng dồn', dataIndex: 'cumulative', width: 140, align: 'right' },
  ];

  const loading =
    count.isFetching || registrations.isFetching || expiring.isFetching || activity.isFetching;

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Báo cáo bạn đọc"
        description="Số lượng theo loại, khoa, ngành, khóa và trạng thái; bạn đọc đăng ký mới; thẻ sắp hết hạn; mức độ sử dụng thư viện."
        actions={
          <Can permission={PERMISSIONS.reader.reportView}>
            <Space>
              <Button
                icon={<FileExcelOutlined />}
                loading={exportReport.isPending}
                onClick={() => exportReport.mutate(false)}
              >
                Excel
              </Button>
              <Button
                icon={<FilePdfOutlined />}
                loading={exportReport.isPending}
                onClick={() => exportReport.mutate(true)}
              >
                PDF
              </Button>
            </Space>
          </Can>
        }
      />

      <Card size="small">
        <Space direction="vertical" size={12} style={{ width: '100%' }}>
          <Radio.Group
            value={kind}
            onChange={(event) => setKind(event.target.value)}
            optionType="button"
            buttonStyle="solid"
            options={(Object.keys(kindLabels) as ReportKind[]).map((value) => ({
              value,
              label: kindLabels[value],
            }))}
          />

          <Space wrap>
            {kind === 'count' && (
              // Ô chọn chiều thống kê phải có nhãn: bên cạnh nó là bộ lọc cũng tên "Loại bạn đọc",
              // hai ô giống hệt nhau đứng cạnh nhau thì không ai biết ô nào làm gì.
              <Space size={4}>
                <span>Thống kê theo</span>
                <Select
                  style={{ width: 180 }}
                  value={dimension}
                  options={(Object.keys(dimensionLabels) as ReaderReportDimension[]).map((value) => ({
                    value,
                    label: dimensionLabels[value],
                  }))}
                  onChange={setDimension}
                />
              </Space>
            )}

            {kind === 'registration' && (
              <Space size={4}>
                <span>Gộp</span>
                <Select
                  style={{ width: 160 }}
                  value={grouping}
                  options={(Object.keys(groupingLabels) as ReaderTimeGrouping[]).map((value) => ({
                    value,
                    label: groupingLabels[value],
                  }))}
                  onChange={setGrouping}
                />
              </Space>
            )}

            {kind === 'expiring' && (
              <Space size={4}>
                <span>Hết hạn trong</span>
                <InputNumber<number>
                  min={1}
                  max={365}
                  value={withinDays}
                  onChange={(value) => setWithinDays(value ?? 30)}
                  style={{ width: 90 }}
                />
                <span>ngày tới</span>
              </Space>
            )}

            {kind === 'activity' && (
              <Space>
                <Select
                  style={{ width: 220 }}
                  value={neverBorrowed}
                  options={[
                    { value: false, label: 'Bạn đọc mượn nhiều nhất' },
                    { value: true, label: 'Bạn đọc chưa từng mượn' },
                  ]}
                  onChange={setNeverBorrowed}
                />
                <Space size={4}>
                  <span>Lấy</span>
                  <InputNumber<number>
                    min={1}
                    max={500}
                    value={top}
                    onChange={(value) => setTop(value ?? 20)}
                    style={{ width: 80 }}
                  />
                  <span>dòng</span>
                </Space>
              </Space>
            )}

            <DatePicker.RangePicker
              format="DD/MM/YYYY"
              placeholder={['Từ ngày', 'Đến ngày']}
              onChange={(dates) =>
                setFilter({
                  ...filter,
                  fromDate: dates?.[0] ? (dates[0] as Dayjs).format('YYYY-MM-DD') : undefined,
                  toDate: dates?.[1] ? (dates[1] as Dayjs).format('YYYY-MM-DD') : undefined,
                })
              }
            />

            <Select
              allowClear
              style={{ width: 160 }}
              placeholder="Loại bạn đọc"
              options={toOptions(readerTypes.data)}
              value={filter.readerTypeId}
              onChange={(value) => setFilter({ ...filter, readerTypeId: value })}
            />
            <Select
              allowClear
              showSearch
              optionFilterProp="label"
              style={{ width: 200 }}
              placeholder="Khoa"
              options={toOptions(faculties.data)}
              value={filter.facultyId}
              onChange={(value) => setFilter({ ...filter, facultyId: value })}
            />
            <Select
              allowClear
              style={{ width: 120 }}
              placeholder="Khóa"
              options={(cohorts.data ?? []).map((item) => ({ value: item.code, label: item.code }))}
              value={filter.courseYear}
              onChange={(value) => setFilter({ ...filter, courseYear: value })}
            />
            <Select
              allowClear
              style={{ width: 150 }}
              placeholder="Trạng thái thẻ"
              options={Object.entries(readerStatusLabels).map(([value, label]) => ({
                value,
                label,
              }))}
              value={filter.status}
              onChange={(value) => setFilter({ ...filter, status: value })}
            />
          </Space>
        </Space>
      </Card>

      {kind === 'count' && (
        <Row gutter={16}>
          <Col span={14}>
            <Card size="small" title={`Số lượng bạn đọc theo ${dimensionLabels[dimension].toLowerCase()}`}>
              <Table
                rowKey="key"
                size="small"
                loading={loading}
                dataSource={count.data ?? []}
                columns={countColumns}
                pagination={false}
                summary={(rows) => {
                  const total = rows.reduce((sum, row) => sum + row.total, 0);
                  const active = rows.reduce((sum, row) => sum + row.active, 0);
                  const borrowed = rows.reduce((sum, row) => sum + row.everBorrowed, 0);

                  return (
                    <Table.Summary.Row>
                      <Table.Summary.Cell index={0}>
                        <strong>Tổng cộng</strong>
                      </Table.Summary.Cell>
                      <Table.Summary.Cell index={1} align="right">
                        <strong>{total.toLocaleString('vi-VN')}</strong>
                      </Table.Summary.Cell>
                      <Table.Summary.Cell index={2} align="right">
                        {active.toLocaleString('vi-VN')}
                      </Table.Summary.Cell>
                      <Table.Summary.Cell index={3} align="right" colSpan={3} />
                      <Table.Summary.Cell index={6} align="right">
                        {borrowed.toLocaleString('vi-VN')}
                      </Table.Summary.Cell>
                      <Table.Summary.Cell index={7} align="right">
                        100%
                      </Table.Summary.Cell>
                    </Table.Summary.Row>
                  );
                }}
              />
            </Card>
          </Col>
          <Col span={10}>
            <Card size="small" title="Biểu đồ">
              {(count.data ?? []).length === 0 ? (
                <Empty description="Chưa có số liệu" />
              ) : (
                <ResponsiveContainer width="100%" height={320}>
                  <PieChart>
                    <Pie
                      data={(count.data ?? []).slice(0, 8)}
                      dataKey="total"
                      nameKey="label"
                      outerRadius={110}
                      label={(entry: { label?: string; total?: number }) =>
                        `${entry.label}: ${entry.total}`
                      }
                    >
                      {(count.data ?? []).slice(0, 8).map((row, index) => (
                        <Cell key={row.key} fill={chartColors[index % chartColors.length]} />
                      ))}
                    </Pie>
                    <Tooltip />
                  </PieChart>
                </ResponsiveContainer>
              )}
            </Card>
          </Col>
        </Row>
      )}

      {kind === 'registration' && (
        <Space direction="vertical" size={16} style={{ width: '100%' }}>
          <Card size="small" title="Bạn đọc đăng ký mới">
            {(registrations.data ?? []).length === 0 ? (
              <Empty description="Chưa có số liệu" />
            ) : (
              <ResponsiveContainer width="100%" height={300}>
                <LineChart data={registrations.data ?? []}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="period" />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Legend />
                  <Line
                    type="monotone"
                    dataKey="newReaders"
                    name="Đăng ký mới"
                    stroke="#1677ff"
                    strokeWidth={2}
                  />
                  <Line
                    type="monotone"
                    dataKey="cumulative"
                    name="Cộng dồn"
                    stroke="#52c41a"
                    strokeWidth={2}
                  />
                </LineChart>
              </ResponsiveContainer>
            )}
          </Card>

          <Card size="small" title="Số liệu chi tiết">
            <Table
              rowKey="period"
              size="small"
              loading={loading}
              dataSource={registrations.data ?? []}
              columns={registrationColumns}
              pagination={false}
            />
          </Card>
        </Space>
      )}

      {kind === 'expiring' && (
        <Space direction="vertical" size={16} style={{ width: '100%' }}>
          <Row gutter={16}>
            <Col span={8}>
              <Card size="small">
                <Statistic
                  title="Thẻ đã hết hạn"
                  value={expiring.data?.expiredCount ?? 0}
                  valueStyle={{ color: '#cf1322' }}
                />
              </Card>
            </Col>
            <Col span={8}>
              <Card size="small">
                <Statistic
                  title={`Hết hạn trong ${withinDays} ngày tới`}
                  value={expiring.data?.expiringCount ?? 0}
                  valueStyle={{ color: '#d46b08' }}
                />
              </Card>
            </Col>
            <Col span={8}>
              <Card size="small">
                <Statistic
                  title="Thẻ còn hạn dài"
                  value={expiring.data?.validCount ?? 0}
                  valueStyle={{ color: '#389e0d' }}
                />
              </Card>
            </Col>
          </Row>

          <Card size="small" title="Danh sách cần nhắc gia hạn">
            <Table
              rowKey="readerId"
              size="small"
              loading={loading}
              dataSource={expiring.data?.rows ?? []}
              columns={expiringColumns}
              pagination={{ pageSize: 20 }}
              locale={{ emptyText: <Empty description="Không có thẻ nào sắp hết hạn" /> }}
            />
          </Card>
        </Space>
      )}

      {kind === 'activity' && (
        <Space direction="vertical" size={16} style={{ width: '100%' }}>
          {!neverBorrowed && (activity.data ?? []).length > 0 && (
            <Card size="small" title="Bạn đọc mượn nhiều nhất">
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={(activity.data ?? []).slice(0, 15)}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="fullName" interval={0} angle={-25} textAnchor="end" height={90} />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Bar dataKey="loanCount" name="Lượt mượn" fill="#1677ff" />
                </BarChart>
              </ResponsiveContainer>
            </Card>
          )}

          <Card
            size="small"
            title={neverBorrowed ? 'Bạn đọc chưa từng mượn tài liệu' : 'Bảng số liệu'}
          >
            <Table
              rowKey="readerId"
              size="small"
              loading={loading}
              dataSource={activity.data ?? []}
              columns={activityColumns}
              pagination={{ pageSize: 20 }}
              locale={{ emptyText: <Empty description="Chưa có số liệu" /> }}
            />
          </Card>
        </Space>
      )}

      <Typography.Text type="secondary" style={{ fontSize: 12 }}>
        Tệp xuất ra lấy đúng bộ lọc đang hiển thị trên màn hình.
      </Typography.Text>
    </Space>
  );
}
