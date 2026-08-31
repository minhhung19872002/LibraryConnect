import { useState } from 'react';
import {
  App,
  Button,
  Card,
  Drawer,
  Form,
  Input,
  InputNumber,
  Popconfirm,
  Space,
  Switch,
  Table,
  Tag,
} from 'antd';
import { DeleteOutlined, EditOutlined, EyeOutlined, PlusOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { cmsApi } from './api';
import { RichTextEditor } from './RichTextEditor';
import type { CmsPageRow } from './types';

/**
 * VIII.1 — Quản lý trang tĩnh: Giới thiệu, Nội quy, Hướng dẫn sử dụng, Liên hệ, Hỏi đáp.
 *
 * Đường dẫn để trống thì hệ thống tự sinh từ tiêu đề và tự tránh trùng — cán bộ soạn nội dung không
 * phải nghĩ về địa chỉ trang.
 */
export function CmsPagesPage() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [keyword, setKeyword] = useState('');
  const [page, setPage] = useState(1);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [open, setOpen] = useState(false);
  const [form] = Form.useForm();

  const pages = useQuery({
    queryKey: ['cms-pages', keyword, page],
    queryFn: () => cmsApi.pages({ keyword: keyword || undefined, page, pageSize: 20 }),
  });

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      cmsApi.savePage(values, editingId ?? undefined),
    onSuccess: () => {
      message.success('Đã lưu trang.');
      setOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['cms-pages'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  const remove = useMutation({
    mutationFn: (id: string) => cmsApi.deletePage(id),
    onSuccess: () => {
      message.success('Đã xóa trang.');
      void queryClient.invalidateQueries({ queryKey: ['cms-pages'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xóa được.'),
  });

  const openEditor = async (id?: string) => {
    setEditingId(id ?? null);
    form.resetFields();

    if (id) {
      const detail = await cmsApi.page(id);
      form.setFieldsValue(detail);
    }

    setOpen(true);
  };

  const columns: ColumnsType<CmsPageRow> = [
    { title: 'Tiêu đề', dataIndex: 'title', width: 280 },
    {
      title: 'Đường dẫn',
      dataIndex: 'slug',
      width: 220,
      render: (slug: string) => <code>/trang/{slug}</code>,
    },
    { title: 'Mô tả cho công cụ tìm kiếm', dataIndex: 'metaDescription', width: 320, ellipsis: true },
    {
      title: 'Trạng thái',
      dataIndex: 'isPublished',
      width: 130,
      render: (published: boolean) =>
        published ? <Tag color="green">Đã đăng</Tag> : <Tag>Bản nháp</Tag>,
    },
    {
      title: 'Lượt xem',
      dataIndex: 'viewCount',
      width: 100,
      align: 'right',
    },
    { title: 'Thứ tự', dataIndex: 'sortOrder', width: 90 },
    {
      title: 'Cập nhật',
      dataIndex: 'updatedAt',
      width: 140,
      render: (value?: string) => (value ? dayjs(value).format('DD/MM/YYYY HH:mm') : '—'),
    },
    {
      title: '',
      dataIndex: 'id',
      width: 140,
      render: (id: string, row) => (
        <Space>
          <Button
            size="small"
            icon={<EyeOutlined />}
            href={`/trang/${row.slug}`}
            target="_blank"
            rel="noopener noreferrer"
            disabled={!row.isPublished}
          />
          <Can permission={PERMISSIONS.cms.pageManage}>
            <Button size="small" icon={<EditOutlined />} onClick={() => void openEditor(id)} />
          </Can>
          <Can permission={PERMISSIONS.cms.pageManage}>
            <Popconfirm
              title="Xóa trang này?"
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
      <PageHeader
        title="Trang tĩnh"
        description="Giới thiệu, nội quy, hướng dẫn sử dụng, liên hệ, hỏi đáp — nội dung hiển thị trên trang tra cứu."
        actions={
          <Can permission={PERMISSIONS.cms.pageManage}>
            <Button type="primary" icon={<PlusOutlined />} onClick={() => void openEditor()}>
              Thêm trang
            </Button>
          </Can>
        }
      />

      <Card>
        <Input.Search
          placeholder="Tìm theo tiêu đề hoặc đường dẫn"
          allowClear
          style={{ maxWidth: 420, marginBottom: 16 }}
          onSearch={(value) => {
            setKeyword(value);
            setPage(1);
          }}
        />

        <Table
          rowKey="id"
          size="small"
          loading={pages.isLoading}
          columns={columns}
          dataSource={pages.data?.items ?? []}
          scroll={{ x: 1420 }}
          pagination={{
            current: pages.data?.page ?? 1,
            pageSize: pages.data?.pageSize ?? 20,
            total: pages.data?.totalCount ?? 0,
            showSizeChanger: false,
            onChange: setPage,
          }}
          locale={{ emptyText: 'Chưa có trang tĩnh nào.' }}
        />
      </Card>

      <Drawer
        open={open}
        width={920}
        title={editingId ? 'Sửa trang' : 'Thêm trang'}
        onClose={() => setOpen(false)}
        extra={
          <Button type="primary" loading={save.isPending} onClick={() => form.submit()}>
            Lưu trang
          </Button>
        }
      >
        <Form form={form} layout="vertical" onFinish={(values) => save.mutate(values)}>
          <Form.Item
            name="title"
            label="Tiêu đề"
            rules={[{ required: true, message: 'Chưa nhập tiêu đề trang.' }]}
          >
            <Input />
          </Form.Item>

          <Space size="large" style={{ display: 'flex' }}>
            <Form.Item
              name="slug"
              label="Đường dẫn"
              extra="Bỏ trống để hệ thống tự sinh từ tiêu đề."
              style={{ flex: 1 }}
            >
              <Input placeholder="gioi-thieu" />
            </Form.Item>
            <Form.Item name="sortOrder" label="Thứ tự" initialValue={0}>
              <InputNumber min={0} />
            </Form.Item>
            <Form.Item name="isPublished" label="Đăng ngay" valuePropName="checked">
              <Switch />
            </Form.Item>
          </Space>

          <Form.Item
            name="metaDescription"
            label="Mô tả cho công cụ tìm kiếm"
            extra="Bỏ trống thì lấy đoạn đầu của nội dung."
          >
            <Input.TextArea rows={2} maxLength={300} showCount />
          </Form.Item>

          <Form.Item name="content" label="Nội dung">
            <RichTextEditor folder="page" />
          </Form.Item>
        </Form>
      </Drawer>
    </>
  );
}
