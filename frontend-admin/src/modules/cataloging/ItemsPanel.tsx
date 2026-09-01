import { useState } from 'react';
import {
  App,
  Button,
  Drawer,
  Empty,
  Form,
  Input,
  InputNumber,
  Popconfirm,
  Select,
  Space,
  Switch,
  Table,
  Tag,
  Typography,
} from 'antd';
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { applyApiError, errorMessage } from '@/api/formErrors';
import { catalogingApi, locationsApi, type CreateItemsPayload } from './api';
import { useCatalogOptions, toOptions } from './useCatalogOptions';
import { ACQUISITION_TYPE_LABELS, ITEM_STATUS_LABELS, type Item } from './types';

const MONOSPACE = { fontFamily: 'ui-monospace, Consolas, monospace' } as const;

/**
 * Đăng ký cá biệt của một biểu ghi (II.2).
 *
 * Copies are created in a batch because that is how they arrive: a library buys five of the same
 * textbook, and typing five near-identical forms would be five chances to make a mistake. Barcodes,
 * register numbers and the shelf mark are generated, so the form only asks for what actually differs
 * between one purchase and the next.
 */
export function ItemsPanel({ bibId }: { bibId: string }) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);

  const items = useQuery({
    queryKey: ['bib-items', bibId],
    queryFn: () => catalogingApi.items(bibId),
  });

  const remove = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => catalogingApi.deleteItem(id, reason),
    onSuccess: async () => {
      message.success('Đã xóa đăng ký cá biệt.');
      await refresh();
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const refresh = async () => {
    await queryClient.invalidateQueries({ queryKey: ['bib-items', bibId] });
    await queryClient.invalidateQueries({ queryKey: ['bib-record', bibId] });
  };

  return (
    <Space direction="vertical" size={12} style={{ width: '100%' }}>
      <Space style={{ justifyContent: 'space-between', width: '100%' }}>
        <Typography.Text type="secondary">
          Mã vạch, số đăng ký cá biệt và ký hiệu xếp giá do hệ thống sinh theo quy tắc đã cấu hình.
        </Typography.Text>

        <Can permission={PERMISSIONS.acquisition.itemCreate}>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setOpen(true)}>
            Thêm bản sách
          </Button>
        </Can>
      </Space>

      {!items.isLoading && (items.data ?? []).length === 0 ? (
        <Empty description="Biểu ghi chưa có đăng ký cá biệt nào" />
      ) : (
        <Table<Item>
          rowKey="id"
          size="small"
          loading={items.isLoading}
          dataSource={items.data ?? []}
          pagination={false}
          columns={[
            { title: 'Bản', dataIndex: 'copyNumber', width: 60, align: 'right' },
            {
              title: 'Mã vạch',
              dataIndex: 'barcode',
              width: 150,
              render: (value: string) => <span style={MONOSPACE}>{value}</span>,
            },
            {
              title: 'Số ĐKCB',
              dataIndex: 'registerNumber',
              width: 150,
              render: (value: string) => <span style={MONOSPACE}>{value}</span>,
            },
            { title: 'Kho', dataIndex: 'warehouseName', width: 160 },
            {
              title: 'Vị trí giá',
              dataIndex: 'shelfName',
              width: 130,
              render: (value?: string | null) =>
                value ?? <Typography.Text type="secondary">Chưa xếp</Typography.Text>,
            },
            {
              title: 'Ký hiệu xếp giá',
              dataIndex: 'callNumber',
              width: 160,
              render: (value?: string) => <span style={MONOSPACE}>{value}</span>,
            },
            {
              title: 'Trạng thái',
              dataIndex: 'status',
              width: 150,
              render: (value: Item['status'], row) => (
                <Space size={4} direction="vertical">
                  <Tag color={value === 'InStock' ? 'green' : value === 'OnLoan' ? 'blue' : 'default'}>
                    {ITEM_STATUS_LABELS[value] ?? value}
                  </Tag>
                  {row.isLocked && <Tag color="orange">{row.lockReason ?? 'Đang khóa'}</Tag>}
                </Space>
              ),
            },
            {
              title: 'Đơn giá',
              dataIndex: 'price',
              width: 120,
              align: 'right',
              render: (value: number) => value.toLocaleString('vi-VN'),
            },
            { title: 'Lượt mượn', dataIndex: 'loanCount', width: 100, align: 'right' },
            {
              title: '',
              width: 60,
              align: 'right',
              render: (_, row) => (
                <Can permission={PERMISSIONS.acquisition.itemDelete}>
                  <Popconfirm
                    title={`Xóa bản sách ${row.barcode}?`}
                    description="Bản sách được giữ lại trong cơ sở dữ liệu và không còn tham gia lưu thông."
                    okText="Xóa"
                    cancelText="Không"
                    onConfirm={() => remove.mutate({ id: row.id, reason: 'Xóa từ màn hình biên mục' })}
                  >
                    <Button type="text" danger icon={<DeleteOutlined />} />
                  </Popconfirm>
                </Can>
              ),
            },
          ]}
        />
      )}

      <CreateItemsDrawer
        bibId={bibId}
        open={open}
        onClose={() => setOpen(false)}
        onCreated={async () => {
          setOpen(false);
          await refresh();
        }}
      />
    </Space>
  );
}

