import { useEffect, useState } from 'react';
import { App, Button, Card, Form, Input, Modal, Select, Space, Switch, Table, Tag, Typography } from 'antd';
import { DownloadOutlined, PlusOutlined, QrcodeOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { api } from '@/api/client';
import { downloadFile } from '@/api/download';
import { locationsApi } from '@/modules/acquisition/api';
import { circulationApi } from './api';
import type { CheckoutStationDto } from './types';

/**
 * Trạm mượn tự phục vụ (Phase 15, mục 3.2).
 *
 * Mỗi trạm là một mã QR dán tại kho. Bạn đọc mở ứng dụng, quét mã, máy chủ kiểm chữ ký và trạng thái
 * trạm rồi cấp phiếu xác thực vị trí có hạn; sau đó mới quét sách. Tắt trạm là mã trên tường hết tác
 * dụng ngay, không cần bóc.
 */
export function CheckoutStationsPage() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();
  const [form] = Form.useForm<{
    id?: string;
    code: string;
    name: string;
    warehouseId?: string | null;
    location?: string;
    isActive: boolean;
  }>();

  const [editing, setEditing] = useState<CheckoutStationDto | null | undefined>(undefined);
  const [preview, setPreview] = useState<CheckoutStationDto | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);

  const stations = useQuery({
    queryKey: ['circulation-stations'],
    queryFn: () => circulationApi.stations(true),
  });

  const warehouses = useQuery({
    queryKey: ['warehouses-all'],
    queryFn: () => locationsApi.warehouses(null, false),
  });

  const save = useMutation({
    mutationFn: circulationApi.saveStation,
    onSuccess: (station) => {
      message.success(`Đã lưu trạm ${station.code}.`);
      setEditing(undefined);
      void queryClient.invalidateQueries({ queryKey: ['circulation-stations'] });
    },
    onError: (error: Error) => message.error(error.message),
  });

  const remove = useMutation({
    mutationFn: circulationApi.deleteStation,
    onSuccess: () => {
      message.success('Đã xoá trạm.');
      void queryClient.invalidateQueries({ queryKey: ['circulation-stations'] });
    },
    onError: (error: Error) => message.error(error.message),
  });

  // Ảnh QR lấy qua lớp gọi API (mang mã đăng nhập) rồi đưa cho thẻ ảnh bằng địa chỉ blob.
  useEffect(() => {
    if (!preview) {
      setPreviewUrl(null);
      return;
    }

    let url: string | null = null;
    let cancelled = false;

    void api.download(`/circulation/stations/${preview.id}/qr.png?size=480`).then(({ blob }) => {
      if (cancelled) return;
      url = URL.createObjectURL(blob);
      setPreviewUrl(url);
    });

    return () => {
      cancelled = true;
      if (url) URL.revokeObjectURL(url);
    };
  }, [preview]);

  const open = (station?: CheckoutStationDto) => {
    form.setFieldsValue({
      id: station?.id,
      code: station?.code ?? '',
      name: station?.name ?? '',
      warehouseId: station?.warehouseId ?? null,
      location: station?.location ?? '',
      isActive: station?.isActive ?? true,
    });
    setEditing(station ?? null);
  };

  const columns: ColumnsType<CheckoutStationDto> = [
    { title: 'Mã trạm', dataIndex: 'code', width: 140, render: (code: string) => <Typography.Text code>{code}</Typography.Text> },
    { title: 'Tên trạm', dataIndex: 'name', width: 240 },
    { title: 'Kho', dataIndex: 'warehouseName', width: 180, render: (value: string | null) => value ?? '—' },
    { title: 'Vị trí dán mã', dataIndex: 'location', width: 220, render: (value: string | null) => value ?? '—' },
    {
      title: 'Trạng thái',
      dataIndex: 'isActive',
      width: 130,
      render: (active: boolean) => (active ? <Tag color="green">Đang dùng</Tag> : <Tag>Tạm ngừng</Tag>),
    },
    {
      title: 'Thao tác',
      key: 'actions',
      width: 300,
      render: (_, station) => (
        <Space size="small" wrap>
          <Button size="small" icon={<QrcodeOutlined />} onClick={() => setPreview(station)}>
            Xem mã
          </Button>
          <Button
            size="small"
            icon={<DownloadOutlined />}
            onClick={() => downloadFile(`/circulation/stations/${station.id}/qr.png?size=900`, `tram-${station.code}.png`)}
          >
            Tải PNG
          </Button>
          <Can permission={PERMISSIONS.circulation.policyManage}>
            <Button size="small" onClick={() => open(station)}>
              Sửa
            </Button>
            <Button
              size="small"
              danger
              onClick={() =>
                modal.confirm({
                  title: `Xoá trạm ${station.code}?`,
                  content: 'Mã QR đã in của trạm này sẽ không còn dùng được.',
                  okText: 'Xoá',
                  okButtonProps: { danger: true },
                  cancelText: 'Hủy',
                  onOk: () => remove.mutateAsync(station.id),
                })
              }
            >
              Xoá
            </Button>
          </Can>
        </Space>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Trạm mượn tự phục vụ"
        description="Mỗi trạm là một mã QR dán tại kho. Bạn đọc quét mã bằng ứng dụng để chứng minh đang ở trong thư viện rồi mới quét sách; tắt trạm là mã hết tác dụng ngay. Chế độ xác thực chọn ở Tham số hệ thống → Cấu hình lưu thông."
        actions={
          <Can permission={PERMISSIONS.circulation.policyManage}>
            <Button type="primary" icon={<PlusOutlined />} onClick={() => open()}>
              Thêm trạm
            </Button>
          </Can>
        }
      />

      <Card>
        <Table
          rowKey="id"
          size="middle"
          loading={stations.isLoading}
          columns={columns}
          dataSource={stations.data ?? []}
          pagination={false}
          scroll={{ x: 1210 }}
          locale={{ emptyText: 'Chưa có trạm nào — thêm trạm rồi in mã QR dán tại cửa kho.' }}
        />
      </Card>

      <Modal
        open={editing !== undefined}
        title={editing ? `Sửa trạm ${editing.code}` : 'Thêm trạm mượn'}
        okText="Lưu"
        cancelText="Hủy"
        confirmLoading={save.isPending}
        onCancel={() => setEditing(undefined)}
        onOk={() => form.submit()}
      >
        <Form form={form} layout="vertical" onFinish={(values) => save.mutate(values)}>
          <Form.Item name="id" hidden>
            <Input />
          </Form.Item>
          <Form.Item
            name="code"
            label="Mã trạm"
            rules={[
              { required: true, message: 'Nhập mã trạm.' },
              { pattern: /^[A-Za-z0-9_-]+$/, message: 'Chỉ gồm chữ, số, gạch ngang và gạch dưới.' },
            ]}
          >
            <Input placeholder="KHOMO-01" maxLength={50} />
          </Form.Item>
          <Form.Item name="name" label="Tên trạm" rules={[{ required: true, message: 'Nhập tên trạm.' }]}>
            <Input placeholder="Cửa kho mở tầng 2" maxLength={200} />
          </Form.Item>
          <Form.Item name="warehouseId" label="Kho">
            <Select
              allowClear
              placeholder="Chọn kho"
              loading={warehouses.isLoading}
              options={(warehouses.data ?? []).map((warehouse) => ({ value: warehouse.id, label: warehouse.name }))}
            />
          </Form.Item>
          <Form.Item name="location" label="Vị trí dán mã">
            <Input placeholder="Cột bên phải cửa vào kho" maxLength={500} />
          </Form.Item>
          <Form.Item name="isActive" label="Đang dùng" valuePropName="checked">
            <Switch />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        open={preview !== null}
        title={preview ? `Mã QR trạm ${preview.code}` : ''}
        footer={null}
        onCancel={() => setPreview(null)}
      >
        {preview ? (
          <div style={{ textAlign: 'center' }}>
            {previewUrl ? (
              <img src={previewUrl} alt={`Mã QR trạm ${preview.code}`} style={{ width: 320, height: 320 }} />
            ) : (
              <Typography.Text type="secondary">Đang dựng mã…</Typography.Text>
            )}
            <Typography.Paragraph style={{ marginTop: 12 }}>
              <strong>{preview.name}</strong>
              {preview.location ? ` · ${preview.location}` : ''}
            </Typography.Paragraph>
            <Typography.Text type="secondary" code>
              {preview.qrContent}
            </Typography.Text>
          </div>
        ) : null}
      </Modal>
    </>
  );
}
