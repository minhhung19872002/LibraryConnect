import { useState } from 'react';
import {
  App,
  Button,
  Card,
  Col,
  DatePicker,
  Progress,
  Rate,
  Row,
  Select,
  Space,
  Statistic,
  Table,
  Tabs,
  Tag,
  Typography,
} from 'antd';
import { FileExcelOutlined, FilePdfOutlined } from '@ant-design/icons';
import { useMutation, useQuery } from '@tanstack/react-query';
import type { Dayjs } from 'dayjs';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { saveBlob } from '@/modules/marc/api';
import { useCatalogOptions, toOptions } from '@/modules/cataloging/useCatalogOptions';
import { acqReportsApi, locationsApi } from './api';
import { acquisitionTypeLabels, formatDate, money, orderStatusLabels } from './labels';
import { StatChart } from './StatChart';
import type { ChartMeasure } from './chartData';
import { MAU } from '@/lib/palette';
import type { AcquisitionReportFilter, DisposalReportRowDto } from './types';

/**
 * III.2 và III.7 — Báo cáo bổ sung.
 *
 * Mỗi tab là một câu hỏi mà thư viện phải trả lời khi báo cáo lên trên: năm nay bổ sung được gì,
 * thanh lý những gì, kho đang có bao nhiêu, và cắt lát theo chiều nào cũng phải ra được.
 */
