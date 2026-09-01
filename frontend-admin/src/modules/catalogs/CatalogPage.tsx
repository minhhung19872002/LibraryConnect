import { useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  App,
  Button,
  Card,
  Input,
  Result,
  Select,
  Space,
  Table,
  Tag,
  Tooltip,
  TreeSelect,
  Typography,
} from 'antd';
import type { ColumnsType } from 'antd/es/table';
import {
  DeleteOutlined,
  DownloadOutlined,
  EditOutlined,
  ImportOutlined,
  MergeCellsOutlined,
  PlusOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import { errorMessage } from '@/api/formErrors';
import { PERMISSIONS } from '@/api/permissions';
import { FilterBar } from '@/components/FilterBar';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { usePagedQuery } from '@/hooks/usePagedQuery';
import { messages } from '@/i18n/messages';
import { downloadFile } from '@/modules/system/helpers';
import { CatalogFormDrawer } from './CatalogFormDrawer';
import { CatalogImportModal } from './CatalogImportModal';
import { CatalogMergeDrawer } from './CatalogMergeDrawer';
import { buildTreeSelectData } from './treeUtils';
import { ReferenceLabel } from './ReferenceSelect';
import { CATALOG_COLUMN_WIDTHS, catalogScrollX } from './catalogColumns';
import type { CatalogItem, CatalogMetadata, CatalogTreeNode } from './types';

/**
 * Một màn hình duy nhất phục vụ toàn bộ danh mục nghiệp vụ.
 *
 * The screen is built from the metadata the backend publishes for each catalogue, so a list added to
 * the registry appears here complete with its own columns, its own edit form and its own Excel
 * template — without a line of screen code per catalogue.
 */
export function CatalogPage() {
  const { catalog = '' } = useParams<{ catalog: string }>();
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [keyword, setKeyword] = useState('');
  const [isActive, setIsActive] = useState<boolean | undefined>();
  const [parentId, setParentId] = useState<string | undefined>();

  const [editing, setEditing] = useState<CatalogItem | null>(null);
  const [creating, setCreating] = useState(false);
  const [importOpen, setImportOpen] = useState(false);
  const [mergeOpen, setMergeOpen] = useState(false);
  const [exporting, setExporting] = useState(false);

  const metadata = useQuery({
    queryKey: ['catalog-metadata', catalog],
    queryFn: () => api.get<CatalogMetadata>(`/catalogs/${catalog}/metadata`),
    enabled: catalog.length > 0,
  });

  const list = usePagedQuery<CatalogItem, { isActive?: boolean; parentId?: string }>({
    queryKey: `catalog-items-${catalog}`,
    url: `/catalogs/${catalog}/items`,
    enabled: catalog.length > 0,
  });

  const tree = useQuery({
    queryKey: ['catalog-tree', catalog],
    queryFn: () => api.get<CatalogTreeNode[]>(`/catalogs/${catalog}/tree`),
    enabled: metadata.data?.isHierarchical === true,
  });

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: [`catalog-items-${catalog}`] });
    await queryClient.invalidateQueries({ queryKey: ['catalog-tree', catalog] });
  };

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/catalogs/${catalog}/items/${id}`),
    onSuccess: async () => {
      message.success(messages.notify.deleteSuccess);
      await invalidate();
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const handleExport = async () => {
    setExporting(true);

    try {
      const { blob, fileName } = await api.download(`/catalogs/${catalog}/export`);
      downloadFile(blob, fileName);
      message.success('Đã tải tệp xuống.');
    } catch (error) {
      message.error(errorMessage(error));
    } finally {
      setExporting(false);
    }
  };

  const beNgangBang = useMemo(() => {
    const definition = metadata.data;

    return definition
      ? catalogScrollX({
          coCotMa: definition.showCode,
          coCotTenTiengAnh: definition.showNameEn,
          soCotRieng: definition.fields.filter((item) => item.showInList).length,
        })
      : 1100;
  }, [metadata.data]);

  const columns = useMemo<ColumnsType<CatalogItem>>(() => {
    const definition = metadata.data;
    if (!definition) {
      return [];
    }

    const result: ColumnsType<CatalogItem> = [];

    if (definition.showCode) {
      result.push({
        title: 'Mã',
        dataIndex: 'code',
        width: CATALOG_COLUMN_WIDTHS.ma,
        sorter: true,
      });
    }

    result.push({
      title: 'Tên',
      dataIndex: 'name',
      // Khai bề rộng chứ không để trống. Danh mục tác giả khai thêm sáu cột riêng, nên nếu cột này
      // chỉ nhận phần còn thừa thì phần thừa bằng không và cột co lại đúng 0 px (lỗi E1).
      width: CATALOG_COLUMN_WIDTHS.ten,
      sorter: true,
      render: (name: string, record) => (
        <Space direction="vertical" size={0}>
          <Typography.Text>{name}</Typography.Text>
          {definition.isHierarchical && record.parentName && (
            <Typography.Text type="secondary" className="lc-small">
              Thuộc: {record.parentName}
            </Typography.Text>
          )}
        </Space>
      ),
    });

    if (definition.showNameEn) {
      result.push({
        title: 'Tên tiếng Anh',
        dataIndex: 'nameEn',
        width: CATALOG_COLUMN_WIDTHS.tenTiengAnh,
        responsive: ['lg'],
        ellipsis: true,
      });
    }

    // Each catalogue contributes its own columns, exactly the ones it declared for the list view.
    for (const field of definition.fields.filter((item) => item.showInList)) {
      result.push({
        title: field.label,
        key: field.key,
        width:
          field.type === 'Boolean'
            ? CATALOG_COLUMN_WIDTHS.cotRiengKieuDungSai
            : CATALOG_COLUMN_WIDTHS.cotRieng,
        ellipsis: true,
        render: (_, record) =>
          field.type === 'Reference' ? (
            <ReferenceLabel catalog={field.referenceCatalog ?? ''} value={record.extras[field.key]} />
          ) : (
            renderExtra(record.extras[field.key], field.type, field.options)
          ),
      });
    }

    result.push(
      {
        title: 'Thứ tự',
        dataIndex: 'sortOrder',
        width: CATALOG_COLUMN_WIDTHS.thuTu,
        align: 'right',
        responsive: ['xl'],
      },
      {
        title: 'Trạng thái',
        dataIndex: 'isActive',
        width: CATALOG_COLUMN_WIDTHS.trangThai,
        render: (active: boolean) => (active ? <Tag color="green">Đang dùng</Tag> : <Tag>Ngưng dùng</Tag>),
      },
      {
        title: 'Thao tác',
        key: 'actions',
        width: CATALOG_COLUMN_WIDTHS.thaoTac,
        fixed: 'right',
        render: (_, record) => (
          <Space size={2}>
            <Can permission={PERMISSIONS.catalogList.update}>
              <Tooltip title={messages.actions.edit}>
                <Button type="link" size="small" icon={<EditOutlined />} onClick={() => setEditing(record)} />
              </Tooltip>
            </Can>
            <Can permission={PERMISSIONS.catalogList.delete}>
              <Tooltip title={messages.actions.delete}>
                <Button
                  type="link"
                  size="small"
                  danger
                  icon={<DeleteOutlined />}
                  onClick={() =>
                    modal.confirm({
                      title: messages.confirm.deleteTitle,
                      content: `Xóa "${record.name}"? ${messages.confirm.deleteContent}`,
                      okText: messages.confirm.yes,
                      cancelText: messages.confirm.no,
                      okButtonProps: { danger: true },
                      onOk: () => deleteMutation.mutateAsync(record.id),
                    })
                  }
                />
              </Tooltip>
            </Can>
          </Space>
        ),
      },
    );

    return result;
  }, [metadata.data, deleteMutation, modal]);

  if (metadata.isError) {
    return (
      <Result
        status="404"
        title="Không tìm thấy danh mục"
        subTitle={`Hệ thống không có danh mục với mã "${catalog}".`}
      />
    );
  }

  const definition = metadata.data;

  return (
    <div className="lc-page">
      <PageHeader
        title={definition?.pluralName ?? messages.menu.catalogs}
        description={definition?.description}
        actions={
          <Space wrap>
            {definition?.supportsMerge && (
              <Can permission={PERMISSIONS.catalogList.merge}>
                <Button icon={<MergeCellsOutlined />} onClick={() => setMergeOpen(true)}>
                  Gộp trùng
                </Button>
              </Can>
            )}
            <Can permission={PERMISSIONS.catalogList.export}>
              <Button icon={<DownloadOutlined />} loading={exporting} onClick={handleExport}>
                {messages.actions.exportExcel}
              </Button>
            </Can>
            <Can permission={PERMISSIONS.catalogList.import}>
              <Button icon={<ImportOutlined />} onClick={() => setImportOpen(true)}>
                {messages.actions.import}
              </Button>
            </Can>
            <Can permission={PERMISSIONS.catalogList.create}>
              <Button type="primary" icon={<PlusOutlined />} onClick={() => setCreating(true)}>
                {messages.actions.create}
              </Button>
            </Can>
          </Space>
        }
      />

      <FilterBar
        loading={list.isFetching}
        onSearch={() => list.applyFilter({ keyword, isActive, parentId })}
        onReset={() => {
          setKeyword('');
          setIsActive(undefined);
          setParentId(undefined);
          list.resetFilter();
        }}
      >
        <Input
          allowClear
          placeholder="Tìm theo mã hoặc tên"
          value={keyword}
          onChange={(event) => setKeyword(event.target.value)}
          style={{ width: 320 }}
        />

        {definition?.isHierarchical && (
          <TreeSelect
            allowClear
            showSearch
            treeNodeFilterProp="title"
            placeholder="Thuộc cấp trên"
            style={{ width: 280 }}
            value={parentId}
            onChange={setParentId}
            treeData={buildTreeSelectData(tree.data ?? [])}
          />
        )}

        <Select
          allowClear
          placeholder="Trạng thái"
          value={isActive}
          onChange={setIsActive}
          style={{ width: 170 }}
          options={[
            { value: true, label: 'Đang dùng' },
            { value: false, label: 'Ngưng dùng' },
          ]}
        />
      </FilterBar>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<CatalogItem>
          rowKey="id"
          columns={columns}
          dataSource={list.items}
          loading={list.isLoading || metadata.isLoading}
          pagination={list.pagination}
          onChange={list.handleTableChange}
          // Cuộn ngang theo đúng tổng bề rộng đã khai, không bóp cột nào.
          scroll={{ x: beNgangBang }}
          size="middle"
          locale={{ emptyText: messages.table.empty }}
        />
      </Card>

      {definition && (
        <CatalogFormDrawer
          open={creating || editing !== null}
          catalog={catalog}
          metadata={definition}
          item={editing}
          tree={tree.data ?? []}
          onClose={() => {
            setCreating(false);
            setEditing(null);
          }}
          onSaved={async () => {
            setCreating(false);
            setEditing(null);
            await invalidate();
          }}
        />
      )}

      {definition && importOpen && (
        <CatalogImportModal
          catalog={catalog}
          metadata={definition}
          onClose={() => setImportOpen(false)}
          onImported={invalidate}
        />
      )}

      {definition && mergeOpen && (
        <CatalogMergeDrawer
          catalog={catalog}
          metadata={definition}
          onClose={() => setMergeOpen(false)}
          onMerged={invalidate}
        />
      )}
    </div>
  );
}

/** Renders one catalogue-specific value according to the type the backend declared for it. */
function renderExtra(
  value: string | null | undefined,
  type: string,
  options: { value: string; label: string }[],
) {
  if (value === null || value === undefined || value === '') {
    return <Typography.Text type="secondary">—</Typography.Text>;
  }

  if (type === 'Boolean') {
    return value === 'true' ? <Tag color="blue">Có</Tag> : <Tag>Không</Tag>;
  }

  if (type === 'Select') {
    return options.find((option) => option.value === value)?.label ?? value;
  }

  if (type === 'Decimal' || type === 'Number') {
    const parsed = Number(value);
    return Number.isNaN(parsed) ? value : parsed.toLocaleString('vi-VN');
  }

  return value;
}
