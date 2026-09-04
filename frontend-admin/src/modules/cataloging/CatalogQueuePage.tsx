import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  App,
  Badge,
  Button,
  Card,
  Col,
  DatePicker,
  Empty,
  Input,
  Modal,
  Row,
  Select,
  Space,
  Table,
  Tabs,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import {
  CheckOutlined,
  DeleteOutlined,
  EditOutlined,
  PlayCircleOutlined,
  RollbackOutlined,
  SendOutlined,
  TeamOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { errorMessage } from '@/api/formErrors';
import { api } from '@/api/client';
import type { PagedResult } from '@/types/api';
import { queueApi } from './api';
import {
  QUEUE_STATUS_LABELS,
  PRIORITY_LABELS,
  type CatalogQueueItem,
  type CatalogQueueStatus,
} from './queueTypes';

const MONOSPACE = { fontFamily: 'ui-monospace, Consolas, monospace' } as const;

/** Bốn cột của bảng công việc, theo đúng thứ tự một việc đi qua. */
const COLUMNS: Array<{ key: CatalogQueueStatus; label: string; hint: string }> = [
  { key: 'Pending', label: 'Chờ xử lý', hint: 'Biểu ghi đã vào hàng đợi nhưng chưa ai nhận.' },
  { key: 'InProgress', label: 'Đang biên mục', hint: 'Cán bộ đã nhận và đang làm.' },
  { key: 'WaitingApproval', label: 'Chờ duyệt', hint: 'Đã biên mục xong, chờ người duyệt kiểm tra.' },
  { key: 'Completed', label: 'Đã hoàn thành', hint: 'Đã duyệt; biểu ghi sẵn sàng phục vụ.' },
];

/**
 * Hàng đợi biên mục chi tiết (II.4).
 *
 * The four columns are the four states a job passes through, and the counts on the tabs are what a
 * head of cataloguing looks at first. Work arrives here from the quick-cataloguing form in
 * acquisitions and from file imports, which is why the source of each record is on the row: a record
 * captured from a supplier list needs different attention from one harvested off a partner catalogue.
 */
export function CatalogQueuePage() {
  const { message, modal } = App.useApp();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [status, setStatus] = useState<CatalogQueueStatus>('Pending');
  const [keyword, setKeyword] = useState('');
  const [selected, setSelected] = useState<string[]>([]);
  const [assignOpen, setAssignOpen] = useState(false);

  const summary = useQuery({
    queryKey: ['catalog-queue-summary'],
    queryFn: () => queueApi.summary(),
    refetchInterval: 30_000,
  });

  const items = useQuery({
    queryKey: ['catalog-queue', status, keyword],
    queryFn: () => queueApi.list({ status, keyword: keyword || undefined, pageSize: 100 }),
  });

  const refresh = async () => {
    await queryClient.invalidateQueries({ queryKey: ['catalog-queue'] });
    await queryClient.invalidateQueries({ queryKey: ['catalog-queue-summary'] });
  };

  const changeStatus = useMutation({
    mutationFn: ({ id, next, reason }: { id: string; next: CatalogQueueStatus; reason?: string }) =>
      queueApi.changeStatus(id, next, reason),
    onSuccess: async () => {
      message.success('Đã cập nhật trạng thái công việc.');
      setSelected([]);
      await refresh();
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const changeStatusBatch = useMutation({
    mutationFn: ({ ids, next, reason }: { ids: string[]; next: CatalogQueueStatus; reason?: string }) =>
      queueApi.changeStatusBatch(ids, next, reason),
    onSuccess: async (count) => {
      message.success(`Đã cập nhật trạng thái ${count} việc.`);
      setSelected([]);
      await refresh();
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const remove = useMutation({
    mutationFn: (id: string) => queueApi.remove(id),
    onSuccess: async () => {
      message.success('Đã bỏ việc khỏi hàng đợi.');
      await refresh();
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const confirmReturn = (item: CatalogQueueItem) => {
    let reason = '';

    modal.confirm({
      title: `Trả lại việc "${item.title}"?`,
      width: 520,
      content: (
        <Space direction="vertical" style={{ width: '100%' }}>
          <Typography.Text type="secondary">
            Cán bộ biên mục cần biết phải sửa gì, nên phải nêu lý do.
          </Typography.Text>
          <Input.TextArea
            rows={3}
            placeholder="Ví dụ: thiếu chỉ số phân loại và đề mục chủ đề"
            onChange={(event) => {
              reason = event.target.value;
            }}
          />
        </Space>
      ),
      okText: 'Trả lại',
      cancelText: 'Không',
      onOk: async () => {
        if (!reason.trim()) {
          message.error('Phải nêu lý do trả lại.');
          return Promise.reject(new Error('reason'));
        }

        return changeStatus.mutateAsync({ id: item.id, next: 'Returned', reason: reason.trim() });
      },
    });
  };

  const counts = summary.data;

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Hàng đợi biên mục chi tiết"
        description="Biểu ghi từ biên mục sơ lược và từ các lần nhập tệp vào đây chờ cán bộ biên mục đầy đủ. Phân công, đặt hạn xử lý, duyệt hoặc trả lại kèm lý do."
        actions={
          <Space>
            <Can permission={PERMISSIONS.cataloging.queueAssign}>
              <Button
                icon={<TeamOutlined />}
                disabled={selected.length === 0}
                onClick={() => setAssignOpen(true)}
              >
                Phân công {selected.length > 0 ? `(${selected.length})` : ''}
              </Button>
            </Can>

            <Can permission={PERMISSIONS.cataloging.queueProcess}>
              <Button
                type="primary"
                icon={<CheckOutlined />}
                disabled={selected.length === 0}
                loading={changeStatusBatch.isPending}
                onClick={() =>
                  modal.confirm({
                    title: `Duyệt ${selected.length} biểu ghi?`,
                    content:
                      'Biểu ghi được duyệt sẽ hiện ngay trên trang tra cứu của bạn đọc. '
                      + 'Dùng khi cả nhóm cùng một nguồn và đã đạt yêu cầu.',
                    okText: 'Duyệt',
                    cancelText: 'Để sau',
                    onOk: () =>
                      changeStatusBatch.mutateAsync({ ids: selected, next: 'Completed' }),
                  })
                }
              >
                Duyệt {selected.length > 0 ? `(${selected.length})` : ''}
              </Button>
            </Can>
          </Space>
        }
      />

      {counts && counts.overdue > 0 && (
        <Card size="small" styles={{ body: { padding: 12 } }}>
          <Typography.Text type="danger">
            {counts.overdue} việc đã quá hạn xử lý.
          </Typography.Text>
        </Card>
      )}

      <Tabs
        activeKey={status}
        onChange={(key) => {
          setStatus(key as CatalogQueueStatus);
          setSelected([]);
        }}
        items={[...COLUMNS, { key: 'Returned' as CatalogQueueStatus, label: 'Bị trả lại', hint: '' }].map(
          (column) => ({
            key: column.key,
            label: (
              <Tooltip title={'hint' in column ? column.hint : undefined}>
                <Space size={6}>
                  <span>{column.label}</span>
                  <Badge
                    count={
                      counts
                        ? column.key === 'Pending'
                          ? counts.pending
                          : column.key === 'InProgress'
                            ? counts.inProgress
                            : column.key === 'WaitingApproval'
                              ? counts.waitingApproval
                              : column.key === 'Completed'
                                ? counts.completed
                                : counts.returned
                        : 0
                    }
                    showZero
                    color={column.key === 'Completed' ? 'green' : 'blue'}
                  />
                </Space>
              </Tooltip>
            ),
          }),
        )}
      />

      <Input.Search
        value={keyword}
        onChange={(event) => setKeyword(event.target.value)}
        placeholder="Tìm theo nhan đề hoặc số kiểm soát"
        allowClear
        style={{ width: 360 }}
      />

      <Table<CatalogQueueItem>
        rowKey="id"
        size="small"
        loading={items.isFetching}
        dataSource={items.data?.items ?? []}
        pagination={false}
        locale={{ emptyText: <Empty description="Không có việc nào ở trạng thái này" /> }}
        rowSelection={{
          selectedRowKeys: selected,
          onChange: (keys) => setSelected(keys as string[]),
        }}
        columns={[
          {
            title: 'Biểu ghi',
            render: (_, row) => (
              <Space direction="vertical" size={0}>
                <Typography.Link onClick={() => navigate(`/bien-muc/${row.bibId}`)}>
                  {row.title}
                </Typography.Link>
                <Typography.Text type="secondary" style={{ fontSize: 12, ...MONOSPACE }}>
                  {row.controlNumber}
                </Typography.Text>
              </Space>
            ),
          },
          { title: 'Tác giả', dataIndex: 'authorMain', width: 170 },
          { title: 'Dạng', dataIndex: 'documentTypeName', width: 130 },
          {
            title: 'Ưu tiên',
            dataIndex: 'priority',
            width: 120,
            render: (value: number) => (
              <Tag color={value <= 2 ? 'red' : value === 3 ? 'blue' : 'default'}>
                {PRIORITY_LABELS[value] ?? value}
              </Tag>
            ),
          },
          { title: 'Cán bộ', dataIndex: 'assignedToName', width: 170 },
          {
            title: 'Hạn xử lý',
            dataIndex: 'deadline',
            width: 130,
            render: (value: string | null, row) =>
              value ? (
                <Typography.Text type={row.isOverdue ? 'danger' : undefined}>
                  {dayjs(value).format('DD/MM/YYYY')}
                </Typography.Text>
              ) : (
                <Typography.Text type="secondary">—</Typography.Text>
              ),
          },
          {
            title: 'Ghi chú',
            render: (_, row) => (
              <Typography.Text type={row.returnReason ? 'danger' : 'secondary'} style={{ fontSize: 12 }}>
                {row.returnReason ? `Trả lại: ${row.returnReason}` : row.note}
              </Typography.Text>
            ),
          },
          {
            title: '',
            width: 190,
            align: 'right',
            render: (_, row) => (
              <Space size={0}>
                <Can permission={PERMISSIONS.cataloging.queueProcess}>
                  {row.status === 'Pending' || row.status === 'Returned' ? (
                    <Tooltip title="Nhận việc">
                      <Button
                        type="text"
                        icon={<PlayCircleOutlined />}
                        onClick={() => changeStatus.mutate({ id: row.id, next: 'InProgress' })}
                      />
                    </Tooltip>
                  ) : null}
                </Can>

                <Tooltip title="Mở trình soạn MARC">
                  <Button
                    type="text"
                    icon={<EditOutlined />}
                    onClick={() => navigate(`/bien-muc/${row.bibId}/sua`)}
                  />
                </Tooltip>

                <Can permission={PERMISSIONS.cataloging.queueProcess}>
                  {row.status === 'InProgress' ? (
                    <Tooltip title="Gửi duyệt">
                      <Button
                        type="text"
                        icon={<SendOutlined />}
                        onClick={() => changeStatus.mutate({ id: row.id, next: 'WaitingApproval' })}
                      />
                    </Tooltip>
                  ) : null}
                </Can>

                <Can permission={PERMISSIONS.cataloging.queueApprove}>
                  {row.status === 'WaitingApproval' ? (
                    <>
                      <Tooltip title="Duyệt">
                        <Button
                          type="text"
                          icon={<CheckOutlined />}
                          onClick={() => changeStatus.mutate({ id: row.id, next: 'Completed' })}
                        />
                      </Tooltip>
                      <Tooltip title="Trả lại kèm lý do">
                        <Button type="text" danger icon={<RollbackOutlined />} onClick={() => confirmReturn(row)} />
                      </Tooltip>
                    </>
                  ) : null}
                </Can>

                <Can permission={PERMISSIONS.cataloging.queueAssign}>
                  <Tooltip title="Bỏ khỏi hàng đợi">
                    <Button type="text" danger icon={<DeleteOutlined />} onClick={() => remove.mutate(row.id)} />
                  </Tooltip>
                </Can>
              </Space>
            ),
          },
        ]}
      />

      <ProductivityCard />

      <AssignModal
        open={assignOpen}
        ids={selected}
        onClose={() => setAssignOpen(false)}
        onDone={async () => {
          setAssignOpen(false);
          setSelected([]);
          await refresh();
        }}
      />
    </Space>
  );
}

function AssignModal({
  open,
  ids,
  onClose,
  onDone,
}: {
  open: boolean;
  ids: string[];
  onClose: () => void;
  onDone: () => void | Promise<void>;
}) {
  const { message } = App.useApp();
  const [assignedTo, setAssignedTo] = useState<string | undefined>();
  const [priority, setPriority] = useState<number | undefined>();
  const [deadline, setDeadline] = useState<dayjs.Dayjs | null>(null);
  const [note, setNote] = useState('');

  const staff = useQuery({
    queryKey: ['staff-options'],
    queryFn: () =>
      api.get<PagedResult<{ id: string; fullName: string; username: string }>>('/users', {
        params: { pageSize: 200, isActive: true },
      }),
    enabled: open,
  });

  const assign = useMutation({
    mutationFn: () =>
      queueApi.assign({
        ids,
        assignedTo: assignedTo ?? null,
        priority,
        deadline: deadline ? deadline.format('YYYY-MM-DD') : undefined,
        note: note.trim() || undefined,
      }),
    onSuccess: async (count) => {
      message.success(`Đã phân công ${count} việc.`);
      setNote('');
      await onDone();
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  return (
    <Modal
      open={open}
      title={`Phân công ${ids.length} việc`}
      okText="Phân công"
      cancelText="Hủy"
      confirmLoading={assign.isPending}
      onCancel={onClose}
      onOk={() => assign.mutate()}
    >
      <Space direction="vertical" size={12} style={{ width: '100%' }}>
        <div>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Cán bộ biên mục
          </Typography.Text>
          <Select
            value={assignedTo}
            onChange={setAssignedTo}
            options={(staff.data?.items ?? []).map((user) => ({
              value: user.id,
              label: `${user.fullName} (${user.username})`,
            }))}
            placeholder="Bỏ trống để trả việc về cột chờ xử lý"
            allowClear
            showSearch
            optionFilterProp="label"
            style={{ width: '100%' }}
          />
        </div>

        <Row gutter={12}>
          <Col span={12}>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              Độ ưu tiên
            </Typography.Text>
            <Select
              value={priority}
              onChange={setPriority}
              options={Object.entries(PRIORITY_LABELS).map(([value, label]) => ({
                value: Number(value),
                label,
              }))}
              placeholder="Giữ nguyên"
              allowClear
              style={{ width: '100%' }}
            />
          </Col>
          <Col span={12}>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              Hạn xử lý
            </Typography.Text>
            <DatePicker
              value={deadline}
              onChange={setDeadline}
              format="DD/MM/YYYY"
              placeholder="Không đặt hạn"
              style={{ width: '100%' }}
            />
          </Col>
        </Row>

        <div>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Ghi chú cho cán bộ
          </Typography.Text>
          <Input.TextArea
            value={note}
            onChange={(event) => setNote(event.target.value)}
            rows={2}
            maxLength={500}
            placeholder="Ví dụ: bổ sung đề mục chủ đề và chỉ số DDC; bỏ trống thì giữ ghi chú cũ"
          />
        </div>
      </Space>
    </Modal>
  );
}

/** Năng suất biên mục theo cán bộ (II.4). */
function ProductivityCard() {
  const productivity = useQuery({
    queryKey: ['catalog-productivity'],
    queryFn: () => queueApi.productivity(),
  });

  if (!productivity.data || productivity.data.length === 0) {
    return null;
  }

  return (
    <Card size="small" title="Năng suất biên mục">
      <Table
        rowKey={(row) => row.userId ?? row.userName}
        size="small"
        dataSource={productivity.data}
        pagination={false}
        columns={[
          { title: 'Cán bộ', dataIndex: 'userName' },
          { title: 'Được giao', dataIndex: 'assigned', width: 120, align: 'right' },
          { title: 'Hoàn thành', dataIndex: 'completed', width: 120, align: 'right' },
          { title: 'Bị trả lại', dataIndex: 'returned', width: 120, align: 'right' },
          {
            title: 'Thời gian trung bình',
            dataIndex: 'averageDays',
            width: 180,
            align: 'right',
            render: (value?: number) => (value === null || value === undefined ? '—' : `${value} ngày`),
          },
        ]}
      />
    </Card>
  );
}

export { QUEUE_STATUS_LABELS };
