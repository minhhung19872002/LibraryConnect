import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Input,
  Select,
  Space,
  Statistic,
  Table,
  Tag,
  Typography,
} from 'antd';
import { CheckCircleOutlined } from '@ant-design/icons';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import { PageHeader } from '@/components/PageHeader';
import { FilterBar } from '@/components/FilterBar';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { circulationApi } from './api';
import { formatDate, formatDateTime, holdStatusColors, holdStatusLabels } from './labels';
import type { HoldRowDto, HoldStatus, PendingRenewalDto } from './types';

const statusOptions = (Object.keys(holdStatusLabels) as HoldStatus[]).map((value) => ({
  value,
  label: holdStatusLabels[value],
}));

/**
 * VII.2 — Đặt giữ chỗ và các yêu cầu gia hạn từ xa.
 *
 * Hai việc này đi cùng nhau vì cùng là hàng đợi cán bộ phải xử lý mỗi sáng: ai đang chờ sách, và ai
 * xin gia hạn qua trang tra cứu.
 */
export function HoldsPage() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [filter, setFilter] = useState<{ keyword?: string; status?: HoldStatus; activeOnly?: boolean }>({
    activeOnly: true,
  });
  const [draft, setDraft] = useState(filter);
  const [page, setPage] = useState({ page: 1, pageSize: 20 });

  const holds = useQuery({
    queryKey: ['circulation-holds', page, filter],
    queryFn: () => circulationApi.holds({ ...page, ...filter }),
    placeholderData: keepPreviousData,
  });

  const renewals = useQuery({
    queryKey: ['circulation-pending-renewals'],
    queryFn: () => circulationApi.pendingRenewals(),
  });

  const cancel = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      circulationApi.cancelHold(id, reason),
    onSuccess: () => {
      message.success('Đã hủy phiếu đặt giữ.');
      void queryClient.invalidateQueries({ queryKey: ['circulation-holds'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không hủy được.'),
  });

  const process = useMutation({
    mutationFn: ({ id, approve, reason }: { id: string; approve: boolean; reason?: string }) =>
      circulationApi.processRenewal(id, { approve, rejectReason: reason }),
    onSuccess: (_, variables) => {
      message.success(variables.approve ? 'Đã duyệt gia hạn.' : 'Đã từ chối yêu cầu.');
      void queryClient.invalidateQueries({ queryKey: ['circulation-pending-renewals'] });
      void queryClient.invalidateQueries({ queryKey: ['circulation-loans'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xử lý được.'),
  });

  const columns: ColumnsType<HoldRowDto> = [
    {
      title: 'Thứ tự',
      dataIndex: 'queuePosition',
      width: 90,
      align: 'center',
      render: (value: number, row) =>
        row.status === 'Ready' ? <Tag color="green">Sẵn sàng</Tag> : `#${value}`,
    },
    {
      title: 'Bạn đọc',
      dataIndex: 'readerName',
      width: 240,
      render: (name: string, row) => (
        <Space direction="vertical" size={0}>
          <span>{name}</span>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {row.readerCardNumber}
          </Typography.Text>
        </Space>
      ),
    },
    { title: 'Nhan đề', dataIndex: 'title', width: 300, ellipsis: true },
    {
      title: 'Bản đang giữ',
      dataIndex: 'barcode',
      width: 140,
      render: (value: string | null) => value ?? '',
    },
    {
      title: 'Bản rảnh',
      dataIndex: 'availableCopies',
      width: 100,
      align: 'right',
      render: (value: number) =>
        value > 0 ? <Tag color="green">{value}</Tag> : <Tag color="orange">0</Tag>,
    },
    { title: 'Ngày đặt', dataIndex: 'holdDate', width: 150, render: formatDateTime },
    {
      title: 'Hạn nhận',
      dataIndex: 'expireDate',
      width: 130,
      render: (value: string | null) => formatDate(value),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      width: 140,
      render: (status: HoldStatus) => (
        <Tag color={holdStatusColors[status]}>{holdStatusLabels[status]}</Tag>
      ),
    },
    {
      title: '',
      width: 90,
      render: (_, row) =>
        row.status === 'Waiting' || row.status === 'Ready' ? (
          <Can permission={PERMISSIONS.circulation.holdManage}>
            <Button
              type="link"
              danger
              size="small"
              onClick={() => {
                let reason = '';

                modal.confirm({
                  title: 'Hủy phiếu đặt giữ',
                  content: (
                    <Input.TextArea
                      rows={2}
                      placeholder="Lý do hủy"
                      onChange={(event) => {
                        reason = event.target.value;
                      }}
                    />
                  ),
                  okText: 'Hủy phiếu',
                  cancelText: 'Đóng',
                  onOk: () => cancel.mutateAsync({ id: row.id, reason }),
                });
              }}
            >
              Hủy
            </Button>
          </Can>
        ) : null,
    },
  ];

  const renewalColumns: ColumnsType<PendingRenewalDto> = [
    { title: 'Bạn đọc', dataIndex: 'readerName', width: 220 },
    { title: 'Số thẻ', dataIndex: 'readerCardNumber', width: 140 },
    { title: 'Nhan đề', dataIndex: 'title', width: 300, ellipsis: true },
    { title: 'Hạn hiện tại', dataIndex: 'oldDueDate', width: 130, render: formatDate },
    { title: 'Hạn xin gia hạn', dataIndex: 'newDueDate', width: 150, render: formatDate },
    { title: 'Gửi lúc', dataIndex: 'requestedAt', width: 160, render: formatDateTime },
    {
      title: '',
      width: 190,
      render: (_, row) => (
        <Can permission={PERMISSIONS.circulation.loanRenew}>
          <Space>
            <Button
              type="link"
              size="small"
              icon={<CheckCircleOutlined />}
              onClick={() => process.mutate({ id: row.id, approve: true })}
            >
              Duyệt
            </Button>
            <Button
              type="link"
              danger
              size="small"
              onClick={() => {
                let reason = '';

                modal.confirm({
                  title: 'Từ chối yêu cầu gia hạn',
                  content: (
                    <Input.TextArea
                      rows={2}
                      placeholder="Lý do từ chối"
                      onChange={(event) => {
                        reason = event.target.value;
                      }}
                    />
                  ),
                  okText: 'Từ chối',
                  cancelText: 'Đóng',
                  onOk: () => process.mutateAsync({ id: row.id, approve: false, reason }),
                });
              }}
            >
              Từ chối
            </Button>
          </Space>
        </Can>
      ),
    },
  ];

  const ready = (holds.data?.items ?? []).filter((hold) => hold.status === 'Ready');

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Đặt giữ chỗ"
        description="Hàng đợi bạn đọc chờ tài liệu và các yêu cầu gia hạn gửi từ trang tra cứu."
      />

      {ready.length > 0 && (
        <Alert
          type="success"
          showIcon
          message={`${ready.length} tài liệu đang giữ tại quầy chờ bạn đọc tới nhận.`}
        />
      )}

      <Space size={16} wrap>
        <Card size="small">
          <Statistic title="Tổng phiếu đang mở" value={holds.data?.totalCount ?? 0} />
        </Card>
        <Card size="small">
          <Statistic title="Sẵn sàng nhận" value={ready.length} valueStyle={{ color: '#389e0d' }} />
        </Card>
        <Card size="small">
          <Statistic
            title="Yêu cầu gia hạn chờ duyệt"
            value={renewals.data?.length ?? 0}
            valueStyle={{ color: (renewals.data?.length ?? 0) > 0 ? '#d46b08' : undefined }}
          />
        </Card>
      </Space>

      <FilterBar
        loading={holds.isFetching}
        onSearch={() => {
          setFilter(draft);
          setPage((current) => ({ ...current, page: 1 }));
        }}
        onReset={() => {
          setDraft({ activeOnly: true });
          setFilter({ activeOnly: true });
        }}
      >
        <Input
          allowClear
          style={{ width: 260 }}
          placeholder="Số thẻ, tên bạn đọc, nhan đề…"
          value={draft.keyword}
          onChange={(event) => setDraft({ ...draft, keyword: event.target.value })}
        />
        <Select
          allowClear
          style={{ width: 180 }}
          placeholder="Trạng thái"
          options={statusOptions}
          value={draft.status}
          onChange={(value) => setDraft({ ...draft, status: value, activeOnly: undefined })}
        />
      </FilterBar>

      <Table
        rowKey="id"
        size="small"
        loading={holds.isFetching}
        dataSource={holds.data?.items ?? []}
        columns={columns}
        scroll={{ x: 1500 }}
        pagination={{
          current: holds.data?.page ?? 1,
          pageSize: holds.data?.pageSize ?? 20,
          total: holds.data?.totalCount ?? 0,
          showSizeChanger: true,
          showTotal: (total) => `Tổng ${total.toLocaleString('vi-VN')} phiếu`,
        }}
        onChange={(pagination) =>
          setPage({ page: pagination.current ?? 1, pageSize: pagination.pageSize ?? 20 })
        }
      />

      <Card size="small" title="Yêu cầu gia hạn chờ duyệt">
        <Table
          rowKey="id"
          size="small"
          loading={renewals.isLoading}
          dataSource={renewals.data ?? []}
          columns={renewalColumns}
          scroll={{ x: 1300 }}
          pagination={false}
          locale={{ emptyText: 'Không có yêu cầu nào đang chờ' }}
        />
      </Card>
    </Space>
  );
}
