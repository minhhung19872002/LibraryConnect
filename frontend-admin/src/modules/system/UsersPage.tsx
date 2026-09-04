import { useState } from 'react';
import {
  Alert,
  App,
  Button,
  Card,
  Drawer,
  Form,
  Input,
  Modal,
  Select,
  Space,
  Switch,
  Table,
  Tag,
  Tooltip,
  Typography,
  Upload,
} from 'antd';
import type { ColumnsType } from 'antd/es/table';
import type { UploadFile } from 'antd/es/upload/interface';
import {
  DeleteOutlined,
  DownloadOutlined,
  EditOutlined,
  HistoryOutlined,
  ImportOutlined,
  KeyOutlined,
  LockOutlined,
  PlusOutlined,
  UnlockOutlined,
  UploadOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, http } from '@/api/client';
import { applyApiError, errorMessage } from '@/api/formErrors';
import { PERMISSIONS } from '@/api/permissions';
import { FilterBar } from '@/components/FilterBar';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { usePagedQuery } from '@/hooks/usePagedQuery';
import { messages } from '@/i18n/messages';
import { downloadFile, formatDateTime } from './helpers';
import { buildDataScopes, splitDataScopes } from './dataScopes';
import type {
  LoginHistoryItem,
  UserDetail,
  UserGroupListItem,
  UserImportResult,
  UserListItem,
} from './types';
import type { PagedResult } from '@/types/api';

interface UserFormValues {
  username: string;
  fullName: string;
  email?: string;
  phone?: string;
  position?: string;
  department?: string;
  isActive: boolean;
  groupIds: string[];
  /** Phạm vi dữ liệu (I.2): thư viện và kho người dùng được thao tác; rỗng là không giới hạn. */
  libraryIds: string[];
  warehouseIds: string[];
}

