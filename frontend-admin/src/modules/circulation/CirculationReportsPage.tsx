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
import { FileExcelOutlined, FilePdfOutlined, MailOutlined } from '@ant-design/icons';
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
import { ApiRequestError } from '@/api/client';
import { saveBlob } from '@/modules/marc/api';
import { useCatalogOptions, toOptions } from '@/modules/cataloging/useCatalogOptions';
import { locationsApi } from '@/modules/acquisition/api';
import { circulationApi } from './api';
import { formatDate, money } from './labels';
import type { CirculationReportFilter, LoanRowDto, TopItemRowDto, TopReaderRowDto } from './types';
import { MAU, MAU_BIEU_DO, mauBieuDo } from '@/lib/palette';

type ReportKind = 'visits' | 'current' | 'history' | 'overdue' | 'lockers' | 'topReaders' | 'topItems';

const kindLabels: Record<ReportKind, string> = {
  visits: '1. Ra vào thư viện',
  current: '2. Đang mượn',
  history: '3. Lịch sử mượn trả',
  overdue: '4. Quá hạn',
  lockers: '5. Tủ đựng đồ',
  topReaders: '6. Bạn đọc mượn nhiều',
  topItems: '7. Ấn phẩm mượn nhiều',
};

/** Mã báo cáo gửi cho máy chủ khi xuất tệp, khớp với enum CirculationReportKind. */
const kindCodes: Record<ReportKind, number> = {
  visits: 0,
  current: 1,
  history: 2,
  overdue: 3,
  lockers: 4,
  topReaders: 5,
  topItems: 6,
};

/*
 * Màu của biểu đồ phân loại.
 *
 * Dùng thẳng dải màu biểu đồ, **không** trộn màu ngữ nghĩa vào. Lấy `MAU.chinh` và `MAU.tot` làm
 * hai màu đầu là sai hai lần: chúng là hai sắc xanh rêu gần nhau nên hai mảng cạnh nhau trong biểu
 * đồ tròn không phân biệt được, mà chúng còn *mang nghĩa* — xanh lá là "tốt", đỏ là "hỏng" — trong
 * khi ở đây chúng chỉ đang đánh dấu loại bạn đọc hay dạng tài liệu, không có gì tốt xấu.
 */
const chartColors = MAU_BIEU_DO;

/**
 * VII.5 — Bảy báo cáo lưu thông bắt buộc.
 *
 * Mỗi báo cáo đủ ba dạng đầu ra như đặc tả yêu cầu: bảng số liệu, biểu đồ và tệp xuất ra đúng bằng
 * bộ lọc đang xem.
 */
