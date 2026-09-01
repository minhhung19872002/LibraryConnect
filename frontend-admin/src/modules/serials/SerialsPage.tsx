import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Checkbox,
  Col,
  DatePicker,
  Drawer,
  Form,
  Input,
  InputNumber,
  Modal,
  Popconfirm,
  Row,
  Select,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import { CalendarOutlined, DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import dayjs, { type Dayjs } from 'dayjs';
import { PageHeader } from '@/components/PageHeader';
import { FilterBar } from '@/components/FilterBar';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { useCatalogOptions, toOptions } from '@/modules/cataloging/useCatalogOptions';
import { locationsApi } from '@/modules/acquisition/api';
import { formatDate, money } from '@/modules/acquisition/labels';
import { serialsApi } from './api';
import {
  formatIssn,
  frequencyLabels,
  issuesPerYear,
  months,
  numberingLabels,
  weekdays,
  weeklyFrequencies,
} from './labels';
import { SerialWorkbenchDrawer } from './SerialWorkbench';
import type { SerialDto, SerialFrequency } from './types';

/**
 * IV.1 và IV.4 — Danh sách báo, tạp chí.
 *
 * Cột tình trạng nhận số nằm ngay trên danh sách vì đó là câu hỏi cán bộ đặt ra mỗi sáng: đầu nào
 * còn thiếu số. Bấm vào một đầu báo là mở bàn làm việc của riêng nó — lưới nhận số, ghi nhận, khiếu
 * nại, bài trích và đóng tập.
 */
export function SerialsPage() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [filter, setFilter] = useState<Record<string, unknown>>({ page: 1, pageSize: 20 });
  const [draft, setDraft] = useState<Record<string, unknown>>({});
  const [editorId, setEditorId] = useState<string | null | undefined>(undefined);
  const [workbenchId, setWorkbenchId] = useState<string | null>(null);
  const [selected, setSelected] = useState<string[]>([]);
  const [batchOpen, setBatchOpen] = useState(false);

  const publishers = useCatalogOptions('publishers');
  const languages = useCatalogOptions('languages');
  const warehouses = useQuery({
    queryKey: ['acq-warehouses', null],
    queryFn: () => locationsApi.warehouses(),
  });

  const serials = useQuery({
    queryKey: ['serials', filter],
    queryFn: () => serialsApi.search(filter),
    placeholderData: keepPreviousData,
  });

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['serials'] });
    void queryClient.invalidateQueries({ queryKey: ['serial-issues'] });
  };

  const remove = useMutation({
    mutationFn: (id: string) => serialsApi.remove(id),
    onSuccess: () => {
      message.success('Đã xóa đầu báo.');
      refresh();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xóa được.'),
  });

  const columns: ColumnsType<SerialDto> = [
    {
      title: 'Tên báo / tạp chí',
      dataIndex: 'title',
      render: (value: string, row) => (
        <Space direction="vertical" size={0}>
          <Button
            type="link"
            size="small"
            style={{ padding: 0, height: 'auto', textAlign: 'left' }}
            onClick={() => setWorkbenchId(row.id)}
          >
            {value}
          </Button>
          <Typography.Text type="secondary">
            {row.issn ? `ISSN ${formatIssn(row.issn)}` : 'Chưa có ISSN'}
            {row.publisherName ? ` · ${row.publisherName}` : ''}
          </Typography.Text>
        </Space>
      ),
    },
    {
      title: 'Kỳ hạn',
      dataIndex: 'frequency',
      width: 150,
      render: (value: SerialFrequency) => frequencyLabels[value],
    },
    { title: 'Kho', dataIndex: 'warehouseName', width: 150 },
    {
      title: 'Thời gian đặt',
      width: 210,
      render: (_, row) => (
        <Space direction="vertical" size={0}>
          <span>
            {row.subscriptionStart ? formatDate(row.subscriptionStart) : '—'}
            {row.subscriptionEnd ? ` → ${formatDate(row.subscriptionEnd)}` : ''}
          </span>
          {row.subscriptionEndingSoon && <Tag color="warning">Sắp hết hạn đặt</Tag>}
        </Space>
      ),
    },
    {
      title: 'Tình trạng nhận số',
      width: 240,
      render: (_, row) => (
        <Space size={4} wrap>
          <Tag color="green">{row.receivedCount} đã nhận</Tag>
          <Tag>{row.expectedCount} dự kiến</Tag>
          {row.missingCount > 0 && <Tag color="red">{row.missingCount} thiếu</Tag>}
        </Space>
      ),
    },
    {
      title: 'Đơn giá / kỳ',
      dataIndex: 'pricePerIssue',
      width: 130,
      align: 'right',
      render: (value: number | null) => (value ? money(value) : '—'),
    },
    {
      title: '',
      width: 130,
      align: 'right',
      render: (_, row) => (
        <Space>
          <Tooltip title="Bàn làm việc: lưới nhận số, ghi nhận, bài trích, đóng tập">
            <Button
              size="small"
              icon={<CalendarOutlined />}
              onClick={() => setWorkbenchId(row.id)}
            />
          </Tooltip>
          <Can permission={PERMISSIONS.serial.update}>
            <Tooltip title="Sửa">
              <Button size="small" icon={<EditOutlined />} onClick={() => setEditorId(row.id)} />
            </Tooltip>
          </Can>
          <Can permission={PERMISSIONS.serial.delete}>
            <Popconfirm
              title="Xóa đầu báo này?"
              description="Chỉ xóa được khi chưa nhận số nào."
              okText="Xóa"
              cancelText="Bỏ qua"
              onConfirm={() => remove.mutate(row.id)}
            >
              <Button size="small" danger icon={<DeleteOutlined />} />
            </Popconfirm>
          </Can>
        </Space>
      ),
    },
  ];

  return (
    <div className="lc-page">
      <PageHeader
        title="Ấn phẩm định kỳ"
        description="Báo và tạp chí: khai kỳ hạn, sinh số dự kiến, ghi nhận số đến, khiếu nại số thiếu và đóng tập."
        actions={
          <Space>
            <Can permission={PERMISSIONS.serial.predict}>
              <Button
                icon={<CalendarOutlined />}
                disabled={selected.length === 0}
                onClick={() => setBatchOpen(true)}
              >
                Sinh số hàng loạt ({selected.length})
              </Button>
            </Can>
            <Can permission={PERMISSIONS.serial.create}>
              <Button type="primary" icon={<PlusOutlined />} onClick={() => setEditorId(null)}>
                Thêm đầu báo
              </Button>
            </Can>
          </Space>
        }
      />

      <FilterBar
        loading={serials.isFetching}
        onSearch={() => setFilter({ ...draft, page: 1, pageSize: 20 })}
        onReset={() => {
          setDraft({});
          setFilter({ page: 1, pageSize: 20 });
        }}
      >
        <Input
          allowClear
          placeholder="Tên báo, tạp chí hoặc ISSN"
          style={{ width: 300 }}
          value={(draft.keyword as string) ?? ''}
          onChange={(event) => setDraft({ ...draft, keyword: event.target.value })}
        />
        <Select
          allowClear
          placeholder="Kỳ hạn"
          style={{ width: 190 }}
          value={draft.frequency as string | undefined}
          onChange={(value) => setDraft({ ...draft, frequency: value })}
          options={Object.entries(frequencyLabels).map(([value, label]) => ({ value, label }))}
        />
        <Select
          allowClear
          placeholder="Nhà xuất bản"
          style={{ width: 200 }}
          value={draft.publisherId as string | undefined}
          onChange={(value) => setDraft({ ...draft, publisherId: value })}
          options={toOptions(publishers.data)}
        />
        <Select
          allowClear
          placeholder="Ngôn ngữ"
          style={{ width: 160 }}
          value={draft.languageId as string | undefined}
          onChange={(value) => setDraft({ ...draft, languageId: value })}
          options={toOptions(languages.data)}
        />
        <Select
          allowClear
          placeholder="Kho"
          style={{ width: 180 }}
          value={draft.warehouseId as string | undefined}
          onChange={(value) => setDraft({ ...draft, warehouseId: value })}
          options={(warehouses.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
        />
        <Select
          allowClear
          placeholder="Trạng thái đặt"
          style={{ width: 180 }}
          value={draft.subscribedOnly as string | undefined}
          onChange={(value) => setDraft({ ...draft, subscribedOnly: value })}
          options={[{ value: 'true', label: 'Đang trong hạn đặt' }]}
        />
      </FilterBar>

      <Card variant="borderless">
        <Table
          rowKey="id"
          size="small"
          loading={serials.isFetching}
          columns={columns}
          dataSource={serials.data?.items ?? []}
          scroll={{ x: 1300 }}
          rowSelection={{
            selectedRowKeys: selected,
            onChange: (keys) => setSelected(keys as string[]),
          }}
          pagination={{
            current: serials.data?.page ?? 1,
            pageSize: serials.data?.pageSize ?? 20,
            total: serials.data?.totalCount ?? 0,
            showSizeChanger: true,
            showTotal: (total) => `Tổng ${total} đầu báo`,
          }}
          onChange={(pagination) =>
            setFilter((current) => ({
              ...current,
              page: pagination.current ?? 1,
              pageSize: pagination.pageSize ?? 20,
            }))
          }
        />
      </Card>

      {editorId !== undefined && (
        <SerialEditorDrawer
          id={editorId}
          onClose={() => setEditorId(undefined)}
          onSaved={refresh}
        />
      )}

      {workbenchId && (
        <SerialWorkbenchDrawer
          serialId={workbenchId}
          onClose={() => setWorkbenchId(null)}
          onChanged={refresh}
        />
      )}

      <BatchGenerateModal
        open={batchOpen}
        serialIds={selected}
        onClose={() => setBatchOpen(false)}
        onDone={() => {
          setBatchOpen(false);
          setSelected([]);
          refresh();
        }}
      />
    </div>
  );
}

/** Form khai báo một đầu báo: thông tin thư mục, phân kho và kỳ hạn xuất bản. */
function SerialEditorDrawer({
  id,
  onClose,
  onSaved,
}: {
  id: string | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm();

  const publishers = useCatalogOptions('publishers');
  const languages = useCatalogOptions('languages');
  const suppliers = useCatalogOptions('suppliers');
  const warehouses = useQuery({
    queryKey: ['acq-warehouses', null],
    queryFn: () => locationsApi.warehouses(),
  });

  const warehouseId = Form.useWatch('warehouseId', form) as string | undefined;
  const frequency = (Form.useWatch('frequency', form) ?? 'Monthly') as SerialFrequency;
  const numbering = Form.useWatch(['pattern', 'numbering'], form) as string | undefined;

  const shelves = useQuery({
    queryKey: ['acq-shelves', warehouseId],
    queryFn: () => locationsApi.shelves(warehouseId),
    enabled: Boolean(warehouseId),
  });

  const detail = useQuery({
    queryKey: ['serial', id],
    queryFn: async () => {
      const loaded = await serialsApi.get(id!);

      form.setFieldsValue({
        ...loaded,
        subscriptionStart: loaded.subscriptionStart ? dayjs(loaded.subscriptionStart) : undefined,
        subscriptionEnd: loaded.subscriptionEnd ? dayjs(loaded.subscriptionEnd) : undefined,
      });

      return loaded;
    },
    enabled: Boolean(id),
  });

  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      serialsApi.save(id, {
        ...values,
        subscriptionStart: (values.subscriptionStart as Dayjs | undefined)?.format('YYYY-MM-DD'),
        subscriptionEnd: (values.subscriptionEnd as Dayjs | undefined)?.format('YYYY-MM-DD'),
      }),
    onSuccess: () => {
      message.success('Đã lưu đầu báo.');
      onSaved();
      onClose();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  const usesWeekday = weeklyFrequencies.includes(frequency);

  return (
    <Drawer
      open
      width={840}
      onClose={onClose}
      title={detail.data ? `Sửa ${detail.data.title}` : 'Thêm đầu báo / tạp chí'}
      extra={
        <Button type="primary" loading={save.isPending} onClick={() => form.submit()}>
          Lưu
        </Button>
      }
    >
      <Form
        form={form}
        layout="vertical"
        initialValues={{
          frequency: 'Monthly',
          copiesPerIssue: 1,
          isActive: true,
          pattern: {
            numbering: 'RestartEachYear',
            startIssueNumber: 1,
            startVolume: 1,
            dayOfMonth: 1,
            skipMonths: [],
          },
        }}
        onFinish={(values) => save.mutate(values)}
      >
        <Typography.Title level={5}>Thông tin ấn phẩm</Typography.Title>

        <Row gutter={12}>
          <Col span={14}>
            <Form.Item
              name="title"
              label="Tên báo / tạp chí"
              rules={[{ required: true, message: 'Chưa nhập tên.' }]}
            >
              <Input placeholder="Tạp chí Thư viện Việt Nam" />
            </Form.Item>
          </Col>
          <Col span={5}>
            <Form.Item name="issn" label="ISSN">
              <Input placeholder="1859-1234" />
            </Form.Item>
          </Col>
          <Col span={5}>
            <Form.Item name="ddc" label="Chỉ số DDC" extra="Dùng cho báo cáo theo môn loại.">
              <Input placeholder="070.4" />
            </Form.Item>
          </Col>
        </Row>

        <Row gutter={12}>
          <Col span={8}>
            <Form.Item name="publisherId" label="Nhà xuất bản">
              <Select allowClear options={toOptions(publishers.data)} />
            </Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item name="languageId" label="Ngôn ngữ">
              <Select allowClear options={toOptions(languages.data)} />
            </Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item name="supplierId" label="Nhà cung cấp">
              <Select allowClear options={toOptions(suppliers.data)} />
            </Form.Item>
          </Col>
        </Row>

        <Typography.Title level={5}>Phân kho</Typography.Title>

        <Row gutter={12}>
          <Col span={8}>
            <Form.Item name="warehouseId" label="Kho lưu">
              <Select
                allowClear
                options={(warehouses.data ?? []).map((item) => ({
                  value: item.id,
                  label: item.name,
                }))}
              />
            </Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item name="shelfId" label="Vị trí giá">
              <Select
                allowClear
                disabled={!warehouseId}
                options={(shelves.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
              />
            </Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item name="callNumber" label="Ký hiệu xếp giá">
              <Input placeholder="070.4 TCTV" />
            </Form.Item>
          </Col>
        </Row>

        <Typography.Title level={5}>Đặt mua</Typography.Title>

        <Row gutter={12}>
          <Col span={6}>
            <Form.Item name="subscriptionStart" label="Đặt từ ngày">
              <DatePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={6}>
            <Form.Item name="subscriptionEnd" label="Đến ngày">
              <DatePicker format="DD/MM/YYYY" style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={6}>
            <Form.Item name="pricePerIssue" label="Đơn giá mỗi kỳ">
              <InputNumber min={0} step={1000} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={6}>
            <Form.Item name="copiesPerIssue" label="Số bản mỗi kỳ">
              <InputNumber min={1} max={100} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
        </Row>

        <Typography.Title level={5}>Kỳ hạn xuất bản</Typography.Title>

        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message="Khai đúng kỳ hạn là điều quan trọng nhất ở màn hình này."
          description="Hệ thống dựa vào đây để đoán những số nào sẽ đến và đến ngày nào. Khai sai thì lưới theo dõi sẽ báo thiếu những số chưa bao giờ tồn tại."
        />

        <Row gutter={12}>
          <Col span={8}>
            <Form.Item name="frequency" label="Kỳ hạn">
              <Select
                options={Object.entries(frequencyLabels).map(([value, label]) => ({ value, label }))}
              />
            </Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item
              name={['pattern', 'issuesPerYear']}
              label="Số kỳ trong năm"
              extra={
                frequency === 'Irregular'
                  ? 'Bắt buộc với kỳ hạn không định kỳ.'
                  : `Bỏ trống thì hiểu là ${issuesPerYear[frequency]} kỳ.`
              }
              rules={[
                {
                  required: frequency === 'Irregular',
                  message: 'Kỳ hạn không định kỳ thì phải khai số kỳ trong năm.',
                },
              ]}
            >
              <InputNumber min={1} max={400} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={8}>
            {usesWeekday ? (
              <Form.Item name={['pattern', 'dayOfWeek']} label="Thứ phát hành">
                <Select allowClear options={weekdays} />
              </Form.Item>
            ) : (
              <Form.Item name={['pattern', 'dayOfMonth']} label="Ngày phát hành trong tháng">
                <InputNumber min={1} max={31} style={{ width: '100%' }} />
              </Form.Item>
            )}
          </Col>
        </Row>

        {frequency === 'SemiMonthly' && (
          <Row gutter={12}>
            <Col span={8}>
              <Form.Item
                name={['pattern', 'secondDayOfMonth']}
                label="Ngày phát hành thứ hai trong tháng"
              >
                <InputNumber min={1} max={31} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>
        )}

        <Row gutter={12}>
          <Col span={10}>
            <Form.Item name={['pattern', 'numbering']} label="Cách đánh số">
              <Select
                options={Object.entries(numberingLabels).map(([value, label]) => ({ value, label }))}
              />
            </Form.Item>
          </Col>
          <Col span={5}>
            <Form.Item
              name={['pattern', 'startIssueNumber']}
              label="Số bắt đầu"
              extra="Đặt khi tiếp nối một đầu báo đã đặt từ trước."
            >
              <InputNumber min={1} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          {numbering === 'VolumeAndIssue' && (
            <Col span={4}>
              <Form.Item name={['pattern', 'startVolume']} label="Tập bắt đầu">
                <InputNumber min={1} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          )}
          <Col span={5}>
            <Form.Item name={['pattern', 'startYear']} label="Năm ứng với số bắt đầu">
              <InputNumber min={1900} max={2200} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
        </Row>

        <Form.Item
          name={['pattern', 'skipMonths']}
          label="Các tháng không xuất bản"
          extra="Ví dụ tạp chí nghỉ hè thì chọn tháng 7 và tháng 8."
        >
          <Select mode="multiple" allowClear options={months} />
        </Form.Item>

        <Form.Item name="note" label="Ghi chú">
          <Input.TextArea rows={2} />
        </Form.Item>

        <Form.Item name="isActive" valuePropName="checked">
          <Checkbox>Đang đặt mua</Checkbox>
        </Form.Item>
      </Form>
    </Drawer>
  );
}

/** IV.3 — Sinh số cho nhiều đầu báo cùng lúc. */
function BatchGenerateModal({
  open,
  serialIds,
  onClose,
  onDone,
}: {
  open: boolean;
  serialIds: string[];
  onClose: () => void;
  onDone: () => void;
}) {
  const { message, modal } = App.useApp();
  const [range, setRange] = useState<[Dayjs, Dayjs] | null>(null);

  const generate = useMutation({
    mutationFn: () =>
      serialsApi.generateIssues({
        serialIds,
        from: range?.[0]?.format('YYYY-MM-DD'),
        to: range?.[1]?.format('YYYY-MM-DD'),
      }),
    onSuccess: (result) => {
      modal.success({
        title: `Đã sinh ${result.created} số dự kiến`,
        width: 560,
        content: (
          <Space direction="vertical">
            {result.skipped > 0 && (
              <Typography.Text type="secondary">
                Bỏ qua {result.skipped} số đã có từ trước.
              </Typography.Text>
            )}
            <ul style={{ maxHeight: 240, overflow: 'auto', paddingLeft: 18 }}>
              {result.captions.map((caption) => (
                <li key={caption}>{caption}</li>
              ))}
            </ul>
          </Space>
        ),
      });
      onDone();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không sinh được số.'),
  });

  return (
    <Modal
      open={open}
      title={`Sinh số dự kiến cho ${serialIds.length} đầu báo`}
      onCancel={onClose}
      onOk={() => generate.mutate()}
      confirmLoading={generate.isPending}
      okText="Sinh số"
      cancelText="Bỏ qua"
      destroyOnHidden
    >
      <Typography.Paragraph type="secondary">
        Mỗi đầu báo sinh theo kỳ hạn của chính nó. Số đã có trong kỳ sẽ được bỏ qua, nên chạy lại
        không nhân đôi.
      </Typography.Paragraph>

      <DatePicker.RangePicker
        format="DD/MM/YYYY"
        style={{ width: '100%' }}
        placeholder={['Từ ngày', 'đến ngày']}
        onChange={(value) => setRange(value as [Dayjs, Dayjs] | null)}
      />

      <Typography.Paragraph type="secondary" style={{ marginTop: 8, marginBottom: 0 }}>
        Bỏ trống thì lấy theo thời gian đặt mua của từng đầu báo.
      </Typography.Paragraph>
    </Modal>
  );
}
