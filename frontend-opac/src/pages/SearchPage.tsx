import { useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { App, Button, Card, Checkbox, Col, Pagination, Row, Select, Space, Tag } from 'antd';
import { SaveOutlined } from '@ant-design/icons';
import { opacApi, readerApi, type SearchParams } from '@/api/opac';
import { ResultList } from '@/components/ResultList';
import { SearchBox } from '@/components/SearchBox';
import { SCOPE_OPTIONS } from '@/components/searchScopes';
import { useAuthStore } from '@/stores/authStore';
import type { FacetGroup, PagedResult, SearchResult, SearchScope, SortOrder } from '@/types/api';

const SORT_OPTIONS: { value: SortOrder; label: string }[] = [
  { value: 'Relevance', label: 'Liên quan nhất' },
  { value: 'Newest', label: 'Mới nhất' },
  { value: 'Title', label: 'Nhan đề A → Z' },
  { value: 'Author', label: 'Tác giả A → Z' },
  { value: 'Popular', label: 'Được mượn nhiều' },
];

/** Tên tham số lọc trên địa chỉ, khớp với tên trường của máy chủ. */
const FACET_PARAM: Record<string, string> = {
  documentType: 'documentTypeId',
  author: 'authorId',
  subject: 'subjectId',
  language: 'languageId',
  publishYear: 'publishYear',
  warehouse: 'warehouseId',
};

/**
 * IX.2 — Trang kết quả tra cứu.
 *
 * Toàn bộ trạng thái nằm trên địa chỉ: bấm bộ lọc là đổi địa chỉ, nên bạn đọc gửi đường dẫn cho
 * người khác thì họ mở ra thấy đúng kết quả đó, và nút quay lại của trình duyệt hoạt động đúng.
 */
export function SearchPage() {
  const [params, setParams] = useSearchParams();
  const { message, modal } = App.useApp();
  const user = useAuthStore((state) => state.user);

  const keyword = params.get('keyword') ?? '';
  const scope = (params.get('scope') as SearchScope | null) ?? 'All';
  const sort = (params.get('sort') as SortOrder | null) ?? 'Relevance';
  const page = Number(params.get('page') ?? '1');

  const searchParams = useMemo<SearchParams>(() => {
    const filter: SearchParams['filter'] = {};

    if (params.get('documentTypeId')) filter.documentTypeId = params.get('documentTypeId')!;
    if (params.get('authorId')) filter.authorId = params.get('authorId')!;
    if (params.get('subjectId')) filter.subjectId = params.get('subjectId')!;
    if (params.get('languageId')) filter.languageId = params.get('languageId')!;
    if (params.get('warehouseId')) filter.warehouseId = params.get('warehouseId')!;
    if (params.get('collectionId')) filter.collectionId = params.get('collectionId')!;
    if (params.get('ddc')) filter.ddc = params.get('ddc')!;
    if (params.get('publishYear')) {
      const year = Number(params.get('publishYear'));
      filter.publishYearFrom = year;
      filter.publishYearTo = year;
    }
    if (params.get('available') === '1') filter.availableOnly = true;
    if (params.get('digital') === '1') filter.hasDigital = true;

    return { keyword, scope, sort, page, pageSize: 20, filter };
  }, [keyword, page, params, scope, sort]);

  const results = useQuery<PagedResult<SearchResult>>({
    queryKey: ['search', searchParams],
    queryFn: () => opacApi.search(searchParams),
  });

  const facets = useQuery<FacetGroup[]>({
    queryKey: ['facets', { ...searchParams, page: 1 }],
    queryFn: () => opacApi.facets({ ...searchParams, page: 1 }),
  });

  const update = (changes: Record<string, string | null>) => {
    const next = new URLSearchParams(params);

    Object.entries(changes).forEach(([key, value]) => {
      if (value === null || value === '') {
        next.delete(key);
      } else {
        next.set(key, value);
      }
    });

    // Đổi bộ lọc thì phải về trang đầu, nếu không bạn đọc đang ở trang 5 bấm lọc lại ra danh sách
    // rỗng vì kết quả mới chỉ còn hai trang.
    if (!('page' in changes)) {
      next.delete('page');
    }

    setParams(next);
  };

  const saveSearch = () => {
    let name = keyword || 'Tìm kiếm không từ khóa';

    modal.confirm({
      title: 'Lưu lần tra cứu này',
      content: (
        <input
          defaultValue={name}
          onChange={(event) => {
            name = event.target.value;
          }}
          style={{ width: '100%', padding: 8, marginTop: 8 }}
        />
      ),
      okText: 'Lưu',
      cancelText: 'Bỏ qua',
      onOk: async () => {
        await readerApi.saveSearch(name, JSON.stringify(searchParams));
        message.success('Đã lưu tìm kiếm vào tài khoản của bạn.');
      },
    });
  };

  return (
    <div className="lc-container" style={{ padding: '24px 16px 48px' }}>
      <Card style={{ marginBottom: 16 }}>
        <SearchBox size="middle" initialKeyword={keyword} initialScope={scope} />
      </Card>

      <Row gutter={24}>
        <Col xs={24} lg={6}>
          <Card title="Thu hẹp kết quả" size="small" loading={facets.isLoading}>
            <Space direction="vertical" size="middle" style={{ width: '100%' }}>
              <Checkbox
                checked={params.get('available') === '1'}
                onChange={(event) => update({ available: event.target.checked ? '1' : null })}
              >
                Chỉ tài liệu còn bản rảnh
              </Checkbox>
              <Checkbox
                checked={params.get('digital') === '1'}
                onChange={(event) => update({ digital: event.target.checked ? '1' : null })}
              >
                Chỉ tài liệu có bản số
              </Checkbox>

              {(facets.data ?? [])
                .filter((group) => group.code !== 'availability' && group.values.length > 0)
                .map((group) => (
                  <div key={group.code}>
                    <div style={{ fontWeight: 600, marginBottom: 6 }}>{group.name}</div>
                    <Space direction="vertical" size={2} style={{ width: '100%' }}>
                      {group.values.map((value) => {
                        const param = FACET_PARAM[group.code];
                        const active = param ? params.get(param) === value.id : false;

                        return (
                          <a
                            key={`${group.code}-${value.id}`}
                            onClick={() =>
                              param ? update({ [param]: active ? null : (value.id ?? null) }) : undefined
                            }
                            style={{ fontWeight: active ? 600 : 400 }}
                          >
                            {value.label}{' '}
                            <span style={{ color: 'var(--lc-muted)' }}>({value.count})</span>
                          </a>
                        );
                      })}
                    </Space>
                  </div>
                ))}
            </Space>
          </Card>
        </Col>

        <Col xs={24} lg={18}>
          <Card
            title={
              results.data
                ? `Tìm thấy ${results.data.totalCount.toLocaleString('vi-VN')} tài liệu`
                : 'Đang tra cứu…'
            }
            extra={
              <Space>
                {user ? (
                  <Button size="small" icon={<SaveOutlined />} onClick={saveSearch}>
                    Lưu tìm kiếm
                  </Button>
                ) : null}
                <Select
                  size="small"
                  value={sort}
                  options={SORT_OPTIONS}
                  onChange={(value) => update({ sort: value })}
                  style={{ width: 170 }}
                />
              </Space>
            }
          >
            <Space size={[8, 8]} wrap style={{ marginBottom: 8 }}>
              {keyword ? (
                <Tag closable onClose={() => update({ keyword: null })}>
                  {SCOPE_OPTIONS.find((option) => option.value === scope)?.label}: {keyword}
                </Tag>
              ) : null}
              {[...params.entries()]
                .filter(([key]) => Object.values(FACET_PARAM).includes(key))
                .map(([key, value]) => (
                  <Tag key={key} closable onClose={() => update({ [key]: null })}>
                    Bộ lọc đang áp dụng
                    {key === 'publishYear' ? `: năm ${value}` : ''}
                  </Tag>
                ))}
            </Space>

            <ResultList items={results.data?.items ?? []} loading={results.isLoading} />

            {results.data && results.data.totalCount > 0 ? (
              <div style={{ textAlign: 'right', marginTop: 16 }}>
                <Pagination
                  current={results.data.page}
                  pageSize={results.data.pageSize}
                  total={results.data.totalCount}
                  showSizeChanger={false}
                  onChange={(value) => update({ page: String(value) })}
                />
              </div>
            ) : null}
          </Card>
        </Col>
      </Row>
    </div>
  );
}
