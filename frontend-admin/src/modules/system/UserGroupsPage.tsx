import { useMemo, useState } from 'react';
import {
  App,
  Button,
  Card,
  Drawer,
  Form,
  Input,
  Modal,
  Select,
  Space,
  Spin,
  Switch,
  Table,
  Tag,
  Tree,
  Typography,
} from 'antd';
import type { ColumnsType } from 'antd/es/table';
import type { DataNode } from 'antd/es/tree';
import {
  CopyOutlined,
  DeleteOutlined,
  EditOutlined,
  PlusOutlined,
  SafetyCertificateOutlined,
  TeamOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import { applyApiError, errorMessage } from '@/api/formErrors';
import { PERMISSIONS } from '@/api/permissions';
import { FilterBar } from '@/components/FilterBar';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { usePermission } from '@/hooks/usePermission';
import { usePagedQuery } from '@/hooks/usePagedQuery';
import { messages } from '@/i18n/messages';
import type {
  GroupMember,
  GroupPermissions,
  PermissionTreeNode,
  UserGroupListItem,
  UserListItem,
} from './types';
import type { PagedResult } from '@/types/api';

interface GroupFormValues {
  code: string;
  name: string;
  description?: string;
  isActive: boolean;
}

/** Phân hệ I.1 — danh sách nhóm người dùng, phân quyền và thành viên. */
export function UserGroupsPage() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();
  const { can } = usePermission();

  const [keyword, setKeyword] = useState('');
  const [activeFilter, setActiveFilter] = useState<boolean | undefined>(undefined);

  const [editing, setEditing] = useState<UserGroupListItem | null>(null);
  const [creating, setCreating] = useState(false);
  const [permissionTarget, setPermissionTarget] = useState<UserGroupListItem | null>(null);
  const [memberTarget, setMemberTarget] = useState<UserGroupListItem | null>(null);

  const list = usePagedQuery<UserGroupListItem, { isActive?: boolean }>({
    queryKey: 'user-groups',
    url: '/admin/user-groups',
  });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['user-groups'] });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/admin/user-groups/${id}`),
    onSuccess: async () => {
      message.success(messages.notify.deleteSuccess);
      await invalidate();
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const columns: ColumnsType<UserGroupListItem> = [
    {
      title: 'Mã nhóm',
      dataIndex: 'code',
      // Wide enough for the longest seeded code plus the "Hệ thống" tag on one line.
      width: 210,
      sorter: true,
      render: (code: string, record) => (
        <Space>
          <Typography.Text strong>{code}</Typography.Text>
          {record.isSystem && <Tag color="gold">Hệ thống</Tag>}
        </Space>
      ),
    },
    { title: 'Tên nhóm', dataIndex: 'name', sorter: true },
    { title: 'Mô tả', dataIndex: 'description', ellipsis: true, responsive: ['lg'] },
    {
      title: 'Số quyền',
      dataIndex: 'permissionCount',
      width: 110,
      align: 'right',
      render: (count: number) => count.toLocaleString('vi-VN'),
    },
    {
      title: 'Thành viên',
      dataIndex: 'memberCount',
      width: 110,
      align: 'right',
      render: (count: number) => count.toLocaleString('vi-VN'),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'isActive',
      width: 120,
      render: (isActive: boolean) =>
        isActive ? <Tag color="green">Đang dùng</Tag> : <Tag>Ngưng dùng</Tag>,
    },
    {
      title: 'Thao tác',
      key: 'actions',
      width: 230,
      fixed: 'right',
      render: (_, record) => (
        <Space size={4}>
          <Can permission={PERMISSIONS.system.groupView}>
            <Button
              type="link"
              size="small"
              icon={<SafetyCertificateOutlined />}
              onClick={() => setPermissionTarget(record)}
            >
              Phân quyền
            </Button>
          </Can>
          <Can permission={PERMISSIONS.system.groupView}>
            <Button type="link" size="small" icon={<TeamOutlined />} onClick={() => setMemberTarget(record)}>
              Thành viên
            </Button>
          </Can>
          <Can permission={PERMISSIONS.system.groupUpdate}>
            <Button type="link" size="small" icon={<EditOutlined />} onClick={() => setEditing(record)} />
          </Can>
          <Can permission={PERMISSIONS.system.groupDelete}>
            <Button
              type="link"
              size="small"
              danger
              icon={<DeleteOutlined />}
              // A system group is seeded and referenced from code, so it can never be removed.
              disabled={record.isSystem}
              onClick={() =>
                modal.confirm({
                  title: messages.confirm.deleteTitle,
                  content: `Xóa nhóm "${record.name}"? ${messages.confirm.deleteContent}`,
                  okText: messages.confirm.yes,
                  cancelText: messages.confirm.no,
                  okButtonProps: { danger: true },
                  onOk: () => deleteMutation.mutateAsync(record.id),
                })
              }
            />
          </Can>
        </Space>
      ),
    },
  ];

  return (
    <div className="lc-page">
      <PageHeader
        title={messages.menu.userGroups}
        description="Nhóm quyền quyết định cán bộ được sử dụng những chức năng nào của hệ thống."
        actions={
          <Can permission={PERMISSIONS.system.groupCreate}>
            <Button type="primary" icon={<PlusOutlined />} onClick={() => setCreating(true)}>
              {messages.actions.create}
            </Button>
          </Can>
        }
      />

      <FilterBar
        loading={list.isFetching}
        onSearch={() => list.applyFilter({ keyword, isActive: activeFilter })}
        onReset={() => {
          setKeyword('');
          setActiveFilter(undefined);
          list.resetFilter();
        }}
      >
        <Input
          allowClear
          placeholder="Tìm theo mã, tên hoặc mô tả nhóm"
          value={keyword}
          onChange={(event) => setKeyword(event.target.value)}
          style={{ width: 320 }}
        />
        <Select
          allowClear
          placeholder="Trạng thái"
          value={activeFilter}
          onChange={setActiveFilter}
          style={{ width: 180 }}
          options={[
            { value: true, label: 'Đang dùng' },
            { value: false, label: 'Ngưng dùng' },
          ]}
        />
      </FilterBar>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<UserGroupListItem>
          rowKey="id"
          columns={columns}
          dataSource={list.items}
          loading={list.isLoading}
          pagination={list.pagination}
          onChange={list.handleTableChange}
          scroll={{ x: 1000 }}
          size="middle"
          locale={{ emptyText: messages.table.empty }}
        />
      </Card>

      <GroupFormDrawer
        open={creating || editing !== null}
        group={editing}
        onClose={() => {
          setCreating(false);
          setEditing(null);
        }}
        onSaved={async () => {
          setCreating(false);
          setEditing(null);
          await invalidate();
        }}
      />

      {permissionTarget && can(PERMISSIONS.system.groupView) && (
        <PermissionDrawer
          group={permissionTarget}
          onClose={() => setPermissionTarget(null)}
          onSaved={invalidate}
        />
      )}

      {memberTarget && (
        <MemberDrawer group={memberTarget} onClose={() => setMemberTarget(null)} onSaved={invalidate} />
      )}
    </div>
  );
}

// ---------------------------------------------------------------------------

function GroupFormDrawer({
  open,
  group,
  onClose,
  onSaved,
}: {
  open: boolean;
  group: UserGroupListItem | null;
  onClose: () => void;
  onSaved: () => void | Promise<void>;
}) {
  const [form] = Form.useForm<GroupFormValues>();
  const { message } = App.useApp();
  const isEdit = group !== null;

  const mutation = useMutation({
    mutationFn: (values: GroupFormValues) =>
      isEdit
        ? api.put(`/admin/user-groups/${group.id}`, {
            name: values.name,
            description: values.description,
            isActive: values.isActive,
          })
        : api.post('/admin/user-groups', values),
    onSuccess: async () => {
      message.success(isEdit ? messages.notify.updateSuccess : messages.notify.createSuccess);
      await onSaved();
      form.resetFields();
    },
    onError: (error: unknown) => message.error(applyApiError(form, error)),
  });

  return (
    <Drawer
      title={isEdit ? `Sửa nhóm: ${group.name}` : 'Thêm nhóm người dùng'}
      open={open}
      width={480}
      onClose={onClose}
      destroyOnHidden
      afterOpenChange={(visible) => {
        if (visible) {
          form.setFieldsValue(
            group
              ? { code: group.code, name: group.name, description: group.description, isActive: group.isActive }
              : { isActive: true },
          );
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
      <Form<GroupFormValues> form={form} layout="vertical" onFinish={(values) => mutation.mutate(values)}>
        <Form.Item
          name="code"
          label="Mã nhóm"
          rules={[
            { required: true, message: 'Vui lòng nhập mã nhóm.' },
            { pattern: /^[A-Za-z0-9_]+$/, message: 'Mã nhóm chỉ gồm chữ cái, chữ số và dấu gạch dưới.' },
          ]}
          // The code identifies the group in seeded data and in code, so it is fixed after creation.
          extra={isEdit ? 'Mã nhóm không thay đổi được sau khi tạo.' : 'Ví dụ: CATALOGER, CIRCULATION'}
        >
          <Input disabled={isEdit} placeholder="VD: CATALOGER" />
        </Form.Item>

        <Form.Item name="name" label="Tên nhóm" rules={[{ required: true, message: 'Vui lòng nhập tên nhóm.' }]}>
          <Input placeholder="VD: Cán bộ biên mục" />
        </Form.Item>

        <Form.Item name="description" label="Mô tả">
          <Input.TextArea rows={3} placeholder="Nhóm này phụ trách những công việc gì?" />
        </Form.Item>

        <Form.Item name="isActive" label="Trạng thái" valuePropName="checked">
          <Switch checkedChildren="Đang dùng" unCheckedChildren="Ngưng dùng" />
        </Form.Item>
      </Form>
    </Drawer>
  );
}

// ---------------------------------------------------------------------------

function PermissionDrawer({
  group,
  onClose,
  onSaved,
}: {
  group: UserGroupListItem;
  onClose: () => void;
  onSaved: () => void | Promise<void>;
}) {
  const { message } = App.useApp();
  const { can } = usePermission();
  const [checkedKeys, setCheckedKeys] = useState<string[] | null>(null);
  const [cloneOpen, setCloneOpen] = useState(false);

  const query = useQuery({
    queryKey: ['group-permissions', group.id],
    queryFn: () => api.get<GroupPermissions>(`/admin/user-groups/${group.id}/permissions`),
  });

  // Null means "not touched yet", so the server's state shows until the user changes something.
  const effectiveChecked = checkedKeys ?? query.data?.grantedCodes ?? [];

  const treeData = useMemo<DataNode[]>(() => toTreeData(query.data?.tree ?? []), [query.data]);

  const mutation = useMutation({
    mutationFn: (codes: string[]) => api.put(`/admin/user-groups/${group.id}/permissions`, { permissionCodes: codes }),
    onSuccess: async () => {
      message.success('Cập nhật quyền cho nhóm thành công.');
      await onSaved();
      onClose();
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const leafCount = effectiveChecked.filter((key) => !key.includes(':')).length;

  return (
    <Drawer
      title={`Phân quyền: ${group.name}`}
      open
      width={640}
      onClose={onClose}
      extra={
        <Space>
          <Can permission={PERMISSIONS.system.groupPermission}>
            <Button icon={<CopyOutlined />} onClick={() => setCloneOpen(true)}>
              Sao chép từ nhóm khác
            </Button>
          </Can>
          <Button onClick={onClose}>{messages.actions.cancel}</Button>
          <Can permission={PERMISSIONS.system.groupPermission}>
            <Button
              type="primary"
              loading={mutation.isPending}
              disabled={!can(PERMISSIONS.system.groupPermission)}
              onClick={() => mutation.mutate(effectiveChecked.filter((key) => !key.includes(':')))}
            >
              {messages.actions.save}
            </Button>
          </Can>
        </Space>
      }
    >
      <Typography.Paragraph type="secondary">
        Đang cấp <Typography.Text strong>{leafCount}</Typography.Text> quyền. Chọn ở cấp module hoặc
        cấp chức năng sẽ tự động chọn toàn bộ hành động bên dưới.
      </Typography.Paragraph>

      <Spin spinning={query.isLoading}>
        <Tree
          checkable
          treeData={treeData}
          checkedKeys={effectiveChecked}
          onCheck={(checked) => {
            const keys = Array.isArray(checked) ? checked : checked.checked;
            setCheckedKeys(keys.map(String));
          }}
          height={520}
          selectable={false}
        />
      </Spin>

      {cloneOpen && (
        <CloneModal
          targetGroup={group}
          onClose={() => setCloneOpen(false)}
          onCloned={async () => {
            setCloneOpen(false);
            setCheckedKeys(null);
            await query.refetch();
            await onSaved();
          }}
        />
      )}
    </Drawer>
  );
}

/** Turns the API's permission tree into the node shape Ant Design's Tree expects. */
function toTreeData(nodes: PermissionTreeNode[]): DataNode[] {
  return nodes.map((node) => ({
    key: node.key,
    title: node.title,
    children: node.children.length > 0 ? toTreeData(node.children) : undefined,
  }));
}

function CloneModal({
  targetGroup,
  onClose,
  onCloned,
}: {
  targetGroup: UserGroupListItem;
  onClose: () => void;
  onCloned: () => void | Promise<void>;
}) {
  const { message } = App.useApp();
  const [sourceId, setSourceId] = useState<string | undefined>();
  const [replace, setReplace] = useState(true);

  const groups = useQuery({
    queryKey: ['user-groups', 'all'],
    queryFn: () => api.get<PagedResult<UserGroupListItem>>('/admin/user-groups', { params: { pageSize: 200 } }),
  });

  const mutation = useMutation({
    mutationFn: () => api.post<number>(`/admin/user-groups/${targetGroup.id}/clone`, { sourceGroupId: sourceId, replace }),
    onSuccess: async (count) => {
      message.success(`Đã sao chép ${count} quyền sang nhóm "${targetGroup.name}".`);
      await onCloned();
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  return (
    <Modal
      open
      title="Sao chép quyền từ nhóm khác"
      onCancel={onClose}
      onOk={() => mutation.mutate()}
      okText="Sao chép"
      cancelText={messages.actions.cancel}
      okButtonProps={{ disabled: !sourceId, loading: mutation.isPending }}
    >
      <Form layout="vertical">
        <Form.Item label="Nhóm nguồn">
          <Select
            showSearch
            optionFilterProp="label"
            placeholder="Chọn nhóm để lấy bộ quyền"
            value={sourceId}
            onChange={setSourceId}
            options={(groups.data?.items ?? [])
              .filter((item) => item.id !== targetGroup.id)
              .map((item) => ({ value: item.id, label: `${item.name} (${item.permissionCount} quyền)` }))}
          />
        </Form.Item>

        <Form.Item label="Cách áp dụng">
          <Select
            value={replace}
            onChange={setReplace}
            options={[
              { value: true, label: 'Thay thế toàn bộ quyền hiện có' },
              { value: false, label: 'Gộp thêm vào quyền đang có' },
            ]}
          />
        </Form.Item>
      </Form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------

function MemberDrawer({
  group,
  onClose,
  onSaved,
}: {
  group: UserGroupListItem;
  onClose: () => void;
  onSaved: () => void | Promise<void>;
}) {
  const { message } = App.useApp();
  const [selected, setSelected] = useState<string[]>([]);
  const [addOpen, setAddOpen] = useState(false);

  const members = usePagedQuery<GroupMember>({
    queryKey: `group-members-${group.id}`,
    url: `/admin/user-groups/${group.id}/members`,
  });

  const mutation = useMutation({
    mutationFn: (payload: { addUserIds: string[]; removeUserIds: string[] }) =>
      api.put(`/admin/user-groups/${group.id}/members`, payload),
    onSuccess: async () => {
      message.success('Cập nhật thành viên nhóm thành công.');
      setSelected([]);
      await members.refetch();
      await onSaved();
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const columns: ColumnsType<GroupMember> = [
    { title: 'Tên đăng nhập', dataIndex: 'username', width: 160 },
    { title: 'Họ và tên', dataIndex: 'fullName' },
    { title: 'Đơn vị', dataIndex: 'department', responsive: ['lg'] },
    {
      title: 'Trạng thái',
      dataIndex: 'isActive',
      width: 120,
      render: (isActive: boolean) => (isActive ? <Tag color="green">Hoạt động</Tag> : <Tag>Ngưng</Tag>),
    },
  ];

  return (
    <Drawer
      title={`Thành viên nhóm: ${group.name}`}
      open
      width={720}
      onClose={onClose}
      extra={
        <Can permission={PERMISSIONS.system.groupUpdate}>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setAddOpen(true)}>
            Thêm thành viên
          </Button>
        </Can>
      }
    >
      {selected.length > 0 && (
        <Space className="lc-bulk-bar">
          <Typography.Text>{messages.table.selected(selected.length)}</Typography.Text>
          <Can permission={PERMISSIONS.system.groupUpdate}>
            <Button
              danger
              size="small"
              loading={mutation.isPending}
              onClick={() => mutation.mutate({ addUserIds: [], removeUserIds: selected })}
            >
              Bỏ khỏi nhóm
            </Button>
          </Can>
        </Space>
      )}

      <Table<GroupMember>
        rowKey="userId"
        size="small"
        columns={columns}
        dataSource={members.items}
        loading={members.isLoading}
        pagination={members.pagination}
        onChange={members.handleTableChange}
        rowSelection={{ selectedRowKeys: selected, onChange: (keys) => setSelected(keys.map(String)) }}
        locale={{ emptyText: 'Nhóm chưa có thành viên nào.' }}
      />

      {addOpen && (
        <AddMemberModal
          onClose={() => setAddOpen(false)}
          onAdd={async (userIds) => {
            await mutation.mutateAsync({ addUserIds: userIds, removeUserIds: [] });
            setAddOpen(false);
          }}
        />
      )}
    </Drawer>
  );
}

function AddMemberModal({
  onClose,
  onAdd,
}: {
  onClose: () => void;
  onAdd: (userIds: string[]) => Promise<void>;
}) {
  const [keyword, setKeyword] = useState('');
  const [selected, setSelected] = useState<string[]>([]);

  const users = useQuery({
    queryKey: ['users-picker', keyword],
    queryFn: () =>
      api.get<PagedResult<UserListItem>>('/admin/users', { params: { keyword, pageSize: 50, isActive: true } }),
  });

  return (
    <Modal
      open
      title="Thêm thành viên vào nhóm"
      width={640}
      onCancel={onClose}
      onOk={() => onAdd(selected)}
      okText={`Thêm ${selected.length > 0 ? `(${selected.length})` : ''}`}
      cancelText={messages.actions.cancel}
      okButtonProps={{ disabled: selected.length === 0 }}
    >
      <Input.Search
        allowClear
        placeholder="Tìm theo tên đăng nhập, họ tên hoặc email"
        onSearch={setKeyword}
        style={{ marginBottom: 12 }}
      />

      <Table<UserListItem>
        rowKey="id"
        size="small"
        loading={users.isFetching}
        dataSource={users.data?.items ?? []}
        pagination={false}
        scroll={{ y: 320 }}
        rowSelection={{ selectedRowKeys: selected, onChange: (keys) => setSelected(keys.map(String)) }}
        columns={[
          { title: 'Tên đăng nhập', dataIndex: 'username', width: 160 },
          { title: 'Họ và tên', dataIndex: 'fullName' },
          { title: 'Đơn vị', dataIndex: 'department' },
        ]}
        locale={{ emptyText: messages.table.empty }}
      />
    </Modal>
  );
}
