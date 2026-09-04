import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Checkbox,
  Col,
  DatePicker,
  Descriptions,
  Drawer,
  Dropdown,
  Form,
  Input,
  InputNumber,
  Modal,
  Row,
  Select,
  Space,
  Statistic,
  Table,
  Tag,
  Timeline,
  Typography,
} from 'antd';
import {
  BarcodeOutlined,
  DownOutlined,
  ExportOutlined,
  FileTextOutlined,
  LockOutlined,
  PrinterOutlined,
  SwapOutlined,
  TagOutlined,
  UnlockOutlined,
} from '@ant-design/icons';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import type { Dayjs } from 'dayjs';
import { PageHeader } from '@/components/PageHeader';
import { FilterBar } from '@/components/FilterBar';
import { Can } from '@/components/PermissionGate';
import { usePermission } from '@/hooks/usePermission';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { saveBlob } from '@/modules/marc/api';
import { useCatalogOptions, toOptions } from '@/modules/cataloging/useCatalogOptions';
import { formsApi, locationsApi, stockApi } from './api';
import { MAU } from '@/lib/palette';
import { printableFormFor, printedDocumentTitle, type PrintableFormType, type StockBulkAction } from './printing';
import { TransferSlipsDrawer } from './TransferSlipsDrawer';
import { LabelPreview } from './LabelPreview';
import { toLabelData } from './labelContent';
import {
  acquisitionTypeLabels,
  disposalTypes,
  formatDate,
  itemStatusColors,
  itemStatusLabels,
  money,
} from './labels';
import type {
  BulkItemResultDto,
  ItemStatus,
  StockItemDto,
  StockItemFilter,
} from './types';

type BulkAction = StockBulkAction;

const actionTitles: Record<BulkAction, string> = {
  shelve: 'Xếp giá',
  inspect: 'Kiểm nhận và mở khóa',
  lock: 'Khóa lưu thông',
  unlock: 'Mở khóa lưu thông',
  transfer: 'Chuyển kho',
  dispose: 'Thanh lý / ghi mất',
};

/**
 * III.5 — Quản lý ấn phẩm bổ sung.
 *
 * Một danh sách duy nhất, nhìn theo trạng thái xếp giá qua các thẻ đếm ở trên. Mọi thao tác đều là
 * thao tác hàng loạt vì công việc kho luôn đến theo lô: một chồng sách mới nhập, một giá phải dọn,
 * một quyết định thanh lý cho cả đợt.
 */
