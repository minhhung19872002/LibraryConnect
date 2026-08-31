import { useEffect, useState } from 'react';
import {
  App,
  Button,
  Checkbox,
  Drawer,
  Empty,
  Form,
  Input,
  InputNumber,
  Modal,
  Popconfirm,
  Radio,
  Select,
  Space,
  Table,
  Tag,
  Typography,
} from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined, PrinterOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { applyApiError, errorMessage } from '@/api/formErrors';
import { saveBlob } from '@/modules/marc/api';
import { cardApi } from './api';
import { CardDesigner } from './CardDesigner';
import {
  CARD_TYPE_LABELS,
  defaultLayout,
  type CardLayout,
  type CardTemplate,
  type CardType,
} from './cardTypes';

/**
 * Mẫu phích và in phích thư mục (II.10).
 *
 * A card is still how many Vietnamese libraries let readers browse the collection, and the four card
 * types are the four ways a drawer can be filed. The designer works in millimetres on a canvas that
 * is the card at scale, so what is laid out here is what comes off the printer.
 */
export function CardTemplatePage() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [editing, setEditing] = useState<CardTemplate | null>(null);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [printOpen, setPrintOpen] = useState(false);

  const templates = useQuery({
    queryKey: ['card-templates'],
    queryFn: () => cardApi.templates(),
  });

  const remove = useMutation({
    mutationFn: (id: string) => cardApi.deleteTemplate(id),
    onSuccess: async () => {
      message.success('Đã xóa mẫu phích.');
      await queryClient.invalidateQueries({ queryKey: ['card-templates'] });
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Mẫu phích và in phích"
        description="Thiết kế mẫu phích theo khổ giấy của thư viện, đặt các ô nội dung ánh xạ tới trường MARC, rồi in hàng loạt ra PDF đúng khổ."
        actions={
          <Space>
            <Can permission={PERMISSIONS.cataloging.cardPrint}>
              <Button icon={<PrinterOutlined />} onClick={() => setPrintOpen(true)}>
                In phích
              </Button>
            </Can>
            <Can permission={PERMISSIONS.cataloging.cardTemplate}>
              <Button
                type="primary"
                icon={<PlusOutlined />}
                onClick={() => {
                  setEditing(null);
                  setDrawerOpen(true);
                }}
              >
                Thêm mẫu phích
              </Button>
            </Can>
          </Space>
        }
      />

      <Table<CardTemplate>
        rowKey="id"
        size="small"
        loading={templates.isFetching}
        dataSource={templates.data ?? []}
        pagination={false}
        locale={{
          emptyText: (
            <Empty description="Chưa thiết kế mẫu phích nào — hệ thống sẽ dùng mẫu chuẩn 12,5 × 7,5 cm khi in" />
          ),
        }}
        columns={[
          {
            title: 'Mẫu phích',
            render: (_, row) => (
              <Space direction="vertical" size={0}>
                <Typography.Text strong>{row.name}</Typography.Text>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  {row.cardTypeName} · {row.widthMm} × {row.heightMm} mm · {row.layout.boxes.length} ô
                </Typography.Text>
              </Space>
            ),
          },
          {
            title: 'Trạng thái',
            width: 200,
            render: (_, row) => (
              <Space size={4}>
                {row.isDefault && <Tag color="blue">Mặc định</Tag>}
                {!row.isActive && <Tag>Đã tắt</Tag>}
              </Space>
            ),
          },
          {
            title: '',
            width: 100,
            align: 'right',
            render: (_, row) => (
              <Space size={0}>
                <Can permission={PERMISSIONS.cataloging.cardTemplate}>
                  <Button
                    type="text"
                    icon={<EditOutlined />}
                    onClick={() => {
                      setEditing(row);
                      setDrawerOpen(true);
                    }}
                  />
                </Can>
                <Can permission={PERMISSIONS.cataloging.cardTemplate}>
                  <Popconfirm
                    title={`Xóa mẫu phích "${row.name}"?`}
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

      <TemplateDrawer
        open={drawerOpen}
        template={editing}
        onClose={() => setDrawerOpen(false)}
        onSaved={async () => {
          setDrawerOpen(false);
          await queryClient.invalidateQueries({ queryKey: ['card-templates'] });
        }}
      />

      <PrintModal
        open={printOpen}
        templates={templates.data ?? []}
        onClose={() => setPrintOpen(false)}
      />
    </Space>
  );
}

function TemplateDrawer({
  open,
  template,
  onClose,
  onSaved,
}: {
  open: boolean;
  template: CardTemplate | null;
  onClose: () => void;
  onSaved: () => void | Promise<void>;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm();
  const [layout, setLayout] = useState<CardLayout>(defaultLayout());

  const widthMm = (Form.useWatch('widthMm', form) as number | undefined) ?? 125;
  const heightMm = (Form.useWatch('heightMm', form) as number | undefined) ?? 75;

  useEffect(() => {
    if (!open) {
      return;
    }

    if (template) {
      form.setFieldsValue(template);
      setLayout(template.layout);
    } else {
      form.resetFields();
      form.setFieldsValue({
        cardType: 'MAIN',
        widthMm: 125,
        heightMm: 75,
        isDefault: false,
        isActive: true,
      });
      setLayout(defaultLayout());
    }
  }, [open, template, form]);

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      cardApi.saveTemplate(template?.id ?? null, { ...values, layout }),
    onSuccess: async () => {
      message.success(template ? 'Đã cập nhật mẫu phích.' : 'Đã thêm mẫu phích.');
      await onSaved();
    },
    onError: (error: unknown) => message.error(applyApiError(form, error)),
  });

  return (
    <Drawer
      open={open}
      onClose={onClose}
      width={1040}
      title={template ? `Sửa mẫu phích "${template.name}"` : 'Thiết kế mẫu phích'}
      extra={
        <Space>
          <Button onClick={onClose}>Hủy</Button>
          <Button type="primary" loading={save.isPending} onClick={() => form.submit()}>
            Lưu mẫu
          </Button>
        </Space>
      }
    >
      <Form form={form} layout="vertical" onFinish={(values) => save.mutate(values)}>
        <Space size={12} align="start" wrap>
          <Form.Item
            name="name"
            label="Tên mẫu"
            rules={[{ required: true, message: 'Chưa nhập tên mẫu phích.' }]}
            style={{ width: 280 }}
          >
            <Input placeholder="Ví dụ: Phích chính khổ 12,5 × 7,5" />
          </Form.Item>

          <Form.Item name="cardType" label="Loại phích" style={{ width: 220 }}>
            <Select
              options={Object.entries(CARD_TYPE_LABELS).map(([value, label]) => ({
                value: value as CardType,
                label,
              }))}
            />
          </Form.Item>

          <Form.Item name="widthMm" label="Rộng (mm)" style={{ width: 120 }}>
            <InputNumber min={50} max={210} style={{ width: '100%' }} />
          </Form.Item>

          <Form.Item name="heightMm" label="Cao (mm)" style={{ width: 120 }}>
            <InputNumber min={40} max={297} style={{ width: '100%' }} />
          </Form.Item>
        </Space>

        <Space size={20} style={{ marginBottom: 16 }}>
          <Form.Item name="isDefault" valuePropName="checked" noStyle>
            <Checkbox>Mẫu mặc định cho loại phích này</Checkbox>
          </Form.Item>
          <Form.Item name="isActive" valuePropName="checked" noStyle>
            <Checkbox>Đang sử dụng</Checkbox>
          </Form.Item>
        </Space>

        <CardDesigner widthMm={widthMm} heightMm={heightMm} layout={layout} onChange={setLayout} />
      </Form>
    </Drawer>
  );
}

function PrintModal({
  open,
  templates,
  onClose,
}: {
  open: boolean;
  templates: CardTemplate[];
  onClose: () => void;
}) {
  const { message } = App.useApp();
  const [templateId, setTemplateId] = useState<string | undefined>();
  const [cardTypes, setCardTypes] = useState<CardType[]>(['MAIN']);
  const [multiplePerPage, setMultiplePerPage] = useState(true);
  const [keyword, setKeyword] = useState('');

  const print = useMutation({
    mutationFn: () =>
      cardApi.print({
        bibIds: [],
        filter: { keyword: keyword.trim() || undefined },
        templateId,
        cardTypes,
        multiplePerPage,
      }),
    onSuccess: ({ blob, fileName }) => {
      saveBlob(blob, fileName);
      message.success(`Đã tạo tệp ${fileName}.`);
      onClose();
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  return (
    <Modal
      open={open}
      title="In phích thư mục"
      okText="Tạo tệp PDF"
      cancelText="Hủy"
      confirmLoading={print.isPending}
      onCancel={onClose}
      onOk={() => print.mutate()}
      width={560}
    >
      <Space direction="vertical" size={14} style={{ width: '100%' }}>
        <div>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Biểu ghi cần in
          </Typography.Text>
          <Input
            value={keyword}
            onChange={(event) => setKeyword(event.target.value)}
            placeholder="Từ khóa lọc biểu ghi; bỏ trống để in toàn bộ"
          />
        </div>

        <div>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Loại phích
          </Typography.Text>
          <Checkbox.Group
            value={cardTypes}
            onChange={(values) => setCardTypes(values as CardType[])}
            options={Object.entries(CARD_TYPE_LABELS).map(([value, label]) => ({ value, label }))}
            style={{ display: 'flex', flexDirection: 'column', gap: 4 }}
          />
          <Typography.Text type="secondary" style={{ fontSize: 11 }}>
            Một biểu ghi có ba đề mục chủ đề sẽ cho ba phích chủ đề.
          </Typography.Text>
        </div>

        <div>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Mẫu phích
          </Typography.Text>
          <Select
            value={templateId}
            onChange={setTemplateId}
            options={templates.map((template) => ({
              value: template.id,
              label: `${template.name} (${template.widthMm}×${template.heightMm} mm)`,
            }))}
            placeholder="Dùng mẫu mặc định"
            allowClear
            style={{ width: '100%' }}
          />
        </div>

        <div>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Cách xếp giấy
          </Typography.Text>
          <Radio.Group
            value={multiplePerPage}
            onChange={(event) => setMultiplePerPage(event.target.value)}
          >
            <Space direction="vertical">
              <Radio value={true}>
                Nhiều phích trên một tờ A4
                <Typography.Text type="secondary" style={{ fontSize: 11, display: 'block' }}>
                  In lên giấy A4 thường rồi cắt rời.
                </Typography.Text>
              </Radio>
              <Radio value={false}>
                Mỗi phích một trang đúng khổ
                <Typography.Text type="secondary" style={{ fontSize: 11, display: 'block' }}>
                  In thẳng lên bìa phích in sẵn.
                </Typography.Text>
              </Radio>
            </Space>
          </Radio.Group>
        </div>
      </Space>
    </Modal>
  );
}
