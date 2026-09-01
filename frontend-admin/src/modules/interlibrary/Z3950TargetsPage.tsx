import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Checkbox,
  Drawer,
  Form,
  Input,
  InputNumber,
  Select,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import {
  ApiOutlined,
  DeleteOutlined,
  EditOutlined,
  PlusOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { interLibraryApi } from './api';
import {
  charsetOptions,
  describeTarget,
  formatDateTime,
  formatDuration,
  recordSyntaxOptions,
} from './labels';
import type { Z3950CheckResultDto, Z3950TargetDto } from './types';

/**
 * Mục 3.3 — Khai báo máy chủ thư viện bạn.
 *
 * Mỗi máy chủ khai theo một trong hai đường: Z39.50 trên TCP cổng 210, hoặc SRU trên HTTP. Nút
 * "Kiểm tra" không chỉ mở cổng rồi báo được: nó bắt tay đầy đủ và tra thử một từ khóa, vì nhiều
 * máy chủ mở cổng nhưng từ chối phiên hoặc không có cơ sở dữ liệu như đã khai.
 */
export function Z3950TargetsPage() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [editing, setEditing] = useState<Z3950TargetDto | null>(null);
  const [creating, setCreating] = useState(false);
  const [checkResult, setCheckResult] = useState<Z3950CheckResultDto | null>(null);
  const [form] = Form.useForm();

  const useSru = Form.useWatch('useSru', form) as boolean | undefined;

  const targets = useQuery({
    queryKey: ['ill-targets'],
    queryFn: () => interLibraryApi.targets(true),
  });

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      interLibraryApi.saveTarget(values, editing?.id),
    onSuccess: () => {
      message.success('Đã lưu máy chủ.');
      close();
      void queryClient.invalidateQueries({ queryKey: ['ill-targets'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  const remove = useMutation({
    mutationFn: (id: string) => interLibraryApi.deleteTarget(id),
    onSuccess: () => {
      message.success('Đã xóa máy chủ.');
      void queryClient.invalidateQueries({ queryKey: ['ill-targets'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xóa được.'),
  });

  const check = useMutation({
    mutationFn: (id: string) => interLibraryApi.checkTarget(id),
    onSuccess: (result) => {
      setCheckResult(result);

      if (result.success) {
        message.success(result.message);
      } else {
        message.error(result.message);
      }

      void queryClient.invalidateQueries({ queryKey: ['ill-targets'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không kiểm tra được.'),
  });

  const close = () => {
    setEditing(null);
    setCreating(false);
    form.resetFields();
  };

  const open = (target: Z3950TargetDto | null) => {
    setEditing(target);
    setCreating(target === null);

    form.setFieldsValue(
      target ?? {
        port: 210,
        charset: 'UTF-8',
        recordSyntax: 'USMARC',
        timeoutSeconds: 20,
        isActive: true,
        showOnOpac: false,
        useSru: false,
        sortOrder: 0,
      },
    );
  };

  const columns: ColumnsType<Z3950TargetDto> = [
    {
      title: 'Tên máy chủ',
      dataIndex: 'name',
      width: 260,
      render: (name: string, row) => (
        <Space direction="vertical" size={0}>
          <span>{name}</span>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {describeTarget(row)}
          </Typography.Text>
        </Space>
      ),
    },
    {
      title: 'Giao thức',
      width: 170,
      render: (_, row) =>
        row.useSru ? (
          <Tag color="blue">SRU</Tag>
        ) : (
          <Space size={4} wrap>
            <Tag color="purple">Z39.50</Tag>
            {row.sruBaseUrl ? (
              <Tooltip title={`Khi máy chủ từ chối trả biểu ghi, hệ thống lấy qua ${row.sruBaseUrl}`}>
                <Tag color="blue">+ SRU dự phòng</Tag>
              </Tooltip>
            ) : null}
          </Space>
        ),
    },
    { title: 'Bảng mã', dataIndex: 'charset', width: 110 },
    { title: 'Cú pháp biểu ghi', dataIndex: 'recordSyntax', width: 150 },
    {
      title: 'Chờ tối đa',
      dataIndex: 'timeoutSeconds',
      width: 110,
      align: 'right',
      render: (value: number) => `${value} giây`,
    },
    {
      title: 'Trạng thái',
      width: 150,
      render: (_, row) => (
        <Space size={4} wrap>
          {row.isActive ? <Tag color="green">Đang bật</Tag> : <Tag>Đã tắt</Tag>}
          {row.showOnOpac && <Tag color="cyan">Hiện trên OPAC</Tag>}
        </Space>
      ),
    },
    {
      title: 'Lần kiểm tra cuối',
      width: 240,
      render: (_, row) =>
        row.lastCheckedAt ? (
          <Space direction="vertical" size={0}>
            <Tag color={row.lastCheckOk ? 'green' : 'red'}>
              {row.lastCheckOk ? 'Tốt' : 'Hỏng'}
            </Tag>
            <Tooltip title={row.lastCheckMessage}>
              <Typography.Text type="secondary" style={{ fontSize: 12 }} ellipsis>
                {formatDateTime(row.lastCheckedAt)}
              </Typography.Text>
            </Tooltip>
          </Space>
        ) : (
          <Typography.Text type="secondary">Chưa kiểm tra</Typography.Text>
        ),
    },
    {
      title: '',
      width: 200,
      render: (_, row) => (
        <Space size={2}>
          <Can permission={PERMISSIONS.interlibrary.targetManage}>
            <Button
              type="link"
              size="small"
              icon={<ApiOutlined />}
              loading={check.isPending && check.variables === row.id}
              onClick={() => check.mutate(row.id)}
            >
              Kiểm tra
            </Button>
          </Can>
          <Can permission={PERMISSIONS.interlibrary.targetManage}>
            <Button type="link" size="small" icon={<EditOutlined />} onClick={() => open(row)} />
          </Can>
          <Can permission={PERMISSIONS.interlibrary.targetManage}>
            <Button
              type="link"
              size="small"
              danger
              icon={<DeleteOutlined />}
              onClick={() =>
                modal.confirm({
                  title: `Xóa máy chủ "${row.name}"?`,
                  okText: 'Xóa',
                  cancelText: 'Hủy',
                  onOk: () => remove.mutateAsync(row.id),
                })
              }
            />
          </Can>
        </Space>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Máy chủ thư viện bạn"
        description="Khai báo các thư viện tra cứu sang được qua Z39.50 hoặc SRU, và kiểm tra kết nối tới từng nơi."
        actions={
          <Can permission={PERMISSIONS.interlibrary.targetManage}>
            <Button type="primary" icon={<PlusOutlined />} onClick={() => open(null)}>
              Thêm máy chủ
            </Button>
          </Can>
        }
      />

      {checkResult && (
        <Alert
          type={checkResult.success ? 'success' : 'error'}
          showIcon
          closable
          onClose={() => setCheckResult(null)}
          message={checkResult.message}
          description={
            <Space size={16} wrap>
              <span>Thời gian: {formatDuration(checkResult.durationMs)}</span>
              {checkResult.serverName && <span>Máy chủ khai tên: {checkResult.serverName}</span>}
              {checkResult.serverVersion && <span>Phiên bản: {checkResult.serverVersion}</span>}
            </Space>
          }
        />
      )}

      <Alert
        type="info"
        showIcon
        message="Z39.50 và SRU khác nhau ở đường truyền, không khác ở kết quả"
        description="Z39.50 chạy trên TCP cổng 210 và hay bị tường lửa chặn; SRU là bản HTTP của cùng giao thức nên đi qua tường lửa dễ hơn. Thư viện nào cho cả hai thì nên khai cả hai, tra cứu tự dùng đường nào chạy được."
      />

      <Table
        rowKey="id"
        size="small"
        loading={targets.isFetching}
        dataSource={targets.data ?? []}
        columns={columns}
        scroll={{ x: 1400 }}
        pagination={false}
      />

      <Drawer
        open={editing !== null || creating}
        onClose={close}
        width={560}
        title={editing ? `Sửa "${editing.name}"` : 'Thêm máy chủ thư viện bạn'}
        extra={
          <Space>
            <Button onClick={close}>Hủy</Button>
            <Button
              type="primary"
              loading={save.isPending}
              onClick={() => void form.validateFields().then((values) => save.mutate(values))}
            >
              Lưu
            </Button>
          </Space>
        }
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="name"
            label="Tên máy chủ"
            rules={[{ required: true, message: 'Chưa nhập tên máy chủ.' }]}
          >
            <Input placeholder="Ví dụ: Thư viện Quốc hội Mỹ" />
          </Form.Item>

          <Form.Item
            name="useSru"
            valuePropName="checked"
            extra="Bật khi thư viện bạn cho tra qua HTTP thay vì mở cổng Z39.50."
          >
            <Checkbox>Tra qua SRU (HTTP)</Checkbox>
          </Form.Item>

          {useSru ? (
            <Form.Item
              name="sruBaseUrl"
              label="Địa chỉ SRU"
              rules={[{ required: true, message: 'Chưa nhập địa chỉ SRU.' }]}
            >
              <Input placeholder="http://lx2.loc.gov:210/lcdb" />
            </Form.Item>
          ) : (
            <>
              <Form.Item
                name="host"
                label="Địa chỉ máy chủ"
                rules={[{ required: true, message: 'Chưa nhập địa chỉ máy chủ.' }]}
              >
                <Input placeholder="lx2.loc.gov" />
              </Form.Item>

              <Space size={16} style={{ display: 'flex' }}>
                <Form.Item name="port" label="Cổng" style={{ flex: 1 }}>
                  <InputNumber min={1} max={65535} style={{ width: '100%' }} />
                </Form.Item>

                <Form.Item
                  name="databaseName"
                  label="Cơ sở dữ liệu"
                  style={{ flex: 2 }}
                  rules={[{ required: true, message: 'Chưa nhập tên cơ sở dữ liệu.' }]}
                >
                  <Input placeholder="LCDB" />
                </Form.Item>
              </Space>

              <Form.Item
                name="sruBaseUrl"
                label="Địa chỉ SRU dự phòng"
                extra={
                  'Không bắt buộc. Nhiều máy chủ nhận truy vấn và báo đúng số kết quả nhưng từ ' +
                  'chối trả biểu ghi; khai địa chỉ SRU của chính thư viện đó vào đây thì hệ thống ' +
                  'tự lấy qua lối kia thay vì trả danh sách rỗng.'
                }
              >
                <Input placeholder="http://lx2.loc.gov:210/lcdb" />
              </Form.Item>
            </>
          )}

          <Space size={16} style={{ display: 'flex' }}>
            <Form.Item name="username" label="Tài khoản" style={{ flex: 1 }}>
              <Input placeholder="Để trống nếu tra công khai" />
            </Form.Item>

            <Form.Item
              name="password"
              label="Mật khẩu"
              style={{ flex: 1 }}
              extra={editing ? 'Bỏ trống thì giữ mật khẩu cũ.' : undefined}
            >
              <Input.Password autoComplete="new-password" />
            </Form.Item>
          </Space>

          <Space size={16} style={{ display: 'flex' }}>
            <Form.Item
              name="charset"
              label="Bảng mã biểu ghi"
              style={{ flex: 1 }}
              extra="Thư viện Mỹ thường dùng MARC-8."
            >
              <Select options={charsetOptions} />
            </Form.Item>

            <Form.Item name="recordSyntax" label="Cú pháp biểu ghi" style={{ flex: 1 }}>
              <Select options={recordSyntaxOptions} />
            </Form.Item>
          </Space>

          <Space size={16} style={{ display: 'flex' }}>
            <Form.Item name="timeoutSeconds" label="Chờ tối đa (giây)" style={{ flex: 1 }}>
              <InputNumber min={1} max={300} style={{ width: '100%' }} />
            </Form.Item>

            <Form.Item name="sortOrder" label="Thứ tự hiển thị" style={{ flex: 1 }}>
              <InputNumber min={0} max={999} style={{ width: '100%' }} />
            </Form.Item>
          </Space>

          <Space direction="vertical">
            <Form.Item name="isActive" valuePropName="checked" noStyle>
              <Checkbox>Đang dùng</Checkbox>
            </Form.Item>
            <Form.Item name="showOnOpac" valuePropName="checked" noStyle>
              <Checkbox>Cho bạn đọc tra sang từ trang tra cứu</Checkbox>
            </Form.Item>
          </Space>
        </Form>
      </Drawer>
    </Space>
  );
}
