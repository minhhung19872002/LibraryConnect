import { useState } from 'react';
import {
  App,
  Button,
  Card,
  Col,
  DatePicker,
  Drawer,
  Empty,
  Form,
  Input,
  Popconfirm,
  Row,
  Space,
  Switch,
  Tag,
  Upload,
} from 'antd';
import {
  DeleteOutlined,
  EditOutlined,
  PlusOutlined,
  UploadOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { cmsApi } from './api';
import type { CmsGallery, CmsGalleryImage } from './types';

/**
 * VIII.2 — Thư viện ảnh sự kiện.
 *
 * Ảnh tải lên kho đối tượng ngay khi chọn, album chỉ giữ đường dẫn. Ảnh bìa bỏ trống thì lấy ảnh
 * đầu tiên, để lưới album trên trang tra cứu không có ô trống.
 */
export function CmsGalleriesPage() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [editing, setEditing] = useState<CmsGallery | null>(null);
  const [open, setOpen] = useState(false);
  const [images, setImages] = useState<CmsGalleryImage[]>([]);
  const [form] = Form.useForm();

  const galleries = useQuery({ queryKey: ['cms-galleries'], queryFn: () => cmsApi.galleries() });

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) => cmsApi.saveGallery(values, editing?.id),
    onSuccess: () => {
      message.success('Đã lưu album.');
      setOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['cms-galleries'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  const remove = useMutation({
    mutationFn: (id: string) => cmsApi.deleteGallery(id),
    onSuccess: () => {
      message.success('Đã xóa album.');
      void queryClient.invalidateQueries({ queryKey: ['cms-galleries'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xóa được.'),
  });

  const openEditor = (album?: CmsGallery) => {
    setEditing(album ?? null);
    setImages(album?.images ?? []);
    form.resetFields();

    if (album) {
      form.setFieldsValue({
        ...album,
        eventDate: album.eventDate ? dayjs(album.eventDate) : undefined,
      });
    }

    setOpen(true);
  };

  const upload = async (file: File) => {
    try {
      const media = await cmsApi.uploadMedia(file, 'gallery');

      setImages((current) => [
        ...current,
        { imageUrl: media.url, caption: '', sortOrder: current.length },
      ]);
    } catch (error) {
      message.error((error as Error).message);
    }

    return false;
  };

  return (
    <>
      <PageHeader
        title="Thư viện ảnh"
        description="Album ảnh các sự kiện của thư viện, hiển thị trên trang tra cứu."
        actions={
          <Can permission={PERMISSIONS.cms.galleryManage}>
            <Button type="primary" icon={<PlusOutlined />} onClick={() => openEditor()}>
              Tạo album
            </Button>
          </Can>
        }
      />

      <Card loading={galleries.isLoading}>
        {(galleries.data ?? []).length === 0 ? (
          <Empty description="Chưa có album nào." />
        ) : (
          <Row gutter={[16, 16]}>
            {(galleries.data ?? []).map((album) => (
              <Col xs={24} sm={12} md={8} lg={6} key={album.id}>
                <Card
                  size="small"
                  cover={
                    album.coverUrl ? (
                      <img
                        src={album.coverUrl}
                        alt={album.title}
                        style={{ height: 150, objectFit: 'cover' }}
                      />
                    ) : undefined
                  }
                  actions={[
                    <Button
                      key="edit"
                      type="text"
                      icon={<EditOutlined />}
                      onClick={() => openEditor(album)}
                    />,
                    <Popconfirm
                      key="delete"
                      title="Xóa album này?"
                      okText="Xóa"
                      cancelText="Không"
                      onConfirm={() => remove.mutate(album.id)}
                    >
                      <Button type="text" danger icon={<DeleteOutlined />} />
                    </Popconfirm>,
                  ]}
                >
                  <Card.Meta
                    title={album.title}
                    description={
                      <Space size={[6, 4]} wrap>
                        <Tag>{album.images.length} ảnh</Tag>
                        {album.eventDate ? (
                          <span>{dayjs(album.eventDate).format('DD/MM/YYYY')}</span>
                        ) : null}
                        {album.isPublished ? (
                          <Tag color="green">Đã đăng</Tag>
                        ) : (
                          <Tag>Bản nháp</Tag>
                        )}
                      </Space>
                    }
                  />
                </Card>
              </Col>
            ))}
          </Row>
        )}
      </Card>

      <Drawer
        open={open}
        width={720}
        title={editing ? 'Sửa album' : 'Tạo album'}
        onClose={() => setOpen(false)}
        extra={
          <Button type="primary" loading={save.isPending} onClick={() => form.submit()}>
            Lưu album
          </Button>
        }
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={(values) =>
            save.mutate({
              ...values,
              eventDate: values.eventDate ? dayjs(values.eventDate).format('YYYY-MM-DD') : null,
              images: images.map((image, index) => ({ ...image, sortOrder: index })),
            })
          }
        >
          <Form.Item
            name="title"
            label="Tên album"
            rules={[{ required: true, message: 'Chưa nhập tên album.' }]}
          >
            <Input />
          </Form.Item>
          <Form.Item name="description" label="Mô tả">
            <Input.TextArea rows={2} />
          </Form.Item>
          <Space size="large">
            <Form.Item name="eventDate" label="Ngày diễn ra">
              <DatePicker format="DD/MM/YYYY" />
            </Form.Item>
            <Form.Item name="isPublished" label="Đăng" valuePropName="checked">
              <Switch />
            </Form.Item>
          </Space>
          <Form.Item name="coverUrl" label="Ảnh bìa" extra="Bỏ trống thì lấy ảnh đầu tiên.">
            <Input placeholder="Đường dẫn ảnh bìa" />
          </Form.Item>
        </Form>

        <Upload accept="image/*" multiple showUploadList={false} beforeUpload={upload}>
          <Button icon={<UploadOutlined />}>Thêm ảnh vào album</Button>
        </Upload>

        <Row gutter={[12, 12]} style={{ marginTop: 16 }}>
          {images.map((image, index) => (
            <Col xs={12} sm={8} key={`${image.imageUrl}-${index}`}>
              <Card
                size="small"
                cover={
                  <img
                    src={image.imageUrl}
                    alt={image.caption ?? ''}
                    style={{ height: 110, objectFit: 'cover' }}
                  />
                }
              >
                <Input
                  size="small"
                  placeholder="Chú thích"
                  value={image.caption}
                  onChange={(event) =>
                    setImages((current) =>
                      current.map((row, position) =>
                        position === index ? { ...row, caption: event.target.value } : row,
                      ),
                    )
                  }
                />
                <Button
                  size="small"
                  danger
                  type="text"
                  icon={<DeleteOutlined />}
                  style={{ marginTop: 6 }}
                  onClick={() =>
                    setImages((current) => current.filter((_, position) => position !== index))
                  }
                >
                  Bỏ ảnh
                </Button>
              </Card>
            </Col>
          ))}
        </Row>
      </Drawer>
    </>
  );
}
