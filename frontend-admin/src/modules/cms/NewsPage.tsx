import { useState } from 'react';
import {
  App,
  Button,
  Card,
  Col,
  DatePicker,
  Drawer,
  Form,
  Input,
  List,
  Popconfirm,
  Row,
  Select,
  Space,
  Statistic,
  Switch,
  Table,
  Tabs,
  Tag,
  Upload,
} from 'antd';
import {
  DeleteOutlined,
  EditOutlined,
  EyeOutlined,
  PlusOutlined,
  UploadOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { api } from '@/api/client';
import type { PagedResult } from '@/types/api';
import type { CatalogItem } from '@/modules/catalogs/types';
import { cmsApi } from './api';
import { RichTextEditor } from './RichTextEditor';
import type { CmsNewsRow } from './types';

/**
 * VIII.2 — Quản lý tin tức và sự kiện.
 *
 * Chuyên mục tin dùng chung màn hình danh mục của hệ thống, nên ở đây chỉ chọn chứ không sửa.
 * Hẹn giờ đăng: đặt mốc tương lai thì bài chỉ hiện trên trang tra cứu khi tới giờ.
 */
export function CmsNewsPage() {
  return (
    <>
      <PageHeader
        title="Tin tức – sự kiện"
        description="Soạn, hẹn giờ đăng và theo dõi lượt xem các bản tin của thư viện."
      />

      <Card>
        <Tabs
          items={[
            { key: 'list', label: 'Danh sách tin', children: <NewsTab /> },
            { key: 'stats', label: 'Thống kê lượt xem', children: <NewsStatisticsTab /> },
          ]}
        />
      </Card>
    </>
  );
}

function NewsTab() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [keyword, setKeyword] = useState('');
  const [categoryId, setCategoryId] = useState<string | undefined>();
  const [page, setPage] = useState(1);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [open, setOpen] = useState(false);
  const [form] = Form.useForm();

  const news = useQuery({
    queryKey: ['cms-news', keyword, categoryId, page],
    queryFn: () =>
      cmsApi.news({ keyword: keyword || undefined, categoryId, page, pageSize: 20 }),
  });

  const categories = useQuery({
    queryKey: ['catalog', 'news-categories'],
    queryFn: () =>
      api.get<PagedResult<CatalogItem>>('/catalogs/news-categories/items', {
        params: { page: 1, pageSize: 200, isActive: true },
      }),
  });

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      cmsApi.saveNews(values, editingId ?? undefined),
    onSuccess: () => {
      message.success('Đã lưu bản tin.');
      setOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['cms-news'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  const publish = useMutation({
    mutationFn: ({ id, value }: { id: string; value: boolean }) => cmsApi.publishNews(id, value),
    onSuccess: (_result, variables) => {
      message.success(variables.value ? 'Đã đăng bản tin.' : 'Đã gỡ bản tin.');
      void queryClient.invalidateQueries({ queryKey: ['cms-news'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không đổi được trạng thái.'),
  });

  const remove = useMutation({
    mutationFn: (id: string) => cmsApi.deleteNews(id),
    onSuccess: () => {
      message.success('Đã xóa bản tin.');
      void queryClient.invalidateQueries({ queryKey: ['cms-news'] });
    },
  });

  const openEditor = async (id?: string) => {
    setEditingId(id ?? null);
    form.resetFields();

    if (id) {
      const detail = await cmsApi.newsItem(id);
      form.setFieldsValue({
        ...detail,
        publishedAt: detail.publishedAt ? dayjs(detail.publishedAt) : undefined,
      });
    }

    setOpen(true);
  };

  const columns: ColumnsType<CmsNewsRow> = [
    {
      title: 'Ảnh',
      dataIndex: 'thumbnailUrl',
      width: 110,
      render: (url?: string) =>
        url ? (
          <img src={url} alt="" style={{ width: 84, height: 52, objectFit: 'cover', borderRadius: 4 }} />
        ) : (
          <span style={{ color: '#999' }}>—</span>
        ),
    },
    { title: 'Tiêu đề', dataIndex: 'title', width: 320 },
    { title: 'Chuyên mục', dataIndex: 'categoryName', width: 160 },
    {
      title: 'Nổi bật',
      dataIndex: 'isFeatured',
      width: 100,
      render: (featured: boolean) => (featured ? <Tag color="gold">Nổi bật</Tag> : null),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'isPublished',
      width: 160,
      render: (published: boolean, row) => {
        if (!published) {
          return <Tag>Bản nháp</Tag>;
        }

        const scheduled = row.publishedAt && dayjs(row.publishedAt).isAfter(dayjs());

        return scheduled ? (
          <Tag color="blue">Hẹn {dayjs(row.publishedAt).format('DD/MM HH:mm')}</Tag>
        ) : (
          <Tag color="green">Đã đăng</Tag>
        );
      },
    },
    { title: 'Lượt xem', dataIndex: 'viewCount', width: 100, align: 'right' },
    {
      title: 'Cập nhật',
      dataIndex: 'updatedAt',
      width: 140,
      render: (value?: string) => (value ? dayjs(value).format('DD/MM/YYYY HH:mm') : '—'),
    },
    {
      title: '',
      dataIndex: 'id',
      width: 190,
      render: (id: string, row) => (
        <Space>
          <Button
            size="small"
            icon={<EyeOutlined />}
            href={`/tin-tuc/${row.slug}`}
            target="_blank"
            rel="noopener noreferrer"
            disabled={!row.isPublished}
          />
          <Can permission={PERMISSIONS.cms.newsManage}>
            <Button size="small" icon={<EditOutlined />} onClick={() => void openEditor(id)} />
          </Can>
          <Can permission={PERMISSIONS.cms.newsPublish}>
            <Button
              size="small"
              onClick={() => publish.mutate({ id, value: !row.isPublished })}
            >
              {row.isPublished ? 'Gỡ' : 'Đăng'}
            </Button>
          </Can>
          <Can permission={PERMISSIONS.cms.newsManage}>
            <Popconfirm
              title="Xóa bản tin này?"
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
      <Space wrap style={{ marginBottom: 16 }}>
        <Input.Search
          placeholder="Tìm theo tiêu đề, tóm tắt, thẻ"
          allowClear
          style={{ width: 320 }}
          onSearch={(value) => {
            setKeyword(value);
            setPage(1);
          }}
        />
        <Select
          allowClear
          placeholder="Mọi chuyên mục"
          style={{ width: 220 }}
          value={categoryId}
          onChange={(value) => {
            setCategoryId(value);
            setPage(1);
          }}
          options={(categories.data?.items ?? []).map((item) => ({
            value: item.id,
            label: item.name,
          }))}
        />
        <Can permission={PERMISSIONS.cms.newsManage}>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => void openEditor()}>
            Soạn bản tin
          </Button>
        </Can>
      </Space>

      <Table
        rowKey="id"
        size="small"
        loading={news.isLoading}
        columns={columns}
        dataSource={news.data?.items ?? []}
        scroll={{ x: 1480 }}
        pagination={{
          current: news.data?.page ?? 1,
          pageSize: news.data?.pageSize ?? 20,
          total: news.data?.totalCount ?? 0,
          showSizeChanger: false,
          onChange: setPage,
        }}
        locale={{ emptyText: 'Chưa có bản tin nào.' }}
      />

      <Drawer
        open={open}
        width={960}
        title={editingId ? 'Sửa bản tin' : 'Soạn bản tin'}
        onClose={() => setOpen(false)}
        extra={
          <Button type="primary" loading={save.isPending} onClick={() => form.submit()}>
            Lưu bản tin
          </Button>
        }
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={(values) =>
            save.mutate({
              ...values,
              publishedAt: values.publishedAt ? dayjs(values.publishedAt).toISOString() : null,
            })
          }
        >
          <Form.Item
            name="title"
            label="Tiêu đề"
            rules={[{ required: true, message: 'Chưa nhập tiêu đề tin.' }]}
          >
            <Input />
          </Form.Item>

          <Row gutter={16}>
            <Col span={8}>
              <Form.Item name="categoryId" label="Chuyên mục">
                <Select
                  allowClear
                  options={(categories.data?.items ?? []).map((item) => ({
                    value: item.id,
                    label: item.name,
                  }))}
                />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="slug" label="Đường dẫn" extra="Bỏ trống để tự sinh từ tiêu đề.">
                <Input />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="tags" label="Thẻ" extra="Ngăn cách bằng dấu phẩy.">
                <Input />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col span={8}>
              <Form.Item name="author" label="Người viết" extra="Bỏ trống thì lấy tên người đăng.">
                <Input />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item
                name="publishedAt"
                label="Thời điểm đăng"
                extra="Đặt mốc tương lai để hẹn giờ."
              >
                <DatePicker showTime format="DD/MM/YYYY HH:mm" style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={4}>
              <Form.Item name="isFeatured" label="Tin nổi bật" valuePropName="checked">
                <Switch />
              </Form.Item>
            </Col>
            <Col span={4}>
              <Form.Item name="isPublished" label="Đăng" valuePropName="checked">
                <Switch />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item name="thumbnailUrl" label="Ảnh đại diện">
            <ThumbnailField />
          </Form.Item>

          <Form.Item name="summary" label="Tóm tắt" extra="Bỏ trống thì lấy đoạn đầu của bài.">
            <Input.TextArea rows={2} maxLength={1000} showCount />
          </Form.Item>

          <Form.Item name="content" label="Nội dung">
            <RichTextEditor folder="news" />
          </Form.Item>
        </Form>
      </Drawer>
    </>
  );
}

function ThumbnailField({
  value,
  onChange,
}: {
  value?: string;
  onChange?: (value: string) => void;
}) {
  const { message } = App.useApp();

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
            void (async () => {
              try {
                const media = await cmsApi.uploadMedia(file, 'news');
                onChange?.(media.url);
                message.success('Đã tải ảnh lên.');
              } catch (error) {
                message.error((error as Error).message);
              }
            })();

            return false;
          }}
        >
          <Button icon={<UploadOutlined />}>Tải ảnh</Button>
        </Upload>
      </Space.Compact>

      {value ? (
        <img src={value} alt="Xem trước" style={{ maxHeight: 96, borderRadius: 6 }} />
      ) : null}
    </Space>
  );
}

function NewsStatisticsTab() {
  const stats = useQuery({
    queryKey: ['cms-news-statistics'],
    queryFn: () => cmsApi.newsStatistics(10),
  });

  return (
    <>
      <Space size="large" wrap style={{ marginBottom: 24 }}>
        <Statistic title="Tổng số bài" value={stats.data?.totalCount ?? 0} />
        <Statistic title="Đã đăng" value={stats.data?.publishedCount ?? 0} />
        <Statistic title="Bản nháp" value={stats.data?.draftCount ?? 0} />
        <Statistic title="Tổng lượt xem" value={stats.data?.totalViews ?? 0} />
      </Space>

      <Row gutter={24}>
        <Col xs={24} md={12}>
          <Card size="small" title="Theo chuyên mục" loading={stats.isLoading}>
            <Table
              rowKey="categoryName"
              size="small"
              pagination={false}
              dataSource={stats.data?.byCategory ?? []}
              columns={[
                { title: 'Chuyên mục', dataIndex: 'categoryName' },
                { title: 'Số bài', dataIndex: 'newsCount', width: 100, align: 'right' },
                { title: 'Lượt xem', dataIndex: 'viewCount', width: 120, align: 'right' },
              ]}
              locale={{ emptyText: 'Chưa có dữ liệu.' }}
            />
          </Card>
        </Col>

        <Col xs={24} md={12}>
          <Card size="small" title="Bài được xem nhiều nhất" loading={stats.isLoading}>
            <List
              dataSource={stats.data?.topViewed ?? []}
              locale={{ emptyText: 'Chưa có dữ liệu.' }}
              renderItem={(item, index) => (
                <List.Item>
                  <List.Item.Meta
                    title={`${index + 1}. ${item.title}`}
                    description={`${item.viewCount} lượt xem${
                      item.publishedAt
                        ? ` • đăng ${dayjs(item.publishedAt).format('DD/MM/YYYY')}`
                        : ''
                    }`}
                  />
                </List.Item>
              )}
            />
          </Card>
        </Col>
      </Row>
    </>
  );
}
