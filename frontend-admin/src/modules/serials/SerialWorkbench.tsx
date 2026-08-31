import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Col,
  DatePicker,
  Drawer,
  Form,
  Input,
  InputNumber,
  Modal,
  Row,
  Select,
  Space,
  Statistic,
  Table,
  Tabs,
  Tag,
  Tooltip,
  Typography,
  Upload,
} from 'antd';
import {
  BookOutlined,
  DownloadOutlined,
  ImportOutlined,
  InboxOutlined,
  PlusOutlined,
  ProfileOutlined,
  WarningOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import dayjs, { type Dayjs } from 'dayjs';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { saveBlob } from '@/modules/marc/api';
import { locationsApi } from '@/modules/acquisition/api';
import { formatDate, money } from '@/modules/acquisition/labels';
import { serialsApi } from './api';
import {
  claimStatusColors,
  claimStatusLabels,
  frequencyLabels,
  issueStatusColors,
  issueStatusLabels,
} from './labels';
import type {
  IssuePreviewDto,
  SerialArticleDto,
  SerialIssueDto,
  SerialIssueStatus,
} from './types';

/**
 * Bàn làm việc của một đầu báo (IV.4).
 *
 * Gom vào một chỗ mọi việc làm với một đầu báo: nhìn lưới nhận số, sinh thêm số, ghi nhận số đến,
 * khiếu nại số thiếu, nhập mục lục bài trích và đóng tập. Tách ra nhiều màn hình sẽ bắt cán bộ nhớ
 * đang làm cho đầu báo nào.
 */
export function SerialWorkbenchDrawer({
  serialId,
  onClose,
  onChanged,
}: {
  serialId: string;
  onClose: () => void;
  onChanged: () => void;
}) {
  const serial = useQuery({ queryKey: ['serial', serialId], queryFn: () => serialsApi.get(serialId) });

  const grid = useQuery({
    queryKey: ['serial-grid', serialId],
    queryFn: () => serialsApi.grid(serialId),
  });

  const summary = useQuery({
    queryKey: ['serial-summary', serialId],
    queryFn: () => serialsApi.summary(serialId),
  });

  const issues = useQuery({
    queryKey: ['serial-issues', serialId],
    queryFn: () => serialsApi.issues({ serialId, pageSize: 500 }),
  });

  const claims = useQuery({
    queryKey: ['serial-claims', serialId],
    queryFn: () => serialsApi.claims(serialId),
  });

  const bindings = useQuery({
    queryKey: ['serial-bindings', serialId],
    queryFn: () => serialsApi.bindings(serialId),
  });

  const reload = () => {
    void grid.refetch();
    void summary.refetch();
    void issues.refetch();
    void claims.refetch();
    void bindings.refetch();
    void serial.refetch();
    onChanged();
  };

  const data = serial.data;

  return (
    <Drawer
      open
      width={1180}
      onClose={onClose}
      title={data ? data.title : 'Ấn phẩm định kỳ'}
    >
      {data && (
        <Space direction="vertical" size="middle" style={{ width: '100%' }}>
          <Row gutter={12}>
            <Col span={5}>
              <Card size="small">
                <Statistic title="Kỳ hạn" value={frequencyLabels[data.frequency]} />
              </Card>
            </Col>
            <Col span={5}>
              <Card size="small">
                <Statistic title="Đã nhận" value={data.receivedCount} valueStyle={{ color: '#389e0d' }} />
              </Card>
            </Col>
            <Col span={5}>
              <Card size="small">
                <Statistic title="Dự kiến" value={data.expectedCount} />
              </Card>
            </Col>
            <Col span={5}>
              <Card size="small">
                <Statistic title="Thiếu" value={data.missingCount} valueStyle={{ color: '#cf1322' }} />
              </Card>
            </Col>
            <Col span={4}>
              <Card size="small">
                <Statistic
                  title="Đơn giá / kỳ"
                  value={data.pricePerIssue ? money(data.pricePerIssue) : '—'}
                />
              </Card>
            </Col>
          </Row>

          <Tabs
            items={[
              {
                key: 'grid',
                label: 'Lưới nhận số',
                children: (
                  <IssueGridTab
                    serialId={serialId}
                    years={grid.data ?? []}
                    summary={summary.data ?? []}
                    onChanged={reload}
                  />
                ),
              },
              {
                key: 'issues',
                label: 'Ghi nhận số',
                children: (
                  <ReceiveTab
                    serialId={serialId}
                    issues={issues.data?.items ?? []}
                    defaultWarehouseId={data.warehouseId ?? null}
                    onChanged={reload}
                  />
                ),
              },
              {
                key: 'articles',
                label: 'Mục lục bài trích',
                children: <ArticlesTab issues={issues.data?.items ?? []} onChanged={reload} />,
              },
              {
                key: 'claims',
                label: `Khiếu nại (${claims.data?.length ?? 0})`,
                children: <ClaimsTab claims={claims.data ?? []} onChanged={reload} />,
              },
              {
                key: 'bindings',
                label: 'Đóng tập',
                children: (
                  <BindingsTab
                    serialId={serialId}
                    bindings={bindings.data ?? []}
                    issues={issues.data?.items ?? []}
                    defaultWarehouseId={data.warehouseId ?? null}
                    onChanged={reload}
                  />
                ),
              },
            ]}
          />
        </Space>
      )}
    </Drawer>
  );
}

/** IV.1 và IV.4 — Lưới các số theo năm, tô màu theo trạng thái. */
function IssueGridTab({
  serialId,
  years,
  summary,
  onChanged,
}: {
  serialId: string;
  years: import('./types').IssueGridYearDto[];
  summary: import('./types').SerialSummaryRowDto[];
  onChanged: () => void;
}) {
  const [generateOpen, setGenerateOpen] = useState(false);

  return (
    <Space direction="vertical" size="middle" style={{ width: '100%' }}>
      <Space>
        <Can permission={PERMISSIONS.serial.predict}>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setGenerateOpen(true)}>
            Sinh số dự kiến
          </Button>
        </Can>
        <Space size={4}>
          {(Object.keys(issueStatusLabels) as SerialIssueStatus[]).map((status) => (
            <Tag key={status} color={issueStatusColors[status]}>
              {issueStatusLabels[status]}
            </Tag>
          ))}
        </Space>
      </Space>

      {years.length === 0 ? (
        <Alert
          type="info"
          showIcon
          message="Chưa sinh số dự kiến nào."
          description="Bấm Sinh số dự kiến để hệ thống dựng danh sách số theo kỳ hạn đã khai."
        />
      ) : (
        years.map((year) => (
          <Card
            key={year.year}
            variant="borderless"
            size="small"
            title={`Năm ${year.year}`}
            extra={
              <Space size={4}>
                <Tag color="green">{year.received} đã nhận</Tag>
                <Tag>{year.expected} dự kiến</Tag>
                {year.missing > 0 && <Tag color="red">{year.missing} thiếu</Tag>}
                {year.bound > 0 && <Tag color="blue">{year.bound} đã đóng tập</Tag>}
              </Space>
            }
          >
            <Space wrap size={6}>
              {year.cells.map((cell) => (
                <Tooltip
                  key={cell.issueId}
                  title={
                    <>
                      <div>
                        {cell.volume ? `Tập ${cell.volume}, ` : ''}Số {cell.issueNo}
                      </div>
                      <div>Dự kiến: {formatDate(cell.expectedDate)}</div>
                      {cell.receivedDate && <div>Đã nhận: {formatDate(cell.receivedDate)}</div>}
                      <div>{issueStatusLabels[cell.status]}</div>
                    </>
                  }
                >
                  <Tag
                    color={cell.isOverdue ? 'red' : issueStatusColors[cell.status]}
                    style={{ margin: 0, minWidth: 46, textAlign: 'center' }}
                  >
                    {cell.issueNo}
                  </Tag>
                </Tooltip>
              ))}
            </Space>
          </Card>
        ))
      )}

      {summary.length > 0 && (
        <Card variant="borderless" size="small" title="Tổng hợp theo năm">
          <Table
            rowKey="year"
            size="small"
            pagination={false}
            dataSource={summary}
            columns={[
              { title: 'Năm', dataIndex: 'year', width: 100 },
              { title: 'Số kỳ dự kiến', dataIndex: 'planned', width: 140, align: 'right' },
              { title: 'Đã nhận', dataIndex: 'received', width: 120, align: 'right' },
              { title: 'Thiếu', dataIndex: 'missing', width: 100, align: 'right' },
              { title: 'Đã đóng tập', dataIndex: 'bound', width: 130, align: 'right' },
              {
                title: 'Tỷ lệ nhận',
                dataIndex: 'receivedPercent',
                width: 130,
                align: 'right',
                render: (value: number) => `${value}%`,
              },
              {
                title: 'Giá trị (VNĐ)',
                dataIndex: 'value',
                width: 150,
                align: 'right',
                render: (value: number) => money(value),
              },
            ]}
          />
        </Card>
      )}

      <GenerateIssuesModal
        open={generateOpen}
        serialId={serialId}
        onClose={() => setGenerateOpen(false)}
        onDone={() => {
          setGenerateOpen(false);
          onChanged();
        }}
      />
    </Space>
  );
}

