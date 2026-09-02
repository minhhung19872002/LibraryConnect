import { useEffect } from 'react';
import { App, Button, Checkbox, Drawer, Form, Input, InputNumber, Space, Typography } from 'antd';
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons';
import { useMutation } from '@tanstack/react-query';
import { applyApiError } from '@/api/formErrors';
import { marcApi } from './api';
import type { MarcFieldDefinition, SaveMarcFieldPayload } from './types';
import { isControlTag } from './marcRecord';
import { MAU } from '@/lib/palette';

interface MarcFieldFormDrawerProps {
  open: boolean;
  /** Null nghĩa là thêm mới. */
  field: MarcFieldDefinition | null;
  onClose: () => void;
  onSaved: () => void | Promise<void>;
}

/**
 * Khai báo hoặc sửa một trường trong bộ định nghĩa MARC 21.
 *
 * A control field has no indicators and no subfields, so those sections disappear the moment the
 * tag entered falls in 001–009 — the standard's rule is enforced by the form's shape rather than by
 * an error message after the fact.
 */
export function MarcFieldFormDrawer({ open, field, onClose, onSaved }: MarcFieldFormDrawerProps) {
  const { message } = App.useApp();
  const [form] = Form.useForm<SaveMarcFieldPayload>();

  const tag = Form.useWatch('tag', form) ?? '';
  const control = isControlTag(tag);

  useEffect(() => {
    if (!open) {
      return;
    }

    if (field) {
      form.setFieldsValue({
        tag: field.tag,
        name: field.name,
        nameEn: field.nameEn ?? '',
        description: field.description ?? '',
        isControl: field.isControl,
        isRepeatable: field.isRepeatable,
        isRequired: field.isRequired,
        isRecommended: field.isRecommended,
        isActive: field.isActive,
        sortOrder: field.sortOrder,
        indicators: field.indicators,
        subfields: field.subfields,
      });
    } else {
      form.resetFields();
      form.setFieldsValue({
        isActive: true,
        isRepeatable: false,
        isRequired: false,
        isRecommended: false,
        sortOrder: 0,
        indicators: [],
        subfields: [],
      });
    }
  }, [open, field, form]);

  const save = useMutation({
    mutationFn: (values: SaveMarcFieldPayload) => {
      const payload: SaveMarcFieldPayload = {
        ...values,
        // The tag decides this, not the operator: a field in 001–009 is a control field by
        // definition and one outside that range never is.
        isControl: isControlTag(values.tag),
        indicators: isControlTag(values.tag) ? [] : (values.indicators ?? []),
        subfields: isControlTag(values.tag) ? [] : (values.subfields ?? []),
      };

      return field ? marcApi.updateField(field.id, payload) : marcApi.createField(payload);
    },
    onSuccess: async () => {
      message.success(field ? 'Đã cập nhật định nghĩa trường.' : 'Đã thêm định nghĩa trường.');
      await onSaved();
    },
    onError: (error: unknown) => {
      message.error(applyApiError(form, error));
    },
  });

  return (
    <Drawer
      open={open}
      onClose={onClose}
      width={720}
      title={field ? `Sửa định nghĩa trường ${field.tag}` : 'Thêm định nghĩa trường MARC'}
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
        <Space size={12} align="start">
          <Form.Item
            name="tag"
            label="Nhãn trường"
            rules={[
              { required: true, message: 'Chưa nhập nhãn trường.' },
              { pattern: /^[0-9]{3}$/, message: 'Nhãn trường gồm đúng 3 chữ số, ví dụ 245.' },
            ]}
          >
            <Input
              style={{ width: 120, fontFamily: 'ui-monospace, Consolas, monospace' }}
              maxLength={3}
              disabled={Boolean(field)}
            />
          </Form.Item>

          <Form.Item name="sortOrder" label="Thứ tự hiển thị">
            <InputNumber style={{ width: 140 }} min={0} max={100000} />
          </Form.Item>
        </Space>

        <Form.Item name="name" label="Tên trường (tiếng Việt)" rules={[{ required: true, message: 'Chưa nhập tên trường.' }]}>
          <Input placeholder="Ví dụ: Nhan đề và thông tin trách nhiệm" />
        </Form.Item>

        <Form.Item name="nameEn" label="Tên trường (tiếng Anh)">
          <Input placeholder="Title Statement" />
        </Form.Item>

        <Form.Item name="description" label="Hướng dẫn nhập">
          <Input.TextArea rows={3} placeholder="Giải thích ngắn gọn cách nhập trường này cho cán bộ biên mục." />
        </Form.Item>

        <Space size={20} wrap style={{ marginBottom: 16 }}>
          <Form.Item name="isRepeatable" valuePropName="checked" noStyle>
            <Checkbox>Lặp lại được</Checkbox>
          </Form.Item>
          <Form.Item name="isRequired" valuePropName="checked" noStyle>
            <Checkbox>Bắt buộc (thiếu là lỗi)</Checkbox>
          </Form.Item>
          <Form.Item name="isRecommended" valuePropName="checked" noStyle>
            <Checkbox>Nên có (thiếu chỉ cảnh báo)</Checkbox>
          </Form.Item>
          <Form.Item name="isActive" valuePropName="checked" noStyle>
            <Checkbox>Đang sử dụng</Checkbox>
          </Form.Item>
        </Space>

        {control ? (
          <Typography.Paragraph type="secondary">
            Trường {tag} nằm trong khoảng 001–009 nên là trường điều khiển: chỉ có giá trị, không có
            chỉ thị và không có trường con.
          </Typography.Paragraph>
        ) : (
          <>
            <Typography.Title level={5}>Chỉ thị</Typography.Title>
            <Form.List name="indicators">
              {(items, { add, remove }) => (
                <Space direction="vertical" size={12} style={{ width: '100%', marginBottom: 16 }}>
                  {items.map((item) => (
                    <div key={item.key} style={{ border: `1px solid ${MAU.vien}`, borderRadius: 6, padding: 12 }}>
                      <Space align="start">
                        <Form.Item
                          name={[item.name, 'position']}
                          label="Vị trí"
                          rules={[{ required: true, message: 'Chọn 1 hoặc 2.' }]}
                        >
                          <InputNumber min={1} max={2} style={{ width: 80 }} />
                        </Form.Item>
                        <Form.Item name={[item.name, 'name']} label="Ý nghĩa" style={{ width: 420 }}>
                          <Input placeholder="Ví dụ: Số ký tự bỏ qua khi sắp xếp" />
                        </Form.Item>
                        <Button type="text" danger icon={<DeleteOutlined />} onClick={() => remove(item.name)} />
                      </Space>

                      <Form.List name={[item.name, 'values']}>
                        {(values, valueOps) => (
                          <Space direction="vertical" size={6} style={{ width: '100%' }}>
                            {values.map((value) => (
                              <Space key={value.key} align="start">
                                <Form.Item name={[value.name, 'code']} noStyle>
                                  <Input
                                    style={{ width: 60, fontFamily: 'ui-monospace, Consolas, monospace' }}
                                    maxLength={1}
                                    placeholder="#"
                                  />
                                </Form.Item>
                                <Form.Item name={[value.name, 'label']} noStyle>
                                  <Input style={{ width: 480 }} placeholder="Ý nghĩa của giá trị này" />
                                </Form.Item>
                                <Button
                                  type="text"
                                  danger
                                  icon={<DeleteOutlined />}
                                  onClick={() => valueOps.remove(value.name)}
                                />
                              </Space>
                            ))}
                            <Button
                              type="dashed"
                              size="small"
                              icon={<PlusOutlined />}
                              onClick={() => valueOps.add({ code: '', label: '' })}
                            >
                              Thêm giá trị chỉ thị
                            </Button>
                          </Space>
                        )}
                      </Form.List>
                    </div>
                  ))}

                  <Button
                    type="dashed"
                    icon={<PlusOutlined />}
                    onClick={() => add({ position: items.length + 1, name: '', values: [] })}
                    disabled={items.length >= 2}
                  >
                    Thêm chỉ thị
                  </Button>
                </Space>
              )}
            </Form.List>

            <Typography.Title level={5}>Trường con</Typography.Title>
            <Form.List name="subfields">
              {(items, { add, remove }) => (
                <Space direction="vertical" size={6} style={{ width: '100%' }}>
                  {items.map((item) => (
                    <Space key={item.key} align="start">
                      <Form.Item
                        name={[item.name, 'code']}
                        noStyle
                        rules={[{ pattern: /^[a-z0-9]$/, message: 'Mã trường con là một chữ thường hoặc một chữ số.' }]}
                      >
                        <Input
                          style={{ width: 60, fontFamily: 'ui-monospace, Consolas, monospace' }}
                          maxLength={1}
                          placeholder="a"
                        />
                      </Form.Item>
                      <Form.Item name={[item.name, 'name']} noStyle>
                        <Input style={{ width: 360 }} placeholder="Tên trường con" />
                      </Form.Item>
                      <Form.Item name={[item.name, 'repeatable']} valuePropName="checked" noStyle>
                        <Checkbox>Lặp lại</Checkbox>
                      </Form.Item>
                      <Form.Item name={[item.name, 'required']} valuePropName="checked" noStyle>
                        <Checkbox>Bắt buộc</Checkbox>
                      </Form.Item>
                      <Button type="text" danger icon={<DeleteOutlined />} onClick={() => remove(item.name)} />
                    </Space>
                  ))}

                  <Button
                    type="dashed"
                    icon={<PlusOutlined />}
                    onClick={() => add({ code: '', name: '', repeatable: false, required: false })}
                  >
                    Thêm trường con
                  </Button>
                </Space>
              )}
            </Form.List>
          </>
        )}
      </Form>
    </Drawer>
  );
}
