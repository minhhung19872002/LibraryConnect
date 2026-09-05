import { useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { App, Button, Checkbox, Pagination, Select, Skeleton, Space, Tag } from 'antd';
import { SaveOutlined } from '@ant-design/icons';
import { opacApi, readerApi, type SearchParams } from '@/api/opac';
import { Hero } from '@/components/Hero';
import { ResultList } from '@/components/ResultList';
import { SCOPE_OPTIONS } from '@/components/searchScopes';
import { useAuthStore } from '@/stores/authStore';
import type { FacetGroup, PagedResult, SearchResult, SearchScope, SortOrder } from '@/types/api';

const SORT_OPTIONS: { value: SortOrder; label: string }[] = [
  { value: 'Relevance', label: 'Phù hợp nhất' },
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
 * Danh mục tự tạo (II.9) đến dưới dạng nhóm có mã "custom:<mã danh mục>" và đi cùng một tham số
 * lọc duy nhất. Thư viện tự khai bao nhiêu danh mục cũng được, nên không thể liệt kê sẵn ở bảng
 * trên; nhận diện bằng tiền tố.
 */
const CUSTOM_PREFIX = 'custom:';
const CUSTOM_PARAM = 'customIndexValueId';

const facetParam = (code: string) =>
  code.startsWith(CUSTOM_PREFIX) ? CUSTOM_PARAM : FACET_PARAM[code];

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
  const documentTypeId = params.get('documentTypeId');

  const searchParams = useMemo<SearchParams>(() => {
    const filter: SearchParams['filter'] = {};

    if (params.get('documentTypeId')) filter.documentTypeId = params.get('documentTypeId')!;
    if (params.get('authorId')) filter.authorId = params.get('authorId')!;
    if (params.get('subjectId')) filter.subjectId = params.get('subjectId')!;
    if (params.get('languageId')) filter.languageId = params.get('languageId')!;
    if (params.get('warehouseId')) filter.warehouseId = params.get('warehouseId')!;
    if (params.get('collectionId')) filter.collectionId = params.get('collectionId')!;
    // Duyệt theo môn học dẫn sang đây; bộ lọc đã có ở máy chủ từ phân hệ X, chỉ thiếu chỗ đọc ra.
    if (params.get('courseId')) filter.courseId = params.get('courseId')!;
    if (params.get('customIndexValueId')) filter.customIndexValueId = params.get('customIndexValueId')!;
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

  const total = results.data?.totalCount ?? 0;

  return (
    <>
      <Hero
        compact
        keyword={keyword}
        scope={scope}
        documentTypeId={documentTypeId}
        onPickDocumentType={(id) => update({ documentTypeId: id })}
      />

      <div className="lc-container lc-results">
        <aside className="lc-paper lc-facets">
          <div className="lc-facets__title">Thu hẹp kết quả</div>

          <div className="lc-facets__list">
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
          </div>

          {facets.isLoading ? <Skeleton active paragraph={{ rows: 6 }} /> : null}

          {(facets.data ?? [])
            .filter((group) => group.code !== 'availability' && group.values.length > 0)
            .map((group) => (
              <div key={group.code}>
                {/* Chữ hoa nhỏ: tách hẳn nhãn nhóm ra khỏi danh sách giá trị ngay bên dưới, mà
                    không phải kẻ thêm đường nào giữa sáu bảy nhóm bộ lọc. */}
                <div className="lc-nhan-nhom" style={{ margin: '16px 0 6px' }}>
                  {group.name}
                </div>
                <div className="lc-facets__list">
                  {group.values.map((value) => {
                    const param = facetParam(group.code);
                    const active = param ? params.get(param) === value.id : false;

                    return (
                      <span
                        key={`${group.code}-${value.id}`}
                        role="button"
                        tabIndex={0}
                        className={['lc-facets__row', active ? 'lc-facets__row--active' : ''].join(' ')}
                        onClick={() =>
                          param ? update({ [param]: active ? null : (value.id ?? null) }) : undefined
                        }
                        onKeyDown={(event) => {
                          if (event.key === 'Enter' && param) {
                            update({ [param]: active ? null : (value.id ?? null) });
                          }
                        }}
                      >
                        <span>{value.label}</span>
                        <span className="lc-facets__count">
                          {value.count.toLocaleString('vi-VN')}
                        </span>
                      </span>
                    );
                  })}
                </div>
              </div>
            ))}
        </aside>

        <div>
          <div className="lc-results__bar">
            <span className="lc-results__count">
              {results.data ? (
                <>
                  {results.data.totalCountCapped ? 'Tìm thấy hơn ' : 'Tìm thấy '}
                  <b>{total.toLocaleString('vi-VN')}</b> tài liệu
                </>
              ) : (
                'Đang tra cứu…'
              )}
            </span>
            <Space>
              {user ? (
                <Button size="small" icon={<SaveOutlined />} onClick={saveSearch}>
                  Lưu tìm kiếm
                </Button>
              ) : null}
              <Select
                size="small"
                aria-label="Sắp xếp kết quả"
                value={sort}
                options={SORT_OPTIONS}
                onChange={(value) => update({ sort: value })}
                style={{ width: 170 }}
              />
            </Space>
          </div>

          {keyword ||
          [...params.keys()].some(
            (key) => Object.values(FACET_PARAM).includes(key) || key === CUSTOM_PARAM,
          ) ? (
            <Space size={[8, 8]} wrap style={{ marginBottom: 12 }}>
              {keyword ? (
                <Tag closable onClose={() => update({ keyword: null })}>
                  {SCOPE_OPTIONS.find((option) => option.value === scope)?.label}: {keyword}
                </Tag>
              ) : null}
              {[...params.entries()]
                .filter(
                  ([key]) => Object.values(FACET_PARAM).includes(key) || key === CUSTOM_PARAM,
                )
                .map(([key, value]) => (
                  <Tag key={key} closable onClose={() => update({ [key]: null })}>
                    Bộ lọc đang áp dụng
                    {key === 'publishYear' ? `: năm ${value}` : ''}
                  </Tag>
                ))}
            </Space>
          ) : null}

          <ResultList
            items={results.data?.items ?? []}
            loading={results.isLoading}
            emptyText={
              keyword
                ? `Không tìm thấy tài liệu cho "${keyword}".`
                : 'Không tìm thấy tài liệu nào phù hợp.'
            }
          />

          {results.data && total > 0 ? (
            <div style={{ textAlign: 'right', marginTop: 16 }}>
              <Pagination
                current={results.data.page}
                pageSize={results.data.pageSize}
                total={total}
                showSizeChanger={false}
                onChange={(value) => update({ page: String(value) })}
              />
            </div>
          ) : null}
        </div>
      </div>
    </>
  );
}
