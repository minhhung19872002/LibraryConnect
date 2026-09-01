import { useMemo, useState } from 'react';
import {
  App,
  Button,
  Card,
  Checkbox,
  Col,
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
  Tree,
  Typography,
} from 'antd';
import {
  CloudUploadOutlined,
  DeleteOutlined,
  EditOutlined,
  FileSearchOutlined,
  PlusOutlined,
  ReadOutlined,
  ScanOutlined,
} from '@ant-design/icons';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import type { DataNode } from 'antd/es/tree';
import { PageHeader } from '@/components/PageHeader';
import { FilterBar } from '@/components/FilterBar';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { digitalApi } from './api';
import { DigitalUploadDrawer } from './DigitalUploadDrawer';
import { DigitalViewer } from './DigitalViewer';
import {
  accessLevelColors,
  accessLevelHints,
  accessLevelLabels,
  formatDateTime,
  formatGroupLabels,
  formatSize,
} from './labels';
import type {
  DigitalAccessLevel,
  DigitalCollectionDto,
  DigitalDocumentRowDto,
} from './types';

const accessOptions = (Object.keys(accessLevelLabels) as DigitalAccessLevel[]).map((value) => ({
  value,
  label: accessLevelLabels[value],
}));

const formatOptions = Object.entries(formatGroupLabels).map(([value, label]) => ({ value, label }));

/**
 * V.1 — Kho tài liệu số: cây bộ sưu tập bên trái, danh sách tài liệu bên phải.
 *
 * Ô tìm kiếm có công tắc "tìm trong nội dung": bật lên thì máy chủ dò cả phần văn bản đã rút ra từ
 * tệp (hoặc nhận dạng được từ bản quét) và trả về đoạn trích quanh chỗ khớp.
 */
