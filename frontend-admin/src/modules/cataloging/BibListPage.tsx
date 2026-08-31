import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  App,
  Button,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import { DeleteOutlined, DownloadOutlined, EditOutlined, EyeOutlined, PlusOutlined } from '@ant-design/icons';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { PageHeader } from '@/components/PageHeader';
import { FilterBar } from '@/components/FilterBar';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { errorMessage } from '@/api/formErrors';
import { usePagedQuery } from '@/hooks/usePagedQuery';
import { catalogingApi, importApi, type BibListParams } from './api';
import { saveBlob } from '@/modules/marc/api';
import { useCatalogOptions, toOptions } from './useCatalogOptions';
import {
  BIB_SOURCE_LABELS,
  RECORD_STATUS_LABELS,
  type BibListItem,
  type RecordStatus,
} from './types';

const MONOSPACE = { fontFamily: 'ui-monospace, Consolas, monospace' } as const;

/**
 * Danh sách biểu ghi thư mục (II.3).
 *
 * The filter strip carries the questions a cataloguer actually asks — which titles have no copies
 * registered yet, what came in from an import, what was published in a given range — rather than one
 * box per column. Search is accent-insensitive on the server, so typing without diacritics works.
 */
export function BibListPage() {
  const navigate = useNavigate();
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [keyword, setKeyword] = useState('');
  const [filter, setFilter] = useState<BibListParams>({});
  const [selected, setSelected] = useState<string[]>([]);

  const documentTypes = useCatalogOptions('document-types');
  const languages = useCatalogOptions('languages');

  const list = usePagedQuery<BibListItem, BibListParams>({
    queryKey: 'bib-records',
    url: '/cataloging/bibs',
  });

  const remove = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => catalogingApi.remove(id, reason),
    onSuccess: async () => {
      message.success('Đã xóa biểu ghi.');
      await queryClient.invalidateQueries({ queryKey: ['bib-records'] });
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const exportRecords = useMutation({
    mutationFn: (format: 'iso2709' | 'marcxml') =>
      // With rows ticked, those rows are exported; with none, the export follows the filter the
      // librarian is looking at, which is what "xuất theo bộ lọc" means to them.
      importApi.export(selected, selected.length > 0 ? undefined : { ...filter, keyword: keyword.trim() }, format),
    onSuccess: ({ blob, fileName }) => {
      saveBlob(blob, fileName);
      message.success(`Đã xuất tệp ${fileName}.`);
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const confirmDelete = (record: BibListItem) => {
    let reason = '';

    modal.confirm({
      title: `Xóa biểu ghi ${record.controlNumber}?`,
      width: 520,
      content: (
        <Space direction="vertical" style={{ width: '100%' }}>
          <Typography.Text>{record.title}</Typography.Text>
          <Typography.Text type="secondary">
            Biểu ghi được giữ lại trong cơ sở dữ liệu và không còn hiện trên các màn hình. Phải nhập
            lý do xóa.
          </Typography.Text>
          <Input.TextArea
            rows={2}
            placeholder="Lý do xóa, ví dụ: biểu ghi nhập trùng"
            onChange={(event) => {
              reason = event.target.value;
            }}
          />
        </Space>
      ),
      okText: 'Xóa',
      okButtonProps: { danger: true },
      cancelText: 'Không',
      onOk: async () => {
        if (!reason.trim()) {
          message.error('Phải nhập lý do xóa biểu ghi.');
          return Promise.reject(new Error('reason'));
        }

        return remove.mutateAsync({ id: record.id, reason: reason.trim() });
      },
    });
  };

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Biên mục"
        description="Biểu ghi thư mục theo khổ mẫu MARC 21. Tra cứu được cả khi gõ tiếng Việt không dấu."
        actions={
          <Space wrap>
            <Can permission={PERMISSIONS.cataloging.bibExport}>
              <Button
                icon={<DownloadOutlined />}
                loading={exportRecords.isPending}
                onClick={() => exportRecords.mutate('iso2709')}
              >
                {selected.length > 0 ? `Xuất ${selected.length} biểu ghi (.mrc)` : 'Xuất theo bộ lọc (.mrc)'}
              </Button>
            </Can>
            <Can permission={PERMISSIONS.cataloging.bibExport}>
              <Button
                icon={<DownloadOutlined />}
                loading={exportRecords.isPending}
                onClick={() => exportRecords.mutate('marcxml')}
              >
                MARCXML
              </Button>
            </Can>
            <Can permission={PERMISSIONS.cataloging.bibCreate}>
              <Button type="primary" icon={<PlusOutlined />} onClick={() => navigate('/bien-muc/moi')}>
                Biên mục mới
              </Button>
            </Can>
          </Space>
        }
      />

      <FilterBar
        loading={list.isFetching}
        onSearch={() => list.applyFilter({ ...filter, keyword: keyword.trim() })}
        onReset={() => {
          setKeyword('');
          setFilter({});
          list.resetFilter();
        }}
      >
        <Input
          value={keyword}
          onChange={(event) => setKeyword(event.target.value)}
          placeholder="Nhan đề, tác giả, nhà xuất bản, ISBN, số kiểm soát"
          allowClear
          style={{ width: 340 }}
        />

        <Select
          value={filter.documentTypeId}
          onChange={(value) => setFilter((current) => ({ ...current, documentTypeId: value }))}
          options={toOptions(documentTypes.data)}
          placeholder="Dạng tài liệu"
          allowClear
          showSearch
          optionFilterProp="label"
          style={{ width: 200 }}
        />

        <Select
          value={filter.languageId}
          onChange={(value) => setFilter((current) => ({ ...current, languageId: value }))}
          options={toOptions(languages.data)}
          placeholder="Ngôn ngữ"
          allowClear
          showSearch
          optionFilterProp="label"
          style={{ width: 170 }}
        />

        <Select<RecordStatus>
          value={filter.status}
          onChange={(value) => setFilter((current) => ({ ...current, status: value }))}
          options={Object.entries(RECORD_STATUS_LABELS).map(([value, label]) => ({ value, label }))}
          placeholder="Trạng thái"
          allowClear
          style={{ width: 160 }}
        />

        <Space.Compact>
          <InputNumber
            value={filter.publishYearFrom}
            onChange={(value) => setFilter((current) => ({ ...current, publishYearFrom: value ?? undefined }))}
            placeholder="Năm từ"
            min={1400}
            max={2200}
            style={{ width: 110 }}
          />
          <InputNumber
            value={filter.publishYearTo}
            onChange={(value) => setFilter((current) => ({ ...current, publishYearTo: value ?? undefined }))}
            placeholder="đến"
            min={1400}
            max={2200}
            style={{ width: 100 }}
          />
        </Space.Compact>

        <Select
          value={
            filter.withoutItems ? 'without' : filter.availableOnly ? 'available' : undefined
          }
          onChange={(value) =>
            setFilter((current) => ({
              ...current,
              withoutItems: value === 'without' ? true : undefined,
              availableOnly: value === 'available' ? true : undefined,
            }))
          }
          options={[
            { value: 'without', label: 'Chưa có đăng ký cá biệt' },
            { value: 'available', label: 'Còn bản sẵn sàng cho mượn' },
          ]}
          placeholder="Tình trạng bản sách"
          allowClear
          style={{ width: 220 }}
        />
      </FilterBar>

      <Table<BibListItem>
        rowKey="id"
        dataSource={list.items}
        loading={list.isFetching}
        pagination={list.pagination}
        onChange={list.handleTableChange}
        size="small"
        rowSelection={{
          selectedRowKeys: selected,
          onChange: (keys) => setSelected(keys as string[]),
        }}
        columns={[
          {
            title: 'Số kiểm soát',
            dataIndex: 'controlNumber',
            width: 140,
            sorter: true,
            render: (value: string, record) => (
              <Link to={`/bien-muc/${record.id}`} style={MONOSPACE}>
                {value}
              </Link>
            ),
          },
          {
            title: 'Nhan đề',
            dataIndex: 'title',
            sorter: true,
            render: (value: string, record) => (
              <Space direction="vertical" size={0}>
                <Link to={`/bien-muc/${record.id}`}>
                  <Typography.Text strong>{value}</Typography.Text>
                </Link>
                {record.subtitle && (
                  <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                    {record.subtitle}
                  </Typography.Text>
                )}
              </Space>
            ),
          },
          { title: 'Tác giả', dataIndex: 'authorMain', width: 180, sorter: true },
          {
            title: 'Xuất bản',
            width: 220,
            render: (_, record) => (
              <Space direction="vertical" size={0}>
                <Typography.Text style={{ fontSize: 13 }}>{record.publisherName}</Typography.Text>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  {record.publishYear ?? ''}
                </Typography.Text>
              </Space>
            ),
          },
          { title: 'DDC', dataIndex: 'ddc', width: 90, sorter: true },
          { title: 'Dạng', dataIndex: 'documentTypeName', width: 130 },
          {
            title: 'Bản',
            dataIndex: 'itemCount',
            width: 90,
            align: 'right',
            sorter: true,
            render: (value: number, record) =>
              value === 0 ? (
                <Tooltip title="Chưa đăng ký cá biệt cho biểu ghi này">
                  <Tag color="orange">chưa có</Tag>
                </Tooltip>
              ) : (
                <Tooltip title={`${record.availableItemCount} bản đang sẵn sàng cho mượn`}>
                  <span>
                    {record.availableItemCount}/{value}
                  </span>
                </Tooltip>
              ),
          },
          {
            title: 'Nguồn',
            dataIndex: 'source',
            width: 140,
            render: (value: keyof typeof BIB_SOURCE_LABELS) => (
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                {BIB_SOURCE_LABELS[value] ?? value}
              </Typography.Text>
            ),
          },
          {
            title: '',
            width: 120,
            align: 'right',
            render: (_, record) => (
              <Space size={0}>
                <Tooltip title="Xem chi tiết">
                  <Button
                    type="text"
                    icon={<EyeOutlined />}
                    onClick={() => navigate(`/bien-muc/${record.id}`)}
                  />
                </Tooltip>
                <Can permission={PERMISSIONS.cataloging.bibUpdate}>
                  <Tooltip title="Sửa biểu ghi">
                    <Button
                      type="text"
                      icon={<EditOutlined />}
                      onClick={() => navigate(`/bien-muc/${record.id}/sua`)}
                    />
                  </Tooltip>
                </Can>
                <Can permission={PERMISSIONS.cataloging.bibDelete}>
                  <Tooltip title="Xóa biểu ghi">
                    <Button type="text" danger icon={<DeleteOutlined />} onClick={() => confirmDelete(record)} />
                  </Tooltip>
                </Can>
              </Space>
            ),
          },
        ]}
      />
    </Space>
  );
}

/** Hộp thoại nhỏ dùng lại cho các nơi cần nhập lý do trước khi xóa. */
export function ReasonModal({
  open,
  title,
  description,
  onCancel,
  onConfirm,
}: {
  open: boolean;
  title: string;
  description?: string;
  onCancel: () => void;
  onConfirm: (reason: string) => void | Promise<void>;
}) {
  const [reason, setReason] = useState('');
  const { message } = App.useApp();

  return (
    <Modal
      open={open}
      title={title}
      okText="Xóa"
      okButtonProps={{ danger: true }}
      cancelText="Không"
      onCancel={() => {
        setReason('');
        onCancel();
      }}
      onOk={async () => {
        if (!reason.trim()) {
          message.error('Phải nhập lý do.');
          return;
        }

        await onConfirm(reason.trim());
        setReason('');
      }}
    >
      <Space direction="vertical" style={{ width: '100%' }}>
        {description && <Typography.Text type="secondary">{description}</Typography.Text>}
        <Input.TextArea
          rows={2}
          value={reason}
          onChange={(event) => setReason(event.target.value)}
          placeholder="Lý do"
        />
      </Space>
    </Modal>
  );
}
