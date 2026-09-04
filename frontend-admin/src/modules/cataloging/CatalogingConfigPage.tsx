import { useEffect, useState } from 'react';
import {
  App,
  Button,
  Drawer,
  Empty,
  Form,
  Input,
  InputNumber,
  Popconfirm,
  Select,
  Space,
  Switch,
  Table,
  Tabs,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined, StarOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { applyApiError, errorMessage } from '@/api/formErrors';
import { marcApi } from '@/modules/marc/api';
import { catalogingApi } from './api';
import { useCatalogOptions, toOptions } from './useCatalogOptions';
import type { MarcFieldDefault, MarcTemplate } from './types';
import {
  formatTemplateLines,
  parseTemplateLines,
  readTemplateFields,
  TemplateLineError,
} from './templateFields';

const MONOSPACE = { fontFamily: 'ui-monospace, Consolas, monospace' } as const;

/**
 * Cấu hình biên mục: giá trị ngầm định của trường MARC (II.1) và mẫu biên mục (II.5).
 *
 * These two decide what a cataloguer sees the moment they start a new record — the skeleton of
 * fields and the values already filled in — so they sit on one screen: changing one usually means
 * looking at the other.
 */
export function CatalogingConfigPage() {
  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Cấu hình biên mục"
        description="Quyết định biểu ghi mới trông thế nào: mẫu biên mục cho khung trường, bảng giá trị ngầm định cho những ô điền sẵn."
      />

      <Tabs
        defaultActiveKey="defaults"
        items={[
          { key: 'defaults', label: 'Giá trị ngầm định (II.1)', children: <DefaultsTab /> },
          { key: 'templates', label: 'Mẫu biên mục (II.5)', children: <TemplatesTab /> },
        ]}
      />
    </Space>
  );
}

