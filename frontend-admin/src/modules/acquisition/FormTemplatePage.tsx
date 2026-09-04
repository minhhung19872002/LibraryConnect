import { useEffect, useState } from 'react';
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
  Popconfirm,
  Row,
  Select,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { moveItem } from '@/lib/reorder';
import { formsApi } from './api';
import type {
  FormColumnDto,
  FormFieldDto,
  FormLayoutDto,
  FormTemplateDto,
  FormTypeMetadataDto,
} from './types';

const emptyLayout: FormLayoutDto = {
  showLogo: false,
  organisationLines: ['{libraryName}'],
  showNationalHeading: true,
  title: '',
  subtitle: null,
  introLines: [],
  fields: [],
  columns: [],
  showTotals: true,
  closingLines: [],
  signatures: [],
  footer: null,
  fontSize: 10,
};

/**
 * III.6 — Trình thiết kế biểu mẫu in.
 *
 * Người thiết kế đổi được nội dung từng phần nhưng không đổi được trật tự các phần: tên cơ quan bên
 * trái, quốc hiệu bên phải, tên biểu mẫu ở giữa, bảng chi tiết, rồi ô ký. Trật tự đó là cái làm cho
 * tờ giấy được chấp nhận khi mang đi ký.
 */
export function FormTemplatePage() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [formType, setFormType] = useState<string | null>(null);
  const [editing, setEditing] = useState<{ open: boolean; template: FormTemplateDto | null } | null>(
    null,
  );

  const types = useQuery({ queryKey: ['form-types'], queryFn: () => formsApi.types() });

  const templates = useQuery({
    queryKey: ['form-templates', formType],
    queryFn: () => formsApi.templates(formType, true),
  });

  const refresh = () => void queryClient.invalidateQueries({ queryKey: ['form-templates'] });

  const remove = useMutation({
    mutationFn: (id: string) => formsApi.remove(id),
    onSuccess: () => {
      message.success('Đã xóa mẫu biểu.');
      refresh();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xóa được.'),
  });

  const columns: ColumnsType<FormTemplateDto> = [
    { title: 'Mã', dataIndex: 'code', width: 170 },
    {
      title: 'Tên mẫu',
      dataIndex: 'name',
      render: (value: string, row) => (
        <Space>
          <span>{value}</span>
          {row.isDefault && <Tag color="blue">Mặc định</Tag>}
          {!row.isActive && <Tag>Ngừng dùng</Tag>}
        </Space>
      ),
    },
    { title: 'Loại chứng từ', dataIndex: 'formTypeName', width: 200 },
    {
      title: 'Khổ giấy',
      width: 130,
      render: (_, row) => `${row.paperSize}${row.isLandscape ? ' ngang' : ''}`,
    },
    { title: 'Số cột bảng', width: 120, align: 'right', render: (_, row) => row.layout.columns.length },
    { title: 'Ô ký', width: 90, align: 'right', render: (_, row) => row.layout.signatures.length },
    {
      title: '',
      width: 100,
      align: 'right',
      render: (_, row) => (
        <Space>
          <Can permission={PERMISSIONS.acquisition.formTemplate}>
            <Tooltip title="Sửa">
              <Button
                size="small"
                icon={<EditOutlined />}
                onClick={() => setEditing({ open: true, template: row })}
              />
            </Tooltip>
          </Can>
          <Can permission={PERMISSIONS.acquisition.formTemplate}>
            <Popconfirm
              title="Xóa mẫu biểu này?"
              okText="Xóa"
              cancelText="Bỏ qua"
              onConfirm={() => remove.mutate(row.id)}
            >
              <Button size="small" danger icon={<DeleteOutlined />} />
            </Popconfirm>
          </Can>
        </Space>
      ),
    },
  ];

  return (
    <div className="lc-page">
      <PageHeader
        title="Mẫu biểu in"
        description="Phiếu nhập kho, biên bản bàn giao, phiếu chuyển kho, biên bản kiểm kê, quyết định thanh lý, đơn đặt hàng."
        actions={
          <Space>
            <Select
              allowClear
              placeholder="Lọc theo loại chứng từ"
              style={{ width: 240 }}
              value={formType ?? undefined}
              onChange={(value) => setFormType(value ?? null)}
              options={(types.data ?? []).map((item) => ({
                value: item.formType,
                label: item.name,
              }))}
            />
            <Can permission={PERMISSIONS.acquisition.formTemplate}>
              <Button
                type="primary"
                icon={<PlusOutlined />}
                onClick={() => setEditing({ open: true, template: null })}
              >
                Thêm mẫu biểu
              </Button>
            </Can>
          </Space>
        }
      />

      <Card variant="borderless">
        <Table
          rowKey="id"
          size="small"
          loading={templates.isFetching}
          columns={columns}
          dataSource={templates.data ?? []}
          pagination={false}
        />
      </Card>

      {editing?.open && (
        <FormTemplateDrawer
          template={editing.template}
          types={types.data ?? []}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            refresh();
          }}
        />
      )}
    </div>
  );
}

