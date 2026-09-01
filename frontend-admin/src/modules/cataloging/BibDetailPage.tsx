import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  App,
  Button,
  Card,
  Descriptions,
  Empty,
  Space,
  Spin,
  Table,
  Tabs,
  Tag,
  Typography,
} from 'antd';
import { ArrowLeftOutlined, EditOutlined, RollbackOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { errorMessage } from '@/api/formErrors';
import { formatRecordAsText } from '@/modules/marc/marcRecord';
import { catalogingApi, parseMarc } from './api';
import { ItemsPanel } from './ItemsPanel';
import { CoverPanel } from './CoverPanel';
import {
  BIB_SOURCE_LABELS,
  RECORD_STATUS_LABELS,
  type BibVersion,
  type MarcDiffLine,
} from './types';

const MONOSPACE = { fontFamily: 'ui-monospace, Consolas, monospace' } as const;

/**
 * Chi tiết biểu ghi (II.3): bốn tab — thông tin thư mục, MARC thô, đăng ký cá biệt, lịch sử.
 *
 * The four tabs answer the four questions a librarian arrives with: what is this title, what does
 * the record actually say, which copies exist and where, and who changed what.
 */
export function BibDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const bib = useQuery({
    queryKey: ['bib-record', id],
    queryFn: () => catalogingApi.get(id!),
    enabled: Boolean(id),
  });

  if (bib.isLoading || !bib.data) {
    return (
      <Card>
        <Spin tip="Đang tải biểu ghi...">
          <div style={{ height: 120 }} />
        </Spin>
      </Card>
    );
  }

  const record = bib.data;
  const marc = parseMarc(record.marcJson);

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title={record.title}
        description={[record.authorMain, record.publisherName, record.publishYear]
          .filter(Boolean)
          .join(' · ')}
        actions={
          <Space wrap>
            <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/bien-muc')}>
              Về danh sách
            </Button>
            <Can permission={PERMISSIONS.cataloging.bibUpdate}>
              <Button
                type="primary"
                icon={<EditOutlined />}
                onClick={() => navigate(`/bien-muc/${record.id}/sua`)}
              >
                Sửa biểu ghi
              </Button>
            </Can>
          </Space>
        }
      />

      <Space size={6} wrap>
        <Tag style={MONOSPACE}>{record.controlNumber}</Tag>
        <Tag color="blue">{RECORD_STATUS_LABELS[record.status] ?? record.status}</Tag>
        <Tag>{BIB_SOURCE_LABELS[record.source] ?? record.source}</Tag>
        {record.documentTypeName && <Tag>{record.documentTypeName}</Tag>}
        {record.languageName && <Tag>{record.languageName}</Tag>}
        <Tag color={record.itemCount > 0 ? 'green' : 'orange'}>
          {record.itemCount > 0
            ? `${record.availableItemCount}/${record.itemCount} bản sẵn sàng`
            : 'chưa có đăng ký cá biệt'}
        </Tag>
      </Space>

      <Tabs
        defaultActiveKey="bibliographic"
        items={[
          {
            key: 'bibliographic',
            label: 'Thông tin thư mục',
            children: (
              <Space direction="vertical" size={16} style={{ width: '100%' }}>
                <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap', alignItems: 'flex-start' }}>
                  <div style={{ width: 232, flex: 'none' }}>
                    <CoverPanel
                      bibId={record.id}
                      coverImageUrl={record.coverImageUrl}
                      coverImageSource={record.coverImageSource}
                    />
                  </div>
                  <div style={{ flex: '1 1 420px', minWidth: 0 }}>
                <Card size="small" title="Mô tả theo ISBD">
                  <Descriptions column={1} size="small" bordered>
                    {record.isbd.map((area) => (
                      <Descriptions.Item key={area.label} label={area.label}>
                        {area.content}
                      </Descriptions.Item>
                    ))}
                  </Descriptions>
                </Card>
                  </div>
                </div>

                <Card size="small" title="Điểm truy cập">
                  <Descriptions column={{ xs: 1, md: 2 }} size="small">
                    <Descriptions.Item label="Tác giả">
                      {record.authors.length === 0
                        ? '—'
                        : record.authors
                            .map((author) => `${author.name}${author.role ? ` (${author.role})` : ''}`)
                            .join('; ')}
                    </Descriptions.Item>
                    <Descriptions.Item label="Đề mục chủ đề">
                      {record.subjects.join('; ') || '—'}
                    </Descriptions.Item>
                    <Descriptions.Item label="Từ khóa">
                      {record.keywords.join('; ') || '—'}
                    </Descriptions.Item>
                    <Descriptions.Item label="Chỉ số phân loại">
                      {record.classifications
                        .map((item) => `${item.code} (${item.scheme})`)
                        .join('; ') || '—'}
                    </Descriptions.Item>
                    <Descriptions.Item label="Tùng thư">
                      {record.seriesTitle
                        ? `${record.seriesTitle}${record.seriesVolume ? ` ; ${record.seriesVolume}` : ''}`
                        : '—'}
                    </Descriptions.Item>
                    <Descriptions.Item label="Số lượt xem">{record.viewCount}</Descriptions.Item>
                  </Descriptions>
                </Card>
              </Space>
            ),
          },
          {
            key: 'marc',
            label: 'MARC thô',
            children: (
              <Card size="small">
                <Typography.Paragraph style={{ ...MONOSPACE, whiteSpace: 'pre-wrap', marginBottom: 0 }}>
                  {formatRecordAsText(marc)}
                </Typography.Paragraph>
              </Card>
            ),
          },
          {
            key: 'items',
            label: `Đăng ký cá biệt (${record.itemCount})`,
            children: <ItemsPanel bibId={record.id} />,
          },
          {
            key: 'history',
            label: `Lịch sử (${record.versionCount})`,
            children: <VersionsPanel bibId={record.id} />,
          },
        ]}
      />
    </Space>
  );
}

