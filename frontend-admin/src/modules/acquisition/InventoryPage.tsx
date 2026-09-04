import { useEffect, useRef, useState } from 'react';
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
  Modal,
  Progress,
  Row,
  Select,
  Space,
  Statistic,
  Table,
  Tag,
  Typography,
  Upload,
} from 'antd';
import {
  BarcodeOutlined,
  CheckCircleOutlined,
  ExportOutlined,
  ImportOutlined,
  LockOutlined,
  PlusOutlined,
  PrinterOutlined,
  UnlockOutlined,
} from '@ant-design/icons';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import type { Dayjs } from 'dayjs';
import type { InputRef } from 'antd';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError, api } from '@/api/client';
import { saveBlob } from '@/modules/marc/api';
import { useCatalogOptions, toOptions } from '@/modules/cataloging/useCatalogOptions';
import { formsApi, inventoryApi, locationsApi } from './api';
import { MAU } from '@/lib/palette';
import {
  disposalTypes,
  formatDate,
  inventoryResultColors,
  inventoryResultLabels,
  inventoryStatusLabels,
  money,
} from './labels';
import type {
  InventoryPeriodDto,
  InventoryResultRowDto,
  InventoryResultType,
  InventoryScanResultDto,
} from './types';

/**
 * III.4 — Quản lý kiểm kê.
 *
 * Màn hình đi đúng thứ tự nghiệp vụ trong đặc tả: đóng kho, tạo kỳ và chốt danh sách kỳ vọng, quét,
 * chốt kỳ, ra kết quả. Không có nút nào cho phép nhảy bước, vì nhảy bước là cách một kỳ kiểm kê
 * không bao giờ khớp.
 */
