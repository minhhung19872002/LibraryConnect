import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Card, Input, Pagination, Table, Tag, Typography } from 'antd';
import { opacApi } from '@/api/opac';
import { ResultList } from '@/components/ResultList';
import { ScrollHint } from '@/components/ScrollHint';
import type { SerialSummary } from '@/types/api';
import { formatDate } from '@/lib/datetime';

const { Paragraph } = Typography;

/** XI.1 / IX.2 — Danh mục luận văn – luận án. */
export function ThesesPage() {
  const [keyword, setKeyword] = useState('');
  const [page, setPage] = useState(1);

  const { data, isLoading } = useQuery({
    queryKey: ['theses', keyword, page],
    queryFn: () => opacApi.theses({ keyword, page, pageSize: 20, sort: 'Newest' }),
  });

  return (
    <div className="lc-container" style={{ padding: '24px 16px 48px' }}>
      <Card title="Luận văn – Luận án">
        <Paragraph type="secondary">
          Danh mục các luận văn thạc sĩ, luận án tiến sĩ và công trình nghiên cứu thư viện đang lưu
          giữ.
        </Paragraph>

        <Input.Search
          placeholder="Tìm theo nhan đề, tác giả, chủ đề"
          allowClear
          enterButton
          style={{ maxWidth: 520, marginBottom: 16 }}
          onSearch={(value) => {
            setKeyword(value);
            setPage(1);
          }}
        />

        <ResultList
          items={data?.items ?? []}
          loading={isLoading}
          emptyText="Chưa có luận văn, luận án nào phù hợp."
        />

        {data && data.totalCount > 0 ? (
          <div style={{ textAlign: 'right', marginTop: 16 }}>
            <Pagination
              current={data.page}
              pageSize={data.pageSize}
              total={data.totalCount}
              showSizeChanger={false}
              onChange={setPage}
            />
          </div>
        ) : null}
      </Card>
    </div>
  );
}

/** XI.1 / IX.2 — Danh mục ấn phẩm định kỳ kèm tình trạng nhận số. */
export function SerialsPage() {
  const [keyword, setKeyword] = useState('');
  const [page, setPage] = useState(1);

  const { data, isLoading } = useQuery({
    queryKey: ['serials', keyword, page],
    queryFn: () => opacApi.serials(page, keyword || undefined),
  });

  const columns = [
    {
      title: 'Tên báo / tạp chí',
      dataIndex: 'title',
      width: 320,
      // Neo lại: bảng rộng hơn khung nên khi cuộn sang phải, không neo thì người xem mất luôn tên
      // của dòng mình đang đọc.
      fixed: 'left' as const,
      render: (title: string, row: SerialSummary) =>
        row.bibId ? <Link to={`/tai-lieu/${row.bibId}`}>{title}</Link> : title,
    },
    { title: 'ISSN', dataIndex: 'issn', width: 130 },
    { title: 'Nhà xuất bản', dataIndex: 'publisherName', width: 220 },
    {
      title: 'Kỳ hạn',
      dataIndex: 'frequencyLabel',
      width: 130,
      render: (value: string) => <Tag>{value}</Tag>,
    },
    { title: 'Kho lưu', dataIndex: 'warehouseName', width: 160 },
    {
      title: 'Số đã nhận',
      dataIndex: 'receivedIssueCount',
      width: 120,
      align: 'right' as const,
    },
    {
      title: 'Số mới nhất',
      dataIndex: 'latestIssueNo',
      width: 180,
      render: (value: string | undefined, row: SerialSummary) =>
        value
          ? `${value}${row.latestIssueDate ? ` (${formatDate(row.latestIssueDate)})` : ''}`
          : '—',
    },
  ];

  return (
    <div className="lc-container" style={{ padding: '24px 16px 48px' }}>
      <Card title="Báo – Tạp chí">
        <Input.Search
          placeholder="Tìm theo tên báo hoặc ISSN"
          allowClear
          enterButton
          style={{ maxWidth: 520, marginBottom: 16 }}
          onSearch={(value) => {
            setKeyword(value);
            setPage(1);
          }}
        />

        <ScrollHint deps={[data?.items]}>
          <Table
            rowKey="id"
            size="small"
            loading={isLoading}
            columns={columns}
            dataSource={data?.items ?? []}
            pagination={false}
            scroll={{ x: 1260 }}
            locale={{ emptyText: 'Chưa có ấn phẩm định kỳ nào.' }}
          />
        </ScrollHint>

        {data && data.totalCount > 0 ? (
          <div style={{ textAlign: 'right', marginTop: 16 }}>
            <Pagination
              current={data.page}
              pageSize={data.pageSize}
              total={data.totalCount}
              showSizeChanger={false}
              onChange={setPage}
            />
          </div>
        ) : null}
      </Card>
    </div>
  );
}
