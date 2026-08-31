import { useState } from 'react';
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
  Tabs,
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
import { stockApi } from './api';
import type {
  BarcodeTemplateDto,
  LabelBoxDto,
  LabelLayoutDto,
  LabelTemplateDto,
} from './types';

/** Các trường kéo lên tem, khớp với LabelFields ở máy chủ. */
const labelFields: { value: string; label: string }[] = [
  { value: 'barcode', label: 'Mã vạch' },
  { value: 'registerNumber', label: 'Số ĐKCB' },
  { value: 'callNumber', label: 'Ký hiệu xếp giá' },
  { value: 'callNumberLine1', label: 'Ký hiệu xếp giá — dòng 1' },
  { value: 'callNumberLine2', label: 'Ký hiệu xếp giá — dòng 2' },
  { value: 'callNumberLine3', label: 'Ký hiệu xếp giá — dòng 3' },
  { value: 'ddc', label: 'Chỉ số DDC' },
  { value: 'title', label: 'Nhan đề' },
  { value: 'author', label: 'Tác giả' },
  { value: 'libraryName', label: 'Tên thư viện' },
  { value: 'warehouseName', label: 'Tên kho' },
  { value: 'isbn', label: 'ISBN' },
  { value: 'publishYear', label: 'Năm xuất bản' },
  { value: 'price', label: 'Giá bìa' },
  { value: 'copyNumber', label: 'Số bản' },
];

const emptyLayout: LabelLayoutDto = { boxes: [], barcode: null, padding: 1.5, showBorder: false };

/**
 * III.2 — Mẫu tem mã vạch và nhãn gáy.
 *
 * Một trình thiết kế cho cả hai vì chúng chỉ khác nhau ở chỗ có khối mã vạch hay không. Khổ tem và
 * lưới trên tờ A4 là phần quan trọng nhất: tờ tem mua sẵn đã cắt sẵn rãnh nên in lệch là hỏng cả tờ.
 */
