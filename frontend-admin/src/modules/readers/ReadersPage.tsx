import { useState } from 'react';
import {
  App,
  Alert,
  AutoComplete,
  Avatar,
  Button,
  Checkbox,
  Col,
  DatePicker,
  Drawer,
  Dropdown,
  Form,
  Input,
  InputNumber,
  Modal,
  Row,
  Select,
  Space,
  Table,
  Tag,
  Typography,
} from 'antd';
import {
  DownOutlined,
  ExportOutlined,
  LockOutlined,
  PlusOutlined,
  PrinterOutlined,
  UnlockOutlined,
} from '@ant-design/icons';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import dayjs, { type Dayjs } from 'dayjs';
import { PageHeader } from '@/components/PageHeader';
import { FilterBar } from '@/components/FilterBar';
import { Can } from '@/components/PermissionGate';
import { usePermission } from '@/hooks/usePermission';
import { PERMISSIONS } from '@/api/permissions';
import { saveBlob } from '@/modules/marc/api';
import { useCatalogOptions, toOptions } from '@/modules/cataloging/useCatalogOptions';
import { readersApi } from './api';
import { ReaderDetailDrawer } from './ReaderDetailDrawer';
import { useReaderPhoto } from './useReaderPhoto';
import { MAU } from '@/lib/palette';
import {
  describeExpiry,
  formatDate,
  genderOptions,
  initials,
  money,
  readerStatusColors,
  readerStatusLabels,
} from './labels';
import type {
  BulkResultDto,
  ReaderCardTemplateDto,
  ReaderDto,
  ReaderFilter,
  ReaderStatus,
} from './types';

type BulkAction = 'extend' | 'lock' | 'unlock' | 'graduate' | 'print';

const actionTitles: Record<BulkAction, string> = {
  extend: 'Gia hạn thẻ',
  lock: 'Tạm khóa thẻ',
  unlock: 'Mở khóa thẻ',
  graduate: 'Chuyển trạng thái ra trường',
  print: 'In thẻ bạn đọc',
};

const statusOptions = (Object.keys(readerStatusLabels) as ReaderStatus[]).map((status) => ({
  value: status,
  label: readerStatusLabels[status],
}));

/**
 * VI.1 — Quản lý hồ sơ bạn đọc.
 *
 * Danh sách là nơi cán bộ làm gần hết công việc của phân hệ: tra một người khi họ đứng ở quầy, và
 * xử lý cả khóa khi vào đầu năm học. Vì vậy mọi thao tác đều làm được cho một người hoặc cho toàn bộ
 * kết quả lọc, và ô tìm kiếm chấp nhận số thẻ, mã sinh viên, họ tên gõ không dấu, CCCD, email hay
 * số điện thoại — cán bộ gõ cái gì có trong tay chứ không chọn trước là tìm theo trường nào.
 */
