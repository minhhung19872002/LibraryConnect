import { useState } from 'react';
import {
  App,
  Button,
  Card,
  Checkbox,
  Form,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Statistic,
  Table,
  Tabs,
  Tag,
  Typography,
} from 'antd';
import { CheckCircleOutlined, StopOutlined } from '@ant-design/icons';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import { PageHeader } from '@/components/PageHeader';
import { FilterBar } from '@/components/FilterBar';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { digitalApi } from './api';
import { MAU } from '@/lib/palette';
import {
  accessActionLabels,
  formatDate,
  formatDateTime,
  requestStatusColors,
  requestStatusLabels,
} from './labels';
import type {
  AccessRequestStatus,
  DigitalAccessLogRowDto,
  DigitalAccessRequestRowDto,
} from './types';

const statusOptions = (Object.keys(requestStatusLabels) as AccessRequestStatus[]).map((value) => ({
  value,
  label: requestStatusLabels[value],
}));

/**
 * V.2 — Xử lý yêu cầu đọc tài liệu hạn chế và tra nhật ký truy cập.
 *
 * Hai việc đứng chung một màn hình vì cán bộ hay làm nối nhau: duyệt cho ai đó đọc rồi xem lại họ
 * đã mở tài liệu bao nhiêu lần, từ máy nào.
 */
