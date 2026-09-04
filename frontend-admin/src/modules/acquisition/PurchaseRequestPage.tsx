import { useEffect, useState } from 'react';
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
  Popconfirm,
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
  CheckOutlined,
  CloseOutlined,
  DeleteOutlined,
  DownloadOutlined,
  EditOutlined,
  ImportOutlined,
  PlusOutlined,
  SearchOutlined,
  SendOutlined,
  ShoppingCartOutlined,
} from '@ant-design/icons';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import dayjs, { type Dayjs } from 'dayjs';
import { frequencyLabels, issuesPerYear as issuesPerYearOf } from '@/modules/serials/labels';
import type { SerialFrequency } from '@/modules/serials/types';
import { requestLineAmount, subscriptionIssueCount, type RequestLineDraft } from './serialRequest';
import { PageHeader } from '@/components/PageHeader';
import { FilterBar } from '@/components/FilterBar';
import { Can } from '@/components/PermissionGate';
import { usePermission } from '@/hooks/usePermission';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { saveBlob } from '@/modules/marc/api';
import { useCatalogOptions, toOptions } from '@/modules/cataloging/useCatalogOptions';
import { purchaseApi } from './api';
import { formatDate, money, requestStatusColors, requestStatusLabels } from './labels';
import type {
  PurchaseDuplicateDto,
  PurchaseRequestDto,
  PurchaseRequestItemDto,
  PurchaseRequestStatus,
  PurchaseRequestType,
} from './types';

/**
 * III.1 — Yêu cầu đặt mua.
 *
 * Màn hình phục vụ hai vai khác nhau trên cùng một danh sách: người đề nghị lập và gửi duyệt, người
 * duyệt xem lại rồi quyết định từng dòng. Cảnh báo trùng đi kèm ngay trên dòng chứ không nằm ở một
 * chỗ khác, vì người sắp tiêu tiền là người cần thấy nó.
 */