export function ReadersPage() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();
  const { can } = usePermission();

  const [filter, setFilter] = useState<ReaderFilter>({});
  const [draft, setDraft] = useState<ReaderFilter>({});
  const [page, setPage] = useState<{
    page: number;
    pageSize: number;
    sortBy?: string;
    sortDescending?: boolean;
  }>({ page: 1, pageSize: 20 });

  const [selected, setSelected] = useState<string[]>([]);
  /** Áp dụng thao tác cho toàn bộ kết quả lọc thay vì chỉ các dòng đã tick. */
  const [applyToAll, setApplyToAll] = useState(false);

  const [action, setAction] = useState<BulkAction | null>(null);
  const [actionForm] = Form.useForm();

  const [editing, setEditing] = useState<{ id: string | null } | null>(null);
  const [editForm] = Form.useForm();
  const [detailId, setDetailId] = useState<string | null>(null);

  const readerTypes = useCatalogOptions('reader-types');
  const faculties = useCatalogOptions('faculties');
  const majors = useCatalogOptions('majors');
  const cohorts = useCatalogOptions('cohorts');
  const classes = useCatalogOptions('student-classes');

  const templates = useQuery({
    queryKey: ['reader-card-templates'],
    queryFn: () => readersApi.cardTemplates(),
    enabled: can(PERMISSIONS.reader.printCard),
  });

  const readers = useQuery({
    queryKey: ['readers', page, filter],
    queryFn: () => readersApi.search({ ...page, ...filter }),
    placeholderData: keepPreviousData,
  });

  const selection = () =>
    applyToAll ? { useFilter: true, filter } : { readerIds: selected };

  const affectedCount = applyToAll ? (readers.data?.totalCount ?? 0) : selected.length;

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ['readers'] });
    setSelected([]);
    setApplyToAll(false);
  };

  const reportResult = (result: BulkResultDto) => {
    if (result.skipped === 0) {
      message.success(`Đã xử lý ${result.succeeded} bạn đọc.`);
      return;
    }

    modal.info({
      title: `Đã xử lý ${result.succeeded}/${result.total} bạn đọc`,
      width: 640,
      content: (
        <Space direction="vertical" style={{ width: '100%' }}>
          <Typography.Text>
            {result.skipped} trường hợp chưa xử lý được, lý do như sau:
          </Typography.Text>
          <Table
            rowKey="readerId"
            size="small"
            pagination={{ pageSize: 8 }}
            dataSource={result.skips}
            columns={[
              { title: 'Số thẻ', dataIndex: 'cardNumber', width: 120 },
              { title: 'Họ và tên', dataIndex: 'fullName', width: 180 },
              { title: 'Lý do', dataIndex: 'reason' },
            ]}
          />
        </Space>
      ),
    });
  };

  const runAction = useMutation({
    mutationFn: async (values: Record<string, unknown>) => {
      const payload = { selection: selection(), ...values };

      switch (action) {
        case 'extend':
          return readersApi.extendCards(payload);
        case 'lock':
          return readersApi.setLock({ ...payload, locked: true });
        case 'unlock':
          return readersApi.setLock({ ...payload, locked: false });
        case 'graduate':
          return readersApi.graduate(payload);
        default:
          throw new Error('Chưa chọn thao tác.');
      }
    },
    onSuccess: (result) => {
      setAction(null);
      actionForm.resetFields();
      invalidate();
      reportResult(result);
    },
    onError: (error: Error) => message.error(error.message),
  });

  const printCards = useMutation({
    mutationFn: (values: { templateId?: string; multiplePerPage: boolean }) =>
      readersApi.printCards({ selection: selection(), ...values }),
    onSuccess: ({ blob, fileName }) => {
      saveBlob(blob, fileName);
      setAction(null);
      actionForm.resetFields();
      invalidate();
      message.success('Đã xuất tệp in thẻ.');
    },
    onError: (error: Error) => message.error(error.message),
  });

  const saveReader = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      readersApi.save(editing?.id ?? null, values),
    onSuccess: () => {
      message.success('Đã lưu hồ sơ bạn đọc.');
      setEditing(null);
      editForm.resetFields();
      void queryClient.invalidateQueries({ queryKey: ['readers'] });
      void queryClient.invalidateQueries({ queryKey: ['reader'] });
    },
    onError: (error: Error) => message.error(error.message),
  });

  const removeReader = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => readersApi.remove(id, reason),
    onSuccess: () => {
      message.success('Đã xóa hồ sơ bạn đọc.');
      setDetailId(null);
      void queryClient.invalidateQueries({ queryKey: ['readers'] });
    },
    onError: (error: Error) => message.error(error.message),
  });

  const exportList = useMutation({
    mutationFn: () => readersApi.export(filter as Record<string, unknown>),
    onSuccess: ({ blob, fileName }) => {
      saveBlob(blob, fileName);
      message.success('Đã xuất danh sách bạn đọc.');
    },
    onError: (error: Error) => message.error(error.message),
  });

  const openEditor = async (id: string | null) => {
    setEditing({ id });
    editForm.resetFields();

    if (!id) {
      return;
    }

    const detail = await readersApi.get(id);

    editForm.setFieldsValue({
      ...detail,
      dateOfBirth: detail.dateOfBirth ? dayjsOrNull(detail.dateOfBirth) : null,
      cardIssueDate: dayjsOrNull(detail.cardIssueDate),
      cardExpireDate: dayjsOrNull(detail.cardExpireDate),
    });
  };

  const columns: ColumnsType<ReaderDto> = [
    {
      title: 'Bạn đọc',
      dataIndex: 'fullName',
      sorter: true,
      render: (_, row) => (
        <Space>
          <ReaderAvatar reader={row} />
          <Space direction="vertical" size={0}>
            <Button type="link" style={{ padding: 0 }} onClick={() => setDetailId(row.id)}>
              {row.fullName}
            </Button>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              {row.cardNumber}
              {row.studentCode ? ` · ${row.studentCode}` : ''}
            </Typography.Text>
          </Space>
        </Space>
      ),
    },
    { title: 'Loại bạn đọc', dataIndex: 'readerTypeName', width: 130 },
    {
      title: 'Đơn vị',
      dataIndex: 'facultyName',
      width: 200,
      render: (_, row) => (
        <Space direction="vertical" size={0}>
          <span>{row.facultyName}</span>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {[row.className, row.courseYear].filter(Boolean).join(' · ')}
          </Typography.Text>
        </Space>
      ),
    },
    {
      title: 'Hạn thẻ',
      dataIndex: 'cardExpireDate',
      width: 150,
      sorter: true,
      render: (value: string, row) => (
        <Space direction="vertical" size={0}>
          <span>{formatDate(value)}</span>
          <Typography.Text
            type={row.isExpired ? 'danger' : row.isExpiringSoon ? 'warning' : 'secondary'}
            style={{ fontSize: 12 }}
          >
            {describeExpiry(value)}
          </Typography.Text>
        </Space>
      ),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      width: 120,
      sorter: true,
      render: (status: ReaderStatus) => (
        <Tag color={readerStatusColors[status]}>{readerStatusLabels[status]}</Tag>
      ),
    },
    {
      title: 'Đang mượn',
      dataIndex: 'currentLoanCount',
      width: 100,
      align: 'right',
    },
    {
      title: 'Còn nợ',
      dataIndex: 'debtAmount',
      width: 110,
      align: 'right',
      render: (value: number) =>
        value > 0 ? <Typography.Text type="danger">{money(value)}</Typography.Text> : '',
    },
  ];

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Bạn đọc"
        description="Hồ sơ bạn đọc, thẻ thư viện, lịch sử sử dụng và các thao tác theo lớp, theo khóa."
        actions={
          <Space>
            <Can permission={PERMISSIONS.reader.export}>
              <Button
                icon={<ExportOutlined />}
                loading={exportList.isPending}
                onClick={() => exportList.mutate()}
              >
                Xuất Excel
              </Button>
            </Can>
            <Can permission={PERMISSIONS.reader.create}>
              <Button type="primary" icon={<PlusOutlined />} onClick={() => void openEditor(null)}>
                Thêm bạn đọc
              </Button>
            </Can>
          </Space>
        }
      />

      <FilterBar
        loading={readers.isFetching}
        onSearch={() => {
          setFilter(draft);
          setPage((current) => ({ ...current, page: 1 }));
        }}
        onReset={() => {
          setDraft({});
          setFilter({});
          setPage((current) => ({ ...current, page: 1 }));
        }}
      >
        <Input
          allowClear
          style={{ width: 260 }}
          placeholder="Số thẻ, mã SV, họ tên, CCCD, email…"
          value={draft.keyword}
          onChange={(event) => setDraft({ ...draft, keyword: event.target.value })}
        />
        <Select
          allowClear
          style={{ width: 160 }}
          placeholder="Loại bạn đọc"
          options={toOptions(readerTypes.data)}
          value={draft.readerTypeId}
          onChange={(value) => setDraft({ ...draft, readerTypeId: value })}
        />
        <Select
          allowClear
          showSearch
          optionFilterProp="label"
          style={{ width: 200 }}
          placeholder="Khoa"
          options={toOptions(faculties.data)}
          value={draft.facultyId}
          onChange={(value) => setDraft({ ...draft, facultyId: value })}
        />
        <AutoComplete
          allowClear
          style={{ width: 130 }}
          placeholder="Lớp"
          options={(classes.data ?? []).map((item) => ({ value: item.code }))}
          filterOption={(input, option) =>
            (option?.value ?? '').toLowerCase().includes(input.toLowerCase())
          }
          value={draft.className}
          onChange={(value) => setDraft({ ...draft, className: value })}
        />
        <AutoComplete
          allowClear
          style={{ width: 110 }}
          placeholder="Khóa"
          options={(cohorts.data ?? []).map((item) => ({ value: item.code }))}
          value={draft.courseYear}
          onChange={(value) => setDraft({ ...draft, courseYear: value })}
        />
        <Select
          allowClear
          style={{ width: 150 }}
          placeholder="Trạng thái thẻ"
          options={statusOptions}
          value={draft.status}
          onChange={(value) => setDraft({ ...draft, status: value })}
        />
        <Select
          allowClear
          style={{ width: 190 }}
          placeholder="Tình trạng"
          value={
            draft.expired
              ? 'expired'
              : draft.expiringInDays
                ? 'expiring'
                : draft.hasDebt
                  ? 'debt'
                  : draft.borrowing
                    ? 'borrowing'
                    : draft.neverBorrowed
                      ? 'never'
                      : undefined
          }
          options={[
            { value: 'expired', label: 'Thẻ đã hết hạn' },
            { value: 'expiring', label: 'Thẻ hết hạn trong 30 ngày' },
            { value: 'debt', label: 'Còn nợ tiền phạt' },
            { value: 'borrowing', label: 'Đang giữ tài liệu' },
            { value: 'never', label: 'Chưa từng mượn' },
          ]}
          onChange={(value) =>
            setDraft({
              ...draft,
              expired: value === 'expired' ? true : undefined,
              expiringInDays: value === 'expiring' ? 30 : undefined,
              hasDebt: value === 'debt' ? true : undefined,
              borrowing: value === 'borrowing' ? true : undefined,
              neverBorrowed: value === 'never' ? true : undefined,
            })
          }
        />
      </FilterBar>

      {selected.length > 0 && (
        <Alert
          type="info"
          showIcon
          message={
            <Space wrap>
              <span>
                Đã chọn <strong>{selected.length}</strong> bạn đọc.
              </span>
              <Checkbox checked={applyToAll} onChange={(event) => setApplyToAll(event.target.checked)}>
                Áp dụng cho toàn bộ {readers.data?.totalCount ?? 0} kết quả của bộ lọc
              </Checkbox>
              <Dropdown
                menu={{
                  items: [
                    {
                      key: 'extend',
                      label: actionTitles.extend,
                      disabled: !can(PERMISSIONS.reader.extendCard),
                    },
                    {
                      key: 'lock',
                      label: actionTitles.lock,
                      icon: <LockOutlined />,
                      disabled: !can(PERMISSIONS.reader.lock),
                    },
                    {
                      key: 'unlock',
                      label: actionTitles.unlock,
                      icon: <UnlockOutlined />,
                      disabled: !can(PERMISSIONS.reader.lock),
                    },
                    {
                      key: 'graduate',
                      label: actionTitles.graduate,
                      disabled: !can(PERMISSIONS.reader.update),
                    },
                    { type: 'divider' },
                    {
                      key: 'print',
                      label: actionTitles.print,
                      icon: <PrinterOutlined />,
                      disabled: !can(PERMISSIONS.reader.printCard),
                    },
                  ],
                  onClick: ({ key }) => {
                    setAction(key as BulkAction);
                    actionForm.resetFields();
                  },
                }}
              >
                <Button type="primary">
                  Thao tác hàng loạt <DownOutlined />
                </Button>
              </Dropdown>
            </Space>
          }
        />
      )}

      <Table
        rowKey="id"
        size="small"
        loading={readers.isFetching}
        dataSource={readers.data?.items ?? []}
        columns={columns}
        rowSelection={{
          selectedRowKeys: selected,
          onChange: (keys) => {
            setSelected(keys as string[]);
            if (keys.length === 0) setApplyToAll(false);
          },
        }}
        pagination={{
          current: readers.data?.page ?? 1,
          pageSize: readers.data?.pageSize ?? 20,
          total: readers.data?.totalCount ?? 0,
          showSizeChanger: true,
          showTotal: (total) => `Tổng ${total.toLocaleString('vi-VN')} bạn đọc`,
        }}
        onChange={(pagination, _filters, sorter) => {
          const single = Array.isArray(sorter) ? sorter[0] : sorter;

          setPage({
            page: pagination.current ?? 1,
            pageSize: pagination.pageSize ?? 20,
            sortBy: single?.order ? String(single.field) : undefined,
            sortDescending: single?.order === 'descend',
          });
        }}
      />

      {/* Thao tác hàng loạt */}
      <Modal
        open={action !== null}
        title={action ? `${actionTitles[action]} — ${affectedCount} bạn đọc` : ''}
        okText="Thực hiện"
        cancelText="Hủy"
        confirmLoading={runAction.isPending || printCards.isPending}
        onCancel={() => setAction(null)}
        onOk={() => {
          actionForm
            .validateFields()
            .then((values) => {
              if (action === 'print') {
                printCards.mutate({
                  templateId: values.templateId,
                  multiplePerPage: values.multiplePerPage ?? true,
                });
                return;
              }

              runAction.mutate({
                ...values,
                newExpireDate: values.newExpireDate
                  ? (values.newExpireDate as Dayjs).format('YYYY-MM-DD')
                  : undefined,
                lockedUntil: values.lockedUntil
                  ? (values.lockedUntil as Dayjs).format('YYYY-MM-DD')
                  : undefined,
              });
            })
            .catch(() => undefined);
        }}
      >
        <Form form={actionForm} layout="vertical">
          {action === 'extend' && (
            <>
              <Form.Item
                name="months"
                label="Số tháng gia hạn"
                initialValue={12}
                extra="Thẻ còn hạn thì cộng tiếp vào hạn cũ; thẻ đã hết hạn thì tính từ hôm nay."
              >
                <InputNumber<number> min={1} max={120} style={{ width: '100%' }} />
              </Form.Item>
              <Form.Item
                name="newExpireDate"
                label="Hoặc đặt thẳng ngày hết hạn mới"
                extra="Dùng khi nhà trường chốt một mốc chung cho cả khóa."
              >
                <DatePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
              </Form.Item>
              <Form.Item name="note" label="Ghi chú">
                <Input placeholder="Ví dụ: gia hạn theo quyết định số 123/QĐ" />
              </Form.Item>
            </>
          )}

          {action === 'lock' && (
            <>
              <Form.Item
                name="reason"
                label="Lý do tạm khóa"
                rules={[{ required: true, message: 'Phải ghi lý do tạm khóa thẻ.' }]}
              >
                <Input.TextArea rows={3} placeholder="Vì sao khóa thẻ bạn đọc này" />
              </Form.Item>
              <Form.Item name="lockedUntil" label="Khóa đến ngày" extra="Bỏ trống là khóa đến khi mở lại.">
                <DatePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
              </Form.Item>
            </>
          )}

          {action === 'unlock' && (
            <Form.Item name="reason" label="Ghi chú khi mở khóa">
              <Input.TextArea rows={2} placeholder="Ví dụ: đã bồi thường xong" />
            </Form.Item>
          )}

          {action === 'graduate' && (
            <>
              <Alert
                type="warning"
                showIcon
                style={{ marginBottom: 12 }}
                message="Bạn đọc còn giữ tài liệu hoặc còn nợ tiền phạt sẽ được giữ lại kèm lý do."
              />
              <Form.Item name="note" label="Ghi chú">
                <Input placeholder="Ví dụ: tốt nghiệp đợt tháng 6/2026" />
              </Form.Item>
            </>
          )}

          {action === 'print' && (
            <>
              <Form.Item name="templateId" label="Mẫu thẻ">
                <Select
                  allowClear
                  placeholder="Mẫu mặc định"
                  options={(templates.data ?? []).map((template: ReaderCardTemplateDto) => ({
                    value: template.id,
                    label: `${template.name}${template.isDefault ? ' (mặc định)' : ''}`,
                  }))}
                />
              </Form.Item>
              <Form.Item name="multiplePerPage" valuePropName="checked" initialValue>
                <Checkbox>
                  Xếp nhiều thẻ trên tờ A4 (bỏ chọn nếu in thẳng lên phôi thẻ nhựa)
                </Checkbox>
              </Form.Item>
            </>
          )}
        </Form>
      </Modal>

      {/* Thêm / sửa hồ sơ */}
      <Drawer
        open={editing !== null}
        width={720}
        title={editing?.id ? 'Sửa hồ sơ bạn đọc' : 'Thêm bạn đọc'}
        onClose={() => setEditing(null)}
        extra={
          <Space>
            {editing?.id && (
              <Can permission={PERMISSIONS.reader.delete}>
                <Button
                  danger
                  onClick={() => {
                    let reason = '';

                    modal.confirm({
                      title: 'Xóa hồ sơ bạn đọc',
                      content: (
                        <Space direction="vertical" style={{ width: '100%' }}>
                          <Typography.Text>
                            Hồ sơ chỉ xóa được khi bạn đọc không còn tài liệu chưa trả và không còn
                            nợ phí. Dữ liệu được lưu lại để tra cứu về sau.
                          </Typography.Text>
                          <Input.TextArea
                            rows={2}
                            placeholder="Lý do xóa"
                            onChange={(event) => {
                              reason = event.target.value;
                            }}
                          />
                        </Space>
                      ),
                      okText: 'Xóa hồ sơ',
                      okButtonProps: { danger: true },
                      cancelText: 'Hủy',
                      onOk: () => removeReader.mutateAsync({ id: editing.id as string, reason }),
                    });
                  }}
                >
                  Xóa hồ sơ
                </Button>
              </Can>
            )}
            <Button
              type="primary"
              loading={saveReader.isPending}
              onClick={() => {
                editForm
                  .validateFields()
                  .then((values) =>
                    saveReader.mutate({
                      ...values,
                      dateOfBirth: values.dateOfBirth
                        ? (values.dateOfBirth as Dayjs).format('YYYY-MM-DD')
                        : null,
                      cardIssueDate: values.cardIssueDate
                        ? (values.cardIssueDate as Dayjs).format('YYYY-MM-DD')
                        : null,
                      cardExpireDate: values.cardExpireDate
                        ? (values.cardExpireDate as Dayjs).format('YYYY-MM-DD')
                        : null,
                    }),
                  )
                  .catch(() => undefined);
              }}
            >
              Lưu
            </Button>
          </Space>
        }
      >
        <Form form={editForm} layout="vertical">
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item
                name="fullName"
                label="Họ và tên"
                rules={[{ required: true, message: 'Chưa nhập họ và tên.' }]}
              >
                <Input placeholder="Nguyễn Văn An" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="studentCode" label="Mã sinh viên / mã cán bộ">
                <Input placeholder="2151010101" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                name="cardNumber"
                label="Số thẻ"
                extra={editing?.id ? undefined : 'Bỏ trống thì hệ thống tự sinh.'}
              >
                <Input placeholder="Tự sinh" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                name="readerTypeId"
                label="Loại bạn đọc"
                rules={[{ required: true, message: 'Chưa chọn loại bạn đọc.' }]}
              >
                <Select options={toOptions(readerTypes.data)} placeholder="Sinh viên" />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="gender" label="Giới tính">
                <Select allowClear options={genderOptions} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="dateOfBirth" label="Ngày sinh">
                <DatePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="idCardNumber" label="Số CCCD">
                <Input />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="email" label="Email">
                <Input placeholder="an.nv@sinhvien.edu.vn" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="phone" label="Điện thoại">
                <Input placeholder="0901234567" />
              </Form.Item>
            </Col>
            <Col span={24}>
              <Form.Item name="address" label="Địa chỉ">
                <Input />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="facultyId" label="Khoa">
                <Select
                  allowClear
                  showSearch
                  optionFilterProp="label"
                  options={toOptions(faculties.data)}
                />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="majorId" label="Ngành đào tạo">
                <Select
                  allowClear
                  showSearch
                  optionFilterProp="label"
                  options={toOptions(majors.data)}
                />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="className" label="Lớp">
                <AutoComplete
                  allowClear
                  placeholder="DH21TH1"
                  options={(classes.data ?? []).map((item) => ({ value: item.code }))}
                />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="courseYear" label="Khóa">
                <AutoComplete
                  allowClear
                  placeholder="K21"
                  options={(cohorts.data ?? []).map((item) => ({ value: item.code }))}
                />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="cardIssueDate" label="Ngày cấp thẻ">
                <DatePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item
                name="cardExpireDate"
                label="Ngày hết hạn"
                extra="Bỏ trống thì tính theo hạn thẻ của loại bạn đọc."
              >
                <DatePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="depositAmount" label="Tiền đặt cọc (đ)" initialValue={0}>
                <InputNumber<number> min={0} step={10000} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            {editing?.id && (
              <Col span={12}>
                <Form.Item name="status" label="Trạng thái thẻ">
                  <Select options={statusOptions} />
                </Form.Item>
              </Col>
            )}
            <Col span={24}>
              <Form.Item name="note" label="Ghi chú">
                <Input.TextArea rows={2} />
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Drawer>

      <ReaderDetailDrawer
        readerId={detailId}
        onClose={() => setDetailId(null)}
        onEdit={(id) => {
          setDetailId(null);
          void openEditor(id);
        }}
        onChanged={() => void queryClient.invalidateQueries({ queryKey: ['readers'] })}
      />
    </Space>
  );
}

/** Ô ảnh của một dòng danh sách; chưa có ảnh thì hiện chữ cái viết tắt của tên. */
function ReaderAvatar({ reader }: { reader: ReaderDto }) {
  const photo = useReaderPhoto(reader.id, Boolean(reader.photoUrl));

  return (
    <Avatar shape="square" size={40} src={photo} style={{ backgroundColor: MAU.chinh }}>
      {initials(reader.fullName)}
    </Avatar>
  );
}

/** Chuỗi ngày ISO thành Dayjs cho ô chọn ngày; null khi không có giá trị. */
function dayjsOrNull(value: string | null): Dayjs | null {
  return value ? dayjs(value) : null;
}
