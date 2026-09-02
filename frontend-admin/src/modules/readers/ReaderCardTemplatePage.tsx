import { useMemo, useRef, useState } from 'react';
import {
  App,
  Button,
  Card,
  Checkbox,
  Col,
  ColorPicker,
  Drawer,
  Empty,
  Form,
  Input,
  InputNumber,
  Popconfirm,
  Radio,
  Row,
  Select,
  Space,
  Table,
  Tag,
  Typography,
} from 'antd';
import { DeleteOutlined, PlusOutlined, PrinterOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { saveBlob } from '@/modules/marc/api';
import { readersApi } from './api';
import { MAU } from '@/lib/palette';
import type {
  CardBarcodeDto,
  CardBoxDto,
  CardFaceLayoutDto,
  CardImageDto,
  ReaderCardTemplateDto,
} from './types';

/** Tỷ lệ xem trước: 1 mm trên thẻ vẽ thành 3,6 điểm ảnh trên màn hình. */
const SCALE = 3.6;

const emptyFace: CardFaceLayoutDto = { boxes: [], images: [] };

/**
 * VI.2 — Thiết kế mẫu thẻ bạn đọc.
 *
 * Thẻ in ra là vật thật có kích thước thật, nên màn hình thiết kế hiện đúng khổ thẻ theo milimét và
 * cho kéo thả từng ô ngay trên khung xem trước. Con số milimét vẫn sửa được trong bảng bên dưới —
 * kéo tay để bố cục cho nhanh, gõ số để căn cho chuẩn.
 */
export function ReaderCardTemplatePage() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [editing, setEditing] = useState<{ template: ReaderCardTemplateDto | null } | null>(null);

  const templates = useQuery({
    queryKey: ['reader-card-templates', 'all'],
    queryFn: () => readersApi.cardTemplates(true),
  });

  const remove = useMutation({
    mutationFn: (id: string) => readersApi.deleteCardTemplate(id),
    onSuccess: () => {
      message.success('Đã xóa mẫu thẻ.');
      void queryClient.invalidateQueries({ queryKey: ['reader-card-templates'] });
    },
    onError: (error: Error) => message.error(error.message),
  });

  const columns: ColumnsType<ReaderCardTemplateDto> = [
    { title: 'Mã mẫu', dataIndex: 'code', width: 140 },
    {
      title: 'Tên mẫu',
      dataIndex: 'name',
      render: (name: string, row) => (
        <Space>
          <Button type="link" style={{ padding: 0 }} onClick={() => setEditing({ template: row })}>
            {name}
          </Button>
          {row.isDefault && <Tag color="blue">Mặc định</Tag>}
          {!row.isActive && <Tag>Ngừng dùng</Tag>}
        </Space>
      ),
    },
    {
      title: 'Khổ thẻ',
      width: 150,
      render: (_, row) => `${row.widthMm} × ${row.heightMm} mm`,
    },
    { title: 'Thẻ / tờ A4', dataIndex: 'cardsPerPage', width: 110, align: 'right' },
    {
      title: 'Mặt sau',
      dataIndex: 'printBack',
      width: 100,
      render: (value: boolean) => (value ? <Tag color="green">Có in</Tag> : <Tag>Không</Tag>),
    },
    {
      title: '',
      width: 90,
      render: (_, row) => (
        <Can permission={PERMISSIONS.reader.cardTemplate}>
          <Popconfirm
            title="Xóa mẫu thẻ này?"
            okText="Xóa"
            cancelText="Hủy"
            onConfirm={() => remove.mutate(row.id)}
          >
            <Button type="link" danger size="small" icon={<DeleteOutlined />}>
              Xóa
            </Button>
          </Popconfirm>
        </Can>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Mẫu thẻ bạn đọc"
        description="Thiết kế mặt trước và mặt sau của thẻ theo khổ CR80 hoặc khổ tự đặt, kéo thả từng ô nội dung."
        actions={
          <Can permission={PERMISSIONS.reader.cardTemplate}>
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => setEditing({ template: null })}
            >
              Thêm mẫu thẻ
            </Button>
          </Can>
        }
      />

      <Table
        rowKey="id"
        size="small"
        loading={templates.isLoading}
        dataSource={templates.data ?? []}
        columns={columns}
        pagination={false}
        locale={{ emptyText: <Empty description="Chưa có mẫu thẻ nào" /> }}
      />

      {editing && (
        <CardTemplateDrawer
          template={editing.template}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            void queryClient.invalidateQueries({ queryKey: ['reader-card-templates'] });
          }}
        />
      )}
    </Space>
  );
}