/**
 * Lịch sử sửa đổi của biểu ghi: chọn một phiên bản để xem khác biệt so với bản hiện tại, và
 * khôi phục nếu cần.
 */
function VersionsPanel({ bibId }: { bibId: string }) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const [selected, setSelected] = useState<string | null>(null);

  const versions = useQuery({
    queryKey: ['bib-versions', bibId],
    queryFn: () => catalogingApi.versions(bibId),
  });

  const diff = useQuery({
    queryKey: ['bib-diff', bibId, selected],
    queryFn: () => catalogingApi.diff(bibId, selected!),
    enabled: Boolean(selected),
  });

  const restore = useMutation({
    mutationFn: (versionId: string) => catalogingApi.restore(bibId, versionId),
    onSuccess: async () => {
      message.success('Đã khôi phục biểu ghi về phiên bản đã chọn.');
      await queryClient.invalidateQueries({ queryKey: ['bib-record', bibId] });
      await queryClient.invalidateQueries({ queryKey: ['bib-versions', bibId] });
      setSelected(null);
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  if (!versions.isLoading && (versions.data ?? []).length === 0) {
    return <Empty description="Biểu ghi chưa được sửa lần nào" />;
  }

  const changes = (diff.data ?? []).filter((line) => line.kind !== 'Unchanged');

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <Table<BibVersion>
        rowKey="id"
        size="small"
        loading={versions.isLoading}
        dataSource={versions.data ?? []}
        pagination={false}
        rowClassName={(row) => (row.id === selected ? 'ant-table-row-selected' : '')}
        onRow={(row) => ({ onClick: () => setSelected(row.id), style: { cursor: 'pointer' } })}
        columns={[
          { title: 'Phiên bản', dataIndex: 'versionNumber', width: 100 },
          {
            title: 'Thời điểm',
            dataIndex: 'changedAt',
            width: 180,
            render: (value: string) => new Date(value).toLocaleString('vi-VN'),
          },
          { title: 'Người sửa', dataIndex: 'changedByName', width: 180 },
          { title: 'Ghi chú thay đổi', dataIndex: 'changeNote' },
          {
            title: '',
            width: 150,
            align: 'right',
            render: (_, row) => (
              <Can permission={PERMISSIONS.cataloging.bibVersionRestore}>
                <Button
                  size="small"
                  icon={<RollbackOutlined />}
                  loading={restore.isPending}
                  onClick={(event) => {
                    event.stopPropagation();
                    restore.mutate(row.id);
                  }}
                >
                  Khôi phục
                </Button>
              </Can>
            ),
          },
        ]}
      />

      {selected && (
        <Card size="small" title="Khác biệt so với biểu ghi hiện tại" loading={diff.isFetching}>
          {changes.length === 0 ? (
            <Empty description="Phiên bản này giống hệt biểu ghi hiện tại" />
          ) : (
            <Table<MarcDiffLine>
              rowKey={(row, index) => `${row.tag}-${index}`}
              size="small"
              dataSource={changes}
              pagination={false}
              columns={[
                {
                  title: 'Trường',
                  dataIndex: 'tag',
                  width: 80,
                  render: (value: string) => <Tag style={MONOSPACE}>{value}</Tag>,
                },
                {
                  title: 'Thay đổi',
                  dataIndex: 'kind',
                  width: 110,
                  render: (value: MarcDiffLine['kind']) => {
                    const labels: Record<MarcDiffLine['kind'], [string, string]> = {
                      Added: ['green', 'Thêm mới'],
                      Removed: ['red', 'Đã xóa'],
                      Changed: ['orange', 'Đã sửa'],
                      Unchanged: ['default', 'Không đổi'],
                    };

                    return <Tag color={labels[value][0]}>{labels[value][1]}</Tag>;
                  },
                },
                {
                  title: 'Phiên bản này',
                  dataIndex: 'before',
                  render: (value?: string) => (
                    <Typography.Text style={{ ...MONOSPACE, fontSize: 12 }} type="secondary">
                      {value ?? '—'}
                    </Typography.Text>
                  ),
                },
                {
                  title: 'Hiện tại',
                  dataIndex: 'after',
                  render: (value?: string) => (
                    <Typography.Text style={{ ...MONOSPACE, fontSize: 12 }}>
                      {value ?? '—'}
                    </Typography.Text>
                  ),
                },
              ]}
            />
          )}
        </Card>
      )}
    </Space>
  );
}