function FormTemplateDrawer({
  template,
  types,
  onClose,
  onSaved,
}: {
  template: FormTemplateDto | null;
  types: FormTypeMetadataDto[];
  onClose: () => void;
  onSaved: () => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm();
  const [layout, setLayout] = useState<FormLayoutDto>(template?.layout ?? { ...emptyLayout });
  const [selectedType, setSelectedType] = useState(template?.formType ?? types[0]?.formType ?? '');

  const metadata = types.find((item) => item.formType === selectedType);

  useEffect(() => {
    if (template) {
      form.setFieldsValue(template);
    }
  }, [form, template]);

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      formsApi.save(template?.id ?? null, { ...values, layout }),
    onSuccess: () => {
      message.success('Đã lưu mẫu biểu.');
      onSaved();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  const updateField = (index: number, patch: Partial<FormFieldDto>) =>
    setLayout((current) => ({
      ...current,
      fields: current.fields.map((field, position) =>
        position === index ? { ...field, ...patch } : field,
      ),
    }));

  // Kéo thả sắp xếp (III.6 — "kéo thả trường"): trước đây thêm trường xong là thứ tự cố định, muốn
  // đổi phải xoá rồi thêm lại. Dùng kéo thả gốc của trình duyệt, không thêm thư viện.
  const [dragging, setDragging] = useState<{ list: 'fields' | 'columns'; index: number } | null>(null);

  const moveField = (from: number, to: number) =>
    setLayout((current) => ({ ...current, fields: moveItem(current.fields, from, to) }));

  const moveColumn = (from: number, to: number) =>
    setLayout((current) => ({ ...current, columns: moveItem(current.columns, from, to) }));

  /** Thuộc tính kéo thả cho một dòng bảng; `list` để không kéo nhầm giữa hai bảng. */
  const dragRow = (list: 'fields' | 'columns', index: number, move: (from: number, to: number) => void) => ({
    draggable: true,
    onDragStart: () => setDragging({ list, index }),
    onDragEnd: () => setDragging(null),
    onDragOver: (event: React.DragEvent<HTMLElement>) => {
      if (dragging?.list === list) {
        event.preventDefault();
      }
    },
    onDrop: (event: React.DragEvent<HTMLElement>) => {
      event.preventDefault();
      if (dragging?.list === list && dragging.index !== index) {
        move(dragging.index, index);
      }
      setDragging(null);
    },
    style: { cursor: 'grab' },
  });

  const updateColumn = (index: number, patch: Partial<FormColumnDto>) =>
    setLayout((current) => ({
      ...current,
      columns: current.columns.map((column, position) =>
        position === index ? { ...column, ...patch } : column,
      ),
    }));

  const headerOptions = (metadata?.headerFields ?? []).map((field) => ({
    value: field.key,
    label: field.label,
  }));

  const rowOptions = (metadata?.rowFields ?? []).map((field) => ({
    value: field.key,
    label: field.label,
  }));

  return (
    <Drawer
      open
      width={1040}
      onClose={onClose}
      title={template ? `Sửa mẫu ${template.code}` : 'Thêm mẫu biểu in'}
      extra={
        <Button type="primary" loading={save.isPending} onClick={() => form.submit()}>
          Lưu
        </Button>
      }
    >
      <Form
        form={form}
        layout="vertical"
        initialValues={
          template ?? {
            formType: selectedType,
            paperSize: 'A4',
            isLandscape: false,
            isActive: true,
            isDefault: false,
          }
        }
        onFinish={(values) => save.mutate(values)}
      >
        <Row gutter={12}>
          <Col span={6}>
            <Form.Item name="code" label="Mã mẫu" rules={[{ required: true, message: 'Chưa nhập mã.' }]}>
              <Input placeholder="BB-BANGIAO" />
            </Form.Item>
          </Col>
          <Col span={10}>
            <Form.Item name="name" label="Tên mẫu" rules={[{ required: true, message: 'Chưa nhập tên.' }]}>
              <Input placeholder="Biên bản bàn giao tài liệu" />
            </Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item
              name="formType"
              label="Loại chứng từ"
              rules={[{ required: true, message: 'Chưa chọn loại.' }]}
            >
              <Select
                options={types.map((item) => ({ value: item.formType, label: item.name }))}
                onChange={(value) => setSelectedType(value)}
              />
            </Form.Item>
          </Col>
        </Row>

        <Row gutter={12}>
          <Col span={6}>
            <Form.Item name="paperSize" label="Khổ giấy">
              <Select
                options={[
                  { value: 'A4', label: 'A4' },
                  { value: 'A5', label: 'A5' },
                  { value: 'CUSTOM', label: 'Tự đặt' },
                ]}
              />
            </Form.Item>
          </Col>
          <Col span={6}>
            <Form.Item name="customWidthMm" label="Rộng (mm)">
              <InputNumber min={50} max={500} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={6}>
            <Form.Item name="customHeightMm" label="Cao (mm)">
              <InputNumber min={50} max={500} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={6}>
            <Space direction="vertical">
              <Form.Item name="isLandscape" valuePropName="checked" noStyle>
                <Checkbox>Giấy ngang</Checkbox>
              </Form.Item>
              <Form.Item name="isDefault" valuePropName="checked" noStyle>
                <Checkbox>Mẫu mặc định của loại này</Checkbox>
              </Form.Item>
              <Form.Item name="isActive" valuePropName="checked" noStyle>
                <Checkbox>Đang sử dụng</Checkbox>
              </Form.Item>
            </Space>
          </Col>
        </Row>
      </Form>

      <Card variant="borderless" size="small" title="Phần đầu">
        <Space direction="vertical" style={{ width: '100%' }}>
          <Input
            addonBefore="Tên biểu mẫu"
            value={layout.title}
            placeholder="Biên bản bàn giao tài liệu"
            onChange={(event) => setLayout({ ...layout, title: event.target.value })}
          />
          <Input
            addonBefore="Dòng phụ đề"
            value={layout.subtitle ?? ''}
            placeholder="Số: {code}"
            onChange={(event) => setLayout({ ...layout, subtitle: event.target.value })}
          />
          <Input
            addonBefore="Tên cơ quan"
            value={layout.organisationLines.join(' | ')}
            placeholder="{libraryName}"
            onChange={(event) =>
              setLayout({
                ...layout,
                organisationLines: event.target.value
                  .split('|')
                  .map((line) => line.trim())
                  .filter(Boolean),
              })
            }
          />
          <Input.TextArea
            rows={2}
            placeholder="Câu dẫn, mỗi dòng một câu. Ví dụ: Hôm nay, ngày {day} tháng {month} năm {year}, chúng tôi gồm:"
            value={layout.introLines.join('\n')}
            onChange={(event) =>
              setLayout({
                ...layout,
                introLines: event.target.value.split('\n').filter((line) => line.trim().length > 0),
              })
            }
          />
          <Space>
            <Checkbox
              checked={layout.showNationalHeading}
              onChange={(event) =>
                setLayout({ ...layout, showNationalHeading: event.target.checked })
              }
            >
              In quốc hiệu và tiêu ngữ
            </Checkbox>
            <Checkbox
              checked={layout.showLogo}
              onChange={(event) => setLayout({ ...layout, showLogo: event.target.checked })}
            >
              In logo thư viện
            </Checkbox>
            <span>
              Cỡ chữ:{' '}
              <InputNumber
                size="small"
                min={7}
                max={16}
                step={0.5}
                value={layout.fontSize}
                onChange={(value) => setLayout({ ...layout, fontSize: value ?? 10 })}
              />
            </span>
          </Space>
          <Typography.Text type="secondary">
            Ô thay thế dùng được ở mọi dòng chữ: {headerOptions.map((option) => `{${option.value}}`).join(', ')}
          </Typography.Text>
        </Space>
      </Card>

      <Card
        variant="borderless"
        size="small"
        title="Dòng thông tin"
        style={{ marginTop: 12 }}
        extra={
          <Button
            size="small"
            icon={<PlusOutlined />}
            onClick={() =>
              setLayout((current) => ({
                ...current,
                fields: [
                  ...current.fields,
                  { label: '', key: headerOptions[0]?.value ?? '', fullWidth: false },
                ],
              }))
            }
          >
            Thêm dòng
          </Button>
        }
      >
        <Table
          rowKey={(_, index) => String(index)}
          size="small"
          pagination={false}
          onRow={(_, index) => dragRow('fields', index ?? 0, moveField)}
          dataSource={layout.fields}
          columns={[
            {
              title: 'Nhãn',
              width: 260,
              render: (_, row: FormFieldDto, index: number) => (
                <Input
                  value={row.label}
                  placeholder="Bên giao"
                  onChange={(event) => updateField(index, { label: event.target.value })}
                />
              ),
            },
            {
              title: 'Trường dữ liệu',
              render: (_, row: FormFieldDto, index: number) => (
                <Select
                  value={row.key}
                  style={{ width: '100%' }}
                  options={headerOptions}
                  onChange={(value) => updateField(index, { key: value })}
                />
              ),
            },
            {
              title: 'Cả dòng',
              width: 100,
              align: 'center',
              render: (_, row: FormFieldDto, index: number) => (
                <Checkbox
                  checked={row.fullWidth}
                  onChange={(event) => updateField(index, { fullWidth: event.target.checked })}
                />
              ),
            },
            {
              title: '',
              width: 60,
              render: (_, _row, index: number) => (
                <Button
                  size="small"
                  danger
                  icon={<DeleteOutlined />}
                  onClick={() =>
                    setLayout((current) => ({
                      ...current,
                      fields: current.fields.filter((_field, position) => position !== index),
                    }))
                  }
                />
              ),
            },
          ]}
        />
      </Card>

      <Card
        variant="borderless"
        size="small"
        title="Cột bảng chi tiết"
        style={{ marginTop: 12 }}
        extra={
          <Button
            size="small"
            icon={<PlusOutlined />}
            onClick={() =>
              setLayout((current) => ({
                ...current,
                columns: [
                  ...current.columns,
                  {
                    header: '',
                    key: rowOptions[0]?.value ?? '',
                    width: 1,
                    align: 'left',
                    sum: false,
                  },
                ],
              }))
            }
          >
            Thêm cột
          </Button>
        }
      >
        <Table
          rowKey={(_, index) => String(index)}
          size="small"
          pagination={false}
          onRow={(_, index) => dragRow('columns', index ?? 0, moveColumn)}
          dataSource={layout.columns}
          columns={[
            {
              title: 'Tiêu đề cột',
              width: 200,
              render: (_, row: FormColumnDto, index: number) => (
                <Input
                  value={row.header}
                  placeholder="Nhan đề"
                  onChange={(event) => updateColumn(index, { header: event.target.value })}
                />
              ),
            },
            {
              title: 'Trường dữ liệu',
              render: (_, row: FormColumnDto, index: number) => (
                <Select
                  value={row.key}
                  style={{ width: '100%' }}
                  options={[{ value: 'index', label: 'Số thứ tự' }, ...rowOptions]}
                  onChange={(value) => updateColumn(index, { key: value })}
                />
              ),
            },
            {
              title: 'Độ rộng',
              width: 100,
              render: (_, row: FormColumnDto, index: number) => (
                <InputNumber
                  value={row.width}
                  min={0.3}
                  max={10}
                  step={0.1}
                  style={{ width: '100%' }}
                  onChange={(value) => updateColumn(index, { width: value ?? 1 })}
                />
              ),
            },
            {
              title: 'Căn',
              width: 110,
              render: (_, row: FormColumnDto, index: number) => (
                <Select
                  value={row.align}
                  style={{ width: '100%' }}
                  options={[
                    { value: 'left', label: 'Trái' },
                    { value: 'center', label: 'Giữa' },
                    { value: 'right', label: 'Phải' },
                  ]}
                  onChange={(value) => updateColumn(index, { align: value })}
                />
              ),
            },
            {
              title: 'Cộng tổng',
              width: 100,
              align: 'center',
              render: (_, row: FormColumnDto, index: number) => (
                <Checkbox
                  checked={row.sum}
                  onChange={(event) => updateColumn(index, { sum: event.target.checked })}
                />
              ),
            },
            {
              title: '',
              width: 60,
              render: (_, _row, index: number) => (
                <Button
                  size="small"
                  danger
                  icon={<DeleteOutlined />}
                  onClick={() =>
                    setLayout((current) => ({
                      ...current,
                      columns: current.columns.filter((_column, position) => position !== index),
                    }))
                  }
                />
              ),
            },
          ]}
        />

        <Checkbox
          style={{ marginTop: 8 }}
          checked={layout.showTotals}
          onChange={(event) => setLayout({ ...layout, showTotals: event.target.checked })}
        >
          In dòng tổng cộng cuối bảng
        </Checkbox>
      </Card>

      <Card variant="borderless" size="small" title="Phần cuối" style={{ marginTop: 12 }}>
        <Space direction="vertical" style={{ width: '100%' }}>
          <Input.TextArea
            rows={2}
            placeholder="Câu kết, mỗi dòng một câu. Ví dụ: Biên bản được lập thành 02 bản, mỗi bên giữ 01 bản."
            value={layout.closingLines.join('\n')}
            onChange={(event) =>
              setLayout({
                ...layout,
                closingLines: event.target.value.split('\n').filter((line) => line.trim().length > 0),
              })
            }
          />
          <Input
            addonBefore="Ô ký"
            placeholder="Đại diện bên giao | Đại diện bên nhận | Thủ trưởng đơn vị"
            value={layout.signatures.map((signature) => signature.role).join(' | ')}
            onChange={(event) =>
              setLayout({
                ...layout,
                signatures: event.target.value
                  .split('|')
                  .map((role) => role.trim())
                  .filter(Boolean)
                  .map((role) => ({ role, note: '(Ký, ghi rõ họ tên)' })),
              })
            }
          />
          <Input
            addonBefore="Chân trang"
            value={layout.footer ?? ''}
            placeholder="In từ phần mềm LibraryConnect ngày {printedAt}"
            onChange={(event) => setLayout({ ...layout, footer: event.target.value })}
          />
        </Space>
      </Card>
    </Drawer>
  );
}
