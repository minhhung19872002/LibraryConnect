import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Checkbox,
  Descriptions,
  Drawer,
  Form,
  Input,
  Select,
  Space,
  Table,
  Tabs,
  Tag,
  Typography,
} from 'antd';
import {
  CloudDownloadOutlined,
  DeleteOutlined,
  EditOutlined,
  PlusOutlined,
  QuestionCircleOutlined,
} from '@ant-design/icons';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { useCatalogOptions, toOptions } from '@/modules/cataloging/useCatalogOptions';
import { interLibraryApi } from './api';
import {
  formatDateTime,
  harvestStatusColors,
  harvestStatusLabels,
  metadataPrefixOptions,
} from './labels';
import type { OaiHarvestLogDto, OaiIdentifyDto, OaiRepositoryDto } from './types';

/**
 * Mục 3.4 — Kho OAI-PMH: khai nguồn, hỏi thử xem nguồn tự khai những gì, chạy thu hoạch và xem
 * nhật ký từng lần chạy.
 *
 * Biểu ghi thu về đi thẳng vào hàng đợi biên mục chứ không xuất bản ngay, vì Dublin Core nghèo hơn
 * MARC nhiều — cán bộ còn phải hiệu đính.
 */
export function OaiRepositoriesPage() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [editing, setEditing] = useState<OaiRepositoryDto | null>(null);
  const [creating, setCreating] = useState(false);
  const [identify, setIdentify] = useState<OaiIdentifyDto | null>(null);
  const [logPage, setLogPage] = useState({ page: 1, pageSize: 20 });
  const [form] = Form.useForm();

  const documentTypes = useCatalogOptions('document-types');

  const repositories = useQuery({
    queryKey: ['oai-repositories'],
    queryFn: () => interLibraryApi.repositories(true),
  });

  const logs = useQuery({
    queryKey: ['oai-harvest-logs', logPage],
    queryFn: () => interLibraryApi.harvestLogs(logPage),
    placeholderData: keepPreviousData,
  });

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      interLibraryApi.saveRepository(values, editing?.id),
    onSuccess: () => {
      message.success('Đã lưu kho OAI-PMH.');
      close();
      void queryClient.invalidateQueries({ queryKey: ['oai-repositories'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  const remove = useMutation({
    mutationFn: (id: string) => interLibraryApi.deleteRepository(id),
    onSuccess: () => {
      message.success('Đã xóa kho.');
      void queryClient.invalidateQueries({ queryKey: ['oai-repositories'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xóa được.'),
  });

  const askIdentify = useMutation({
    mutationFn: (baseUrl: string) => interLibraryApi.identify(baseUrl),
    onSuccess: (result) => {
      setIdentify(result);
      message.success(`Kho "${result.repositoryName}" trả lời tốt.`);
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không hỏi được kho.'),
  });

  const harvest = useMutation({
    mutationFn: ({ id, fullReload }: { id: string; fullReload: boolean }) =>
      interLibraryApi.harvest(id, fullReload),
    onSuccess: (log) => {
      if (log.status === 'Failed') {
        message.error(`Thu hoạch thất bại: ${log.errors ?? 'không rõ lý do'}`);
      } else {
        message.success(
          `Lấy về ${log.recordsFetched} biểu ghi, nhập ${log.recordsImported}, bỏ qua ${log.recordsSkipped}.`,
        );
      }

      void queryClient.invalidateQueries({ queryKey: ['oai-repositories'] });
      void queryClient.invalidateQueries({ queryKey: ['oai-harvest-logs'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không thu hoạch được.'),
  });

  const close = () => {
    setEditing(null);
    setCreating(false);
    setIdentify(null);
    form.resetFields();
  };

  const open = (repository: OaiRepositoryDto | null) => {
    setEditing(repository);
    setCreating(repository === null);
    setIdentify(null);

    form.setFieldsValue(
      repository ?? { metadataPrefix: 'oai_dc', isActive: true },
    );
  };

  const columns: ColumnsType<OaiRepositoryDto> = [
    {
      title: 'Tên kho',
      dataIndex: 'name',
      width: 260,
      render: (name: string, row) => (
        <Space direction="vertical" size={0}>
          <span>{name}</span>
          <Typography.Text type="secondary" style={{ fontSize: 12 }} copyable>
            {row.baseUrl}
          </Typography.Text>
        </Space>
      ),
    },
    { title: 'Định dạng', dataIndex: 'metadataPrefix', width: 120 },
    {
      title: 'Bộ',
      dataIndex: 'setSpec',
      width: 170,
      render: (value: string | null) => value ?? <Typography.Text type="secondary">Toàn kho</Typography.Text>,
    },
    {
      title: 'Dạng tài liệu gán cho biểu ghi',
      dataIndex: 'defaultDocumentTypeName',
      width: 220,
      ellipsis: true,
    },
    {
      title: 'Thu hoạch lần cuối',
      dataIndex: 'lastHarvestAt',
      width: 170,
      render: (value: string | null) =>
        value ? formatDateTime(value) : <Typography.Text type="secondary">Chưa chạy</Typography.Text>,
    },
    {
      title: 'Trạng thái',
      dataIndex: 'isActive',
      width: 110,
      render: (active: boolean) => (active ? <Tag color="green">Đang bật</Tag> : <Tag>Đã tắt</Tag>),
    },
    {
      title: '',
      width: 260,
      render: (_, row) => (
        <Space size={2}>
          <Can permission={PERMISSIONS.interlibrary.oaiHarvest}>
            <Button
              type="link"
              size="small"
              icon={<CloudDownloadOutlined />}
              loading={harvest.isPending && harvest.variables?.id === row.id}
              onClick={() => harvest.mutate({ id: row.id, fullReload: false })}
            >
              Thu hoạch
            </Button>
          </Can>
          <Can permission={PERMISSIONS.interlibrary.oaiHarvest}>
            <Button
              type="link"
              size="small"
              onClick={() =>
                modal.confirm({
                  title: 'Nạp lại từ đầu?',
                  content:
                    'Bỏ qua mốc thời gian lần trước và kéo lại toàn bộ kho. Biểu ghi đã có sẽ được bỏ qua chứ không nhân đôi.',
                  okText: 'Nạp lại',
                  cancelText: 'Hủy',
                  onOk: () => harvest.mutateAsync({ id: row.id, fullReload: true }),
                })
              }
            >
              Nạp lại
            </Button>
          </Can>
          <Can permission={PERMISSIONS.interlibrary.oaiManage}>
            <Button type="link" size="small" icon={<EditOutlined />} onClick={() => open(row)} />
          </Can>
          <Can permission={PERMISSIONS.interlibrary.oaiManage}>
            <Button
              type="link"
              size="small"
              danger
              icon={<DeleteOutlined />}
              onClick={() =>
                modal.confirm({
                  title: `Xóa kho "${row.name}"?`,
                  okText: 'Xóa',
                  cancelText: 'Hủy',
                  onOk: () => remove.mutateAsync(row.id),
                })
              }
            />
          </Can>
        </Space>
      ),
    },
  ];

  const logColumns: ColumnsType<OaiHarvestLogDto> = [
    { title: 'Kho', dataIndex: 'repositoryName', width: 240, ellipsis: true },
    { title: 'Bắt đầu', dataIndex: 'startedAt', width: 170, render: formatDateTime },
    { title: 'Kết thúc', dataIndex: 'finishedAt', width: 170, render: formatDateTime },
    { title: 'Lấy về', dataIndex: 'recordsFetched', width: 100, align: 'right' },
    {
      title: 'Nhập được',
      dataIndex: 'recordsImported',
      width: 110,
      align: 'right',
      render: (value: number) => <Typography.Text type="success">{value}</Typography.Text>,
    },
    { title: 'Bỏ qua', dataIndex: 'recordsSkipped', width: 100, align: 'right' },
    {
      title: 'Kết quả',
      dataIndex: 'status',
      width: 130,
      render: (status: string) => (
        <Tag color={harvestStatusColors[status] ?? 'default'}>
          {harvestStatusLabels[status] ?? status}
        </Tag>
      ),
    },
    { title: 'Lỗi', dataIndex: 'errors', ellipsis: true },
  ];

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Kho OAI-PMH"
        description="Thu hoạch metadata thư mục từ kho của nơi khác về, theo lịch hoặc chạy tay."
        actions={
          <Can permission={PERMISSIONS.interlibrary.oaiManage}>
            <Button type="primary" icon={<PlusOutlined />} onClick={() => open(null)}>
              Thêm kho
            </Button>
          </Can>
        }
      />

      <Tabs
        items={[
          {
            key: 'repositories',
            label: 'Kho đã khai báo',
            children: (
              <Space direction="vertical" size={12} style={{ width: '100%' }}>
                <Alert
                  type="info"
                  showIcon
                  message="Biểu ghi thu về nằm ở hàng đợi biên mục"
                  description="Dublin Core chỉ có 15 phần tử, nghèo hơn MARC nhiều, nên biểu ghi thu về là bản nháp để cán bộ hiệu đính chứ không phải bản thay thế cho biên mục thật."
                />

                <Table
                  rowKey="id"
                  size="small"
                  loading={repositories.isFetching}
                  dataSource={repositories.data ?? []}
                  columns={columns}
                  scroll={{ x: 1500 }}
                  pagination={false}
                />
              </Space>
            ),
          },
          {
            key: 'logs',
            label: 'Nhật ký thu hoạch',
            children: (
              <Table
                rowKey="id"
                size="small"
                loading={logs.isFetching}
                dataSource={logs.data?.items ?? []}
                columns={logColumns}
                scroll={{ x: 1300 }}
                pagination={{
                  current: logs.data?.page ?? 1,
                  pageSize: logs.data?.pageSize ?? 20,
                  total: logs.data?.totalCount ?? 0,
                  showSizeChanger: true,
                  showTotal: (total) => `Tổng ${total.toLocaleString('vi-VN')} lần chạy`,
                }}
                onChange={(pagination) =>
                  setLogPage({
                    page: pagination.current ?? 1,
                    pageSize: pagination.pageSize ?? 20,
                  })
                }
              />
            ),
          },
        ]}
      />

      <Drawer
        open={editing !== null || creating}
        onClose={close}
        width={560}
        title={editing ? `Sửa "${editing.name}"` : 'Thêm kho OAI-PMH'}
        extra={
          <Space>
            <Button onClick={close}>Hủy</Button>
            <Button
              type="primary"
              loading={save.isPending}
              onClick={() => void form.validateFields().then((values) => save.mutate(values))}
            >
              Lưu
            </Button>
          </Space>
        }
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="name"
            label="Tên kho"
            rules={[{ required: true, message: 'Chưa nhập tên kho.' }]}
          >
            <Input placeholder="Ví dụ: Kho luận văn Đại học Quốc gia" />
          </Form.Item>

          <Form.Item
            name="baseUrl"
            label="Địa chỉ kho"
            rules={[{ required: true, message: 'Chưa nhập địa chỉ kho.' }]}
            extra="Địa chỉ gốc của giao thức, thường kết thúc bằng /oai."
          >
            <Input placeholder="https://thuvien.edu.vn/oai" />
          </Form.Item>

          <Can permission={PERMISSIONS.interlibrary.oaiManage}>
            <Button
              icon={<QuestionCircleOutlined />}
              loading={askIdentify.isPending}
              style={{ marginBottom: 16 }}
              onClick={() => {
                const baseUrl = form.getFieldValue('baseUrl') as string | undefined;

                if (!baseUrl) {
                  message.warning('Nhập địa chỉ kho trước đã.');
                  return;
                }

                askIdentify.mutate(baseUrl);
              }}
            >
              Hỏi thử kho này
            </Button>
          </Can>

          {identify && (
            <Descriptions
              size="small"
              column={1}
              bordered
              style={{ marginBottom: 16 }}
              items={[
                { key: 'name', label: 'Tên kho', children: identify.repositoryName },
                { key: 'version', label: 'Phiên bản giao thức', children: identify.protocolVersion },
                {
                  key: 'earliest',
                  label: 'Biểu ghi cũ nhất',
                  children: identify.earliestDatestamp ?? '—',
                },
                {
                  key: 'formats',
                  label: 'Định dạng hỗ trợ',
                  children: identify.metadataPrefixes.join(', '),
                },
                {
                  key: 'sets',
                  label: 'Bộ có thể lọc',
                  children: identify.sets.length > 0 ? identify.sets.join(', ') : 'Kho không chia bộ',
                },
              ]}
            />
          )}

          <Form.Item name="metadataPrefix" label="Định dạng metadata">
            <Select options={metadataPrefixOptions} />
          </Form.Item>

          <Form.Item
            name="setSpec"
            label="Bộ cần lấy"
            extra="Bỏ trống thì lấy toàn kho. Bấm Hỏi thử ở trên để biết kho có những bộ nào."
          >
            <Input placeholder="Ví dụ: doctype:LUANVAN" />
          </Form.Item>

          <Form.Item
            name="defaultDocumentTypeId"
            label="Dạng tài liệu gán cho biểu ghi thu về"
            extra="Dublin Core không nói rõ dạng tài liệu, nên gán sẵn để biểu ghi vào đúng nhóm."
          >
            <Select allowClear showSearch optionFilterProp="label" options={toOptions(documentTypes.data)} />
          </Form.Item>

          <Form.Item name="isActive" valuePropName="checked">
            <Checkbox>Đang dùng (được thu hoạch theo lịch)</Checkbox>
          </Form.Item>
        </Form>
      </Drawer>
    </Space>
  );
}
