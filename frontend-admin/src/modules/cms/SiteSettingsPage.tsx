import { useEffect, useState } from 'react';
import {
  App,
  Button,
  Card,
  DatePicker,
  Drawer,
  Form,
  Input,
  InputNumber,
  Popconfirm,
  Select,
  Space,
  Switch,
  Table,
  Tabs,
  Tag,
  Tooltip,
  Tree,
  Typography,
  Upload,
} from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined, UploadOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import type { DataNode } from 'antd/es/tree';
import dayjs from 'dayjs';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { cmsApi } from './api';
import type { CmsBanner, CmsLink, CmsMenu, CmsSettingItem } from './types';
import { MAU } from '@/lib/palette';

const { Paragraph } = Typography;

/**
 * VIII.1 — Cập nhật thông tin trang thư viện.
 *
 * Bốn thẻ trên cùng một màn hình vì với cán bộ đây là một việc: sửa lại bộ mặt của thư viện trên
 * Internet. Bên dưới thì cấu hình chung đi về hai kho khác nhau, còn menu, banner và liên kết là
 * ba bảng riêng — nhưng đó là chuyện của hệ thống, không phải chuyện người dùng phải biết.
 */
export function SiteSettingsPage() {
  return (
    <>
      <PageHeader
        title="Thông tin trang thư viện"
        description="Tên, logo, liên hệ, giờ mở cửa, thanh điều hướng, banner và liên kết hiển thị trên trang tra cứu."
      />

      <Card>
        <Tabs
          items={[
            { key: 'settings', label: 'Cấu hình chung', children: <SettingsTab /> },
            { key: 'menus', label: 'Menu điều hướng', children: <MenusTab /> },
            { key: 'banners', label: 'Banner trang chủ', children: <BannersTab /> },
            { key: 'links', label: 'Liên kết website', children: <LinksTab /> },
          ]}
        />
      </Card>
    </>
  );
}