export function StockItemsPage() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();
  const { can } = usePermission();

  const [filter, setFilter] = useState<StockItemFilter>({});
  const [draft, setDraft] = useState<StockItemFilter>({});
  const [page, setPage] = useState<{
    page: number;
    pageSize: number;
    sortBy?: string;
    sortDescending?: boolean;
  }>({ page: 1, pageSize: 20 });
  const [selected, setSelected] = useState<string[]>([]);
  /** Áp dụng cho toàn bộ kết quả lọc thay vì chỉ các dòng đã tick. */
  const [applyToAll, setApplyToAll] = useState(false);

  const [action, setAction] = useState<BulkAction | null>(null);
  const [actionForm] = Form.useForm();
  const [detailId, setDetailId] = useState<string | null>(null);
  const [printKind, setPrintKind] = useState<'barcode' | 'label' | null>(null);
  const [printForm] = Form.useForm();
  const [slipsOpen, setSlipsOpen] = useState(false);
  /** Chứng từ vừa sinh ra từ thao tác chuyển kho / thanh lý, chờ người dùng bấm in. */
  const [printableDocument, setPrintableDocument] = useState<{
    formType: PrintableFormType;
    code: string;
  } | null>(null);

  const warehouses = useQuery({ queryKey: ['acq-warehouses', null], queryFn: () => locationsApi.warehouses() });
  const documentTypes = useCatalogOptions('document-types');
  const fundingSources = useCatalogOptions('funding-sources');

  const targetWarehouse = Form.useWatch('warehouseId', actionForm) as string | undefined;
  const transferWarehouse = Form.useWatch('toWarehouseId', actionForm) as string | undefined;
  const shelfWarehouse = targetWarehouse ?? transferWarehouse;

  const shelves = useQuery({
    queryKey: ['acq-shelves', shelfWarehouse],
    queryFn: () => locationsApi.shelves(shelfWarehouse),
    enabled: Boolean(shelfWarehouse),
  });

  const items = useQuery({
    queryKey: ['stock-items', page, filter],
    queryFn: () => stockApi.search({ ...page, filter }),
    placeholderData: keepPreviousData,
  });

  const summary = useQuery({
    queryKey: ['stock-summary', filter],
    queryFn: () => stockApi.summary(filter),
  });

  const detail = useQuery({
    queryKey: ['stock-item', detailId],
    queryFn: () => stockApi.item(detailId!),
    enabled: Boolean(detailId),
  });

  const barcodeTemplates = useQuery({
    queryKey: ['barcode-templates'],
    queryFn: () => stockApi.barcodeTemplates(),
    enabled: printKind === 'barcode',
  });

  const labelTemplates = useQuery({
    queryKey: ['label-templates'],
    queryFn: () => stockApi.labelTemplates(),
    enabled: printKind === 'label',
  });

  const selectionPayload = () =>
    applyToAll ? { itemIds: [], filter } : { itemIds: selected, filter: null };

  // Mẫu đang chọn trong hộp in (hoặc mẫu mặc định) và ấn phẩm đầu tiên đang chọn — đủ để mô phỏng
  // một tem với dữ liệu thật trước khi tạo tệp.
  const printTemplateId = Form.useWatch('templateId', printForm) as string | undefined;
  const printTemplates = printKind === 'barcode' ? barcodeTemplates.data : labelTemplates.data;
  const previewTemplate =
    printTemplates?.find((template) => template.id === printTemplateId) ??
    printTemplates?.find((template) => template.isDefault) ??
    printTemplates?.[0];
  const previewItem = applyToAll
    ? items.data?.items[0]
    : items.data?.items.find((item) => selected.includes(item.id));

  const selectionCount = applyToAll ? (items.data?.totalCount ?? 0) : selected.length;

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['stock-items'] });
    void queryClient.invalidateQueries({ queryKey: ['stock-summary'] });
    void queryClient.invalidateQueries({ queryKey: ['stock-item'] });
    setSelected([]);
    setApplyToAll(false);
  };

  const reportResult = (result: BulkItemResultDto, done: string) => {
    if (result.skipped.length === 0) {
      message.success(`${done} ${result.affected} bản.`);
      return;
    }

    modal.info({
      title: `${done} ${result.affected} bản, bỏ qua ${result.skipped.length} bản`,
      width: 620,
      content: (
        <Table
          rowKey="barcode"
          size="small"
          pagination={false}
          scroll={{ y: 260 }}
          dataSource={result.skipped}
          columns={[
            { title: 'Mã vạch', dataIndex: 'barcode', width: 150 },
            { title: 'Lý do bỏ qua', dataIndex: 'reason' },
          ]}
        />
      ),
    });
  };

  const runAction = useMutation({
    mutationFn: async (values: Record<string, unknown>) => {
      const payload = { ...selectionPayload(), ...values };

      switch (action) {
        case 'shelve':
          return { result: await stockApi.shelve(payload), done: 'Đã xếp giá' };
        case 'inspect':
          return { result: await stockApi.inspect(payload), done: 'Đã kiểm nhận' };
        case 'lock':
          return { result: await stockApi.setLock({ ...payload, isLocked: true }), done: 'Đã khóa' };
        case 'unlock':
          return {
            result: await stockApi.setLock({ ...payload, isLocked: false }),
            done: 'Đã mở khóa',
          };
        case 'transfer':
          return { result: await stockApi.transfer(payload), done: 'Đã chuyển' };
        default:
          return { result: await stockApi.dispose(payload), done: 'Đã xử lý' };
      }
    },
    onSuccess: ({ result, done }) => {
      const formType = action ? printableFormFor(action) : null;

      setAction(null);
      refresh();

      if (result.documentCode) {
        message.success(`${done} ${result.affected} bản. Số phiếu: ${result.documentCode}.`);
      }

      // Chuyển kho và thanh lý sinh chứng từ có số: mời in ngay, cán bộ đang cần tờ để ký.
      if (formType && result.documentCode && result.affected > 0) {
        setPrintableDocument({ formType, code: result.documentCode });
      }

      reportResult(result, done);
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không thực hiện được.'),
  });

  const printDocument = useMutation({
    mutationFn: ({ formType, code }: { formType: PrintableFormType; code: string }) =>
      formsApi.print(formType, code),
    onSuccess: ({ blob, fileName }) => {
      saveBlob(blob, fileName);
      message.success('Đã tạo tệp in.');
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không in được.'),
  });

  const print = useMutation({
    mutationFn: async (values: { templateId?: string; copies: number }) => {
      const payload = { ...selectionPayload(), ...values };

      return printKind === 'barcode'
        ? stockApi.printBarcodes(payload)
        : stockApi.printLabels(payload);
    },
    onSuccess: ({ blob, fileName }) => {
      saveBlob(blob, fileName);
      setPrintKind(null);
      message.success('Đã tạo tệp in.');
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không in được.'),
  });

  const exportItems = useMutation({
    mutationFn: () => stockApi.exportItems(filter, applyToAll ? undefined : selected),
    onSuccess: ({ blob, fileName }) => {
      saveBlob(blob, fileName);
      message.success('Đã xuất danh sách.');
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xuất được.'),
  });

  const openAction = (next: BulkAction) => {
    if (selectionCount === 0) {
      message.warning('Chưa chọn ấn phẩm nào.');
      return;
    }

    actionForm.resetFields();

    if (next === 'dispose') {
      actionForm.setFieldsValue({ disposalType: 'Thanh lý' });
    }

    setAction(next);
  };

  const columns: ColumnsType<StockItemDto> = [
    {
      title: 'Mã vạch',
      dataIndex: 'barcode',
      width: 140,
      sorter: true,
      render: (value: string, row) => (
        <Button type="link" size="small" onClick={() => setDetailId(row.id)} style={{ padding: 0 }}>
          {value}
        </Button>
      ),
    },
    { title: 'Số ĐKCB', dataIndex: 'registerNumber', width: 150, sorter: true },
    {
      title: 'Nhan đề',
      dataIndex: 'title',
      sorter: true,
      render: (value: string, row) => (
        <Space direction="vertical" size={0}>
          <span>{value}</span>
          {row.authorMain && (
            <Typography.Text type="secondary">{row.authorMain}</Typography.Text>
          )}
        </Space>
      ),
    },
    { title: 'Kho', dataIndex: 'warehouseName', width: 140 },
    {
      title: 'Vị trí giá',
      dataIndex: 'shelfName',
      width: 120,
      render: (value: string | null) => value ?? <Typography.Text type="secondary">Chưa xếp</Typography.Text>,
    },
    { title: 'Ký hiệu xếp giá', dataIndex: 'callNumber', width: 150, sorter: true },
    {
      title: 'Tình trạng',
      dataIndex: 'status',
      width: 150,
      render: (value: ItemStatus, row) => (
        <Space direction="vertical" size={0}>
          <Tag color={itemStatusColors[value]}>{itemStatusLabels[value]}</Tag>
          {row.isLocked && (
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              Khóa: {row.lockReason ?? 'không ghi lý do'}
            </Typography.Text>
          )}
        </Space>
      ),
    },
    {
      title: 'Giá bìa',
      dataIndex: 'price',
      width: 110,
      align: 'right',
      sorter: true,
      render: (value: number) => money(value),
    },
    {
      title: 'Ngày bổ sung',
      dataIndex: 'acquisitionDate',
      width: 130,
      sorter: true,
      render: (value: string) => formatDate(value),
    },
  ];

  return (
    <div className="lc-page">
      <PageHeader
        title="Ấn phẩm trong kho"
        description="Xếp giá, kiểm nhận, khóa lưu thông, chuyển kho, thanh lý và in tem — tất cả theo lô."
        actions={
          <Space>
            <Can permission={PERMISSIONS.acquisition.itemExport}>
              <Button
                icon={<ExportOutlined />}
                loading={exportItems.isPending}
                onClick={() => exportItems.mutate()}
              >
                Xuất Excel
              </Button>
            </Can>
            <Can permission={PERMISSIONS.acquisition.itemMove}>
              <Button icon={<FileTextOutlined />} onClick={() => setSlipsOpen(true)}>
                Phiếu chuyển kho
              </Button>
            </Can>
            <Can permission={PERMISSIONS.acquisition.itemPrintBarcode}>
              <Button icon={<BarcodeOutlined />} onClick={() => {
                if (selectionCount === 0) {
                  message.warning('Chưa chọn ấn phẩm nào.');
                  return;
                }
                printForm.setFieldsValue({ copies: 1 });
                setPrintKind('barcode');
              }}>
                In tem mã vạch
              </Button>
            </Can>
            <Can permission={PERMISSIONS.acquisition.itemPrintLabel}>
              <Button icon={<TagOutlined />} onClick={() => {
                if (selectionCount === 0) {
                  message.warning('Chưa chọn ấn phẩm nào.');
                  return;
                }
                printForm.setFieldsValue({ copies: 1 });
                setPrintKind('label');
              }}>
                In nhãn gáy
              </Button>
            </Can>
            <Dropdown
              menu={{
                items: [
                  {
                    key: 'shelve',
                    label: actionTitles.shelve,
                    disabled: !can(PERMISSIONS.acquisition.itemUpdate),
                  },
                  {
                    key: 'inspect',
                    label: actionTitles.inspect,
                    disabled: !can(PERMISSIONS.acquisition.itemInspect),
                  },
                  {
                    key: 'lock',
                    label: actionTitles.lock,
                    icon: <LockOutlined />,
                    disabled: !can(PERMISSIONS.acquisition.itemLock),
                  },
                  {
                    key: 'unlock',
                    label: actionTitles.unlock,
                    icon: <UnlockOutlined />,
                    disabled: !can(PERMISSIONS.acquisition.itemLock),
                  },
                  { type: 'divider' },
                  {
                    key: 'transfer',
                    label: actionTitles.transfer,
                    icon: <SwapOutlined />,
                    disabled: !can(PERMISSIONS.acquisition.itemMove),
                  },
                  {
                    key: 'dispose',
                    label: actionTitles.dispose,
                    danger: true,
                    disabled: !can(PERMISSIONS.acquisition.itemDispose),
                  },
                ],
                onClick: ({ key }) => openAction(key as BulkAction),
              }}
            >
              <Button type="primary">
                Thao tác hàng loạt <DownOutlined />
              </Button>
            </Dropdown>
          </Space>
        }
      />

      <Row gutter={12} style={{ marginBottom: 12 }}>
        {[
          { title: 'Chưa kiểm nhận', value: summary.data?.pendingInspection, status: 'PendingInspection' as ItemStatus },
          { title: 'Trong kho', value: summary.data?.inStock, status: 'InStock' as ItemStatus },
          { title: 'Đang mượn', value: summary.data?.onLoan, status: 'OnLoan' as ItemStatus },
          { title: 'Thanh lý', value: summary.data?.discarded, status: 'Discarded' as ItemStatus },
          { title: 'Mất', value: summary.data?.lost, status: 'Lost' as ItemStatus },
        ].map((card) => (
          <Col key={card.title} span={4}>
            <Card
              size="small"
              hoverable
              onClick={() => {
                const next = { ...filter, status: filter.status === card.status ? null : card.status };
                setDraft(next);
                setFilter(next);
                setPage((current) => ({ ...current, page: 1 }));
              }}
              style={filter.status === card.status ? { borderColor: MAU.chinh } : undefined}
            >
              <Statistic title={card.title} value={card.value ?? 0} />
            </Card>
          </Col>
        ))}
        <Col span={4}>
          <Card size="small">
            <Statistic title="Chưa xếp giá" value={summary.data?.unshelved ?? 0} />
          </Card>
        </Col>
      </Row>

      <FilterBar
        loading={items.isFetching}
        onSearch={() => {
          setFilter(draft);
          setPage((current) => ({ ...current, page: 1 }));
        }}
        onReset={() => {
          setDraft({});
          setFilter({});
          setPage({ page: 1, pageSize: 20 });
        }}
      >
        <Input
          allowClear
          placeholder="Mã vạch, số ĐKCB, nhan đề, ISBN, ký hiệu xếp giá"
          style={{ width: 320 }}
          value={draft.keyword ?? ''}
          onChange={(event) => setDraft({ ...draft, keyword: event.target.value })}
        />
        <Select
          allowClear
          placeholder="Kho"
          style={{ width: 180 }}
          value={draft.warehouseId ?? undefined}
          onChange={(value) => setDraft({ ...draft, warehouseId: value ?? null })}
          options={(warehouses.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
        />
        <Select
          allowClear
          placeholder="Tình trạng"
          style={{ width: 170 }}
          value={draft.status ?? undefined}
          onChange={(value) => setDraft({ ...draft, status: value ?? null })}
          options={Object.entries(itemStatusLabels).map(([value, label]) => ({ value, label }))}
        />
        <Select
          allowClear
          placeholder="Dạng tài liệu"
          style={{ width: 180 }}
          value={draft.documentTypeId ?? undefined}
          onChange={(value) => setDraft({ ...draft, documentTypeId: value ?? null })}
          options={toOptions(documentTypes.data)}
        />
        <Select
          allowClear
          placeholder="Nguồn kinh phí"
          style={{ width: 180 }}
          value={draft.fundingSourceId ?? undefined}
          onChange={(value) => setDraft({ ...draft, fundingSourceId: value ?? null })}
          options={toOptions(fundingSources.data)}
        />
        <Select
          allowClear
          placeholder="Khóa lưu thông"
          style={{ width: 160 }}
          value={draft.isLocked === null || draft.isLocked === undefined ? undefined : String(draft.isLocked)}
          onChange={(value) =>
            setDraft({ ...draft, isLocked: value === undefined ? null : value === 'true' })
          }
          options={[
            { value: 'true', label: 'Đang khóa' },
            { value: 'false', label: 'Không khóa' },
          ]}
        />
        <Input
          allowClear
          placeholder="Số ĐKCB từ"
          style={{ width: 150 }}
          value={draft.registerFrom ?? ''}
          onChange={(event) => setDraft({ ...draft, registerFrom: event.target.value })}
        />
        <Input
          allowClear
          placeholder="đến"
          style={{ width: 150 }}
          value={draft.registerTo ?? ''}
          onChange={(event) => setDraft({ ...draft, registerTo: event.target.value })}
        />
        <DatePicker.RangePicker
          format="DD/MM/YYYY"
          placeholder={['Bổ sung từ', 'đến']}
          onChange={(range) =>
            setDraft({
              ...draft,
              acquiredFrom: range?.[0] ? (range[0] as Dayjs).format('YYYY-MM-DD') : null,
              acquiredTo: range?.[1] ? (range[1] as Dayjs).format('YYYY-MM-DD') : null,
            })
          }
        />
      </FilterBar>

      {selectionCount > 0 && (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message={
            <Space>
              <span>Đang chọn {selectionCount} ấn phẩm.</span>
              <Checkbox checked={applyToAll} onChange={(event) => setApplyToAll(event.target.checked)}>
                Áp dụng cho toàn bộ {items.data?.totalCount ?? 0} kết quả của bộ lọc
              </Checkbox>
            </Space>
          }
        />
      )}

      <Card variant="borderless">
        <Table
          rowKey="id"
          size="small"
          loading={items.isFetching}
          columns={columns}
          dataSource={items.data?.items ?? []}
          scroll={{ x: 1400 }}
          rowSelection={{
            selectedRowKeys: selected,
            onChange: (keys) => {
              setSelected(keys as string[]);
              setApplyToAll(false);
            },
          }}
          pagination={{
            current: items.data?.page ?? 1,
            pageSize: items.data?.pageSize ?? 20,
            total: items.data?.totalCount ?? 0,
            showSizeChanger: true,
            showTotal: (total) => `Tổng ${total} bản`,
          }}
          onChange={(pagination, _filters, sorter) => {
            const single = Array.isArray(sorter) ? sorter[0] : sorter;

            setPage({
              page: pagination.current ?? 1,
              pageSize: pagination.pageSize ?? 20,
              sortBy: single?.order ? String(single.field) : undefined,
              sortDescending: single?.order === 'descend',
            });
          }}
        />
      </Card>

      <Modal
        open={action !== null}
        title={action ? `${actionTitles[action]} — ${selectionCount} ấn phẩm` : ''}
        onCancel={() => setAction(null)}
        onOk={() => actionForm.submit()}
        confirmLoading={runAction.isPending}
        okText="Thực hiện"
        cancelText="Bỏ qua"
        destroyOnHidden
      >
        <Form form={actionForm} layout="vertical" onFinish={(values) => runAction.mutate(values)}>
          {action === 'shelve' && (
            <>
              <Form.Item
                name="warehouseId"
                label="Kho"
                rules={[{ required: true, message: 'Chưa chọn kho.' }]}
              >
                <Select
                  options={(warehouses.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
                />
              </Form.Item>
              <Form.Item name="shelfId" label="Vị trí giá">
                <Select
                  allowClear
                  disabled={!shelfWarehouse}
                  options={(shelves.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
                />
              </Form.Item>
              <Form.Item
                name="regenerateCallNumber"
                valuePropName="checked"
                extra="Sinh lại theo quy tắc của kho đích. Chuyển giá trong cùng kho thì thường không cần."
              >
                <Checkbox>Sinh lại ký hiệu xếp giá</Checkbox>
              </Form.Item>
              <Form.Item name="callNumber" label="Hoặc đặt tay một ký hiệu chung cho cả lô">
                <Input placeholder="005.74 NGU" />
              </Form.Item>
            </>
          )}

          {action === 'inspect' && (
            <>
              <Alert
                type="info"
                showIcon
                style={{ marginBottom: 12 }}
                message="Kiểm nhận xong, ấn phẩm chuyển sang Trong kho và được mở khóa cho lưu thông."
              />
              <Form.Item name="condition" label="Tình trạng vật lý">
                <Select
                  allowClear
                  placeholder="Tốt"
                  options={['Tốt', 'Rách bìa', 'Ố vàng', 'Long gáy', 'Thiếu trang'].map((value) => ({
                    value,
                    label: value,
                  }))}
                />
              </Form.Item>
              <Form.Item name="note" label="Ghi chú">
                <Input.TextArea rows={2} />
              </Form.Item>
            </>
          )}

          {action === 'lock' && (
            <Form.Item
              name="reason"
              label="Lý do khóa"
              rules={[{ required: true, message: 'Phải ghi lý do để người khác biết vì sao không cho mượn được.' }]}
            >
              <Select
                allowClear
                showSearch
                placeholder="Đang sửa chữa"
                options={['Đang sửa chữa', 'Đang số hóa', 'Chờ kiểm nhận', 'Tài liệu quý hiếm'].map(
                  (value) => ({ value, label: value }),
                )}
              />
            </Form.Item>
          )}

          {action === 'unlock' && (
            <Alert
              type="warning"
              showIcon
              message="Bản chưa kiểm nhận sẽ bị bỏ qua — hãy kiểm nhận trước rồi mở khóa."
            />
          )}

          {action === 'transfer' && (
            <>
              <Form.Item
                name="toWarehouseId"
                label="Kho nhận"
                rules={[{ required: true, message: 'Chưa chọn kho nhận.' }]}
              >
                <Select
                  options={(warehouses.data ?? [])
                    .filter((item) => !item.isClosedForInventory)
                    .map((item) => ({ value: item.id, label: item.name }))}
                />
              </Form.Item>
              <Form.Item name="toShelfId" label="Giá nhận">
                <Select
                  allowClear
                  disabled={!shelfWarehouse}
                  options={(shelves.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
                />
              </Form.Item>
              <Form.Item
                name="reason"
                label="Lý do chuyển"
                rules={[{ required: true, message: 'Phiếu chuyển kho in ra cần lý do.' }]}
              >
                <Input placeholder="Điều chuyển phục vụ cơ sở 2" />
              </Form.Item>
              <Form.Item name="decisionNo" label="Số quyết định">
                <Input placeholder="QĐ-12/2026" />
              </Form.Item>
              <Form.Item name="regenerateCallNumber" valuePropName="checked">
                <Checkbox>Sinh lại ký hiệu xếp giá theo quy tắc kho nhận</Checkbox>
              </Form.Item>
            </>
          )}

          {action === 'dispose' && (
            <>
              <Form.Item name="disposalType" label="Hình thức">
                <Select options={disposalTypes.map((value) => ({ value, label: value }))} />
              </Form.Item>
              <Form.Item
                name="reason"
                label="Lý do"
                rules={[{ required: true, message: 'Quyết định in ra cần có lý do.' }]}
              >
                <Input.TextArea rows={2} placeholder="Rách nát không phục hồi được" />
              </Form.Item>
              <Form.Item
                name="decisionNo"
                label="Số quyết định"
                extra="Bỏ trống thì hệ thống tự sinh theo quy tắc cấu hình."
              >
                <Input />
              </Form.Item>
            </>
          )}
        </Form>
      </Modal>

      <Modal
        open={printableDocument !== null}
        title={
          printableDocument
            ? printedDocumentTitle(printableDocument.formType, printableDocument.code)
            : ''
        }
        onCancel={() => setPrintableDocument(null)}
        onOk={() => printableDocument && printDocument.mutate(printableDocument)}
        confirmLoading={printDocument.isPending}
        okText={printableDocument?.formType === 'TRANSFER' ? 'In phiếu' : 'In quyết định'}
        okButtonProps={{ icon: <PrinterOutlined /> }}
        cancelText="Để sau"
      >
        <Typography.Paragraph>
          Chứng từ đã lập xong. In ngay để ký, hoặc in lại sau
          {printableDocument?.formType === 'TRANSFER'
            ? ' từ danh sách "Phiếu chuyển kho".'
            : ' từ chi tiết ấn phẩm đã thanh lý.'}
        </Typography.Paragraph>
      </Modal>

      <TransferSlipsDrawer open={slipsOpen} onClose={() => setSlipsOpen(false)} />

      <Modal
        open={printKind !== null}
        title={printKind === 'barcode' ? 'In tem mã vạch' : 'In nhãn gáy'}
        onCancel={() => setPrintKind(null)}
        onOk={() => printForm.submit()}
        confirmLoading={print.isPending}
        okText="Tạo tệp PDF"
        cancelText="Bỏ qua"
        destroyOnHidden
      >
        <Form form={printForm} layout="vertical" onFinish={(values) => print.mutate(values)}>
          <Typography.Paragraph type="secondary">
            In cho {selectionCount} ấn phẩm đang chọn.
          </Typography.Paragraph>
          <Form.Item name="templateId" label="Mẫu" extra="Bỏ trống thì dùng mẫu mặc định.">
            <Select
              allowClear
              loading={barcodeTemplates.isFetching || labelTemplates.isFetching}
              options={(printKind === 'barcode' ? barcodeTemplates.data : labelTemplates.data)?.map(
                (item) => ({
                  value: item.id,
                  label: `${item.name}${item.isDefault ? ' (mặc định)' : ''}`,
                }),
              )}
            />
          </Form.Item>
          {previewTemplate && previewItem && (
            <Form.Item label="Xem trước với ấn phẩm đầu tiên đang chọn">
              <Space align="start" size="middle">
                <LabelPreview
                  layout={previewTemplate.layout}
                  widthMm={previewTemplate.widthMm}
                  heightMm={previewTemplate.heightMm}
                  data={toLabelData(previewItem)}
                  barcodeType={
                    'barcodeType' in previewTemplate ? String(previewTemplate.barcodeType) : 'Code128'
                  }
                />
                <Typography.Text type="secondary" style={{ maxWidth: 200, display: 'block' }}>
                  {previewItem.barcode} — {previewItem.title}
                </Typography.Text>
              </Space>
            </Form.Item>
          )}
          <Form.Item
            name="copies"
            label="Số bản mỗi ấn phẩm"
            extra="In dư vài bản khi tem hay bị bong."
          >
            <InputNumber min={1} max={10} style={{ width: '100%' }} />
          </Form.Item>
        </Form>
      </Modal>

      <Drawer
        open={detailId !== null}
        onClose={() => setDetailId(null)}
        width={640}
        title={detail.data ? `Ấn phẩm ${detail.data.barcode}` : 'Ấn phẩm'}
      >
        {detail.data && (
          <Space direction="vertical" size="middle" style={{ width: '100%' }}>
            <Descriptions column={2} size="small" bordered>
              <Descriptions.Item label="Nhan đề" span={2}>
                {detail.data.title}
              </Descriptions.Item>
              <Descriptions.Item label="Tác giả">{detail.data.authorMain ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Số kiểm soát">{detail.data.controlNumber ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Số ĐKCB">{detail.data.registerNumber}</Descriptions.Item>
              <Descriptions.Item label="Bản số">{detail.data.copyNumber}</Descriptions.Item>
              <Descriptions.Item label="Thư viện">{detail.data.libraryName ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Kho">{detail.data.warehouseName}</Descriptions.Item>
              <Descriptions.Item label="Vị trí giá">{detail.data.shelfName ?? 'Chưa xếp giá'}</Descriptions.Item>
              <Descriptions.Item label="Ký hiệu xếp giá">{detail.data.callNumber ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Tình trạng">
                <Tag color={itemStatusColors[detail.data.status]}>
                  {itemStatusLabels[detail.data.status]}
                </Tag>
              </Descriptions.Item>
              <Descriptions.Item label="Khóa lưu thông">
                {detail.data.isLocked ? `Có — ${detail.data.lockReason ?? 'không ghi lý do'}` : 'Không'}
              </Descriptions.Item>
              <Descriptions.Item label="Giá bìa">{money(detail.data.price)} đ</Descriptions.Item>
              <Descriptions.Item label="Nguồn kinh phí">
                {detail.data.fundingSourceName ?? '—'}
              </Descriptions.Item>
              <Descriptions.Item label="Ngày bổ sung">
                {formatDate(detail.data.acquisitionDate)}
              </Descriptions.Item>
              <Descriptions.Item label="Hình thức">
                {acquisitionTypeLabels[detail.data.acquisitionType]}
              </Descriptions.Item>
              <Descriptions.Item label="Đơn đặt">{detail.data.orderCode ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Lượt mượn">{detail.data.loanCount}</Descriptions.Item>
              <Descriptions.Item label="Tình trạng vật lý">
                {detail.data.condition ?? '—'}
              </Descriptions.Item>
            </Descriptions>

            {detail.data.disposal && (
              <Alert
                type="warning"
                showIcon
                message={`${detail.data.disposal.disposalType} theo quyết định ${detail.data.disposal.decisionNo ?? ''}`}
                description={
                  <>
                    <div>Ngày: {formatDate(detail.data.disposal.disposalDate)}</div>
                    <div>Lý do: {detail.data.disposal.reason ?? '—'}</div>
                    <div>Người duyệt: {detail.data.disposal.approvedByName ?? '—'}</div>
                  </>
                }
                action={
                  detail.data.disposal.decisionNo ? (
                    <Can permission={PERMISSIONS.acquisition.itemDispose}>
                      <Button
                        size="small"
                        icon={<PrinterOutlined />}
                        loading={printDocument.isPending}
                        onClick={() =>
                          printDocument.mutate({
                            formType: 'DISPOSAL',
                            code: detail.data!.disposal!.decisionNo!,
                          })
                        }
                      >
                        In quyết định
                      </Button>
                    </Can>
                  ) : undefined
                }
              />
            )}

            <Card variant="borderless" title="Lịch sử chuyển kho" size="small">
              {detail.data.movements.length === 0 ? (
                <Typography.Text type="secondary">Bản này chưa từng chuyển kho.</Typography.Text>
              ) : (
                <Timeline
                  items={detail.data.movements.map((movement) => ({
                    children: (
                      <Space direction="vertical" size={0}>
                        <Typography.Text strong>
                          {formatDate(movement.movementDate)} — phiếu {movement.batchCode}
                        </Typography.Text>
                        <span>
                          {movement.fromWarehouseName ?? '—'} → {movement.toWarehouseName ?? '—'}
                        </span>
                        {movement.reason && (
                          <Typography.Text type="secondary">Lý do: {movement.reason}</Typography.Text>
                        )}
                        {movement.decisionNo && (
                          <Typography.Text type="secondary">
                            Quyết định: {movement.decisionNo}
                          </Typography.Text>
                        )}
                        {movement.performedByName && (
                          <Typography.Text type="secondary">
                            Người thực hiện: {movement.performedByName}
                          </Typography.Text>
                        )}
                      </Space>
                    ),
                  }))}
                />
              )}
            </Card>
          </Space>
        )}
      </Drawer>
    </div>
  );
}
