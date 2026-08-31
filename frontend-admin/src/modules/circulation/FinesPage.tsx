import { useState } from 'react';
import {
  App,
  Button,
  Card,
  Col,
  Form,
  Input,
  InputNumber,
  Modal,
  Row,
  Select,
  Space,
  Statistic,
  Table,
  Tag,
  Typography,
} from 'antd';
import { DollarOutlined, PlusOutlined, PrinterOutlined } from '@ant-design/icons';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import { PageHeader } from '@/components/PageHeader';
import { FilterBar } from '@/components/FilterBar';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { saveBlob } from '@/modules/marc/api';
import { circulationApi } from './api';
import { fineTypeLabels, formatDateTime, money } from './labels';
import type { FineRowDto, FineType } from './types';

const typeOptions = (Object.keys(fineTypeLabels) as FineType[]).map((value) => ({
  value,
  label: fineTypeLabels[value],
}));

/**
 * VII.2 — Thu tiền phạt và miễn giảm.
 *
 * Quầy thu tiền cần ba thứ: ai đang nợ bao nhiêu, thu được từng phần, và in ngay biên lai. Miễn giảm
 * tách thành quyền riêng vì đó là quyết định về tiền chứ không phải thao tác thường ngày.
 */
export function FinesPage() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [filter, setFilter] = useState<{ keyword?: string; type?: FineType; outstandingOnly?: boolean }>({
    outstandingOnly: true,
  });
  const [draft, setDraft] = useState(filter);
  const [page, setPage] = useState({ page: 1, pageSize: 20 });
  const [createOpen, setCreateOpen] = useState(false);
  const [form] = Form.useForm();
  const [readerCard, setReaderCard] = useState('');
  const [readerId, setReaderId] = useState<string | null>(null);

  const fines = useQuery({
    queryKey: ['circulation-fines', page, filter],
    queryFn: () => circulationApi.fines({ ...page, ...filter }),
    placeholderData: keepPreviousData,
  });

  const summary = useQuery({
    queryKey: ['circulation-reader-fines', readerId],
    queryFn: () => circulationApi.readerFines(readerId as string),
    enabled: Boolean(readerId),
  });

  const lookupReader = useMutation({
    mutationFn: (card: string) => circulationApi.deskReader(card),
    onSuccess: (reader) => setReaderId(reader.id),
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không tra được thẻ.'),
  });

  const pay = useMutation({
    mutationFn: ({ id, amount }: { id: string; amount?: number }) =>
      circulationApi.payFine(id, { amount }),
    onSuccess: (fine) => {
      message.success(
        fine.outstanding > 0
          ? `Đã thu, còn nợ ${money(fine.outstanding)} đ.`
          : 'Đã thu đủ tiền phạt.',
      );
      void queryClient.invalidateQueries({ queryKey: ['circulation-fines'] });
      void queryClient.invalidateQueries({ queryKey: ['circulation-reader-fines'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không thu được.'),
  });

  const waive = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      circulationApi.waiveFine(id, reason),
    onSuccess: () => {
      message.success('Đã miễn khoản phạt.');
      void queryClient.invalidateQueries({ queryKey: ['circulation-fines'] });
      void queryClient.invalidateQueries({ queryKey: ['circulation-reader-fines'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không miễn được.'),
  });

  const create = useMutation({
    mutationFn: (values: Record<string, unknown>) => circulationApi.createFine(values),
    onSuccess: () => {
      message.success('Đã lập khoản phạt.');
      setCreateOpen(false);
      form.resetFields();
      void queryClient.invalidateQueries({ queryKey: ['circulation-fines'] });
      void queryClient.invalidateQueries({ queryKey: ['circulation-reader-fines'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lập được.'),
  });

  const printReceipt = async (code: string) => {
    try {
      const { blob, fileName } = await circulationApi.printForm('FINE_RECEIPT', code);
      saveBlob(blob, fileName);
    } catch (error) {
      message.error(error instanceof ApiRequestError ? error.message : 'Không in được biên lai.');
    }
  };

  const columns: ColumnsType<FineRowDto> = [
    { title: 'Số biên lai', dataIndex: 'code', width: 140 },
    {
      title: 'Bạn đọc',
      dataIndex: 'readerName',
      width: 230,
      render: (name: string, row) => (
        <Space direction="vertical" size={0}>
          <Button
            type="link"
            style={{ padding: 0, height: 'auto' }}
            onClick={() => setReaderId(row.readerId)}
          >
            {name}
          </Button>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {row.readerCardNumber}
          </Typography.Text>
        </Space>
      ),
    },
    {
      title: 'Loại',
      dataIndex: 'type',
      width: 120,
      render: (value: FineType) => <Tag>{fineTypeLabels[value]}</Tag>,
    },
    { title: 'Tài liệu', dataIndex: 'title', ellipsis: true },
    { title: 'Số tiền', dataIndex: 'amount', width: 120, align: 'right', render: money },
    { title: 'Đã thu', dataIndex: 'paidAmount', width: 120, align: 'right', render: money },
    {
      title: 'Còn nợ',
      dataIndex: 'outstanding',
      width: 120,
      align: 'right',
      render: (value: number, row) =>
        row.waived ? (
          <Tag>Đã miễn</Tag>
        ) : value > 0 ? (
          <Typography.Text type="danger">{money(value)}</Typography.Text>
        ) : (
          <Tag color="green">Đã thu đủ</Tag>
        ),
    },
    { title: 'Lập lúc', dataIndex: 'createdAt', width: 160, render: formatDateTime },
    {
      title: '',
      width: 210,
      render: (_, row) => (
        <Space size={4}>
          {!row.waived && row.outstanding > 0 && (
            <>
              <Can permission={PERMISSIONS.circulation.fineCollect}>
                <Button
                  type="link"
                  size="small"
                  icon={<DollarOutlined />}
                  onClick={() => {
                    let amount = row.outstanding;

                    modal.confirm({
                      title: `Thu tiền phạt ${row.code}`,
                      content: (
                        <Space direction="vertical" style={{ width: '100%' }}>
                          <Typography.Text>
                            Bạn đọc {row.readerName} còn nợ {money(row.outstanding)} đ.
                          </Typography.Text>
                          <InputNumber<number>
                            min={1}
                            max={row.outstanding}
                            step={1000}
                            defaultValue={row.outstanding}
                            style={{ width: '100%' }}
                            onChange={(value) => {
                              amount = value ?? row.outstanding;
                            }}
                          />
                        </Space>
                      ),
                      okText: 'Thu tiền',
                      cancelText: 'Đóng',
                      onOk: () => pay.mutateAsync({ id: row.id, amount }),
                    });
                  }}
                >
                  Thu
                </Button>
              </Can>
              <Can permission={PERMISSIONS.circulation.fineWaive}>
                <Button
                  type="link"
                  size="small"
                  onClick={() => {
                    let reason = '';

                    modal.confirm({
                      title: `Miễn khoản phạt ${row.code}`,
                      content: (
                        <Input.TextArea
                          rows={2}
                          placeholder="Lý do miễn giảm (bắt buộc)"
                          onChange={(event) => {
                            reason = event.target.value;
                          }}
                        />
                      ),
                      okText: 'Miễn',
                      cancelText: 'Đóng',
                      onOk: () => waive.mutateAsync({ id: row.id, reason }),
                    });
                  }}
                >
                  Miễn
                </Button>
              </Can>
            </>
          )}
          <Button
            type="link"
            size="small"
            icon={<PrinterOutlined />}
            onClick={() => void printReceipt(row.code)}
          >
            Biên lai
          </Button>
        </Space>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Tiền phạt"
        description="Thu tiền phạt quá hạn, bồi thường tài liệu mất hỏng và các khoản thu khác; in biên lai ngay tại quầy."
        actions={
          <Can permission={PERMISSIONS.circulation.fineCollect}>
            <Button type="primary" icon={<PlusOutlined />} onClick={() => setCreateOpen(true)}>
              Lập khoản phạt
            </Button>
          </Can>
        }
      />

      <Card size="small" title="Tra nợ của một bạn đọc">
        <Space wrap>
          <Input
            allowClear
            style={{ width: 260 }}
            placeholder="Quét thẻ hoặc nhập số thẻ"
            value={readerCard}
            onChange={(event) => setReaderCard(event.target.value)}
            onPressEnter={() => lookupReader.mutate(readerCard.trim())}
          />
          <Button loading={lookupReader.isPending} onClick={() => lookupReader.mutate(readerCard.trim())}>
            Tra nợ
          </Button>

          {summary.data && (
            <Space size={24} wrap>
              <Statistic
                title={`${summary.data.fullName} (${summary.data.cardNumber})`}
                value={money(summary.data.totalOutstanding)}
                suffix="đ còn nợ"
                valueStyle={{
                  color: summary.data.totalOutstanding > 0 ? '#cf1322' : '#389e0d',
                  fontSize: 20,
                }}
              />
              <Statistic title="Đã thu" value={money(summary.data.totalPaid)} suffix="đ" />
              <Statistic title="Đã miễn" value={money(summary.data.totalWaived)} suffix="đ" />
            </Space>
          )}
        </Space>
      </Card>

      <FilterBar
        loading={fines.isFetching}
        onSearch={() => {
          setFilter(draft);
          setPage((current) => ({ ...current, page: 1 }));
        }}
        onReset={() => {
          setDraft({ outstandingOnly: true });
          setFilter({ outstandingOnly: true });
        }}
      >
        <Input
          allowClear
          style={{ width: 260 }}
          placeholder="Số biên lai, số thẻ, tên bạn đọc…"
          value={draft.keyword}
          onChange={(event) => setDraft({ ...draft, keyword: event.target.value })}
        />
        <Select
          allowClear
          style={{ width: 160 }}
          placeholder="Loại phạt"
          options={typeOptions}
          value={draft.type}
          onChange={(value) => setDraft({ ...draft, type: value })}
        />
        <Select
          style={{ width: 180 }}
          value={draft.outstandingOnly ? 'debt' : 'all'}
          options={[
            { value: 'debt', label: 'Chỉ khoản còn nợ' },
            { value: 'all', label: 'Tất cả khoản phạt' },
          ]}
          onChange={(value) => setDraft({ ...draft, outstandingOnly: value === 'debt' })}
        />
      </FilterBar>

      <Table
        rowKey="id"
        size="small"
        loading={fines.isFetching}
        dataSource={fines.data?.items ?? []}
        columns={columns}
        scroll={{ x: 1500 }}
        pagination={{
          current: fines.data?.page ?? 1,
          pageSize: fines.data?.pageSize ?? 20,
          total: fines.data?.totalCount ?? 0,
          showSizeChanger: true,
          showTotal: (total) => `Tổng ${total.toLocaleString('vi-VN')} khoản`,
        }}
        onChange={(pagination) =>
          setPage({ page: pagination.current ?? 1, pageSize: pagination.pageSize ?? 20 })
        }
      />

      <Modal
        open={createOpen}
        title="Lập khoản phạt"
        okText="Lưu"
        cancelText="Hủy"
        confirmLoading={create.isPending}
        onCancel={() => setCreateOpen(false)}
        onOk={() => form.submit()}
      >
        <Form
          form={form}
          layout="vertical"
          initialValues={{ type: 'Other', amount: 20000 }}
          onFinish={(values) => create.mutate({ ...values, readerId: readerId ?? values.readerId })}
        >
          {!readerId && (
            <Form.Item
              name="readerId"
              label="Mã bạn đọc"
              rules={[{ required: true, message: 'Hãy tra thẻ bạn đọc ở khung phía trên trước.' }]}
            >
              <Input placeholder="Tra thẻ ở khung Tra nợ để chọn bạn đọc" />
            </Form.Item>
          )}

          {readerId && summary.data && (
            <Row gutter={12} style={{ marginBottom: 12 }}>
              <Col span={24}>
                <Typography.Text>
                  Lập cho bạn đọc <strong>{summary.data.fullName}</strong> ({summary.data.cardNumber})
                </Typography.Text>
              </Col>
            </Row>
          )}

          <Form.Item name="type" label="Loại phạt">
            <Select options={typeOptions} />
          </Form.Item>
          <Form.Item
            name="amount"
            label="Số tiền (đ)"
            rules={[{ required: true, message: 'Chưa nhập số tiền.' }]}
          >
            <InputNumber<number> min={1000} step={1000} style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="note" label="Lý do">
            <Input.TextArea rows={2} placeholder="Ví dụ: làm mất thẻ thư viện" />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}