function CreateItemsDrawer({
  bibId,
  open,
  onClose,
  onCreated,
}: {
  bibId: string;
  open: boolean;
  onClose: () => void;
  onCreated: () => void | Promise<void>;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm<CreateItemsPayload>();
  const warehouseId = Form.useWatch('warehouseId', form);

  const warehouses = useQuery({
    queryKey: ['warehouses'],
    queryFn: () => locationsApi.warehouses(),
    enabled: open,
  });

  const shelves = useQuery({
    queryKey: ['shelves', warehouseId],
    queryFn: () => locationsApi.shelves(warehouseId),
    enabled: open && Boolean(warehouseId),
  });

  const fundingSources = useCatalogOptions('funding-sources', open);

  const create = useMutation({
    mutationFn: (values: CreateItemsPayload) => catalogingApi.createItems(bibId, values),
    onSuccess: async (result) => {
      message.success(
        `Đã tạo ${result.created} bản: ${result.barcodes.join(', ')}` +
          (result.callNumber ? ` — ký hiệu xếp giá ${result.callNumber}` : ''),
      );

      form.resetFields();
      await onCreated();
    },
    onError: (error: unknown) => message.error(applyApiError(form, error)),
  });

  return (
    <Drawer
      open={open}
      onClose={onClose}
      width={560}
      title="Thêm đăng ký cá biệt"
      extra={
        <Space>
          <Button onClick={onClose}>Hủy</Button>
          <Button type="primary" loading={create.isPending} onClick={() => form.submit()}>
            Tạo
          </Button>
        </Space>
      }
    >
      <Form
        form={form}
        layout="vertical"
        initialValues={{
          quantity: 1,
          price: 0,
          acquisitionType: 'Purchase',
          unlockImmediately: true,
        }}
        onFinish={(values) => create.mutate(values)}
      >
        <Form.Item
          name="quantity"
          label="Số bản"
          rules={[{ required: true, message: 'Chưa nhập số bản.' }]}
          extra="Mỗi bản được cấp một mã vạch và một số đăng ký cá biệt riêng, đánh số liền nhau."
        >
          <InputNumber min={1} max={500} style={{ width: 160 }} />
        </Form.Item>

        <Form.Item
          name="warehouseId"
          label="Kho"
          rules={[{ required: true, message: 'Chưa chọn kho.' }]}
        >
          <Select
            options={(warehouses.data ?? []).map((warehouse) => ({
              value: warehouse.id,
              label: `${warehouse.name} — ${warehouse.libraryName}`,
            }))}
            placeholder="Chọn kho lưu giữ"
            showSearch
            optionFilterProp="label"
          />
        </Form.Item>

        <Form.Item name="shelfId" label="Vị trí giá">
          <Select
            options={(shelves.data ?? []).map((shelf) => ({ value: shelf.id, label: shelf.name }))}
            placeholder={warehouseId ? 'Chưa xếp giá cụ thể' : 'Chọn kho trước'}
            disabled={!warehouseId}
            allowClear
            showSearch
            optionFilterProp="label"
          />
        </Form.Item>

        <Form.Item
          name="callNumber"
          label="Ký hiệu xếp giá"
          extra="Bỏ trống thì hệ thống sinh theo quy tắc của kho, hoặc quy tắc chung của thư viện."
        >
          <Input placeholder="Ví dụ: 005.74 NGU" style={MONOSPACE} />
        </Form.Item>

        <Space size={12} align="start" style={{ width: '100%' }}>
          <Form.Item name="price" label="Đơn giá (đồng)" style={{ width: 200 }}>
            {/* Vietnamese money is grouped with dots, so the box shows 185.000 while holding 185000. */}
            <InputNumber<number>
              min={0}
              style={{ width: '100%' }}
              formatter={(value) => `${value ?? ''}`.replace(/\B(?=(\d{3})+(?!\d))/g, '.')}
              parser={(value) => Number((value ?? '').replace(/\D/g, '')) || 0}
            />
          </Form.Item>

          <Form.Item name="acquisitionType" label="Hình thức bổ sung" style={{ width: 200 }}>
            <Select
              options={Object.entries(ACQUISITION_TYPE_LABELS).map(([value, label]) => ({
                value,
                label,
              }))}
            />
          </Form.Item>
        </Space>

        <Form.Item name="fundingSourceId" label="Nguồn kinh phí">
          <Select
            options={toOptions(fundingSources.data)}
            placeholder="Không ghi nguồn kinh phí"
            allowClear
            showSearch
            optionFilterProp="label"
          />
        </Form.Item>

        <Form.Item name="volumeNumber" label="Tập / số">
          <Input placeholder="Ví dụ: T.2" />
        </Form.Item>

        <Form.Item name="condition" label="Tình trạng">
          <Input placeholder="Ví dụ: Tốt" />
        </Form.Item>

        <Form.Item name="note" label="Ghi chú">
          <Input.TextArea rows={2} />
        </Form.Item>

        <Form.Item
          name="unlockImmediately"
          label="Cho lưu thông ngay"
          valuePropName="checked"
          extra="Tắt nếu các bản này còn phải kiểm nhận trước khi đưa ra phục vụ."
        >
          <Switch />
        </Form.Item>
      </Form>
    </Drawer>
  );
}