/** IV.4 — Xem trước, sửa tay rồi chốt danh sách số dự kiến. */
function GenerateIssuesModal({
  open,
  serialId,
  onClose,
  onDone,
}: {
  open: boolean;
  serialId: string;
  onClose: () => void;
  onDone: () => void;
}) {
  const { message } = App.useApp();
  const [range, setRange] = useState<[Dayjs, Dayjs] | null>(null);
  const [preview, setPreview] = useState<IssuePreviewDto[] | null>(null);

  const load = useMutation({
    mutationFn: () =>
      serialsApi.previewIssues(
        serialId,
        range?.[0]?.format('YYYY-MM-DD'),
        range?.[1]?.format('YYYY-MM-DD'),
      ),
    onSuccess: (result) => {
      setPreview(result);

      if (result.length === 0) {
        message.warning('Kỳ hạn hiện tại không sinh ra số nào trong khoảng đã chọn.');
      }
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xem trước được.'),
  });

  const generate = useMutation({
    mutationFn: () =>
      serialsApi.generateIssues({
        serialIds: [serialId],
        from: range?.[0]?.format('YYYY-MM-DD'),
        to: range?.[1]?.format('YYYY-MM-DD'),
        issues: preview ?? undefined,
      }),
    onSuccess: (result) => {
      message.success(
        result.skipped > 0
          ? `Đã sinh ${result.created} số, bỏ qua ${result.skipped} số đã có.`
          : `Đã sinh ${result.created} số dự kiến.`,
      );
      setPreview(null);
      onDone();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không sinh được số.'),
  });

  const updateRow = (index: number, patch: Partial<IssuePreviewDto>) =>
    setPreview((current) =>
      (current ?? []).map((row, position) => (position === index ? { ...row, ...patch } : row)),
    );

  return (
    <Modal
      open={open}
      width={840}
      title="Sinh số dự kiến"
      onCancel={onClose}
      onOk={() => generate.mutate()}
      confirmLoading={generate.isPending}
      okText="Chốt danh sách"
      cancelText="Bỏ qua"
      okButtonProps={{ disabled: !preview || preview.length === 0 }}
      destroyOnHidden
    >
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <Space>
          <DatePicker.RangePicker
            format="DD/MM/YYYY"
            placeholder={['Từ ngày', 'đến ngày']}
            onChange={(value) => setRange(value as [Dayjs, Dayjs] | null)}
          />
          <Button type="primary" loading={load.isPending} onClick={() => load.mutate()}>
            Xem trước
          </Button>
        </Space>

        <Typography.Text type="secondary">
          Bỏ trống khoảng thời gian thì lấy theo thời gian đặt mua của đầu báo. Sửa tay từng dòng
          trước khi chốt nếu số nào ra khác lệ thường.
        </Typography.Text>

        {preview && preview.length > 0 && (
          <Table
            rowKey={(_, index) => String(index)}
            size="small"
            pagination={{ pageSize: 12, showSizeChanger: false }}
            dataSource={preview}
            columns={[
              {
                title: 'Số',
                width: 130,
                render: (_, row: IssuePreviewDto, index: number) => (
                  <Input
                    value={row.issueNo}
                    onChange={(event) => updateRow(index, { issueNo: event.target.value })}
                  />
                ),
              },
              {
                title: 'Tập',
                width: 100,
                render: (_, row: IssuePreviewDto, index: number) => (
                  <Input
                    value={row.volume ?? ''}
                    onChange={(event) => updateRow(index, { volume: event.target.value })}
                  />
                ),
              },
              { title: 'Năm', dataIndex: 'year', width: 90 },
              {
                title: 'Ngày dự kiến',
                width: 170,
                render: (_, row: IssuePreviewDto, index: number) => (
                  <DatePicker
                    format="DD/MM/YYYY"
                    style={{ width: '100%' }}
                    value={dayjs(row.expectedDate)}
                    onChange={(value) =>
                      updateRow(index, {
                        expectedDate: value?.format('YYYY-MM-DD') ?? row.expectedDate,
                      })
                    }
                  />
                ),
              },
              { title: 'Nhãn', dataIndex: 'caption' },
            ]}
          />
        )}
      </Space>
    </Modal>
  );
}

/** IV.3 và IV.4 — Ghi nhận số đến, đánh dấu thiếu và lập khiếu nại. */
function ReceiveTab({
  serialId,
  issues,
  defaultWarehouseId,
  onChanged,
}: {
  serialId: string;
  issues: SerialIssueDto[];
  defaultWarehouseId: string | null;
  onChanged: () => void;
}) {
  const { message } = App.useApp();
  const [selected, setSelected] = useState<string[]>([]);
  const [quantities, setQuantities] = useState<Record<string, number>>({});
  const [receiveOpen, setReceiveOpen] = useState(false);
  const [claimOpen, setClaimOpen] = useState(false);
  const [form] = Form.useForm();
  const [claimForm] = Form.useForm();

  const warehouses = useQuery({
    queryKey: ['acq-warehouses', null],
    queryFn: () => locationsApi.warehouses(),
  });

  const fail = (error: unknown) =>
    message.error(error instanceof ApiRequestError ? error.message : 'Không thực hiện được.');

  const receive = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      serialsApi.receive({
        issues: selected.map((id) => ({
          issueId: id,
          quantity: quantities[id] ?? 1,
          receivedDate: (values.receivedDate as Dayjs | undefined)?.format('YYYY-MM-DD'),
          note: values.note,
        })),
        createItems: values.createItems,
        warehouseId: values.warehouseId,
        shelfId: values.shelfId,
      }),
    onSuccess: (result) => {
      message.success(
        `Đã ghi nhận ${result.received} số và tạo ${result.createdItems} ĐKCB.`,
      );
      setSelected([]);
      setReceiveOpen(false);
      onChanged();
    },
    onError: fail,
  });

  const markMissing = useMutation({
    mutationFn: () => serialsApi.markMissing({ issueIds: selected, serialId }),
    onSuccess: (affected) => {
      message.success(`Đã đánh dấu ${affected} số là thiếu.`);
      setSelected([]);
      onChanged();
    },
    onError: fail,
  });

  const claim = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      serialsApi.createClaims({ issueIds: selected, ...values }),
    onSuccess: (result) => {
      message.success(
        `Đã lập ${result.created} phiếu khiếu nại: ${result.claimNumbers.join(', ')}.`,
      );
      setSelected([]);
      setClaimOpen(false);
      onChanged();
    },
    onError: fail,
  });

  const columns: ColumnsType<SerialIssueDto> = [
    {
      title: 'Số',
      width: 140,
      render: (_, row) => (
        <Space direction="vertical" size={0}>
          <span>{row.volume ? `Tập ${row.volume}, số ${row.issueNo}` : `Số ${row.issueNo}`}</span>
          <Typography.Text type="secondary">Năm {row.year}</Typography.Text>
        </Space>
      ),
    },
    {
      title: 'Dự kiến',
      dataIndex: 'expectedDate',
      width: 150,
      render: (value: string, row) => (
        <Space direction="vertical" size={0}>
          <span>{formatDate(value)}</span>
          {row.isOverdue && <Tag color="red">Quá hạn</Tag>}
        </Space>
      ),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      width: 150,
      render: (value: SerialIssueStatus, row) => (
        <Space direction="vertical" size={0}>
          <Tag color={issueStatusColors[value]}>{issueStatusLabels[value]}</Tag>
          {row.hasOpenClaim && <Tag color="orange">Đang khiếu nại</Tag>}
        </Space>
      ),
    },
    {
      title: 'Số lượng nhận',
      width: 130,
      render: (_, row) =>
        row.status === 'Expected' || row.status === 'Missing' ? (
          <InputNumber
            min={1}
            max={100}
            value={quantities[row.id] ?? 1}
            onChange={(value) => setQuantities({ ...quantities, [row.id]: value ?? 1 })}
            style={{ width: '100%' }}
          />
        ) : (
          <span>{row.quantity}</span>
        ),
    },
    {
      title: 'Ngày nhận',
      dataIndex: 'receivedDate',
      width: 130,
      render: (value: string | null) => (value ? formatDate(value) : '—'),
    },
    { title: 'Mã vạch', dataIndex: 'barcode', width: 150 },
    { title: 'Người nhận', dataIndex: 'receivedByName', width: 160 },
    { title: 'Bài trích', dataIndex: 'articleCount', width: 100, align: 'right' },
  ];

  const warehouseId = Form.useWatch('warehouseId', form) as string | undefined;

  const shelves = useQuery({
    queryKey: ['acq-shelves', warehouseId],
    queryFn: () => locationsApi.shelves(warehouseId),
    enabled: Boolean(warehouseId),
  });

  return (
    <Space direction="vertical" size="middle" style={{ width: '100%' }}>
      <Space>
        <Can permission={PERMISSIONS.serial.receive}>
          <Button
            type="primary"
            icon={<InboxOutlined />}
            disabled={selected.length === 0}
            onClick={() => {
              form.setFieldsValue({
                createItems: true,
                warehouseId: defaultWarehouseId ?? undefined,
                receivedDate: dayjs(),
              });
              setReceiveOpen(true);
            }}
          >
            Ghi nhận đã nhận ({selected.length})
          </Button>
        </Can>
        <Can permission={PERMISSIONS.serial.receive}>
          <Button
            icon={<WarningOutlined />}
            disabled={selected.length === 0}
            loading={markMissing.isPending}
            onClick={() => markMissing.mutate()}
          >
            Đánh dấu thiếu
          </Button>
        </Can>
        <Can permission={PERMISSIONS.serial.claim}>
          <Button
            danger
            disabled={selected.length === 0}
            onClick={() => setClaimOpen(true)}
          >
            Lập khiếu nại
          </Button>
        </Can>
      </Space>

      <Table
        rowKey="id"
        size="small"
        columns={columns}
        dataSource={issues}
        scroll={{ x: 1200 }}
        pagination={{ pageSize: 20, showTotal: (total) => `Tổng ${total} số` }}
        rowSelection={{
          selectedRowKeys: selected,
          onChange: (keys) => setSelected(keys as string[]),
          getCheckboxProps: (row) => ({ disabled: row.status === 'Bound' }),
        }}
      />

      <Modal
        open={receiveOpen}
        title={`Ghi nhận ${selected.length} số đã nhận`}
        onCancel={() => setReceiveOpen(false)}
        onOk={() => form.submit()}
        confirmLoading={receive.isPending}
        okText="Ghi nhận"
        cancelText="Bỏ qua"
        destroyOnHidden
      >
        <Form form={form} layout="vertical" onFinish={(values) => receive.mutate(values)}>
          <Form.Item name="receivedDate" label="Ngày nhận">
            <DatePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item
            name="createItems"
            label="Sinh ĐKCB cho từng bản"
            extra="Mỗi bản nhận về thành một ấn phẩm có mã vạch riêng trong kho."
          >
            <Select
              options={[
                { value: true, label: 'Có — sinh ĐKCB và mã vạch' },
                { value: false, label: 'Không — chỉ ghi nhận vào sổ' },
              ]}
            />
          </Form.Item>
          <Form.Item name="warehouseId" label="Kho lưu">
            <Select
              allowClear
              options={(warehouses.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
            />
          </Form.Item>
          <Form.Item name="shelfId" label="Giá">
            <Select
              allowClear
              disabled={!warehouseId}
              options={(shelves.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
            />
          </Form.Item>
          <Form.Item name="note" label="Ghi chú">
            <Input.TextArea rows={2} />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        open={claimOpen}
        title={`Lập khiếu nại cho ${selected.length} số`}
        onCancel={() => setClaimOpen(false)}
        onOk={() => claimForm.submit()}
        confirmLoading={claim.isPending}
        okText="Lập phiếu"
        cancelText="Bỏ qua"
        okButtonProps={{ danger: true }}
        destroyOnHidden
      >
        <Typography.Paragraph type="secondary">
          Bỏ trống nội dung thì hệ thống soạn sẵn câu khiếu nại kèm tên số và ngày phát hành dự kiến.
        </Typography.Paragraph>

        <Form
          form={claimForm}
          layout="vertical"
          onFinish={(values) =>
            claim.mutate({
              ...values,
              claimDate: (values.claimDate as Dayjs | undefined)?.format('YYYY-MM-DD'),
            })
          }
        >
          <Form.Item name="claimDate" label="Ngày khiếu nại">
            <DatePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="content" label="Nội dung khiếu nại">
            <Input.TextArea rows={3} />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}

/** IV.2 — Mục lục bài trích của một số. */
function ArticlesTab({
  issues,
  onChanged,
}: {
  issues: SerialIssueDto[];
  onChanged: () => void;
}) {
  const { message, modal } = App.useApp();
  const [issueId, setIssueId] = useState<string | null>(null);
  const [rows, setRows] = useState<SerialArticleDto[]>([]);
  const [file, setFile] = useState<File | null>(null);

  const received = issues.filter((issue) => issue.status !== 'Expected');

  const articles = useQuery({
    queryKey: ['serial-articles', issueId],
    queryFn: async () => {
      const loaded = await serialsApi.articles(issueId!);
      setRows(loaded);
      return loaded;
    },
    enabled: Boolean(issueId),
  });

  const fail = (error: unknown) =>
    message.error(error instanceof ApiRequestError ? error.message : 'Không thực hiện được.');

  const save = useMutation({
    mutationFn: () =>
      serialsApi.saveArticles(
        issueId!,
        rows.map((row) => ({
          id: row.id.startsWith('new-') ? null : row.id,
          title: row.title,
          authors: row.authors,
          pageFrom: row.pageFrom,
          pageTo: row.pageTo,
          abstract: row.abstract,
          keywords: row.keywords,
        })),
      ),
    onSuccess: (count) => {
      message.success(`Đã lưu mục lục ${count} bài.`);
      void articles.refetch();
      onChanged();
    },
    onError: fail,
  });

  const generate = useMutation({
    mutationFn: () => serialsApi.generateArticleRecords(issueId!, []),
    onSuccess: (result) => {
      message.success(`Đã sinh ${result.created} biểu ghi bài trích; bạn đọc tra được từ OPAC.`);
      void articles.refetch();
      onChanged();
    },
    onError: fail,
  });

  const template = useMutation({
    mutationFn: () => serialsApi.articleTemplate(),
    onSuccess: ({ blob, fileName }) => saveBlob(blob, fileName),
    onError: fail,
  });

  const importArticles = useMutation({
    mutationFn: () => serialsApi.importArticles(issueId!, file!),
    onSuccess: (result) => {
      message.success(`Đã nhập ${result.imported} bài trích.`);

      if (result.errors.length > 0) {
        modal.warning({
          title: 'Các dòng có vấn đề',
          width: 600,
          content: (
            <Table
              rowKey="rowNumber"
              size="small"
              pagination={false}
              scroll={{ y: 240 }}
              dataSource={result.errors}
              columns={[
                { title: 'Dòng', dataIndex: 'rowNumber', width: 80 },
                { title: 'Nội dung', dataIndex: 'message' },
              ]}
            />
          ),
        });
      }

      setFile(null);
      void articles.refetch();
      onChanged();
    },
    onError: fail,
  });

  const updateRow = (index: number, patch: Partial<SerialArticleDto>) =>
    setRows((current) => current.map((row, position) => (position === index ? { ...row, ...patch } : row)));

  return (
    <Space direction="vertical" size="middle" style={{ width: '100%' }}>
      <Space wrap>
        <Select
          placeholder="Chọn số báo"
          style={{ width: 300 }}
          value={issueId ?? undefined}
          onChange={(value) => setIssueId(value)}
          options={received.map((issue) => ({
            value: issue.id,
            label: `${issue.caption ?? `Số ${issue.issueNo} (${issue.year})`} — ${issue.articleCount} bài`,
          }))}
        />
        <Can permission={PERMISSIONS.serial.article}>
          <Button icon={<DownloadOutlined />} onClick={() => template.mutate()}>
            Tải tệp mẫu
          </Button>
        </Can>
        <Upload
          accept=".xlsx,.xls"
          maxCount={1}
          disabled={!issueId}
          beforeUpload={(selected) => {
            setFile(selected);
            return false;
          }}
          onRemove={() => setFile(null)}
          fileList={file ? [{ uid: '1', name: file.name }] : []}
        >
          <Button icon={<ImportOutlined />} disabled={!issueId}>
            Chọn tệp mục lục
          </Button>
        </Upload>
        <Button
          disabled={!file}
          loading={importArticles.isPending}
          onClick={() => importArticles.mutate()}
        >
          Nhập từ Excel
        </Button>
      </Space>

      {!issueId ? (
        <Alert
          type="info"
          showIcon
          message="Chọn một số báo đã nhận để nhập mục lục bài trích."
          description="Bài trích chỉ nhập cho số đã về tay, vì phải có tờ báo mới đọc được trang bao nhiêu."
        />
      ) : (
        <>
          <Space>
            <Can permission={PERMISSIONS.serial.article}>
              <Button
                type="primary"
                loading={save.isPending}
                onClick={() => save.mutate()}
              >
                Lưu mục lục
              </Button>
            </Can>
            <Button
              icon={<PlusOutlined />}
              onClick={() =>
                setRows((current) => [
                  ...current,
                  {
                    id: `new-${current.length}-${Date.now()}`,
                    issueId,
                    title: '',
                    authors: null,
                    pageFrom: null,
                    pageTo: null,
                    abstract: null,
                    keywords: null,
                    bibId: null,
                    controlNumber: null,
                  },
                ])
              }
            >
              Thêm bài
            </Button>
            <Can permission={PERMISSIONS.serial.article}>
              <Button
                icon={<BookOutlined />}
                loading={generate.isPending}
                onClick={() => generate.mutate()}
              >
                Sinh biểu ghi bài trích
              </Button>
            </Can>
          </Space>

          <Typography.Text type="secondary">
            Sinh biểu ghi tạo cho mỗi bài một biểu ghi MARC riêng, liên kết về tạp chí mẹ qua trường
            773. Đó là điều làm cho bạn đọc tra được tên bài trên OPAC.
          </Typography.Text>

          <Table
            rowKey="id"
            size="small"
            loading={articles.isFetching}
            pagination={false}
            dataSource={rows}
            scroll={{ x: 1100 }}
            columns={[
              {
                title: 'Nhan đề bài',
                render: (_, row: SerialArticleDto, index: number) => (
                  <Input
                    value={row.title}
                    disabled={Boolean(row.bibId)}
                    onChange={(event) => updateRow(index, { title: event.target.value })}
                  />
                ),
              },
              {
                title: 'Tác giả',
                width: 220,
                render: (_, row: SerialArticleDto, index: number) => (
                  <Input
                    value={row.authors ?? ''}
                    disabled={Boolean(row.bibId)}
                    placeholder="Nguyễn Văn A; Trần Thị B"
                    onChange={(event) => updateRow(index, { authors: event.target.value })}
                  />
                ),
              },
              {
                title: 'Trang từ',
                width: 100,
                render: (_, row: SerialArticleDto, index: number) => (
                  <InputNumber
                    min={1}
                    value={row.pageFrom ?? undefined}
                    disabled={Boolean(row.bibId)}
                    style={{ width: '100%' }}
                    onChange={(value) => updateRow(index, { pageFrom: value })}
                  />
                ),
              },
              {
                title: 'đến',
                width: 100,
                render: (_, row: SerialArticleDto, index: number) => (
                  <InputNumber
                    min={1}
                    value={row.pageTo ?? undefined}
                    disabled={Boolean(row.bibId)}
                    style={{ width: '100%' }}
                    onChange={(value) => updateRow(index, { pageTo: value })}
                  />
                ),
              },
              {
                title: 'Từ khóa',
                width: 200,
                render: (_, row: SerialArticleDto, index: number) => (
                  <Input
                    value={row.keywords ?? ''}
                    disabled={Boolean(row.bibId)}
                    onChange={(event) => updateRow(index, { keywords: event.target.value })}
                  />
                ),
              },
              {
                title: 'Biểu ghi',
                width: 150,
                render: (_, row: SerialArticleDto) =>
                  row.bibId ? (
                    <Tag color="green" icon={<ProfileOutlined />}>
                      {row.controlNumber}
                    </Tag>
                  ) : (
                    <Typography.Text type="secondary">Chưa sinh</Typography.Text>
                  ),
              },
              {
                title: '',
                width: 60,
                render: (_, row: SerialArticleDto, index: number) =>
                  row.bibId ? null : (
                    <Button
                      size="small"
                      danger
                      onClick={() =>
                        setRows((current) => current.filter((_item, position) => position !== index))
                      }
                    >
                      Xóa
                    </Button>
                  ),
              },
            ]}
          />
        </>
      )}
    </Space>
  );
}

/** IV.3 — Theo dõi phiếu khiếu nại nhà cung cấp. */
function ClaimsTab({
  claims,
  onChanged,
}: {
  claims: import('./types').SerialClaimDto[];
  onChanged: () => void;
}) {
  const { message } = App.useApp();
  const [responding, setResponding] = useState<string | null>(null);
  const [form] = Form.useForm();

  const respond = useMutation({
    mutationFn: (values: { response: string; status: import('./types').SerialClaimStatus }) =>
      serialsApi.respondClaim(responding!, values.response, values.status),
    onSuccess: () => {
      message.success('Đã ghi nhận phản hồi.');
      setResponding(null);
      onChanged();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không ghi nhận được.'),
  });

  if (claims.length === 0) {
    return <Alert type="success" showIcon message="Đầu báo này chưa có phiếu khiếu nại nào." />;
  }

  return (
    <>
      <Table
        rowKey="id"
        size="small"
        dataSource={claims}
        pagination={false}
        scroll={{ x: 1100 }}
        columns={[
          { title: 'Số phiếu', dataIndex: 'claimNo', width: 130 },
          {
            title: 'Ngày lập',
            dataIndex: 'claimDate',
            width: 120,
            render: (value: string) => formatDate(value),
          },
          { title: 'Số báo', dataIndex: 'issueCaption', width: 180 },
          { title: 'Nhà cung cấp', dataIndex: 'supplierName', width: 180 },
          { title: 'Nội dung', dataIndex: 'content' },
          {
            title: 'Phản hồi',
            dataIndex: 'response',
            width: 200,
            render: (value: string | null, row) => (
              <Space direction="vertical" size={0}>
                <span>{value ?? '—'}</span>
                {row.responseDate && (
                  <Typography.Text type="secondary">{formatDate(row.responseDate)}</Typography.Text>
                )}
              </Space>
            ),
          },
          {
            title: 'Trạng thái',
            dataIndex: 'status',
            width: 140,
            render: (value: import('./types').SerialClaimStatus) => (
              <Tag color={claimStatusColors[value]}>{claimStatusLabels[value]}</Tag>
            ),
          },
          {
            title: '',
            width: 120,
            render: (_, row) => (
              <Can permission={PERMISSIONS.serial.claim}>
                <Button
                  size="small"
                  onClick={() => {
                    form.setFieldsValue({ status: 'Responded', response: row.response ?? '' });
                    setResponding(row.id);
                  }}
                >
                  Ghi phản hồi
                </Button>
              </Can>
            ),
          },
        ]}
      />

      <Modal
        open={responding !== null}
        title="Ghi nhận phản hồi của nhà cung cấp"
        onCancel={() => setResponding(null)}
        onOk={() => form.submit()}
        confirmLoading={respond.isPending}
        okText="Lưu"
        cancelText="Bỏ qua"
        destroyOnHidden
      >
        <Form form={form} layout="vertical" onFinish={(values) => respond.mutate(values)}>
          <Form.Item
            name="response"
            label="Nội dung phản hồi"
            rules={[{ required: true, message: 'Chưa nhập nội dung phản hồi.' }]}
          >
            <Input.TextArea rows={3} />
          </Form.Item>
          <Form.Item name="status" label="Chuyển phiếu sang">
            <Select
              options={[
                { value: 'Responded', label: 'Đã phản hồi' },
                { value: 'Resolved', label: 'Đã giải quyết — nhà cung cấp đã gửi bù' },
                { value: 'Cancelled', label: 'Đã hủy — số quay lại danh sách thiếu' },
              ]}
            />
          </Form.Item>
        </Form>
      </Modal>
    </>
  );
}

/** IV.4 — Đóng tập. */
function BindingsTab({
  serialId,
  bindings,
  issues,
  defaultWarehouseId,
  onChanged,
}: {
  serialId: string;
  bindings: import('./types').SerialBindingDto[];
  issues: SerialIssueDto[];
  defaultWarehouseId: string | null;
  onChanged: () => void;
}) {
  const { message } = App.useApp();
  const [open, setOpen] = useState(false);
  const [form] = Form.useForm();

  const warehouses = useQuery({
    queryKey: ['acq-warehouses', null],
    queryFn: () => locationsApi.warehouses(),
  });

  const years = Array.from(new Set(issues.map((issue) => issue.year))).sort((a, b) => b - a);

  const bind = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      serialsApi.bind({
        serialId,
        ...values,
        bindingDate: (values.bindingDate as Dayjs | undefined)?.format('YYYY-MM-DD'),
      }),
    onSuccess: (result) => {
      message.success(
        `Đã đóng tập ${result.code} gồm ${result.issueCount} số, mã vạch ${result.barcode}.`,
      );
      setOpen(false);
      onChanged();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không đóng tập được.'),
  });

  return (
    <Space direction="vertical" size="middle" style={{ width: '100%' }}>
      <Space>
        <Can permission={PERMISSIONS.serial.bind}>
          <Button
            type="primary"
            icon={<BookOutlined />}
            onClick={() => {
              form.setFieldsValue({
                year: years[0],
                warehouseId: defaultWarehouseId ?? undefined,
                bindingDate: dayjs(),
              });
              setOpen(true);
            }}
          >
            Đóng tập
          </Button>
        </Can>
        <Typography.Text type="secondary">
          Tập đóng thành một ấn phẩm mới có mã vạch riêng; các số lẻ chuyển sang &quot;đã đóng tập&quot;
          nhưng vẫn còn trong sổ nhận số để đối chiếu khi kiểm kê.
        </Typography.Text>
      </Space>

      {bindings.length === 0 ? (
        <Alert type="info" showIcon message="Đầu báo này chưa đóng tập nào." />
      ) : (
        <Table
          rowKey="id"
          size="small"
          pagination={false}
          dataSource={bindings}
          columns={[
            { title: 'Mã tập', dataIndex: 'code', width: 130 },
            { title: 'Năm', dataIndex: 'year', width: 90 },
            {
              title: 'Khoảng số',
              width: 150,
              render: (_, row) => `${row.fromIssue} → ${row.toIssue}`,
            },
            { title: 'Số kỳ', dataIndex: 'issueCount', width: 100, align: 'right' },
            {
              title: 'Ngày đóng',
              dataIndex: 'bindingDate',
              width: 130,
              render: (value: string) => formatDate(value),
            },
            { title: 'Mã vạch tập', dataIndex: 'barcode', width: 160 },
            { title: 'Ký hiệu xếp giá', dataIndex: 'callNumber', width: 160 },
            { title: 'Ghi chú', dataIndex: 'note' },
          ]}
        />
      )}

      <Modal
        open={open}
        title="Đóng tập"
        onCancel={() => setOpen(false)}
        onOk={() => form.submit()}
        confirmLoading={bind.isPending}
        okText="Đóng tập"
        cancelText="Bỏ qua"
        destroyOnHidden
      >
        <Typography.Paragraph type="secondary">
          Chỉ các số đã nhận trong năm được chọn mới đóng được thành tập.
        </Typography.Paragraph>

        <Form form={form} layout="vertical" onFinish={(values) => bind.mutate(values)}>
          <Form.Item name="year" label="Năm" rules={[{ required: true, message: 'Chưa chọn năm.' }]}>
            <Select options={years.map((year) => ({ value: year, label: `Năm ${year}` }))} />
          </Form.Item>
          <Form.Item name="bindingDate" label="Ngày đóng tập">
            <DatePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item
            name="warehouseId"
            label="Kho lưu tập"
            rules={[{ required: true, message: 'Tập đóng là ấn phẩm trong kho nên phải chọn kho.' }]}
          >
            <Select
              options={(warehouses.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
            />
          </Form.Item>
          <Form.Item name="callNumber" label="Ký hiệu xếp giá của tập">
            <Input placeholder="070.4 TCTV 2026" />
          </Form.Item>
          <Form.Item name="price" label="Giá trị tập" extra="Bỏ trống thì tính bằng đơn giá kỳ nhân số kỳ.">
            <InputNumber min={0} step={1000} style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="note" label="Ghi chú">
            <Input.TextArea rows={2} />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}
