import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  App,
  AutoComplete,
  Button,
  Card,
  Col,
  Drawer,
  Empty,
  Form,
  Input,
  InputNumber,
  Popconfirm,
  Row,
  Select,
  Space,
  Switch,
  Table,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import {
  DeleteOutlined,
  EditOutlined,
  MergeCellsOutlined,
  PlusOutlined,
  SyncOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { applyApiError, errorMessage } from '@/api/formErrors';
import { marcApi } from '@/modules/marc/api';
import { catalogingApi } from './api';
import { FILTER_LABEL_PARAM } from './bibListFilters';
import type { CustomIndex, CustomIndexValue } from './customIndexTypes';

const MONOSPACE = { fontFamily: 'ui-monospace, Consolas, monospace' } as const;

/**
 * Danh mục tự tạo từ trường MARC 21 (II.9).
 *
 * A library declares a list by pointing at a tag and subfield — "Nơi xuất bản" is 260$a — and the
 * system harvests the distinct values out of every record. What makes this useful rather than a
 * curiosity is the two steps that follow: the values can be merged so the same place written three
 * ways becomes one entry, and each value then works as a filter over the catalogue.
 */
export function CustomIndexPage() {
  const { message } = App.useApp();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [editing, setEditing] = useState<CustomIndex | null>(null);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [selectedIndex, setSelectedIndex] = useState<CustomIndex | null>(null);
  const [valueKeyword, setValueKeyword] = useState('');
  const [selectedValues, setSelectedValues] = useState<string[]>([]);

  const indexes = useQuery({
    queryKey: ['custom-indexes'],
    queryFn: () => catalogingApi.customIndexes(),
  });

  const values = useQuery({
    queryKey: ['custom-index-values', selectedIndex?.id, valueKeyword],
    queryFn: () => catalogingApi.customIndexValues(selectedIndex!.id, valueKeyword || undefined),
    enabled: Boolean(selectedIndex),
  });

  const harvest = useMutation({
    mutationFn: (id: string) => catalogingApi.harvestCustomIndex(id),
    onSuccess: async (result) => {
      message.success(
        `Quét xong: ${result.distinctValues} giá trị, ${result.newValues} giá trị mới, ` +
          `${result.recordsScanned} liên kết biểu ghi.`,
      );

      await queryClient.invalidateQueries({ queryKey: ['custom-indexes'] });
      await queryClient.invalidateQueries({ queryKey: ['custom-index-values'] });
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const remove = useMutation({
    mutationFn: (id: string) => catalogingApi.deleteCustomIndex(id),
    onSuccess: async () => {
      message.success('Đã xóa danh mục tự tạo.');
      setSelectedIndex(null);
      await queryClient.invalidateQueries({ queryKey: ['custom-indexes'] });
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const merge = useMutation({
    mutationFn: ({ keepId, mergeIds }: { keepId: string; mergeIds: string[] }) =>
      catalogingApi.mergeCustomIndexValues(selectedIndex!.id, keepId, mergeIds),
    onSuccess: async (count) => {
      message.success(`Đã gộp ${count} giá trị. Lần quét sau sẽ không tạo lại các cách viết đã gộp.`);
      setSelectedValues([]);
      await queryClient.invalidateQueries({ queryKey: ['custom-index-values'] });
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const confirmMerge = () => {
    if (selectedValues.length < 2) {
      message.warning('Chọn ít nhất hai giá trị để gộp.');
      return;
    }

    const rows = (values.data ?? []).filter((value) => selectedValues.includes(value.id));
    // The entry used by the most records is the one to keep: it is almost always the correct
    // spelling, and keeping it means the fewest links have to move.
    const keep = rows.reduce((best, row) => (row.recordCount > best.recordCount ? row : best), rows[0]!);

    merge.mutate({
      keepId: keep.id,
      mergeIds: rows.filter((row) => row.id !== keep.id).map((row) => row.id),
    });
  };

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Danh mục tự tạo từ trường MARC"
        description="Khai báo một danh mục bằng cách chỉ định trường và trường con nguồn, ví dụ 260$a cho nơi xuất bản. Hệ thống quét toàn bộ biểu ghi, rút giá trị duy nhất, cho gộp các cách viết trùng, rồi dùng làm bộ lọc tra cứu."
        actions={
          <Can permission={PERMISSIONS.catalogList.customIndex}>
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => {
                setEditing(null);
                setDrawerOpen(true);
              }}
            >
              Khai báo danh mục
            </Button>
          </Can>
        }
      />

      <Row gutter={16}>
        <Col xs={24} lg={10}>
          <Card size="small" title="Các danh mục đã khai báo" styles={{ body: { padding: 0 } }}>
            <Table<CustomIndex>
              rowKey="id"
              size="small"
              loading={indexes.isFetching}
              dataSource={indexes.data ?? []}
              pagination={false}
              locale={{ emptyText: <Empty description="Chưa khai báo danh mục tự tạo nào" /> }}
              rowClassName={(row) => (row.id === selectedIndex?.id ? 'ant-table-row-selected' : '')}
              onRow={(row) => ({
                onClick: () => {
                  setSelectedIndex(row);
                  setSelectedValues([]);
                },
                style: { cursor: 'pointer' },
              })}
              columns={[
                {
                  title: 'Danh mục',
                  render: (_, row) => (
                    <Space direction="vertical" size={0}>
                      <Typography.Text strong>{row.name}</Typography.Text>
                      <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                        <span style={MONOSPACE}>
                          {row.marcTag}${row.marcSubfield}
                        </span>
                        {row.sourceFieldName ? ` — ${row.sourceFieldName}` : ''}
                      </Typography.Text>
                    </Space>
                  ),
                },
                {
                  title: 'Giá trị',
                  dataIndex: 'valueCount',
                  width: 90,
                  align: 'right',
                  render: (value: number, row) =>
                    row.lastHarvestAt ? (
                      value
                    ) : (
                      <Tooltip title="Chưa quét lần nào">
                        <Tag color="orange">chưa quét</Tag>
                      </Tooltip>
                    ),
                },
                {
                  title: '',
                  width: 120,
                  align: 'right',
                  render: (_, row) => (
                    <Space size={0}>
                      <Can permission={PERMISSIONS.catalogList.customIndex}>
                        <Tooltip title="Quét lại từ biểu ghi">
                          <Button
                            type="text"
                            icon={<SyncOutlined />}
                            loading={harvest.isPending && harvest.variables === row.id}
                            onClick={(event) => {
                              event.stopPropagation();
                              harvest.mutate(row.id);
                            }}
                          />
                        </Tooltip>
                      </Can>
                      <Can permission={PERMISSIONS.catalogList.customIndex}>
                        <Tooltip title="Sửa">
                          <Button
                            type="text"
                            icon={<EditOutlined />}
                            onClick={(event) => {
                              event.stopPropagation();
                              setEditing(row);
                              setDrawerOpen(true);
                            }}
                          />
                        </Tooltip>
                      </Can>
                      <Can permission={PERMISSIONS.catalogList.customIndex}>
                        <Popconfirm
                          title={`Xóa danh mục "${row.name}"?`}
                          description="Các giá trị đã rút và liên kết với biểu ghi cũng bị xóa. Biểu ghi không đổi."
                          okText="Xóa"
                          cancelText="Không"
                          onConfirm={() => remove.mutate(row.id)}
                        >
                          <Button
                            type="text"
                            danger
                            icon={<DeleteOutlined />}
                            onClick={(event) => event.stopPropagation()}
                          />
                        </Popconfirm>
                      </Can>
                    </Space>
                  ),
                },
              ]}
            />
          </Card>
        </Col>

        <Col xs={24} lg={14}>
          {selectedIndex ? (
            <Card
              size="small"
              title={`Giá trị của "${selectedIndex.name}"`}
              extra={
                <Space>
                  <Input.Search
                    value={valueKeyword}
                    onChange={(event) => setValueKeyword(event.target.value)}
                    placeholder="Tìm giá trị"
                    allowClear
                    style={{ width: 200 }}
                  />
                  <Can permission={PERMISSIONS.catalogList.customIndex}>
                    <Button
                      icon={<MergeCellsOutlined />}
                      disabled={selectedValues.length < 2}
                      loading={merge.isPending}
                      onClick={confirmMerge}
                    >
                      Gộp {selectedValues.length > 0 ? selectedValues.length : ''}
                    </Button>
                  </Can>
                </Space>
              }
            >
              <Table<CustomIndexValue>
                rowKey="id"
                size="small"
                loading={values.isFetching}
                dataSource={values.data ?? []}
                pagination={{ pageSize: 20, showSizeChanger: true }}
                rowSelection={{
                  selectedRowKeys: selectedValues,
                  onChange: (keys) => setSelectedValues(keys as string[]),
                }}
                locale={{
                  emptyText: (
                    <Empty
                      description={
                        selectedIndex.lastHarvestAt
                          ? 'Không tìm thấy giá trị nào'
                          : 'Chưa quét lần nào — bấm nút quét ở danh sách bên trái'
                      }
                    />
                  ),
                }}
                columns={[
                  { title: 'Giá trị', dataIndex: 'name' },
                  {
                    title: 'Số biểu ghi',
                    dataIndex: 'recordCount',
                    width: 130,
                    align: 'right',
                    render: (value: number, row) =>
                      value === 0 ? (
                        <Typography.Text type="secondary">0</Typography.Text>
                      ) : (
                        <Button
                          type="link"
                          size="small"
                          onClick={() =>
                            navigate(
                              `/bien-muc?customIndexValueId=${row.id}&${FILTER_LABEL_PARAM}=${encodeURIComponent(row.name)}`,
                            )
                          }
                        >
                          {value}
                        </Button>
                      ),
                  },
                ]}
              />

              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                Khi gộp, giá trị được nhiều biểu ghi dùng nhất sẽ được giữ lại; các cách viết còn lại
                được ghi nhớ để lần quét sau không tạo lại chúng.
              </Typography.Text>
            </Card>
          ) : (
            <Card>
              <Empty description="Chọn một danh mục ở bên trái để xem các giá trị đã rút được" />
            </Card>
          )}
        </Col>
      </Row>

      <CustomIndexDrawer
        open={drawerOpen}
        index={editing}
        onClose={() => setDrawerOpen(false)}
        onSaved={async () => {
          setDrawerOpen(false);
          await queryClient.invalidateQueries({ queryKey: ['custom-indexes'] });
        }}
      />
    </Space>
  );
}

function CustomIndexDrawer({
  open,
  index,
  onClose,
  onSaved,
}: {
  open: boolean;
  index: CustomIndex | null;
  onClose: () => void;
  onSaved: () => void | Promise<void>;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm();
  const tag = Form.useWatch('marcTag', form) as string | undefined;

  const definitions = useQuery({
    queryKey: ['marc-fields', '', false],
    queryFn: () => marcApi.getFields(),
    staleTime: 10 * 60 * 1000,
    enabled: open,
  });

  const field = (definitions.data ?? []).find((item) => item.tag === tag);

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      catalogingApi.saveCustomIndex(index?.id ?? null, values),
    onSuccess: async () => {
      message.success(index ? 'Đã cập nhật danh mục tự tạo.' : 'Đã khai báo danh mục tự tạo.');
      form.resetFields();
      await onSaved();
    },
    onError: (error: unknown) => message.error(applyApiError(form, error)),
  });

  return (
    <Drawer
      open={open}
      onClose={onClose}
      width={560}
      title={index ? `Sửa danh mục "${index.name}"` : 'Khai báo danh mục tự tạo'}
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
      <Form
        form={form}
        layout="vertical"
        initialValues={
          index ?? { showAsFacet: true, isActive: true, sortOrder: 0, marcTag: '260', marcSubfield: 'a' }
        }
        onFinish={(values) => save.mutate(values)}
      >
        <Form.Item
          name="name"
          label="Tên danh mục"
          rules={[{ required: true, message: 'Chưa nhập tên danh mục.' }]}
        >
          <Input placeholder="Ví dụ: Nơi xuất bản" />
        </Form.Item>

        <Form.Item name="description" label="Mô tả">
          <Input.TextArea rows={2} placeholder="Giải thích danh mục này dùng để làm gì" />
        </Form.Item>

        <Space size={12} align="start">
          <Form.Item
            name="marcTag"
            label="Trường MARC nguồn"
            rules={[
              { required: true, message: 'Chưa chọn trường nguồn.' },
              { pattern: /^[0-9]{3}$/, message: 'Nhãn trường gồm đúng 3 chữ số.' },
            ]}
          >
            <AutoComplete
              options={(definitions.data ?? [])
                .filter((item) => !item.isControl)
                .map((item) => ({ value: item.tag, label: `${item.tag} — ${item.name}` }))}
              style={{ width: 260 }}
              filterOption={(input, option) =>
                (option?.label as string).toLowerCase().includes(input.toLowerCase())
              }
            />
          </Form.Item>

          <Form.Item
            name="marcSubfield"
            label="Trường con"
            rules={[{ required: true, message: 'Chưa chọn trường con.' }]}
          >
            <Select
              style={{ width: 220 }}
              options={(field?.subfields ?? []).map((subfield) => ({
                value: subfield.code,
                label: `$${subfield.code} — ${subfield.name}`,
              }))}
              placeholder={field ? 'Chọn trường con' : 'Chọn trường trước'}
              showSearch
              optionFilterProp="label"
            />
          </Form.Item>
        </Space>

        {index && (
          <Typography.Paragraph type="warning">
            Đổi trường nguồn sẽ xóa toàn bộ giá trị đã rút, vì chúng không còn mô tả đúng danh mục này.
          </Typography.Paragraph>
        )}

        <Form.Item name="sortOrder" label="Thứ tự hiển thị">
          <InputNumber min={0} max={100000} style={{ width: 140 }} />
        </Form.Item>

        <Space size={20}>
          <Form.Item name="showAsFacet" valuePropName="checked" label="Hiện làm bộ lọc trên tra cứu">
            <Switch />
          </Form.Item>
          <Form.Item name="isActive" valuePropName="checked" label="Đang sử dụng">
            <Switch />
          </Form.Item>
        </Space>
      </Form>
    </Drawer>
  );
}
