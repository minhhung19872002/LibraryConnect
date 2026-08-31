import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import {
  Button,
  Card,
  Col,
  Divider,
  Input,
  InputNumber,
  Pagination,
  Row,
  Select,
  Space,
  Switch,
  Typography,
} from 'antd';
import { DeleteOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons';
import { opacApi } from '@/api/opac';
import { SCOPE_OPTIONS } from '@/components/searchScopes';
import { ResultList } from '@/components/ResultList';
import type {
  Connector,
  PagedResult,
  SearchClause,
  SearchFilter,
  SearchResult,
  SearchScope,
  SortOrder,
} from '@/types/api';

const { Paragraph } = Typography;

const CONNECTORS: { value: Connector; label: string }[] = [
  { value: 'And', label: 'VÀ' },
  { value: 'Or', label: 'HOẶC' },
  { value: 'Not', label: 'KHÔNG' },
];

/** IX.2 — Tra cứu nâng cao: nhiều điều kiện kết hợp AND / OR / NOT kèm bộ lọc. */
export function AdvancedSearchPage() {
  const [clauses, setClauses] = useState<SearchClause[]>([
    { connector: 'And', field: 'Title', term: '' },
    { connector: 'And', field: 'Author', term: '' },
  ]);
  const [filter, setFilter] = useState<SearchFilter>({});
  const [sort, setSort] = useState<SortOrder>('Relevance');
  const [page, setPage] = useState(1);

  const search = useMutation<PagedResult<SearchResult>, Error, number>({
    mutationFn: (targetPage: number) =>
      opacApi.advancedSearch({
        clauses: clauses.filter((clause) => clause.term.trim().length > 0),
        filter,
        sort,
        page: targetPage,
        pageSize: 20,
      }),
  });

  const run = (targetPage: number) => {
    setPage(targetPage);
    search.mutate(targetPage);
  };

  const setClause = (index: number, changes: Partial<SearchClause>) =>
    setClauses((current) =>
      current.map((clause, position) =>
        position === index ? { ...clause, ...changes } : clause,
      ),
    );

  return (
    <div className="lc-container" style={{ padding: '24px 16px 48px' }}>
      <Card title="Tra cứu nâng cao">
        <Paragraph type="secondary">
          Mỗi dòng là một điều kiện. Dòng đầu tiên là điểm xuất phát; các dòng sau nối vào bằng VÀ
          (phải thỏa mãn thêm), HOẶC (thỏa mãn một trong hai) hoặc KHÔNG (loại trừ).
        </Paragraph>

        <Space direction="vertical" size="small" style={{ width: '100%' }}>
          {clauses.map((clause, index) => (
            <Row gutter={8} key={index} align="middle">
              <Col xs={6} sm={4} md={3}>
                {index === 0 ? (
                  <Input value="Tìm" disabled />
                ) : (
                  <Select
                    value={clause.connector}
                    options={CONNECTORS}
                    onChange={(value) => setClause(index, { connector: value })}
                    style={{ width: '100%' }}
                  />
                )}
              </Col>
              <Col xs={18} sm={6} md={5}>
                <Select
                  value={clause.field}
                  options={SCOPE_OPTIONS}
                  onChange={(value: SearchScope) => setClause(index, { field: value })}
                  style={{ width: '100%' }}
                />
              </Col>
              <Col xs={20} sm={12} md={14}>
                <Input
                  value={clause.term}
                  placeholder="Nhập nội dung cần tìm"
                  onChange={(event) => setClause(index, { term: event.target.value })}
                  onPressEnter={() => run(1)}
                />
              </Col>
              <Col xs={4} sm={2}>
                <Button
                  icon={<DeleteOutlined />}
                  disabled={clauses.length <= 1}
                  onClick={() =>
                    setClauses((current) => current.filter((_, position) => position !== index))
                  }
                />
              </Col>
            </Row>
          ))}
        </Space>

        <Button
          type="dashed"
          icon={<PlusOutlined />}
          style={{ marginTop: 12 }}
          disabled={clauses.length >= 10}
          onClick={() =>
            setClauses((current) => [...current, { connector: 'And', field: 'All', term: '' }])
          }
        >
          Thêm điều kiện
        </Button>

        <Divider orientation="left" plain>
          Giới hạn kết quả
        </Divider>

        <Row gutter={[16, 16]}>
          <Col xs={12} md={6}>
            <div style={{ marginBottom: 4 }}>Năm xuất bản từ</div>
            <InputNumber
              style={{ width: '100%' }}
              min={1}
              max={2999}
              value={filter.publishYearFrom}
              onChange={(value) =>
                setFilter((current) => ({ ...current, publishYearFrom: value ?? undefined }))
              }
            />
          </Col>
          <Col xs={12} md={6}>
            <div style={{ marginBottom: 4 }}>đến năm</div>
            <InputNumber
              style={{ width: '100%' }}
              min={1}
              max={2999}
              value={filter.publishYearTo}
              onChange={(value) =>
                setFilter((current) => ({ ...current, publishYearTo: value ?? undefined }))
              }
            />
          </Col>
          <Col xs={12} md={6}>
            <div style={{ marginBottom: 4 }}>Ký hiệu phân loại bắt đầu bằng</div>
            <Input
              value={filter.ddc}
              placeholder="Ví dụ 005"
              onChange={(event) =>
                setFilter((current) => ({ ...current, ddc: event.target.value || undefined }))
              }
            />
          </Col>
          <Col xs={12} md={6}>
            <Space direction="vertical" size={4}>
              <Space>
                <Switch
                  checked={filter.availableOnly ?? false}
                  onChange={(checked) =>
                    setFilter((current) => ({ ...current, availableOnly: checked || undefined }))
                  }
                />
                <span>Còn bản rảnh</span>
              </Space>
              <Space>
                <Switch
                  checked={filter.hasDigital ?? false}
                  onChange={(checked) =>
                    setFilter((current) => ({ ...current, hasDigital: checked || undefined }))
                  }
                />
                <span>Có tài liệu số</span>
              </Space>
            </Space>
          </Col>
        </Row>

        <Space style={{ marginTop: 20 }}>
          <Button
            type="primary"
            icon={<SearchOutlined />}
            loading={search.isPending}
            onClick={() => run(1)}
          >
            Tra cứu
          </Button>
          <Select
            value={sort}
            style={{ width: 180 }}
            onChange={setSort}
            options={[
              { value: 'Relevance', label: 'Liên quan nhất' },
              { value: 'Newest', label: 'Mới nhất' },
              { value: 'Title', label: 'Nhan đề A → Z' },
              { value: 'Author', label: 'Tác giả A → Z' },
              { value: 'Popular', label: 'Được mượn nhiều' },
            ]}
          />
        </Space>
      </Card>

      {search.data ? (
        <Card
          style={{ marginTop: 24 }}
          title={`Tìm thấy ${search.data.totalCount.toLocaleString('vi-VN')} tài liệu`}
        >
          <ResultList items={search.data.items} loading={search.isPending} />

          {search.data.totalCount > 0 ? (
            <div style={{ textAlign: 'right', marginTop: 16 }}>
              <Pagination
                current={page}
                pageSize={search.data.pageSize}
                total={search.data.totalCount}
                showSizeChanger={false}
                onChange={run}
              />
            </div>
          ) : null}
        </Card>
      ) : null}
    </div>
  );
}
