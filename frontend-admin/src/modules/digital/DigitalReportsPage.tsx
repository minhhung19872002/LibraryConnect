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
  Tabs,
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
import { ApiRequestError } from '@/api/client';
import { saveBlob } from '@/modules/marc/api';
import { digitalApi } from './api';
import { formatGroupLabels, formatSize } from './labels';
import { MAU, MAU_BIEU_DO, mauBieuDo } from '@/lib/palette';
import type {
  DigitalCollectionDto,
  DigitalCountRowDto,
  DigitalReportFilter,
  DigitalTopDocumentRowDto,
  DigitalTopReaderRowDto,
} from './types';

type ReportKind = 'inventory' | 'usage' | 'storage' | 'requests';

const kindIndex: Record<ReportKind, number> = {
  inventory: 0,
  usage: 1,
  storage: 2,
  requests: 3,
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
 * V.4 — Bốn báo cáo thống kê tài liệu số. Mỗi báo cáo đều có bảng, biểu đồ và hai nút xuất tệp,
 * xuất đúng bộ lọc đang hiển thị chứ không phải toàn bộ dữ liệu.
 */
export function DigitalReportsPage() {
  const { message } = App.useApp();

  const [kind, setKind] = useState<ReportKind>('inventory');
  const [range, setRange] = useState<[Dayjs, Dayjs] | null>(null);
  const [collectionId, setCollectionId] = useState<string | undefined>(undefined);
  const [groupBy, setGroupBy] = useState('THANG');
  const [top, setTop] = useState(10);

  const filter: DigitalReportFilter = {
    fromDate: range?.[0]?.format('YYYY-MM-DD'),
    toDate: range?.[1]?.format('YYYY-MM-DD'),
    collectionId,
    groupBy,
    top,
  };

  const collections = useQuery({
    queryKey: ['digital-collections'],
    queryFn: () => digitalApi.collections(true),
  });

  const inventory = useQuery({
    queryKey: ['digital-report-inventory', filter],
    queryFn: () => digitalApi.inventoryReport(filter),
    enabled: kind === 'inventory',
  });

  const usage = useQuery({
    queryKey: ['digital-report-usage', filter],
    queryFn: () => digitalApi.usageReport(filter),
    enabled: kind === 'usage',
  });

  const storage = useQuery({
    queryKey: ['digital-report-storage'],
    queryFn: () => digitalApi.storageReport(),
    enabled: kind === 'storage',
  });

  const requests = useQuery({
    queryKey: ['digital-report-requests', filter],
    queryFn: () => digitalApi.requestReport(filter),
    enabled: kind === 'requests',
  });

  const exportReport = useMutation({
    mutationFn: (asPdf: boolean) =>
      digitalApi.exportReport({ kind: kindIndex[kind], asPdf, filter }),
    onSuccess: ({ blob, fileName }) => {
      saveBlob(blob, fileName);
      message.success('Đã tải tệp báo cáo xuống.');
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xuất được.'),
  });

  const countColumns: ColumnsType<DigitalCountRowDto> = [
    { title: 'Giá trị', dataIndex: 'label', ellipsis: true },
    {
      title: 'Số tài liệu',
      dataIndex: 'count',
      width: 130,
      align: 'right',
      render: (value: number) => value.toLocaleString('vi-VN'),
    },
    {
      title: 'Dung lượng',
      dataIndex: 'totalSize',
      width: 150,
      align: 'right',
      render: formatSize,
    },
  ];

  const topDocumentColumns: ColumnsType<DigitalTopDocumentRowDto> = [
    { title: 'Nhan đề', dataIndex: 'title', ellipsis: true },
    { title: 'Bộ sưu tập', dataIndex: 'collectionName', width: 220, ellipsis: true },
    { title: 'Lượt xem', dataIndex: 'views', width: 110, align: 'right' },
    { title: 'Lượt tải', dataIndex: 'downloads', width: 110, align: 'right' },
  ];

  const topReaderColumns: ColumnsType<DigitalTopReaderRowDto> = [
    { title: 'Bạn đọc', dataIndex: 'readerName', ellipsis: true },
    { title: 'Số thẻ', dataIndex: 'cardNumber', width: 160 },
    { title: 'Lượt xem', dataIndex: 'views', width: 110, align: 'right' },
    { title: 'Lượt tải', dataIndex: 'downloads', width: 110, align: 'right' },
  ];

  const exportButtons = (
    <Can permission={PERMISSIONS.digital.reportView}>
      <Space>
        <Button
          icon={<FileExcelOutlined />}
          loading={exportReport.isPending && exportReport.variables === false}
          onClick={() => exportReport.mutate(false)}
        >
          Xuất Excel
        </Button>
        <Button
          icon={<FilePdfOutlined />}
          loading={exportReport.isPending && exportReport.variables === true}
          onClick={() => exportReport.mutate(true)}
        >
          Xuất PDF
        </Button>
      </Space>
    </Can>
  );

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Báo cáo tài liệu số"
        description="Số lượng tài liệu, lượt xem và tải, dung lượng lưu trữ, tình hình yêu cầu đọc."
        actions={exportButtons}
      />

      <Card size="small">
        <Space wrap>
          <DatePicker.RangePicker
            format="DD/MM/YYYY"
            value={range}
            onChange={(values) => setRange(values as [Dayjs, Dayjs] | null)}
          />
          <Select
            allowClear
            style={{ width: 240 }}
            placeholder="Toàn bộ bộ sưu tập"
            options={flatten(collections.data ?? [])}
            value={collectionId}
            onChange={setCollectionId}
          />
          <Radio.Group
            value={groupBy}
            onChange={(event) => setGroupBy(event.target.value)}
            optionType="button"
            options={[
              { value: 'NGAY', label: 'Ngày' },
              { value: 'THANG', label: 'Tháng' },
              { value: 'QUY', label: 'Quý' },
              { value: 'NAM', label: 'Năm' },
            ]}
          />
          <Space>
            Số dòng xếp hạng:
            <InputNumber min={1} max={100} value={top} onChange={(value) => setTop(Number(value) || 10)} />
          </Space>
        </Space>
      </Card>

      <Tabs
        activeKey={kind}
        onChange={(key) => setKind(key as ReportKind)}
        items={[
          {
            key: 'inventory',
            label: '1. Số lượng tài liệu',
            children: inventory.data ? (
              <Space direction="vertical" size={16} style={{ width: '100%' }}>
                <Space size={16} wrap>
                  <Card size="small">
                    <Statistic title="Tổng tài liệu" value={inventory.data.totalDocuments} />
                  </Card>
                  <Card size="small">
                    <Statistic
                      title="Dung lượng"
                      value={formatSize(inventory.data.totalSize)}
                    />
                  </Card>
                  <Card size="small">
                    <Statistic
                      title="Tìm được toàn văn"
                      value={inventory.data.withText}
                      valueStyle={{ color: MAU.tot }}
                    />
                  </Card>
                  <Card size="small">
                    <Statistic title="Đã nhận dạng ký tự" value={inventory.data.ocrProcessed} />
                  </Card>
                </Space>

                <Row gutter={16}>
                  <Col span={12}>
                    <Card size="small" title="Theo bộ sưu tập">
                      <Table
                        rowKey="label"
                        size="small"
                        dataSource={inventory.data.byCollection}
                        columns={countColumns}
                        pagination={false}
                        scroll={{ x: 520 }}
                      />
                    </Card>
                  </Col>
                  <Col span={12}>
                    <Card size="small" title="Theo mức truy cập">
                      <ResponsiveContainer width="100%" height={260}>
                        <PieChart>
                          <Pie
                            data={inventory.data.byAccessLevel}
                            dataKey="count"
                            nameKey="label"
                            outerRadius={90}
                            label
                          >
                            {inventory.data.byAccessLevel.map((row, index) => (
                              <Cell key={row.label} fill={chartColors[index % chartColors.length]} />
                            ))}
                          </Pie>
                          <Tooltip />
                          <Legend />
                        </PieChart>
                      </ResponsiveContainer>
                    </Card>
                  </Col>
                </Row>

                <Card size="small" title="Theo định dạng">
                  <ResponsiveContainer width="100%" height={260}>
                    <BarChart data={inventory.data.byFormat}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis
                        dataKey="label"
                        tickFormatter={(label: string) => formatGroupLabels[label] ?? label}
                      />
                      <YAxis allowDecimals={false} />
                      <Tooltip />
                      <Bar dataKey="count" name="Số tài liệu" fill={mauBieuDo(2)} />
                    </BarChart>
                  </ResponsiveContainer>
                </Card>
              </Space>
            ) : (
              <Empty description="Đang tải số liệu" />
            ),
          },
          {
            key: 'usage',
            label: '2. Lượt xem và tải',
            children: usage.data ? (
              <Space direction="vertical" size={16} style={{ width: '100%' }}>
                <Space size={16} wrap>
                  <Card size="small">
                    <Statistic title="Lượt xem" value={usage.data.totalViews} />
                  </Card>
                  <Card size="small">
                    <Statistic title="Lượt tải" value={usage.data.totalDownloads} />
                  </Card>
                </Space>

                <Card size="small" title="Theo thời gian">
                  <ResponsiveContainer width="100%" height={280}>
                    <LineChart data={usage.data.byPeriod}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="label" />
                      <YAxis allowDecimals={false} />
                      <Tooltip />
                      <Legend />
                      <Line type="monotone" dataKey="views" name="Lượt xem" stroke={mauBieuDo(2)} />
                      <Line type="monotone" dataKey="downloads" name="Lượt tải" stroke={mauBieuDo(5)} />
                    </LineChart>
                  </ResponsiveContainer>
                </Card>

                <Row gutter={16}>
                  <Col span={14}>
                    <Card size="small" title="Tài liệu được xem nhiều nhất">
                      <Table
                        rowKey="documentId"
                        size="small"
                        dataSource={usage.data.topDocuments}
                        columns={topDocumentColumns}
                        pagination={false}
                        scroll={{ x: 700 }}
                      />
                    </Card>
                  </Col>
                  <Col span={10}>
                    <Card size="small" title="Bạn đọc dùng nhiều nhất">
                      <Table
                        rowKey="readerId"
                        size="small"
                        dataSource={usage.data.topReaders}
                        columns={topReaderColumns}
                        pagination={false}
                        scroll={{ x: 560 }}
                      />
                    </Card>
                  </Col>
                </Row>
              </Space>
            ) : (
              <Empty description="Đang tải số liệu" />
            ),
          },
          {
            key: 'storage',
            label: '3. Dung lượng lưu trữ',
            children: storage.data ? (
              <Space direction="vertical" size={16} style={{ width: '100%' }}>
                <Space size={16} wrap>
                  <Card size="small">
                    <Statistic title="Tổng dung lượng" value={formatSize(storage.data.totalSize)} />
                  </Card>
                  <Card size="small">
                    <Statistic title="Bản gốc" value={formatSize(storage.data.originalSize)} />
                  </Card>
                  <Card size="small">
                    <Statistic
                      title="Bản dẫn xuất (ảnh bìa…)"
                      value={formatSize(storage.data.derivedSize)}
                    />
                  </Card>
                  <Card size="small">
                    <Statistic title="Số tệp" value={storage.data.fileCount} />
                  </Card>
                </Space>

                <Card size="small" title="Theo định dạng">
                  <Table
                    rowKey="label"
                    size="small"
                    dataSource={storage.data.byFormat}
                    columns={countColumns}
                    pagination={false}
                    scroll={{ x: 520 }}
                  />
                </Card>
              </Space>
            ) : (
              <Empty description="Đang tải số liệu" />
            ),
          },
          {
            key: 'requests',
            label: '4. Yêu cầu đọc hạn chế',
            children: requests.data ? (
              <Space direction="vertical" size={16} style={{ width: '100%' }}>
                <Space size={16} wrap>
                  <Card size="small">
                    <Statistic title="Tổng yêu cầu" value={requests.data.total} />
                  </Card>
                  <Card size="small">
                    <Statistic
                      title="Chờ duyệt"
                      value={requests.data.pending}
                      valueStyle={{ color: requests.data.pending > 0 ? MAU.luuY : undefined }}
                    />
                  </Card>
                  <Card size="small">
                    <Statistic
                      title="Đã duyệt"
                      value={requests.data.approved}
                      valueStyle={{ color: MAU.tot }}
                    />
                  </Card>
                  <Card size="small">
                    <Statistic title="Từ chối" value={requests.data.rejected} />
                  </Card>
                  <Card size="small">
                    <Statistic
                      title="Xử lý trung bình"
                      value={requests.data.averageProcessingHours}
                      suffix="giờ"
                    />
                  </Card>
                </Space>

                <Card size="small" title="Theo thời gian">
                  <ResponsiveContainer width="100%" height={280}>
                    <BarChart data={requests.data.byPeriod}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="label" />
                      <YAxis allowDecimals={false} />
                      <Tooltip />
                      <Legend />
                      <Bar dataKey="views" name="Đã duyệt" fill={mauBieuDo(5)} />
                      <Bar dataKey="downloads" name="Từ chối" fill={mauBieuDo(1)} />
                    </BarChart>
                  </ResponsiveContainer>
                </Card>
              </Space>
            ) : (
              <Empty description="Đang tải số liệu" />
            ),
          },
        ]}
      />
    </Space>
  );
}

function flatten(nodes: DigitalCollectionDto[], depth = 0): { value: string; label: string }[] {
  return nodes.flatMap((node) => [
    { value: node.id, label: `${'— '.repeat(depth)}${node.name}` },
    ...flatten(node.children, depth + 1),
  ]);
}