function DefaultsTab() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<MarcFieldDefault | null>(null);
  const [open, setOpen] = useState(false);

  const defaults = useQuery({
    queryKey: ['marc-defaults'],
    queryFn: () => catalogingApi.marcDefaults(undefined, true),
  });

  const remove = useMutation({
    mutationFn: (id: string) => catalogingApi.deleteMarcDefault(id),
    onSuccess: async () => {
      message.success('Đã xóa giá trị ngầm định.');
      await queryClient.invalidateQueries({ queryKey: ['marc-defaults'] });
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  return (
    <Space direction="vertical" size={12} style={{ width: '100%' }}>
      <Space style={{ justifyContent: 'space-between', width: '100%' }}>
        <Typography.Text type="secondary">
          Khi tạo biểu ghi mới, hệ thống điền sẵn các giá trị này. Giá trị lấy từ tham số hệ thống sẽ
          tự đổi theo khi thư viện sửa tham số, không phải sửa hai nơi.
        </Typography.Text>

        <Can permission={PERMISSIONS.cataloging.defaultValue}>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => {
              setEditing(null);
              setOpen(true);
            }}
          >
            Thêm giá trị
          </Button>
        </Can>
      </Space>

      <Table<MarcFieldDefault>
        rowKey="id"
        size="small"
        loading={defaults.isFetching}
        dataSource={defaults.data ?? []}
        pagination={false}
        locale={{ emptyText: <Empty description="Chưa khai báo giá trị ngầm định nào" /> }}
        columns={[
          {
            title: 'Trường',
            width: 260,
            render: (_, row) => (
              <Space direction="vertical" size={0}>
                <Typography.Text style={MONOSPACE}>
                  {row.tag}
                  {row.subfield ? `$${row.subfield}` : ''}
                  {row.position !== null && row.position !== undefined
                    ? ` vị trí ${row.position}${row.length ? `–${row.position + row.length - 1}` : ''}`
                    : ''}
                </Typography.Text>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  {row.fieldName}
                </Typography.Text>
              </Space>
            ),
          },
          {
            title: 'Giá trị',
            render: (_, row) =>
              row.parameterKey ? (
                <Tooltip title="Giá trị lấy từ tham số hệ thống, đổi tham số là biểu ghi mới đổi theo">
                  <Tag color="blue" style={MONOSPACE}>
                    {row.parameterKey}
                  </Tag>
                </Tooltip>
              ) : (
                <Typography.Text>{row.defaultValue}</Typography.Text>
              ),
          },
          {
            title: 'Áp dụng cho',
            width: 200,
            render: (_, row) =>
              row.documentTypeName ?? (
                <Typography.Text type="secondary">Mọi dạng tài liệu</Typography.Text>
              ),
          },
          {
            title: 'Trạng thái',
            width: 110,
            render: (_, row) => (row.isActive ? <Tag color="green">Đang dùng</Tag> : <Tag>Đã tắt</Tag>),
          },
          {
            title: '',
            width: 100,
            align: 'right',
            render: (_, row) => (
              <Space size={0}>
                <Can permission={PERMISSIONS.cataloging.defaultValue}>
                  <Button
                    type="text"
                    icon={<EditOutlined />}
                    onClick={() => {
                      setEditing(row);
                      setOpen(true);
                    }}
                  />
                </Can>
                <Can permission={PERMISSIONS.cataloging.defaultValue}>
                  <Popconfirm
                    title="Xóa giá trị ngầm định này?"
                    okText="Xóa"
                    cancelText="Không"
                    onConfirm={() => remove.mutate(row.id)}
                  >
                    <Button type="text" danger icon={<DeleteOutlined />} />
                  </Popconfirm>
                </Can>
              </Space>
            ),
          },
        ]}
      />

      <DefaultDrawer
        open={open}
        value={editing}
        onClose={() => setOpen(false)}
        onSaved={async () => {
          setOpen(false);
          await queryClient.invalidateQueries({ queryKey: ['marc-defaults'] });
        }}
      />
    </Space>
  );
}

function DefaultDrawer({
  open,
  value,
  onClose,
  onSaved,
}: {
  open: boolean;
  value: MarcFieldDefault | null;
  onClose: () => void;
  onSaved: () => void | Promise<void>;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm();
  const tag = Form.useWatch('tag', form) as string | undefined;
  const isControl = Boolean(tag && /^00[1-9]$/.test(tag));

  const documentTypes = useCatalogOptions('document-types', open);

  const definitions = useQuery({
    queryKey: ['marc-fields', '', false],
    queryFn: () => marcApi.getFields(),
    staleTime: 10 * 60 * 1000,
    enabled: open,
  });

  const field = (definitions.data ?? []).find((item) => item.tag === tag);

  useEffect(() => {
    if (!open) {
      return;
    }

    if (value) {
      form.setFieldsValue(value);
    } else {
      form.resetFields();
      form.setFieldsValue({ isActive: true, sortOrder: 0 });
    }
  }, [open, value, form]);

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      catalogingApi.saveMarcDefault(value?.id ?? null, values),
    onSuccess: async () => {
      message.success(value ? 'Đã cập nhật giá trị ngầm định.' : 'Đã thêm giá trị ngầm định.');
      await onSaved();
    },
    onError: (error: unknown) => message.error(applyApiError(form, error)),
  });

  return (
    <Drawer
      open={open}
      onClose={onClose}
      width={620}
      title={value ? 'Sửa giá trị ngầm định' : 'Thêm giá trị ngầm định'}
      destroyOnClose
      extra={
        <Space>
          <Button onClick={onClose}>Hủy</Button>
          <Button type="primary" loading={save.isPending} onClick={() => form.submit()}>
            Lưu
          </Button>
        </Space>
      }
    >
      <Form form={form} layout="vertical" onFinish={(values) => save.mutate(values)}>
        <Form.Item name="documentTypeId" label="Áp dụng cho dạng tài liệu">
          <Select
            options={toOptions(documentTypes.data)}
            placeholder="Mọi dạng tài liệu"
            allowClear
            showSearch
            optionFilterProp="label"
          />
        </Form.Item>

        <Space size={12} align="start">
          <Form.Item
            name="tag"
            label="Trường MARC"
            rules={[
              { required: true, message: 'Chưa chọn trường.' },
              { pattern: /^[0-9]{3}$/, message: 'Nhãn trường gồm đúng 3 chữ số.' },
            ]}
            style={{ width: 300 }}
          >
            <Select
              options={(definitions.data ?? []).map((item) => ({
                value: item.tag,
                label: `${item.tag} — ${item.name}`,
              }))}
              showSearch
              optionFilterProp="label"
            />
          </Form.Item>

          {!isControl && (
            <Form.Item name="subfield" label="Trường con" style={{ width: 240 }}>
              <Select
                options={(field?.subfields ?? []).map((subfield) => ({
                  value: subfield.code,
                  label: `$${subfield.code} — ${subfield.name}`,
                }))}
                placeholder={field ? 'Chọn trường con' : 'Chọn trường trước'}
                showSearch
                optionFilterProp="label"
              />
            </Form.Item>
          )}
        </Space>

        {isControl ? (
          <Space size={12} align="start">
            <Form.Item
              name="position"
              label="Vị trí ký tự"
              rules={[{ required: true, message: 'Trường điều khiển phải chỉ rõ vị trí.' }]}
              style={{ width: 160 }}
              extra="Ví dụ 35 cho mã ngôn ngữ trong trường 008"
            >
              <InputNumber min={0} max={39} style={{ width: '100%' }} />
            </Form.Item>

            <Form.Item name="length" label="Số ký tự" style={{ width: 160 }}>
              <InputNumber min={1} max={40} style={{ width: '100%' }} />
            </Form.Item>
          </Space>
        ) : (
          <Space size={12} align="start">
            <Form.Item name="ind1" label="Chỉ thị 1" style={{ width: 120 }}>
              <Input maxLength={1} style={MONOSPACE} />
            </Form.Item>
            <Form.Item name="ind2" label="Chỉ thị 2" style={{ width: 120 }}>
              <Input maxLength={1} style={MONOSPACE} />
            </Form.Item>
          </Space>
        )}

        <Form.Item name="defaultValue" label="Giá trị cố định">
          <Input placeholder="Ví dụ: AACR2" />
        </Form.Item>

        <Form.Item
          name="parameterKey"
          label="Hoặc lấy từ tham số hệ thống"
          extra="Khi đặt, giá trị được đọc từ tham số này mỗi lần tạo biểu ghi mới, nên đổi tham số là biểu ghi mới đổi theo."
        >
          <Select
            options={[
              { value: 'CATALOG.MARC_040A', label: 'CATALOG.MARC_040A — Nguồn biên mục' },
              { value: 'CATALOG.DEFAULT_LANGUAGE', label: 'CATALOG.DEFAULT_LANGUAGE — Mã ngôn ngữ mặc định' },
              { value: 'CATALOG.DEFAULT_COUNTRY', label: 'CATALOG.DEFAULT_COUNTRY — Mã nước mặc định' },
              { value: 'LIBRARY.NAME', label: 'LIBRARY.NAME — Tên thư viện' },
            ]}
            placeholder="Không dùng tham số"
            allowClear
            showSearch
            optionFilterProp="label"
          />
        </Form.Item>

        <Space size={16}>
          <Form.Item name="sortOrder" label="Thứ tự áp dụng" style={{ width: 160 }}>
            <InputNumber min={0} max={100000} style={{ width: '100%' }} />
          </Form.Item>

          <Form.Item name="isActive" label="Đang sử dụng" valuePropName="checked">
            <Switch />
          </Form.Item>
        </Space>
      </Form>
    </Drawer>
  );
}

