import { useEffect, useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Col,
  DatePicker,
  Descriptions,
  Drawer,
  Form,
  Input,
  InputNumber,
  Modal,
  Row,
  Select,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
  Upload,
} from 'antd';
import {
  FileTextOutlined,
  InboxOutlined,
  PlusOutlined,
  PrinterOutlined,
  UploadOutlined,
} from '@ant-design/icons';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import type { Dayjs } from 'dayjs';
import { PageHeader } from '@/components/PageHeader';
import { FilterBar } from '@/components/FilterBar';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { saveBlob } from '@/modules/marc/api';
import { useCatalogOptions, toOptions } from '@/modules/cataloging/useCatalogOptions';
import { formsApi, locationsApi, purchaseApi } from './api';
import { formatDate, money, orderStatusColors, orderStatusLabels } from './labels';
import type { PurchaseOrderDto, PurchaseOrderItemDto, PurchaseOrderStatus } from './types';

/**
 * III.1 — Quản lý đơn đặt.
 *
 * Đơn đặt là chỗ nối giữa tiền và sách: từ đây ghi nhận giao hàng, biên mục sơ lược cho dòng chưa
 * có biểu ghi, tạo ĐKCB, in đơn và lập biên bản bàn giao.
 */
export function PurchaseOrderPage() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [filter, setFilter] = useState<Record<string, unknown>>({ page: 1, pageSize: 20 });
  const [draft, setDraft] = useState<Record<string, unknown>>({});
  const [detailId, setDetailId] = useState<string | null>(null);
  const [editorOpen, setEditorOpen] = useState(false);

  const suppliers = useCatalogOptions('suppliers');
  const fundingSources = useCatalogOptions('funding-sources');

  const orders = useQuery({
    queryKey: ['purchase-orders', filter],
    queryFn: () => purchaseApi.orders(filter),
    placeholderData: keepPreviousData,
  });

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['purchase-orders'] });
    void queryClient.invalidateQueries({ queryKey: ['purchase-order'] });
    void queryClient.invalidateQueries({ queryKey: ['stock-items'] });
  };

  const print = useMutation({
    mutationFn: (code: string) => formsApi.print('ORDER', code),
    onSuccess: ({ blob, fileName }) => saveBlob(blob, fileName),
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không in được.'),
  });

  const columns: ColumnsType<PurchaseOrderDto> = [
    {
      title: 'Mã đơn',
      dataIndex: 'code',
      width: 150,
      render: (value: string, row) => (
        <Button type="link" size="small" style={{ padding: 0 }} onClick={() => setDetailId(row.id)}>
          {value}
        </Button>
      ),
    },
    { title: 'Nhà cung cấp', dataIndex: 'supplierName' },
    {
      title: 'Ngày đặt',
      dataIndex: 'orderDate',
      width: 115,
      render: (value: string) => formatDate(value),
    },
    {
      title: 'Dự kiến giao',
      dataIndex: 'expectedDate',
      width: 150,
      render: (value: string | null, row) => (
        <Space direction="vertical" size={0}>
          <span>{value ? formatDate(value) : '—'}</span>
          {row.isOverdue && <Tag color="error">Quá hạn {row.overdueDays} ngày</Tag>}
        </Space>
      ),
    },
    { title: 'Số hợp đồng', dataIndex: 'contractNo', width: 140 },
    {
      title: 'Đã nhận',
      width: 120,
      align: 'right',
      render: (_, row) => `${row.receivedQuantity} / ${row.orderedQuantity}`,
    },
    { title: 'ĐKCB đã tạo', dataIndex: 'itemCount', width: 120, align: 'right' },
    {
      title: 'Giá trị',
      dataIndex: 'totalAmount',
      width: 140,
      align: 'right',
      render: (value: number) => money(value),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      width: 140,
      render: (value: PurchaseOrderStatus) => (
        <Tag color={orderStatusColors[value]}>{orderStatusLabels[value]}</Tag>
      ),
    },
    {
      title: '',
      width: 60,
      align: 'right',
      render: (_, row) => (
        <Can permission={PERMISSIONS.acquisition.orderPrint}>
          <Tooltip title="In đơn đặt hàng">
            <Button
              size="small"
              icon={<PrinterOutlined />}
              loading={print.isPending}
              onClick={() => print.mutate(row.code)}
            />
          </Tooltip>
        </Can>
      ),
    },
  ];

  return (
    <div className="lc-page">
      <PageHeader
        title="Đơn đặt"
        description="Theo dõi giao hàng, nhập kho, in đơn và lập biên bản bàn giao."
        actions={
          <Can permission={PERMISSIONS.acquisition.orderCreate}>
            <Button type="primary" icon={<PlusOutlined />} onClick={() => setEditorOpen(true)}>
              Lập đơn đặt
            </Button>
          </Can>
        }
      />

      <FilterBar
        loading={orders.isFetching}
        onSearch={() => setFilter({ ...draft, page: 1, pageSize: 20 })}
        onReset={() => {
          setDraft({});
          setFilter({ page: 1, pageSize: 20 });
        }}
      >
        <Input
          allowClear
          placeholder="Mã đơn, số hợp đồng, nhà cung cấp"
          style={{ width: 300 }}
          value={(draft.keyword as string) ?? ''}
          onChange={(event) => setDraft({ ...draft, keyword: event.target.value })}
        />
        <Select
          allowClear
          placeholder="Trạng thái"
          style={{ width: 180 }}
          value={draft.status as string | undefined}
          onChange={(value) => setDraft({ ...draft, status: value })}
          options={Object.entries(orderStatusLabels).map(([value, label]) => ({ value, label }))}
        />
        <Select
          allowClear
          placeholder="Nhà cung cấp"
          style={{ width: 220 }}
          value={draft.supplierId as string | undefined}
          onChange={(value) => setDraft({ ...draft, supplierId: value })}
          options={toOptions(suppliers.data)}
        />
        <Select
          allowClear
          placeholder="Chỉ đơn quá hạn giao"
          style={{ width: 200 }}
          value={draft.overdueOnly as string | undefined}
          onChange={(value) => setDraft({ ...draft, overdueOnly: value })}
          options={[{ value: 'true', label: 'Chỉ đơn quá hạn giao' }]}
        />
        <DatePicker.RangePicker
          format="DD/MM/YYYY"
          placeholder={['Đặt từ', 'đến']}
          onChange={(range) =>
            setDraft({
              ...draft,
              from: range?.[0] ? (range[0] as Dayjs).format('YYYY-MM-DD') : undefined,
              to: range?.[1] ? (range[1] as Dayjs).format('YYYY-MM-DD') : undefined,
            })
          }
        />
      </FilterBar>

      <Card variant="borderless">
        <Table
          rowKey="id"
          size="small"
          loading={orders.isFetching}
          columns={columns}
          dataSource={orders.data?.items ?? []}
          scroll={{ x: 1400 }}
          pagination={{
            current: orders.data?.page ?? 1,
            pageSize: orders.data?.pageSize ?? 20,
            total: orders.data?.totalCount ?? 0,
            showSizeChanger: true,
            showTotal: (total) => `Tổng ${total} đơn`,
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

      {detailId && (
        <OrderDetailDrawer id={detailId} onClose={() => setDetailId(null)} onChanged={refresh} />
      )}

      <OrderEditorModal
        open={editorOpen}
        suppliers={toOptions(suppliers.data)}
        fundingSources={toOptions(fundingSources.data)}
        onClose={() => setEditorOpen(false)}
        onDone={(id) => {
          setEditorOpen(false);
          refresh();
          setDetailId(id);
        }}
      />
    </div>
  );
}

interface Option {
  value: string;
  label: string;
}

function OrderDetailDrawer({
  id,
  onClose,
  onChanged,
}: {
  id: string;
  onClose: () => void;
  onChanged: () => void;
}) {
  const { message } = App.useApp();
  const [received, setReceived] = useState<Record<string, number>>({});
  const [createItemsOpen, setCreateItemsOpen] = useState(false);
  const [quickCatalogLine, setQuickCatalogLine] = useState<PurchaseOrderItemDto | null>(null);
  const [handoverOpen, setHandoverOpen] = useState(false);

  const order = useQuery({
    queryKey: ['purchase-order', id],
    queryFn: () => purchaseApi.order(id),
  });

  const reload = () => {
    void order.refetch();
    onChanged();
  };

  const fail = (error: unknown) =>
    message.error(error instanceof ApiRequestError ? error.message : 'Không thực hiện được.');

  const receive = useMutation({
    mutationFn: () =>
      purchaseApi.receiveOrder(
        id,
        (order.data?.items ?? []).map((line) => ({
          itemId: line.id,
          receivedQuantity: received[line.id] ?? line.receivedQuantity,
        })),
      ),
    onSuccess: (status) => {
      message.success(`Đã ghi nhận giao hàng. Đơn chuyển sang "${orderStatusLabels[status as PurchaseOrderStatus] ?? status}".`);
      reload();
    },
    onError: fail,
  });

  const setStatus = useMutation({
    mutationFn: (status: string) => purchaseApi.setOrderStatus(id, status),
    onSuccess: () => {
      message.success('Đã cập nhật trạng thái đơn.');
      reload();
    },
    onError: fail,
  });

  const print = useMutation({
    mutationFn: (formType: string) => formsApi.print(formType, order.data!.code),
    onSuccess: ({ blob, fileName }) => saveBlob(blob, fileName),
    onError: fail,
  });

  const printHandover = useMutation({
    mutationFn: (code: string) => formsApi.print('HANDOVER', code),
    onSuccess: ({ blob, fileName }) => saveBlob(blob, fileName),
    onError: fail,
  });

  const data = order.data;

  const columns: ColumnsType<PurchaseOrderItemDto> = [
    {
      title: 'Nhan đề',
      dataIndex: 'title',
      render: (value: string, row) => (
        <Space direction="vertical" size={0}>
          <span>{value}</span>
          <Typography.Text type="secondary">
            {row.author ?? ''} {row.isbn ? `· ISBN ${row.isbn}` : ''}
          </Typography.Text>
          {row.requestCode && (
            <Typography.Text type="secondary">Từ yêu cầu {row.requestCode}</Typography.Text>
          )}
        </Space>
      ),
    },
    {
      title: 'Biểu ghi',
      width: 170,
      render: (_, row) =>
        row.bibId ? (
          <Tag color="green">{row.controlNumber ?? 'Đã biên mục'}</Tag>
        ) : (
          <Button size="small" onClick={() => setQuickCatalogLine(row)}>
            Biên mục sơ lược
          </Button>
        ),
    },
    { title: 'Đặt', dataIndex: 'quantity', width: 80, align: 'right' },
    {
      title: 'Thực nhận',
      width: 110,
      render: (_, row) => (
        <InputNumber
          min={0}
          max={row.quantity}
          value={received[row.id] ?? row.receivedQuantity}
          onChange={(value) => setReceived({ ...received, [row.id]: value ?? 0 })}
          style={{ width: '100%' }}
        />
      ),
    },
    { title: 'ĐKCB đã tạo', dataIndex: 'createdItemCount', width: 110, align: 'right' },
    {
      title: 'Đơn giá',
      dataIndex: 'unitPrice',
      width: 120,
      align: 'right',
      render: (value: number) => money(value),
    },
  ];

  return (
    <Drawer
      open
      width={1000}
      onClose={onClose}
      title={data ? `Đơn đặt ${data.code}` : 'Đơn đặt'}
      extra={
        data && (
          <Space>
            <Can permission={PERMISSIONS.acquisition.orderPrint}>
              <Button icon={<PrinterOutlined />} onClick={() => print.mutate('ORDER')}>
                In đơn
              </Button>
            </Can>
            <Can permission={PERMISSIONS.acquisition.orderPrint}>
              <Button icon={<FileTextOutlined />} onClick={() => print.mutate('RECEIPT')}>
                In phiếu nhập kho
              </Button>
            </Can>
            {data.status === 'New' && (
              <Can permission={PERMISSIONS.acquisition.orderApprove}>
                <Button type="primary" onClick={() => setStatus.mutate('Ordered')}>
                  Đánh dấu đã gửi NCC
                </Button>
              </Can>
            )}
          </Space>
        )
      }
    >
      {data && (
        <Space direction="vertical" size="middle" style={{ width: '100%' }}>
          {data.isOverdue && (
            <Alert
              type="error"
              showIcon
              message={`Đơn đã quá hạn giao ${data.overdueDays} ngày.`}
              description="Hãy liên hệ nhà cung cấp hoặc ghi nhận phần đã nhận."
            />
          )}

          <Descriptions column={3} size="small" bordered>
            <Descriptions.Item label="Nhà cung cấp">{data.supplierName}</Descriptions.Item>
            <Descriptions.Item label="Ngày đặt">{formatDate(data.orderDate)}</Descriptions.Item>
            <Descriptions.Item label="Dự kiến giao">
              {data.expectedDate ? formatDate(data.expectedDate) : '—'}
            </Descriptions.Item>
            <Descriptions.Item label="Số hợp đồng">{data.contractNo ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Nguồn kinh phí">{data.fundingSourceName ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Trạng thái">
              <Tag color={orderStatusColors[data.status]}>{orderStatusLabels[data.status]}</Tag>
            </Descriptions.Item>
            <Descriptions.Item label="Tổng giá trị" span={3}>
              {money(data.totalAmount)} đ
            </Descriptions.Item>
          </Descriptions>

          <Card
            variant="borderless"
            size="small"
            title="Dòng tài liệu"
            extra={
              <Space>
                <Can permission={PERMISSIONS.acquisition.orderReceive}>
                  <Button
                    type="primary"
                    loading={receive.isPending}
                    onClick={() => receive.mutate()}
                    disabled={data.status === 'Cancelled'}
                  >
                    Ghi nhận giao hàng
                  </Button>
                </Can>
                <Can permission={PERMISSIONS.acquisition.itemCreate}>
                  <Button
                    icon={<InboxOutlined />}
                    disabled={data.receivedQuantity === 0}
                    onClick={() => setCreateItemsOpen(true)}
                  >
                    Nhập kho — tạo ĐKCB
                  </Button>
                </Can>
              </Space>
            }
          >
            <Table
              rowKey="id"
              size="small"
              pagination={false}
              columns={columns}
              dataSource={data.items}
            />
          </Card>

          <Card
            variant="borderless"
            size="small"
            title="Biên bản bàn giao"
            extra={
              <Can permission={PERMISSIONS.acquisition.handoverManage}>
                <Button onClick={() => setHandoverOpen(true)}>Lập biên bản</Button>
              </Can>
            }
          >
            {data.handovers.length === 0 ? (
              <Typography.Text type="secondary">Đơn này chưa có biên bản bàn giao.</Typography.Text>
            ) : (
              <Table
                rowKey="id"
                size="small"
                pagination={false}
                dataSource={data.handovers}
                columns={[
                  { title: 'Số biên bản', dataIndex: 'code', width: 150 },
                  {
                    title: 'Ngày',
                    dataIndex: 'handoverDate',
                    width: 130,
                    render: (value: string) => formatDate(value),
                  },
                  { title: 'Số bản', dataIndex: 'totalItems', width: 100, align: 'right' },
                  {
                    title: 'Giá trị',
                    dataIndex: 'totalAmount',
                    width: 140,
                    align: 'right',
                    render: (value: number) => money(value),
                  },
                  {
                    title: '',
                    width: 60,
                    align: 'right',
                    render: (_, row) => (
                      <Tooltip title="In biên bản">
                        <Button
                          size="small"
                          icon={<PrinterOutlined />}
                          onClick={() => printHandover.mutate(row.code)}
                        />
                      </Tooltip>
                    ),
                  },
                ]}
              />
            )}
          </Card>
        </Space>
      )}

      {createItemsOpen && data && (
        <CreateItemsModal
          orderId={data.id}
          onClose={() => setCreateItemsOpen(false)}
          onDone={() => {
            setCreateItemsOpen(false);
            reload();
          }}
        />
      )}

      {quickCatalogLine && (
        <QuickCatalogModal
          line={quickCatalogLine}
          onClose={() => setQuickCatalogLine(null)}
          onDone={() => {
            setQuickCatalogLine(null);
            reload();
          }}
        />
      )}

      {handoverOpen && data && (
        <HandoverModal
          orderId={data.id}
          supplierName={data.supplierName}
          orderCode={data.code}
          onClose={() => setHandoverOpen(false)}
          onDone={() => {
            setHandoverOpen(false);
            reload();
          }}
        />
      )}
    </Drawer>
  );
}

function CreateItemsModal({
  orderId,
  onClose,
  onDone,
}: {
  orderId: string;
  onClose: () => void;
  onDone: () => void;
}) {
  const { message, modal } = App.useApp();
  const [form] = Form.useForm();

  const warehouses = useQuery({ queryKey: ['acq-warehouses', null], queryFn: () => locationsApi.warehouses() });
  const warehouseId = Form.useWatch('warehouseId', form) as string | undefined;

  const shelves = useQuery({
    queryKey: ['acq-shelves', warehouseId],
    queryFn: () => locationsApi.shelves(warehouseId),
    enabled: Boolean(warehouseId),
  });

  const create = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      purchaseApi.createItemsFromOrder(orderId, values),
    onSuccess: (result) => {
      message.success(`Đã tạo ${result.createdItems} ĐKCB.`);

      if (result.pendingCataloging.length > 0) {
        modal.warning({
          title: 'Còn dòng chưa biên mục',
          content: (
            <>
              <p>Các dòng sau chưa có biểu ghi nên chưa tạo được ĐKCB:</p>
              <ul>
                {result.pendingCataloging.map((title) => (
                  <li key={title}>{title}</li>
                ))}
              </ul>
              <p>Hãy biên mục sơ lược cho chúng rồi nhập kho lại.</p>
            </>
          ),
        });
      }

      onDone();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không tạo được ĐKCB.'),
  });

  return (
    <Modal
      open
      title="Nhập kho — tạo ĐKCB từ đơn đặt"
      onCancel={onClose}
      onOk={() => form.submit()}
      confirmLoading={create.isPending}
      okText="Tạo ĐKCB"
      cancelText="Bỏ qua"
      destroyOnHidden
    >
      <Typography.Paragraph type="secondary">
        Hệ thống tạo đúng số bản còn thiếu so với số đã ghi nhận nhận hàng, nên bấm hai lần cũng
        không sinh trùng.
      </Typography.Paragraph>

      <Form
        form={form}
        layout="vertical"
        initialValues={{ acquisitionType: 'Purchase', unlockImmediately: false }}
        onFinish={(values) => create.mutate(values)}
      >
        <Form.Item
          name="warehouseId"
          label="Kho nhập"
          rules={[{ required: true, message: 'Chưa chọn kho.' }]}
        >
          <Select
            options={(warehouses.data ?? [])
              .filter((item) => !item.isClosedForInventory)
              .map((item) => ({ value: item.id, label: item.name }))}
          />
        </Form.Item>
        <Form.Item name="shelfId" label="Giá">
          <Select
            allowClear
            disabled={!warehouseId}
            options={(shelves.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
          />
        </Form.Item>
        <Form.Item name="acquisitionType" label="Hình thức bổ sung">
          <Select
            options={[
              { value: 'Purchase', label: 'Mua' },
              { value: 'Donation', label: 'Biếu tặng' },
              { value: 'Exchange', label: 'Trao đổi' },
              { value: 'LegalDeposit', label: 'Lưu chiểu' },
            ]}
          />
        </Form.Item>
        <Form.Item
          name="unlockImmediately"
          label="Mở khóa ngay"
          extra="Bỏ qua bước kiểm nhận. Mặc định là chờ kiểm nhận."
        >
          <Select
            options={[
              { value: false, label: 'Không — chờ kiểm nhận' },
              { value: true, label: 'Có — cho lưu thông ngay' },
            ]}
          />
        </Form.Item>
      </Form>
    </Modal>
  );
}

/** III.2 — Biên mục sơ lược cho một dòng đơn đặt. */
function QuickCatalogModal({
  line,
  onClose,
  onDone,
}: {
  line: PurchaseOrderItemDto;
  onClose: () => void;
  onDone: () => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm();

  const documentTypes = useCatalogOptions('document-types');
  const languages = useCatalogOptions('languages');

  useEffect(() => {
    form.setFieldsValue({
      title: line.title,
      author: line.author,
      isbn: line.isbn,
      price: line.unitPrice,
      orderItemId: line.id,
      reuseDuplicate: true,
      itemQuantity: 0,
    });
  }, [form, line]);

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) => purchaseApi.quickCatalog(values),
    onSuccess: (result) => {
      message.success(
        result.reusedExisting
          ? `Thư viện đã có biểu ghi ${result.controlNumber}; hệ thống dùng lại biểu ghi đó.`
          : `Đã tạo biểu ghi ${result.controlNumber} và đưa vào hàng đợi biên mục chi tiết.`,
      );
      onDone();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  return (
    <Modal
      open
      width={720}
      title="Biên mục sơ lược"
      onCancel={onClose}
      onOk={() => form.submit()}
      confirmLoading={save.isPending}
      okText="Lưu biểu ghi"
      cancelText="Bỏ qua"
      destroyOnHidden
    >
      <Typography.Paragraph type="secondary">
        Mười trường nhưng lưu đúng cấu trúc MARC 21. Biểu ghi được đưa vào hàng đợi để cán bộ biên
        mục chi tiết sau, không phải gõ lại.
      </Typography.Paragraph>

      <Form form={form} layout="vertical" onFinish={(values) => save.mutate(values)}>
        <Form.Item name="orderItemId" hidden>
          <Input />
        </Form.Item>
        <Form.Item
          name="title"
          label="Nhan đề"
          rules={[{ required: true, message: 'Chưa nhập nhan đề.' }]}
        >
          <Input />
        </Form.Item>
        <Row gutter={12}>
          <Col span={12}>
            <Form.Item name="subTitle" label="Phụ đề">
              <Input />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item name="author" label="Tác giả">
              <Input />
            </Form.Item>
          </Col>
        </Row>
        <Row gutter={12}>
          <Col span={8}>
            <Form.Item name="publishPlace" label="Nơi xuất bản">
              <Input placeholder="Hà Nội" />
            </Form.Item>
          </Col>
          <Col span={10}>
            <Form.Item name="publisherName" label="Nhà xuất bản">
              <Input />
            </Form.Item>
          </Col>
          <Col span={6}>
            <Form.Item name="publishYear" label="Năm">
              <InputNumber min={1400} max={2200} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
        </Row>
        <Row gutter={12}>
          <Col span={8}>
            <Form.Item name="isbn" label="ISBN">
              <Input />
            </Form.Item>
          </Col>
          <Col span={5}>
            <Form.Item name="pages" label="Số trang">
              <InputNumber min={1} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={5}>
            <Form.Item name="ddc" label="Chỉ số DDC">
              <Input placeholder="005.74" />
            </Form.Item>
          </Col>
          <Col span={6}>
            <Form.Item name="price" label="Giá bìa">
              <InputNumber min={0} step={1000} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
        </Row>
        <Row gutter={12}>
          <Col span={12}>
            <Form.Item name="documentTypeId" label="Dạng tài liệu">
              <Select allowClear options={toOptions(documentTypes.data)} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item name="languageId" label="Ngôn ngữ">
              <Select allowClear options={toOptions(languages.data)} />
            </Form.Item>
          </Col>
        </Row>
        <Form.Item name="note" label="Ghi chú">
          <Input.TextArea rows={2} />
        </Form.Item>
        <Form.Item
          name="reuseDuplicate"
          label="Khi thư viện đã có tài liệu này"
          extra="Dùng lại biểu ghi cũ để OPAC không hiện hai kết quả cho một cuốn sách."
        >
          <Select
            options={[
              { value: true, label: 'Dùng lại biểu ghi đã có' },
              { value: false, label: 'Vẫn tạo biểu ghi mới' },
            ]}
          />
        </Form.Item>
      </Form>
    </Modal>
  );
}

function HandoverModal({
  orderId,
  orderCode,
  supplierName,
  onClose,
  onDone,
}: {
  orderId: string;
  orderCode: string;
  supplierName: string;
  onClose: () => void;
  onDone: () => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm();
  const [createdId, setCreatedId] = useState<string | null>(null);
  const [scan, setScan] = useState<File | null>(null);

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      purchaseApi.saveHandover(null, { ...values, orderId }),
    onSuccess: (id) => {
      setCreatedId(id);
      message.success('Đã lập biên bản bàn giao.');
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lập được biên bản.'),
  });

  const attach = useMutation({
    mutationFn: () => purchaseApi.attachHandoverScan(createdId!, scan!),
    onSuccess: () => {
      message.success('Đã đính kèm bản scan.');
      onDone();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không đính kèm được.'),
  });

  return (
    <Modal
      open
      title="Lập biên bản bàn giao"
      onCancel={onClose}
      onOk={() => (createdId ? (scan ? attach.mutate() : onDone()) : form.submit())}
      confirmLoading={save.isPending || attach.isPending}
      okText={createdId ? (scan ? 'Đính kèm và đóng' : 'Đóng') : 'Lập biên bản'}
      cancelText="Bỏ qua"
      destroyOnHidden
    >
      <Form
        form={form}
        layout="vertical"
        initialValues={{
          partyA: supplierName,
          content: `Bàn giao tài liệu theo đơn đặt ${orderCode}`,
        }}
        onFinish={(values) => save.mutate(values)}
      >
        <Form.Item
          name="partyA"
          label="Bên giao"
          rules={[{ required: true, message: 'Chưa nhập bên giao.' }]}
        >
          <Input />
        </Form.Item>
        <Form.Item
          name="partyB"
          label="Bên nhận"
          rules={[{ required: true, message: 'Chưa nhập bên nhận.' }]}
        >
          <Input placeholder="Thư viện Trường" />
        </Form.Item>
        <Form.Item name="content" label="Nội dung">
          <Input.TextArea rows={2} />
        </Form.Item>
        <Form.Item name="note" label="Ghi chú">
          <Input.TextArea rows={2} />
        </Form.Item>
      </Form>

      {createdId && (
        <>
          <Typography.Paragraph type="secondary">
            Biên bản đã lập. Có thể đính kèm bản scan sau khi hai bên ký.
          </Typography.Paragraph>
          <Upload
            accept=".pdf,.png,.jpg,.jpeg,.tif"
            maxCount={1}
            beforeUpload={(file) => {
              setScan(file);
              return false;
            }}
            onRemove={() => setScan(null)}
            fileList={scan ? [{ uid: '1', name: scan.name }] : []}
          >
            <Button icon={<UploadOutlined />}>Chọn bản scan đã ký</Button>
          </Upload>
        </>
      )}
    </Modal>
  );
}

function OrderEditorModal({
  open,
  suppliers,
  fundingSources,
  onClose,
  onDone,
}: {
  open: boolean;
  suppliers: Option[];
  fundingSources: Option[];
  onClose: () => void;
  onDone: (id: string) => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm();

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) => purchaseApi.saveOrder(null, values),
    onSuccess: (id) => {
      message.success('Đã lập đơn đặt.');
      form.resetFields();
      onDone(id);
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lập được đơn đặt.'),
  });

  return (
    <Modal
      open={open}
      width={860}
      title="Lập đơn đặt bằng tay"
      onCancel={onClose}
      onOk={() => form.submit()}
      confirmLoading={save.isPending}
      okText="Lập đơn"
      cancelText="Bỏ qua"
      destroyOnHidden
    >
      <Form
        form={form}
        layout="vertical"
        initialValues={{ items: [{ quantity: 1, unitPrice: 0 }] }}
        onFinish={(values) =>
          save.mutate({
            ...values,
            orderDate: (values.orderDate as Dayjs | undefined)?.format('YYYY-MM-DD'),
            expectedDate: (values.expectedDate as Dayjs | undefined)?.format('YYYY-MM-DD'),
          })
        }
      >
        <Row gutter={12}>
          <Col span={10}>
            <Form.Item
              name="supplierId"
              label="Nhà cung cấp"
              rules={[{ required: true, message: 'Chưa chọn nhà cung cấp.' }]}
            >
              <Select options={suppliers} />
            </Form.Item>
          </Col>
          <Col span={7}>
            <Form.Item name="orderDate" label="Ngày đặt">
              <DatePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={7}>
            <Form.Item name="expectedDate" label="Dự kiến giao">
              <DatePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
            </Form.Item>
          </Col>
        </Row>
        <Row gutter={12}>
          <Col span={12}>
            <Form.Item name="contractNo" label="Số hợp đồng">
              <Input />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item name="fundingSourceId" label="Nguồn kinh phí">
              <Select allowClear options={fundingSources} />
            </Form.Item>
          </Col>
        </Row>

        <Form.List name="items">
          {(fields, { add, remove }) => (
            <>
              <Table
                rowKey="key"
                size="small"
                pagination={false}
                dataSource={fields}
                columns={[
                  {
                    title: 'Nhan đề',
                    render: (_, field) => (
                      <Form.Item
                        name={[field.name, 'title']}
                        rules={[{ required: true, message: 'Chưa nhập nhan đề.' }]}
                        style={{ marginBottom: 0 }}
                      >
                        <Input />
                      </Form.Item>
                    ),
                  },
                  {
                    title: 'Tác giả',
                    width: 180,
                    render: (_, field) => (
                      <Form.Item name={[field.name, 'author']} style={{ marginBottom: 0 }}>
                        <Input />
                      </Form.Item>
                    ),
                  },
                  {
                    title: 'ISBN',
                    width: 160,
                    render: (_, field) => (
                      <Form.Item name={[field.name, 'isbn']} style={{ marginBottom: 0 }}>
                        <Input />
                      </Form.Item>
                    ),
                  },
                  {
                    title: 'SL',
                    width: 80,
                    render: (_, field) => (
                      <Form.Item name={[field.name, 'quantity']} style={{ marginBottom: 0 }}>
                        <InputNumber min={1} style={{ width: '100%' }} />
                      </Form.Item>
                    ),
                  },
                  {
                    title: 'Đơn giá',
                    width: 130,
                    render: (_, field) => (
                      <Form.Item name={[field.name, 'unitPrice']} style={{ marginBottom: 0 }}>
                        <InputNumber min={0} step={1000} style={{ width: '100%' }} />
                      </Form.Item>
                    ),
                  },
                  {
                    title: '',
                    width: 50,
                    render: (_, field) => (
                      <Button size="small" danger onClick={() => remove(field.name)}>
                        Xóa
                      </Button>
                    ),
                  },
                ]}
              />
              <Button
                type="dashed"
                block
                icon={<PlusOutlined />}
                style={{ marginTop: 8 }}
                onClick={() => add({ quantity: 1, unitPrice: 0 })}
              >
                Thêm dòng
              </Button>
            </>
          )}
        </Form.List>
      </Form>
    </Modal>
  );
}
