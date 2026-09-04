import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Alert,
  App,
  Badge,
  Button,
  Card,
  Descriptions,
  Empty,
  Form,
  Input,
  List,
  Popconfirm,
  Space,
  Statistic,
  Table,
  Tabs,
  Tag,
} from 'antd';
import { readerApi } from '@/api/opac';
import {
  describeDigitalRequest,
  describeFineType,
  describeHoldStatus,
  describeLoanStatus,
  holdStatusColor,
} from '@/labels';
import { useAuthStore } from '@/stores/authStore';
import type { FineRow, HoldRow, LoanRow } from '@/types/api';
import { formatDate, formatDateTime } from '@/lib/datetime';

const currency = (value: number) => `${value.toLocaleString('vi-VN')} đ`;

const date = (value?: string) => formatDate(value) || '—';

/**
 * IX.3 — Trang cá nhân của bạn đọc.
 *
 * Mọi con số ở đây đều do máy chủ tính: hạn trả, số ngày quá hạn, tiền phạt, còn được gia hạn hay
 * không. Giao diện chỉ hiển thị — nếu tính lại ở đây thì hai nơi sẽ lệch nhau vào đúng ngày lễ.
 */
export function AccountPage() {
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const [tab, setTab] = useState('loans');

  const profile = useQuery({ queryKey: ['profile'], queryFn: () => readerApi.profile() });
  const card = useQuery({ queryKey: ['card'], queryFn: () => readerApi.card() });
  const loans = useQuery({ queryKey: ['loans', 'current'], queryFn: () => readerApi.currentLoans() });
  const history = useQuery({ queryKey: ['loans', 'history'], queryFn: () => readerApi.loanHistory() });
  const holds = useQuery({ queryKey: ['holds'], queryFn: () => readerApi.holds() });
  const fines = useQuery({ queryKey: ['fines'], queryFn: () => readerApi.fines() });
  const notifications = useQuery({
    queryKey: ['notifications'],
    queryFn: () => readerApi.notifications(),
  });
  const favorites = useQuery({ queryKey: ['favorites'], queryFn: () => readerApi.favorites() });
  const savedSearches = useQuery({
    queryKey: ['saved-searches'],
    queryFn: () => readerApi.savedSearches(),
  });
  const renewals = useQuery({
    queryKey: ['card-renewals'],
    queryFn: () => readerApi.cardRenewals(),
  });

  // IX.3 liệt kê "tài liệu số được cấp quyền" trong trang cá nhân; trước 04/09/2026 phần ấy chỉ có ở
  // trang Tài liệu số riêng, và trạng thái yêu cầu đã gửi thì không xem được ở đâu cả.
  const digitalRequests = useQuery({
    queryKey: ['digital-requests'],
    queryFn: () => readerApi.digitalRequests(),
  });

  const renew = useMutation({
    mutationFn: (id: string) => readerApi.renewLoan(id),
    onSuccess: (loan) => {
      // Thư viện bật "gia hạn phải duyệt" thì đây là lượt **gửi yêu cầu**: hạn trả chưa đổi, nên
      // báo đúng việc vừa xảy ra thay vì hứa một ngày mới.
      message.success(
        loan.renewalPending
          ? 'Đã gửi yêu cầu gia hạn. Hạn trả đổi sau khi thư viện duyệt.'
          : `Đã gia hạn tới ngày ${date(loan.dueDate)}.`,
      );
      void queryClient.invalidateQueries({ queryKey: ['loans'] });
    },
    onError: (error: Error) => message.error(error.message),
  });

  const cancelHold = useMutation({
    mutationFn: (id: string) => readerApi.cancelHold(id),
    onSuccess: () => {
      message.success('Đã hủy đặt giữ.');
      void queryClient.invalidateQueries({ queryKey: ['holds'] });
    },
    onError: (error: Error) => message.error(error.message),
  });

  const requestRenewal = useMutation({
    mutationFn: (reason: string) => readerApi.requestCardRenewal(reason),
    onSuccess: () => {
      message.success('Đã gửi yêu cầu gia hạn thẻ.');
      void queryClient.invalidateQueries({ queryKey: ['card-renewals'] });
    },
    onError: (error: Error) => message.error(error.message),
  });

  const updateProfile = useMutation({
    mutationFn: (values: { email?: string; phone?: string; address?: string }) =>
      readerApi.updateProfile(values),
    onSuccess: () => {
      message.success('Đã cập nhật thông tin liên hệ.');
      void queryClient.invalidateQueries({ queryKey: ['profile'] });
    },
    onError: (error: Error) => message.error(error.message),
  });

  const changePassword = useMutation({
    mutationFn: (values: { currentPassword: string; newPassword: string }) =>
      readerApi.changePassword(values.currentPassword, values.newPassword),
    onSuccess: () => message.success('Đã đổi mật khẩu.'),
    onError: (error: Error) => message.error(error.message),
  });

  if (!user) {
    return (
      <div className="lc-container" style={{ padding: 48 }}>
        <Empty description="Bạn cần đăng nhập để xem trang này.">
          <Link to="/dang-nhap">
            <Button type="primary">Đăng nhập</Button>
          </Link>
        </Empty>
      </div>
    );
  }

  const loanColumns = [
    {
      title: 'Nhan đề',
      dataIndex: 'title',
      width: 320,
      render: (title: string) => title ?? '—',
    },
    { title: 'Mã vạch', dataIndex: 'barcode', width: 140 },
    { title: 'Ngày mượn', dataIndex: 'loanDate', width: 120, render: date },
    { title: 'Hạn trả', dataIndex: 'dueDate', width: 120, render: date },
    {
      title: 'Tình trạng',
      dataIndex: 'overdueDays',
      width: 160,
      render: (days: number) =>
        days > 0 ? <Tag color="red">Quá hạn {days} ngày</Tag> : <Tag color="green">Trong hạn</Tag>,
    },
    {
      title: 'Phạt dự kiến',
      dataIndex: 'estimatedFine',
      width: 130,
      align: 'right' as const,
      render: currency,
    },
    {
      title: 'Gia hạn',
      dataIndex: 'id',
      width: 160,
      render: (id: string, row: LoanRow) => (
        <Button
          size="small"
          loading={renew.isPending}
          disabled={row.renewedCount >= row.maxRenewals}
          onClick={() => renew.mutate(id)}
        >
          {row.renewedCount >= row.maxRenewals
            ? 'Hết lượt gia hạn'
            : `Gia hạn (${row.renewedCount}/${row.maxRenewals})`}
        </Button>
      ),
    },
  ];

  const unread = (notifications.data?.items ?? []).filter((row) => !row.isRead).length;

  return (
    <div className="lc-container" style={{ padding: '24px 16px 48px' }}>
      <Card
        style={{ marginBottom: 16 }}
        title={profile.data?.fullName ?? user.fullName}
        extra={
          <Button
            onClick={() => {
              logout();
              navigate('/');
            }}
          >
            Đăng xuất
          </Button>
        }
        loading={profile.isLoading}
      >
        <Space size="large" wrap>
          <Statistic title="Đang mượn" value={profile.data?.currentLoanCount ?? 0} suffix="cuốn" />
          <Statistic
            title="Còn nợ phí"
            value={profile.data?.debtAmount ?? 0}
            formatter={(value) => currency(Number(value))}
          />
          <Statistic title="Số thẻ" value={profile.data?.cardNumber ?? ''} />
          <Statistic title="Hạn thẻ" value={date(profile.data?.cardExpireDate)} />
          <Statistic title="Tình trạng thẻ" value={profile.data?.statusLabel ?? ''} />
        </Space>

        {card.data && card.data.warnings.length > 0 ? (
          <Space direction="vertical" style={{ width: '100%', marginTop: 16 }}>
            {card.data.warnings.map((warning) => (
              <Alert
                key={warning.code}
                type={warning.blocking ? 'error' : 'warning'}
                message={warning.message}
                showIcon
              />
            ))}
          </Space>
        ) : null}
      </Card>

      <Card>
        <Tabs
          activeKey={tab}
          onChange={setTab}
          items={[
            {
              key: 'loans',
              label: `Đang mượn (${loans.data?.totalCount ?? 0})`,
              children: (
                <Table
                  rowKey="id"
                  size="small"
                  loading={loans.isLoading}
                  columns={loanColumns}
                  dataSource={loans.data?.items ?? []}
                  pagination={false}
                  scroll={{ x: 1150 }}
                  locale={{ emptyText: 'Bạn không có tài liệu nào đang mượn.' }}
                />
              ),
            },
            {
              key: 'history',
              label: 'Lịch sử mượn trả',
              children: (
                <Table
                  rowKey="id"
                  size="small"
                  loading={history.isLoading}
                  dataSource={history.data?.items ?? []}
                  pagination={false}
                  scroll={{ x: 900 }}
                  columns={[
                    { title: 'Nhan đề', dataIndex: 'title', width: 320 },
                    { title: 'Ngày mượn', dataIndex: 'loanDate', width: 120, render: date },
                    { title: 'Hạn trả', dataIndex: 'dueDate', width: 120, render: date },
                    { title: 'Ngày trả', dataIndex: 'returnDate', width: 120, render: date },
                    {
                      title: 'Tình trạng',
                      dataIndex: 'status',
                      width: 130,
                      render: (status: LoanRow['status']) => describeLoanStatus(status),
                    },
                    {
                      title: 'Tiền phạt',
                      dataIndex: 'fineAmount',
                      width: 120,
                      align: 'right' as const,
                      render: currency,
                    },
                  ]}
                  locale={{ emptyText: 'Chưa có giao dịch mượn trả nào.' }}
                />
              ),
            },
            {
              key: 'holds',
              label: `Đặt giữ (${holds.data?.totalCount ?? 0})`,
              children: (
                <Table
                  rowKey="id"
                  size="small"
                  loading={holds.isLoading}
                  dataSource={holds.data?.items ?? []}
                  pagination={false}
                  scroll={{ x: 900 }}
                  columns={[
                    {
                      title: 'Nhan đề',
                      dataIndex: 'title',
                      width: 320,
                      render: (title: string, row) => (
                        <Link to={`/tai-lieu/${row.bibId}`}>{title}</Link>
                      ),
                    },
                    { title: 'Ngày đặt', dataIndex: 'holdDate', width: 130, render: date },
                    { title: 'Vị trí hàng đợi', dataIndex: 'queuePosition', width: 130 },
                    { title: 'Nơi nhận', dataIndex: 'pickupWarehouseName', width: 180 },
                    {
                      title: 'Trạng thái',
                      dataIndex: 'status',
                      width: 170,
                      render: (status: HoldRow['status']) => (
                        <Tag color={holdStatusColor(status)}>{describeHoldStatus(status)}</Tag>
                      ),
                    },
                    {
                      title: '',
                      dataIndex: 'id',
                      width: 100,
                      render: (id: string) => (
                        <Popconfirm
                          title="Hủy phiếu đặt giữ này?"
                          okText="Hủy đặt"
                          cancelText="Không"
                          onConfirm={() => cancelHold.mutate(id)}
                        >
                          <Button size="small" danger>
                            Hủy
                          </Button>
                        </Popconfirm>
                      ),
                    },
                  ]}
                  locale={{ emptyText: 'Bạn chưa đặt giữ tài liệu nào.' }}
                />
              ),
            },
            {
              key: 'fines',
              label: 'Tiền phạt',
              children: (
                <>
                  <Space size="large" style={{ marginBottom: 16 }}>
                    <Statistic
                      title="Còn phải nộp"
                      value={fines.data?.totalOutstanding ?? 0}
                      formatter={(value) => currency(Number(value))}
                    />
                    <Statistic
                      title="Đã nộp"
                      value={fines.data?.totalPaid ?? 0}
                      formatter={(value) => currency(Number(value))}
                    />
                    <Statistic
                      title="Được miễn"
                      value={fines.data?.totalWaived ?? 0}
                      formatter={(value) => currency(Number(value))}
                    />
                  </Space>

                  <Table
                    rowKey="id"
                    size="small"
                    loading={fines.isLoading}
                    dataSource={fines.data?.fines ?? []}
                    pagination={false}
                    scroll={{ x: 900 }}
                    columns={[
                      { title: 'Mã', dataIndex: 'code', width: 130 },
                      { title: 'Tài liệu', dataIndex: 'title', width: 300 },
                      {
                        title: 'Lý do',
                        dataIndex: 'type',
                        width: 120,
                        render: (type: FineRow['type']) => describeFineType(type),
                      },
                      {
                        title: 'Số tiền',
                        dataIndex: 'amount',
                        width: 120,
                        align: 'right' as const,
                        render: currency,
                      },
                      {
                        title: 'Còn nợ',
                        dataIndex: 'outstanding',
                        width: 120,
                        align: 'right' as const,
                        render: currency,
                      },
                      { title: 'Ngày lập', dataIndex: 'createdAt', width: 130, render: date },
                    ]}
                    locale={{ emptyText: 'Bạn không có khoản phạt nào.' }}
                  />
                </>
              ),
            },
            {
              key: 'digital',
              label: `Tài liệu số (${digitalRequests.data?.totalCount ?? 0})`,
              children: (
                <List
                  loading={digitalRequests.isLoading}
                  dataSource={digitalRequests.data?.items ?? []}
                  locale={{
                    emptyText: (
                      <Empty
                        description={
                          <>
                            Bạn chưa gửi yêu cầu đọc tài liệu hạn chế nào.{' '}
                            <Link to="/tai-lieu-so">Xem tài liệu số của thư viện</Link>
                          </>
                        }
                      />
                    ),
                  }}
                  renderItem={(item) => (
                    <List.Item
                      actions={
                        item.status === 'Approved'
                          ? [
                              <Link key="read" to={`/tai-lieu-so/${item.documentId}`}>
                                Đọc
                              </Link>,
                            ]
                          : undefined
                      }
                    >
                      <List.Item.Meta
                        title={item.documentTitle}
                        description={
                          <Space direction="vertical" size={0}>
                            <span>Gửi yêu cầu: {formatDateTime(item.requestDate)}</span>
                            {item.status === 'Approved' && item.expireAt && (
                              <span>Được đọc tới {formatDateTime(item.expireAt)}</span>
                            )}
                            {item.status === 'Approved' && item.maxViews != null && (
                              <span>
                                Đã xem {item.viewCount}/{item.maxViews} lần
                                {item.allowDownload ? ' · được tải về' : ''}
                              </span>
                            )}
                            {item.status === 'Rejected' && item.rejectReason && (
                              <span>Lý do từ chối: {item.rejectReason}</span>
                            )}
                          </Space>
                        }
                      />
                      <Tag color={describeDigitalRequest(item.status).color}>
                        {describeDigitalRequest(item.status).label}
                      </Tag>
                    </List.Item>
                  )}
                />
              ),
            },
            {
              key: 'notifications',
              label: <Badge count={unread} size="small" offset={[8, -2]}>Thông báo</Badge>,
              children: (
                <>
                  <Button
                    size="small"
                    style={{ marginBottom: 12 }}
                    disabled={unread === 0}
                    onClick={async () => {
                      await readerApi.markAllNotificationsRead();
                      void queryClient.invalidateQueries({ queryKey: ['notifications'] });
                    }}
                  >
                    Đánh dấu tất cả đã đọc
                  </Button>

                  <List
                    loading={notifications.isLoading}
                    dataSource={notifications.data?.items ?? []}
                    locale={{ emptyText: <Empty description="Chưa có thông báo nào." /> }}
                    renderItem={(item) => (
                      <List.Item
                        actions={
                          item.isRead
                            ? undefined
                            : [
                                <a
                                  key="read"
                                  onClick={async () => {
                                    await readerApi.markNotificationRead(item.id);
                                    void queryClient.invalidateQueries({
                                      queryKey: ['notifications'],
                                    });
                                  }}
                                >
                                  Đánh dấu đã đọc
                                </a>,
                              ]
                        }
                      >
                        <List.Item.Meta
                          title={
                            <Space>
                              {item.isRead ? null : <Badge status="processing" />}
                              <span>{item.title}</span>
                            </Space>
                          }
                          description={
                            <>
                              <div>{item.body}</div>
                              <div style={{ fontSize: 12, color: 'var(--lc-muted)' }}>
                                {formatDateTime(item.createdAt)}
                              </div>
                            </>
                          }
                        />
                      </List.Item>
                    )}
                  />
                </>
              ),
            },
            {
              key: 'favorites',
              label: 'Yêu thích',
              children: (
                <List
                  loading={favorites.isLoading}
                  dataSource={favorites.data?.items ?? []}
                  locale={{ emptyText: <Empty description="Bạn chưa đánh dấu tài liệu nào." /> }}
                  renderItem={(item) => (
                    <List.Item>
                      <List.Item.Meta
                        title={<Link to={`/tai-lieu/${item.id}`}>{item.title}</Link>}
                        description={[item.authorMain, item.publishYear].filter(Boolean).join(' • ')}
                      />
                    </List.Item>
                  )}
                />
              ),
            },
            {
              key: 'searches',
              label: 'Tìm kiếm đã lưu',
              children: (
                <List
                  loading={savedSearches.isLoading}
                  dataSource={savedSearches.data ?? []}
                  locale={{ emptyText: <Empty description="Bạn chưa lưu tìm kiếm nào." /> }}
                  renderItem={(item) => (
                    <List.Item
                      actions={[
                        <a
                          key="delete"
                          onClick={async () => {
                            await readerApi.deleteSavedSearch(item.id);
                            void queryClient.invalidateQueries({ queryKey: ['saved-searches'] });
                          }}
                        >
                          Xóa
                        </a>,
                      ]}
                    >
                      <List.Item.Meta
                        title={item.name}
                        description={formatDateTime(item.createdAt)}
                      />
                    </List.Item>
                  )}
                />
              ),
            },
            {
              key: 'profile',
              label: 'Thông tin cá nhân',
              children: (
                <Space direction="vertical" size="large" style={{ width: '100%', maxWidth: 640 }}>
                  <Descriptions column={1} size="small" bordered>
                    <Descriptions.Item label="Họ và tên">
                      {profile.data?.fullName}
                    </Descriptions.Item>
                    <Descriptions.Item label="Mã sinh viên">
                      {profile.data?.studentCode ?? '—'}
                    </Descriptions.Item>
                    <Descriptions.Item label="Loại bạn đọc">
                      {profile.data?.readerTypeName}
                    </Descriptions.Item>
                    <Descriptions.Item label="Khoa / Ngành">
                      {[profile.data?.facultyName, profile.data?.majorName]
                        .filter(Boolean)
                        .join(' — ') || '—'}
                    </Descriptions.Item>
                    <Descriptions.Item label="Lớp">
                      {profile.data?.className ?? '—'}
                    </Descriptions.Item>
                  </Descriptions>

                  <Card size="small" title="Cập nhật thông tin liên hệ">
                    <Form
                      layout="vertical"
                      initialValues={{
                        email: profile.data?.email,
                        phone: profile.data?.phone,
                        address: profile.data?.address,
                      }}
                      onFinish={(values) => updateProfile.mutate(values)}
                    >
                      <Form.Item label="Email" name="email">
                        <Input placeholder="Địa chỉ email nhận thông báo" />
                      </Form.Item>
                      <Form.Item label="Điện thoại" name="phone">
                        <Input />
                      </Form.Item>
                      <Form.Item label="Địa chỉ" name="address">
                        <Input />
                      </Form.Item>
                      <Button type="primary" htmlType="submit" loading={updateProfile.isPending}>
                        Lưu thay đổi
                      </Button>
                    </Form>
                  </Card>

                  <Card size="small" title="Đổi mật khẩu">
                    <Form
                      layout="vertical"
                      onFinish={(values) => changePassword.mutate(values)}
                      requiredMark={false}
                    >
                      <Form.Item
                        label="Mật khẩu hiện tại"
                        name="currentPassword"
                        rules={[{ required: true, message: 'Chưa nhập mật khẩu hiện tại.' }]}
                      >
                        <Input.Password />
                      </Form.Item>
                      <Form.Item
                        label="Mật khẩu mới"
                        name="newPassword"
                        rules={[{ required: true, message: 'Chưa nhập mật khẩu mới.' }]}
                      >
                        <Input.Password />
                      </Form.Item>
                      <Button type="primary" htmlType="submit" loading={changePassword.isPending}>
                        Đổi mật khẩu
                      </Button>
                    </Form>
                  </Card>

                  <Card size="small" title="Gia hạn thẻ thư viện">
                    <Form
                      layout="vertical"
                      onFinish={(values: { reason: string }) => requestRenewal.mutate(values.reason)}
                    >
                      <Form.Item label="Lý do gia hạn" name="reason">
                        <Input.TextArea rows={2} placeholder="Ví dụ: tiếp tục học năm cuối" />
                      </Form.Item>
                      <Button type="primary" htmlType="submit" loading={requestRenewal.isPending}>
                        Gửi yêu cầu
                      </Button>
                    </Form>

                    <List
                      style={{ marginTop: 12 }}
                      loading={renewals.isLoading}
                      dataSource={renewals.data ?? []}
                      locale={{ emptyText: 'Chưa gửi yêu cầu nào.' }}
                      renderItem={(item) => (
                        <List.Item>
                          <List.Item.Meta
                            title={`Gửi ngày ${formatDate(item.requestDate)}`}
                            description={
                              <Space size={[8, 4]} wrap>
                                <Tag>{item.statusLabel}</Tag>
                                {item.newExpireDate ? (
                                  <span>Hạn thẻ mới: {date(item.newExpireDate)}</span>
                                ) : null}
                                {item.rejectReason ? <span>{item.rejectReason}</span> : null}
                              </Space>
                            }
                          />
                        </List.Item>
                      )}
                    />
                  </Card>
                </Space>
              ),
            },
          ]}
        />
      </Card>
    </div>
  );
}