export function LabelTemplatePage() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [kind, setKind] = useState<'barcode' | 'label'>('barcode');
  const [editing, setEditing] = useState<
    { open: boolean; template: BarcodeTemplateDto | LabelTemplateDto | null } | null
  >(null);

  const barcodeTemplates = useQuery({
    queryKey: ['barcode-templates', true],
    queryFn: () => stockApi.barcodeTemplates(true),
  });

  const labelTemplates = useQuery({
    queryKey: ['label-templates', true],
    queryFn: () => stockApi.labelTemplates(true),
  });

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['barcode-templates'] });
    void queryClient.invalidateQueries({ queryKey: ['label-templates'] });
  };

  const fail = (error: unknown) =>
    message.error(error instanceof ApiRequestError ? error.message : 'Không thực hiện được.');

  const remove = useMutation({
    mutationFn: (id: string) =>
      kind === 'barcode' ? stockApi.deleteBarcodeTemplate(id) : stockApi.deleteLabelTemplate(id),
    onSuccess: () => {
      message.success('Đã xóa mẫu.');
      refresh();
    },
    onError: fail,
  });

  const columns: ColumnsType<BarcodeTemplateDto | LabelTemplateDto> = [
    { title: 'Mã', dataIndex: 'code', width: 160 },
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
    {
      title: 'Khổ tem',
      width: 130,
      render: (_, row) => `${row.widthMm} × ${row.heightMm} mm`,
    },
    {
      title: 'Lưới trên A4',
      width: 150,
      render: (_, row) => `${row.columnsPerPage} cột × ${row.rowsPerPage} hàng`,
    },
    {
      title: 'Lề',
      width: 140,
      render: (_, row) => `trên ${row.marginTopMm} mm, trái ${row.marginLeftMm} mm`,
    },
    {
      title: 'Số ô nội dung',
      width: 130,
      align: 'right',
      render: (_, row) => row.layout.boxes.length,
    },
    {
      title: '',
      width: 100,
      align: 'right',
      render: (_, row) => (
        <Space>
          <Can permission={PERMISSIONS.acquisition.itemPrintBarcode}>
            <Tooltip title="Sửa">
              <Button
                size="small"
                icon={<EditOutlined />}
                onClick={() => setEditing({ open: true, template: row })}
              />
            </Tooltip>
          </Can>
          <Can permission={PERMISSIONS.acquisition.itemPrintBarcode}>
            <Popconfirm
              title="Xóa mẫu này?"
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

  const data = kind === 'barcode' ? barcodeTemplates.data : labelTemplates.data;
  const loading = kind === 'barcode' ? barcodeTemplates.isFetching : labelTemplates.isFetching;

  return (
    <div className="lc-page">
      <PageHeader
        title="Mẫu tem và nhãn"
        description="Khổ tem, lưới trên tờ A4 và nội dung từng ô — dùng khi in mã vạch và nhãn gáy sách."
        actions={
          <Can permission={PERMISSIONS.acquisition.itemPrintBarcode}>
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => setEditing({ open: true, template: null })}
            >
              Thêm mẫu
            </Button>
          </Can>
        }
      />

      <Tabs
        activeKey={kind}
        onChange={(key) => setKind(key as 'barcode' | 'label')}
        items={[
          { key: 'barcode', label: 'Tem mã vạch' },
          { key: 'label', label: 'Nhãn gáy sách' },
        ]}
      />

      <Card variant="borderless">
        <Table
          rowKey="id"
          size="small"
          loading={loading}
          columns={columns}
          dataSource={data ?? []}
          pagination={false}
        />
      </Card>

      {editing?.open && (
        <TemplateDrawer
          kind={kind}
          template={editing.template}
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

function TemplateDrawer({
  kind,
  template,
  onClose,
  onSaved,
}: {
  kind: 'barcode' | 'label';
  template: BarcodeTemplateDto | LabelTemplateDto | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm();
  const [layout, setLayout] = useState<LabelLayoutDto>(template?.layout ?? { ...emptyLayout });

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) => {
      const payload = { ...values, layout };

      return kind === 'barcode'
        ? stockApi.saveBarcodeTemplate(template?.id ?? null, payload)
        : stockApi.saveLabelTemplate(template?.id ?? null, payload);
    },
    onSuccess: () => {
      message.success('Đã lưu mẫu.');
      onSaved();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  const width = Form.useWatch('widthMm', form) ?? template?.widthMm ?? 50;
  const height = Form.useWatch('heightMm', form) ?? template?.heightMm ?? 25;
  const columns = Form.useWatch('columnsPerPage', form) ?? template?.columnsPerPage ?? 4;
  const rows = Form.useWatch('rowsPerPage', form) ?? template?.rowsPerPage ?? 10;
  const marginLeft = Form.useWatch('marginLeftMm', form) ?? template?.marginLeftMm ?? 8;
  const marginTop = Form.useWatch('marginTopMm', form) ?? template?.marginTopMm ?? 10;

  const overflowX = marginLeft + columns * width > 210;
  const overflowY = marginTop + rows * height > 297;

  const updateBox = (index: number, patch: Partial<LabelBoxDto>) => {
    setLayout((current) => ({
      ...current,
      boxes: current.boxes.map((box, position) =>
        position === index ? { ...box, ...patch } : box,
      ),
    }));
  };

  return (
    <Drawer
      open
      width={980}
      onClose={onClose}
      title={
        template
          ? `Sửa mẫu ${template.code}`
          : kind === 'barcode'
            ? 'Thêm mẫu tem mã vạch'
            : 'Thêm mẫu nhãn gáy'
      }
      extra={
        <Button
          type="primary"
          loading={save.isPending}
          disabled={overflowX || overflowY}
          onClick={() => form.submit()}
        >
          Lưu
        </Button>
      }
    >
      <Form
        form={form}
        layout="vertical"
        initialValues={
          template ?? {
            widthMm: kind === 'barcode' ? 50 : 35,
            heightMm: kind === 'barcode' ? 25 : 45,
            columnsPerPage: kind === 'barcode' ? 4 : 5,
            rowsPerPage: kind === 'barcode' ? 10 : 6,
            marginTopMm: 10,
            marginLeftMm: 8,
            barcodeType: 'Code128',
            isActive: true,
            isDefault: false,
          }
        }
        onFinish={(values) => save.mutate(values)}
      >
        <Row gutter={12}>
          <Col span={8}>
            <Form.Item name="code" label="Mã mẫu" rules={[{ required: true, message: 'Chưa nhập mã.' }]}>
              <Input placeholder="TEM50X25" />
            </Form.Item>
          </Col>
          <Col span={16}>
            <Form.Item name="name" label="Tên mẫu" rules={[{ required: true, message: 'Chưa nhập tên.' }]}>
              <Input placeholder="Tem mã vạch 50×25 mm" />
            </Form.Item>
          </Col>
        </Row>

        <Row gutter={12}>
          <Col span={4}>
            <Form.Item name="widthMm" label="Rộng (mm)">
              <InputNumber min={10} max={210} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={4}>
            <Form.Item name="heightMm" label="Cao (mm)">
              <InputNumber min={8} max={297} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={4}>
            <Form.Item name="columnsPerPage" label="Số cột">
              <InputNumber min={1} max={20} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={4}>
            <Form.Item name="rowsPerPage" label="Số hàng">
              <InputNumber min={1} max={40} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={4}>
            <Form.Item name="marginLeftMm" label="Lề trái (mm)">
              <InputNumber min={0} max={100} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={4}>
            <Form.Item name="marginTopMm" label="Lề trên (mm)">
              <InputNumber min={0} max={100} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
        </Row>

        {(overflowX || overflowY) && (
          <Typography.Paragraph type="danger">
            {overflowX && `Lề trái cộng ${columns} tem rộng ${width} mm vượt quá khổ giấy A4 (210 mm). `}
            {overflowY && `Lề trên cộng ${rows} hàng cao ${height} mm vượt quá khổ giấy A4 (297 mm).`}
          </Typography.Paragraph>
        )}

        <Row gutter={12}>
          {kind === 'barcode' && (
            <Col span={8}>
              <Form.Item name="barcodeType" label="Loại mã vạch">
                <Select
                  options={[
                    { value: 'Code128', label: 'CODE 128' },
                    { value: 'Code39', label: 'CODE 39' },
                    { value: 'QrCode', label: 'QR Code' },
                  ]}
                />
              </Form.Item>
            </Col>
          )}
          <Col span={8}>
            <Form.Item name="isDefault" valuePropName="checked" label=" ">
              <Checkbox>Mẫu mặc định</Checkbox>
            </Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item name="isActive" valuePropName="checked" label=" ">
              <Checkbox>Đang sử dụng</Checkbox>
            </Form.Item>
          </Col>
        </Row>
      </Form>

      <Card
        variant="borderless"
        size="small"
        title="Khối mã vạch"
        extra={
          <Checkbox
            checked={layout.barcode !== null}
            onChange={(event) =>
              setLayout((current) => ({
                ...current,
                barcode: event.target.checked
                  ? {
                      x: 3,
                      y: 6,
                      width: Math.max(10, width - 9),
                      height: 11,
                      showText: true,
                      fontSize: 6.5,
                      type: null,
                      source: 'barcode',
                    }
                  : null,
              }))
            }
          >
            Tem này có mã vạch
          </Checkbox>
        }
      >
        {layout.barcode ? (
          <Row gutter={12}>
            <Col span={3}>
              <Typography.Text type="secondary">X (mm)</Typography.Text>
              <InputNumber
                value={layout.barcode.x}
                min={0}
                step={0.5}
                style={{ width: '100%' }}
                onChange={(value) =>
                  setLayout((c) => ({ ...c, barcode: { ...c.barcode!, x: value ?? 0 } }))
                }
              />
            </Col>
            <Col span={3}>
              <Typography.Text type="secondary">Y (mm)</Typography.Text>
              <InputNumber
                value={layout.barcode.y}
                min={0}
                step={0.5}
                style={{ width: '100%' }}
                onChange={(value) =>
                  setLayout((c) => ({ ...c, barcode: { ...c.barcode!, y: value ?? 0 } }))
                }
              />
            </Col>
            <Col span={3}>
              <Typography.Text type="secondary">Rộng</Typography.Text>
              <InputNumber
                value={layout.barcode.width}
                min={5}
                step={0.5}
                style={{ width: '100%' }}
                onChange={(value) =>
                  setLayout((c) => ({ ...c, barcode: { ...c.barcode!, width: value ?? 10 } }))
                }
              />
            </Col>
            <Col span={3}>
              <Typography.Text type="secondary">Cao</Typography.Text>
              <InputNumber
                value={layout.barcode.height}
                min={3}
                step={0.5}
                style={{ width: '100%' }}
                onChange={(value) =>
                  setLayout((c) => ({ ...c, barcode: { ...c.barcode!, height: value ?? 8 } }))
                }
              />
            </Col>
            <Col span={5}>
              <Typography.Text type="secondary">Mã hóa trường</Typography.Text>
              <Select
                value={layout.barcode.source}
                style={{ width: '100%' }}
                options={[
                  { value: 'barcode', label: 'Mã vạch' },
                  { value: 'registerNumber', label: 'Số ĐKCB' },
                  { value: 'callNumber', label: 'Ký hiệu xếp giá' },
                ]}
                onChange={(value) =>
                  setLayout((c) => ({ ...c, barcode: { ...c.barcode!, source: value } }))
                }
              />
            </Col>
            <Col span={7}>
              <Typography.Text type="secondary"> </Typography.Text>
              <div>
                <Checkbox
                  checked={layout.barcode.showText}
                  onChange={(event) =>
                    setLayout((c) => ({
                      ...c,
                      barcode: { ...c.barcode!, showText: event.target.checked },
                    }))
                  }
                >
                  In dãy số dưới vạch
                </Checkbox>
              </div>
            </Col>
          </Row>
        ) : (
          <Typography.Text type="secondary">
            Nhãn gáy thường không có mã vạch — nó nằm ở gáy sách để đọc ký hiệu xếp giá, còn mã vạch
            dán trong bìa cho máy quét ở quầy.
          </Typography.Text>
        )}
      </Card>

      <Card
        variant="borderless"
        size="small"
        title="Ô nội dung"
        style={{ marginTop: 12 }}
        extra={
          <Button
            size="small"
            icon={<PlusOutlined />}
            onClick={() =>
              setLayout((current) => ({
                ...current,
                boxes: [
                  ...current.boxes,
                  {
                    source: 'callNumber',
                    x: 1,
                    y: 1,
                    width: Math.max(10, width - 3),
                    height: 4,
                    fontSize: 7,
                    bold: false,
                    italic: false,
                    align: 'center',
                    border: false,
                    prefix: null,
                  },
                ],
              }))
            }
          >
            Thêm ô
          </Button>
        }
      >
        <Table
          rowKey={(_, index) => String(index)}
          size="small"
          pagination={false}
          dataSource={layout.boxes}
          columns={[
            {
              title: 'Nội dung',
              width: 220,
              render: (_, row: LabelBoxDto, index: number) => (
                <Select
                  value={row.source}
                  style={{ width: '100%' }}
                  options={labelFields}
                  onChange={(value) => updateBox(index, { source: value })}
                />
              ),
            },
            {
              title: 'X',
              width: 80,
              render: (_, row: LabelBoxDto, index: number) => (
                <InputNumber
                  value={row.x}
                  min={0}
                  step={0.5}
                  style={{ width: '100%' }}
                  onChange={(value) => updateBox(index, { x: value ?? 0 })}
                />
              ),
            },
            {
              title: 'Y',
              width: 80,
              render: (_, row: LabelBoxDto, index: number) => (
                <InputNumber
                  value={row.y}
                  min={0}
                  step={0.5}
                  style={{ width: '100%' }}
                  onChange={(value) => updateBox(index, { y: value ?? 0 })}
                />
              ),
            },
            {
              title: 'Rộng',
              width: 90,
              render: (_, row: LabelBoxDto, index: number) => (
                <InputNumber
                  value={row.width}
                  min={2}
                  step={0.5}
                  style={{ width: '100%' }}
                  onChange={(value) => updateBox(index, { width: value ?? 10 })}
                />
              ),
            },
            {
              title: 'Cao',
              width: 90,
              render: (_, row: LabelBoxDto, index: number) => (
                <InputNumber
                  value={row.height}
                  min={2}
                  step={0.5}
                  style={{ width: '100%' }}
                  onChange={(value) => updateBox(index, { height: value ?? 4 })}
                />
              ),
            },
            {
              title: 'Cỡ chữ',
              width: 90,
              render: (_, row: LabelBoxDto, index: number) => (
                <InputNumber
                  value={row.fontSize}
                  min={4}
                  max={30}
                  step={0.5}
                  style={{ width: '100%' }}
                  onChange={(value) => updateBox(index, { fontSize: value ?? 7 })}
                />
              ),
            },
            {
              title: 'Căn',
              width: 110,
              render: (_, row: LabelBoxDto, index: number) => (
                <Select
                  value={row.align}
                  style={{ width: '100%' }}
                  options={[
                    { value: 'left', label: 'Trái' },
                    { value: 'center', label: 'Giữa' },
                    { value: 'right', label: 'Phải' },
                  ]}
                  onChange={(value) => updateBox(index, { align: value })}
                />
              ),
            },
            {
              title: 'Đậm',
              width: 70,
              align: 'center',
              render: (_, row: LabelBoxDto, index: number) => (
                <Checkbox
                  checked={row.bold}
                  onChange={(event) => updateBox(index, { bold: event.target.checked })}
                />
              ),
            },
            {
              title: '',
              width: 60,
              render: (_, _row: LabelBoxDto, index: number) => (
                <Button
                  size="small"
                  danger
                  icon={<DeleteOutlined />}
                  onClick={() =>
                    setLayout((current) => ({
                      ...current,
                      boxes: current.boxes.filter((_box, position) => position !== index),
                    }))
                  }
                />
              ),
            },
          ]}
        />

        <Space style={{ marginTop: 8 }}>
          <Checkbox
            checked={layout.showBorder}
            onChange={(event) =>
              setLayout((current) => ({ ...current, showBorder: event.target.checked }))
            }
          >
            Vẽ khung viền quanh tem (để cắt)
          </Checkbox>
          <span>
            Lề trong tem:{' '}
            <InputNumber
              size="small"
              value={layout.padding}
              min={0}
              max={10}
              step={0.5}
              onChange={(value) =>
                setLayout((current) => ({ ...current, padding: value ?? 1.5 }))
              }
            />{' '}
            mm
          </span>
        </Space>
      </Card>
    </Drawer>
  );
}