/** Phân hệ I.2 — quản lý tài khoản cán bộ thư viện. */
export function UsersPage() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [keyword, setKeyword] = useState('');
  const [groupId, setGroupId] = useState<string | undefined>();
  const [isActive, setIsActive] = useState<boolean | undefined>();
  const [department, setDepartment] = useState<string | undefined>();

  const [editingId, setEditingId] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [historyUser, setHistoryUser] = useState<UserListItem | null>(null);
  const [importOpen, setImportOpen] = useState(false);

  const list = usePagedQuery<UserListItem, { groupId?: string; isActive?: boolean; department?: string }>({
    queryKey: 'users',
    url: '/admin/users',
  });

  const groups = useQuery({
    queryKey: ['user-groups', 'picker'],
    queryFn: () => api.get<PagedResult<UserGroupListItem>>('/admin/user-groups', { params: { pageSize: 200 } }),
  });

  const departments = useQuery({
    queryKey: ['user-departments'],
    queryFn: () => api.get<string[]>('/admin/users/departments'),
  });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['users'] });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/admin/users/${id}`),
    onSuccess: async () => {
      message.success(messages.notify.deleteSuccess);
      await invalidate();
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const lockMutation = useMutation({
    mutationFn: ({ id, locked, reason }: { id: string; locked: boolean; reason?: string }) =>
      api.post(`/admin/users/${id}/lock`, { locked, reason }),
    onSuccess: async (_, variables) => {
      message.success(variables.locked ? 'Đã khóa tài khoản.' : 'Đã mở khóa tài khoản.');
      await invalidate();
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const resetMutation = useMutation({
    mutationFn: (id: string) => api.post<string>(`/admin/users/${id}/reset-password`, {}),
    onSuccess: (password) => {
      // Shown once and never again: the value is not retrievable after this dialog closes.
      modal.success({
        title: 'Đã đặt lại mật khẩu',
        width: 460,
        content: (
          <div>
            <Typography.Paragraph>
              Mật khẩu tạm thời của tài khoản là:
            </Typography.Paragraph>
            <Typography.Paragraph copyable strong className="lc-generated-password">
              {password}
            </Typography.Paragraph>
            <Typography.Text type="secondary">
              Hãy bàn giao cho người dùng. Hệ thống bắt buộc đổi mật khẩu ở lần đăng nhập kế tiếp và
              mật khẩu này sẽ không hiển thị lại.
            </Typography.Text>
          </div>
        ),
      });
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const isLocked = (user: UserListItem) =>
    user.lockedUntil !== undefined && new Date(user.lockedUntil) > new Date();

  const columns: ColumnsType<UserListItem> = [
    { title: 'Tên đăng nhập', dataIndex: 'username', width: 160, sorter: true },
    {
      title: 'Họ và tên',
      dataIndex: 'fullName',
      sorter: true,
      render: (fullName: string, record) => (
        <Space direction="vertical" size={0}>
          <Typography.Text strong>{fullName}</Typography.Text>
          {record.position && <Typography.Text type="secondary">{record.position}</Typography.Text>}
        </Space>
      ),
    },
    { title: 'Email', dataIndex: 'email', responsive: ['lg'], ellipsis: true },
    { title: 'Đơn vị', dataIndex: 'department', responsive: ['xl'], ellipsis: true },
    {
      title: 'Nhóm quyền',
      dataIndex: 'groupNames',
      render: (names: string[]) =>
        names.length === 0 ? (
          <Typography.Text type="secondary">Chưa gán nhóm</Typography.Text>
        ) : (
          <Space size={[0, 4]} wrap>
            {names.map((name) => (
              <Tag key={name} color="blue">
                {name}
              </Tag>
            ))}
          </Space>
        ),
    },
    {
      title: 'Trạng thái',
      key: 'status',
      width: 150,
      render: (_, record) => {
        if (isLocked(record)) {
          return <Tag color="red">Đang khóa</Tag>;
        }
        if (!record.isActive) {
          return <Tag>Ngưng hoạt động</Tag>;
        }
        return record.mustChangePassword ? (
          <Tag color="orange">Chờ đổi mật khẩu</Tag>
        ) : (
          <Tag color="green">Hoạt động</Tag>
        );
      },
    },
    {
      title: 'Đăng nhập gần nhất',
      dataIndex: 'lastLoginAt',
      width: 170,
      sorter: true,
      responsive: ['xl'],
      render: (value?: string) => formatDateTime(value) ?? <Typography.Text type="secondary">Chưa từng</Typography.Text>,
    },
    {
      title: 'Thao tác',
      key: 'actions',
      width: 200,
      fixed: 'right',
      render: (_, record) => (
        <Space size={2}>
          <Can permission={PERMISSIONS.system.userUpdate}>
            <Tooltip title={messages.actions.edit}>
              <Button type="link" size="small" icon={<EditOutlined />} onClick={() => setEditingId(record.id)} />
            </Tooltip>
          </Can>
          <Can permission={PERMISSIONS.system.userResetPassword}>
            <Tooltip title="Đặt lại mật khẩu">
              <Button
                type="link"
                size="small"
                icon={<KeyOutlined />}
                onClick={() =>
                  modal.confirm({
                    title: 'Đặt lại mật khẩu',
                    content: `Đặt lại mật khẩu cho "${record.fullName}"? Mọi phiên đăng nhập hiện tại của tài khoản sẽ bị thu hồi.`,
                    okText: messages.confirm.yes,
                    cancelText: messages.confirm.no,
                    onOk: () => resetMutation.mutateAsync(record.id),
                  })
                }
              />
            </Tooltip>
          </Can>
          <Can permission={PERMISSIONS.system.userLock}>
            <Tooltip title={isLocked(record) ? 'Mở khóa' : 'Khóa tài khoản'}>
              <Button
                type="link"
                size="small"
                icon={isLocked(record) ? <UnlockOutlined /> : <LockOutlined />}
                danger={!isLocked(record)}
                onClick={() =>
                  modal.confirm({
                    title: isLocked(record) ? 'Mở khóa tài khoản' : 'Khóa tài khoản',
                    content: isLocked(record)
                      ? `Mở khóa cho "${record.fullName}"?`
                      : `Khóa tài khoản "${record.fullName}"? Người dùng sẽ bị đăng xuất ngay lập tức.`,
                    okText: messages.confirm.yes,
                    cancelText: messages.confirm.no,
                    onOk: () => lockMutation.mutateAsync({ id: record.id, locked: !isLocked(record) }),
                  })
                }
              />
            </Tooltip>
          </Can>
          <Tooltip title="Lịch sử đăng nhập">
            <Button type="link" size="small" icon={<HistoryOutlined />} onClick={() => setHistoryUser(record)} />
          </Tooltip>
          <Can permission={PERMISSIONS.system.userDelete}>
            <Tooltip title={messages.actions.delete}>
              <Button
                type="link"
                size="small"
                danger
                icon={<DeleteOutlined />}
                onClick={() =>
                  modal.confirm({
                    title: messages.confirm.deleteTitle,
                    content: `Xóa tài khoản "${record.fullName}"? ${messages.confirm.deleteContent}`,
                    okText: messages.confirm.yes,
                    cancelText: messages.confirm.no,
                    okButtonProps: { danger: true },
                    onOk: () => deleteMutation.mutateAsync(record.id),
                  })
                }
              />
            </Tooltip>
          </Can>
        </Space>
      ),
    },
  ];

  return (
    <div className="lc-page">
      <PageHeader
        title={messages.menu.users}
        description="Tài khoản của cán bộ thư viện. Quyền sử dụng chức năng được cấp thông qua nhóm quyền."
        actions={
          <Space>
            <Can permission={PERMISSIONS.system.userImport}>
              <Button icon={<ImportOutlined />} onClick={() => setImportOpen(true)}>
                Nhập từ Excel
              </Button>
            </Can>
            <Can permission={PERMISSIONS.system.userCreate}>
              <Button type="primary" icon={<PlusOutlined />} onClick={() => setCreating(true)}>
                {messages.actions.create}
              </Button>
            </Can>
          </Space>
        }
      />

      <FilterBar
        loading={list.isFetching}
        onSearch={() => list.applyFilter({ keyword, groupId, isActive, department })}
        onReset={() => {
          setKeyword('');
          setGroupId(undefined);
          setIsActive(undefined);
          setDepartment(undefined);
          list.resetFilter();
        }}
      >
        <Input
          allowClear
          placeholder="Tìm theo tên đăng nhập, họ tên, email, điện thoại"
          value={keyword}
          onChange={(event) => setKeyword(event.target.value)}
          style={{ width: 320 }}
        />
        <Select
          allowClear
          placeholder="Nhóm quyền"
          value={groupId}
          onChange={setGroupId}
          style={{ width: 200 }}
          options={(groups.data?.items ?? []).map((group) => ({ value: group.id, label: group.name }))}
        />
        <Select
          allowClear
          showSearch
          placeholder="Đơn vị"
          value={department}
          onChange={setDepartment}
          style={{ width: 200 }}
          options={(departments.data ?? []).map((name) => ({ value: name, label: name }))}
        />
        <Select
          allowClear
          placeholder="Trạng thái"
          value={isActive}
          onChange={setIsActive}
          style={{ width: 170 }}
          options={[
            { value: true, label: 'Hoạt động' },
            { value: false, label: 'Ngưng hoạt động' },
          ]}
        />
      </FilterBar>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<UserListItem>
          rowKey="id"
          columns={columns}
          dataSource={list.items}
          loading={list.isLoading}
          pagination={list.pagination}
          onChange={list.handleTableChange}
          scroll={{ x: 1300 }}
          size="middle"
          locale={{ emptyText: messages.table.empty }}
        />
      </Card>

      <UserFormDrawer
        open={creating || editingId !== null}
        userId={editingId}
        groups={groups.data?.items ?? []}
        onClose={() => {
          setCreating(false);
          setEditingId(null);
        }}
        onSaved={async () => {
          setCreating(false);
          setEditingId(null);
          await invalidate();
        }}
      />

      {historyUser && <LoginHistoryDrawer user={historyUser} onClose={() => setHistoryUser(null)} />}

      {importOpen && (
        <ImportUsersModal
          onClose={() => setImportOpen(false)}
          onImported={async () => {
            await invalidate();
            await departments.refetch();
          }}
        />
      )}
    </div>
  );
}

// ---------------------------------------------------------------------------

function UserFormDrawer({
  open,
  userId,
  groups,
  onClose,
  onSaved,
}: {
  open: boolean;
  userId: string | null;
  groups: UserGroupListItem[];
  onClose: () => void;
  onSaved: () => void | Promise<void>;
}) {
  const [form] = Form.useForm<UserFormValues>();
  const { message, modal } = App.useApp();
  const isEdit = userId !== null;

  const detail = useQuery({
    queryKey: ['user', userId],
    queryFn: () => api.get<UserDetail>(`/admin/users/${userId}`),
    enabled: isEdit && open,
  });

  // The two scope lists are short and change rarely; they are only fetched while the drawer is open.
  const libraries = useQuery({
    queryKey: ['scope-libraries'],
    queryFn: () => api.get<Array<{ id: string; name: string }>>('/locations/libraries'),
    enabled: open,
    staleTime: 5 * 60 * 1000,
  });

  const warehouses = useQuery({
    queryKey: ['scope-warehouses'],
    queryFn: () => api.get<Array<{ id: string; name: string }>>('/locations/warehouses'),
    enabled: open,
    staleTime: 5 * 60 * 1000,
  });

  const mutation = useMutation({
    mutationFn: (values: UserFormValues) => {
      const profile = {
        fullName: values.fullName,
        email: values.email,
        phone: values.phone,
        position: values.position,
        department: values.department,
        isActive: values.isActive,
        groupIds: values.groupIds ?? [],
        // An empty list means unrestricted; anything else narrows the user to those libraries and
        // warehouses. Document-type scopes are not assigned from this screen.
        dataScopes: [
          ...buildDataScopes(values),
          ...(detail.data?.dataScopes ?? []).filter((scope) => scope.scopeType === 'DocumentType'),
        ],
      };

      return isEdit
        ? api.put(`/admin/users/${userId}`, profile)
        : api.post<{ id: string; temporaryPassword: string }>('/admin/users', {
            username: values.username,
            profile,
          });
    },
    onSuccess: async (result) => {
      if (!isEdit && result && typeof result === 'object' && 'temporaryPassword' in result) {
        modal.success({
          title: 'Đã tạo tài khoản',
          width: 460,
          content: (
            <div>
              <Typography.Paragraph>Mật khẩu tạm thời:</Typography.Paragraph>
              <Typography.Paragraph copyable strong className="lc-generated-password">
                {(result as { temporaryPassword: string }).temporaryPassword}
              </Typography.Paragraph>
              <Typography.Text type="secondary">
                Mật khẩu chỉ hiển thị một lần. Người dùng phải đổi mật khẩu ở lần đăng nhập đầu tiên.
              </Typography.Text>
            </div>
          ),
        });
      } else {
        message.success(messages.notify.updateSuccess);
      }

      await onSaved();
      form.resetFields();
    },
    onError: (error: unknown) => message.error(applyApiError(form, error)),
  });

  return (
    <Drawer
      title={isEdit ? `Sửa người dùng: ${detail.data?.fullName ?? ''}` : 'Thêm người dùng'}
      open={open}
      width={560}
      onClose={onClose}
      destroyOnHidden
      afterOpenChange={(visible) => {
        if (!visible) {
          form.resetFields();
        } else if (!isEdit) {
          form.setFieldsValue({ isActive: true, groupIds: [] });
        }
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
      <Form<UserFormValues>
        form={form}
        layout="vertical"
        onFinish={(values) => mutation.mutate(values)}
        initialValues={{ isActive: true, groupIds: [] }}
        key={detail.data?.id ?? 'new'}
        // Re-mounting on the loaded record is what makes the drawer show the right values without a
        // separate effect syncing the form to the query result.
      >
        {isEdit && detail.data && (
          <Form.Item label="Tên đăng nhập">
            <Input value={detail.data.username} disabled />
          </Form.Item>
        )}

        {!isEdit && (
          <Form.Item
            name="username"
            label="Tên đăng nhập"
            rules={[
              { required: true, message: 'Vui lòng nhập tên đăng nhập.' },
              { min: 3, message: 'Tên đăng nhập tối thiểu 3 ký tự.' },
              { pattern: /^[a-zA-Z0-9._-]+$/, message: 'Chỉ gồm chữ cái, chữ số và các ký tự . _ -' },
            ]}
            extra="Hệ thống sẽ sinh mật khẩu tạm và bắt buộc người dùng đổi ở lần đăng nhập đầu tiên."
          >
            <Input placeholder="VD: nguyenvana" autoComplete="off" />
          </Form.Item>
        )}

        <Form.Item
          name="fullName"
          label="Họ và tên"
          rules={[{ required: true, message: 'Vui lòng nhập họ tên.' }]}
          initialValue={detail.data?.fullName}
        >
          <Input placeholder="VD: Nguyễn Văn A" />
        </Form.Item>

        <Form.Item
          name="email"
          label="Email"
          rules={[{ type: 'email', message: 'Địa chỉ email không hợp lệ.' }]}
          initialValue={detail.data?.email}
        >
          <Input placeholder="VD: nguyenvana@thuvien.edu.vn" />
        </Form.Item>

        <Form.Item name="phone" label="Điện thoại" initialValue={detail.data?.phone}>
          <Input placeholder="VD: 0912345678" />
        </Form.Item>

        <Form.Item name="position" label="Chức vụ" initialValue={detail.data?.position}>
          <Input placeholder="VD: Cán bộ biên mục" />
        </Form.Item>

        <Form.Item name="department" label="Đơn vị" initialValue={detail.data?.department}>
          <Input placeholder="VD: Phòng Nghiệp vụ" />
        </Form.Item>

        <Form.Item
          name="groupIds"
          label="Nhóm quyền"
          extra="Người dùng nhận hợp của tất cả quyền trong các nhóm được gán."
          initialValue={detail.data?.groupIds ?? []}
        >
          <Select
            mode="multiple"
            allowClear
            placeholder="Chọn một hoặc nhiều nhóm"
            options={groups.map((group) => ({ value: group.id, label: group.name }))}
          />
        </Form.Item>

        <Form.Item
          name="libraryIds"
          label="Phạm vi dữ liệu — thư viện"
          extra="Bỏ trống cả hai ô phạm vi là người dùng thao tác được trên mọi thư viện và kho."
          initialValue={splitDataScopes(detail.data?.dataScopes).libraryIds}
        >
          <Select
            mode="multiple"
            allowClear
            placeholder="Mọi thư viện"
            loading={libraries.isFetching}
            options={(libraries.data ?? []).map((library) => ({ value: library.id, label: library.name }))}
            optionFilterProp="label"
          />
        </Form.Item>

        <Form.Item
          name="warehouseIds"
          label="Phạm vi dữ liệu — kho"
          initialValue={splitDataScopes(detail.data?.dataScopes).warehouseIds}
        >
          <Select
            mode="multiple"
            allowClear
            placeholder="Mọi kho"
            loading={warehouses.isFetching}
            options={(warehouses.data ?? []).map((warehouse) => ({ value: warehouse.id, label: warehouse.name }))}
            optionFilterProp="label"
          />
        </Form.Item>

        <Form.Item
          name="isActive"
          label="Trạng thái"
          valuePropName="checked"
          initialValue={detail.data?.isActive ?? true}
        >
          <Switch checkedChildren="Hoạt động" unCheckedChildren="Ngưng" />
        </Form.Item>
      </Form>
    </Drawer>
  );
}

// ---------------------------------------------------------------------------

function LoginHistoryDrawer({ user, onClose }: { user: UserListItem; onClose: () => void }) {
  const history = usePagedQuery<LoginHistoryItem>({
    queryKey: `login-history-${user.id}`,
    url: `/admin/users/${user.id}/login-history`,
  });

  const columns: ColumnsType<LoginHistoryItem> = [
    {
      title: 'Thời điểm',
      dataIndex: 'occurredAt',
      width: 170,
      render: (value: string) => formatDateTime(value),
    },
    {
      title: 'Kết quả',
      dataIndex: 'success',
      width: 130,
      render: (success: boolean, record) =>
        success ? (
          <Tag color="green">Thành công</Tag>
        ) : (
          <Tooltip title={record.failureReason}>
            <Tag color="red">Thất bại</Tag>
          </Tooltip>
        ),
    },
    { title: 'Địa chỉ IP', dataIndex: 'ip', width: 140 },
    { title: 'Trình duyệt / thiết bị', dataIndex: 'userAgent', ellipsis: true },
  ];

  return (
    <Drawer title={`Lịch sử đăng nhập: ${user.fullName}`} open width={780} onClose={onClose}>
      <Table<LoginHistoryItem>
        rowKey="id"
        size="small"
        columns={columns}
        dataSource={history.items}
        loading={history.isLoading}
        pagination={history.pagination}
        onChange={history.handleTableChange}
        locale={{ emptyText: 'Chưa có lần đăng nhập nào được ghi nhận.' }}
      />
    </Drawer>
  );
}

// ---------------------------------------------------------------------------

function ImportUsersModal({
  onClose,
  onImported,
}: {
  onClose: () => void;
  onImported: () => void | Promise<void>;
}) {
  const { message } = App.useApp();
  const [file, setFile] = useState<UploadFile | null>(null);
  const [checkResult, setCheckResult] = useState<UserImportResult | null>(null);
  const [imported, setImported] = useState<UserImportResult | null>(null);

  const upload = useMutation({
    mutationFn: async ({ dryRun }: { dryRun: boolean }) => {
      const form = new FormData();
      form.append('file', file!.originFileObj as Blob);

      const response = await http.post<{ data: UserImportResult }>(
        `/admin/users/import?dryRun=${dryRun}`,
        form,
        { headers: { 'Content-Type': 'multipart/form-data' } },
      );

      return response.data.data;
    },
    onSuccess: async (result, variables) => {
      if (variables.dryRun) {
        setCheckResult(result);
        message.info(`Kiểm tra xong: ${result.successRows}/${result.totalRows} dòng hợp lệ.`);
      } else {
        setImported(result);
        message.success(`Đã nhập ${result.successRows}/${result.totalRows} tài khoản.`);
        await onImported();
      }
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const downloadTemplate = async () => {
    try {
      const { blob, fileName } = await api.download('/admin/users/import-template');
      downloadFile(blob, fileName);
    } catch (error) {
      message.error(errorMessage(error));
    }
  };

  const result = imported ?? checkResult;

  return (
    <Modal
      open
      width={820}
      title="Nhập người dùng từ Excel"
      onCancel={onClose}
      footer={
        <Space>
          <Button onClick={onClose}>{messages.actions.close}</Button>
          <Button
            disabled={!file || imported !== null}
            loading={upload.isPending && upload.variables?.dryRun === true}
            onClick={() => upload.mutate({ dryRun: true })}
          >
            Kiểm tra tệp
          </Button>
          <Button
            type="primary"
            // Importing is only offered once the check has found at least one usable row.
            disabled={!file || imported !== null || (checkResult?.successRows ?? 0) === 0}
            loading={upload.isPending && upload.variables?.dryRun === false}
            onClick={() => upload.mutate({ dryRun: false })}
          >
            Nhập dữ liệu
          </Button>
        </Space>
      }
    >
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <Alert
          type="info"
          showIcon
          message="Trình tự nhập"
          description="Tải tệp mẫu → điền dữ liệu → chọn tệp → Kiểm tra tệp để xem lỗi → Nhập dữ liệu. Bước kiểm tra không ghi bất kỳ bản ghi nào."
        />

        <Space>
          <Button icon={<DownloadOutlined />} onClick={downloadTemplate}>
            Tải tệp mẫu
          </Button>

          <Upload
            accept=".xlsx,.xls"
            maxCount={1}
            beforeUpload={() => false}
            fileList={file ? [file] : []}
            onChange={({ fileList }) => {
              setFile(fileList[0] ?? null);
              setCheckResult(null);
              setImported(null);
            }}
          >
            <Button icon={<UploadOutlined />}>Chọn tệp Excel</Button>
          </Upload>
        </Space>

        {result && (
          <Alert
            type={result.errorRows > 0 ? 'warning' : 'success'}
            showIcon
            message={`Tổng ${result.totalRows} dòng · hợp lệ ${result.successRows} · lỗi ${result.errorRows}`}
          />
        )}

        {result && result.errors.length > 0 && (
          <Table
            size="small"
            rowKey={(row) => `${row.row}-${row.column}-${row.message}`}
            dataSource={result.errors}
            pagination={{ pageSize: 8 }}
            columns={[
              { title: 'Dòng', dataIndex: 'row', width: 70 },
              { title: 'Cột', dataIndex: 'column', width: 150 },
              { title: 'Giá trị', dataIndex: 'value', width: 160, ellipsis: true },
              { title: 'Lỗi', dataIndex: 'message' },
            ]}
          />
        )}

        {imported && Object.keys(imported.generatedPasswords).length > 0 && (
          <>
            <Typography.Text strong>
              Mật khẩu tạm của các tài khoản vừa tạo (chỉ hiển thị một lần):
            </Typography.Text>
            <Table
              size="small"
              rowKey={(row) => row.username}
              dataSource={Object.entries(imported.generatedPasswords).map(([username, password]) => ({
                username,
                password,
              }))}
              pagination={{ pageSize: 8 }}
              columns={[
                { title: 'Tên đăng nhập', dataIndex: 'username', width: 220 },
                {
                  title: 'Mật khẩu tạm',
                  dataIndex: 'password',
                  render: (password: string) => (
                    <Typography.Text copyable code>
                      {password}
                    </Typography.Text>
                  ),
                },
              ]}
            />
          </>
        )}
      </Space>
    </Modal>
  );
}
