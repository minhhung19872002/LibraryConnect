import { useMemo, useState } from 'react';
import {
  App,
  Button,
  Card,
  Checkbox,
  Col,
  Drawer,
  Empty,
  Form,
  Input,
  InputNumber,
  Popconfirm,
  Progress,
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
import { usePermission } from '@/hooks/usePermission';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { locationsApi } from './api';
import { warehouseTypeLabels } from './labels';
import type { LibraryDto, ShelfDto, WarehouseDto, WarehouseType } from './types';

/**
 * III.3 — Quản lý kho.
 *
 * Ba mức lồng nhau: thư viện chứa kho, kho chứa giá. Màn hình đi theo đúng thứ tự đó vì đó là thứ
 * tự cán bộ phải khai — không có thư viện thì không tạo được kho, không có kho thì không có chỗ để
 * xếp một cuốn sách.
 */
export function WarehousePage() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const { can } = usePermission();

  const [selectedLibrary, setSelectedLibrary] = useState<string | null>(null);
  const [selectedWarehouse, setSelectedWarehouse] = useState<string | null>(null);

  const [libraryForm] = Form.useForm();
  const [warehouseForm] = Form.useForm();
  const [shelfForm] = Form.useForm();

  const [libraryDrawer, setLibraryDrawer] = useState<{ open: boolean; id: string | null }>({
    open: false,
    id: null,
  });
  const [warehouseDrawer, setWarehouseDrawer] = useState<{ open: boolean; id: string | null }>({
    open: false,
    id: null,
  });
  const [shelfDrawer, setShelfDrawer] = useState<{ open: boolean; id: string | null }>({
    open: false,
    id: null,
  });

  const libraries = useQuery({
    queryKey: ['acq-libraries'],
    queryFn: () => locationsApi.libraries(true),
  });

  const warehouses = useQuery({
    queryKey: ['acq-warehouses', selectedLibrary],
    queryFn: () => locationsApi.warehouses(selectedLibrary, true),
  });

  const shelves = useQuery({
    queryKey: ['acq-shelves', selectedWarehouse],
    queryFn: () => locationsApi.shelves(selectedWarehouse, true),
    enabled: Boolean(selectedWarehouse),
  });

  const shelfMap = useQuery({
    queryKey: ['acq-shelf-map', selectedWarehouse],
    queryFn: () => locationsApi.shelfMap(selectedWarehouse!),
    enabled: Boolean(selectedWarehouse),
  });

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['acq-libraries'] });
    void queryClient.invalidateQueries({ queryKey: ['acq-warehouses'] });
    void queryClient.invalidateQueries({ queryKey: ['acq-shelves'] });
    void queryClient.invalidateQueries({ queryKey: ['acq-shelf-map'] });
  };

  const fail = (error: unknown) => {
    message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.');
  };

  const saveLibrary = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      locationsApi.saveLibrary(libraryDrawer.id, values),
    onSuccess: () => {
      message.success('Đã lưu thư viện.');
      setLibraryDrawer({ open: false, id: null });
      refresh();
    },
    onError: fail,
  });

  const saveWarehouse = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      locationsApi.saveWarehouse(warehouseDrawer.id, values),
    onSuccess: () => {
      message.success('Đã lưu kho.');
      setWarehouseDrawer({ open: false, id: null });
      refresh();
    },
    onError: fail,
  });

  const saveShelf = useMutation({
    mutationFn: (values: Record<string, unknown>) => locationsApi.saveShelf(shelfDrawer.id, values),
    onSuccess: () => {
      message.success('Đã lưu giá.');
      setShelfDrawer({ open: false, id: null });
      refresh();
    },
    onError: fail,
  });

  const removeLibrary = useMutation({
    mutationFn: (id: string) => locationsApi.deleteLibrary(id),
    onSuccess: () => {
      message.success('Đã xóa thư viện.');
      refresh();
    },
    onError: fail,
  });

  const removeWarehouse = useMutation({
    mutationFn: (id: string) => locationsApi.deleteWarehouse(id),
    onSuccess: () => {
      message.success('Đã xóa kho.');
      setSelectedWarehouse(null);
      refresh();
    },
    onError: fail,
  });

  const removeShelf = useMutation({
    mutationFn: (id: string) => locationsApi.deleteShelf(id),
    onSuccess: () => {
      message.success('Đã xóa giá.');
      refresh();
    },
    onError: fail,
  });

  const openLibrary = async (row?: LibraryDto) => {
    if (row) {
      const detail = await locationsApi.library(row.id);
      libraryForm.setFieldsValue(detail);
      setLibraryDrawer({ open: true, id: row.id });
    } else {
      libraryForm.resetFields();
      libraryForm.setFieldsValue({ isActive: true, sortOrder: 0 });
      setLibraryDrawer({ open: true, id: null });
    }
  };

  const openWarehouse = async (row?: WarehouseDto) => {
    if (row) {
      const detail = await locationsApi.warehouse(row.id);
      warehouseForm.setFieldsValue(detail);
      setWarehouseDrawer({ open: true, id: row.id });
    } else {
      warehouseForm.resetFields();
      warehouseForm.setFieldsValue({
        isActive: true,
        sortOrder: 0,
        type: 'OpenStack' satisfies WarehouseType,
        libraryId: selectedLibrary ?? libraries.data?.[0]?.id,
      });
      setWarehouseDrawer({ open: true, id: null });
    }
  };

  const openShelf = (row?: ShelfDto) => {
    if (row) {
      shelfForm.setFieldsValue(row);
      setShelfDrawer({ open: true, id: row.id });
    } else {
      shelfForm.resetFields();
      shelfForm.setFieldsValue({ isActive: true, warehouseId: selectedWarehouse });
      setShelfDrawer({ open: true, id: null });
    }
  };

  const libraryColumns: ColumnsType<LibraryDto> = [
    { title: 'Mã', dataIndex: 'code', width: 110 },
    {
      title: 'Tên thư viện / cơ sở',
      dataIndex: 'name',
      render: (value: string, row) => (
        <Space>
          <span>{value}</span>
          {row.isHeadquarters && <Tag color="blue">Trụ sở chính</Tag>}
          {!row.isActive && <Tag>Ngừng dùng</Tag>}
        </Space>
      ),
    },
    { title: 'Địa chỉ', dataIndex: 'address', ellipsis: true },
    {
      title: '',
      width: 100,
      align: 'right',
      render: (_, row) => (
        <Space>
          <Can permission={PERMISSIONS.acquisition.libraryManage}>
            <Tooltip title="Sửa">
              <Button size="small" icon={<EditOutlined />} onClick={() => void openLibrary(row)} />
            </Tooltip>
          </Can>
          <Can permission={PERMISSIONS.acquisition.libraryManage}>
            <Popconfirm
              title="Xóa thư viện này?"
              description="Chỉ xóa được khi thư viện không còn kho nào."
              okText="Xóa"
              cancelText="Bỏ qua"
              onConfirm={() => removeLibrary.mutate(row.id)}
            >
              <Button size="small" danger icon={<DeleteOutlined />} />
            </Popconfirm>
          </Can>
        </Space>
      ),
    },
  ];

  const warehouseColumns: ColumnsType<WarehouseDto> = [
    { title: 'Mã', dataIndex: 'code', width: 110 },
    {
      title: 'Tên kho',
      dataIndex: 'name',
      render: (value: string, row) => (
        <Space>
          <span>{value}</span>
          {row.isClosedForInventory && <Tag color="gold">Đang đóng để kiểm kê</Tag>}
          {!row.isActive && <Tag>Ngừng dùng</Tag>}
        </Space>
      ),
    },
    { title: 'Thư viện', dataIndex: 'libraryName', width: 200 },
    {
      title: 'Loại kho',
      dataIndex: 'type',
      width: 150,
      render: (value: WarehouseType) => warehouseTypeLabels[value] ?? value,
    },
    {
      title: 'Số bản',
      dataIndex: 'itemCount',
      width: 110,
      align: 'right',
      render: (value: number, row) =>
        row.capacity ? `${value} / ${row.capacity}` : String(value),
    },
    {
      title: '',
      width: 100,
      align: 'right',
      render: (_, row) => (
        <Space>
          <Can permission={PERMISSIONS.acquisition.warehouseManage}>
            <Tooltip title="Sửa">
              <Button size="small" icon={<EditOutlined />} onClick={() => void openWarehouse(row)} />
            </Tooltip>
          </Can>
          <Can permission={PERMISSIONS.acquisition.warehouseManage}>
            <Popconfirm
              title="Xóa kho này?"
              description="Chỉ xóa được khi trong kho không còn ấn phẩm."
              okText="Xóa"
              cancelText="Bỏ qua"
              onConfirm={() => removeWarehouse.mutate(row.id)}
            >
              <Button size="small" danger icon={<DeleteOutlined />} />
            </Popconfirm>
          </Can>
        </Space>
      ),
    },
  ];

  const shelfColumns: ColumnsType<ShelfDto> = [
    { title: 'Mã giá', dataIndex: 'code', width: 110 },
    { title: 'Tên giá', dataIndex: 'name' },
    {
      title: 'Vị trí bản đồ',
      width: 130,
      render: (_, row) =>
        row.mapRow && row.mapColumn ? `Hàng ${row.mapRow}, cột ${row.mapColumn}` : '—',
    },
    {
      title: 'Khoảng ký hiệu',
      width: 180,
      render: (_, row) =>
        row.callNumberFrom || row.callNumberTo
          ? `${row.callNumberFrom ?? ''} → ${row.callNumberTo ?? ''}`
          : '—',
    },
    {
      title: 'Số bản',
      dataIndex: 'currentCount',
      width: 110,
      align: 'right',
      render: (value: number, row) => (row.capacity ? `${value} / ${row.capacity}` : String(value)),
    },
    {
      title: '',
      width: 100,
      align: 'right',
      render: (_, row) => (
        <Space>
          <Can permission={PERMISSIONS.acquisition.warehouseManage}>
            <Tooltip title="Sửa">
              <Button size="small" icon={<EditOutlined />} onClick={() => openShelf(row)} />
            </Tooltip>
          </Can>
          <Can permission={PERMISSIONS.acquisition.warehouseManage}>
            <Popconfirm
              title="Xóa giá này?"
              description="Chỉ xóa được khi trên giá không còn ấn phẩm."
              okText="Xóa"
              cancelText="Bỏ qua"
              onConfirm={() => removeShelf.mutate(row.id)}
            >
              <Button size="small" danger icon={<DeleteOutlined />} />
            </Popconfirm>
          </Can>
        </Space>
      ),
    },
  ];

  const warehouseOptions = useMemo(
    () => (warehouses.data ?? []).map((item) => ({ value: item.id, label: item.name })),
    [warehouses.data],
  );

  return (
    <div className="lc-page">
      <PageHeader
        title="Quản lý kho"
        description="Thư viện / cơ sở, kho và giá — ba mức quyết định một ấn phẩm nằm ở đâu."
      />

      <Tabs
        items={[
          {
            key: 'libraries',
            label: 'Thư viện / cơ sở',
            children: (
              <Card
                variant="borderless"
                title="Danh sách thư viện"
                extra={
                  <Can permission={PERMISSIONS.acquisition.libraryManage}>
                    <Button type="primary" icon={<PlusOutlined />} onClick={() => void openLibrary()}>
                      Thêm thư viện
                    </Button>
                  </Can>
                }
              >
                <Table
                  rowKey="id"
                  size="small"
                  loading={libraries.isFetching}
                  columns={libraryColumns}
                  dataSource={libraries.data ?? []}
                  pagination={false}
                  onRow={(row) => ({
                    onClick: () => setSelectedLibrary(row.id),
                  })}
                  rowClassName={(row) => (row.id === selectedLibrary ? 'lc-row-selected' : '')}
                />
                <Typography.Text type="secondary">
                  Bấm vào một thư viện để lọc danh sách kho ở tab bên cạnh.
                </Typography.Text>
              </Card>
            ),
          },
          {
            key: 'warehouses',
            label: 'Kho',
            children: (
              <Card
                variant="borderless"
                title="Danh sách kho"
                extra={
                  <Space>
                    <Select
                      allowClear
                      placeholder="Lọc theo thư viện"
                      style={{ width: 240 }}
                      value={selectedLibrary ?? undefined}
                      onChange={(value) => setSelectedLibrary(value ?? null)}
                      options={(libraries.data ?? []).map((item) => ({
                        value: item.id,
                        label: item.name,
                      }))}
                    />
                    <Can permission={PERMISSIONS.acquisition.warehouseManage}>
                      <Button
                        type="primary"
                        icon={<PlusOutlined />}
                        onClick={() => void openWarehouse()}
                      >
                        Thêm kho
                      </Button>
                    </Can>
                  </Space>
                }
              >
                <Table
                  rowKey="id"
                  size="small"
                  loading={warehouses.isFetching}
                  columns={warehouseColumns}
                  dataSource={warehouses.data ?? []}
                  pagination={false}
                  onRow={(row) => ({ onClick: () => setSelectedWarehouse(row.id) })}
                  rowClassName={(row) => (row.id === selectedWarehouse ? 'lc-row-selected' : '')}
                />
                <Typography.Text type="secondary">
                  Bấm vào một kho để xem danh sách giá và bản đồ kho.
                </Typography.Text>
              </Card>
            ),
          },
          {
            key: 'shelves',
            label: 'Giá và bản đồ kho',
            children: (
              <Space direction="vertical" size="middle" style={{ width: '100%' }}>
                <Card
                  variant="borderless"
                  title="Danh sách giá"
                  extra={
                    <Space>
                      <Select
                        placeholder="Chọn kho"
                        style={{ width: 260 }}
                        value={selectedWarehouse ?? undefined}
                        onChange={(value) => setSelectedWarehouse(value)}
                        options={warehouseOptions}
                      />
                      <Can permission={PERMISSIONS.acquisition.warehouseManage}>
                        <Button
                          type="primary"
                          icon={<PlusOutlined />}
                          disabled={!selectedWarehouse}
                          onClick={() => openShelf()}
                        >
                          Thêm giá
                        </Button>
                      </Can>
                    </Space>
                  }
                >
                  {selectedWarehouse ? (
                    <Table
                      rowKey="id"
                      size="small"
                      loading={shelves.isFetching}
                      columns={shelfColumns}
                      dataSource={shelves.data ?? []}
                      pagination={false}
                    />
                  ) : (
                    <Empty description="Chọn một kho để xem các giá của kho đó." />
                  )}
                </Card>

                {selectedWarehouse && shelfMap.data && (
                  <ShelfMapCard data={shelfMap.data} />
                )}
              </Space>
            ),
          },
        ]}
      />

      <Drawer
        open={libraryDrawer.open}
        onClose={() => setLibraryDrawer({ open: false, id: null })}
        width={520}
        title={libraryDrawer.id ? 'Sửa thư viện' : 'Thêm thư viện'}
        extra={
          <Button
            type="primary"
            loading={saveLibrary.isPending}
            onClick={() => libraryForm.submit()}
            disabled={!can(PERMISSIONS.acquisition.libraryManage)}
          >
            Lưu
          </Button>
        }
      >
        <Form form={libraryForm} layout="vertical" onFinish={(values) => saveLibrary.mutate(values)}>
          <Row gutter={12}>
            <Col span={10}>
              <Form.Item name="code" label="Mã" rules={[{ required: true, message: 'Chưa nhập mã.' }]}>
                <Input placeholder="TRUSO" />
              </Form.Item>
            </Col>
            <Col span={14}>
              <Form.Item name="name" label="Tên thư viện" rules={[{ required: true, message: 'Chưa nhập tên.' }]}>
                <Input placeholder="Thư viện Trụ sở chính" />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="address" label="Địa chỉ">
            <Input />
          </Form.Item>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="phone" label="Điện thoại">
                <Input />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="email" label="Thư điện tử">
                <Input />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="manager" label="Người phụ trách">
                <Input />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="openingHours" label="Giờ mở cửa">
                <Input placeholder="7h30 – 17h00 các ngày trong tuần" />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={12}>
            <Col span={8}>
              <Form.Item name="sortOrder" label="Thứ tự hiển thị">
                <InputNumber min={0} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="isHeadquarters" valuePropName="checked" label=" ">
                <Checkbox>Là trụ sở chính</Checkbox>
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="isActive" valuePropName="checked" label=" ">
                <Checkbox>Đang sử dụng</Checkbox>
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Drawer>

      <Drawer
        open={warehouseDrawer.open}
        onClose={() => setWarehouseDrawer({ open: false, id: null })}
        width={520}
        title={warehouseDrawer.id ? 'Sửa kho' : 'Thêm kho'}
        extra={
          <Button
            type="primary"
            loading={saveWarehouse.isPending}
            onClick={() => warehouseForm.submit()}
            disabled={!can(PERMISSIONS.acquisition.warehouseManage)}
          >
            Lưu
          </Button>
        }
      >
        <Form form={warehouseForm} layout="vertical" onFinish={(values) => saveWarehouse.mutate(values)}>
          <Form.Item
            name="libraryId"
            label="Thuộc thư viện"
            rules={[{ required: true, message: 'Chưa chọn thư viện.' }]}
          >
            <Select
              options={(libraries.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
            />
          </Form.Item>
          <Row gutter={12}>
            <Col span={10}>
              <Form.Item name="code" label="Mã kho" rules={[{ required: true, message: 'Chưa nhập mã.' }]}>
                <Input placeholder="KHOMO" />
              </Form.Item>
            </Col>
            <Col span={14}>
              <Form.Item name="name" label="Tên kho" rules={[{ required: true, message: 'Chưa nhập tên.' }]}>
                <Input placeholder="Kho mở" />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="type" label="Loại kho">
                <Select
                  options={Object.entries(warehouseTypeLabels).map(([value, label]) => ({
                    value,
                    label,
                  }))}
                />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="capacity" label="Sức chứa (số bản)">
                <InputNumber min={1} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="location" label="Vị trí">
            <Input placeholder="Tầng 2, nhà A" />
          </Form.Item>
          <Form.Item
            name="callNumberRule"
            label="Quy tắc ký hiệu xếp giá riêng của kho"
            extra="Bỏ trống thì dùng quy tắc chung ở Biên mục → Cấu hình. Ví dụ: {DDC} {AUTHOR:3}"
          >
            <Input placeholder="{DDC} {AUTHOR:3}" />
          </Form.Item>
          <Form.Item name="description" label="Mô tả">
            <Input.TextArea rows={2} />
          </Form.Item>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="sortOrder" label="Thứ tự hiển thị">
                <InputNumber min={0} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="isActive" valuePropName="checked" label=" ">
                <Checkbox>Đang sử dụng</Checkbox>
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Drawer>

      <Drawer
        open={shelfDrawer.open}
        onClose={() => setShelfDrawer({ open: false, id: null })}
        width={480}
        title={shelfDrawer.id ? 'Sửa giá' : 'Thêm giá'}
        extra={
          <Button
            type="primary"
            loading={saveShelf.isPending}
            onClick={() => shelfForm.submit()}
            disabled={!can(PERMISSIONS.acquisition.warehouseManage)}
          >
            Lưu
          </Button>
        }
      >
        <Form form={shelfForm} layout="vertical" onFinish={(values) => saveShelf.mutate(values)}>
          <Form.Item
            name="warehouseId"
            label="Thuộc kho"
            rules={[{ required: true, message: 'Chưa chọn kho.' }]}
          >
            <Select options={warehouseOptions} />
          </Form.Item>
          <Row gutter={12}>
            <Col span={10}>
              <Form.Item name="code" label="Mã giá" rules={[{ required: true, message: 'Chưa nhập mã.' }]}>
                <Input placeholder="A01" />
              </Form.Item>
            </Col>
            <Col span={14}>
              <Form.Item name="name" label="Tên giá" rules={[{ required: true, message: 'Chưa nhập tên.' }]}>
                <Input placeholder="Giá A01" />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={12}>
            <Col span={8}>
              <Form.Item name="capacity" label="Sức chứa">
                <InputNumber min={1} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="mapRow" label="Hàng trên bản đồ">
                <InputNumber min={1} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="mapColumn" label="Cột trên bản đồ">
                <InputNumber min={1} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="callNumberFrom" label="Ký hiệu từ">
                <Input placeholder="000" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="callNumberTo" label="Ký hiệu đến">
                <Input placeholder="099.99" />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="isActive" valuePropName="checked">
            <Checkbox>Đang sử dụng</Checkbox>
          </Form.Item>
        </Form>
      </Drawer>
    </div>
  );
}

/**
 * Bản đồ kho: lưới giá tô màu theo mức lấp đầy (III.2).
 *
 * Cán bộ cần trả lời một câu duy nhất khi cầm chồng sách trên tay — "còn chỗ ở giá nào" — nên ô nào
 * cũng hiện phần trăm đã dùng, và giá gần đầy được tô đậm dần.
 */
function ShelfMapCard({ data }: { data: import('./types').ShelfMapDto }) {
  const grid = useMemo(() => {
    const cells = new Map<string, (typeof data.cells)[number]>();
    data.cells.forEach((cell) => cells.set(`${cell.row}:${cell.column}`, cell));
    return cells;
  }, [data]);

  if (data.rows === 0 || data.columns === 0) {
    return (
      <Card variant="borderless" title={`Bản đồ kho — ${data.warehouseName}`}>
        <Empty description="Chưa giá nào được đặt vị trí hàng / cột nên chưa vẽ được bản đồ." />
      </Card>
    );
  }

  return (
    <Card
      variant="borderless"
      title={`Bản đồ kho — ${data.warehouseName}`}
      extra={
        <Typography.Text type="secondary">
          {data.itemCount} bản{data.capacity ? ` / sức chứa ${data.capacity}` : ''}
        </Typography.Text>
      }
    >
      <div
        className="lc-shelf-map"
        style={{
          display: 'grid',
          gridTemplateColumns: `repeat(${data.columns}, minmax(120px, 1fr))`,
          gap: 8,
        }}
      >
        {Array.from({ length: data.rows * data.columns }, (_, index) => {
          const row = Math.floor(index / data.columns) + 1;
          const column = (index % data.columns) + 1;
          const cell = grid.get(`${row}:${column}`);

          if (!cell) {
            return <div key={`${row}:${column}`} className="lc-shelf-cell lc-shelf-cell-empty" />;
          }

          const percent = cell.usagePercent ?? null;

          return (
            <Card key={cell.shelfId} size="small" title={cell.code} styles={{ body: { padding: 8 } }}>
              <Typography.Text ellipsis style={{ display: 'block' }}>
                {cell.name}
              </Typography.Text>
              {percent === null ? (
                <Typography.Text type="secondary">{cell.currentCount} bản</Typography.Text>
              ) : (
                <Progress
                  percent={Math.min(100, percent)}
                  size="small"
                  status={percent >= 100 ? 'exception' : percent >= 80 ? 'active' : 'normal'}
                  format={() => `${cell.currentCount}/${cell.capacity}`}
                />
              )}
            </Card>
          );
        })}
      </div>

      {data.unplaced.length > 0 && (
        <Typography.Paragraph type="secondary" style={{ marginTop: 12, marginBottom: 0 }}>
          Chưa đặt vị trí trên bản đồ:{' '}
          {data.unplaced.map((cell) => `${cell.code} (${cell.currentCount} bản)`).join(', ')}
        </Typography.Paragraph>
      )}
    </Card>
  );
}