function SettingsTab() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const [form] = Form.useForm();

  const groups = useQuery({ queryKey: ['cms-settings'], queryFn: () => cmsApi.settings() });

  useEffect(() => {
    if (!groups.data) return;

    const values: Record<string, unknown> = {};

    groups.data.forEach((group) =>
      group.items.forEach((item) => {
        values[item.key] = item.dataType === 'boolean' ? item.value === 'true' : item.value;
      }),
    );

    form.setFieldsValue(values);
  }, [form, groups.data]);

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) => {
      const items = Object.entries(values).map(([key, value]) => ({
        key,
        value:
          value === undefined || value === null
            ? ''
            : typeof value === 'boolean'
              ? String(value)
              : String(value),
      }));

      return cmsApi.saveSettings(items);
    },
    onSuccess: () => {
      message.success('Đã lưu cấu hình trang thư viện.');
      void queryClient.invalidateQueries({ queryKey: ['cms-settings'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  return (
    <Form form={form} layout="vertical" onFinish={(values) => save.mutate(values)}>
      {(groups.data ?? []).map((group) => (
        <Card key={group.code} size="small" title={group.name} style={{ marginBottom: 16 }}>
          {group.items.map((item) => (
            <SettingField key={item.key} item={item} />
          ))}
        </Card>
      ))}

      <Can permission={PERMISSIONS.cms.settingManage}>
        <Button type="primary" htmlType="submit" loading={save.isPending}>
          Lưu cấu hình
        </Button>
      </Can>
    </Form>
  );
}

/** Một ô cấu hình, dựng đúng loại điều khiển theo kiểu dữ liệu đã khai ở máy chủ. */
function SettingField({ item }: { item: CmsSettingItem }) {
  const { message } = App.useApp();
  const form = Form.useFormInstance();
  const isImage = item.dataType === 'image' || item.dataType === 'file';

  const control = () => {
    switch (item.dataType) {
      case 'boolean':
        return <Switch />;
      case 'number':
        return <InputNumber style={{ width: 200 }} />;
      case 'multiline':
        return <Input.TextArea autoSize={{ minRows: 3, maxRows: 8 }} />;
      default:
        return <Input />;
    }
  };

  return (
    <Form.Item
      name={item.key}
      label={item.name}
      extra={item.description}
      valuePropName={item.dataType === 'boolean' ? 'checked' : 'value'}
    >
      {isImage ? (
        <ImageField
          onUpload={async (file) => {
            try {
              const media = await cmsApi.uploadMedia(file, 'logo');
              form.setFieldValue(item.key, media.url);
              message.success('Đã tải ảnh lên.');
            } catch (error) {
              message.error((error as Error).message);
            }
          }}
        />
      ) : (
        control()
      )}
    </Form.Item>
  );
}

/** Ô nhập đường dẫn ảnh kèm nút tải tệp lên và ô xem trước. */
function ImageField({
  value,
  onChange,
  onUpload,
}: {
  value?: string;
  onChange?: (value: string) => void;
  onUpload: (file: File) => Promise<void>;
}) {
  return (
    <Space direction="vertical" style={{ width: '100%' }}>
      <Space.Compact style={{ width: '100%', maxWidth: 560 }}>
        <Input
          value={value}
          placeholder="Đường dẫn ảnh"
          onChange={(event) => onChange?.(event.target.value)}
        />
        <Upload
          accept="image/*"
          showUploadList={false}
          beforeUpload={(file) => {
            void onUpload(file);
            return false;
          }}
        >
          <Button icon={<UploadOutlined />}>Tải ảnh</Button>
        </Upload>
      </Space.Compact>

      {value ? (
        <img
          src={value}
          alt="Xem trước"
          style={{ maxHeight: 72, borderRadius: 6, border: '1px solid ${MAU.vien}' }}
        />
      ) : null}
    </Space>
  );
}

function MenusTab() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<CmsMenu | null>(null);
  const [open, setOpen] = useState(false);
  const [form] = Form.useForm();

  const menus = useQuery({ queryKey: ['cms-menus'], queryFn: () => cmsApi.menus() });

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) => cmsApi.saveMenu(values, editing?.id),
    onSuccess: () => {
      message.success('Đã lưu mục menu.');
      setOpen(false);
      setEditing(null);
      void queryClient.invalidateQueries({ queryKey: ['cms-menus'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  const remove = useMutation({
    mutationFn: (id: string) => cmsApi.deleteMenu(id),
    onSuccess: () => {
      message.success('Đã xóa mục menu.');
      void queryClient.invalidateQueries({ queryKey: ['cms-menus'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xóa được.'),
  });

  const flat = flatten(menus.data ?? []);

  return (
    <>
      <Space style={{ marginBottom: 12 }}>
        <Can permission={PERMISSIONS.cms.menuManage}>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => {
              setEditing(null);
              form.resetFields();
              setOpen(true);
            }}
          >
            Thêm mục menu
          </Button>
        </Can>
      </Space>

      <Paragraph type="secondary">
        Thanh điều hướng của trang tra cứu. Mục cha bị tắt thì cả nhánh bên dưới cũng ẩn theo.
      </Paragraph>

      <Tree
        treeData={buildMenuNodes(menus.data ?? [], {
          onEdit: (item) => {
            setEditing(item);
            form.setFieldsValue(item);
            setOpen(true);
          },
          onDelete: (id) => remove.mutate(id),
        })}
        defaultExpandAll
        selectable={false}
      />

      <Drawer
        open={open}
        width={480}
        title={editing ? 'Sửa mục menu' : 'Thêm mục menu'}
        onClose={() => setOpen(false)}
        extra={
          <Button type="primary" loading={save.isPending} onClick={() => form.submit()}>
            Lưu
          </Button>
        }
      >
        <Form form={form} layout="vertical" onFinish={(values) => save.mutate(values)}>
          <Form.Item
            name="name"
            label="Tên mục"
            rules={[{ required: true, message: 'Chưa nhập tên mục.' }]}
          >
            <Input />
          </Form.Item>
          <Form.Item
            name="url"
            label="Đường dẫn"
            extra="Đường dẫn nội bộ bắt đầu bằng / (ví dụ /tra-cuu), hoặc địa chỉ đầy đủ ra ngoài."
          >
            <Input placeholder="/tra-cuu" />
          </Form.Item>
          <Form.Item name="parentId" label="Thuộc mục cha">
            <Select
              allowClear
              placeholder="Là mục cấp trên cùng"
              options={flat
                .filter((item) => item.id !== editing?.id)
                .map((item) => ({ value: item.id, label: item.name }))}
            />
          </Form.Item>
          <Form.Item name="sortOrder" label="Thứ tự" initialValue={0}>
            <InputNumber min={0} />
          </Form.Item>
          <Form.Item name="target" label="Mở ở">
            <Select
              allowClear
              options={[
                { value: '_self', label: 'Cùng cửa sổ' },
                { value: '_blank', label: 'Cửa sổ mới' },
              ]}
            />
          </Form.Item>
          <Form.Item name="isActive" label="Đang hiển thị" valuePropName="checked" initialValue>
            <Switch />
          </Form.Item>
        </Form>
      </Drawer>
    </>
  );
}

function BannersTab() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<CmsBanner | null>(null);
  const [open, setOpen] = useState(false);
  const [form] = Form.useForm();

  const banners = useQuery({ queryKey: ['cms-banners'], queryFn: () => cmsApi.banners() });

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) => cmsApi.saveBanner(values, editing?.id),
    onSuccess: () => {
      message.success('Đã lưu banner.');
      setOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['cms-banners'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  const remove = useMutation({
    mutationFn: (id: string) => cmsApi.deleteBanner(id),
    onSuccess: () => {
      message.success('Đã xóa banner.');
      void queryClient.invalidateQueries({ queryKey: ['cms-banners'] });
    },
  });

  const columns: ColumnsType<CmsBanner> = [
    {
      title: 'Ảnh',
      dataIndex: 'imageUrl',
      width: 160,
      render: (url: string) => (
        <img src={url} alt="" style={{ width: 130, height: 60, objectFit: 'cover', borderRadius: 4 }} />
      ),
    },
    { title: 'Tiêu đề', dataIndex: 'title', width: 240 },
    { title: 'Vị trí', dataIndex: 'position', width: 140 },
    { title: 'Liên kết', dataIndex: 'link', width: 240, ellipsis: true },
    {
      title: 'Thời gian hiển thị',
      dataIndex: 'startDate',
      width: 220,
      render: (_: unknown, row) =>
        row.startDate || row.endDate
          ? `${row.startDate ? dayjs(row.startDate).format('DD/MM/YYYY') : '…'} → ${
              row.endDate ? dayjs(row.endDate).format('DD/MM/YYYY') : '…'
            }`
          : 'Luôn hiển thị',
    },
    { title: 'Thứ tự', dataIndex: 'sortOrder', width: 90 },
    {
      title: 'Trạng thái',
      dataIndex: 'isActive',
      width: 120,
      render: (active: boolean) =>
        active ? <Tag color="green">Đang bật</Tag> : <Tag>Đang tắt</Tag>,
    },
    {
      title: '',
      dataIndex: 'id',
      width: 110,
      render: (id: string, row) => (
        <Space>
          <Can permission={PERMISSIONS.cms.bannerManage}>
            <Tooltip title="Sửa">
              <Button
                size="small"
                icon={<EditOutlined />}
                onClick={() => {
                  setEditing(row);
                  form.setFieldsValue({
                    ...row,
                    startDate: row.startDate ? dayjs(row.startDate) : undefined,
                    endDate: row.endDate ? dayjs(row.endDate) : undefined,
                  });
                  setOpen(true);
                }}
              />
            </Tooltip>
          </Can>
          <Can permission={PERMISSIONS.cms.bannerManage}>
            <Popconfirm
              title="Xóa banner này?"
              okText="Xóa"
              cancelText="Không"
              onConfirm={() => remove.mutate(id)}
            >
              <Button size="small" danger icon={<DeleteOutlined />} />
            </Popconfirm>
          </Can>
        </Space>
      ),
    },
  ];

  return (
    <>
      <Space style={{ marginBottom: 12 }}>
        <Can permission={PERMISSIONS.cms.bannerManage}>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => {
              setEditing(null);
              form.resetFields();
              setOpen(true);
            }}
          >
            Thêm banner
          </Button>
        </Can>
      </Space>

      <Table
        rowKey="id"
        size="small"
        loading={banners.isLoading}
        columns={columns}
        dataSource={banners.data ?? []}
        pagination={false}
        scroll={{ x: 1180 }}
        locale={{ emptyText: 'Chưa có banner nào.' }}
      />

      <Drawer
        open={open}
        width={520}
        title={editing ? 'Sửa banner' : 'Thêm banner'}
        onClose={() => setOpen(false)}
        extra={
          <Button type="primary" loading={save.isPending} onClick={() => form.submit()}>
            Lưu
          </Button>
        }
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={(values) =>
            save.mutate({
              ...values,
              startDate: values.startDate ? dayjs(values.startDate).format('YYYY-MM-DD') : null,
              endDate: values.endDate ? dayjs(values.endDate).format('YYYY-MM-DD') : null,
            })
          }
        >
          <Form.Item
            name="title"
            label="Tiêu đề"
            rules={[{ required: true, message: 'Chưa nhập tiêu đề.' }]}
          >
            <Input />
          </Form.Item>
          <Form.Item
            name="imageUrl"
            label="Ảnh banner"
            rules={[{ required: true, message: 'Chưa chọn ảnh.' }]}
          >
            <BannerImageField />
          </Form.Item>
          <Form.Item name="link" label="Liên kết khi bấm vào">
            <Input placeholder="/tin-tuc/... hoặc https://..." />
          </Form.Item>
          <Form.Item name="position" label="Vị trí" initialValue="HOME_SLIDER">
            <Select
              options={[
                { value: 'HOME_SLIDER', label: 'Trình chiếu trang chủ' },
                { value: 'SIDEBAR', label: 'Cột bên' },
                { value: 'FOOTER', label: 'Chân trang' },
              ]}
            />
          </Form.Item>
          <Form.Item name="startDate" label="Bắt đầu hiển thị">
            <DatePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="endDate" label="Kết thúc hiển thị">
            <DatePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="sortOrder" label="Thứ tự" initialValue={0}>
            <InputNumber min={0} />
          </Form.Item>
          <Form.Item name="isActive" label="Đang bật" valuePropName="checked" initialValue>
            <Switch />
          </Form.Item>
        </Form>
      </Drawer>
    </>
  );
}

function BannerImageField({
  value,
  onChange,
}: {
  value?: string;
  onChange?: (value: string) => void;
}) {
  const { message } = App.useApp();

  return (
    <ImageField
      value={value}
      onChange={onChange}
      onUpload={async (file) => {
        try {
          const media = await cmsApi.uploadMedia(file, 'banner');
          onChange?.(media.url);
          message.success('Đã tải ảnh lên.');
        } catch (error) {
          message.error((error as Error).message);
        }
      }}
    />
  );
}

function LinksTab() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<CmsLink | null>(null);
  const [open, setOpen] = useState(false);
  const [form] = Form.useForm();

  const links = useQuery({ queryKey: ['cms-links'], queryFn: () => cmsApi.links() });

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) => cmsApi.saveLink(values, editing?.id),
    onSuccess: () => {
      message.success('Đã lưu liên kết.');
      setOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['cms-links'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  const remove = useMutation({
    mutationFn: (id: string) => cmsApi.deleteLink(id),
    onSuccess: () => {
      message.success('Đã xóa liên kết.');
      void queryClient.invalidateQueries({ queryKey: ['cms-links'] });
    },
  });

  const columns: ColumnsType<CmsLink> = [
    { title: 'Tên', dataIndex: 'name', width: 260 },
    {
      title: 'Địa chỉ',
      dataIndex: 'url',
      width: 320,
      render: (url: string) => (
        <a href={url} target="_blank" rel="noopener noreferrer">
          {url}
        </a>
      ),
    },
    { title: 'Nhóm', dataIndex: 'groupName', width: 200 },
    { title: 'Thứ tự', dataIndex: 'sortOrder', width: 90 },
    {
      title: 'Trạng thái',
      dataIndex: 'isActive',
      width: 120,
      render: (active: boolean) =>
        active ? <Tag color="green">Đang bật</Tag> : <Tag>Đang tắt</Tag>,
    },
    {
      title: '',
      dataIndex: 'id',
      width: 110,
      render: (id: string, row) => (
        <Space>
          <Can permission={PERMISSIONS.cms.linkManage}>
            <Button
              size="small"
              icon={<EditOutlined />}
              onClick={() => {
                setEditing(row);
                form.setFieldsValue(row);
                setOpen(true);
              }}
            />
          </Can>
          <Can permission={PERMISSIONS.cms.linkManage}>
            <Popconfirm
              title="Xóa liên kết này?"
              okText="Xóa"
              cancelText="Không"
              onConfirm={() => remove.mutate(id)}
            >
              <Button size="small" danger icon={<DeleteOutlined />} />
            </Popconfirm>
          </Can>
        </Space>
      ),
    },
  ];

  return (
    <>
      <Space style={{ marginBottom: 12 }}>
        <Can permission={PERMISSIONS.cms.linkManage}>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => {
              setEditing(null);
              form.resetFields();
              setOpen(true);
            }}
          >
            Thêm liên kết
          </Button>
        </Can>
      </Space>

      <Table
        rowKey="id"
        size="small"
        loading={links.isLoading}
        columns={columns}
        dataSource={links.data ?? []}
        pagination={false}
        scroll={{ x: 1100 }}
        locale={{ emptyText: 'Chưa khai báo liên kết nào.' }}
      />

      <Drawer
        open={open}
        width={480}
        title={editing ? 'Sửa liên kết' : 'Thêm liên kết'}
        onClose={() => setOpen(false)}
        extra={
          <Button type="primary" loading={save.isPending} onClick={() => form.submit()}>
            Lưu
          </Button>
        }
      >
        <Form form={form} layout="vertical" onFinish={(values) => save.mutate(values)}>
          <Form.Item
            name="name"
            label="Tên liên kết"
            rules={[{ required: true, message: 'Chưa nhập tên.' }]}
          >
            <Input />
          </Form.Item>
          <Form.Item
            name="url"
            label="Địa chỉ"
            rules={[{ required: true, message: 'Chưa nhập địa chỉ.' }]}
          >
            <Input placeholder="https://..." />
          </Form.Item>
          <Form.Item name="groupName" label="Nhóm hiển thị">
            <Input placeholder="Thư viện bạn / Cơ sở dữ liệu trực tuyến" />
          </Form.Item>
          <Form.Item name="description" label="Mô tả ngắn">
            <Input.TextArea rows={2} />
          </Form.Item>
          <Form.Item name="sortOrder" label="Thứ tự" initialValue={0}>
            <InputNumber min={0} />
          </Form.Item>
          <Form.Item name="isActive" label="Đang bật" valuePropName="checked" initialValue>
            <Switch />
          </Form.Item>
        </Form>
      </Drawer>
    </>
  );
}

/**
 * Dựng dữ liệu cây cho thanh điều hướng, kèm nút sửa và xóa ngay trên từng dòng.
 *
 * Tách khỏi thành phần vì đây là hàm đệ quy: viết lồng trong phần dựng giao diện thì mỗi lần vẽ
 * lại lại định nghĩa lại hàm, và kiểu dữ liệu của nhánh con không suy ra được.
 */
function buildMenuNodes(
  items: CmsMenu[],
  actions: { onEdit: (item: CmsMenu) => void; onDelete: (id: string) => void },
): DataNode[] {
  return items.map((item) => ({
    key: item.id,
    title: (
      <Space>
        <span style={{ fontWeight: 500 }}>{item.name}</span>
        <span style={{ color: MAU.chuMo }}>{item.url}</span>
        {item.isActive ? null : <Tag>đang tắt</Tag>}
        <Can permission={PERMISSIONS.cms.menuManage}>
          <Button
            size="small"
            type="text"
            icon={<EditOutlined />}
            onClick={() => actions.onEdit(item)}
          />
        </Can>
        <Can permission={PERMISSIONS.cms.menuManage}>
          <Popconfirm
            title="Xóa mục menu này?"
            okText="Xóa"
            cancelText="Không"
            onConfirm={() => actions.onDelete(item.id)}
          >
            <Button size="small" type="text" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Can>
      </Space>
    ),
    children: item.children.length > 0 ? buildMenuNodes(item.children, actions) : undefined,
  }));
}

/** Trải cây menu thành danh sách phẳng để chọn mục cha. */
function flatten(items: CmsMenu[]): CmsMenu[] {
  return items.flatMap((item) => [item, ...flatten(item.children)]);
}