export function CirculationReportsPage() {
  const { message } = App.useApp();

  const [kind, setKind] = useState<ReportKind>('visits');
  const [filter, setFilter] = useState<CirculationReportFilter>({ top: 20 });

  const readerTypes = useCatalogOptions('reader-types');
  const faculties = useCatalogOptions('faculties');
  const documentTypes = useCatalogOptions('document-types');
  const warehouses = useQuery({
    queryKey: ['acq-warehouses', null],
    queryFn: () => locationsApi.warehouses(),
  });

  const visits = useQuery({
    queryKey: ['circ-report-visits', filter],
    queryFn: () => circulationApi.visitReport(filter),
    enabled: kind === 'visits',
  });

  const current = useQuery({
    queryKey: ['circ-report-current', filter],
    queryFn: () => circulationApi.currentLoansReport(filter),
    enabled: kind === 'current',
  });

  const history = useQuery({
    queryKey: ['circ-report-history', filter],
    queryFn: () => circulationApi.historyReport(filter),
    enabled: kind === 'history',
  });

  const overdue = useQuery({
    queryKey: ['circ-report-overdue', filter],
    queryFn: () => circulationApi.overdueReport(filter),
    enabled: kind === 'overdue',
  });

  const lockers = useQuery({
    queryKey: ['circ-report-lockers', filter],
    queryFn: () => circulationApi.lockerReport(filter),
    enabled: kind === 'lockers',
  });

  const topReaders = useQuery({
    queryKey: ['circ-report-top-readers', filter],
    queryFn: () => circulationApi.topReaders(filter),
    enabled: kind === 'topReaders',
  });

  const topItems = useQuery({
    queryKey: ['circ-report-top-items', filter],
    queryFn: () => circulationApi.topItems(filter),
    enabled: kind === 'topItems',
  });

  const exportReport = useMutation({
    mutationFn: (asPdf: boolean) =>
      circulationApi.exportReport({ kind: kindCodes[kind], asPdf, filter }),
    onSuccess: ({ blob, fileName }) => {
      saveBlob(blob, fileName);
      message.success('Đã xuất báo cáo.');
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xuất được.'),
  });

  const remind = useMutation({
    mutationFn: () => circulationApi.sendOverdueReminders(filter, []),
    onSuccess: (sent) => message.success(`Đã gửi nhắc tới ${sent} bạn đọc.`),
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không gửi được.'),
  });

  const loanColumns: ColumnsType<LoanRowDto> = [
    { title: 'Số thẻ', dataIndex: 'readerCardNumber', width: 130 },
    { title: 'Bạn đọc', dataIndex: 'readerName', width: 200 },
    { title: 'Lớp', dataIndex: 'className', width: 110 },
    { title: 'Mã vạch', dataIndex: 'barcode', width: 130 },
    { title: 'Nhan đề', dataIndex: 'title', ellipsis: true },
    { title: 'Ngày mượn', dataIndex: 'loanDate', width: 120, render: formatDate },
    { title: 'Hạn trả', dataIndex: 'dueDate', width: 120, render: formatDate },
    {
      title: 'Quá hạn',
      dataIndex: 'overdueDays',
      width: 110,
      align: 'right',
      render: (value: number) =>
        value > 0 ? <Tag color="red">{value} ngày</Tag> : '',
    },
    {
      title: 'Phạt tạm tính',
      dataIndex: 'estimatedFine',
      width: 130,
      align: 'right',
      render: (value: number) => (value > 0 ? money(value) : ''),
    },
  ];

  const topReaderColumns: ColumnsType<TopReaderRowDto> = [
    { title: 'Số thẻ', dataIndex: 'cardNumber', width: 140 },
    { title: 'Bạn đọc', dataIndex: 'fullName' },
    { title: 'Loại bạn đọc', dataIndex: 'readerTypeName', width: 150 },
    { title: 'Khoa', dataIndex: 'facultyName', width: 200, ellipsis: true },
    { title: 'Lớp', dataIndex: 'className', width: 110 },
    { title: 'Lượt mượn', dataIndex: 'loanCount', width: 110, align: 'right' },
    { title: 'Lượt quá hạn', dataIndex: 'overdueCount', width: 130, align: 'right' },
    {
      title: 'Lần gần nhất',
      dataIndex: 'lastLoanAt',
      width: 140,
      render: formatDate,
    },
  ];

  const topItemColumns: ColumnsType<TopItemRowDto> = [
    { title: 'Nhan đề', dataIndex: 'title' },
    { title: 'Tác giả', dataIndex: 'author', width: 200 },
    { title: 'Dạng tài liệu', dataIndex: 'documentTypeName', width: 160 },
    { title: 'DDC', dataIndex: 'ddc', width: 90 },
    { title: 'Số bản', dataIndex: 'copyCount', width: 90, align: 'right' },
    { title: 'Lượt mượn', dataIndex: 'loanCount', width: 110, align: 'right' },
  ];

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Báo cáo lưu thông"
        description="Bảy báo cáo bắt buộc: ra vào thư viện, đang mượn, lịch sử, quá hạn, tủ đựng đồ, bạn đọc và ấn phẩm mượn nhiều nhất."
        actions={
          <Can permission={PERMISSIONS.circulation.reportExport}>
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
              style={{ width: 170 }}
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
              style={{ width: 180 }}
              placeholder="Kho"
              options={(warehouses.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
              value={filter.warehouseId}
              onChange={(value) => setFilter({ ...filter, warehouseId: value })}
            />
            <Select
              allowClear
              style={{ width: 180 }}
              placeholder="Dạng tài liệu"
              options={toOptions(documentTypes.data)}
              value={filter.documentTypeId}
              onChange={(value) => setFilter({ ...filter, documentTypeId: value })}
            />

            {(kind === 'topReaders' || kind === 'topItems' || kind === 'lockers') && (
              <Space size={4}>
                <span>Lấy</span>
                <InputNumber<number>
                  min={1}
                  max={500}
                  style={{ width: 90 }}
                  value={filter.top}
                  onChange={(value) => setFilter({ ...filter, top: value ?? 20 })}
                />
                <span>dòng</span>
              </Space>
            )}
          </Space>
        </Space>
      </Card>

      {kind === 'visits' && (
        <Space direction="vertical" size={16} style={{ width: '100%' }}>
          <Row gutter={16}>
            <Col span={6}>
              <Card size="small">
                <Statistic title="Tổng lượt vào" value={visits.data?.totalVisits ?? 0} />
              </Card>
            </Col>
            <Col span={6}>
              <Card size="small">
                <Statistic title="Số bạn đọc" value={visits.data?.uniqueReaders ?? 0} />
              </Card>
            </Col>
            <Col span={6}>
              <Card size="small">
                <Statistic title="Đang ở trong" value={visits.data?.insideNow ?? 0} />
              </Card>
            </Col>
            <Col span={6}>
              <Card size="small">
                <Statistic
                  title="Thời gian ở lại trung bình"
                  value={visits.data?.averageMinutes ?? 0}
                  suffix="phút"
                />
              </Card>
            </Col>
          </Row>

          <Card size="small" title="Giờ cao điểm">
            {(visits.data?.byHour ?? []).length === 0 ? (
              <Empty description="Chưa có số liệu" />
            ) : (
              <ResponsiveContainer width="100%" height={280}>
                <BarChart data={visits.data?.byHour ?? []}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="label" interval={1} />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Bar dataKey="count" name="Lượt vào" fill={mauBieuDo(2)} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </Card>

          <Row gutter={16}>
            <Col span={14}>
              <Card size="small" title="Lượt vào theo ngày">
                {(visits.data?.byDay ?? []).length === 0 ? (
                  <Empty description="Chưa có số liệu" />
                ) : (
                  <ResponsiveContainer width="100%" height={260}>
                    <LineChart data={visits.data?.byDay ?? []}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="label" />
                      <YAxis allowDecimals={false} />
                      <Tooltip />
                      <Legend />
                      <Line type="monotone" dataKey="count" name="Lượt vào" stroke={mauBieuDo(5)} />
                    </LineChart>
                  </ResponsiveContainer>
                )}
              </Card>
            </Col>
            <Col span={10}>
              <Card size="small" title="Theo loại bạn đọc">
                {(visits.data?.byReaderType ?? []).length === 0 ? (
                  <Empty description="Chưa có số liệu" />
                ) : (
                  <ResponsiveContainer width="100%" height={260}>
                    <PieChart>
                      <Pie
                        data={visits.data?.byReaderType ?? []}
                        dataKey="count"
                        nameKey="label"
                        outerRadius={90}
                        label={(entry: { label?: string; count?: number }) =>
                          `${entry.label}: ${entry.count}`
                        }
                      >
                        {(visits.data?.byReaderType ?? []).map((row, index) => (
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
        </Space>
      )}

      {kind === 'current' && (
        <Card size="small" title={`Đang có ${current.data?.length ?? 0} tài liệu ngoài thư viện`}>
          <Table
            rowKey="id"
            size="small"
            loading={current.isFetching}
            dataSource={current.data ?? []}
            columns={loanColumns}
            scroll={{ x: 1400 }}
            pagination={{ pageSize: 20 }}
          />
        </Card>
      )}

      {kind === 'history' && (
        <Space direction="vertical" size={16} style={{ width: '100%' }}>
          <Row gutter={16}>
            <Col span={6}>
              <Card size="small">
                <Statistic title="Tổng lượt mượn" value={history.data?.totalLoans ?? 0} />
              </Card>
            </Col>
            <Col span={6}>
              <Card size="small">
                <Statistic title="Đã trả" value={history.data?.returned ?? 0} />
              </Card>
            </Col>
            <Col span={6}>
              <Card size="small">
                <Statistic title="Còn giữ" value={history.data?.stillOut ?? 0} />
              </Card>
            </Col>
            <Col span={6}>
              <Card size="small">
                <Statistic title="Tiền phạt đã ghi" value={money(history.data?.totalFine ?? 0)} suffix="đ" />
              </Card>
            </Col>
          </Row>

          <Card size="small" title="Lượt mượn theo ngày">
            {(history.data?.byDay ?? []).length === 0 ? (
              <Empty description="Chưa có số liệu" />
            ) : (
              <ResponsiveContainer width="100%" height={260}>
                <LineChart data={history.data?.byDay ?? []}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="label" />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Line type="monotone" dataKey="count" name="Lượt mượn" stroke={mauBieuDo(2)} />
                </LineChart>
              </ResponsiveContainer>
            )}
          </Card>

          <Card size="small" title="Chi tiết">
            <Table
              rowKey="id"
              size="small"
              loading={history.isFetching}
              dataSource={history.data?.rows ?? []}
              columns={loanColumns}
              scroll={{ x: 1400 }}
              pagination={{ pageSize: 20 }}
            />
          </Card>
        </Space>
      )}

      {kind === 'overdue' && (
        <Space direction="vertical" size={16} style={{ width: '100%' }}>
          <Row gutter={16}>
            <Col span={8}>
              <Card size="small">
                <Statistic
                  title="Tài liệu quá hạn"
                  value={overdue.data?.totalOverdue ?? 0}
                  valueStyle={{ color: MAU.loi }}
                />
              </Card>
            </Col>
            <Col span={8}>
              <Card size="small">
                <Statistic title="Số bạn đọc" value={overdue.data?.readers ?? 0} />
              </Card>
            </Col>
            <Col span={8}>
              <Card size="small">
                <Statistic
                  title="Tiền phạt tạm tính"
                  value={money(overdue.data?.estimatedFine ?? 0)}
                  suffix="đ"
                />
              </Card>
            </Col>
          </Row>

          <Card
            size="small"
            title="Mức độ trễ"
            extra={
              <Can permission={PERMISSIONS.circulation.loanView}>
                <Button
                  icon={<MailOutlined />}
                  loading={remind.isPending}
                  disabled={(overdue.data?.totalOverdue ?? 0) === 0}
                  onClick={() => remind.mutate()}
                >
                  Gửi nhắc hàng loạt
                </Button>
              </Can>
            }
          >
            {(overdue.data?.byRange ?? []).length === 0 ? (
              <Empty description="Chưa có số liệu" />
            ) : (
              <ResponsiveContainer width="100%" height={240}>
                <BarChart data={overdue.data?.byRange ?? []}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="label" />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Bar dataKey="count" name="Số tài liệu" fill={mauBieuDo(1)} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </Card>

          <Card size="small" title="Danh sách quá hạn">
            <Table
              rowKey="id"
              size="small"
              loading={overdue.isFetching}
              dataSource={overdue.data?.rows ?? []}
              columns={loanColumns}
              scroll={{ x: 1400 }}
              pagination={{ pageSize: 20 }}
            />
          </Card>
        </Space>
      )}

      {kind === 'lockers' && (
        <Space direction="vertical" size={16} style={{ width: '100%' }}>
          <Row gutter={16}>
            <Col span={6}>
              <Card size="small">
                <Statistic title="Tổng số tủ" value={lockers.data?.totalLockers ?? 0} />
              </Card>
            </Col>
            <Col span={6}>
              <Card size="small">
                <Statistic title="Lượt sử dụng" value={lockers.data?.totalUsages ?? 0} />
              </Card>
            </Col>
            <Col span={6}>
              <Card size="small">
                <Statistic
                  title="Thời lượng trung bình"
                  value={lockers.data?.averageMinutes ?? 0}
                  suffix="phút"
                />
              </Card>
            </Col>
            <Col span={6}>
              <Card size="small">
                <Statistic title="Đang mở" value={lockers.data?.openNow ?? 0} />
              </Card>
            </Col>
          </Row>

          <Card size="small" title="Tủ dùng nhiều nhất">
            {(lockers.data?.topLockers ?? []).length === 0 ? (
              <Empty description="Chưa có số liệu" />
            ) : (
              <ResponsiveContainer width="100%" height={260}>
                <BarChart data={lockers.data?.topLockers ?? []}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="label" />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Bar dataKey="count" name="Lượt dùng" fill={mauBieuDo(4)} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </Card>

          <Card size="small" title="Theo khu vực">
            <Table
              rowKey="key"
              size="small"
              pagination={false}
              dataSource={lockers.data?.byArea ?? []}
              columns={[
                { title: 'Khu vực', dataIndex: 'label' },
                { title: 'Lượt dùng', dataIndex: 'count', width: 140, align: 'right' },
                {
                  title: 'Tỷ lệ',
                  dataIndex: 'percentage',
                  width: 120,
                  align: 'right',
                  render: (value: number) => `${value.toLocaleString('vi-VN')}%`,
                },
              ]}
            />
          </Card>
        </Space>
      )}

      {kind === 'topReaders' && (
        <Space direction="vertical" size={16} style={{ width: '100%' }}>
          {(topReaders.data ?? []).length > 0 && (
            <Card size="small" title="Bạn đọc mượn nhiều nhất">
              <ResponsiveContainer width="100%" height={280}>
                <BarChart data={(topReaders.data ?? []).slice(0, 15)}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="fullName" interval={0} angle={-25} textAnchor="end" height={90} />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Bar dataKey="loanCount" name="Lượt mượn" fill={mauBieuDo(2)} />
                </BarChart>
              </ResponsiveContainer>
            </Card>
          )}

          <Card size="small" title="Bảng số liệu">
            <Table
              rowKey="readerId"
              size="small"
              loading={topReaders.isFetching}
              dataSource={topReaders.data ?? []}
              columns={topReaderColumns}
              scroll={{ x: 1100 }}
              pagination={{ pageSize: 20 }}
              locale={{ emptyText: <Empty description="Chưa có số liệu" /> }}
            />
          </Card>
        </Space>
      )}

      {kind === 'topItems' && (
        <Space direction="vertical" size={16} style={{ width: '100%' }}>
          {(topItems.data ?? []).length > 0 && (
            <Card size="small" title="Ấn phẩm được mượn nhiều nhất">
              <ResponsiveContainer width="100%" height={280}>
                <BarChart data={(topItems.data ?? []).slice(0, 12)}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="title" interval={0} angle={-20} textAnchor="end" height={110} />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Bar dataKey="loanCount" name="Lượt mượn" fill={mauBieuDo(5)} />
                </BarChart>
              </ResponsiveContainer>
            </Card>
          )}

          <Card size="small" title="Bảng số liệu">
            <Table
              rowKey="bibId"
              size="small"
              loading={topItems.isFetching}
              dataSource={topItems.data ?? []}
              columns={topItemColumns}
              scroll={{ x: 1200 }}
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
