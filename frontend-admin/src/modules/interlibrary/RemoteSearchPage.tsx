import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Collapse,
  Empty,
  Input,
  InputNumber,
  Select,
  Space,
  Statistic,
  Table,
  Tag,
  Typography,
} from 'antd';
import { DownloadOutlined, SearchOutlined } from '@ant-design/icons';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import type { ColumnsType } from 'antd/es/table';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { interLibraryApi } from './api';
import {
  bib1UseCodes,
  describeTarget,
  formatDuration,
  searchFieldLabels,
} from './labels';
import type { RemoteRecordDto, RemoteSearchField, RemoteSearchResultDto } from './types';

const fieldOptions = (Object.keys(searchFieldLabels) as RemoteSearchField[]).map((value) => ({
  value,
  label: bib1UseCodes[value]
    ? `${searchFieldLabels[value]} (Bib-1 ${bib1UseCodes[value]})`
    : searchFieldLabels[value],
}));

/**
 * II.7 — Nhập biểu ghi từ thư viện khác.
 *
 * Tra song song nhiều máy chủ một lúc; máy chủ nào hỏng thì báo riêng chỗ đó chứ không làm hỏng cả
 * lượt tra. Biểu ghi nào thư viện mình đã có được đánh dấu ngay để khỏi nhập trùng.
 */
export function RemoteSearchPage() {
  const { message } = App.useApp();
  const navigate = useNavigate();

  const [field, setField] = useState<RemoteSearchField>('Title');
  const [term, setTerm] = useState('');
  const [targetIds, setTargetIds] = useState<string[]>([]);
  const [maxRecords, setMaxRecords] = useState(20);
  const [result, setResult] = useState<RemoteSearchResultDto | null>(null);

  const targets = useQuery({
    queryKey: ['ill-targets'],
    queryFn: () => interLibraryApi.targets(false),
  });

  const search = useMutation({
    mutationFn: () =>
      interLibraryApi.search({ targetIds, field, term: term.trim(), maxRecords }),
    onSuccess: (data) => {
      setResult(data);

      const failed = data.targets.filter((target) => !target.success);

      if (failed.length > 0) {
        message.warning(
          `${failed.length}/${data.targets.length} máy chủ không trả lời được, xem chi tiết bên dưới.`,
        );
      } else {
        message.success(`Lấy về ${data.totalRecords} biểu ghi từ ${data.targets.length} máy chủ.`);
      }
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không tra cứu được.'),
  });

  const prepare = useMutation({
    mutationFn: (record: RemoteRecordDto) =>
      interLibraryApi.prepareRecord(record.targetId, record.marcJson),
    onSuccess: (marcJson) => {
      // Chuyển sang trình soạn MARC kèm biểu ghi đã chuẩn bị; cán bộ hiệu đính rồi mới lưu.
      navigate('/bien-muc/bieu-ghi/moi', { state: { marcJson, from: 'interlibrary' } });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không chuẩn bị được.'),
  });

  const columns: ColumnsType<RemoteRecordDto> = [
    { title: '#', dataIndex: 'position', width: 60, align: 'right' },
    {
      title: 'Nhan đề',
      dataIndex: 'title',
      width: 340,
      render: (title: string | null, row) => (
        <Space direction="vertical" size={0}>
          <span>{title ?? '(Không có nhan đề)'}</span>
          {row.existingBibId && (
            <Tag color="orange">Thư viện mình đã có: {row.existingBibTitle}</Tag>
          )}
        </Space>
      ),
    },
    { title: 'Tác giả', dataIndex: 'author', width: 200, ellipsis: true },
    { title: 'Nhà xuất bản', dataIndex: 'publisher', width: 190, ellipsis: true },
    { title: 'Năm', dataIndex: 'publishYear', width: 90 },
    { title: 'ISBN', dataIndex: 'isbn', width: 150 },
    { title: 'Số kiểm soát', dataIndex: 'controlNumber', width: 150, ellipsis: true },
    {
      title: '',
      width: 150,
      render: (_, row) => (
        <Can permission={PERMISSIONS.cataloging.bibCreate}>
          <Button
            type="link"
            size="small"
            icon={<DownloadOutlined />}
            loading={prepare.isPending && prepare.variables?.marcJson === row.marcJson}
            onClick={() => prepare.mutate(row)}
          >
            Nhập vào
          </Button>
        </Can>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Tra cứu liên thư viện"
        description="Tra song song nhiều thư viện bạn qua Z39.50 hoặc SRU, rồi nhập biểu ghi về để hiệu đính."
      />

      <Card size="small">
        <Space wrap size={12}>
          <Select
            style={{ width: 220 }}
            options={fieldOptions}
            value={field}
            onChange={setField}
          />
          <Input
            style={{ width: 320 }}
            placeholder="Từ khóa tra cứu"
            value={term}
            onChange={(event) => setTerm(event.target.value)}
            onPressEnter={() => term.trim() && search.mutate()}
          />
          <Select
            mode="multiple"
            allowClear
            style={{ minWidth: 320 }}
            placeholder="Tra ở mọi máy chủ đang bật"
            value={targetIds}
            onChange={setTargetIds}
            options={(targets.data ?? []).map((target) => ({
              value: target.id,
              label: `${target.name} — ${describeTarget(target)}`,
            }))}
          />
          <Space>
            Lấy về tối đa:
            <InputNumber
              min={1}
              max={100}
              value={maxRecords}
              onChange={(value) => setMaxRecords(Number(value) || 20)}
            />
          </Space>
          <Button
            type="primary"
            icon={<SearchOutlined />}
            loading={search.isPending}
            disabled={!term.trim()}
            onClick={() => search.mutate()}
          >
            Tra cứu
          </Button>
        </Space>
      </Card>

      {result && (
        <Space size={16} wrap>
          <Card size="small">
            <Statistic title="Máy chủ đã hỏi" value={result.targets.length} />
          </Card>
          <Card size="small">
            <Statistic title="Tổng kết quả tìm thấy" value={result.totalHits} />
          </Card>
          <Card size="small">
            <Statistic title="Biểu ghi lấy về" value={result.totalRecords} />
          </Card>
        </Space>
      )}

      {result === null && !search.isPending && (
        <Empty description="Nhập từ khóa rồi bấm Tra cứu để hỏi các thư viện bạn." />
      )}

      {result && (
        <Collapse
          defaultActiveKey={result.targets.map((target) => target.targetId)}
          items={result.targets.map((target) => ({
            key: target.targetId,
            label: (
              <Space wrap>
                <strong>{target.targetName}</strong>
                {target.success ? (
                  <Tag color="green">{target.totalHits.toLocaleString('vi-VN')} kết quả</Tag>
                ) : (
                  <Tag color="red">Không trả lời được</Tag>
                )}
                <Typography.Text type="secondary">
                  {formatDuration(target.durationMs)}
                </Typography.Text>
              </Space>
            ),
            children: (
              <Space direction="vertical" size={12} style={{ width: '100%' }}>
                {target.message && (
                  <Alert
                    type={target.success ? 'warning' : 'error'}
                    showIcon
                    message={target.message}
                  />
                )}

                {target.records.length > 0 ? (
                  <Table
                    rowKey={(row) => `${row.targetId}-${row.position}`}
                    size="small"
                    dataSource={target.records}
                    columns={columns}
                    scroll={{ x: 1300 }}
                    pagination={false}
                  />
                ) : (
                  <Empty description="Máy chủ này không có kết quả nào." />
                )}
              </Space>
            ),
          }))}
        />
      )}
    </Space>
  );
}
