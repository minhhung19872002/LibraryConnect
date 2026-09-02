import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { opacApi } from '@/api/opac';
import { SearchBox } from '@/components/SearchBox';
import { useSiteSettings } from '@/hooks/useSite';
import type { FacetGroup, HomePayload, SearchScope } from '@/types/api';

/** Câu chào mặc định khi thư viện chưa đặt khẩu hiệu — không nhắc tên khách hàng nào. */
const DEFAULT_HEADLINE = 'Tri thức của thư viện, trong tầm tay bạn';

/** Số viên chọn nhanh dạng tài liệu dưới ô tra cứu: bốn là vừa một hàng ở khổ 760px. */
const PILL_COUNT = 4;

/**
 * Khối tra cứu nền xanh rêu ở đầu trang chủ và trang kết quả.
 *
 * Câu chào lấy từ khẩu hiệu thư viện; dòng dưới nói kho có bao nhiêu tài liệu — con số thật đọc
 * từ thống kê chứ không phải câu quảng cáo. Hàng viên dưới ô tra cứu là những dạng tài liệu nhiều
 * biểu ghi nhất, để bạn đọc khoanh vùng trước khi gõ.
 */
export function Hero({
  compact = false,
  keyword = '',
  scope = 'All',
  documentTypeId = null,
  onPickDocumentType,
}: {
  compact?: boolean;
  keyword?: string;
  scope?: SearchScope;
  documentTypeId?: string | null;
  /** Không truyền thì bấm viên là chuyển sang trang kết quả với dạng tài liệu ấy. */
  onPickDocumentType?: (id: string | null) => void;
}) {
  const navigate = useNavigate();
  const { data: settings } = useSiteSettings();

  const home = useQuery<HomePayload>({
    queryKey: ['home'],
    queryFn: () => opacApi.home(),
    staleTime: 5 * 60 * 1000,
  });

  const facets = useQuery<FacetGroup[]>({
    queryKey: ['facets', 'toan-kho'],
    queryFn: () =>
      opacApi.facets({
        keyword: '',
        scope: 'All',
        sort: 'Relevance',
        page: 1,
        pageSize: 1,
        filter: {},
      }),
    staleTime: 10 * 60 * 1000,
  });

  const documentTypes = (facets.data ?? [])
    .find((group) => group.code === 'documentType')
    ?.values.filter((value) => value.id)
    .slice(0, PILL_COUNT) ?? [];

  const pick = (id: string | null) => {
    if (onPickDocumentType) {
      onPickDocumentType(id);
      return;
    }

    navigate(id ? `/tra-cuu?documentTypeId=${id}` : '/tra-cuu');
  };

  const bibCount = home.data?.statistics.bibCount;

  return (
    <section
      className={['lc-hero', compact ? 'lc-hero--compact' : ''].join(' ')}
      style={
        settings?.heroImageUrl
          ? {
              backgroundImage: `linear-gradient(rgba(34, 48, 31, 0.86), rgba(34, 48, 31, 0.94)), url(${settings.heroImageUrl})`,
            }
          : undefined
      }
    >
      <div className="lc-hero__inner">
        {compact ? null : (
          <>
            <h1 className="lc-hero__title">{settings?.slogan || DEFAULT_HEADLINE}</h1>
            <p className="lc-hero__subtitle">
              {bibCount
                ? `Tra cứu ${bibCount.toLocaleString('vi-VN')} tài liệu in và số — gõ không dấu vẫn tìm thấy.`
                : 'Tra cứu tài liệu in và số của thư viện — gõ không dấu vẫn tìm thấy.'}
            </p>
          </>
        )}

        <SearchBox
          initialKeyword={keyword}
          initialScope={scope}
          extraParams={documentTypeId ? { documentTypeId } : undefined}
        />

        {documentTypes.length > 0 ? (
          <div className="lc-hero__pills">
            <button
              type="button"
              className={['lc-pill-btn', documentTypeId ? '' : 'lc-pill-btn--active'].join(' ')}
              onClick={() => pick(null)}
            >
              Tất cả
            </button>
            {documentTypes.map((value) => (
              <button
                key={value.id}
                type="button"
                className={['lc-pill-btn', documentTypeId === value.id ? 'lc-pill-btn--active' : ''].join(' ')}
                onClick={() => pick(documentTypeId === value.id ? null : value.id!)}
              >
                {value.label}
              </button>
            ))}
          </div>
        ) : null}
      </div>
    </section>
  );
}
