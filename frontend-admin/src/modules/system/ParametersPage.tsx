import { useEffect, useMemo, useState } from 'react';
import {
  Alert,
  App,
  Button,
  Card,
  DatePicker,
  Drawer,
  Form,
  Grid,
  Input,
  InputNumber,
  Space,
  Switch,
  Table,
  Tabs,
  Tag,
  Typography,
  Upload,
} from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { DownloadOutlined, HistoryOutlined, SaveOutlined, UploadOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import { saveBlob } from '@/modules/marc/api';
import { errorMessage } from '@/api/formErrors';
import { PERMISSIONS } from '@/api/permissions';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { usePermission } from '@/hooks/usePermission';
import { usePagedQuery } from '@/hooks/usePagedQuery';
import { messages } from '@/i18n/messages';
import { formatDateTime } from './helpers';
import type { ParameterGroup, ParameterHistoryItem, SystemParameter } from './types';

/**
 * Phân hệ I.3 — tham số hệ thống.
 *
 * Every configurable value of the product lives here: library identity, code-generation rules,
 * password policy, circulation defaults, OPAC options. Each parameter renders the control its data
 * type calls for, so a boolean is a switch and a cron expression is a text field with a hint.
 */
export function ParametersPage() {
  // Dưới 992px, bố cục hai cột của trang này tràn ra ngoài màn hình điện thoại.
  const screens = Grid.useBreakpoint();

  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const { can } = usePermission();

  const [activeGroup, setActiveGroup] = useState<string | undefined>();
  const [historyOpen, setHistoryOpen] = useState(false);
  const [form] = Form.useForm();

  const groups = useQuery({
    queryKey: ['parameters'],
    queryFn: () => api.get<ParameterGroup[]>('/admin/parameters'),
  });

  const mutation = useMutation({
    mutationFn: (parameters: { key: string; value: string | null }[]) =>
      api.put<number>('/admin/parameters', { parameters }),
    onSuccess: async (changed) => {
      message.success(changed === 0 ? 'Không có tham số nào thay đổi.' : `Đã cập nhật ${changed} tham số.`);
      await queryClient.invalidateQueries({ queryKey: ['parameters'] });
      // The header and the OPAC read the library identity from here.
      await queryClient.invalidateQueries({ queryKey: ['public-settings'] });
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const currentGroup = useMemo(
    () => groups.data?.find((group) => group.groupCode === activeGroup) ?? groups.data?.[0],
    [groups.data, activeGroup],
  );

  const handleSave = () => {
    if (!currentGroup) {
      return;
    }

    const values = form.getFieldsValue();

    const payload = currentGroup.parameters
      .filter((parameter) => parameter.isEditable)
      .map((parameter) => ({ key: parameter.key, value: serialise(parameter, values[parameter.key]) }))
      // A secret left untouched is sent as an empty value, which the backend reads as "keep as is".
      .filter((entry) => entry.value !== undefined) as { key: string; value: string | null }[];

    mutation.mutate(payload);
  };

  return (
    <div className="lc-page">
      <PageHeader
        title={messages.menu.parameters}
        description="Toàn bộ giá trị cấu hình của hệ thống. Không có thông tin nào của thư viện được viết cứng trong mã nguồn."
        actions={
          <Space>
            <Button icon={<HistoryOutlined />} onClick={() => setHistoryOpen(true)}>
              Lịch sử thay đổi
            </Button>
            <Can permission={PERMISSIONS.system.parameterUpdate}>
              <Button type="primary" icon={<SaveOutlined />} loading={mutation.isPending} onClick={handleSave}>
                {messages.actions.save}
              </Button>
            </Can>
          </Space>
        }
      />

      <Card variant="borderless" loading={groups.isLoading}>
        <Tabs
          /* Màn hình hẹp không đủ chỗ cho một cột nhãn bên trái: nhóm tham số chuyển lên trên. */
          tabPosition={screens.lg ? 'left' : 'top'}
          activeKey={currentGroup?.groupCode}
          onChange={(key) => {
            setActiveGroup(key);
            form.resetFields();
          }}
          items={(groups.data ?? []).map((group) => ({
            key: group.groupCode,
            label: group.groupName,
            children: (
              <ParameterGroupForm
                key={group.groupCode}
                form={form}
                group={group}
                readOnly={!can(PERMISSIONS.system.parameterUpdate)}
              />
            ),
          }))}
        />
      </Card>

      {historyOpen && <ParameterHistoryDrawer onClose={() => setHistoryOpen(false)} />}
    </div>
  );
}

// ---------------------------------------------------------------------------

function ParameterGroupForm({
  form,
  group,
  readOnly,
}: {
  form: ReturnType<typeof Form.useForm>[0];
  group: ParameterGroup;
  readOnly: boolean;
}) {
  const initialValues = useMemo(() => {
    const values: Record<string, unknown> = {};

    for (const parameter of group.parameters) {
      values[parameter.key] = deserialise(parameter);
    }

    return values;
  }, [group]);

  return (
    <Form form={form} layout="vertical" initialValues={initialValues} disabled={readOnly} preserve={false}>
      {readOnly && (
        <Alert
          type="info"
          showIcon
          className="lc-page-alert"
          message="Bạn chỉ có quyền xem tham số hệ thống."
        />
      )}

      <div className="lc-parameter-grid">
        {group.parameters.map((parameter) => (
          <Form.Item
            key={parameter.key}
            name={parameter.key}
            label={
              <Space size={6}>
                <span>{parameter.name}</span>
                {!parameter.isEditable && <Tag>Chỉ đọc</Tag>}
                {parameter.isSecret && <Tag color="red">Bí mật</Tag>}
              </Space>
            }
            extra={
              parameter.isSecret && parameter.hasValue
                ? `${parameter.description ?? ''} Giá trị đã được đặt; để trống nếu không muốn thay đổi.`.trim()
                : parameter.description
            }
            valuePropName={parameter.dataType === 'Boolean' ? 'checked' : 'value'}
            tooltip={parameter.key}
          >
            {renderControl(parameter)}
          </Form.Item>
        ))}
      </div>
    </Form>
  );
}

/** Picks the input that matches the parameter's declared data type. */
function renderControl(parameter: SystemParameter) {
  const disabled = !parameter.isEditable;

  switch (parameter.dataType) {
    case 'Boolean':
      return <Switch disabled={disabled} checkedChildren="Bật" unCheckedChildren="Tắt" />;
    case 'Number':
      return <InputNumber disabled={disabled} style={{ width: '100%' }} />;
    case 'Date':
      return <DatePicker disabled={disabled} format="DD/MM/YYYY" style={{ width: '100%' }} />;
    case 'Password':
      return <Input.Password disabled={disabled} autoComplete="new-password" placeholder="••••••••" />;
    case 'Json':
      return <Input.TextArea disabled={disabled} rows={4} className="lc-mono" />;
    case 'Cron':
      return <Input disabled={disabled} placeholder="0 2 * * *" className="lc-mono" />;
    case 'File':
      return <FileParameterControl parameterKey={parameter.key} disabled={disabled} />;
    default:
      return <Input disabled={disabled} />;
  }
}

/**
 * Tham số kiểu Tệp (I.3) — hiện chỉ có logo thư viện: xem tệp đang dùng, tải tệp mới lên, tải về.
 *
 * Form.Item hands this control the stored object name as `value`; the name is not something a
 * person edits, so the control shows the image itself and uploads through the file endpoint. The
 * image is fetched with the bearer token and shown from a blob URL — an `<img src="/api/...">`
 * would carry no token and render broken.
 */
function FileParameterControl({
  parameterKey,
  disabled,
  value,
}: {
  parameterKey: string;
  disabled: boolean;
  value?: string;
}) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);

  // The object name the store holds right now: what the form was loaded with, or what was just
  // uploaded — the form's own value does not change until the page is reloaded.
  const [objectName, setObjectName] = useState(value);

  useEffect(() => setObjectName(value), [value]);

  const current = useQuery({
    queryKey: ['parameter-file', parameterKey, objectName],
    queryFn: () => api.download(`/admin/parameters/${parameterKey}/file`),
    enabled: Boolean(objectName),
    retry: false,
  });

  useEffect(() => {
    if (!current.data) {
      setPreviewUrl(null);
      return;
    }

    const url = URL.createObjectURL(current.data.blob);
    setPreviewUrl(url);

    return () => URL.revokeObjectURL(url);
  }, [current.data]);

  const upload = useMutation({
    mutationFn: async (file: File) => {
      const form = new FormData();
      form.append('file', file);

      return api.post<string>(`/admin/parameters/${parameterKey}/file`, form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
    },
    onSuccess: async (name) => {
      message.success('Đã tải tệp lên. Ảnh mới hiện ở đầu trang quản trị và trên biểu mẫu in.');
      setObjectName(name);
      await queryClient.invalidateQueries({ queryKey: ['parameters'] });
      await queryClient.invalidateQueries({ queryKey: ['public-settings'] });
      await queryClient.invalidateQueries({ queryKey: ['parameter-file', parameterKey] });
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  return (
    <Space direction="vertical" size={8}>
      {previewUrl ? (
        <img
          src={previewUrl}
          alt="Tệp hiện tại"
          style={{ maxHeight: 96, maxWidth: 280, objectFit: 'contain', display: 'block' }}
        />
      ) : (
        <Typography.Text type="secondary">
          {current.isFetching ? 'Đang tải tệp hiện tại…' : 'Chưa có tệp nào.'}
        </Typography.Text>
      )}

      <Space wrap>
        <Upload
          accept="image/png,image/jpeg,image/gif,image/webp"
          showUploadList={false}
          disabled={disabled || upload.isPending}
          beforeUpload={(file) => {
            upload.mutate(file as unknown as File);
            return false;
          }}
        >
          <Button icon={<UploadOutlined />} loading={upload.isPending} disabled={disabled}>
            {objectName ? 'Thay tệp' : 'Tải tệp lên'}
          </Button>
        </Upload>

        {current.data && (
          <Button
            icon={<DownloadOutlined />}
            onClick={() => saveBlob(current.data.blob, current.data.fileName)}
          >
            Tải về
          </Button>
        )}
      </Space>

      <Typography.Text type="secondary" className="lc-small">
        Ảnh PNG, JPG, GIF hoặc WEBP, tối đa 2 MB.
      </Typography.Text>
    </Space>
  );
}

/** Stored values are text; this turns them into whatever the control expects. */
function deserialise(parameter: SystemParameter): unknown {
  const raw = parameter.value ?? parameter.defaultValue ?? '';

  switch (parameter.dataType) {
    case 'Boolean':
      return raw.toLowerCase() === 'true';
    case 'Number':
      return raw === '' ? undefined : Number(raw);
    case 'Date':
      return raw === '' ? undefined : dayjs(raw);
    case 'Password':
      // Never prefilled: the backend does not send secrets to the client.
      return undefined;
    default:
      return raw;
  }
}

/** Reverse of {@link deserialise}. Returns undefined for a value that should not be submitted. */
function serialise(parameter: SystemParameter, value: unknown): string | null | undefined {
  if (parameter.dataType === 'Password') {
    return value === undefined || value === null || value === '' ? undefined : String(value);
  }

  // A file parameter is set by the upload endpoint, never by the form: the form still holds the
  // object name it was loaded with, and saving that back would undo an upload made a moment ago.
  if (parameter.dataType === 'File') {
    return undefined;
  }

  if (value === undefined || value === null) {
    return null;
  }

  switch (parameter.dataType) {
    case 'Boolean':
      return value ? 'true' : 'false';
    case 'Date':
      return dayjs.isDayjs(value) ? value.format('YYYY-MM-DD') : String(value);
    default:
      return String(value);
  }
}

// ---------------------------------------------------------------------------

function ParameterHistoryDrawer({ onClose }: { onClose: () => void }) {
  const history = usePagedQuery<ParameterHistoryItem>({
    queryKey: 'parameter-history',
    url: '/admin/parameters/history',
  });

  const columns: ColumnsType<ParameterHistoryItem> = [
    { title: 'Thời điểm', dataIndex: 'changedAt', width: 170, render: (value: string) => formatDateTime(value) },
    {
      title: 'Tham số',
      dataIndex: 'parameterName',
      render: (name: string | undefined, record) => (
        <Space direction="vertical" size={0}>
          <Typography.Text>{name ?? record.key}</Typography.Text>
          <Typography.Text type="secondary" className="lc-mono lc-small">
            {record.key}
          </Typography.Text>
        </Space>
      ),
    },
    {
      title: 'Giá trị cũ',
      dataIndex: 'oldValue',
      width: 200,
      ellipsis: true,
      render: (value?: string) =>
        value ? <Typography.Text delete>{value}</Typography.Text> : <Typography.Text type="secondary">—</Typography.Text>,
    },
    {
      title: 'Giá trị mới',
      dataIndex: 'newValue',
      width: 200,
      ellipsis: true,
      render: (value?: string) =>
        value ? <Typography.Text strong>{value}</Typography.Text> : <Typography.Text type="secondary">—</Typography.Text>,
    },
    { title: 'Người thay đổi', dataIndex: 'changedByName', width: 180 },
  ];

  return (
    <Drawer title="Lịch sử thay đổi tham số" open width={900} onClose={onClose}>
      <Table<ParameterHistoryItem>
        rowKey="id"
        size="small"
        columns={columns}
        dataSource={history.items}
        loading={history.isLoading}
        pagination={history.pagination}
        onChange={history.handleTableChange}
        locale={{ emptyText: 'Chưa có thay đổi nào được ghi nhận.' }}
      />
    </Drawer>
  );
}