function TemplatesTab() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<MarcTemplate | null>(null);
  const [open, setOpen] = useState(false);

  const templates = useQuery({
    queryKey: ['marc-templates', undefined],
    queryFn: () => catalogingApi.templates(undefined, true),
  });

  // Đặt mặc định là gửi lại nguyên mẫu với cờ bật; máy chủ tự hạ cờ của mẫu cũ cùng dạng tài liệu.
  const setDefault = useMutation({
    mutationFn: (row: MarcTemplate) =>
      catalogingApi.saveTemplate(row.id, {
        code: row.code,
        name: row.name,
        description: row.description,
        documentTypeId: row.documentTypeId,
        isDefault: true,
        isActive: true,
        fields: row.fields,
        clearValues: false,
      }),
    onSuccess: async (_, row) => {
      message.success(`Đã đặt "${row.name}" làm mẫu mặc định.`);
      await queryClient.invalidateQueries({ queryKey: ['marc-templates'] });
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const remove = useMutation({
    mutationFn: (id: string) => catalogingApi.deleteTemplate(id),
    onSuccess: async () => {
      message.success('Đã xóa mẫu biên mục.');
      await queryClient.invalidateQueries({ queryKey: ['marc-templates'] });
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  return (
    <Space direction="vertical" size={12} style={{ width: '100%' }}>
      <Space style={{ justifyContent: 'space-between', width: '100%' }} align="start">
        <Typography.Text type="secondary">
          Mẫu biên mục quyết định khung trường của biểu ghi mới. Tạo mẫu ở đây, hoặc mở một biểu ghi
          đã soạn ưng ý rồi bấm "Lưu thành mẫu" ngay trong trình soạn.
        </Typography.Text>

        <Can permission={PERMISSIONS.cataloging.template}>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => {
              setEditing(null);
              setOpen(true);
            }}
          >
            Thêm mẫu
          </Button>
        </Can>
      </Space>

      <Table<MarcTemplate>
        rowKey="id"
        size="small"
        loading={templates.isFetching}
        dataSource={templates.data ?? []}
        pagination={false}
        locale={{ emptyText: <Empty description="Chưa có mẫu biên mục nào" /> }}
        columns={[
          {
            title: 'Mẫu biên mục',
            render: (_, row) => (
              <Space direction="vertical" size={0}>
                <Typography.Text strong>{row.name}</Typography.Text>
                {row.description && (
                  <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                    {row.description}
                  </Typography.Text>
                )}
              </Space>
            ),
          },
          {
            title: 'Dạng tài liệu',
            dataIndex: 'documentTypeName',
            width: 200,
            render: (value?: string) => value ?? <Typography.Text type="secondary">Mọi dạng</Typography.Text>,
          },
          { title: 'Số trường', dataIndex: 'fieldCount', width: 110, align: 'right' },
          {
            title: 'Trạng thái',
            width: 160,
            render: (_, row) => (
              <Space size={4}>
                {row.isDefault && <Tag color="blue">Mặc định</Tag>}
                {!row.isActive && <Tag>Đã tắt</Tag>}
              </Space>
            ),
          },
          {
            title: '',
            width: 150,
            align: 'right',
            render: (_, row) => (
              <Space size={0}>
                {!row.isDefault && row.isActive && (
                  <Can permission={PERMISSIONS.cataloging.template}>
                    <Tooltip title="Đặt mặc định cho dạng tài liệu này">
                      <Button
                        type="text"
                        icon={<StarOutlined />}
                        loading={setDefault.isPending}
                        onClick={() => setDefault.mutate(row)}
                      />
                    </Tooltip>
                  </Can>
                )}
                <Can permission={PERMISSIONS.cataloging.template}>
                  <Tooltip title="Sửa mẫu">
                    <Button
                      type="text"
                      icon={<EditOutlined />}
                      onClick={() => {
                        setEditing(row);
                        setOpen(true);
                      }}
                    />
                  </Tooltip>
                </Can>
                <Can permission={PERMISSIONS.cataloging.template}>
                  <Popconfirm
                    title={`Xóa mẫu "${row.name}"?`}
                    description="Biểu ghi đã tạo từ mẫu này không bị ảnh hưởng."
                    okText="Xóa"
                    cancelText="Không"
                    onConfirm={() => remove.mutate(row.id)}
                  >
                    <Button type="text" danger icon={<DeleteOutlined />} />
                  </Popconfirm>
                </Can>
              </Space>
            ),
          },
        ]}
        expandable={{
          expandedRowRender: (row) => <TemplateFields fields={row.fields} />,
          rowExpandable: (row) => row.fieldCount > 0,
        }}
      />

      <TemplateDrawer
        open={open}
        value={editing}
        onClose={() => setOpen(false)}
        onSaved={async () => {
          setOpen(false);
          await queryClient.invalidateQueries({ queryKey: ['marc-templates'] });
        }}
      />
    </Space>
  );
}

/**
 * Tạo hoặc sửa một mẫu biên mục.
 *
 * Khung trường soạn bằng văn bản mỗi dòng một trường — đúng dạng cán bộ đọc trên bản in MARC —
 * thay vì một bảng thêm/bớt dòng: mẫu là thứ làm một lần rồi dùng nhiều năm, gõ mười dòng còn
 * nhanh hơn bấm mười lần "thêm trường".
 */
function TemplateDrawer({
  open,
  value,
  onClose,
  onSaved,
}: {
  open: boolean;
  value: MarcTemplate | null;
  onClose: () => void;
  onSaved: () => void | Promise<void>;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm();
  const documentTypes = useCatalogOptions('document-types', open);

  useEffect(() => {
    if (!open) {
      return;
    }

    if (value) {
      form.setFieldsValue({
        ...value,
        lines: formatTemplateLines(readTemplateFields(value.fields)),
      });
    } else {
      form.resetFields();
      form.setFieldsValue({
        isActive: true,
        isDefault: false,
        lines: ['245 10 $a$b$c', '100 1# $a$e', '260 ## $a$b$c', '300 ## $a$c', '650 #4 $a'].join('\n'),
      });
    }
  }, [open, value, form]);

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) => {
      const fields = parseTemplateLines(String(values.lines ?? ''));

      if (fields.length === 0) {
        throw new TemplateLineError(1, 'Mẫu phải có ít nhất một trường.');
      }

      return catalogingApi.saveTemplate(value?.id ?? null, {
        code: values.code,
        name: values.name,
        description: values.description,
        documentTypeId: values.documentTypeId ?? null,
        isDefault: values.isDefault,
        isActive: values.isActive,
        fields: JSON.stringify(fields),
        // Values typed into the lines are part of the template on purpose.
        clearValues: false,
      });
    },
    onSuccess: async () => {
      message.success(value ? 'Đã cập nhật mẫu biên mục.' : 'Đã thêm mẫu biên mục.');
      await onSaved();
    },
    onError: (error: unknown) => {
      if (error instanceof TemplateLineError) {
        form.setFields([{ name: 'lines', errors: [error.message] }]);
        return;
      }

      message.error(applyApiError(form, error));
    },
  });

  return (
    <Drawer
      open={open}
      onClose={onClose}
      width={640}
      title={value ? `Sửa mẫu "${value.name}"` : 'Thêm mẫu biên mục'}
      destroyOnClose
      extra={
        <Space>
          <Button onClick={onClose}>Hủy</Button>
          <Button type="primary" loading={save.isPending} onClick={() => form.submit()}>
            Lưu
          </Button>
        </Space>
      }
    >
      <Form form={form} layout="vertical" onFinish={(values) => save.mutate(values)}>
        <Form.Item
          name="name"
          label="Tên mẫu"
          rules={[{ required: true, message: 'Chưa nhập tên mẫu.' }]}
        >
          <Input placeholder="Ví dụ: Sách tiếng Việt" maxLength={200} />
        </Form.Item>

        <Space size={12} align="start">
          <Form.Item name="code" label="Mã mẫu" style={{ width: 200 }} extra="Bỏ trống để sinh từ tên">
            <Input style={MONOSPACE} maxLength={50} />
          </Form.Item>

          <Form.Item name="documentTypeId" label="Dạng tài liệu" style={{ width: 340 }}>
            <Select
              options={toOptions(documentTypes.data)}
              placeholder="Mọi dạng tài liệu"
              allowClear
              showSearch
              optionFilterProp="label"
            />
          </Form.Item>
        </Space>

        <Form.Item name="description" label="Mô tả">
          <Input.TextArea rows={2} maxLength={500} />
        </Form.Item>

        <Form.Item
          name="lines"
          label="Khung trường — mỗi dòng một trường"
          extra="Nhãn trường, hai chỉ thị (# là khoảng trắng), rồi các trường con: 245 10 $a$b$c. Có thể điền sẵn giá trị: 041 0# $avie."
          rules={[{ required: true, message: 'Mẫu phải có ít nhất một trường.' }]}
        >
          <Input.TextArea rows={12} style={MONOSPACE} spellCheck={false} />
        </Form.Item>

        <Space size={24}>
          <Form.Item name="isDefault" label="Mẫu mặc định" valuePropName="checked">
            <Switch />
          </Form.Item>

          <Form.Item name="isActive" label="Đang sử dụng" valuePropName="checked">
            <Switch />
          </Form.Item>
        </Space>
      </Form>
    </Drawer>
  );
}

/** Khung trường của một mẫu, hiển thị dạng danh sách nhãn trường và trường con. */
function TemplateFields({ fields }: { fields: string }) {
  let parsed: Array<{ tag: string; ind1?: string; ind2?: string; subfields?: Array<{ code: string }> }> = [];

  try {
    parsed = JSON.parse(fields);
  } catch {
    return <Typography.Text type="danger">Khung trường của mẫu này không đọc được.</Typography.Text>;
  }

  return (
    <Space size={[6, 6]} wrap>
      {parsed.map((field, index) => (
        <Tag key={index} style={MONOSPACE}>
          {field.tag}
          {(field.subfields ?? []).map((subfield) => `$${subfield.code}`).join('')}
        </Tag>
      ))}
    </Space>
  );
}
