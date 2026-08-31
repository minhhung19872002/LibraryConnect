import { App, Button, Drawer, Form, Input, InputNumber, Select, Space, Switch, TreeSelect } from 'antd';
import { useMutation } from '@tanstack/react-query';
import { api } from '@/api/client';
import { applyApiError } from '@/api/formErrors';
import { messages } from '@/i18n/messages';
import { buildTreeSelectData } from './treeUtils';
import type { CatalogField, CatalogItem, CatalogMetadata, CatalogTreeNode } from './types';

interface CatalogFormValues {
  code?: string;
  name: string;
  nameEn?: string;
  description?: string;
  sortOrder: number;
  isActive: boolean;
  parentId?: string | null;
  /** Catalogue-specific values, keyed by field key. */
  extras: Record<string, string | number | boolean | null | undefined>;
}

/**
 * Thêm/sửa một giá trị danh mục.
 *
 * The form is generated from the catalogue's metadata: the shared fields are always there, and each
 * catalogue's own fields are rendered with the control its declared type calls for.
 */
export function CatalogFormDrawer({
  open,
  catalog,
  metadata,
  item,
  tree,
  onClose,
  onSaved,
}: {
  open: boolean;
  catalog: string;
  metadata: CatalogMetadata;
  item: CatalogItem | null;
  tree: CatalogTreeNode[];
  onClose: () => void;
  onSaved: () => void | Promise<void>;
}) {
  const [form] = Form.useForm<CatalogFormValues>();
  const { message } = App.useApp();
  const isEdit = item !== null;

  const mutation = useMutation({
    mutationFn: (values: CatalogFormValues) => {
      const payload = {
        code: values.code,
        name: values.name,
        nameEn: values.nameEn,
        description: values.description,
        sortOrder: values.sortOrder ?? 0,
        isActive: values.isActive,
        parentId: metadata.isHierarchical ? (values.parentId ?? null) : null,
        // Everything crosses the wire as text; the backend converts using the declared field type.
        extras: Object.fromEntries(
          metadata.fields.map((field) => [field.key, serialiseExtra(values.extras?.[field.key])]),
        ),
      };

      return isEdit
        ? api.put(`/catalogs/${catalog}/items/${item.id}`, payload)
        : api.post<string>(`/catalogs/${catalog}/items`, payload);
    },
    onSuccess: async () => {
      message.success(isEdit ? messages.notify.updateSuccess : messages.notify.createSuccess);
      await onSaved();
      form.resetFields();
    },
    onError: (error: unknown) => message.error(applyApiError(form, error)),
  });

  return (
    <Drawer
      title={isEdit ? `Sửa ${metadata.singularName}: ${item.name}` : `Thêm ${metadata.singularName}`}
      open={open}
      width={520}
      onClose={onClose}
      destroyOnHidden
      afterOpenChange={(visible) => {
        if (!visible) {
          form.resetFields();
          return;
        }

        form.setFieldsValue(
          item
            ? {
                code: item.code,
                name: item.name,
                nameEn: item.nameEn,
                description: item.description,
                sortOrder: item.sortOrder,
                isActive: item.isActive,
                parentId: item.parentId,
                extras: Object.fromEntries(
                  metadata.fields.map((field) => [field.key, deserialiseExtra(item.extras[field.key], field)]),
                ),
              }
            : { sortOrder: 0, isActive: true, extras: {} },
        );
      }}
      extra={
        <Space>
          <Button onClick={onClose}>{messages.actions.cancel}</Button>
          <Button type="primary" loading={mutation.isPending} onClick={() => form.submit()}>
            {messages.actions.save}
          </Button>
        </Space>
      }
    >
      <Form<CatalogFormValues> form={form} layout="vertical" onFinish={(values) => mutation.mutate(values)}>
        {metadata.showCode && (
          <Form.Item
            name="code"
            label="Mã"
            extra={
              isEdit
                ? 'Đổi mã sẽ ảnh hưởng tới các tệp nhập liệu đang dùng mã cũ.'
                : 'Để trống thì hệ thống tự sinh mã từ tên.'
            }
          >
            <Input placeholder="VD: SACH" />
          </Form.Item>
        )}

        <Form.Item name="name" label="Tên" rules={[{ required: true, message: 'Vui lòng nhập tên.' }]}>
          <Input autoFocus />
        </Form.Item>

        {metadata.showNameEn && (
          <Form.Item name="nameEn" label="Tên tiếng Anh">
            <Input />
          </Form.Item>
        )}

        {metadata.isHierarchical && (
          <Form.Item
            name="parentId"
            label="Thuộc cấp trên"
            extra="Để trống nếu đây là giá trị gốc."
          >
            <TreeSelect
              allowClear
              showSearch
              treeNodeFilterProp="title"
              placeholder="Chọn giá trị cấp trên"
              // A value cannot be moved under itself or under one of its own descendants.
              treeData={buildTreeSelectData(tree, item?.id)}
            />
          </Form.Item>
        )}

        {metadata.fields.map((field) => (
          <Form.Item
            key={field.key}
            name={['extras', field.key]}
            label={field.label}
            extra={field.description}
            valuePropName={field.type === 'Boolean' ? 'checked' : 'value'}
            rules={field.required ? [{ required: true, message: `Vui lòng nhập ${field.label.toLowerCase()}.` }] : []}
          >
            {renderControl(field)}
          </Form.Item>
        ))}

        <Form.Item name="description" label="Mô tả">
          <Input.TextArea rows={3} />
        </Form.Item>

        <Form.Item name="sortOrder" label="Thứ tự hiển thị" extra="Số nhỏ hiển thị trước.">
          <InputNumber min={0} style={{ width: '100%' }} />
        </Form.Item>

        <Form.Item name="isActive" label="Trạng thái" valuePropName="checked">
          <Switch checkedChildren="Đang dùng" unCheckedChildren="Ngưng dùng" />
        </Form.Item>
      </Form>
    </Drawer>
  );
}

function renderControl(field: CatalogField) {
  switch (field.type) {
    case 'Boolean':
      return <Switch checkedChildren="Có" unCheckedChildren="Không" />;
    case 'Number':
      return <InputNumber style={{ width: '100%' }} />;
    case 'Decimal':
      return (
        <InputNumber
          style={{ width: '100%' }}
          // Vietnamese currency amounts read much better grouped by thousands.
          formatter={(value) => (value === undefined || value === null ? '' : `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, '.'))}
          parser={(value) => (value ? Number(value.replace(/\./g, '')) : 0)}
        />
      );
    case 'LongText':
      return <Input.TextArea rows={3} />;
    case 'Select':
      return (
        <Select
          allowClear
          options={field.options.map((option) => ({ value: option.value, label: option.label }))}
        />
      );
    default:
      return <Input />;
  }
}

function serialiseExtra(value: string | number | boolean | null | undefined): string | null {
  if (value === null || value === undefined || value === '') {
    return null;
  }

  if (typeof value === 'boolean') {
    return value ? 'true' : 'false';
  }

  return String(value);
}

function deserialiseExtra(value: string | null | undefined, field: CatalogField) {
  if (value === null || value === undefined || value === '') {
    return field.type === 'Boolean' ? false : undefined;
  }

  switch (field.type) {
    case 'Boolean':
      return value === 'true';
    case 'Number':
    case 'Decimal':
      return Number(value);
    default:
      return value;
  }
}
