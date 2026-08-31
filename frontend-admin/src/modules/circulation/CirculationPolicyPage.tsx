import { useState } from 'react';
import {
  App,
  Button,
  Card,
  Checkbox,
  Col,
  DatePicker,
  Drawer,
  Empty,
  Form,
  Input,
  InputNumber,
  Popconfirm,
  Row,
  Select,
  Space,
  Table,
  Tag,
  Typography,
} from 'antd';
import { CalendarOutlined, DeleteOutlined, PlusOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import dayjs, { type Dayjs } from 'dayjs';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { useCatalogOptions, toOptions } from '@/modules/cataloging/useCatalogOptions';
import { locationsApi } from '@/modules/acquisition/api';
import { circulationApi } from './api';
import { formatDate, money } from './labels';
import type { CirculationPolicyDto, DueDatePreviewDto, HolidayDto } from './types';

/**
 * VII.1 — Chính sách lưu thông và lịch nghỉ.
 *
 * Ma trận loại bạn đọc × dạng tài liệu × kho quyết định toàn bộ cách hệ thống cư xử ở quầy, nên màn
 * hình có sẵn ô thử: chọn một cặp bất kỳ và xem ô nào thắng trước khi cho cán bộ dùng thật.
 */
export function CirculationPolicyPage() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [editing, setEditing] = useState<{ policy: CirculationPolicyDto | null } | null>(null);
  const [form] = Form.useForm();
  const [holidayForm] = Form.useForm();
  const [holidayOpen, setHolidayOpen] = useState(false);
  const [preview, setPreview] = useState<DueDatePreviewDto | null>(null);
  const [probe, setProbe] = useState<{ readerTypeId?: string; documentTypeId?: string; warehouseId?: string }>({});

  const readerTypes = useCatalogOptions('reader-types');
  const documentTypes = useCatalogOptions('document-types');
  const warehouses = useQuery({
    queryKey: ['acq-warehouses', null],
    queryFn: () => locationsApi.warehouses(),
  });

  const policies = useQuery({
    queryKey: ['circulation-policies'],
    queryFn: () => circulationApi.policies(true),
  });

  const holidays = useQuery({
    queryKey: ['circulation-holidays'],
    queryFn: () => circulationApi.holidays(),
  });

  const effective = useQuery({
    queryKey: ['circulation-policy-preview', probe],
    queryFn: () => circulationApi.previewPolicy(probe),
  });

  const savePolicy = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      circulationApi.savePolicy({ ...values, id: editing?.policy?.id }),
    onSuccess: () => {
      message.success('Đã lưu chính sách.');
      setEditing(null);
      void queryClient.invalidateQueries({ queryKey: ['circulation-policies'] });
      void queryClient.invalidateQueries({ queryKey: ['circulation-policy-preview'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  const deletePolicy = useMutation({
    mutationFn: (id: string) => circulationApi.deletePolicy(id),
    onSuccess: () => {
      message.success('Đã xóa chính sách.');
      void queryClient.invalidateQueries({ queryKey: ['circulation-policies'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xóa được.'),
  });

  const saveHoliday = useMutation({
    mutationFn: (values: Record<string, unknown>) => circulationApi.saveHoliday(values),
    onSuccess: () => {
      message.success('Đã lưu ngày nghỉ.');
      setHolidayOpen(false);
      holidayForm.resetFields();
      void queryClient.invalidateQueries({ queryKey: ['circulation-holidays'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  const deleteHoliday = useMutation({
    mutationFn: (id: string) => circulationApi.deleteHoliday(id),
    onSuccess: () => {
      message.success('Đã xóa ngày nghỉ.');
      void queryClient.invalidateQueries({ queryKey: ['circulation-holidays'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xóa được.'),
  });

  const testDueDate = useMutation({
    mutationFn: ({ date, days }: { date: string; days: number }) =>
      circulationApi.previewDueDate(date, days),
    onSuccess: setPreview,
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không thử được.'),
  });

  const columns: ColumnsType<CirculationPolicyDto> = [
    {
      title: 'Chính sách',
      dataIndex: 'name',
      render: (name: string, row) => (
        <Space direction="vertical" size={0}>
          <Button
            type="link"
            style={{ padding: 0, height: 'auto' }}
            onClick={() => {
              setEditing({ policy: row });
              form.setFieldsValue(row);
            }}
          >
            {name}
          </Button>
          {!row.isActive && <Tag>Ngừng áp dụng</Tag>}
        </Space>
      ),
    },
    {
      title: 'Áp dụng cho',
      width: 320,
      render: (_, row) => (
        <Space direction="vertical" size={0}>
          <span>Bạn đọc: {row.readerTypeName ?? 'mọi loại'}</span>
          <span>Tài liệu: {row.documentTypeName ?? 'mọi dạng'}</span>
          <span>Kho: {row.warehouseName ?? 'mọi kho'}</span>
        </Space>
      ),
    },
    { title: 'Số bản', dataIndex: 'maxItems', width: 90, align: 'right' },
    { title: 'Số ngày mượn', dataIndex: 'loanDays', width: 120, align: 'right' },
    {
      title: 'Gia hạn',
      width: 130,
      align: 'right',
      render: (_, row) => `${row.maxRenewals} lần × ${row.renewalDays} ngày`,
    },
    {
      title: 'Phạt / ngày',
      dataIndex: 'finePerDay',
      width: 120,
      align: 'right',
      render: (value: number) => money(value),
    },
    { title: 'Ân hạn', dataIndex: 'graceDays', width: 90, align: 'right' },
    {
      title: 'Cho phép',
      width: 200,
      render: (_, row) => (
        <Space size={4} wrap>
          {row.allowLoan && <Tag color="green">Mượn</Tag>}
          {row.allowTakeHome ? <Tag color="blue">Về nhà</Tag> : <Tag>Tại chỗ</Tag>}
          {row.allowRenew && <Tag color="cyan">Gia hạn</Tag>}
          {row.allowHold && <Tag color="purple">Đặt giữ</Tag>}
        </Space>
      ),
    },
    { title: 'Ưu tiên', dataIndex: 'priority', width: 90, align: 'right' },
    {
      title: '',
      width: 80,
      render: (_, row) => (
        <Can permission={PERMISSIONS.circulation.policyManage}>
          <Popconfirm
            title="Xóa chính sách này?"
            okText="Xóa"
            cancelText="Hủy"
            onConfirm={() => deletePolicy.mutate(row.id)}
          >
            <Button type="link" danger size="small" icon={<DeleteOutlined />} />
          </Popconfirm>
        </Can>
      ),
    },
  ];

  const holidayColumns: ColumnsType<HolidayDto> = [
    { title: 'Tên kỳ nghỉ', dataIndex: 'name' },
    { title: 'Từ ngày', dataIndex: 'fromDate', width: 130, render: formatDate },
    { title: 'Đến ngày', dataIndex: 'toDate', width: 130, render: formatDate },
    {
      title: 'Lặp hằng năm',
      dataIndex: 'isRecurringYearly',
      width: 130,
      render: (value: boolean) => (value ? <Tag color="blue">Có</Tag> : <Tag>Không</Tag>),
    },
    {
      title: '',
      width: 70,
      render: (_, row) => (
        <Can permission={PERMISSIONS.circulation.policyManage}>
          <Popconfirm
            title="Xóa ngày nghỉ này?"
            okText="Xóa"
            cancelText="Hủy"
            onConfirm={() => deleteHoliday.mutate(row.id)}
          >
            <Button type="link" danger size="small" icon={<DeleteOutlined />} />
          </Popconfirm>
        </Can>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Chính sách lưu thông"
        description="Ma trận loại bạn đọc × dạng tài liệu × kho, kèm lịch nghỉ dùng để dời hạn trả và trừ ngày phạt."
        actions={
          <Can permission={PERMISSIONS.circulation.policyManage}>
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => {
                setEditing({ policy: null });
                form.resetFields();
              }}
            >
              Thêm chính sách
            </Button>
          </Can>
        }
      />

      <Card size="small" title="Thử ma trận: cặp này rơi vào chính sách nào?">
        <Space wrap>
          <Select
            allowClear
            style={{ width: 200 }}
            placeholder="Loại bạn đọc"
            options={toOptions(readerTypes.data)}
            value={probe.readerTypeId}
            onChange={(value) => setProbe({ ...probe, readerTypeId: value })}
          />
          <Select
            allowClear
            style={{ width: 200 }}
            placeholder="Dạng tài liệu"
            options={toOptions(documentTypes.data)}
            value={probe.documentTypeId}
            onChange={(value) => setProbe({ ...probe, documentTypeId: value })}
          />
          <Select
            allowClear
            style={{ width: 200 }}
            placeholder="Kho"
            options={(warehouses.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
            value={probe.warehouseId}
            onChange={(value) => setProbe({ ...probe, warehouseId: value })}
          />

          {effective.data && (
            <Space wrap>
              <Tag color="blue">{effective.data.name}</Tag>
              <span>
                {effective.data.maxItems} bản · {effective.data.loanDays} ngày ·{' '}
                gia hạn {effective.data.maxRenewals} lần · phạt {money(effective.data.finePerDay)} đ/ngày
              </span>
            </Space>
          )}
        </Space>
      </Card>

      <Table
        rowKey="id"
        size="small"
        loading={policies.isLoading}
        dataSource={policies.data ?? []}
        columns={columns}
        pagination={false}
        locale={{ emptyText: <Empty description="Chưa khai chính sách nào" /> }}
      />

      <Card
        size="small"
        title={
          <Space>
            <CalendarOutlined />
            Lịch nghỉ
          </Space>
        }
        extra={
          <Can permission={PERMISSIONS.circulation.policyManage}>
            <Button size="small" icon={<PlusOutlined />} onClick={() => setHolidayOpen(true)}>
              Thêm ngày nghỉ
            </Button>
          </Can>
        }
      >
        <Space direction="vertical" size={12} style={{ width: '100%' }}>
          <Table
            rowKey="id"
            size="small"
            loading={holidays.isLoading}
            dataSource={holidays.data ?? []}
            columns={holidayColumns}
            pagination={false}
          />

          <Space wrap>
            <span>Thử hạn trả:</span>
            <DatePicker
              format="DD/MM/YYYY"
              placeholder="Ngày mượn"
              onChange={(value) => {
                if (value) {
                  const days = (form.getFieldValue('loanDays') as number) ?? 14;
                  testDueDate.mutate({ date: (value as Dayjs).format('YYYY-MM-DD'), days });
                }
              }}
            />
            {preview && (
              <Typography.Text type={preview.shifted ? 'warning' : 'secondary'}>
                {preview.explanation}
              </Typography.Text>
            )}
          </Space>
        </Space>
      </Card>

      <Drawer
        open={editing !== null}
        width={720}
        title={editing?.policy ? `Sửa chính sách ${editing.policy.name}` : 'Thêm chính sách lưu thông'}
        onClose={() => setEditing(null)}
        extra={
          <Button type="primary" loading={savePolicy.isPending} onClick={() => form.submit()}>
            Lưu
          </Button>
        }
      >
        <Form
          form={form}
          layout="vertical"
          initialValues={{
            maxItems: 5,
            loanDays: 14,
            maxRenewals: 2,
            renewalDays: 7,
            finePerDay: 2000,
            graceDays: 1,
            maxHolds: 3,
            holdExpireDays: 3,
            allowLoan: true,
            allowRenew: true,
            allowHold: true,
            allowTakeHome: true,
            requireRenewalApproval: false,
            priority: 100,
            isActive: true,
          }}
          onFinish={(values) => savePolicy.mutate(values)}
        >
          <Row gutter={12}>
            <Col span={16}>
              <Form.Item
                name="name"
                label="Tên chính sách"
                rules={[{ required: true, message: 'Chưa đặt tên chính sách.' }]}
              >
                <Input placeholder="Chính sách mượn — Sinh viên" />
              </Form.Item>
            </Col>
            <Col span={4}>
              <Form.Item name="priority" label="Độ ưu tiên" extra="Lớn hơn thì thắng">
                <InputNumber<number> min={0} max={1000} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={4}>
              <Form.Item name="isActive" label="Áp dụng" valuePropName="checked">
                <Checkbox>Đang dùng</Checkbox>
              </Form.Item>
            </Col>

            <Col span={8}>
              <Form.Item name="readerTypeId" label="Loại bạn đọc" extra="Bỏ trống là mọi loại">
                <Select allowClear options={toOptions(readerTypes.data)} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="documentTypeId" label="Dạng tài liệu" extra="Bỏ trống là mọi dạng">
                <Select allowClear options={toOptions(documentTypes.data)} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="warehouseId" label="Kho" extra="Bỏ trống là mọi kho">
                <Select
                  allowClear
                  options={(warehouses.data ?? []).map((item) => ({
                    value: item.id,
                    label: item.name,
                  }))}
                />
              </Form.Item>
            </Col>

            <Col span={6}>
              <Form.Item name="maxItems" label="Số bản mượn tối đa">
                <InputNumber<number> min={0} max={200} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={6}>
              <Form.Item name="loanDays" label="Số ngày mượn">
                <InputNumber<number> min={1} max={730} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={6}>
              <Form.Item name="maxRenewals" label="Số lần gia hạn">
                <InputNumber<number> min={0} max={20} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={6}>
              <Form.Item name="renewalDays" label="Số ngày mỗi lần gia hạn">
                <InputNumber<number> min={1} max={365} style={{ width: '100%' }} />
              </Form.Item>
            </Col>

            <Col span={6}>
              <Form.Item name="finePerDay" label="Phạt mỗi ngày (đ)">
                <InputNumber<number> min={0} step={500} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={6}>
              <Form.Item name="graceDays" label="Số ngày ân hạn">
                <InputNumber<number> min={0} max={60} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={6}>
              <Form.Item name="maxHolds" label="Số đặt giữ tối đa">
                <InputNumber<number> min={0} max={50} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={6}>
              <Form.Item name="holdExpireDays" label="Số ngày giữ chỗ">
                <InputNumber<number> min={1} max={60} style={{ width: '100%' }} />
              </Form.Item>
            </Col>

            <Col span={24}>
              <Space wrap>
                <Form.Item name="allowLoan" valuePropName="checked" noStyle>
                  <Checkbox>Cho mượn</Checkbox>
                </Form.Item>
                <Form.Item name="allowTakeHome" valuePropName="checked" noStyle>
                  <Checkbox>Cho mang về nhà</Checkbox>
                </Form.Item>
                <Form.Item name="allowRenew" valuePropName="checked" noStyle>
                  <Checkbox>Cho gia hạn</Checkbox>
                </Form.Item>
                <Form.Item name="allowHold" valuePropName="checked" noStyle>
                  <Checkbox>Cho đặt giữ</Checkbox>
                </Form.Item>
                <Form.Item name="requireRenewalApproval" valuePropName="checked" noStyle>
                  <Checkbox>Gia hạn từ xa phải được duyệt</Checkbox>
                </Form.Item>
              </Space>
            </Col>
          </Row>
        </Form>
      </Drawer>

      <Drawer
        open={holidayOpen}
        width={480}
        title="Thêm ngày nghỉ"
        onClose={() => setHolidayOpen(false)}
        extra={
          <Button type="primary" loading={saveHoliday.isPending} onClick={() => holidayForm.submit()}>
            Lưu
          </Button>
        }
      >
        <Form
          form={holidayForm}
          layout="vertical"
          initialValues={{ isRecurringYearly: false, isActive: true }}
          onFinish={(values) => {
            const [from, to] = (values.range ?? []) as Dayjs[];

            if (!from || !to) {
              message.error('Chưa chọn khoảng ngày nghỉ.');
              return;
            }

            saveHoliday.mutate({
              ...values,
              fromDate: from.format('YYYY-MM-DD'),
              toDate: to.format('YYYY-MM-DD'),
              range: undefined,
            });
          }}
        >
          <Form.Item
            name="name"
            label="Tên kỳ nghỉ"
            rules={[{ required: true, message: 'Chưa nhập tên ngày nghỉ.' }]}
          >
            <Input placeholder="Nghỉ Tết Nguyên đán" />
          </Form.Item>
          <Form.Item
            name="range"
            label="Từ ngày — đến ngày"
            rules={[{ required: true, message: 'Chưa chọn khoảng ngày nghỉ.' }]}
            initialValue={[dayjs(), dayjs()]}
          >
            <DatePicker.RangePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item
            name="isRecurringYearly"
            valuePropName="checked"
            extra="Bật cho các ngày lễ dương lịch cố định như 30/4, 2/9. Tết âm lịch thì khai từng năm."
          >
            <Checkbox>Lặp lại hằng năm</Checkbox>
          </Form.Item>
          <Form.Item name="isActive" valuePropName="checked" noStyle>
            <Checkbox>Đang áp dụng</Checkbox>
          </Form.Item>
        </Form>
      </Drawer>
    </Space>
  );
}
