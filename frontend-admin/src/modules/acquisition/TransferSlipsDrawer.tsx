import { useState } from 'react';
import { App, Button, DatePicker, Drawer, Select, Space, Table, Typography } from 'antd';
import { PrinterOutlined } from '@ant-design/icons';
import { useMutation, useQuery } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import type { Dayjs } from 'dayjs';
import { ApiRequestError } from '@/api/client';
import { saveBlob } from '@/modules/marc/api';
import { formsApi, locationsApi, stockApi } from './api';
import { formatDate, money } from './labels';
import type { TransferSlipDto, TransferSlipLineDto } from './types';

/**
 * III.5 — Danh sách phiếu chuyển kho đã lập, in lại được.
 *
 * Phiếu in ngay sau khi chuyển là tờ để ký; tờ ấy rách, thất lạc hoặc cần thêm bản cho kho nhận thì
 * phải in lại được từ chính số phiếu, không phải chuyển kho lần nữa.
 */
export function TransferSlipsDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { message } = App.useApp();
  const [range, setRange] = useState<[Dayjs | null, Dayjs | null] | null>(null);
  const [warehouseId, setWarehouseId] = useState<string | null>(null);

  const warehouses = useQuery({
    queryKey: ['acq-warehouses', null],
    queryFn: () => locationsApi.warehouses(),
    enabled: open,
  });

  const params = {
    from: range?.[0]?.format('YYYY-MM-DD') ?? null,
    to: range?.[1]?.format('YYYY-MM-DD') ?? null,
    warehouseId,
  };

  const slips = useQuery({
    queryKey: ['transfer-slips', params],
    queryFn: () => stockApi.transfers(params),
    enabled: open,
  });

  const print = useMutation({
    mutationFn: (batchCode: string) => formsApi.print('TRANSFER', batchCode),
    onSuccess: ({ blob, fileName }) => {
      saveBlob(blob, fileName);
      message.success('Đã tạo tệp in phiếu chuyển kho.');
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không in được phiếu.'),
  });

  const columns: ColumnsType<TransferSlipDto> = [
    { title: 'Số phiếu', dataIndex: 'batchCode', width: 160 },
    {
      title: 'Ngày chuyển',
      dataIndex: 'movementDate',
      width: 120,
      render: (value: string) => formatDate(value),
    },
    {
      title: 'Kho đi → kho nhận',
      width: 260,
      render: (_, row) => `${row.fromWarehouseName ?? '—'} → ${row.toWarehouseName ?? '—'}`,
    },
    { title: 'Lý do', dataIndex: 'reason', width: 220, ellipsis: true },
    { title: 'Số quyết định', dataIndex: 'decisionNo', width: 130 },
    { title: 'Số bản', dataIndex: 'itemCount', width: 90, align: 'right' },
    {
      title: 'Giá trị (VNĐ)',
      dataIndex: 'totalValue',
      width: 130,
      align: 'right',
      render: (value: number) => money(value),
    },
    { title: 'Người lập', dataIndex: 'performedByName', width: 160 },
    {
      title: '',
      width: 110,
      render: (_, row) => (
        <Button
          size="small"
          icon={<PrinterOutlined />}
          loading={print.isPending && print.variables === row.batchCode}
          onClick={() => print.mutate(row.batchCode)}
        >
          In lại
        </Button>
      ),
    },
  ];

  const lineColumns: ColumnsType<TransferSlipLineDto> = [
    { title: 'Mã vạch', dataIndex: 'barcode', width: 140 },
    { title: 'Số ĐKCB', dataIndex: 'registerNumber', width: 140 },
    { title: 'Nhan đề', dataIndex: 'title', width: 320, ellipsis: true },
    { title: 'Tác giả', dataIndex: 'authorMain', width: 180 },
    { title: 'Ký hiệu xếp giá', dataIndex: 'callNumber', width: 150 },
    {
      title: 'Giá bìa',
      dataIndex: 'price',
      width: 110,
      align: 'right',
      render: (value: number) => money(value),
    },
    { title: 'Tình trạng', dataIndex: 'condition', width: 130 },
  ];

  return (
    <Drawer open={open} onClose={onClose} width={1180} title="Phiếu chuyển kho đã lập">
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <Space wrap>
          <DatePicker.RangePicker
            format="DD/MM/YYYY"
            placeholder={['Chuyển từ ngày', 'đến ngày']}
            onChange={(value) => setRange(value as [Dayjs | null, Dayjs | null] | null)}
          />
          <Select
            allowClear
            placeholder="Kho đi hoặc kho nhận"
            style={{ width: 240 }}
            value={warehouseId ?? undefined}
            onChange={(value) => setWarehouseId(value ?? null)}
            options={(warehouses.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
          />
          <Typography.Text type="secondary">
            Mỗi lần chuyển kho là một phiếu; bấm vào dòng để xem các bản trong phiếu.
          </Typography.Text>
        </Space>

        <Table
          rowKey="batchCode"
          size="small"
          loading={slips.isFetching}
          columns={columns}
          dataSource={slips.data ?? []}
          scroll={{ x: 1380 }}
          pagination={{ pageSize: 20, showTotal: (total) => `Tổng ${total} phiếu` }}
          expandable={{
            expandedRowRender: (row) => (
              <Table
                rowKey="barcode"
                size="small"
                pagination={false}
                columns={lineColumns}
                dataSource={row.lines}
                scroll={{ x: 1170 }}
              />
            ),
          }}
        />
      </Space>
    </Drawer>
  );
}
