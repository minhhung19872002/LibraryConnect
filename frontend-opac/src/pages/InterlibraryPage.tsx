import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useMutation, useQuery } from '@tanstack/react-query';
import { Alert, Button, Card, Checkbox, Empty, Input, Select, Space, Table, Tag, Typography } from 'antd';
import { SearchOutlined } from '@ant-design/icons';
import { opacApi } from '@/api/opac';

const { Paragraph } = Typography;

const FIELDS = [
  { value: 'Any', label: 'Bất kỳ' },
  { value: 'Title', label: 'Nhan đề' },
  { value: 'Author', label: 'Tác giả' },
  { value: 'Isbn', label: 'ISBN' },
  { value: 'Issn', label: 'ISSN' },
  { value: 'Subject', label: 'Chủ đề' },
];

/**
 * IX.5 — Tìm ở thư viện khác.
 *
 * Kết quả gộp nhưng ghi rõ nguồn, và đánh dấu cuốn nào thư viện mình đã có — bạn đọc cần biết ngay
 * là nên mượn tại chỗ hay phải nhờ mượn liên thư viện.
 */
export function InterlibraryPage() {
  const [term, setTerm] = useState('');
  const [field, setField] = useState('Any');
  const [selected, setSelected] = useState<string[]>([]);

  const targets = useQuery({
    queryKey: ['remote-targets'],
    queryFn: () => opacApi.remoteTargets(),
  });

  const search = useMutation({
    mutationFn: () => opacApi.remoteSearch({ term: term.trim(), field, targetIds: selected }),
  });

  return (
    <div className="lc-container" style={{ padding: '24px 16px 48px' }}>
      <Card title="Tìm ở thư viện khác">
        <Paragraph type="secondary">
          Tra cứu song song sang các thư viện đã kết nối theo chuẩn Z39.50 và SRU. Kết quả ghi rõ
          nguồn; những cuốn thư viện mình đã có sẽ có liên kết mở thẳng sang trang chi tiết.
        </Paragraph>

        <Space.Compact style={{ width: '100%', maxWidth: 720 }}>
          <Select value={field} options={FIELDS} onChange={setField} style={{ width: 140 }} />
          <Input
            value={term}
            placeholder="Nhập từ khóa tra cứu"
            onChange={(event) => setTerm(event.target.value)}
            onPressEnter={() => term.trim() && search.mutate()}
          />
          <Button
            type="primary"
            icon={<SearchOutlined />}
            loading={search.isPending}
            disabled={!term.trim()}
            onClick={() => search.mutate()}
          >
            Tra cứu
          </Button>
        </Space.Compact>

        {targets.data && targets.data.length > 0 ? (
          <div style={{ marginTop: 12 }}>
            <Checkbox.Group
              value={selected}
              onChange={(values) => setSelected(values as string[])}
              options={targets.data.map((target) => ({ value: target.id, label: target.name }))}
            />
            <div style={{ fontSize: 12, color: 'var(--lc-muted)', marginTop: 4 }}>
              Không chọn nơi nào thì tra ở tất cả.
            </div>
          </div>
        ) : (
          <Alert
            style={{ marginTop: 12 }}
            type="info"
            message="Thư viện chưa mở kết nối tới thư viện bạn nào cho trang tra cứu."
          />
        )}

        {search.isError ? (
          <Alert
            style={{ marginTop: 16 }}
            type="error"
            message={(search.error as Error).message}
          />
        ) : null}
      </Card>

      {search.data
        ? search.data.targets.map((target) => (
            <Card
              key={target.targetId}
              style={{ marginTop: 16 }}
              title={target.targetName}
              extra={
                target.success ? (
                  <Space>
                    <Tag color="green">{target.totalHits} kết quả</Tag>
                    <span style={{ color: 'var(--lc-muted)' }}>{target.durationMs} ms</span>
                  </Space>
                ) : (
                  <Tag color="red">Không tra được</Tag>
                )
              }
            >
              {!target.success ? (
                <Alert type="warning" message={target.message ?? 'Máy chủ không phản hồi.'} />
              ) : target.records.length === 0 ? (
                <Empty description="Máy chủ này không có tài liệu phù hợp." />
              ) : (
                <Table
                  rowKey={(row) => `${target.targetId}-${row.title}-${row.isbn ?? ''}`}
                  size="small"
                  pagination={false}
                  scroll={{ x: 900 }}
                  dataSource={target.records}
                  columns={[
                    { title: 'Nhan đề', dataIndex: 'title', width: 340 },
                    { title: 'Tác giả', dataIndex: 'author', width: 200 },
                    { title: 'Nhà xuất bản', dataIndex: 'publisher', width: 200 },
                    { title: 'Năm', dataIndex: 'publishYear', width: 90 },
                    { title: 'ISBN', dataIndex: 'isbn', width: 150 },
                    {
                      title: 'Ở thư viện mình',
                      dataIndex: 'existingBibId',
                      width: 200,
                      render: (bibId: string | undefined, row) =>
                        bibId ? (
                          <Link to={`/tai-lieu/${bibId}`}>{row.existingBibTitle ?? 'Đã có'}</Link>
                        ) : (
                          <span style={{ color: 'var(--lc-muted)' }}>Chưa có</span>
                        ),
                    },
                  ]}
                />
              )}
            </Card>
          ))
        : null}

      {search.data && search.data.targets.length === 0 ? (
        <Card style={{ marginTop: 16 }}>
          <Empty description="Không có máy chủ nào để tra cứu." />
        </Card>
      ) : null}
    </div>
  );
}
