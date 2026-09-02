import { useState } from 'react';
import {
  App,
  Button,
  Card,
  Col,
  InputNumber,
  Progress,
  Row,
  Select,
  Space,
  Statistic,
  Table,
  Typography,
} from 'antd';
import { FileExcelOutlined, FilePdfOutlined } from '@ant-design/icons';
import { useMutation, useQuery } from '@tanstack/react-query';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { saveBlob } from '@/modules/marc/api';
import { useCatalogOptions, toOptions } from '@/modules/cataloging/useCatalogOptions';
import { locationsApi } from '@/modules/acquisition/api';
import { money } from '@/modules/acquisition/labels';
import { serialsApi } from './api';
import { frequencyLabels } from './labels';
import type { SerialReportFilter } from './types';
import { MAU } from '@/lib/palette';

/**
 * IV.5 — Báo cáo thống kê ấn phẩm định kỳ.
 *
 * Bốn chiều đặc tả yêu cầu — tổng hợp, môn loại, mức định kỳ, ngôn ngữ — cộng thêm nhà cung cấp và
 * kho, vì đó là hai câu hỏi thư viện hay phải trả lời khi quyết toán tiền đặt báo.
 */
export function SerialReportsPage() {
  const { message } = App.useApp();

  const [dimension, setDimension] = useState('OVERALL');
  const [filter, setFilter] = useState<SerialReportFilter>({});

  const suppliers = useCatalogOptions('suppliers');
  const warehouses = useQuery({
    queryKey: ['acq-warehouses', null],
    queryFn: () => locationsApi.warehouses(),
  });

  const dimensions = useQuery({
    queryKey: ['serial-dimensions'],
    queryFn: () => serialsApi.dimensions(),
  });

  const report = useQuery({
    queryKey: ['serial-stats', dimension, filter],
    queryFn: () => serialsApi.statistics(dimension, filter),
  });

  const exportReport = useMutation({
    mutationFn: (format: string) => serialsApi.exportReport(dimension, format, filter),
    onSuccess: ({ blob, fileName }) => {
      saveBlob(blob, fileName);
      message.success('Đã xuất báo cáo.');
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xuất được.'),
  });

  const rows = report.data?.rows ?? [];

  return (
    <div className="lc-page">
      <PageHeader
        title="Báo cáo ấn phẩm định kỳ"
        description="Tổng hợp, theo môn loại, mức định kỳ, ngôn ngữ, nhà cung cấp và kho."
        actions={
          <Can permission={PERMISSIONS.serial.reportView}>
            <Space>
              <Button
                icon={<FileExcelOutlined />}
                loading={exportReport.isPending}
                onClick={() => exportReport.mutate('Excel')}
              >
                Excel
              </Button>
              <Button
                icon={<FilePdfOutlined />}
                loading={exportReport.isPending}
                onClick={() => exportReport.mutate('Pdf')}
              >
                PDF
              </Button>
            </Space>
          </Can>
        }
      />

      <Card variant="borderless" style={{ marginBottom: 12 }} styles={{ body: { padding: 12 } }}>
        <Space wrap>
          <Select
            style={{ width: 220 }}
            value={dimension}
            onChange={setDimension}
            options={Object.entries(dimensions.data ?? {}).map(([value, label]) => ({
              value,
              label,
            }))}
          />
          <InputNumber
            placeholder="Năm"
            style={{ width: 130 }}
            min={1900}
            max={2200}
            value={filter.year ?? undefined}
            onChange={(value) => setFilter({ ...filter, year: value })}
          />
          <Select
            allowClear
            placeholder="Kỳ hạn"
            style={{ width: 190 }}
            value={filter.frequency ?? undefined}
            onChange={(value) => setFilter({ ...filter, frequency: value ?? null })}
            options={Object.entries(frequencyLabels).map(([value, label]) => ({ value, label }))}
          />
          <Select
            allowClear
            placeholder="Kho"
            style={{ width: 190 }}
            value={filter.warehouseId ?? undefined}
            onChange={(value) => setFilter({ ...filter, warehouseId: value ?? null })}
            options={(warehouses.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
          />
          <Select
            allowClear
            placeholder="Nhà cung cấp"
            style={{ width: 200 }}
            value={filter.supplierId ?? undefined}
            onChange={(value) => setFilter({ ...filter, supplierId: value ?? null })}
            options={toOptions(suppliers.data)}
          />
          <Select
            allowClear
            placeholder="Chỉ đầu báo đang đặt"
            style={{ width: 200 }}
            value={filter.activeOnly === true ? 'true' : undefined}
            onChange={(value) => setFilter({ ...filter, activeOnly: value === 'true' ? true : null })}
            options={[{ value: 'true', label: 'Chỉ đầu báo đang đặt' }]}
          />
        </Space>
      </Card>

      <Row gutter={12} style={{ marginBottom: 12 }}>
        <Col span={6}>
          <Card size="small">
            <Statistic title="Số đầu báo" value={report.data?.totalTitles ?? 0} />
          </Card>
        </Col>
        <Col span={6}>
          <Card size="small">
            <Statistic
              title="Số kỳ đã nhận"
              value={report.data?.totalReceivedIssues ?? 0}
              valueStyle={{ color: MAU.tot }}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card size="small">
            <Statistic
              title="Số kỳ thiếu"
              value={report.data?.totalMissingIssues ?? 0}
              valueStyle={{ color: MAU.loi }}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card size="small">
            <Statistic title="Giá trị (VNĐ)" value={money(report.data?.totalValue ?? 0)} />
          </Card>
        </Col>
      </Row>

      <Card variant="borderless" title={report.data?.title ?? 'Thống kê ấn phẩm định kỳ'}>
        <Table
          rowKey="label"
          size="small"
          loading={report.isFetching}
          dataSource={rows}
          pagination={false}
          columns={[
            { title: report.data?.dimensionName ?? 'Chiều', dataIndex: 'label' },
            { title: 'Số đầu báo', dataIndex: 'titleCount', width: 130, align: 'right' },
            { title: 'Kỳ đã nhận', dataIndex: 'receivedIssues', width: 130, align: 'right' },
            { title: 'Kỳ thiếu', dataIndex: 'missingIssues', width: 120, align: 'right' },
            { title: 'Số bản', dataIndex: 'copies', width: 110, align: 'right' },
            {
              title: 'Tỷ trọng',
              dataIndex: 'percent',
              width: 180,
              render: (value: number) => <Progress percent={value} size="small" />,
            },
            {
              title: 'Giá trị (VNĐ)',
              dataIndex: 'value',
              width: 150,
              align: 'right',
              render: (value: number) => money(value),
            },
          ]}
          summary={() => (
            <Table.Summary.Row>
              <Table.Summary.Cell index={0}>
                <Typography.Text strong>Tổng cộng</Typography.Text>
              </Table.Summary.Cell>
              <Table.Summary.Cell index={1} align="right">
                <Typography.Text strong>{report.data?.totalTitles ?? 0}</Typography.Text>
              </Table.Summary.Cell>
              <Table.Summary.Cell index={2} align="right">
                <Typography.Text strong>{report.data?.totalReceivedIssues ?? 0}</Typography.Text>
              </Table.Summary.Cell>
              <Table.Summary.Cell index={3} align="right">
                <Typography.Text strong>{report.data?.totalMissingIssues ?? 0}</Typography.Text>
              </Table.Summary.Cell>
              <Table.Summary.Cell index={4} />
              <Table.Summary.Cell index={5} />
              <Table.Summary.Cell index={6} align="right">
                <Typography.Text strong>{money(report.data?.totalValue ?? 0)}</Typography.Text>
              </Table.Summary.Cell>
            </Table.Summary.Row>
          )}
        />
      </Card>
    </div>
  );
}