function CardTemplateDrawer({
  template,
  onClose,
  onSaved,
}: {
  template: ReaderCardTemplateDto | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm();

  const [face, setFace] = useState<'front' | 'back'>('front');
  const [front, setFront] = useState<CardFaceLayoutDto>(template?.front ?? { ...emptyFace });
  const [back, setBack] = useState<CardFaceLayoutDto>(template?.back ?? { ...emptyFace });

  const fields = useQuery({ queryKey: ['reader-card-fields'], queryFn: () => readersApi.cardFields() });

  const width = (Form.useWatch('widthMm', form) as number | undefined) ?? template?.widthMm ?? 85.6;
  const height = (Form.useWatch('heightMm', form) as number | undefined) ?? template?.heightMm ?? 54;

  const layout = face === 'front' ? front : back;
  const setLayout = face === 'front' ? setFront : setBack;

  const dragRef = useRef<{
    kind: 'box' | 'image' | 'barcode';
    index: number;
    startX: number;
    startY: number;
    originX: number;
    originY: number;
  } | null>(null);

  const fieldOptions = useMemo(
    () => (fields.data ?? []).map((item) => ({ value: item.key, label: item.label })),
    [fields.data],
  );

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      readersApi.saveCardTemplate({ ...values, id: template?.id, front, back }),
    onSuccess: () => {
      message.success('Đã lưu mẫu thẻ.');
      onSaved();
    },
    onError: (error: Error) => message.error(error.message),
  });

  const preview = useMutation({
    mutationFn: () =>
      readersApi.printCards({
        selection: { useFilter: true, filter: {} },
        templateId: template?.id,
        preview: true,
        multiplePerPage: false,
      }),
    onSuccess: ({ blob, fileName }) => {
      saveBlob(blob, fileName);
      message.success('Đã xuất bản thử để xem trước.');
    },
    onError: (error: Error) => message.error(error.message),
  });

  const patchBox = (index: number, patch: Partial<CardBoxDto>) =>
    setLayout((current) => ({
      ...current,
      boxes: current.boxes.map((box, position) => (position === index ? { ...box, ...patch } : box)),
    }));

  const patchImage = (index: number, patch: Partial<CardImageDto>) =>
    setLayout((current) => ({
      ...current,
      images: current.images.map((image, position) =>
        position === index ? { ...image, ...patch } : image,
      ),
    }));

  const patchBarcode = (patch: Partial<CardBarcodeDto>) =>
    setLayout((current) =>
      current.barcode ? { ...current, barcode: { ...current.barcode, ...patch } } : current,
    );

  const onPointerMove = (event: React.PointerEvent<HTMLDivElement>) => {
    const drag = dragRef.current;
    if (!drag) return;

    // Kéo trên màn hình tính bằng điểm ảnh, còn bố cục lưu bằng milimét — chia lại theo tỷ lệ xem
    // trước, và làm tròn tới 0,5 mm cho dễ căn thẳng hàng.
    const deltaX = (event.clientX - drag.startX) / SCALE;
    const deltaY = (event.clientY - drag.startY) / SCALE;

    const x = Math.max(0, Math.round((drag.originX + deltaX) * 2) / 2);
    const y = Math.max(0, Math.round((drag.originY + deltaY) * 2) / 2);

    if (drag.kind === 'box') patchBox(drag.index, { x, y });
    if (drag.kind === 'image') patchImage(drag.index, { x, y });
    if (drag.kind === 'barcode') patchBarcode({ x, y });
  };

  const startDrag = (
    event: React.PointerEvent<HTMLDivElement>,
    kind: 'box' | 'image' | 'barcode',
    index: number,
    originX: number,
    originY: number,
  ) => {
    dragRef.current = { kind, index, startX: event.clientX, startY: event.clientY, originX, originY };
    event.currentTarget.setPointerCapture(event.pointerId);
    event.stopPropagation();
  };

  const boxColumns: ColumnsType<CardBoxDto> = [
    {
      title: 'Nội dung',
      dataIndex: 'source',
      render: (value: string, _row, index) => (
        <Select
          showSearch
          optionFilterProp="label"
          style={{ width: '100%' }}
          value={value}
          options={[
            ...fieldOptions,
            { value: '"Văn bản cố định"', label: 'Văn bản cố định (sửa bên dưới)' },
          ]}
          onChange={(next) => patchBox(index, { source: next })}
        />
      ),
    },
    {
      title: 'Chữ đứng trước',
      dataIndex: 'prefix',
      width: 130,
      render: (value: string | null | undefined, _row, index) => (
        <Input
          value={value ?? ''}
          placeholder="Họ và tên: "
          onChange={(event) => patchBox(index, { prefix: event.target.value })}
        />
      ),
    },
    ...(['x', 'y', 'width', 'height'] as const).map((key) => ({
      title: { x: 'X', y: 'Y', width: 'Rộng', height: 'Cao' }[key],
      dataIndex: key,
      width: 80,
      render: (value: number, _row: CardBoxDto, index: number) => (
        <InputNumber<number>
          size="small"
          min={0}
          step={0.5}
          value={value}
          onChange={(next) => patchBox(index, { [key]: next ?? 0 })}
          style={{ width: '100%' }}
        />
      ),
    })),
    {
      title: 'Cỡ chữ',
      dataIndex: 'fontSize',
      width: 80,
      render: (value: number, _row, index) => (
        <InputNumber<number>
          size="small"
          min={4}
          max={24}
          step={0.5}
          value={value}
          onChange={(next) => patchBox(index, { fontSize: next ?? 8 })}
          style={{ width: '100%' }}
        />
      ),
    },
    {
      title: 'Kiểu',
      width: 190,
      render: (_, row, index) => (
        <Space size={4}>
          <Checkbox checked={row.bold} onChange={(e) => patchBox(index, { bold: e.target.checked })}>
            Đậm
          </Checkbox>
          <Checkbox
            checked={row.uppercase}
            onChange={(e) => patchBox(index, { uppercase: e.target.checked })}
          >
            HOA
          </Checkbox>
          <Select
            size="small"
            style={{ width: 70 }}
            value={row.align}
            options={[
              { value: 'left', label: 'Trái' },
              { value: 'center', label: 'Giữa' },
              { value: 'right', label: 'Phải' },
            ]}
            onChange={(value) => patchBox(index, { align: value })}
          />
        </Space>
      ),
    },
    {
      title: '',
      width: 50,
      render: (_, __, index) => (
        <Button
          type="link"
          danger
          size="small"
          icon={<DeleteOutlined />}
          onClick={() =>
            setLayout((current) => ({
              ...current,
              boxes: current.boxes.filter((_item, position) => position !== index),
            }))
          }
        />
      ),
    },
  ];

  return (
    <Drawer
      open
      width={1180}
      onClose={onClose}
      title={template ? `Sửa mẫu thẻ ${template.code}` : 'Thêm mẫu thẻ'}
      extra={
        <Space>
          {template && (
            <Button
              icon={<PrinterOutlined />}
              loading={preview.isPending}
              onClick={() => preview.mutate()}
            >
              In thử
            </Button>
          )}
          <Button type="primary" loading={save.isPending} onClick={() => form.submit()}>
            Lưu
          </Button>
        </Space>
      }
    >
      <Form
        form={form}
        layout="vertical"
        initialValues={
          template ?? {
            widthMm: 85.6,
            heightMm: 54,
            cardsPerPage: 10,
            isActive: true,
            isDefault: false,
            printBack: false,
          }
        }
        onFinish={(values) => save.mutate(values)}
      >
        <Row gutter={12}>
          <Col span={5}>
            <Form.Item name="code" label="Mã mẫu" rules={[{ required: true, message: 'Chưa nhập mã.' }]}>
              <Input placeholder="THE-CR80" />
            </Form.Item>
          </Col>
          <Col span={7}>
            <Form.Item name="name" label="Tên mẫu" rules={[{ required: true, message: 'Chưa nhập tên.' }]}>
              <Input placeholder="Thẻ bạn đọc CR80" />
            </Form.Item>
          </Col>
          <Col span={3}>
            <Form.Item name="widthMm" label="Rộng (mm)">
              <InputNumber<number> min={40} max={210} step={0.1} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={3}>
            <Form.Item name="heightMm" label="Cao (mm)">
              <InputNumber<number> min={30} max={297} step={0.1} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={3}>
            <Form.Item name="cardsPerPage" label="Thẻ / tờ A4">
              <InputNumber<number> min={1} max={24} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={3}>
            <Space direction="vertical" size={0} style={{ paddingTop: 30 }}>
              <Form.Item name="isDefault" valuePropName="checked" noStyle>
                <Checkbox>Mặc định</Checkbox>
              </Form.Item>
              <Form.Item name="isActive" valuePropName="checked" noStyle>
                <Checkbox>Đang dùng</Checkbox>
              </Form.Item>
              <Form.Item name="printBack" valuePropName="checked" noStyle>
                <Checkbox>In mặt sau</Checkbox>
              </Form.Item>
            </Space>
          </Col>
        </Row>
      </Form>

      <Radio.Group
        value={face}
        onChange={(event) => setFace(event.target.value)}
        optionType="button"
        buttonStyle="solid"
        style={{ marginBottom: 12 }}
        options={[
          { value: 'front', label: 'Mặt trước' },
          { value: 'back', label: 'Mặt sau' },
        ]}
      />

      <Row gutter={16}>
        <Col flex="none">
          <Card size="small" title={`Xem trước — ${width} × ${height} mm`}>
            <div
              style={{
                width: width * SCALE,
                height: height * SCALE,
                position: 'relative',
                border: `1px solid ${MAU.vien}`,
                borderRadius: 6,
                overflow: 'hidden',
                // Trắng thật, không phải trắng ngà của giao diện: thẻ in trên phôi nhựa
                // trắng, và đây là màu mực đi ra máy in chứ không phải màu màn hình.
                background: layout.backgroundColor ?? '#ffffff',
                touchAction: 'none',
              }}
              onPointerMove={onPointerMove}
              onPointerUp={() => {
                dragRef.current = null;
              }}
            >
              {(layout.headerBandHeight ?? 0) > 0 && (
                <div
                  style={{
                    position: 'absolute',
                    left: 0,
                    top: 0,
                    width: '100%',
                    height: (layout.headerBandHeight ?? 0) * SCALE,
                    // Dải màu đầu thẻ mặc định lấy màu chính của sản phẩm; cán bộ đổi được bằng
                    // ô chọn màu bên dưới, và thẻ đã lưu thì giữ nguyên màu đã chọn.
                    background: layout.headerBandColor ?? MAU.chinh,
                  }}
                />
              )}

              {layout.images.map((image, index) => (
                <div
                  key={`image-${index}`}
                  role="presentation"
                  onPointerDown={(event) => startDrag(event, 'image', index, image.x, image.y)}
                  style={{
                    position: 'absolute',
                    left: image.x * SCALE,
                    top: image.y * SCALE,
                    width: image.width * SCALE,
                    height: image.height * SCALE,
                    border: `1px dashed ${MAU.vienDam}`,
                    background: MAU.nenDam,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    fontSize: 10,
                    color: MAU.chuMo,
                    cursor: 'move',
                  }}
                >
                  {image.kind === 'logo' ? 'Logo' : 'Ảnh 3×4'}
                </div>
              ))}

              {layout.barcode && (
                <div
                  role="presentation"
                  onPointerDown={(event) =>
                    startDrag(event, 'barcode', 0, layout.barcode!.x, layout.barcode!.y)
                  }
                  style={{
                    position: 'absolute',
                    left: layout.barcode.x * SCALE,
                    top: layout.barcode.y * SCALE,
                    width: layout.barcode.width * SCALE,
                    height: layout.barcode.height * SCALE,
                    background:
                      // Vạch mã vạch phải gần đen tuyệt đối: máy quét đọc theo độ tương phản,
                      // đổi sang nâu của bảng màu giao diện là thẻ in ra quét không nhận.
                      'repeating-linear-gradient(90deg, #222 0 2px, transparent 2px 5px)',
                    cursor: 'move',
                  }}
                />
              )}

              {layout.boxes.map((box, index) => (
                <div
                  key={`box-${index}`}
                  role="presentation"
                  onPointerDown={(event) => startDrag(event, 'box', index, box.x, box.y)}
                  title={box.source}
                  style={{
                    position: 'absolute',
                    left: box.x * SCALE,
                    top: box.y * SCALE,
                    width: box.width * SCALE,
                    height: box.height * SCALE,
                    fontSize: box.fontSize * 1.33,
                    fontWeight: box.bold ? 700 : 400,
                    fontStyle: box.italic ? 'italic' : 'normal',
                    color: box.color ?? '#000',
                    textAlign: box.align,
                    textTransform: box.uppercase ? 'uppercase' : 'none',
                    overflow: 'hidden',
                    whiteSpace: 'nowrap',
                    cursor: 'move',
                    outline: '1px dotted rgba(0,0,0,0.2)',
                  }}
                >
                  {(box.prefix ?? '') + sampleOf(box.source)}
                </div>
              ))}
            </div>

            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              Kéo trực tiếp từng ô trên khung để bố cục.
            </Typography.Text>
          </Card>
        </Col>

        <Col flex="auto">
          <Space direction="vertical" size={12} style={{ width: '100%' }}>
            <Card
              size="small"
              title="Nền mặt thẻ"
              extra={
                <Space>
                  <Button
                    size="small"
                    onClick={() =>
                      setLayout((current) => ({
                        ...current,
                        images: [
                          ...current.images,
                          { x: 4, y: 12, width: 22, height: 28, kind: 'photo', border: true },
                        ],
                      }))
                    }
                  >
                    Thêm ô ảnh
                  </Button>
                  <Button
                    size="small"
                    onClick={() =>
                      setLayout((current) => ({
                        ...current,
                        images: [
                          ...current.images,
                          { x: 2, y: 2, width: 8, height: 8, kind: 'logo', border: false },
                        ],
                      }))
                    }
                  >
                    Thêm logo
                  </Button>
                  <Button
                    size="small"
                    disabled={Boolean(layout.barcode)}
                    onClick={() =>
                      setLayout((current) => ({
                        ...current,
                        barcode: {
                          x: 30,
                          y: 42,
                          width: 40,
                          height: 8,
                          type: 'Code128',
                          showText: true,
                          fontSize: 5.5,
                        },
                      }))
                    }
                  >
                    Thêm mã vạch
                  </Button>
                  <Button
                    size="small"
                    type="primary"
                    icon={<PlusOutlined />}
                    onClick={() =>
                      setLayout((current) => ({
                        ...current,
                        boxes: [
                          ...current.boxes,
                          {
                            x: 30,
                            y: 4 + current.boxes.length * 5,
                            width: 50,
                            height: 5,
                            source: 'fullName',
                            fontSize: 8,
                            align: 'left',
                          },
                        ],
                      }))
                    }
                  >
                    Thêm ô chữ
                  </Button>
                </Space>
              }
            >
              <Space wrap>
                <Space size={4}>
                  <span>Màu nền</span>
                  <ColorPicker
                    value={layout.backgroundColor ?? '#FFFFFF'}
                    onChangeComplete={(color) =>
                      setLayout((current) => ({ ...current, backgroundColor: color.toHexString() }))
                    }
                  />
                </Space>
                <Space size={4}>
                  <span>Dải màu đầu thẻ (mm)</span>
                  <InputNumber<number>
                    size="small"
                    min={0}
                    max={height}
                    step={0.5}
                    value={layout.headerBandHeight ?? 0}
                    onChange={(value) =>
                      setLayout((current) => ({ ...current, headerBandHeight: value ?? 0 }))
                    }
                  />
                  <ColorPicker
                    value={layout.headerBandColor ?? MAU.chinh}
                    onChangeComplete={(color) =>
                      setLayout((current) => ({ ...current, headerBandColor: color.toHexString() }))
                    }
                  />
                </Space>
              </Space>
            </Card>

            <Card size="small" title="Ô chữ trên thẻ">
              <Table
                rowKey={(_, index) => `box-${index}`}
                size="small"
                dataSource={layout.boxes}
                columns={boxColumns}
                pagination={false}
                locale={{ emptyText: 'Chưa có ô chữ nào' }}
              />
            </Card>

            {layout.images.length > 0 && (
              <Card size="small" title="Ô ảnh">
                <Table
                  rowKey={(_, index) => `image-${index}`}
                  size="small"
                  pagination={false}
                  dataSource={layout.images}
                  columns={[
                    {
                      title: 'Loại',
                      dataIndex: 'kind',
                      width: 140,
                      render: (value: CardImageDto['kind'], _row, index) => (
                        <Select
                          size="small"
                          style={{ width: '100%' }}
                          value={value}
                          options={[
                            { value: 'photo', label: 'Ảnh bạn đọc' },
                            { value: 'logo', label: 'Logo thư viện' },
                          ]}
                          onChange={(next) => patchImage(index, { kind: next })}
                        />
                      ),
                    },
                    ...(['x', 'y', 'width', 'height'] as const).map((key) => ({
                      title: { x: 'X', y: 'Y', width: 'Rộng', height: 'Cao' }[key],
                      dataIndex: key,
                      width: 80,
                      render: (value: number, _row: CardImageDto, index: number) => (
                        <InputNumber<number>
                          size="small"
                          min={0}
                          step={0.5}
                          value={value}
                          onChange={(next) => patchImage(index, { [key]: next ?? 0 })}
                          style={{ width: '100%' }}
                        />
                      ),
                    })),
                    {
                      title: '',
                      width: 50,
                      render: (_: unknown, __: CardImageDto, index: number) => (
                        <Button
                          type="link"
                          danger
                          size="small"
                          icon={<DeleteOutlined />}
                          onClick={() =>
                            setLayout((current) => ({
                              ...current,
                              images: current.images.filter((_item, position) => position !== index),
                            }))
                          }
                        />
                      ),
                    },
                  ]}
                />
              </Card>
            )}

            {layout.barcode && (
              <Card
                size="small"
                title="Mã vạch số thẻ"
                extra={
                  <Button
                    type="link"
                    danger
                    size="small"
                    onClick={() => setLayout((current) => ({ ...current, barcode: null }))}
                  >
                    Bỏ mã vạch
                  </Button>
                }
              >
                <Space wrap>
                  {(['x', 'y', 'width', 'height'] as const).map((key) => (
                    <Space key={key} size={4}>
                      <span>{{ x: 'X', y: 'Y', width: 'Rộng', height: 'Cao' }[key]}</span>
                      <InputNumber<number>
                        size="small"
                        min={0}
                        step={0.5}
                        style={{ width: 80 }}
                        value={layout.barcode?.[key]}
                        onChange={(value) => patchBarcode({ [key]: value ?? 0 })}
                      />
                    </Space>
                  ))}
                  <Select
                    size="small"
                    style={{ width: 130 }}
                    value={layout.barcode.type}
                    options={[
                      { value: 'Code128', label: 'Code 128' },
                      { value: 'Code39', label: 'Code 39' },
                      { value: 'QrCode', label: 'Mã QR' },
                    ]}
                    onChange={(value) => patchBarcode({ type: value })}
                  />
                  <Checkbox
                    checked={layout.barcode.showText}
                    onChange={(event) => patchBarcode({ showText: event.target.checked })}
                  >
                    In dãy số dưới vạch
                  </Checkbox>
                </Space>
              </Card>
            )}
          </Space>
        </Col>
      </Row>
    </Drawer>
  );
}

/** Dữ liệu giả để nhìn thấy bố cục khi thiết kế; bản in thật lấy dữ liệu của từng bạn đọc. */
function sampleOf(source: string): string {
  if (source.startsWith('"')) {
    return source.replaceAll('"', '');
  }

  const samples: Record<string, string> = {
    cardNumber: 'TV2026000123',
    fullName: 'Nguyễn Văn An',
    studentCode: '2151010101',
    readerTypeName: 'Sinh viên',
    facultyName: 'Khoa Công nghệ thông tin',
    majorName: 'Kỹ thuật phần mềm',
    className: 'DH21TH1',
    courseYear: 'K21',
    dateOfBirth: '05/09/2005',
    gender: 'Nam',
    cardIssueDate: '01/09/2025',
    cardExpireDate: '31/08/2029',
    email: 'an.nv@sinhvien.edu.vn',
    phone: '0901234567',
    address: '12 Nguyễn Huệ, Quận 1',
    libraryName: 'THƯ VIỆN TRƯỜNG',
    libraryAddress: '123 Đường Số 1, TP.HCM',
    libraryPhone: '028 1234 5678',
  };

  return samples[source] ?? source;
}