export function DigitalRequestsPage() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [filter, setFilter] = useState<{ keyword?: string; status?: AccessRequestStatus }>({
    status: 'Pending',
  });
  const [draft, setDraft] = useState(filter);
  const [page, setPage] = useState({ page: 1, pageSize: 20 });
  const [logPage, setLogPage] = useState({ page: 1, pageSize: 20 });
  const [approving, setApproving] = useState<DigitalAccessRequestRowDto | null>(null);
  const [approveForm] = Form.useForm();

  const requests = useQuery({
    queryKey: ['digital-requests', page, filter],
    queryFn: () => digitalApi.requests({ ...page, keyword: filter.keyword, filter: { status: filter.status } }),
    placeholderData: keepPreviousData,
  });

  const pending = useQuery({
    queryKey: ['digital-requests-pending'],
    queryFn: () => digitalApi.requests({ page: 1, pageSize: 1, filter: { status: 'Pending' } }),
  });

  const logs = useQuery({
    queryKey: ['digital-logs', logPage],
    queryFn: () => digitalApi.logs({ ...logPage, filter: {} }),
    placeholderData: keepPreviousData,
  });

  const approve = useMutation({
    mutationFn: (values: Record<string, unknown>) => digitalApi.approveRequest(approving!.id, values),
    onSuccess: (row) => {
      message.success(
        `Đã duyệt cho ${row.readerName} đọc tới ${formatDate(row.expireAt)}.`,
      );
      setApproving(null);
      void queryClient.invalidateQueries({ queryKey: ['digital-requests'] });
      void queryClient.invalidateQueries({ queryKey: ['digital-requests-pending'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không duyệt được.'),
  });

  const reject = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      digitalApi.rejectRequest(id, reason),
    onSuccess: () => {
      message.success('Đã từ chối và thông báo cho bạn đọc.');
      void queryClient.invalidateQueries({ queryKey: ['digital-requests'] });
      void queryClient.invalidateQueries({ queryKey: ['digital-requests-pending'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không từ chối được.'),
  });

  const revoke = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      digitalApi.revokeRequest(id, reason),
    onSuccess: () => {
      message.success('Đã thu hồi quyền đọc.');
      void queryClient.invalidateQueries({ queryKey: ['digital-requests'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không thu hồi được.'),
  });

  const askReason = (title: string, okText: string, action: (reason: string) => Promise<unknown>) => {
    let reason = '';

    modal.confirm({
      title,
      content: (
        <Input.TextArea
          rows={2}
          placeholder="Lý do"
          onChange={(event) => {
            reason = event.target.value;
          }}
        />
      ),
      okText,
      cancelText: 'Đóng',
      onOk: () => action(reason),
    });
  };

  const columns: ColumnsType<DigitalAccessRequestRowDto> = [
    {
      title: 'Bạn đọc',
      dataIndex: 'readerName',
      width: 230,
      render: (name: string, row) => (
        <Space direction="vertical" size={0}>
          <span>{name}</span>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {row.readerCardNumber}
            {row.readerTypeName ? ` · ${row.readerTypeName}` : ''}
          </Typography.Text>
        </Space>
      ),
    },
    { title: 'Khoa', dataIndex: 'facultyName', width: 180, ellipsis: true },
    { title: 'Tài liệu', dataIndex: 'documentTitle', width: 300, ellipsis: true },
    { title: 'Lý do sử dụng', dataIndex: 'reason', width: 260, ellipsis: true },
    { title: 'Gửi lúc', dataIndex: 'requestDate', width: 160, render: formatDateTime },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      width: 130,
      render: (status: AccessRequestStatus, row) => (
        <Space direction="vertical" size={0}>
          <Tag color={requestStatusColors[status]}>{requestStatusLabels[status]}</Tag>
          {row.expireAt && status === 'Approved' && (
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              tới {formatDate(row.expireAt)}
            </Typography.Text>
          )}
          {row.rejectReason && (
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              {row.rejectReason}
            </Typography.Text>
          )}
        </Space>
      ),
    },
    {
      title: 'Lượt xem',
      width: 110,
      align: 'right',
      render: (_, row) => (row.maxViews ? `${row.viewCount}/${row.maxViews}` : row.viewCount),
    },
    {
      title: 'Xử lý sau',
      dataIndex: 'processingHours',
      width: 110,
      align: 'right',
      render: (hours: number | null | undefined) =>
        hours == null ? '' : `${hours.toFixed(1)} giờ`,
    },
    {
      title: '',
      width: 200,
      render: (_, row) => (
        <Can permission={PERMISSIONS.digital.requestApprove}>
          <Space size={2}>
            {row.status === 'Pending' && (
              <>
                <Button
                  type="link"
                  size="small"
                  icon={<CheckCircleOutlined />}
                  onClick={() => {
                    setApproving(row);
                    approveForm.setFieldsValue({ days: 30, maxViews: 0, allowDownload: false });
                  }}
                >
                  Duyệt
                </Button>
                <Button
                  type="link"
                  size="small"
                  danger
                  onClick={() =>
                    askReason('Từ chối yêu cầu đọc', 'Từ chối', (reason) =>
                      reject.mutateAsync({ id: row.id, reason }),
                    )
                  }
                >
                  Từ chối
                </Button>
              </>
            )}
            {row.status === 'Approved' && (
              <Button
                type="link"
                size="small"
                danger
                icon={<StopOutlined />}
                onClick={() =>
                  askReason('Thu hồi quyền đọc', 'Thu hồi', (reason) =>
                    revoke.mutateAsync({ id: row.id, reason }),
                  )
                }
              >
                Thu hồi
              </Button>
            )}
          </Space>
        </Can>
      ),
    },
  ];

  const logColumns: ColumnsType<DigitalAccessLogRowDto> = [
    { title: 'Thời điểm', dataIndex: 'occurredAt', width: 170, render: formatDateTime },
    { title: 'Tài liệu', dataIndex: 'documentTitle', width: 300, ellipsis: true },
    {
      title: 'Người xem',
      width: 220,
      render: (_, row) =>
        row.readerName ? `${row.readerName} (${row.readerCardNumber})` : row.userName ?? 'Khách',
    },
    {
      title: 'Hành động',
      dataIndex: 'action',
      width: 110,
      render: (action: DigitalAccessLogRowDto['action']) => accessActionLabels[action],
    },
    {
      title: 'Trang',
      width: 90,
      align: 'right',
      render: (_, row) => (row.pageFrom ? `${row.pageFrom}` : ''),
    },
    { title: 'Địa chỉ IP', dataIndex: 'ip', width: 150 },
    { title: 'Thiết bị', dataIndex: 'device', ellipsis: true },
  ];

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Yêu cầu đọc tài liệu"
        description="Duyệt quyền đọc tài liệu hạn chế và tra nhật ký ai đã xem tài liệu nào."
      />

      <Space size={16} wrap>
        <Card size="small">
          <Statistic
            title="Đang chờ duyệt"
            value={pending.data?.totalCount ?? 0}
            valueStyle={{ color: (pending.data?.totalCount ?? 0) > 0 ? MAU.luuY : undefined }}
          />
        </Card>
        <Card size="small">
          <Statistic title="Yêu cầu đang hiển thị" value={requests.data?.totalCount ?? 0} />
        </Card>
      </Space>

      <Tabs
        items={[
          {
            key: 'requests',
            label: 'Yêu cầu đọc',
            children: (
              <Space direction="vertical" size={12} style={{ width: '100%' }}>
                <FilterBar
                  loading={requests.isFetching}
                  onSearch={() => {
                    setFilter(draft);
                    setPage((current) => ({ ...current, page: 1 }));
                  }}
                  onReset={() => {
                    setDraft({});
                    setFilter({});
                  }}
                >
                  <Input
                    allowClear
                    style={{ width: 280 }}
                    placeholder="Số thẻ, tên bạn đọc, nhan đề tài liệu…"
                    value={draft.keyword}
                    onChange={(event) => setDraft({ ...draft, keyword: event.target.value })}
                  />
                  <Select
                    allowClear
                    style={{ width: 170 }}
                    placeholder="Trạng thái"
                    options={statusOptions}
                    value={draft.status}
                    onChange={(value) => setDraft({ ...draft, status: value })}
                  />
                </FilterBar>

                <Table
                  rowKey="id"
                  size="small"
                  loading={requests.isFetching}
                  dataSource={requests.data?.items ?? []}
                  columns={columns}
                  scroll={{ x: 1700 }}
                  pagination={{
                    current: requests.data?.page ?? 1,
                    pageSize: requests.data?.pageSize ?? 20,
                    total: requests.data?.totalCount ?? 0,
                    showSizeChanger: true,
                    showTotal: (total) => `Tổng ${total.toLocaleString('vi-VN')} yêu cầu`,
                  }}
                  onChange={(pagination) =>
                    setPage({ page: pagination.current ?? 1, pageSize: pagination.pageSize ?? 20 })
                  }
                />
              </Space>
            ),
          },
          {
            key: 'logs',
            label: 'Nhật ký truy cập',
            children: (
              <Table
                rowKey="id"
                size="small"
                loading={logs.isFetching}
                dataSource={logs.data?.items ?? []}
                columns={logColumns}
                scroll={{ x: 1300 }}
                pagination={{
                  current: logs.data?.page ?? 1,
                  pageSize: logs.data?.pageSize ?? 20,
                  total: logs.data?.totalCount ?? 0,
                  showSizeChanger: true,
                  showTotal: (total) => `Tổng ${total.toLocaleString('vi-VN')} lượt`,
                }}
                onChange={(pagination) =>
                  setLogPage({ page: pagination.current ?? 1, pageSize: pagination.pageSize ?? 20 })
                }
              />
            ),
          },
        ]}
      />

      <Modal
        open={approving !== null}
        title={approving ? `Duyệt cho ${approving.readerName}` : ''}
        okText="Duyệt"
        cancelText="Hủy"
        confirmLoading={approve.isPending}
        onCancel={() => setApproving(null)}
        onOk={() => void approveForm.validateFields().then((values) => approve.mutate(values))}
      >
        {approving && (
          <Typography.Paragraph type="secondary">
            Tài liệu: {approving.documentTitle}
            <br />
            Lý do bạn đọc nêu: {approving.reason}
          </Typography.Paragraph>
        )}

        <Form form={approveForm} layout="vertical">
          <Form.Item
            name="days"
            label="Thời hạn đọc (ngày)"
            rules={[{ required: true, message: 'Chưa nhập thời hạn.' }]}
          >
            <InputNumber min={1} max={3650} style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item
            name="maxViews"
            label="Số lần xem tối đa"
            extra="Để 0 nghĩa là không giới hạn số lần xem."
          >
            <InputNumber min={0} max={10000} style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="allowDownload" valuePropName="checked">
            <Checkbox>Cho phép tải tệp về</Checkbox>
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}