export function PurchaseRequestPage() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const { can } = usePermission();

  const [filter, setFilter] = useState<Record<string, unknown>>({ page: 1, pageSize: 20 });
  const [draft, setDraft] = useState<Record<string, unknown>>({});
  const [editorId, setEditorId] = useState<string | null | undefined>(undefined);
  const [approvingId, setApprovingId] = useState<string | null>(null);
  const [importOpen, setImportOpen] = useState(false);
  const [orderOpen, setOrderOpen] = useState(false);
  const [selected, setSelected] = useState<string[]>([]);

  const suppliers = useCatalogOptions('suppliers');
  const fundingSources = useCatalogOptions('funding-sources');

  const requests = useQuery({
    queryKey: ['purchase-requests', filter],
    queryFn: () => purchaseApi.requests(filter),
    placeholderData: keepPreviousData,
  });

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['purchase-requests'] });
    void queryClient.invalidateQueries({ queryKey: ['purchase-orders'] });
  };

  const fail = (error: unknown) =>
    message.error(error instanceof ApiRequestError ? error.message : 'Không thực hiện được.');

  const submit = useMutation({
    mutationFn: (id: string) => purchaseApi.submitRequest(id),
    onSuccess: () => {
      message.success('Đã gửi yêu cầu đi duyệt.');
      refresh();
    },
    onError: fail,
  });

  const remove = useMutation({
    mutationFn: (id: string) => purchaseApi.deleteRequest(id),
    onSuccess: () => {
      message.success('Đã xóa yêu cầu.');
      refresh();
    },
    onError: fail,
  });

  const template = useMutation({
    mutationFn: () => purchaseApi.requestTemplate(),
    onSuccess: ({ blob, fileName }) => saveBlob(blob, fileName),
    onError: fail,
  });

  const columns: ColumnsType<PurchaseRequestDto> = [
    {
      title: 'Mã yêu cầu',
      dataIndex: 'code',
      width: 130,
      render: (value: string, row) => (
        <Button type="link" size="small" style={{ padding: 0 }} onClick={() => setEditorId(row.id)}>
          {value}
        </Button>
      ),
    },
    {
      title: 'Người đề nghị',
      dataIndex: 'requesterName',
      render: (value: string, row) => (
        <Space direction="vertical" size={0}>
          <span>{value}</span>
          {row.department && <Typography.Text type="secondary">{row.department}</Typography.Text>}
        </Space>
      ),
    },
    {
      title: 'Ngày đề nghị',
      dataIndex: 'requestDate',
      width: 120,
      render: (value: string) => formatDate(value),
    },
    {
      title: 'Số dòng',
      dataIndex: 'lineCount',
      width: 110,
      align: 'right',
      render: (value: number, row) => (
        <Space direction="vertical" size={0} align="end">
          <span>{value} đầu</span>
          <Typography.Text type="secondary">{row.totalQuantity} bản</Typography.Text>
        </Space>
      ),
    },
    {
      title: 'Đã có trong thư viện',
      dataIndex: 'duplicateCount',
      width: 150,
      align: 'center',
      render: (value: number) =>
        value > 0 ? <Tag color="warning">{value} dòng trùng</Tag> : <Typography.Text type="secondary">—</Typography.Text>,
    },
    {
      title: 'Giá trị đề nghị',
      dataIndex: 'totalAmount',
      width: 140,
      align: 'right',
      render: (value: number) => money(value),
    },
    {
      title: 'Giá trị duyệt',
      dataIndex: 'approvedAmount',
      width: 140,
      align: 'right',
      render: (value: number) => money(value),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      width: 160,
      render: (value: PurchaseRequestStatus, row) => (
        <Space direction="vertical" size={0}>
          <Tag color={requestStatusColors[value]}>{requestStatusLabels[value]}</Tag>
          {value === 'Submitted' && row.requiredLevels > 1 && (
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              Đã qua {row.approvalLevel}/{row.requiredLevels} cấp
            </Typography.Text>
          )}
        </Space>
      ),
    },
    {
      title: '',
      width: 150,
      align: 'right',
      render: (_, row) => (
        <Space>
          {row.status === 'Draft' && (
            <Can permission={PERMISSIONS.acquisition.requestSubmit}>
              <Tooltip title="Gửi duyệt">
                <Button
                  size="small"
                  icon={<SendOutlined />}
                  loading={submit.isPending}
                  onClick={() => submit.mutate(row.id)}
                />
              </Tooltip>
            </Can>
          )}
          {(row.status === 'Submitted' || row.status === 'PartiallyApproved') && (
            <Can permission={PERMISSIONS.acquisition.requestApprove}>
              <Tooltip title="Duyệt">
                <Button
                  size="small"
                  type="primary"
                  icon={<CheckOutlined />}
                  onClick={() => setApprovingId(row.id)}
                />
              </Tooltip>
            </Can>
          )}
          {row.status === 'Draft' && (
            <Can permission={PERMISSIONS.acquisition.requestUpdate}>
              <Tooltip title="Sửa">
                <Button size="small" icon={<EditOutlined />} onClick={() => setEditorId(row.id)} />
              </Tooltip>
            </Can>
          )}
          <Can permission={PERMISSIONS.acquisition.requestDelete}>
            <Popconfirm
              title="Xóa yêu cầu này?"
              okText="Xóa"
              cancelText="Bỏ qua"
              onConfirm={() => remove.mutate(row.id)}
            >
              <Button size="small" danger icon={<DeleteOutlined />} />
            </Popconfirm>
          </Can>
        </Space>
      ),
    },
  ];

  return (
    <div className="lc-page">
      <PageHeader
        title="Yêu cầu đặt mua"
        description="Đề nghị mua tài liệu, gửi duyệt, duyệt từng dòng rồi lập đơn đặt gửi nhà cung cấp."
        actions={
          <Space>
            <Can permission={PERMISSIONS.acquisition.requestImport}>
              <Button icon={<DownloadOutlined />} onClick={() => template.mutate()}>
                Tải tệp mẫu
              </Button>
            </Can>
            <Can permission={PERMISSIONS.acquisition.requestImport}>
              <Button icon={<ImportOutlined />} onClick={() => setImportOpen(true)}>
                Nhập từ Excel
              </Button>
            </Can>
            <Can permission={PERMISSIONS.acquisition.orderCreate}>
              <Button
                icon={<ShoppingCartOutlined />}
                disabled={selected.length === 0}
                onClick={() => setOrderOpen(true)}
              >
                Lập đơn đặt ({selected.length})
              </Button>
            </Can>
            <Can permission={PERMISSIONS.acquisition.requestCreate}>
              <Button type="primary" icon={<PlusOutlined />} onClick={() => setEditorId(null)}>
                Tạo yêu cầu
              </Button>
            </Can>
          </Space>
        }
      />

      <FilterBar
        loading={requests.isFetching}
        onSearch={() => setFilter({ ...draft, page: 1, pageSize: 20 })}
        onReset={() => {
          setDraft({});
          setFilter({ page: 1, pageSize: 20 });
        }}
      >
        <Input
          allowClear
          placeholder="Mã yêu cầu, người đề nghị, nhan đề"
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
          options={Object.entries(requestStatusLabels).map(([value, label]) => ({ value, label }))}
        />
        <Select
          allowClear
          placeholder="Loại"
          style={{ width: 160 }}
          value={draft.type as string | undefined}
          onChange={(value) => setDraft({ ...draft, type: value })}
          options={[
            { value: 'Monograph', label: 'Ấn phẩm đơn bản' },
            { value: 'Serial', label: 'Ấn phẩm định kỳ' },
          ]}
        />
        <Select
          allowClear
          placeholder="Nguồn kinh phí"
          style={{ width: 200 }}
          value={draft.fundingSourceId as string | undefined}
          onChange={(value) => setDraft({ ...draft, fundingSourceId: value })}
          options={toOptions(fundingSources.data)}
        />
        <DatePicker.RangePicker
          format="DD/MM/YYYY"
          placeholder={['Từ ngày', 'đến ngày']}
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
          loading={requests.isFetching}
          columns={columns}
          dataSource={requests.data?.items ?? []}
          scroll={{ x: 1300 }}
          rowSelection={{
            selectedRowKeys: selected,
            onChange: (keys) => setSelected(keys as string[]),
            getCheckboxProps: (row) => ({
              // Chỉ yêu cầu đã duyệt mới lập được đơn đặt.
              disabled: row.status !== 'Approved' && row.status !== 'PartiallyApproved',
            }),
          }}
          pagination={{
            current: requests.data?.page ?? 1,
            pageSize: requests.data?.pageSize ?? 20,
            total: requests.data?.totalCount ?? 0,
            showSizeChanger: true,
            showTotal: (total) => `Tổng ${total} yêu cầu`,
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

      {editorId !== undefined && (
        <RequestEditorDrawer
          id={editorId}
          suppliers={toOptions(suppliers.data)}
          fundingSources={toOptions(fundingSources.data)}
          canEdit={can(PERMISSIONS.acquisition.requestUpdate) || can(PERMISSIONS.acquisition.requestCreate)}
          onClose={() => setEditorId(undefined)}
          onSaved={refresh}
        />
      )}

      {approvingId && (
        <ApprovalModal
          id={approvingId}
          onClose={() => setApprovingId(null)}
          onDone={refresh}
        />
      )}

      <ImportModal
        open={importOpen}
        onClose={() => setImportOpen(false)}
        onDone={(requestId) => {
          refresh();
          setImportOpen(false);
          setEditorId(requestId);
        }}
      />

      <CreateOrderModal
        open={orderOpen}
        requestIds={selected}
        suppliers={toOptions(suppliers.data)}
        fundingSources={toOptions(fundingSources.data)}
        onClose={() => setOrderOpen(false)}
        onDone={() => {
          setOrderOpen(false);
          setSelected([]);
          refresh();
        }}
      />
    </div>
  );
}

interface Option {
  value: string;
  label: string;
}

/** Form lập / sửa yêu cầu, kèm nút tra nhanh xem thư viện đã có tài liệu chưa. */
function RequestEditorDrawer({
  id,
  suppliers,
  fundingSources,
  canEdit,
  onClose,
  onSaved,
}: {
  id: string | null;
  suppliers: Option[];
  fundingSources: Option[];
  canEdit: boolean;
  onClose: () => void;
  onSaved: () => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm();
  const [duplicate, setDuplicate] = useState<{ index: number; match: PurchaseDuplicateDto } | null>(null);

  const detail = useQuery({
    queryKey: ['purchase-request', id],
    queryFn: () => purchaseApi.request(id!),
    enabled: Boolean(id),
  });

  const loaded = detail.data;
  const readOnly = Boolean(loaded && loaded.status !== 'Draft');

  // Loại yêu cầu và các dòng đang gõ — để đổi cột bảng và tính thành tiền ngay khi gõ.
  const type = (Form.useWatch('type', form) as PurchaseRequestType | undefined) ?? 'Monograph';
  const draftLines = (Form.useWatch('items', form) as EditorLine[] | undefined) ?? [];
  const isSerial = type === 'Serial';

  useEffect(() => {
    if (!loaded) return;

    form.setFieldsValue({
      type: loaded.type,
      requesterName: loaded.requesterName,
      department: loaded.department,
      fundingSourceId: loaded.fundingSourceId,
      reason: loaded.reason,
      items: loaded.items.map((item) => ({
        ...item,
        subscription:
          item.subscriptionFrom && item.subscriptionTo
            ? [dayjs(item.subscriptionFrom), dayjs(item.subscriptionTo)]
            : undefined,
      })),
    });
  }, [form, loaded]);

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      purchaseApi.saveRequest(id, {
        ...values,
        items: ((values.items as EditorLine[] | undefined) ?? []).map(toLineInput),
      }),
    onSuccess: () => {
      message.success('Đã lưu yêu cầu.');
      onSaved();
      onClose();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  const check = useMutation({
    mutationFn: ({ isbn, title }: { isbn?: string; title?: string; index: number }) =>
      purchaseApi.checkDuplicate(isbn, title),
    onSuccess: (match, variables) => {
      if (match) {
        setDuplicate({ index: variables.index, match });
      } else {
        message.success('Thư viện chưa có tài liệu này.');
        setDuplicate(null);
      }
    },
  });

  return (
    <Drawer
      open
      width={960}
      title={loaded ? `Yêu cầu ${loaded.code}` : 'Tạo yêu cầu đặt mua'}
      onClose={onClose}
      extra={
        !readOnly && (
          <Button type="primary" loading={save.isPending} disabled={!canEdit} onClick={() => form.submit()}>
            Lưu
          </Button>
        )
      }
    >
      {readOnly && loaded && (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message={`Yêu cầu đang ở trạng thái "${requestStatusLabels[loaded.status]}" nên chỉ xem.`}
          description={
            loaded.orderCodes.length > 0
              ? `Đã sinh ra đơn đặt: ${loaded.orderCodes.join(', ')}`
              : loaded.rejectReason
                ? `Lý do từ chối: ${loaded.rejectReason}`
                : undefined
          }
        />
      )}

      {duplicate && (
        <Alert
          type="warning"
          showIcon
          closable
          onClose={() => setDuplicate(null)}
          style={{ marginBottom: 12 }}
          message={`Thư viện đã có "${duplicate.match.title}" (khớp theo ${duplicate.match.matchedBy})`}
          description={`Số kiểm soát ${duplicate.match.controlNumber}, hiện có ${duplicate.match.itemCount} bản, sẵn sàng ${duplicate.match.availableItemCount} bản.`}
        />
      )}

      <Form
        form={form}
        layout="vertical"
        disabled={readOnly}
        initialValues={{ type: 'Monograph', items: [{ quantity: 1, unitPrice: 0 }] }}
        onFinish={(values) => save.mutate(values)}
      >
        <Row gutter={12}>
          <Col span={6}>
            <Form.Item name="type" label="Loại yêu cầu">
              <Select
                options={[
                  { value: 'Monograph', label: 'Ấn phẩm đơn bản' },
                  { value: 'Serial', label: 'Ấn phẩm định kỳ' },
                ]}
              />
            </Form.Item>
          </Col>
          <Col span={9}>
            <Form.Item
              name="requesterName"
              label="Người đề nghị"
              rules={[{ required: true, message: 'Chưa nhập người đề nghị.' }]}
            >
              <Input placeholder="Khoa Công nghệ thông tin" />
            </Form.Item>
          </Col>
          <Col span={9}>
            <Form.Item name="department" label="Đơn vị">
              <Input placeholder="Khoa CNTT" />
            </Form.Item>
          </Col>
        </Row>

        <Row gutter={12}>
          <Col span={8}>
            <Form.Item name="fundingSourceId" label="Nguồn kinh phí dự kiến">
              <Select allowClear options={fundingSources} />
            </Form.Item>
          </Col>
          <Col span={16}>
            <Form.Item name="reason" label="Lý do đề nghị">
              <Input placeholder="Bổ sung giáo trình học kỳ I năm học 2026–2027" />
            </Form.Item>
          </Col>
        </Row>

        <Typography.Title level={5}>
          {isSerial ? 'Danh sách báo, tạp chí đề nghị đặt' : 'Danh sách tài liệu đề nghị'}
        </Typography.Title>

        {isSerial && (
          <Typography.Paragraph type="secondary">
            Đơn giá khai theo một kỳ; số kỳ suy ra từ thời gian đặt và số kỳ mỗi năm, thành tiền là
            số bản × số kỳ × đơn giá kỳ.
          </Typography.Paragraph>
        )}

        <Form.List name="items">
          {(fields, { add, remove }) => {
            const titleColumn = {
              title: isSerial ? 'Tên báo, tạp chí' : 'Nhan đề',
              width: 260,
              render: (_: unknown, field: { name: number }) => (
                <>
                  <Form.Item name={[field.name, 'id']} hidden>
                    <Input />
                  </Form.Item>
                  <Form.Item
                    name={[field.name, 'title']}
                    rules={[{ required: true, message: 'Chưa nhập nhan đề.' }]}
                    style={{ marginBottom: 0 }}
                  >
                    <Input placeholder={isSerial ? 'Tạp chí Khoa học và Công nghệ' : 'Giáo trình cơ sở dữ liệu'} />
                  </Form.Item>
                </>
              ),
            };

            const quantityColumn = {
              title: isSerial ? 'Số bản/kỳ' : 'SL',
              width: isSerial ? 100 : 80,
              render: (_: unknown, field: { name: number }) => (
                <Form.Item
                  name={[field.name, 'quantity']}
                  rules={[{ required: true, message: '' }]}
                  style={{ marginBottom: 0 }}
                >
                  <InputNumber min={1} max={10000} style={{ width: '100%' }} />
                </Form.Item>
              ),
            };

            const priceColumn = {
              title: isSerial ? 'Đơn giá/kỳ' : 'Đơn giá',
              width: 130,
              render: (_: unknown, field: { name: number }) => (
                <Form.Item name={[field.name, 'unitPrice']} style={{ marginBottom: 0 }}>
                  <InputNumber<number>
                    min={0}
                    step={1000}
                    style={{ width: '100%' }}
                    formatter={(value) => money(Number(value ?? 0))}
                    parser={(value) => Number((value ?? '').replace(/\D/g, ''))}
                  />
                </Form.Item>
              ),
            };

            const supplierColumn = {
              title: 'Nhà cung cấp',
              width: 190,
              render: (_: unknown, field: { name: number }) => (
                <Form.Item name={[field.name, 'supplierId']} style={{ marginBottom: 0 }}>
                  <Select allowClear options={suppliers} />
                </Form.Item>
              ),
            };

            const actionColumn = {
              title: '',
              width: 90,
              render: (_: unknown, field: { name: number }) => (
                <Space>
                  {!isSerial && (
                    <Tooltip title="Thư viện đã có tài liệu này chưa?">
                      <Button
                        size="small"
                        icon={<SearchOutlined />}
                        loading={check.isPending}
                        onClick={() => {
                          const line = form.getFieldValue(['items', field.name]) ?? {};
                          check.mutate({
                            isbn: line.isbn,
                            title: line.title,
                            index: field.name,
                          });
                        }}
                      />
                    </Tooltip>
                  )}
                  <Button
                    size="small"
                    danger
                    icon={<CloseOutlined />}
                    onClick={() => remove(field.name)}
                  />
                </Space>
              ),
            };

            const monographColumns = [
              titleColumn,
              {
                title: 'Tác giả',
                width: 170,
                render: (_: unknown, field: { name: number }) => (
                  <Form.Item name={[field.name, 'author']} style={{ marginBottom: 0 }}>
                    <Input />
                  </Form.Item>
                ),
              },
              {
                title: 'Nhà xuất bản',
                width: 170,
                render: (_: unknown, field: { name: number }) => (
                  <Form.Item name={[field.name, 'publisherName']} style={{ marginBottom: 0 }}>
                    <Input />
                  </Form.Item>
                ),
              },
              {
                title: 'Năm',
                width: 90,
                render: (_: unknown, field: { name: number }) => (
                  <Form.Item name={[field.name, 'publishYear']} style={{ marginBottom: 0 }}>
                    <InputNumber min={1400} max={2200} style={{ width: '100%' }} />
                  </Form.Item>
                ),
              },
              {
                title: 'ISBN',
                width: 160,
                render: (_: unknown, field: { name: number }) => (
                  <Form.Item name={[field.name, 'isbn']} style={{ marginBottom: 0 }}>
                    <Input />
                  </Form.Item>
                ),
              },
              quantityColumn,
              priceColumn,
              supplierColumn,
              actionColumn,
            ];

            const serialColumns = [
              titleColumn,
              {
                title: 'ISSN',
                width: 120,
                render: (_: unknown, field: { name: number }) => (
                  <Form.Item name={[field.name, 'issn']} style={{ marginBottom: 0 }}>
                    <Input placeholder="1859-1450" />
                  </Form.Item>
                ),
              },
              {
                title: 'Kỳ hạn',
                width: 150,
                render: (_: unknown, field: { name: number }) => (
                  <Form.Item name={[field.name, 'frequency']} style={{ marginBottom: 0 }}>
                    <Select
                      allowClear
                      options={Object.entries(frequencyLabels).map(([value, label]) => ({ value, label }))}
                      onChange={(value: SerialFrequency | undefined) => {
                        // Kỳ hạn quyết định số kỳ mỗi năm; điền sẵn để không phải nhớ 52 hay 12.
                        if (value && issuesPerYearOf[value] > 0) {
                          form.setFieldValue(['items', field.name, 'issuesPerYear'], issuesPerYearOf[value]);
                        }
                      }}
                    />
                  </Form.Item>
                ),
              },
              {
                title: 'Số kỳ/năm',
                width: 100,
                render: (_: unknown, field: { name: number }) => (
                  <Form.Item
                    name={[field.name, 'issuesPerYear']}
                    rules={[{ required: true, message: 'Chưa có số kỳ/năm.' }]}
                    style={{ marginBottom: 0 }}
                  >
                    <InputNumber min={1} max={366} style={{ width: '100%' }} />
                  </Form.Item>
                ),
              },
              {
                title: 'Thời gian đặt',
                width: 230,
                render: (_: unknown, field: { name: number }) => (
                  <Form.Item
                    name={[field.name, 'subscription']}
                    rules={[{ required: true, message: 'Chưa chọn thời gian đặt.' }]}
                    style={{ marginBottom: 0 }}
                  >
                    <DatePicker.RangePicker
                      picker="month"
                      format="MM/YYYY"
                      placeholder={['Từ tháng', 'đến tháng']}
                      style={{ width: '100%' }}
                    />
                  </Form.Item>
                ),
              },
              quantityColumn,
              priceColumn,
              {
                title: 'Số kỳ × thành tiền',
                width: 170,
                align: 'right' as const,
                render: (_: unknown, field: { name: number }) => {
                  const line = toLineInput(draftLines[field.name] ?? {});

                  return (
                    <Space direction="vertical" size={0} style={{ alignItems: 'flex-end' }}>
                      <Typography.Text type="secondary">
                        {line.issueCount} kỳ
                      </Typography.Text>
                      <Typography.Text strong>{money(requestLineAmount('Serial', line))}</Typography.Text>
                    </Space>
                  );
                },
              },
              supplierColumn,
              actionColumn,
            ];

            return (
              <>
                <Table
                  rowKey="key"
                  size="small"
                  pagination={false}
                  dataSource={fields}
                  scroll={{ x: isSerial ? 1440 : 1100 }}
                  columns={isSerial ? serialColumns : monographColumns}
                />

                <Space style={{ marginTop: 8, width: '100%', justifyContent: 'space-between' }}>
                  <Button
                    type="dashed"
                    icon={<PlusOutlined />}
                    onClick={() => add({ quantity: 1, unitPrice: 0 })}
                  >
                    {isSerial ? 'Thêm đầu báo, tạp chí' : 'Thêm đầu tài liệu'}
                  </Button>
                  <Typography.Text strong>
                    Tổng tiền dự kiến:{' '}
                    {money(
                      draftLines.reduce(
                        (sum, line) => sum + requestLineAmount(type, toLineInput(line ?? {})),
                        0,
                      ),
                    )}{' '}
                    đ
                  </Typography.Text>
                </Space>
              </>
            );
          }}
        </Form.List>
      </Form>

      {loaded && loaded.items.some((item) => item.isDuplicate) && (
        <Alert
          type="warning"
          showIcon
          style={{ marginTop: 12 }}
          message={`${loaded.items.filter((item) => item.isDuplicate).length} dòng trùng với tài liệu thư viện đã có`}
          description={loaded.items
            .filter((item) => item.isDuplicate)
            .map((item) => `${item.title} — hiện có ${item.existingCopies} bản`)
            .join('; ')}
        />
      )}
    </Drawer>
  );
}

/** Một dòng trên form soạn yêu cầu: giống dòng gửi máy chủ, riêng thời gian đặt là một cặp tháng. */
interface EditorLine extends RequestLineDraft {
  id?: string | null;
  title?: string;
  author?: string | null;
  publisherName?: string | null;
  publishYear?: number | null;
  isbn?: string | null;
  issn?: string | null;
  supplierId?: string | null;
  note?: string | null;
  frequency?: SerialFrequency | null;
  subscription?: [Dayjs | null, Dayjs | null] | null;
}

/**
 * Đổi dòng trên form thành dòng máy chủ nhận: cặp tháng thành ngày đầu tháng bắt đầu và ngày đầu
 * tháng kết thúc — máy chủ đếm số kỳ theo tháng nên ngày trong tháng không ảnh hưởng.
 */
function toLineInput(line: EditorLine) {
  const { subscription, ...rest } = line;
  const subscriptionFrom = subscription?.[0]?.startOf('month').format('YYYY-MM-DD') ?? null;
  const subscriptionTo = subscription?.[1]?.startOf('month').format('YYYY-MM-DD') ?? null;

  return {
    ...rest,
    subscriptionFrom,
    subscriptionTo,
    issueCount: subscriptionIssueCount(subscriptionFrom, subscriptionTo, rest.issuesPerYear),
  };
}

/** Duyệt yêu cầu: sửa được số lượng từng dòng trước khi chốt. */
function ApprovalModal({
  id,
  onClose,
  onDone,
}: {
  id: string;
  onClose: () => void;
  onDone: () => void;
}) {
  const { message } = App.useApp();
  const [lines, setLines] = useState<Record<string, number>>({});
  const [note, setNote] = useState('');
  const [rejectOpen, setRejectOpen] = useState(false);
  const [rejectReason, setRejectReason] = useState('');

  const detail = useQuery({
    queryKey: ['purchase-request', id],
    queryFn: () => purchaseApi.request(id),
  });

  const approve = useMutation({
    mutationFn: () =>
      purchaseApi.approveRequest(id, {
        lines: Object.entries(lines).map(([itemId, approvedQuantity]) => ({
          itemId,
          approvedQuantity,
        })),
        note,
      }),
    onSuccess: (status) => {
      message.success(`Yêu cầu chuyển sang trạng thái ${requestStatusLabels[status as PurchaseRequestStatus] ?? status}.`);
      onDone();
      onClose();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không duyệt được.'),
  });

  const reject = useMutation({
    mutationFn: () => purchaseApi.rejectRequest(id, rejectReason),
    onSuccess: () => {
      message.success('Đã từ chối yêu cầu.');
      onDone();
      onClose();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không từ chối được.'),
  });

  const approvedFor = (item: PurchaseRequestItemDto) => lines[item.id] ?? item.quantity;

  // Báo, tạp chí: giá trị duyệt nhân thêm số kỳ trong thời gian đặt, như máy chủ tính.
  const lineTotal = (item: PurchaseRequestItemDto) =>
    approvedFor(item) * (item.issueCount || 1) * item.unitPrice;

  const total = (detail.data?.items ?? []).reduce((sum, item) => sum + lineTotal(item), 0);
  const isSerial = detail.data?.type === 'Serial';

  return (
    <Modal
      open
      width={900}
      title={detail.data ? `Duyệt yêu cầu ${detail.data.code}` : 'Duyệt yêu cầu'}
      onCancel={onClose}
      footer={[
        <Button key="cancel" onClick={onClose}>
          Đóng
        </Button>,
        <Button key="reject" danger onClick={() => setRejectOpen(true)}>
          Từ chối
        </Button>,
        <Button key="approve" type="primary" loading={approve.isPending} onClick={() => approve.mutate()}>
          Duyệt
        </Button>,
      ]}
    >
      {detail.data && (
        <Space direction="vertical" size="middle" style={{ width: '100%' }}>
          {detail.data.requiredLevels > 1 && (
            <Alert
              type="info"
              showIcon
              message={`Quy trình duyệt ${detail.data.requiredLevels} cấp — yêu cầu này đã qua ${detail.data.approvalLevel} cấp.`}
            />
          )}

          <Table
            rowKey="id"
            size="small"
            pagination={false}
            dataSource={detail.data.items}
            columns={[
              {
                title: 'Nhan đề',
                dataIndex: 'title',
                render: (value: string, row: PurchaseRequestItemDto) => (
                  <Space direction="vertical" size={0}>
                    <span>{value}</span>
                    {row.isDuplicate && (
                      <Tag color="warning">Thư viện đã có {row.existingCopies} bản</Tag>
                    )}
                  </Space>
                ),
              },
              isSerial
                ? {
                    title: 'Thời gian đặt',
                    width: 200,
                    render: (_: unknown, row: PurchaseRequestItemDto) => (
                      <Space direction="vertical" size={0}>
                        <span>
                          {row.subscriptionFrom && row.subscriptionTo
                            ? `${dayjs(row.subscriptionFrom).format('MM/YYYY')} – ${dayjs(row.subscriptionTo).format('MM/YYYY')}`
                            : '—'}
                        </span>
                        <Typography.Text type="secondary">
                          {row.frequency ? frequencyLabels[row.frequency as SerialFrequency] : ''}
                          {row.issueCount ? ` · ${row.issueCount} kỳ` : ''}
                        </Typography.Text>
                      </Space>
                    ),
                  }
                : { title: 'Tác giả', dataIndex: 'author', width: 150 },
              { title: isSerial ? 'Bản/kỳ' : 'Đề nghị', dataIndex: 'quantity', width: 90, align: 'right' },
              {
                title: 'Duyệt',
                width: 110,
                render: (_, row: PurchaseRequestItemDto) => (
                  <InputNumber
                    min={0}
                    max={row.quantity}
                    value={approvedFor(row)}
                    onChange={(value) => setLines({ ...lines, [row.id]: value ?? 0 })}
                    style={{ width: '100%' }}
                  />
                ),
              },
              {
                title: isSerial ? 'Đơn giá/kỳ' : 'Đơn giá',
                dataIndex: 'unitPrice',
                width: 120,
                align: 'right',
                render: (value: number) => money(value),
              },
              {
                title: 'Thành tiền duyệt',
                width: 140,
                align: 'right',
                render: (_, row: PurchaseRequestItemDto) => money(lineTotal(row)),
              },
            ]}
            summary={() => (
              <Table.Summary.Row>
                <Table.Summary.Cell index={0} colSpan={5}>
                  <Typography.Text strong>Tổng giá trị duyệt</Typography.Text>
                </Table.Summary.Cell>
                <Table.Summary.Cell index={5} align="right">
                  <Typography.Text strong>{money(total)}</Typography.Text>
                </Table.Summary.Cell>
              </Table.Summary.Row>
            )}
          />

          <Input.TextArea
            rows={2}
            placeholder="Ý kiến duyệt"
            value={note}
            onChange={(event) => setNote(event.target.value)}
          />
        </Space>
      )}

      <Modal
        open={rejectOpen}
        title="Từ chối yêu cầu"
        onCancel={() => setRejectOpen(false)}
        onOk={() => reject.mutate()}
        confirmLoading={reject.isPending}
        okText="Từ chối"
        cancelText="Bỏ qua"
        okButtonProps={{ danger: true, disabled: rejectReason.trim().length === 0 }}
      >
        <Input.TextArea
          rows={3}
          placeholder="Lý do từ chối — người đề nghị sẽ đọc dòng này."
          value={rejectReason}
          onChange={(event) => setRejectReason(event.target.value)}
        />
      </Modal>
    </Modal>
  );
}

/** Nhập danh sách đề nghị từ tệp Excel. */
function ImportModal({
  open,
  onClose,
  onDone,
}: {
  open: boolean;
  onClose: () => void;
  onDone: (requestId: string) => void;
}) {
  const { message } = App.useApp();
  const [file, setFile] = useState<File | null>(null);

  const upload = useMutation({
    mutationFn: () => purchaseApi.importRequestLines(file!),
    onSuccess: (result) => {
      const errors = result.errors.length;

      message.success(
        `Đã nhập ${result.imported} dòng${errors > 0 ? `, ${errors} dòng có vấn đề` : ''}.`,
      );

      if (errors > 0) {
        Modal.warning({
          title: 'Các dòng có vấn đề',
          width: 620,
          content: (
            <Table
              rowKey="rowNumber"
              size="small"
              pagination={false}
              scroll={{ y: 260 }}
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
      onDone(result.requestId);
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không nhập được.'),
  });

  return (
    <Modal
      open={open}
      title="Nhập danh sách đề nghị mua từ Excel"
      onCancel={onClose}
      onOk={() => upload.mutate()}
      confirmLoading={upload.isPending}
      okButtonProps={{ disabled: !file }}
      okText="Nhập"
      cancelText="Bỏ qua"
    >
      <Typography.Paragraph type="secondary">
        Tệp cần có cột <b>Nhan đề</b>. Các cột còn lại theo tệp mẫu; nhà cung cấp so theo tên trong
        danh mục, không phân biệt dấu. Hệ thống tạo một yêu cầu nháp mới từ tệp này.
      </Typography.Paragraph>

      <Upload.Dragger
        accept=".xlsx,.xls"
        maxCount={1}
        beforeUpload={(selected) => {
          setFile(selected);
          return false;
        }}
        onRemove={() => setFile(null)}
        fileList={file ? [{ uid: '1', name: file.name }] : []}
      >
        <p className="ant-upload-drag-icon">
          <ImportOutlined />
        </p>
        <p className="ant-upload-text">Kéo tệp Excel vào đây hoặc bấm để chọn</p>
      </Upload.Dragger>
    </Modal>
  );
}

/** Lập đơn đặt từ các yêu cầu đã duyệt. */
function CreateOrderModal({
  open,
  requestIds,
  suppliers,
  fundingSources,
  onClose,
  onDone,
}: {
  open: boolean;
  requestIds: string[];
  suppliers: Option[];
  fundingSources: Option[];
  onClose: () => void;
  onDone: () => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm();

  const create = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      purchaseApi.createOrdersFromRequests({ ...values, requestIds }),
    onSuccess: (ids) => {
      message.success(`Đã lập ${ids.length} đơn đặt, nhóm theo nhà cung cấp.`);
      onDone();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lập được đơn đặt.'),
  });

  return (
    <Modal
      open={open}
      title={`Lập đơn đặt từ ${requestIds.length} yêu cầu`}
      onCancel={onClose}
      onOk={() => form.submit()}
      confirmLoading={create.isPending}
      okText="Lập đơn"
      cancelText="Bỏ qua"
      destroyOnHidden
    >
      <Typography.Paragraph type="secondary">
        Các dòng đã duyệt được gom lại và tách thành một đơn cho mỗi nhà cung cấp. Dòng đã nằm trong
        một đơn đặt trước đó sẽ được bỏ qua.
      </Typography.Paragraph>

      <Form
        form={form}
        layout="vertical"
        onFinish={(values) =>
          create.mutate({
            ...values,
            expectedDate: (values.expectedDate as Dayjs | undefined)?.format('YYYY-MM-DD'),
          })
        }
      >
        <Form.Item
          name="defaultSupplierId"
          label="Nhà cung cấp cho các dòng chưa gợi ý"
          extra="Chỉ áp dụng cho những dòng người đề nghị bỏ trống nhà cung cấp."
        >
          <Select allowClear options={suppliers} />
        </Form.Item>
        <Form.Item name="fundingSourceId" label="Nguồn kinh phí">
          <Select allowClear options={fundingSources} />
        </Form.Item>
        <Form.Item name="contractNo" label="Số hợp đồng">
          <Input />
        </Form.Item>
        <Form.Item name="expectedDate" label="Ngày dự kiến giao">
          <DatePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
        </Form.Item>
        <Form.Item name="note" label="Ghi chú">
          <Input.TextArea rows={2} />
        </Form.Item>
      </Form>
    </Modal>
  );
}