export function InventoryPage() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [filter, setFilter] = useState<Record<string, unknown>>({ page: 1, pageSize: 20 });
  const [createOpen, setCreateOpen] = useState(false);
  const [workingId, setWorkingId] = useState<string | null>(null);

  const warehouses = useQuery({
    queryKey: ['acq-warehouses', null],
    queryFn: () => locationsApi.warehouses(),
  });

  const periods = useQuery({
    queryKey: ['inventory-periods', filter],
    queryFn: () => inventoryApi.periods(filter),
    placeholderData: keepPreviousData,
  });

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['inventory-periods'] });
    void queryClient.invalidateQueries({ queryKey: ['acq-warehouses'] });
  };

  const fail = (error: unknown) =>
    message.error(error instanceof ApiRequestError ? error.message : 'Không thực hiện được.');

  const setClosed = useMutation({
    mutationFn: ({ id, closed }: { id: string; closed: boolean }) =>
      inventoryApi.setWarehouseClosed(id, closed),
    onSuccess: (_, variables) => {
      message.success(variables.closed ? 'Đã đóng kho.' : 'Đã mở lại kho.');
      refresh();
    },
    onError: fail,
  });

  const columns: ColumnsType<InventoryPeriodDto> = [
    {
      title: 'Mã kỳ',
      dataIndex: 'code',
      width: 120,
      render: (value: string, row) => (
        <Button type="link" size="small" style={{ padding: 0 }} onClick={() => setWorkingId(row.id)}>
          {value}
        </Button>
      ),
    },
    { title: 'Tên kỳ kiểm kê', dataIndex: 'name' },
    { title: 'Kho', dataIndex: 'warehouseName', width: 170 },
    {
      title: 'Phạm vi',
      width: 200,
      render: (_, row) =>
        row.scopeType === 'ALL'
          ? 'Toàn kho'
          : row.scopeType === 'RANGE'
            ? `ĐKCB ${row.scopeFrom} → ${row.scopeTo}`
            : `Dạng: ${row.scopeDocumentTypeName ?? '—'}`,
    },
    {
      title: 'Thời gian',
      width: 190,
      render: (_, row) =>
        `${formatDate(row.startDate)}${row.endDate ? ` → ${formatDate(row.endDate)}` : ''}`,
    },
    {
      title: 'Tiến độ',
      width: 190,
      render: (_, row) => (
        <Progress
          percent={
            row.expectedCount === 0
              ? 0
              : Math.min(100, Math.round((row.scannedCount * 100) / row.expectedCount))
          }
          size="small"
          format={() => `${row.scannedCount}/${row.expectedCount}`}
        />
      ),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      width: 150,
      render: (value: InventoryPeriodDto['status'], row) => (
        <Space direction="vertical" size={0}>
          <Tag color={value === 'Closed' ? 'default' : 'processing'}>
            {inventoryStatusLabels[value]}
          </Tag>
          {row.warehouseClosed && <Tag color="gold">Kho đang đóng</Tag>}
        </Space>
      ),
    },
  ];

  return (
    <div className="lc-page">
      <PageHeader
        title="Kiểm kê kho"
        description="Đóng kho, chốt danh sách kỳ vọng, quét mã vạch, chốt kỳ rồi xử lý bản thiếu."
        actions={
          <Can permission={PERMISSIONS.acquisition.inventoryCreate}>
            <Button type="primary" icon={<PlusOutlined />} onClick={() => setCreateOpen(true)}>
              Tạo kỳ kiểm kê
            </Button>
          </Can>
        }
      />

      <Card variant="borderless" title="Trạng thái các kho" style={{ marginBottom: 12 }}>
        <Space wrap>
          {(warehouses.data ?? []).map((warehouse) => (
            <Card key={warehouse.id} size="small" styles={{ body: { padding: '8px 12px' } }}>
              <Space>
                <span>{warehouse.name}</span>
                {warehouse.isClosedForInventory ? (
                  <Tag color="gold" icon={<LockOutlined />}>
                    Đang đóng
                  </Tag>
                ) : (
                  <Tag color="green" icon={<UnlockOutlined />}>
                    Đang mở
                  </Tag>
                )}
                <Can permission={PERMISSIONS.acquisition.inventoryCreate}>
                  <Button
                    size="small"
                    loading={setClosed.isPending}
                    onClick={() =>
                      setClosed.mutate({
                        id: warehouse.id,
                        closed: !warehouse.isClosedForInventory,
                      })
                    }
                  >
                    {warehouse.isClosedForInventory ? 'Mở kho' : 'Đóng kho'}
                  </Button>
                </Can>
              </Space>
            </Card>
          ))}
        </Space>
      </Card>

      <Card variant="borderless">
        <Table
          rowKey="id"
          size="small"
          loading={periods.isFetching}
          columns={columns}
          dataSource={periods.data?.items ?? []}
          scroll={{ x: 1200 }}
          pagination={{
            current: periods.data?.page ?? 1,
            pageSize: periods.data?.pageSize ?? 20,
            total: periods.data?.totalCount ?? 0,
            showTotal: (total) => `Tổng ${total} kỳ kiểm kê`,
          }}
          onChange={(pagination) =>
            setFilter((current) => ({
              ...current,
              page: pagination.current ?? 1,
              pageSize: pagination.pageSize ?? 20,
            }))
          }
        />
      </Card>

      <CreatePeriodModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onDone={(id) => {
          setCreateOpen(false);
          refresh();
          setWorkingId(id);
        }}
      />

      {workingId && (
        <PeriodDrawer id={workingId} onClose={() => setWorkingId(null)} onChanged={refresh} />
      )}
    </div>
  );
}

