import { useState } from 'react';
import {
  App,
  Alert,
  Avatar,
  Button,
  Card,
  Descriptions,
  Drawer,
  Empty,
  Form,
  Input,
  InputNumber,
  Modal,
  Popconfirm,
  Select,
  Space,
  Table,
  Tabs,
  Tag,
  Typography,
} from 'antd';
import {
  CameraOutlined,
  IdcardOutlined,
  KeyOutlined,
  PrinterOutlined,
  WarningOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { saveBlob } from '@/modules/marc/api';
import { useCatalogOptions, toOptions } from '@/modules/cataloging/useCatalogOptions';
import { readersApi } from './api';
import { ReaderPhotoCapture } from './ReaderPhotoCapture';
import { useReaderPhoto } from './useReaderPhoto';
import {
  describeExpiry,
  formatDate,
  formatDateTime,
  initials,
  loanStatusColors,
  loanStatusLabels,
  money,
  readerStatusColors,
  readerStatusLabels,
} from './labels';
import type {
  ReaderDigitalAccessDto,
  ReaderFineDto,
  ReaderLoanDto,
  ReaderViolationDto,
  ReaderVisitDto,
} from './types';

interface ReaderDetailDrawerProps {
  readerId: string | null;
  onClose: () => void;
  onEdit: (readerId: string) => void;
  onChanged: () => void;
}

/**
 * Hồ sơ một bạn đọc: thông tin, ảnh, lịch sử sử dụng thư viện và các thao tác trên thẻ (VI.1).
 *
 * Mọi thứ cán bộ cần khi bạn đọc đứng trước quầy đều nằm trong một màn hình: thẻ còn hạn không, có
 * đang giữ sách không, có nợ phí không, đã vi phạm gì chưa.
 */
export function ReaderDetailDrawer({
  readerId,
  onClose,
  onEdit,
  onChanged,
}: ReaderDetailDrawerProps) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [photoOpen, setPhotoOpen] = useState(false);
  const [photoStamp, setPhotoStamp] = useState(() => Date.now());
  const [violationOpen, setViolationOpen] = useState(false);
  const [violationForm] = Form.useForm();

  const violationTypes = useCatalogOptions('violation-types', Boolean(readerId));

  const reader = useQuery({
    queryKey: ['reader', readerId],
    queryFn: () => readersApi.get(readerId as string),
    enabled: Boolean(readerId),
  });

  const currentLoans = useQuery({
    queryKey: ['reader-loans', readerId, true],
    queryFn: () => readersApi.loans(readerId as string, { currentOnly: true, pageSize: 100 }),
    enabled: Boolean(readerId),
  });

  const loanHistory = useQuery({
    queryKey: ['reader-loans', readerId, false],
    queryFn: () => readersApi.loans(readerId as string, { pageSize: 100 }),
    enabled: Boolean(readerId),
  });

  const fines = useQuery({
    queryKey: ['reader-fines', readerId],
    queryFn: () => readersApi.fines(readerId as string, { pageSize: 100 }),
    enabled: Boolean(readerId),
  });

  const violations = useQuery({
    queryKey: ['reader-violations', readerId],
    queryFn: () => readersApi.violations(readerId as string, { pageSize: 100 }),
    enabled: Boolean(readerId),
  });

  const visits = useQuery({
    queryKey: ['reader-visits', readerId],
    queryFn: () => readersApi.visits(readerId as string, { pageSize: 100 }),
    enabled: Boolean(readerId),
  });

  const digital = useQuery({
    queryKey: ['reader-digital', readerId],
    queryFn: () => readersApi.digitalAccess(readerId as string, { pageSize: 100 }),
    enabled: Boolean(readerId),
  });

  const clearance = useQuery({
    queryKey: ['reader-clearance', readerId],
    queryFn: () => readersApi.clearance(readerId as string),
    enabled: Boolean(readerId),
  });

  const refreshAll = () => {
    void queryClient.invalidateQueries({ queryKey: ['reader', readerId] });
    void queryClient.invalidateQueries({ queryKey: ['reader-clearance', readerId] });
    onChanged();
  };

  const printCard = useMutation({
    mutationFn: () =>
      readersApi.printCards({
        selection: { readerIds: [readerId] },
        multiplePerPage: false,
      }),
    onSuccess: ({ blob, fileName }) => {
      saveBlob(blob, fileName);
      void queryClient.invalidateQueries({ queryKey: ['reader', readerId] });
      message.success('Đã xuất tệp in thẻ.');
    },
    onError: (error: Error) => message.error(error.message),
  });

  const resetPassword = useMutation({
    mutationFn: () => readersApi.resetPassword(readerId as string),
    onSuccess: (password) => {
      Modal.info({
        title: 'Mật khẩu mới của bạn đọc',
        content: (
          <Space direction="vertical">
            <Typography.Text copyable strong style={{ fontSize: 18 }}>
              {password}
            </Typography.Text>
            <Typography.Text type="secondary">
              Đọc lại cho bạn đọc và nhắc đổi ngay ở lần đăng nhập đầu tiên.
            </Typography.Text>
          </Space>
        ),
      });
      refreshAll();
    },
    onError: (error: Error) => message.error(error.message),
  });

  const saveViolation = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      readersApi.saveViolation(readerId as string, values),
    onSuccess: () => {
      message.success('Đã ghi nhận vi phạm.');
      setViolationOpen(false);
      violationForm.resetFields();
      void queryClient.invalidateQueries({ queryKey: ['reader-violations', readerId] });
    },
    onError: (error: Error) => message.error(error.message),
  });

  const deleteViolation = useMutation({
    mutationFn: (id: string) => readersApi.deleteViolation(id),
    onSuccess: () => {
      message.success('Đã xóa vi phạm.');
      void queryClient.invalidateQueries({ queryKey: ['reader-violations', readerId] });
    },
    onError: (error: Error) => message.error(error.message),
  });

  const detail = reader.data;
  const photo = useReaderPhoto(detail?.id, Boolean(detail?.photoUrl), photoStamp);

  const loanColumns: ColumnsType<ReaderLoanDto> = [
    { title: 'Mã vạch', dataIndex: 'barcode', width: 130 },
    { title: 'Nhan đề', dataIndex: 'title', ellipsis: true },
    { title: 'Ngày mượn', dataIndex: 'loanDate', width: 120, render: formatDate },
    { title: 'Hạn trả', dataIndex: 'dueDate', width: 110, render: formatDate },
    { title: 'Ngày trả', dataIndex: 'returnDate', width: 120, render: formatDate },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      width: 120,
      render: (status: ReaderLoanDto['status'], row) => (
        <Space direction="vertical" size={0}>
          <Tag color={loanStatusColors[status]}>{loanStatusLabels[status]}</Tag>
          {row.overdueDays > 0 && (
            <Typography.Text type="danger" style={{ fontSize: 12 }}>
              Quá {row.overdueDays} ngày
            </Typography.Text>
          )}
        </Space>
      ),
    },
    {
      title: 'Tiền phạt',
      dataIndex: 'fineAmount',
      width: 110,
      align: 'right',
      render: (value: number) => (value > 0 ? money(value) : ''),
    },
  ];

  const fineColumns: ColumnsType<ReaderFineDto> = [
    { title: 'Số biên lai', dataIndex: 'code', width: 130 },
    { title: 'Ngày lập', dataIndex: 'createdAt', width: 130, render: formatDate },
    { title: 'Số tiền', dataIndex: 'amount', width: 110, align: 'right', render: money },
    { title: 'Đã thu', dataIndex: 'paidAmount', width: 110, align: 'right', render: money },
    {
      title: 'Còn nợ',
      dataIndex: 'outstanding',
      width: 110,
      align: 'right',
      render: (value: number) =>
        value > 0 ? <Typography.Text type="danger">{money(value)}</Typography.Text> : money(0),
    },
    {
      title: 'Ghi chú',
      dataIndex: 'note',
      ellipsis: true,
      render: (note: string | null, row) => (row.waived ? <Tag>Đã miễn</Tag> : note),
    },
  ];

  const violationColumns: ColumnsType<ReaderViolationDto> = [
    { title: 'Thời điểm', dataIndex: 'occurredAt', width: 150, render: formatDateTime },
    { title: 'Loại vi phạm', dataIndex: 'violationTypeName', width: 180 },
    { title: 'Mô tả', dataIndex: 'description', ellipsis: true },
    { title: 'Mức phạt', dataIndex: 'fineAmount', width: 110, align: 'right', render: money },
    {
      title: 'Xử lý',
      dataIndex: 'resolvedAt',
      width: 160,
      render: (value: string | null, row) =>
        value ? (
          <Space direction="vertical" size={0}>
            <Tag color="green">Đã xử lý</Tag>
            <Typography.Text style={{ fontSize: 12 }}>{row.resolution}</Typography.Text>
          </Space>
        ) : (
          <Tag color="orange">Chưa xử lý</Tag>
        ),
    },
    {
      title: '',
      width: 70,
      render: (_, row) => (
        <Can permission={PERMISSIONS.reader.violationManage}>
          <Popconfirm
            title="Xóa vi phạm này?"
            okText="Xóa"
            cancelText="Hủy"
            onConfirm={() => deleteViolation.mutate(row.id)}
          >
            <Button type="link" danger size="small">
              Xóa
            </Button>
          </Popconfirm>
        </Can>
      ),
    },
  ];

  const visitColumns: ColumnsType<ReaderVisitDto> = [
    { title: 'Vào lúc', dataIndex: 'checkinAt', width: 170, render: formatDateTime },
    { title: 'Ra lúc', dataIndex: 'checkoutAt', width: 170, render: formatDateTime },
    {
      title: 'Thời lượng',
      dataIndex: 'minutes',
      width: 120,
      render: (minutes: number | null) => (minutes === null ? '' : `${minutes} phút`),
    },
    { title: 'Cổng', dataIndex: 'gate', width: 120 },
    { title: 'Mục đích', dataIndex: 'purpose', ellipsis: true },
  ];

  const digitalColumns: ColumnsType<ReaderDigitalAccessDto> = [
    { title: 'Thời điểm', dataIndex: 'occurredAt', width: 170, render: formatDateTime },
    { title: 'Tài liệu', dataIndex: 'documentTitle', ellipsis: true },
    { title: 'Thao tác', dataIndex: 'action', width: 120 },
    {
      title: 'Thời lượng',
      dataIndex: 'durationSeconds',
      width: 120,
      render: (seconds: number | null) => (seconds ? `${Math.round(seconds / 60)} phút` : ''),
    },
  ];

  return (
    <Drawer
      open={Boolean(readerId)}
      onClose={onClose}
      width={1000}
      title={detail ? `${detail.fullName} — thẻ số ${detail.cardNumber}` : 'Hồ sơ bạn đọc'}
      loading={reader.isLoading}
      extra={
        detail && (
          <Space>
            <Can permission={PERMISSIONS.reader.printCard}>
              <Button
                icon={<PrinterOutlined />}
                loading={printCard.isPending}
                onClick={() => printCard.mutate()}
              >
                In thẻ
              </Button>
            </Can>
            <Can permission={PERMISSIONS.reader.resetPassword}>
              <Button icon={<KeyOutlined />} onClick={() => resetPassword.mutate()}>
                Đặt lại mật khẩu
              </Button>
            </Can>
            <Can permission={PERMISSIONS.reader.update}>
              <Button type="primary" onClick={() => onEdit(detail.id)}>
                Sửa hồ sơ
              </Button>
            </Can>
          </Space>
        )
      }
    >
      {detail && (
        <Tabs
          defaultActiveKey="profile"
          items={[
            {
              key: 'profile',
              label: 'Hồ sơ',
              children: (
                <Space direction="vertical" size={16} style={{ width: '100%' }}>
                  {!detail.canBorrow && (
                    <Alert
                      type="warning"
                      showIcon
                      icon={<WarningOutlined />}
                      message="Bạn đọc hiện không đủ điều kiện mượn tài liệu"
                      description={
                        detail.isExpired
                          ? `Thẻ hết hạn ngày ${formatDate(detail.cardExpireDate)}.`
                          : (detail.statusReason ??
                            `Trạng thái thẻ: ${readerStatusLabels[detail.status]}.`)
                      }
                    />
                  )}

                  <Space align="start" size={24}>
                    <Space direction="vertical" align="center">
                      <Avatar
                        shape="square"
                        size={120}
                        src={photo}
                        style={{ backgroundColor: '#1677ff', fontSize: 36 }}
                      >
                        {initials(detail.fullName)}
                      </Avatar>
                      <Can permission={PERMISSIONS.reader.update}>
                        <Button
                          size="small"
                          icon={<CameraOutlined />}
                          onClick={() => setPhotoOpen(true)}
                        >
                          Đổi ảnh
                        </Button>
                      </Can>
                    </Space>

                    <Descriptions
                      column={2}
                      size="small"
                      bordered
                      style={{ flex: 1 }}
                      items={[
                        { key: 'card', label: 'Số thẻ', children: detail.cardNumber },
                        { key: 'student', label: 'Mã sinh viên', children: detail.studentCode },
                        { key: 'type', label: 'Loại bạn đọc', children: detail.readerTypeName },
                        {
                          key: 'status',
                          label: 'Trạng thái',
                          children: (
                            <Tag color={readerStatusColors[detail.status]}>
                              {readerStatusLabels[detail.status]}
                            </Tag>
                          ),
                        },
                        { key: 'gender', label: 'Giới tính', children: detail.gender },
                        {
                          key: 'dob',
                          label: 'Ngày sinh',
                          children: formatDate(detail.dateOfBirth),
                        },
                        { key: 'faculty', label: 'Khoa', children: detail.facultyName },
                        { key: 'major', label: 'Ngành', children: detail.majorName },
                        { key: 'class', label: 'Lớp', children: detail.className },
                        { key: 'course', label: 'Khóa', children: detail.courseYear },
                        { key: 'email', label: 'Email', children: detail.email },
                        { key: 'phone', label: 'Điện thoại', children: detail.phone },
                        { key: 'cccd', label: 'Số CCCD', children: detail.idCardNumber },
                        {
                          key: 'issue',
                          label: 'Ngày cấp thẻ',
                          children: formatDate(detail.cardIssueDate),
                        },
                        {
                          key: 'expire',
                          label: 'Hạn thẻ',
                          children: (
                            <Space>
                              {formatDate(detail.cardExpireDate)}
                              <Typography.Text type={detail.isExpired ? 'danger' : 'secondary'}>
                                ({describeExpiry(detail.cardExpireDate)})
                              </Typography.Text>
                            </Space>
                          ),
                        },
                        {
                          key: 'deposit',
                          label: 'Tiền đặt cọc',
                          children: `${money(detail.depositAmount)} đ`,
                        },
                        {
                          key: 'address',
                          label: 'Địa chỉ',
                          span: 2,
                          children: detail.address,
                        },
                        { key: 'note', label: 'Ghi chú', span: 2, children: detail.note },
                      ]}
                    />
                  </Space>

                  {clearance.data && (
                    <Card size="small" title="Công nợ với thư viện">
                      {clearance.data.cleared ? (
                        <Alert
                          type="success"
                          showIcon
                          message="Không còn tài liệu chưa trả và không còn nợ phí."
                        />
                      ) : (
                        <Alert
                          type="error"
                          showIcon
                          message="Còn công nợ, chưa xác nhận ra trường được"
                          description={
                            <ul style={{ margin: 0, paddingLeft: 18 }}>
                              {clearance.data.blockers.map((item) => (
                                <li key={item}>{item}</li>
                              ))}
                            </ul>
                          }
                        />
                      )}
                    </Card>
                  )}
                </Space>
              ),
            },
            {
              key: 'cards',
              label: (
                <span>
                  <IdcardOutlined /> Thẻ đã cấp ({detail.cards.length})
                </span>
              ),
              children: (
                <Table
                  rowKey="id"
                  size="small"
                  dataSource={detail.cards}
                  pagination={false}
                  columns={[
                    { title: 'Số thẻ', dataIndex: 'cardNumber', width: 160 },
                    { title: 'Ngày cấp', dataIndex: 'issueDate', width: 120, render: formatDate },
                    { title: 'Hạn thẻ', dataIndex: 'expireDate', width: 120, render: formatDate },
                    { title: 'Số lần in', dataIndex: 'printCount', width: 100, align: 'right' },
                    {
                      title: 'Hiện hành',
                      dataIndex: 'isCurrent',
                      width: 110,
                      render: (value: boolean) =>
                        value ? <Tag color="green">Đang dùng</Tag> : <Tag>Đã thu hồi</Tag>,
                    },
                    { title: 'Lý do cấp lại', dataIndex: 'reissueReason', ellipsis: true },
                  ]}
                />
              ),
            },
            {
              key: 'current',
              label: `Đang mượn (${currentLoans.data?.totalCount ?? 0})`,
              children: (
                <Table
                  rowKey="id"
                  size="small"
                  loading={currentLoans.isLoading}
                  dataSource={currentLoans.data?.items ?? []}
                  columns={loanColumns}
                  pagination={false}
                  locale={{ emptyText: <Empty description="Bạn đọc không giữ tài liệu nào" /> }}
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
                  loading={loanHistory.isLoading}
                  dataSource={loanHistory.data?.items ?? []}
                  columns={loanColumns}
                  pagination={{ pageSize: 20 }}
                  locale={{ emptyText: <Empty description="Chưa có lượt mượn nào" /> }}
                />
              ),
            },
            {
              key: 'fines',
              label: 'Tiền phạt',
              children: (
                <Table
                  rowKey="id"
                  size="small"
                  loading={fines.isLoading}
                  dataSource={fines.data?.items ?? []}
                  columns={fineColumns}
                  pagination={{ pageSize: 20 }}
                  locale={{ emptyText: <Empty description="Không có khoản phạt nào" /> }}
                />
              ),
            },
            {
              key: 'violations',
              label: `Vi phạm (${violations.data?.totalCount ?? 0})`,
              children: (
                <Space direction="vertical" size={12} style={{ width: '100%' }}>
                  <Can permission={PERMISSIONS.reader.violationManage}>
                    <Button onClick={() => setViolationOpen(true)}>Ghi nhận vi phạm</Button>
                  </Can>
                  <Table
                    rowKey="id"
                    size="small"
                    loading={violations.isLoading}
                    dataSource={violations.data?.items ?? []}
                    columns={violationColumns}
                    pagination={false}
                    locale={{ emptyText: <Empty description="Chưa ghi nhận vi phạm nào" /> }}
                  />
                </Space>
              ),
            },
            {
              key: 'visits',
              label: 'Lượt vào thư viện',
              children: (
                <Table
                  rowKey="id"
                  size="small"
                  loading={visits.isLoading}
                  dataSource={visits.data?.items ?? []}
                  columns={visitColumns}
                  pagination={{ pageSize: 20 }}
                  locale={{ emptyText: <Empty description="Chưa có lượt vào thư viện nào" /> }}
                />
              ),
            },
            {
              key: 'digital',
              label: 'Tài liệu số',
              children: (
                <Table
                  rowKey="id"
                  size="small"
                  loading={digital.isLoading}
                  dataSource={digital.data?.items ?? []}
                  columns={digitalColumns}
                  pagination={{ pageSize: 20 }}
                  locale={{ emptyText: <Empty description="Chưa truy cập tài liệu số nào" /> }}
                />
              ),
            },
          ]}
        />
      )}

      {readerId && (
        <ReaderPhotoCapture
          readerId={readerId}
          open={photoOpen}
          onClose={() => setPhotoOpen(false)}
          onSaved={() => {
            setPhotoStamp(Date.now());
            refreshAll();
          }}
        />
      )}

      <Modal
        open={violationOpen}
        title="Ghi nhận vi phạm"
        okText="Lưu"
        cancelText="Hủy"
        confirmLoading={saveViolation.isPending}
        onCancel={() => setViolationOpen(false)}
        onOk={() => {
          violationForm
            .validateFields()
            .then((values) => saveViolation.mutate(values))
            .catch(() => undefined);
        }}
      >
        <Form form={violationForm} layout="vertical">
          <Form.Item name="violationTypeId" label="Loại vi phạm">
            <Select
              allowClear
              showSearch
              optionFilterProp="label"
              placeholder="Chọn loại vi phạm"
              options={toOptions(violationTypes.data)}
            />
          </Form.Item>
          <Form.Item name="description" label="Mô tả">
            <Input.TextArea rows={3} placeholder="Vi phạm cụ thể là gì" />
          </Form.Item>
          <Form.Item
            name="fineAmount"
            label="Mức phạt (đ)"
            extra="Bỏ trống thì lấy mức mặc định của loại vi phạm."
          >
            <InputNumber<number> min={0} step={1000} style={{ width: '100%' }} />
          </Form.Item>
        </Form>
      </Modal>
    </Drawer>
  );
}
