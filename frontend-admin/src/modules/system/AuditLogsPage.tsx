import { useState } from 'react';
import {
  App,
  Button,
  Card,
  Checkbox,
  DatePicker,
  Descriptions,
  Drawer,
  Input,
  InputNumber,
  Select,
  Space,
  Table,
  Tabs,
  Tag,
  Typography,
} from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { FileExcelOutlined, FilePdfOutlined, SaveOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import { errorMessage } from '@/api/formErrors';
import { PERMISSIONS } from '@/api/permissions';
import { FilterBar } from '@/components/FilterBar';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { usePermission } from '@/hooks/usePermission';
import { usePagedQuery } from '@/hooks/usePagedQuery';
import { messages } from '@/i18n/messages';
import { downloadFile, formatDateTime, formatJson } from './helpers';
import type { AuditFilterOptions, AuditLogDetail, AuditLogItem, AuditSetting } from './types';

interface AuditFilterState {
  action?: string;
  entity?: string;
  username?: string;
  result?: boolean;
  range?: [Dayjs, Dayjs] | null;
}

/** Phân hệ I.4 — tra cứu nhật ký và cài đặt chế độ ghi nhận. */
export function AuditLogsPage() {
  const { can } = usePermission();

  return (
    <div className="lc-page">
      <PageHeader
        title={messages.menu.auditLogs}
        description="Mọi thao tác thêm, sửa, xóa và đăng nhập đều được ghi lại tự động và lưu trữ vĩnh viễn."
      />

      <Tabs
        items={[
          { key: 'logs', label: 'Tra cứu nhật ký', children: <AuditLogTab /> },
          ...(can(PERMISSIONS.system.auditSetting)
            ? [{ key: 'settings', label: 'Cài đặt ghi nhận', children: <AuditSettingsTab /> }]
            : []),
        ]}
      />
    </div>
  );
}

// ---------------------------------------------------------------------------

function AuditLogTab() {
  const { message } = App.useApp();
  const [filter, setFilter] = useState<AuditFilterState>({});
  const [keyword, setKeyword] = useState('');
  const [detailId, setDetailId] = useState<string | null>(null);
  const [exporting, setExporting] = useState<'Excel' | 'Pdf' | null>(null);

  const list = usePagedQuery<AuditLogItem, Record<string, unknown>>({
    queryKey: 'audit-logs',
    url: '/admin/audit-logs',
  });

  const options = useQuery({
    queryKey: ['audit-filter-options'],
    queryFn: () => api.get<AuditFilterOptions>('/admin/audit-logs/filter-options'),
  });

  const buildParams = () => ({
    keyword,
    action: filter.action,
    entity: filter.entity,
    username: filter.username,
    result: filter.result,
    fromDate: filter.range?.[0]?.startOf('day').toISOString(),
    toDate: filter.range?.[1]?.endOf('day').toISOString(),
  });

  const handleExport = async (format: 'Excel' | 'Pdf') => {
    setExporting(format);

    try {
      // The export uses exactly the filter on screen, so the printout matches what was reviewed.
      const { blob, fileName } = await api.download('/admin/audit-logs/export', {
        params: { ...buildParams(), format },
      });

      downloadFile(blob, fileName);
      message.success('Đã tải tệp xuống.');
    } catch (error) {
      message.error(errorMessage(error));
    } finally {
      setExporting(null);
    }
  };

  const columns: ColumnsType<AuditLogItem> = [
    {
      title: 'Thời điểm',
      dataIndex: 'occurredAt',
      width: 175,
      render: (value: string) => formatDateTime(value),
    },
    {
      title: 'Người dùng',
      dataIndex: 'username',
      width: 150,
      render: (username?: string) => username ?? <Typography.Text type="secondary">Hệ thống</Typography.Text>,
    },
    {
      title: 'Hành động',
      dataIndex: 'actionLabel',
      width: 165,
      render: (label: string, record) => <Tag color={actionColor(record.action)}>{label}</Tag>,
    },
    { title: 'Đối tượng', dataIndex: 'entityLabel', width: 180 },
    {
      title: 'Bản ghi',
      dataIndex: 'entityDisplay',
      ellipsis: true,
      render: (display: string | undefined, record) => display ?? record.entityId ?? '—',
    },
    {
      title: 'Kết quả',
      dataIndex: 'result',
      width: 110,
      render: (result: boolean) =>
        result ? <Tag color="green">Thành công</Tag> : <Tag color="red">Thất bại</Tag>,
    },
    { title: 'Địa chỉ IP', dataIndex: 'ip', width: 130, responsive: ['xl'] },
    {
      title: '',
      key: 'actions',
      width: 90,
      fixed: 'right',
      render: (_, record) => (
        <Button type="link" size="small" onClick={() => setDetailId(record.id)}>
          Chi tiết
        </Button>
      ),
    },
  ];

  return (
    <>
      <FilterBar
        loading={list.isFetching}
        onSearch={() => list.applyFilter(buildParams())}
        onReset={() => {
          setFilter({});
          setKeyword('');
          list.resetFilter();
        }}
        extra={
          <Can permission={PERMISSIONS.system.auditExport}>
            <Space>
              <Button
                icon={<FileExcelOutlined />}
                loading={exporting === 'Excel'}
                onClick={() => handleExport('Excel')}
              >
                {messages.actions.exportExcel}
              </Button>
              <Button icon={<FilePdfOutlined />} loading={exporting === 'Pdf'} onClick={() => handleExport('Pdf')}>
                {messages.actions.exportPdf}
              </Button>
            </Space>
          </Can>
        }
      >
        <DatePicker.RangePicker
          format="DD/MM/YYYY"
          value={filter.range ?? null}
          onChange={(range) => setFilter((current) => ({ ...current, range: range as [Dayjs, Dayjs] | null }))}
          presets={[
            { label: 'Hôm nay', value: [dayjs().startOf('day'), dayjs().endOf('day')] },
            { label: '7 ngày qua', value: [dayjs().subtract(7, 'day'), dayjs()] },
            { label: '30 ngày qua', value: [dayjs().subtract(30, 'day'), dayjs()] },
          ]}
        />
        <Select
          allowClear
          showSearch
          optionFilterProp="label"
          placeholder="Hành động"
          style={{ width: 200 }}
          value={filter.action}
          onChange={(value) => setFilter((current) => ({ ...current, action: value }))}
          options={(options.data?.actions ?? []).map((item) => ({ value: item.value, label: item.label }))}
        />
        <Select
          allowClear
          showSearch
          placeholder="Đối tượng"
          style={{ width: 200 }}
          value={filter.entity}
          onChange={(value) => setFilter((current) => ({ ...current, entity: value }))}
          options={(options.data?.entities ?? []).map((entity) => ({ value: entity, label: entity }))}
        />
        <Select
          allowClear
          showSearch
          placeholder="Người dùng"
          style={{ width: 180 }}
          value={filter.username}
          onChange={(value) => setFilter((current) => ({ ...current, username: value }))}
          options={(options.data?.usernames ?? []).map((name) => ({ value: name, label: name }))}
        />
        <Select
          allowClear
          placeholder="Kết quả"
          style={{ width: 150 }}
          value={filter.result}
          onChange={(value) => setFilter((current) => ({ ...current, result: value }))}
          options={[
            { value: true, label: 'Thành công' },
            { value: false, label: 'Thất bại' },
          ]}
        />
        <Input
          allowClear
          placeholder="Từ khóa trong bản ghi hoặc ghi chú"
          value={keyword}
          onChange={(event) => setKeyword(event.target.value)}
          style={{ width: 280 }}
        />
      </FilterBar>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<AuditLogItem>
          rowKey="id"
          columns={columns}
          dataSource={list.items}
          loading={list.isLoading}
          pagination={list.pagination}
          onChange={list.handleTableChange}
          scroll={{ x: 1200 }}
          size="middle"
          locale={{ emptyText: messages.table.empty }}
        />
      </Card>

      {detailId && <AuditDetailDrawer id={detailId} onClose={() => setDetailId(null)} />}
    </>
  );
}

function actionColor(action: AuditLogItem['action']): string {
  switch (action) {
    case 'Create':
      return 'green';
    case 'Update':
      return 'blue';
    case 'Delete':
      return 'red';
    case 'LoginFailed':
      return 'volcano';
    case 'Login':
    case 'Logout':
      return 'geekblue';
    case 'PermissionChange':
    case 'ParameterChange':
      return 'purple';
    case 'Backup':
    case 'Restore':
      return 'gold';
    default:
      return 'default';
  }
}

// ---------------------------------------------------------------------------

function AuditDetailDrawer({ id, onClose }: { id: string; onClose: () => void }) {
  const detail = useQuery({
    queryKey: ['audit-log', id],
    queryFn: () => api.get<AuditLogDetail>(`/admin/audit-logs/${id}`),
  });

  const log = detail.data;

  return (
    <Drawer title="Chi tiết bản ghi nhật ký" open width={760} onClose={onClose} loading={detail.isLoading}>
      {log && (
        <Space direction="vertical" size="middle" style={{ width: '100%' }}>
          <Descriptions bordered column={1} size="small">
            <Descriptions.Item label="Thời điểm">{formatDateTime(log.occurredAt)}</Descriptions.Item>
            <Descriptions.Item label="Người dùng">{log.username ?? 'Hệ thống'}</Descriptions.Item>
            <Descriptions.Item label="Hành động">
              <Tag color={actionColor(log.action)}>{log.actionLabel}</Tag>
            </Descriptions.Item>
            <Descriptions.Item label="Đối tượng">{log.entityLabel}</Descriptions.Item>
            <Descriptions.Item label="Bản ghi">{log.entityDisplay ?? log.entityId ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Kết quả">
              {log.result ? <Tag color="green">Thành công</Tag> : <Tag color="red">Thất bại</Tag>}
            </Descriptions.Item>
            {log.message && <Descriptions.Item label="Ghi chú">{log.message}</Descriptions.Item>}
            <Descriptions.Item label="Địa chỉ IP">{log.ip ?? '—'}</Descriptions.Item>
            {log.userAgent && <Descriptions.Item label="Thiết bị">{log.userAgent}</Descriptions.Item>}
          </Descriptions>

          {(log.oldValue || log.newValue) && (
            <div className="lc-diff">
              <div className="lc-diff-pane">
                <Typography.Text strong>Giá trị trước</Typography.Text>
                <pre className="lc-diff-content lc-diff-old">{formatJson(log.oldValue) || '—'}</pre>
              </div>
              <div className="lc-diff-pane">
                <Typography.Text strong>Giá trị sau</Typography.Text>
                <pre className="lc-diff-content lc-diff-new">{formatJson(log.newValue) || '—'}</pre>
              </div>
            </div>
          )}
        </Space>
      )}
    </Drawer>
  );
}

// ---------------------------------------------------------------------------

function AuditSettingsTab() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const [draft, setDraft] = useState<Record<string, AuditSetting>>({});

  const query = useQuery({
    queryKey: ['audit-settings'],
    queryFn: () => api.get<AuditSetting[]>('/admin/audit-logs/settings'),
  });

  const mutation = useMutation({
    mutationFn: (settings: AuditSetting[]) => api.put<number>('/admin/audit-logs/settings', { settings }),
    onSuccess: async (changed) => {
      message.success(changed === 0 ? 'Không có thay đổi nào.' : `Đã cập nhật ${changed} đối tượng.`);
      setDraft({});
      await queryClient.invalidateQueries({ queryKey: ['audit-settings'] });
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const rows = (query.data ?? []).map((setting) => draft[setting.id] ?? setting);

  const update = (setting: AuditSetting, changes: Partial<AuditSetting>) => {
    setDraft((current) => ({ ...current, [setting.id]: { ...setting, ...changes } }));
  };

  const columns: ColumnsType<AuditSetting> = [
    { title: 'Đối tượng nghiệp vụ', dataIndex: 'displayName' },
    {
      title: 'Thêm mới',
      dataIndex: 'logCreate',
      width: 110,
      align: 'center',
      render: (value: boolean, record) => (
        <Checkbox checked={value} onChange={(e) => update(record, { logCreate: e.target.checked })} />
      ),
    },
    {
      title: 'Cập nhật',
      dataIndex: 'logUpdate',
      width: 110,
      align: 'center',
      render: (value: boolean, record) => (
        <Checkbox checked={value} onChange={(e) => update(record, { logUpdate: e.target.checked })} />
      ),
    },
    {
      title: 'Xóa',
      dataIndex: 'logDelete',
      width: 90,
      align: 'center',
      render: (value: boolean, record) => (
        <Checkbox checked={value} onChange={(e) => update(record, { logDelete: e.target.checked })} />
      ),
    },
    {
      title: 'Xem',
      dataIndex: 'logRead',
      width: 90,
      align: 'center',
      render: (value: boolean, record) => (
        <Checkbox checked={value} onChange={(e) => update(record, { logRead: e.target.checked })} />
      ),
    },
    {
      title: 'Thời gian lưu (ngày)',
      dataIndex: 'retentionDays',
      width: 200,
      render: (value: number | null, record) => (
        <InputNumber
          min={1}
          value={value ?? undefined}
          placeholder="Vĩnh viễn"
          style={{ width: '100%' }}
          onChange={(next) => update(record, { retentionDays: next ?? null })}
        />
      ),
    },
  ];

  return (
    <>
      <Card variant="borderless" className="lc-page-card">
        <Space direction="vertical" size="small">
          <Typography.Text type="secondary">
            Bỏ trống cột thời gian lưu để giữ nhật ký vĩnh viễn — đây là mặc định và là yêu cầu của hồ
            sơ mời thầu. Đối tượng không có dòng cấu hình sẽ ghi nhật ký cho thêm/sửa/xóa.
          </Typography.Text>
          <Can permission={PERMISSIONS.system.auditSetting}>
            <Button
              type="primary"
              icon={<SaveOutlined />}
              loading={mutation.isPending}
              disabled={Object.keys(draft).length === 0}
              onClick={() => mutation.mutate(Object.values(draft))}
            >
              {messages.actions.save}
            </Button>
          </Can>
        </Space>
      </Card>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<AuditSetting>
          rowKey="id"
          size="middle"
          columns={columns}
          dataSource={rows}
          loading={query.isLoading}
          pagination={false}
          locale={{ emptyText: messages.table.empty }}
        />
      </Card>
    </>
  );
}