function CreatePeriodModal({
  open,
  onClose,
  onDone,
}: {
  open: boolean;
  onClose: () => void;
  onDone: (id: string) => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm();
  const scopeType = Form.useWatch('scopeType', form) as string | undefined;

  const warehouses = useQuery({
    queryKey: ['acq-warehouses', null],
    queryFn: () => locationsApi.warehouses(),
  });
  const documentTypes = useCatalogOptions('document-types');

  // Danh sách cán bộ để phân công. Đường riêng cho các ô chọn người nhận việc, không đòi quyền quản
  // trị người dùng — cán bộ bổ sung lập kỳ kiểm kê không vì thế mà xem được hồ sơ tài khoản.
  const staff = useQuery({
    queryKey: ['staff-options'],
    queryFn: () => api.get<{ id: string; fullName: string; username: string }[]>('/staff/options'),
  });

  const create = useMutation({
    mutationFn: (values: Record<string, unknown>) => inventoryApi.createPeriod(values),
    onSuccess: (id) => {
      message.success('Đã tạo kỳ kiểm kê và chốt danh sách ấn phẩm kỳ vọng.');
      form.resetFields();
      onDone(id);
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không tạo được kỳ kiểm kê.'),
  });

  return (
    <Modal
      open={open}
      width={640}
      title="Tạo kỳ kiểm kê"
      onCancel={onClose}
      onOk={() => form.submit()}
      confirmLoading={create.isPending}
      okText="Tạo kỳ"
      cancelText="Bỏ qua"
      destroyOnHidden
    >
      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 12 }}
        message="Danh sách ấn phẩm kỳ vọng được chốt ngay lúc tạo kỳ."
        description="Sách nhập kho sau thời điểm này không tính vào kỳ, nếu không một kỳ kéo dài một tuần sẽ không bao giờ khớp."
      />

      <Form
        form={form}
        layout="vertical"
        initialValues={{ scopeType: 'ALL', closeWarehouse: true, assignedUserIds: [] }}
        onFinish={(values) =>
          create.mutate({
            ...values,
            startDate: (values.startDate as Dayjs | undefined)?.format('YYYY-MM-DD'),
            endDate: (values.endDate as Dayjs | undefined)?.format('YYYY-MM-DD'),
          })
        }
      >
        <Form.Item
          name="name"
          label="Tên kỳ kiểm kê"
          rules={[{ required: true, message: 'Chưa nhập tên kỳ.' }]}
        >
          <Input placeholder="Kiểm kê kho mở năm 2026" />
        </Form.Item>
        <Form.Item name="code" label="Mã kỳ" extra="Bỏ trống thì hệ thống tự sinh.">
          <Input />
        </Form.Item>
        <Form.Item
          name="warehouseId"
          label="Kho kiểm kê"
          rules={[{ required: true, message: 'Chưa chọn kho.' }]}
        >
          <Select
            options={(warehouses.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
          />
        </Form.Item>
        <Form.Item name="scopeType" label="Phạm vi kiểm kê">
          <Select
            options={[
              { value: 'ALL', label: 'Toàn kho' },
              { value: 'RANGE', label: 'Theo khoảng số ĐKCB' },
              { value: 'DOCTYPE', label: 'Theo dạng tài liệu' },
            ]}
          />
        </Form.Item>
        {scopeType === 'RANGE' && (
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item
                name="scopeFrom"
                label="Số ĐKCB từ"
                rules={[{ required: true, message: 'Chưa nhập số bắt đầu.' }]}
              >
                <Input />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                name="scopeTo"
                label="đến"
                rules={[{ required: true, message: 'Chưa nhập số kết thúc.' }]}
              >
                <Input />
              </Form.Item>
            </Col>
          </Row>
        )}
        {scopeType === 'DOCTYPE' && (
          <Form.Item
            name="scopeDocumentTypeId"
            label="Dạng tài liệu"
            rules={[{ required: true, message: 'Chưa chọn dạng tài liệu.' }]}
          >
            <Select options={toOptions(documentTypes.data)} />
          </Form.Item>
        )}
        <Row gutter={12}>
          <Col span={12}>
            <Form.Item name="startDate" label="Ngày bắt đầu">
              <DatePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item name="endDate" label="Ngày kết thúc dự kiến">
              <DatePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
            </Form.Item>
          </Col>
        </Row>
        <Form.Item
          name="assignedUserIds"
          label="Cán bộ kiểm kê"
          extra="Người được chọn nhận thông báo và tra được kỳ kiểm kê của mình."
        >
          <Select
            mode="multiple"
            allowClear
            showSearch
            optionFilterProp="label"
            placeholder="Chọn cán bộ được phân công"
            options={(staff.data ?? []).map((user) => ({
              value: user.id,
              label: `${user.fullName} (${user.username})`,
            }))}
          />
        </Form.Item>
        <Form.Item
          name="assignedStaff"
          label="Người ngoài danh sách tài khoản"
          extra="Cán bộ đơn vị khác đến hỗ trợ, chỉ ghi tên để in lên biên bản."
        >
          <Input placeholder="Nguyễn Thị Hoa (Phòng Hành chính)" />
        </Form.Item>
        <Form.Item name="closeWarehouse" label="Đóng kho ngay khi tạo kỳ">
          <Select
            options={[
              { value: true, label: 'Có — ngưng lưu thông tại kho này' },
              { value: false, label: 'Không — đóng kho sau' },
            ]}
          />
        </Form.Item>
        <Form.Item name="note" label="Ghi chú">
          <Input.TextArea rows={2} />
        </Form.Item>
      </Form>
    </Modal>
  );
}

/** Màn hình làm việc của một kỳ: quét liên tục, tiến độ, kết quả và xử lý bản thiếu. */
function PeriodDrawer({
  id,
  onClose,
  onChanged,
}: {
  id: string;
  onClose: () => void;
  onChanged: () => void;
}) {
  const { message, modal } = App.useApp();
  const [barcode, setBarcode] = useState('');

  // Phân công lại giữa kỳ: người ốm, người bận, kỳ kiểm kê chạy cả tuần.
  const [assignees, setAssignees] = useState<string[]>([]);

  const staff = useQuery({
    queryKey: ['staff-options'],
    queryFn: () => api.get<{ id: string; fullName: string; username: string }[]>('/staff/options'),
  });
  const [log, setLog] = useState<InventoryScanResultDto[]>([]);
  const [resultFilter, setResultFilter] = useState<InventoryResultType | null>(null);
  const [resultPage, setResultPage] = useState({ page: 1, pageSize: 20 });
  const [scanFile, setScanFile] = useState<File | null>(null);
  const [resolveOpen, setResolveOpen] = useState(false);
  const [resolveForm] = Form.useForm();
  const inputRef = useRef<InputRef>(null);

  const period = useQuery({
    queryKey: ['inventory-period', id],
    queryFn: () => inventoryApi.period(id),
  });

  // Ô chọn bắt đầu từ danh sách đang có của kỳ, chứ không từ rỗng: mở ra thấy trống rồi bấm Lưu là
  // xoá sạch phân công mà không ai định làm thế.
  useEffect(() => {
    if (period.data) {
      setAssignees(period.data.assignedUsers.map((user) => user.userId));
    }
  }, [period.data]);

  const summary = useQuery({
    queryKey: ['inventory-summary', id],
    queryFn: () => inventoryApi.summary(id),
    // Tiến độ kỳ đang chạy tự cập nhật: máy quét rời và điện thoại cũng quét vào cùng kỳ này,
    // nên màn hình của người điều phối phải thấy số của họ mà không phải bấm làm mới.
    refetchInterval: (query) =>
      period.data?.status !== 'Closed' && query.state.status === 'success' ? 5000 : false,
  });

  const results = useQuery({
    queryKey: ['inventory-results', id, resultFilter, resultPage],
    queryFn: () => inventoryApi.results(id, { ...resultPage, result: resultFilter }),
    placeholderData: keepPreviousData,
  });

  const reload = () => {
    void period.refetch();
    void summary.refetch();
    void results.refetch();
    onChanged();
  };

  const fail = (error: unknown) =>
    message.error(error instanceof ApiRequestError ? error.message : 'Không thực hiện được.');

  const scan = useMutation({
    mutationFn: (value: string) => inventoryApi.scan(id, value),
    onSuccess: (result) => {
      setLog((current) => [result, ...current].slice(0, 50));
      setBarcode('');
      inputRef.current?.focus();

      void summary.refetch();
      void results.refetch();

      if (result.outcome !== 'Match') {
        message.warning(`${result.barcode}: ${result.message}`);
      }
    },
    onError: (error) => {
      fail(error);
      setBarcode('');
      inputRef.current?.focus();
    },
  });

  const importScans = useMutation({
    mutationFn: () => inventoryApi.importScans(id, scanFile!),
    onSuccess: (result) => {
      message.success(
        `Đã nạp ${result.total} mã: khớp ${result.match}, thừa ${result.unexpected}, ` +
          `sai kho ${result.wrongWarehouse}, trùng ${result.duplicate}.`,
      );
      setScanFile(null);
      reload();
    },
    onError: fail,
  });

  const assign = useMutation({
    mutationFn: () => inventoryApi.assignStaff(id, assignees),
    onSuccess: async () => {
      message.success('Đã lưu phân công.');
      await period.refetch();
    },
    onError: fail,
  });

  const close = useMutation({
    mutationFn: () => inventoryApi.close(id, { reopenWarehouse: true }),
    onSuccess: (result) => {
      modal.success({
        title: `Đã chốt kỳ ${result.code}`,
        content: (
          <Space direction="vertical">
            <span>Khớp: {result.matchCount} bản</span>
            <span>Thiếu: {result.missingCount} bản, giá trị {money(result.missingValue)} đ</span>
            <span>Thừa: {result.unexpectedCount} bản</span>
            <span>Sai kho: {result.wrongWarehouseCount} bản</span>
          </Space>
        ),
      });
      reload();
    },
    onError: fail,
  });

  const exportResults = useMutation({
    mutationFn: () => inventoryApi.exportResults(id, resultFilter),
    onSuccess: ({ blob, fileName }) => saveBlob(blob, fileName),
    onError: fail,
  });

  const printMinutes = useMutation({
    mutationFn: () => formsApi.print('INVENTORY', period.data!.code),
    onSuccess: ({ blob, fileName }) => saveBlob(blob, fileName),
    onError: fail,
  });

  const resolve = useMutation({
    mutationFn: (values: Record<string, unknown>) => inventoryApi.resolveMissing(id, values),
    onSuccess: (result) => {
      message.success(
        `Đã xử lý ${result.affected} bản thiếu theo quyết định ${result.documentCode ?? ''}.`,
      );
      setResolveOpen(false);
      reload();
    },
    onError: fail,
  });

  useEffect(() => {
    inputRef.current?.focus();
  }, [period.data?.id]);

  const data = period.data;
  const stats = summary.data;
  const running = data?.status !== 'Closed';

  const resultColumns: ColumnsType<InventoryResultRowDto> = [
    { title: 'Mã vạch', dataIndex: 'barcode', width: 150 },
    { title: 'Số ĐKCB', dataIndex: 'registerNumber', width: 150 },
    { title: 'Nhan đề', dataIndex: 'title' },
    { title: 'Ký hiệu xếp giá', dataIndex: 'callNumber', width: 140 },
    {
      title: 'Kết quả',
      dataIndex: 'result',
      width: 120,
      render: (value: InventoryResultType) => (
        <Tag color={inventoryResultColors[value]}>{inventoryResultLabels[value]}</Tag>
      ),
    },
    {
      title: 'Kho thực tế',
      dataIndex: 'actualWarehouseName',
      width: 150,
      render: (value: string | null) => value ?? '—',
    },
    {
      title: 'Đơn giá',
      dataIndex: 'price',
      width: 110,
      align: 'right',
      render: (value: number) => money(value),
    },
    {
      title: 'Đã xử lý',
      dataIndex: 'isResolved',
      width: 100,
      render: (value: boolean) => (value ? <Tag color="green">Rồi</Tag> : <Tag>Chưa</Tag>),
    },
  ];

  return (
    <Drawer
      open
      width={1100}
      onClose={onClose}
      title={data ? `Kỳ kiểm kê ${data.code} — ${data.name}` : 'Kỳ kiểm kê'}
      extra={
        data && (
          <Space>
            <Can permission={PERMISSIONS.acquisition.inventoryReport}>
              <Button icon={<PrinterOutlined />} onClick={() => printMinutes.mutate()}>
                In biên bản kiểm kê
              </Button>
            </Can>
            <Can permission={PERMISSIONS.acquisition.inventoryReport}>
              <Button icon={<ExportOutlined />} onClick={() => exportResults.mutate()}>
                Xuất kết quả
              </Button>
            </Can>
            {running && (
              <Can permission={PERMISSIONS.acquisition.inventoryClose}>
                <Button
                  type="primary"
                  icon={<CheckCircleOutlined />}
                  loading={close.isPending}
                  onClick={() =>
                    modal.confirm({
                      title: 'Chốt kỳ kiểm kê?',
                      content: 'Sau khi chốt, kỳ không nhận thêm lần quét nào và kho được mở lại.',
                      okText: 'Chốt kỳ',
                      cancelText: 'Bỏ qua',
                      onOk: () => close.mutate(),
                    })
                  }
                >
                  Chốt kỳ
                </Button>
              </Can>
            )}
          </Space>
        )
      }
    >
      {data && stats && (
        <Space direction="vertical" size="middle" style={{ width: '100%' }}>
          <Row gutter={12}>
            <Col span={5}>
              <Card size="small">
                <Statistic title="Theo sổ" value={stats.expectedCount} />
              </Card>
            </Col>
            <Col span={5}>
              <Card size="small">
                <Statistic title="Khớp" value={stats.matchCount} valueStyle={{ color: MAU.tot }} />
              </Card>
            </Col>
            <Col span={5}>
              <Card size="small">
                <Statistic title="Thiếu" value={stats.missingCount} valueStyle={{ color: MAU.loi }} />
              </Card>
            </Col>
            <Col span={5}>
              <Card size="small">
                <Statistic title="Thừa" value={stats.unexpectedCount} valueStyle={{ color: MAU.luuY }} />
              </Card>
            </Col>
            <Col span={4}>
              <Card size="small">
                <Statistic title="Sai kho" value={stats.wrongWarehouseCount} />
              </Card>
            </Col>
          </Row>

          <Progress percent={stats.progressPercent} status={running ? 'active' : 'success'} />

          <Card variant="borderless" size="small" title="Cán bộ được phân công">
            <Space direction="vertical" style={{ width: '100%' }} size={8}>
              <Select
                mode="multiple"
                allowClear
                showSearch
                optionFilterProp="label"
                disabled={!running}
                placeholder="Chưa phân công ai"
                style={{ width: '100%' }}
                value={assignees}
                onChange={setAssignees}
                options={(staff.data ?? []).map((user) => ({
                  value: user.id,
                  label: `${user.fullName} (${user.username})`,
                }))}
              />

              {running && (
                <Button
                  size="small"
                  type="primary"
                  ghost
                  loading={assign.isPending}
                  onClick={() => assign.mutate()}
                >
                  Lưu phân công
                </Button>
              )}

              {data.assignedStaff && (
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  In lên biên bản: {data.assignedStaff}
                </Typography.Text>
              )}
            </Space>
          </Card>

          {running ? (
            <Card variant="borderless" size="small" title="Quét mã vạch">
              <Space direction="vertical" style={{ width: '100%' }}>
                <Input
                  ref={inputRef}
                  size="large"
                  prefix={<BarcodeOutlined />}
                  placeholder="Quét hoặc gõ mã vạch rồi nhấn Enter"
                  value={barcode}
                  disabled={scan.isPending}
                  onChange={(event) => setBarcode(event.target.value)}
                  onPressEnter={() => {
                    const value = barcode.trim();
                    if (value) scan.mutate(value);
                  }}
                />

                <Space>
                  <Upload
                    accept=".txt,.csv"
                    maxCount={1}
                    beforeUpload={(file) => {
                      setScanFile(file);
                      return false;
                    }}
                    onRemove={() => setScanFile(null)}
                    fileList={scanFile ? [{ uid: '1', name: scanFile.name }] : []}
                  >
                    <Button icon={<ImportOutlined />}>Chọn tệp quét từ máy đọc rời</Button>
                  </Upload>
                  <Button
                    type="primary"
                    disabled={!scanFile}
                    loading={importScans.isPending}
                    onClick={() => importScans.mutate()}
                  >
                    Nạp tệp
                  </Button>
                </Space>

                {log.length > 0 && (
                  <Table
                    rowKey={(row) => `${row.barcode}-${log.indexOf(row)}`}
                    size="small"
                    pagination={false}
                    scroll={{ y: 220 }}
                    dataSource={log}
                    columns={[
                      { title: 'Mã vạch', dataIndex: 'barcode', width: 150 },
                      {
                        title: 'Kết quả',
                        dataIndex: 'outcome',
                        width: 110,
                        render: (value: InventoryResultType) => (
                          <Tag color={inventoryResultColors[value]}>
                            {inventoryResultLabels[value]}
                          </Tag>
                        ),
                      },
                      { title: 'Nhan đề', dataIndex: 'title' },
                      { title: 'Ghi chú', dataIndex: 'message' },
                    ]}
                  />
                )}
              </Space>
            </Card>
          ) : (
            <Alert
              type="success"
              showIcon
              message={`Kỳ đã chốt ngày ${formatDate(data.endDate)}.`}
              description={`Giá trị bản thiếu: ${money(stats.missingValue)} đồng.`}
            />
          )}

          <Card
            variant="borderless"
            size="small"
            title="Kết quả kiểm kê"
            extra={
              <Space>
                <Select
                  allowClear
                  placeholder="Lọc theo kết quả"
                  style={{ width: 180 }}
                  value={resultFilter ?? undefined}
                  onChange={(value) => {
                    setResultFilter(value ?? null);
                    setResultPage({ page: 1, pageSize: 20 });
                  }}
                  options={Object.entries(inventoryResultLabels).map(([value, label]) => ({
                    value,
                    label,
                  }))}
                />
                {stats.missingCount > 0 && (
                  <Can permission={PERMISSIONS.acquisition.itemDispose}>
                    <Button danger onClick={() => setResolveOpen(true)}>
                      Xử lý bản thiếu
                    </Button>
                  </Can>
                )}
              </Space>
            }
          >
            <Table
              rowKey="id"
              size="small"
              loading={results.isFetching}
              columns={resultColumns}
              dataSource={results.data?.items ?? []}
              scroll={{ x: 1100 }}
              pagination={{
                current: results.data?.page ?? 1,
                pageSize: results.data?.pageSize ?? 20,
                total: results.data?.totalCount ?? 0,
                showTotal: (total) => `Tổng ${total} dòng`,
              }}
              onChange={(pagination) =>
                setResultPage({
                  page: pagination.current ?? 1,
                  pageSize: pagination.pageSize ?? 20,
                })
              }
            />
          </Card>
        </Space>
      )}

      <Modal
        open={resolveOpen}
        title="Xử lý bản thiếu"
        onCancel={() => setResolveOpen(false)}
        onOk={() => resolveForm.submit()}
        confirmLoading={resolve.isPending}
        okText="Lập quyết định"
        cancelText="Bỏ qua"
        okButtonProps={{ danger: true }}
        destroyOnHidden
      >
        <Typography.Paragraph type="secondary">
          Toàn bộ bản thiếu chưa xử lý của kỳ này sẽ được đưa ra khỏi kho theo một quyết định chung.
        </Typography.Paragraph>

        <Form
          form={resolveForm}
          layout="vertical"
          initialValues={{ disposalType: 'Mất' }}
          onFinish={(values) => resolve.mutate(values)}
        >
          <Form.Item name="disposalType" label="Hình thức">
            <Select options={disposalTypes.map((value) => ({ value, label: value }))} />
          </Form.Item>
          <Form.Item name="reason" label="Lý do">
            <Input.TextArea rows={2} placeholder="Không tìm thấy trên giá khi kiểm kê" />
          </Form.Item>
          <Form.Item name="decisionNo" label="Số quyết định" extra="Bỏ trống thì hệ thống tự sinh.">
            <Input />
          </Form.Item>
        </Form>
      </Modal>
    </Drawer>
  );
}
