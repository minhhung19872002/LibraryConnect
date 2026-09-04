import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Col,
  DatePicker,
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
  Typography,
} from 'antd';
import { CalendarOutlined, InboxOutlined, WarningOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import dayjs, { type Dayjs } from 'dayjs';
import { PageHeader } from '@/components/PageHeader';
import { FilterBar } from '@/components/FilterBar';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { locationsApi } from '@/modules/acquisition/api';
import { formatDate } from '@/modules/acquisition/labels';
import { MAU } from '@/lib/palette';
import { serialsApi } from './api';
import { buildReceiveLines, groupUnresolvedBySerial } from './batch';
import { issueStatusColors, issueStatusLabels } from './labels';
import { BatchGenerateModal } from './SerialsPage';
import type { SerialIssueDto, SerialIssueStatus } from './types';

/**
 * IV.3 — Bổ sung tổng thể: xử lý hàng loạt nhiều đầu báo cùng lúc.
 *
 * Buổi sáng nhận một chồng báo của hai chục đầu khác nhau thì không ai mở hai chục bàn làm việc.
 * Màn hình này liệt kê mọi số đã đến hạn của mọi đầu báo, cho tick nhận hàng loạt với số lượng và
 * ngày nhận từng dòng, rồi đối chiếu những số lẽ ra đã về mà chưa về để lập khiếu nại.
 */
export function SerialBatchPage() {
  const [tab, setTab] = useState('due');

  return (
    <div className="lc-page">
      <PageHeader
        title="Bổ sung tổng thể"
        description="Số đến hạn của mọi đầu báo trong một bảng: sinh số, nhận hàng loạt, đối chiếu số thiếu và lập khiếu nại."
      />

      <Tabs
        activeKey={tab}
        onChange={setTab}
        items={[
          { key: 'due', label: 'Số đến hạn', children: <DueIssuesTab /> },
          { key: 'missing', label: 'Đối chiếu số thiếu', children: <UnresolvedTab /> },
          { key: 'generate', label: 'Sinh số nhiều đầu báo', children: <GenerateTab /> },
        ]}
      />
    </div>
  );
}

const issueColumns: ColumnsType<SerialIssueDto> = [
  {
    title: 'Báo, tạp chí',
    dataIndex: 'serialTitle',
    width: 280,
    ellipsis: true,
  },
  {
    title: 'Số',
    width: 150,
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
    width: 140,
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
  { title: 'Kho', dataIndex: 'warehouseName', width: 150 },
];

/** Số đã đến hạn phát hành mà chưa ghi nhận, của mọi đầu báo. */
function DueIssuesTab() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();
  const [draft, setDraft] = useState<{ keyword?: string; from?: string | null; to?: string | null }>({});
  const [filter, setFilter] = useState(draft);
  const [selected, setSelected] = useState<string[]>([]);
  const [quantities, setQuantities] = useState<Record<string, number>>({});
  const [dates, setDates] = useState<Record<string, string>>({});
  const [receiveOpen, setReceiveOpen] = useState(false);
  const [form] = Form.useForm();

  const issues = useQuery({
    queryKey: ['serial-issues', 'due', filter],
    queryFn: () =>
      serialsApi.issues({
        dueOnly: true,
        pageSize: 500,
        keyword: filter.keyword,
        expectedFrom: filter.from,
        expectedTo: filter.to,
      }),
  });

  const warehouses = useQuery({
    queryKey: ['acq-warehouses', null],
    queryFn: () => locationsApi.warehouses(),
  });

  const warehouseId = Form.useWatch('warehouseId', form) as string | undefined;

  const shelves = useQuery({
    queryKey: ['acq-shelves', warehouseId],
    queryFn: () => locationsApi.shelves(warehouseId),
    enabled: Boolean(warehouseId),
  });

  const rows = issues.data?.items ?? [];
  const titles = new Set(rows.map((row) => row.serialId)).size;

  const afterChange = () => {
    setSelected([]);
    void queryClient.invalidateQueries({ queryKey: ['serial-issues'] });
    void queryClient.invalidateQueries({ queryKey: ['serials'] });
  };

  const receive = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      serialsApi.receive({
        issues: buildReceiveLines(
          selected,
          quantities,
          dates,
          ((values.receivedDate as Dayjs | undefined) ?? dayjs()).format('YYYY-MM-DD'),
        ),
        createItems: values.createItems,
        warehouseId: values.warehouseId,
        shelfId: values.shelfId,
      }),
    onSuccess: (result) => {
      setReceiveOpen(false);
      afterChange();

      modal.success({
        title: `Đã ghi nhận ${result.received} số, tạo ${result.createdItems} ĐKCB`,
        width: 560,
        content:
          result.skipped.length > 0 ? (
            <ul style={{ paddingLeft: 18 }}>
              {result.skipped.map((line) => (
                <li key={line}>{line}</li>
              ))}
            </ul>
          ) : (
            <Typography.Text type="secondary">
              Mỗi bản nhận về đã thành một ấn phẩm trong kho, in tem được ngay ở Ấn phẩm trong kho.
            </Typography.Text>
          ),
      });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không ghi nhận được.'),
  });

  const markMissing = useMutation({
    mutationFn: () => serialsApi.markMissing({ issueIds: selected }),
    onSuccess: (affected) => {
      message.success(`Đã đánh dấu ${affected} số là thiếu.`);
      afterChange();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không thực hiện được.'),
  });

  const columns: ColumnsType<SerialIssueDto> = [
    ...issueColumns,
    {
      title: 'Số lượng nhận',
      width: 130,
      render: (_, row) => (
        <InputNumber
          min={1}
          max={100}
          value={quantities[row.id] ?? 1}
          onChange={(value) => setQuantities({ ...quantities, [row.id]: value ?? 1 })}
          style={{ width: '100%' }}
        />
      ),
    },
    {
      title: 'Ngày nhận',
      width: 160,
      render: (_, row) => (
        <DatePicker
          format="DD/MM/YYYY"
          placeholder="Hôm nay"
          value={dates[row.id] ? dayjs(dates[row.id]) : null}
          onChange={(value) =>
            setDates({ ...dates, [row.id]: value ? value.format('YYYY-MM-DD') : '' })
          }
          style={{ width: '100%' }}
        />
      ),
    },
  ];

  return (
    <Space direction="vertical" size="middle" style={{ width: '100%' }}>
      <Row gutter={12}>
        <Col span={6}>
          <Card size="small">
            <Statistic title="Số đến hạn chưa nhận" value={issues.data?.totalCount ?? 0} />
          </Card>
        </Col>
        <Col span={6}>
          <Card size="small">
            <Statistic title="Đầu báo có số đến hạn" value={titles} />
          </Card>
        </Col>
        <Col span={6}>
          <Card size="small">
            <Statistic
              title="Trong đó quá hạn"
              value={rows.filter((row) => row.isOverdue).length}
              valueStyle={{ color: MAU.loi }}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card size="small">
            <Statistic title="Đang chọn" value={selected.length} valueStyle={{ color: MAU.chinh }} />
          </Card>
        </Col>
      </Row>

      <FilterBar
        loading={issues.isFetching}
        onSearch={() => setFilter(draft)}
        onReset={() => {
          setDraft({});
          setFilter({});
        }}
        extra={
          <Space>
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
            <Can permission={PERMISSIONS.serial.receive}>
              <Button
                type="primary"
                icon={<InboxOutlined />}
                disabled={selected.length === 0}
                onClick={() => {
                  form.setFieldsValue({ createItems: true, receivedDate: dayjs() });
                  setReceiveOpen(true);
                }}
              >
                Ghi nhận đã nhận ({selected.length})
              </Button>
            </Can>
          </Space>
        }
      >
        <Input
          allowClear
          placeholder="Tên báo, số, mã vạch"
          style={{ width: 260 }}
          value={draft.keyword ?? ''}
          onChange={(event) => setDraft({ ...draft, keyword: event.target.value })}
        />
        <DatePicker.RangePicker
          format="DD/MM/YYYY"
          placeholder={['Dự kiến từ', 'đến']}
          onChange={(range) =>
            setDraft({
              ...draft,
              from: range?.[0] ? (range[0] as Dayjs).format('YYYY-MM-DD') : null,
              to: range?.[1] ? (range[1] as Dayjs).format('YYYY-MM-DD') : null,
            })
          }
        />
      </FilterBar>

      {rows.length === 0 && !issues.isFetching ? (
        <Alert
          type="info"
          showIcon
          message="Không có số nào đến hạn mà chưa ghi nhận."
          description="Sinh số dự kiến ở tab Sinh số nhiều đầu báo nếu các đầu báo chưa có khung số."
        />
      ) : (
        <Card variant="borderless">
          <Table
            rowKey="id"
            size="small"
            loading={issues.isFetching}
            columns={columns}
            dataSource={rows}
            scroll={{ x: 1180 }}
            pagination={{ pageSize: 50, showTotal: (total) => `Tổng ${total} số` }}
            rowSelection={{
              selectedRowKeys: selected,
              onChange: (keys) => setSelected(keys as string[]),
            }}
          />
        </Card>
      )}

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
        <Typography.Paragraph type="secondary">
          Số lượng và ngày nhận lấy theo từng dòng đã nhập trên bảng; dòng nào để trống ngày thì lấy
          ngày mặc định dưới đây. Kho để trống thì mỗi số vào kho đã phân cho đầu báo của nó.
        </Typography.Paragraph>
        <Form form={form} layout="vertical" onFinish={(values) => receive.mutate(values)}>
          <Form.Item name="receivedDate" label="Ngày nhận mặc định">
            <DatePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="createItems" label="Sinh ĐKCB cho từng bản">
            <Select
              options={[
                { value: true, label: 'Có — sinh ĐKCB và mã vạch' },
                { value: false, label: 'Không — chỉ ghi nhận vào sổ' },
              ]}
            />
          </Form.Item>
          <Form.Item name="warehouseId" label="Kho lưu (bỏ trống: kho của từng đầu báo)">
            <Select
              allowClear
              options={(warehouses.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
            />
          </Form.Item>
          <Form.Item name="shelfId" label="Vị trí giá">
            <Select
              allowClear
              disabled={!warehouseId}
              options={(shelves.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
            />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}

/** Số lẽ ra đã về mà chưa về, gom theo đầu báo, lập khiếu nại được cho nhiều đầu báo một lần. */
function UnresolvedTab() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();
  const [selected, setSelected] = useState<string[]>([]);
  const [serialFilter, setSerialFilter] = useState<string | null>(null);
  const [claimOpen, setClaimOpen] = useState(false);
  const [claimForm] = Form.useForm();

  const issues = useQuery({
    queryKey: ['serial-issues', 'unresolved'],
    queryFn: () => serialsApi.issues({ unresolvedOnly: true, pageSize: 500 }),
  });

  const rows = (issues.data?.items ?? []).filter(
    (row) => !serialFilter || row.serialId === serialFilter,
  );
  const groups = groupUnresolvedBySerial(issues.data?.items ?? []);

  const afterChange = () => {
    setSelected([]);
    void queryClient.invalidateQueries({ queryKey: ['serial-issues'] });
    void queryClient.invalidateQueries({ queryKey: ['serial-claims'] });
  };

  const markMissing = useMutation({
    mutationFn: () => serialsApi.markMissing({ issueIds: selected }),
    onSuccess: (affected) => {
      message.success(`Đã đánh dấu ${affected} số là thiếu.`);
      afterChange();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không thực hiện được.'),
  });

  const claim = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      serialsApi.createClaims({
        issueIds: selected,
        content: values.content,
        claimDate: (values.claimDate as Dayjs | undefined)?.format('YYYY-MM-DD'),
      }),
    onSuccess: (result) => {
      setClaimOpen(false);
      afterChange();

      modal.success({
        title: `Đã lập ${result.created} phiếu khiếu nại`,
        width: 560,
        content: (
          <Space direction="vertical">
            <Typography.Text>{result.claimNumbers.join(', ')}</Typography.Text>
            {result.skipped.length > 0 && (
              <ul style={{ paddingLeft: 18 }}>
                {result.skipped.map((line) => (
                  <li key={line}>{line}</li>
                ))}
              </ul>
            )}
            <Typography.Text type="secondary">
              Phiếu gửi tới nhà cung cấp của từng đầu báo; theo dõi phản hồi ở tab Khiếu nại trên
              bàn làm việc của đầu báo.
            </Typography.Text>
          </Space>
        ),
      });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lập được khiếu nại.'),
  });

  return (
    <Space direction="vertical" size="middle" style={{ width: '100%' }}>
      <Card variant="borderless" size="small" title="Theo đầu báo">
        {groups.length === 0 ? (
          <Typography.Text type="secondary">Mọi số đến hạn đều đã về.</Typography.Text>
        ) : (
          <Table
            rowKey="serialId"
            size="small"
            pagination={false}
            dataSource={groups}
            onRow={(row) => ({
              onClick: () => setSerialFilter(serialFilter === row.serialId ? null : row.serialId),
              style: { cursor: 'pointer' },
            })}
            rowClassName={(row) => (row.serialId === serialFilter ? 'lc-row-selected' : '')}
            columns={[
              { title: 'Báo, tạp chí', dataIndex: 'serialTitle', width: 360, ellipsis: true },
              { title: 'Số chưa về', dataIndex: 'count', width: 120, align: 'right' },
              { title: 'Đang khiếu nại', dataIndex: 'claimed', width: 140, align: 'right' },
              {
                title: 'Số cũ nhất dự kiến',
                dataIndex: 'oldestExpectedDate',
                width: 170,
                render: (value: string) => formatDate(value),
              },
            ]}
          />
        )}
      </Card>

      <Space>
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
            type="primary"
            disabled={selected.length === 0}
            onClick={() => {
              claimForm.setFieldsValue({ claimDate: dayjs() });
              setClaimOpen(true);
            }}
          >
            Lập khiếu nại ({selected.length})
          </Button>
        </Can>
        {serialFilter && (
          <Button type="link" onClick={() => setSerialFilter(null)}>
            Bỏ lọc theo đầu báo
          </Button>
        )}
      </Space>

      <Card variant="borderless">
        <Table
          rowKey="id"
          size="small"
          loading={issues.isFetching}
          columns={issueColumns}
          dataSource={rows}
          scroll={{ x: 900 }}
          pagination={{ pageSize: 50, showTotal: (total) => `Tổng ${total} số` }}
          rowSelection={{
            selectedRowKeys: selected,
            onChange: (keys) => setSelected(keys as string[]),
            getCheckboxProps: (row) => ({ disabled: row.hasOpenClaim }),
          }}
        />
      </Card>

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
          Mỗi số một phiếu, gửi tới nhà cung cấp đã khai cho đầu báo ấy. Bỏ trống nội dung thì hệ
          thống soạn sẵn câu khiếu nại kèm tên số và ngày phát hành dự kiến.
        </Typography.Paragraph>
        <Form form={claimForm} layout="vertical" onFinish={(values) => claim.mutate(values)}>
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

/** Sinh số dự kiến cho nhiều đầu báo — chọn từ danh sách chứ không phải mở từng đầu. */
function GenerateTab() {
  const queryClient = useQueryClient();
  const [serialIds, setSerialIds] = useState<string[]>([]);
  const [open, setOpen] = useState(false);

  const serials = useQuery({
    queryKey: ['serials', 'all-active'],
    queryFn: () => serialsApi.search({ page: 1, pageSize: 500, isActive: true }),
  });

  return (
    <Space direction="vertical" size="middle" style={{ width: '100%' }}>
      <Typography.Paragraph type="secondary">
        Chọn các đầu báo cần dựng khung số rồi bấm Sinh số. Mỗi đầu báo sinh theo kỳ hạn của chính
        nó; số đã có được bỏ qua nên chạy lại không nhân đôi.
      </Typography.Paragraph>
      <Space wrap>
        <Select
          mode="multiple"
          allowClear
          showSearch
          optionFilterProp="label"
          placeholder="Chọn đầu báo"
          style={{ minWidth: 480 }}
          value={serialIds}
          onChange={setSerialIds}
          options={(serials.data?.items ?? []).map((item) => ({ value: item.id, label: item.title }))}
        />
        <Button
          onClick={() => setSerialIds((serials.data?.items ?? []).map((item) => item.id))}
          disabled={(serials.data?.items ?? []).length === 0}
        >
          Chọn tất cả
        </Button>
        <Can permission={PERMISSIONS.serial.predict}>
          <Button
            type="primary"
            icon={<CalendarOutlined />}
            disabled={serialIds.length === 0}
            onClick={() => setOpen(true)}
          >
            Sinh số ({serialIds.length})
          </Button>
        </Can>
      </Space>

      <BatchGenerateModal
        open={open}
        serialIds={serialIds}
        onClose={() => setOpen(false)}
        onDone={() => {
          setOpen(false);
          void queryClient.invalidateQueries({ queryKey: ['serial-issues'] });
          void queryClient.invalidateQueries({ queryKey: ['serials'] });
        }}
      />
    </Space>
  );
}