export function DigitalDocumentsPage() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [collectionId, setCollectionId] = useState<string | undefined>(undefined);
  const [filter, setFilter] = useState<{
    keyword?: string;
    accessLevel?: DigitalAccessLevel;
    formatGroup?: string;
    fullText?: boolean;
  }>({});
  const [draft, setDraft] = useState(filter);
  const [page, setPage] = useState({ page: 1, pageSize: 20 });

  const [uploadOpen, setUploadOpen] = useState(false);
  const [viewing, setViewing] = useState<string | null>(null);
  const [editing, setEditing] = useState<DigitalDocumentRowDto | null>(null);
  const [collectionOpen, setCollectionOpen] = useState(false);

  const [editForm] = Form.useForm();
  const [collectionForm] = Form.useForm();

  const collections = useQuery({
    queryKey: ['digital-collections'],
    queryFn: () => digitalApi.collections(true),
  });

  const documents = useQuery({
    queryKey: ['digital-documents', page, filter, collectionId],
    queryFn: () =>
      digitalApi.search({
        ...page,
        keyword: filter.keyword,
        filter: {
          collectionId,
          includeDescendants: true,
          accessLevel: filter.accessLevel,
          formatGroup: filter.formatGroup,
          fullText: filter.fullText ?? false,
        },
      }),
    placeholderData: keepPreviousData,
  });

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      digitalApi.update(editing!.id, values),
    onSuccess: () => {
      message.success('Đã lưu tài liệu số.');
      setEditing(null);
      void queryClient.invalidateQueries({ queryKey: ['digital-documents'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  const remove = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => digitalApi.remove(id, reason),
    onSuccess: () => {
      message.success('Đã xóa tài liệu số.');
      void queryClient.invalidateQueries({ queryKey: ['digital-documents'] });
      void queryClient.invalidateQueries({ queryKey: ['digital-collections'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xóa được.'),
  });

  const ocr = useMutation({
    mutationFn: (id: string) => digitalApi.runOcr(id),
    onSuccess: () => message.success('Đã đưa vào hàng đợi nhận dạng ký tự.'),
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không chạy được.'),
  });

  const saveCollection = useMutation({
    mutationFn: (values: Record<string, unknown>) => digitalApi.saveCollection(values),
    onSuccess: () => {
      message.success('Đã thêm bộ sưu tập.');
      setCollectionOpen(false);
      collectionForm.resetFields();
      void queryClient.invalidateQueries({ queryKey: ['digital-collections'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  const treeData = useMemo(() => toTree(collections.data ?? []), [collections.data]);

  const columns: ColumnsType<DigitalDocumentRowDto> = [
    {
      title: 'Nhan đề',
      dataIndex: 'title',
      width: 340,
      render: (title: string, row) => (
        <Space direction="vertical" size={0}>
          <Typography.Link onClick={() => setViewing(row.id)}>{title}</Typography.Link>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {row.fileName} · {formatSize(row.fileSize)}
            {row.pageCount ? ` · ${row.pageCount} trang` : ''}
          </Typography.Text>
          {row.snippet && (
            <Typography.Text type="secondary" style={{ fontSize: 12, fontStyle: 'italic' }}>
              {row.snippet}
            </Typography.Text>
          )}
        </Space>
      ),
    },
    { title: 'Bộ sưu tập', dataIndex: 'collectionName', width: 200, ellipsis: true },
    {
      title: 'Định dạng',
      dataIndex: 'mimeType',
      width: 130,
      render: (mimeType: string) => formatGroupLabels[groupOf(mimeType)] ?? mimeType,
    },
    {
      title: 'Mức truy cập',
      dataIndex: 'accessLevel',
      width: 130,
      render: (level: DigitalAccessLevel) => (
        <Tag color={accessLevelColors[level]}>{accessLevelLabels[level]}</Tag>
      ),
    },
    {
      title: 'Chính sách',
      width: 170,
      render: (_, row) => (
        <Space size={4} wrap>
          {row.allowDownload && <Tag>Tải về</Tag>}
          {row.allowPrint && <Tag>In</Tag>}
          {row.watermarkEnabled && <Tag color="red">Chữ chìm</Tag>}
        </Space>
      ),
    },
    {
      title: 'Tìm toàn văn',
      dataIndex: 'hasText',
      width: 130,
      align: 'center',
      render: (hasText: boolean, row) =>
        hasText ? (
          <Tag color="green">{row.ocrProcessed ? 'Đã nhận dạng' : 'Có văn bản'}</Tag>
        ) : (
          <Tag color="default">Chưa có</Tag>
        ),
    },
    {
      title: 'Lượt xem',
      dataIndex: 'viewCount',
      width: 100,
      align: 'right',
    },
    {
      title: 'Lượt tải',
      dataIndex: 'downloadCount',
      width: 100,
      align: 'right',
    },
    { title: 'Tải lên lúc', dataIndex: 'uploadAt', width: 170, render: formatDateTime },
    {
      title: '',
      width: 230,
      render: (_, row) => (
        <Space size={2}>
          <Button type="link" size="small" icon={<ReadOutlined />} onClick={() => setViewing(row.id)}>
            Đọc
          </Button>
          <Can permission={PERMISSIONS.digital.update}>
            <Button
              type="link"
              size="small"
              icon={<EditOutlined />}
              onClick={() => {
                setEditing(row);
                editForm.setFieldsValue({
                  title: row.title,
                  collectionId: row.collectionId ?? undefined,
                  accessLevel: row.accessLevel,
                  allowDownload: row.allowDownload,
                  allowPrint: row.allowPrint,
                  watermarkEnabled: row.watermarkEnabled,
                  previewPages: row.previewPages,
                });
              }}
            >
              Sửa
            </Button>
          </Can>
          <Can permission={PERMISSIONS.digital.update}>
            <Button
              type="link"
              size="small"
              icon={<ScanOutlined />}
              title="Nhận dạng ký tự lại"
              onClick={() => ocr.mutate(row.id)}
            />
          </Can>
          <Can permission={PERMISSIONS.digital.delete}>
            <Button
              type="link"
              size="small"
              danger
              icon={<DeleteOutlined />}
              onClick={() => {
                let reason = '';

                modal.confirm({
                  title: `Xóa tài liệu "${row.title}"`,
                  content: (
                    <Input.TextArea
                      rows={2}
                      placeholder="Lý do xóa"
                      onChange={(event) => {
                        reason = event.target.value;
                      }}
                    />
                  ),
                  okText: 'Xóa',
                  cancelText: 'Đóng',
                  onOk: () => remove.mutateAsync({ id: row.id, reason }),
                });
              }}
            />
          </Can>
        </Space>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Kho tài liệu số"
        description="Bộ sưu tập, tệp toàn văn, chính sách truy cập và trình đọc trực tuyến có chữ chìm."
        actions={
          <Can permission={PERMISSIONS.digital.upload}>
            <Button type="primary" icon={<CloudUploadOutlined />} onClick={() => setUploadOpen(true)}>
              Tải tài liệu lên
            </Button>
          </Can>
        }
      />

      <Row gutter={16}>
        {/*
          Cây bộ sưu tập chỉ là danh sách vài dòng chữ ngắn, cho nó một phần tư màn hình là phí:
          bảng bên phải rộng 1.700 px bị nhồi vào 848 px và mất bốn cột cuối. Từ 1600 px trở lên
          màn hình đủ rộng cho cả hai, hẹp hơn thì cây xếp lên trên và bảng chiếm trọn bề ngang.
        */}
        <Col xs={24} xxl={5}>
          <Card
            size="small"
            title="Bộ sưu tập"
            extra={
              <Can permission={PERMISSIONS.digital.collection}>
                <Button
                  type="link"
                  size="small"
                  icon={<PlusOutlined />}
                  onClick={() => setCollectionOpen(true)}
                >
                  Thêm
                </Button>
              </Can>
            }
          >
            <Tree
              treeData={treeData}
              defaultExpandAll
              selectedKeys={collectionId ? [collectionId] : []}
              onSelect={(keys) => {
                setCollectionId(keys[0] ? String(keys[0]) : undefined);
                setPage((current) => ({ ...current, page: 1 }));
              }}
            />
            {collectionId && (
              <Button type="link" size="small" onClick={() => setCollectionId(undefined)}>
                Bỏ lọc bộ sưu tập
              </Button>
            )}
          </Card>
        </Col>

        <Col xs={24} xxl={19}>
          <Space direction="vertical" size={12} style={{ width: '100%' }}>
            <FilterBar
              loading={documents.isFetching}
              onSearch={() => {
                setFilter(draft);
                setPage((current) => ({ ...current, page: 1 }));
              }}
              onReset={() => {
                setDraft({});
                setFilter({});
              }}
            >
              <Input
                allowClear
                style={{ width: 280 }}
                prefix={<FileSearchOutlined />}
                placeholder="Nhan đề, tên tệp — gõ không dấu cũng tìm được"
                value={draft.keyword}
                onChange={(event) => setDraft({ ...draft, keyword: event.target.value })}
              />
              <Checkbox
                checked={draft.fullText ?? false}
                onChange={(event) => setDraft({ ...draft, fullText: event.target.checked })}
              >
                Tìm trong nội dung
              </Checkbox>
              <Select
                allowClear
                style={{ width: 160 }}
                placeholder="Mức truy cập"
                options={accessOptions}
                value={draft.accessLevel}
                onChange={(value) => setDraft({ ...draft, accessLevel: value })}
              />
              <Select
                allowClear
                style={{ width: 170 }}
                placeholder="Định dạng"
                options={formatOptions}
                value={draft.formatGroup}
                onChange={(value) => setDraft({ ...draft, formatGroup: value })}
              />
            </FilterBar>

            <Table
              rowKey="id"
              size="small"
              loading={documents.isFetching}
              dataSource={documents.data?.items ?? []}
              columns={columns}
              scroll={{ x: 1700 }}
              pagination={{
                current: documents.data?.page ?? 1,
                pageSize: documents.data?.pageSize ?? 20,
                total: documents.data?.totalCount ?? 0,
                showSizeChanger: true,
                showTotal: (total) => `Tổng ${total.toLocaleString('vi-VN')} tài liệu`,
              }}
              onChange={(pagination) =>
                setPage({ page: pagination.current ?? 1, pageSize: pagination.pageSize ?? 20 })
              }
            />
          </Space>
        </Col>
      </Row>

      <DigitalUploadDrawer
        open={uploadOpen}
        collections={collections.data ?? []}
        defaultCollectionId={collectionId}
        onClose={() => setUploadOpen(false)}
        onUploaded={() => {
          void queryClient.invalidateQueries({ queryKey: ['digital-documents'] });
          void queryClient.invalidateQueries({ queryKey: ['digital-collections'] });
        }}
      />

      <DigitalViewer documentId={viewing} onClose={() => setViewing(null)} />

      <Drawer
        open={editing !== null}
        onClose={() => setEditing(null)}
        width={520}
        title={editing ? `Sửa "${editing.title}"` : ''}
        extra={
          <Space>
            <Button onClick={() => setEditing(null)}>Hủy</Button>
            <Button
              type="primary"
              loading={save.isPending}
              onClick={() => void editForm.validateFields().then((values) => save.mutate(values))}
            >
              Lưu
            </Button>
          </Space>
        }
      >
        <Form form={editForm} layout="vertical">
          <Form.Item
            name="title"
            label="Nhan đề"
            rules={[{ required: true, message: 'Chưa nhập nhan đề.' }]}
          >
            <Input />
          </Form.Item>

          <Form.Item name="collectionId" label="Bộ sưu tập">
            <Select
              allowClear
              showSearch
              optionFilterProp="label"
              options={flatten(collections.data ?? [])}
            />
          </Form.Item>

          <Form.Item name="accessLevel" label="Mức truy cập">
            <Select
              options={(Object.keys(accessLevelLabels) as DigitalAccessLevel[]).map((value) => ({
                value,
                label: `${accessLevelLabels[value]} — ${accessLevelHints[value]}`,
              }))}
            />
          </Form.Item>

          <Form.Item
            name="previewPages"
            label="Số trang xem thử"
            extra="Áp dụng cho người chưa đủ quyền đọc toàn văn."
          >
            <InputNumber min={0} max={10000} style={{ width: '100%' }} />
          </Form.Item>

          <Space direction="vertical">
            <Form.Item name="allowDownload" valuePropName="checked" noStyle>
              <Checkbox>Cho phép tải bản gốc về</Checkbox>
            </Form.Item>
            <Form.Item name="allowPrint" valuePropName="checked" noStyle>
              <Checkbox>Cho phép in</Checkbox>
            </Form.Item>
            <Form.Item name="watermarkEnabled" valuePropName="checked" noStyle>
              <Checkbox>Đóng chữ chìm khi đọc trực tuyến</Checkbox>
            </Form.Item>
          </Space>
        </Form>
      </Drawer>

      <Modal
        open={collectionOpen}
        title="Thêm bộ sưu tập"
        okText="Lưu"
        cancelText="Hủy"
        confirmLoading={saveCollection.isPending}
        onCancel={() => setCollectionOpen(false)}
        onOk={() =>
          void collectionForm.validateFields().then((values) => saveCollection.mutate(values))
        }
      >
        <Form form={collectionForm} layout="vertical" initialValues={{ defaultAccessLevel: 'Internal' }}>
          <Form.Item
            name="code"
            label="Mã"
            rules={[{ required: true, message: 'Chưa nhập mã bộ sưu tập.' }]}
          >
            <Input />
          </Form.Item>
          <Form.Item
            name="name"
            label="Tên"
            rules={[{ required: true, message: 'Chưa nhập tên bộ sưu tập.' }]}
          >
            <Input />
          </Form.Item>
          <Form.Item name="parentId" label="Thuộc nhánh">
            <Select allowClear options={flatten(collections.data ?? [])} />
          </Form.Item>
          <Form.Item name="defaultAccessLevel" label="Mức truy cập mặc định">
            <Select options={accessOptions} />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}

function toTree(nodes: DigitalCollectionDto[]): DataNode[] {
  return nodes.map((node) => ({
    key: node.id,
    title: `${node.name} (${node.documentCount})`,
    children: node.children.length > 0 ? toTree(node.children) : undefined,
  }));
}

function flatten(nodes: DigitalCollectionDto[], depth = 0): { value: string; label: string }[] {
  return nodes.flatMap((node) => [
    { value: node.id, label: `${'— '.repeat(depth)}${node.name}` },
    ...flatten(node.children, depth + 1),
  ]);
}

function groupOf(mimeType: string): string {
  if (mimeType === 'application/pdf') return 'PDF';
  if (mimeType.startsWith('video/')) return 'VIDEO';
  if (mimeType.startsWith('audio/')) return 'AUDIO';
  if (mimeType.startsWith('image/')) return 'IMAGE';
  if (mimeType === 'application/epub+zip') return 'EPUB';
  if (mimeType.includes('officedocument') || mimeType.includes('word')) return 'OFFICE';
  return 'OTHER';
}