export function AcquisitionReportsPage() {
  const { message } = App.useApp();

  const [filter, setFilter] = useState<AcquisitionReportFilter>({});
  const [dimension, setDimension] = useState('DOCTYPE');
  const [grouping, setGrouping] = useState('Month');
  const [pivotRow, setPivotRow] = useState('WAREHOUSE');
  const [pivotColumn, setPivotColumn] = useState('DOCTYPE');
  const [measure, setMeasure] = useState('Items');
  /** Chỉ tiêu vẽ trên biểu đồ của tab thống kê theo chiều. */
  const [chartMeasure, setChartMeasure] = useState<ChartMeasure>('itemCount');

  const warehouses = useQuery({
    queryKey: ['acq-warehouses', null],
    queryFn: () => locationsApi.warehouses(),
  });
  const fundingSources = useCatalogOptions('funding-sources');
  const documentTypes = useCatalogOptions('document-types');
  const suppliers = useCatalogOptions('suppliers');

  const dimensions = useQuery({
    queryKey: ['acq-dimensions'],
    queryFn: () => acqReportsApi.dimensions(),
  });

  const stats = useQuery({
    queryKey: ['acq-stats', dimension, grouping, filter],
    queryFn: () => acqReportsApi.statistics(dimension, grouping, filter),
  });

  const overview = useQuery({
    queryKey: ['acq-overview', filter],
    queryFn: () => acqReportsApi.overview(filter),
  });

  const pivot = useQuery({
    queryKey: ['acq-pivot', pivotRow, pivotColumn, measure, grouping, filter],
    queryFn: () => acqReportsApi.pivot(pivotRow, pivotColumn, measure, grouping, filter),
    enabled: pivotRow !== pivotColumn,
  });

  const acquisitionList = useQuery({
    queryKey: ['acq-list', filter],
    queryFn: () => acqReportsApi.acquisitionList(filter),
  });

  const disposals = useQuery({
    queryKey: ['acq-disposals', filter],
    queryFn: () => acqReportsApi.disposals(filter),
  });

  const approval = useQuery({
    queryKey: ['acq-approval', filter.from, filter.to],
    queryFn: () => acqReportsApi.purchaseApproval(filter.from, filter.to),
  });

  const supplierHistory = useQuery({
    queryKey: ['acq-supplier-history', filter.supplierId, filter.from, filter.to],
    queryFn: () => acqReportsApi.supplierHistory(filter.supplierId!, filter.from, filter.to),
    enabled: Boolean(filter.supplierId),
  });

  const exportReport = useMutation({
    mutationFn: ({ kind, format }: { kind: string; format: string }) =>
      acqReportsApi.export(kind, format, filter, {
        dimension: kind === 'Pivot' ? pivotRow : dimension,
        columnDimension: pivotColumn,
        measure,
        grouping,
      }),
    onSuccess: ({ blob, fileName }) => {
      saveBlob(blob, fileName);
      message.success('Đã xuất báo cáo.');
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xuất được.'),
  });

  const dimensionOptions = Object.entries(dimensions.data ?? {}).map(([value, label]) => ({
    value,
    label,
  }));

  const exportButtons = (kind: string) => (
    <Can permission={PERMISSIONS.acquisition.reportExport}>
      <Space>
        <Button
          icon={<FileExcelOutlined />}
          loading={exportReport.isPending}
          onClick={() => exportReport.mutate({ kind, format: 'Excel' })}
        >
          Excel
        </Button>
        <Button
          icon={<FilePdfOutlined />}
          loading={exportReport.isPending}
          onClick={() => exportReport.mutate({ kind, format: 'Pdf' })}
        >
          PDF
        </Button>
      </Space>
    </Can>
  );

  const statColumns = [
    { title: stats.data?.dimensionName ?? 'Chiều', dataIndex: 'label' },
    { title: 'Số đầu tài liệu', dataIndex: 'titleCount', width: 150, align: 'right' as const },
    { title: 'Số bản', dataIndex: 'itemCount', width: 120, align: 'right' as const },
    {
      title: 'Tỷ trọng',
      dataIndex: 'percent',
      width: 180,
      render: (value: number) => <Progress percent={value} size="small" />,
    },
    {
      title: 'Giá trị (VNĐ)',
      dataIndex: 'value',
      width: 160,
      align: 'right' as const,
      render: (value: number) => money(value),
    },
  ];

  return (
    <div className="lc-page">
      <PageHeader
        title="Báo cáo bổ sung"
        description="Thống kê theo dạng tài liệu, vật mang tin, thời gian và ngôn ngữ; bảng tổng hợp đa chiều; danh sách bổ sung và ĐKCB hủy bỏ."
      />

      <Card variant="borderless" style={{ marginBottom: 12 }} styles={{ body: { padding: 12 } }}>
        <Space wrap>
          <DatePicker.RangePicker
            format="DD/MM/YYYY"
            placeholder={['Từ ngày', 'đến ngày']}
            onChange={(range) =>
              setFilter({
                ...filter,
                from: range?.[0] ? (range[0] as Dayjs).format('YYYY-MM-DD') : null,
                to: range?.[1] ? (range[1] as Dayjs).format('YYYY-MM-DD') : null,
              })
            }
          />
          <Select
            allowClear
            placeholder="Kho"
            style={{ width: 190 }}
            value={filter.warehouseId ?? undefined}
            onChange={(value) => setFilter({ ...filter, warehouseId: value ?? null })}
            options={(warehouses.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
          />
          <Select
            allowClear
            placeholder="Nguồn kinh phí"
            style={{ width: 190 }}
            value={filter.fundingSourceId ?? undefined}
            onChange={(value) => setFilter({ ...filter, fundingSourceId: value ?? null })}
            options={toOptions(fundingSources.data)}
          />
          <Select
            allowClear
            placeholder="Dạng tài liệu"
            style={{ width: 190 }}
            value={filter.documentTypeId ?? undefined}
            onChange={(value) => setFilter({ ...filter, documentTypeId: value ?? null })}
            options={toOptions(documentTypes.data)}
          />
          <Select
            allowClear
            placeholder="Hình thức bổ sung"
            style={{ width: 180 }}
            value={filter.acquisitionType ?? undefined}
            onChange={(value) => setFilter({ ...filter, acquisitionType: value ?? null })}
            options={Object.entries(acquisitionTypeLabels).map(([value, label]) => ({
              value,
              label,
            }))}
          />
          <Select
            allowClear
            placeholder="Nhà cung cấp"
            style={{ width: 200 }}
            value={filter.supplierId ?? undefined}
            onChange={(value) => setFilter({ ...filter, supplierId: value ?? null })}
            options={toOptions(suppliers.data)}
          />
        </Space>
      </Card>

      <Tabs
        items={[
          {
            key: 'overview',
            label: 'Tổng quát',
            children: (
              <Space direction="vertical" size="middle" style={{ width: '100%' }}>
                <Space style={{ justifyContent: 'flex-end', width: '100%' }}>
                  {exportButtons('Overview')}
                </Space>
                <Row gutter={12}>
                  <Col span={5}>
                    <Card size="small">
                      <Statistic title="Biểu ghi" value={overview.data?.totalBibs ?? 0} />
                    </Card>
                  </Col>
                  <Col span={5}>
                    <Card size="small">
                      <Statistic title="Tổng số bản" value={overview.data?.totalItems ?? 0} />
                    </Card>
                  </Col>
                  <Col span={5}>
                    <Card size="small">
                      <Statistic
                        title="Sẵn sàng cho mượn"
                        value={overview.data?.availableItems ?? 0}
                        valueStyle={{ color: MAU.tot }}
                      />
                    </Card>
                  </Col>
                  <Col span={4}>
                    <Card size="small">
                      <Statistic title="Đang khóa" value={overview.data?.lockedItems ?? 0} />
                    </Card>
                  </Col>
                  <Col span={5}>
                    <Card size="small">
                      <Statistic
                        title="Tổng giá trị (VNĐ)"
                        value={money(overview.data?.totalValue ?? 0)}
                      />
                    </Card>
                  </Col>
                </Row>

                <Row gutter={12}>
                  <Col span={8}>
                    <Card variant="borderless" size="small" title="Theo kho">
                      <StatChart rows={overview.data?.byWarehouse ?? []} />
                    </Card>
                  </Col>
                  <Col span={8}>
                    <Card variant="borderless" size="small" title="Theo dạng tài liệu">
                      <StatChart rows={overview.data?.byDocumentType ?? []} defaultKind="pie" />
                    </Card>
                  </Col>
                  <Col span={8}>
                    <Card variant="borderless" size="small" title="Theo tình trạng">
                      <StatChart rows={overview.data?.byStatus ?? []} defaultKind="pie" />
                    </Card>
                  </Col>
                </Row>
                <Row gutter={12}>
                  {[
                    { title: 'Theo kho', rows: overview.data?.byWarehouse ?? [] },
                    { title: 'Theo dạng tài liệu', rows: overview.data?.byDocumentType ?? [] },
                    { title: 'Theo tình trạng', rows: overview.data?.byStatus ?? [] },
                  ].map((block) => (
                    <Col span={8} key={block.title}>
                      <Card variant="borderless" size="small" title={`${block.title} — bảng`}>
                        <Table
                          rowKey="label"
                          size="small"
                          pagination={false}
                          dataSource={block.rows}
                          columns={[
                            { title: 'Nhãn', dataIndex: 'label', width: 160, ellipsis: true },
                            { title: 'Số bản', dataIndex: 'itemCount', width: 90, align: 'right' },
                            {
                              title: 'Giá trị (VNĐ)',
                              dataIndex: 'value',
                              width: 120,
                              align: 'right',
                              render: (value: number) => money(value),
                            },
                          ]}
                        />
                      </Card>
                    </Col>
                  ))}
                </Row>
              </Space>
            ),
          },
          {
            key: 'statistics',
            label: 'Thống kê theo chiều',
            children: (
              <Card
                variant="borderless"
                title={stats.data?.title ?? 'Thống kê bổ sung'}
                extra={
                  <Space>
                    <Select
                      style={{ width: 200 }}
                      value={dimension}
                      onChange={setDimension}
                      options={dimensionOptions}
                    />
                    {dimension === 'TIME' && (
                      <Select
                        style={{ width: 140 }}
                        value={grouping}
                        onChange={setGrouping}
                        options={[
                          { value: 'Day', label: 'Theo ngày' },
                          { value: 'Month', label: 'Theo tháng' },
                          { value: 'Quarter', label: 'Theo quý' },
                          { value: 'Year', label: 'Theo năm' },
                        ]}
                      />
                    )}
                    {exportButtons('Statistics')}
                  </Space>
                }
              >
                <Space style={{ marginBottom: 8 }}>
                  <Typography.Text type="secondary">Biểu đồ theo</Typography.Text>
                  <Select
                    size="small"
                    style={{ width: 140 }}
                    value={chartMeasure}
                    onChange={setChartMeasure}
                    options={[
                      { value: 'itemCount', label: 'Số bản' },
                      { value: 'titleCount', label: 'Số đầu' },
                      { value: 'value', label: 'Giá trị' },
                    ]}
                  />
                </Space>
                <StatChart
                  rows={stats.data?.rows ?? []}
                  measure={chartMeasure}
                  unit={chartMeasure === 'titleCount' ? 'đầu' : 'bản'}
                  height={320}
                />
                <Table
                  rowKey="label"
                  size="small"
                  style={{ marginTop: 12 }}
                  loading={stats.isFetching}
                  columns={statColumns}
                  dataSource={stats.data?.rows ?? []}
                  pagination={false}
                  summary={() => (
                    <Table.Summary.Row>
                      <Table.Summary.Cell index={0}>
                        <Typography.Text strong>Tổng cộng</Typography.Text>
                      </Table.Summary.Cell>
                      <Table.Summary.Cell index={1} align="right">
                        <Typography.Text strong>{stats.data?.totalTitles ?? 0}</Typography.Text>
                      </Table.Summary.Cell>
                      <Table.Summary.Cell index={2} align="right">
                        <Typography.Text strong>{stats.data?.totalItems ?? 0}</Typography.Text>
                      </Table.Summary.Cell>
                      <Table.Summary.Cell index={3} />
                      <Table.Summary.Cell index={4} align="right">
                        <Typography.Text strong>{money(stats.data?.totalValue ?? 0)}</Typography.Text>
                      </Table.Summary.Cell>
                    </Table.Summary.Row>
                  )}
                />
              </Card>
            ),
          },
          {
            key: 'pivot',
            label: 'Bảng tổng hợp',
            children: (
              <Card
                variant="borderless"
                title="Bảng tổng hợp đa chiều"
                extra={
                  <Space>
                    <Select
                      style={{ width: 180 }}
                      value={pivotRow}
                      onChange={setPivotRow}
                      options={dimensionOptions}
                    />
                    <span>×</span>
                    <Select
                      style={{ width: 180 }}
                      value={pivotColumn}
                      onChange={setPivotColumn}
                      options={dimensionOptions}
                    />
                    <Select
                      style={{ width: 160 }}
                      value={measure}
                      onChange={setMeasure}
                      options={[
                        { value: 'Items', label: 'Số bản' },
                        { value: 'Titles', label: 'Số đầu' },
                        { value: 'Value', label: 'Giá trị' },
                      ]}
                    />
                    {exportButtons('Pivot')}
                  </Space>
                }
              >
                {pivotRow === pivotColumn ? (
                  <Typography.Text type="danger">
                    Chiều hàng và chiều cột phải khác nhau.
                  </Typography.Text>
                ) : (
                  <Table
                    rowKey="label"
                    size="small"
                    loading={pivot.isFetching}
                    scroll={{ x: 'max-content' }}
                    dataSource={pivot.data?.rows ?? []}
                    pagination={false}
                    columns={[
                      {
                        title: pivot.data?.rowDimensionName ?? '',
                        dataIndex: 'label',
                        fixed: 'left' as const,
                        width: 220,
                      },
                      ...(pivot.data?.columns ?? []).map((column, index) => ({
                        title: column,
                        width: 130,
                        align: 'right' as const,
                        render: (_: unknown, row: { values: number[] }) => money(row.values[index]),
                      })),
                      {
                        title: 'Tổng',
                        dataIndex: 'total',
                        width: 140,
                        align: 'right' as const,
                        render: (value: number) => (
                          <Typography.Text strong>{money(value)}</Typography.Text>
                        ),
                      },
                    ]}
                    summary={() => (
                      <Table.Summary.Row>
                        <Table.Summary.Cell index={0}>
                          <Typography.Text strong>Tổng cộng</Typography.Text>
                        </Table.Summary.Cell>
                        {(pivot.data?.columnTotals ?? []).map((total, index) => (
                          <Table.Summary.Cell key={index} index={index + 1} align="right">
                            <Typography.Text strong>{money(total)}</Typography.Text>
                          </Table.Summary.Cell>
                        ))}
                        <Table.Summary.Cell
                          index={(pivot.data?.columnTotals.length ?? 0) + 1}
                          align="right"
                        >
                          <Typography.Text strong>{money(pivot.data?.grandTotal ?? 0)}</Typography.Text>
                        </Table.Summary.Cell>
                      </Table.Summary.Row>
                    )}
                  />
                )}
              </Card>
            ),
          },
          {
            key: 'list',
            label: 'Danh sách bổ sung',
            children: (
              <Card
                variant="borderless"
                title="Danh sách tài liệu bổ sung"
                extra={exportButtons('AcquisitionList')}
              >
                <Table
                  rowKey="barcode"
                  size="small"
                  loading={acquisitionList.isFetching}
                  dataSource={acquisitionList.data ?? []}
                  scroll={{ x: 1400 }}
                  pagination={{ pageSize: 20, showTotal: (total) => `Tổng ${total} bản` }}
                  columns={[
                    { title: 'Mã vạch', dataIndex: 'barcode', width: 140 },
                    { title: 'Số ĐKCB', dataIndex: 'registerNumber', width: 150 },
                    { title: 'Nhan đề', dataIndex: 'title' },
                    { title: 'Tác giả', dataIndex: 'author', width: 170 },
                    { title: 'Kho', dataIndex: 'warehouseName', width: 140 },
                    { title: 'Nguồn kinh phí', dataIndex: 'fundingSourceName', width: 150 },
                    { title: 'Nhà cung cấp', dataIndex: 'supplierName', width: 170 },
                    {
                      title: 'Ngày bổ sung',
                      dataIndex: 'acquisitionDate',
                      width: 130,
                      render: (value: string) => formatDate(value),
                    },
                    {
                      title: 'Giá (VNĐ)',
                      dataIndex: 'price',
                      width: 130,
                      align: 'right',
                      render: (value: number) => money(value),
                    },
                  ]}
                />
              </Card>
            ),
          },
          {
            key: 'disposals',
            label: 'ĐKCB hủy bỏ',
            children: (
              <Card
                variant="borderless"
                title="Danh sách ĐKCB đã thanh lý, ghi mất hoặc hỏng"
                extra={exportButtons('Disposal')}
              >
                <Table
                  rowKey={(row: DisposalReportRowDto) => `${row.barcode}-${row.disposalDate}`}
                  size="small"
                  loading={disposals.isFetching}
                  dataSource={disposals.data ?? []}
                  scroll={{ x: 1300 }}
                  pagination={{ pageSize: 20, showTotal: (total) => `Tổng ${total} bản` }}
                  columns={[
                    { title: 'Mã vạch', dataIndex: 'barcode', width: 140 },
                    { title: 'Số ĐKCB', dataIndex: 'registerNumber', width: 150 },
                    { title: 'Nhan đề', dataIndex: 'title' },
                    { title: 'Kho', dataIndex: 'warehouseName', width: 140 },
                    {
                      title: 'Ngày quyết định',
                      dataIndex: 'disposalDate',
                      width: 140,
                      render: (value: string) => formatDate(value),
                    },
                    {
                      title: 'Hình thức',
                      dataIndex: 'disposalType',
                      width: 160,
                      render: (value: string) => <Tag>{value}</Tag>,
                    },
                    { title: 'Số quyết định', dataIndex: 'decisionNo', width: 150 },
                    { title: 'Lý do', dataIndex: 'reason', width: 220 },
                    {
                      title: 'Giá trị (VNĐ)',
                      dataIndex: 'value',
                      width: 130,
                      align: 'right',
                      render: (value: number) => money(value),
                    },
                  ]}
                />
              </Card>
            ),
          },
          {
            key: 'approval',
            label: 'Duyệt mua',
            children: (
              <Space direction="vertical" size="middle" style={{ width: '100%' }}>
                <Space style={{ justifyContent: 'flex-end', width: '100%' }}>
                  {exportButtons('PurchaseApproval')}
                </Space>
                <Row gutter={12}>
                  <Col span={4}>
                    <Card size="small">
                      <Statistic title="Tổng yêu cầu" value={approval.data?.totalRequests ?? 0} />
                    </Card>
                  </Col>
                  <Col span={4}>
                    <Card size="small">
                      <Statistic
                        title="Đã duyệt"
                        value={approval.data?.approvedRequests ?? 0}
                        valueStyle={{ color: MAU.tot }}
                      />
                    </Card>
                  </Col>
                  <Col span={4}>
                    <Card size="small">
                      <Statistic
                        title="Từ chối"
                        value={approval.data?.rejectedRequests ?? 0}
                        valueStyle={{ color: MAU.loi }}
                      />
                    </Card>
                  </Col>
                  <Col span={4}>
                    <Card size="small">
                      <Statistic title="Chờ xử lý" value={approval.data?.pendingRequests ?? 0} />
                    </Card>
                  </Col>
                  <Col span={4}>
                    <Card size="small">
                      <Statistic
                        title="Tỷ lệ duyệt"
                        value={approval.data?.approvalRate ?? 0}
                        suffix="%"
                      />
                    </Card>
                  </Col>
                  <Col span={4}>
                    <Card size="small">
                      <Statistic
                        title="Kinh phí duyệt (VNĐ)"
                        value={money(approval.data?.approvedAmount ?? 0)}
                      />
                    </Card>
                  </Col>
                </Row>

                <Row gutter={12}>
                  <Col span={8}>
                    <Card variant="borderless" size="small" title="Theo trạng thái">
                      <StatChart rows={approval.data?.byStatus ?? []} unit="yêu cầu" defaultKind="pie" />
                    </Card>
                  </Col>
                  <Col span={8}>
                    <Card variant="borderless" size="small" title="Theo đơn vị đề nghị">
                      <StatChart rows={approval.data?.byDepartment ?? []} unit="yêu cầu" />
                    </Card>
                  </Col>
                  <Col span={8}>
                    <Card variant="borderless" size="small" title="Kinh phí duyệt theo tháng">
                      <StatChart rows={approval.data?.byMonth ?? []} measure="value" />
                    </Card>
                  </Col>
                </Row>

                <Card variant="borderless" size="small" title="Theo đơn vị đề nghị — bảng">
                  <Table
                    rowKey="label"
                    size="small"
                    pagination={false}
                    dataSource={approval.data?.byDepartment ?? []}
                    columns={[
                      { title: 'Đơn vị', dataIndex: 'label', width: 300 },
                      { title: 'Số yêu cầu', dataIndex: 'itemCount', width: 120, align: 'right' },
                      {
                        title: 'Kinh phí duyệt (VNĐ)',
                        dataIndex: 'value',
                        width: 180,
                        align: 'right',
                        render: (value: number) => money(value),
                      },
                    ]}
                  />
                </Card>

                {filter.supplierId && supplierHistory.data && (
                  <Card
                    variant="borderless"
                    size="small"
                    title={`Lịch sử giao dịch — ${supplierHistory.data.supplierName}`}
                    extra={
                      <Space>
                        <Typography.Text type="secondary">Đánh giá:</Typography.Text>
                        {supplierHistory.data.rating > 0 ? (
                          <Rate disabled value={supplierHistory.data.rating} />
                        ) : (
                          <Typography.Text type="secondary">
                            chưa chấm — chấm ở Danh mục › Nhà cung cấp
                          </Typography.Text>
                        )}
                      </Space>
                    }
                  >
                    <Row gutter={12} style={{ marginBottom: 12 }}>
                      <Col span={6}>
                        <Statistic title="Số đơn" value={supplierHistory.data.orderCount} />
                      </Col>
                      <Col span={6}>
                        <Statistic
                          title="Tổng giá trị (VNĐ)"
                          value={money(supplierHistory.data.totalAmount)}
                        />
                      </Col>
                      <Col span={6}>
                        <Statistic
                          title="Tỷ lệ giao đủ"
                          value={supplierHistory.data.fulfilmentRate}
                          suffix="%"
                        />
                      </Col>
                      <Col span={6}>
                        <Statistic title="Đơn chưa giao đủ" value={supplierHistory.data.lateOrders} />
                      </Col>
                    </Row>

                    <Table
                      rowKey="code"
                      size="small"
                      pagination={false}
                      dataSource={supplierHistory.data.orders}
                      columns={[
                        { title: 'Mã đơn', dataIndex: 'code', width: 160 },
                        {
                          title: 'Ngày đặt',
                          dataIndex: 'orderDate',
                          width: 130,
                          render: (value: string) => formatDate(value),
                        },
                        {
                          title: 'Dự kiến giao',
                          dataIndex: 'expectedDate',
                          width: 140,
                          render: (value: string | null) => (value ? formatDate(value) : '—'),
                        },
                        {
                          title: 'Đã nhận',
                          width: 130,
                          align: 'right',
                          render: (_, row) => `${row.receivedQuantity} / ${row.orderedQuantity}`,
                        },
                        {
                          title: 'Giá trị (VNĐ)',
                          dataIndex: 'totalAmount',
                          width: 150,
                          align: 'right',
                          render: (value: number) => money(value),
                        },
                        {
                          title: 'Trạng thái',
                          dataIndex: 'status',
                          width: 150,
                          render: (value: keyof typeof orderStatusLabels) => orderStatusLabels[value],
                        },
                      ]}
                    />
                  </Card>
                )}
              </Space>
            ),
          },
        ]}
      />
    </div>
  );
}
