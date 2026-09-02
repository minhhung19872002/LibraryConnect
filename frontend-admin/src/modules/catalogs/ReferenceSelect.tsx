import { useQuery } from '@tanstack/react-query';
import { Select } from 'antd';
import { api } from '@/api/client';
import type { PagedResult } from '@/types/api';
import type { CatalogItem } from './types';
import { MAU } from '@/lib/palette';

/**
 * Ô chọn một giá trị của danh mục khác, ví dụ khoa quản lý của một ngành đào tạo.
 *
 * Danh sách nạp thẳng từ chính endpoint danh mục nên không có bản sao dữ liệu nào ở đây; máy chủ
 * chỉ nói tên danh mục cần nạp, còn nội dung luôn là nội dung mới nhất.
 */
export function ReferenceSelect({
  catalog,
  value,
  onChange,
  placeholder,
}: {
  catalog: string;
  value?: string;
  onChange?: (value: string | undefined) => void;
  placeholder?: string;
}) {
  const items = useQuery({
    queryKey: ['catalog-reference', catalog],
    queryFn: () =>
      api.get<PagedResult<CatalogItem>>(`/catalogs/${catalog}/items`, {
        params: { page: 1, pageSize: 500, isActive: true },
      }),
    staleTime: 5 * 60 * 1000,
  });

  return (
    <Select
      allowClear
      showSearch
      optionFilterProp="label"
      loading={items.isLoading}
      value={value}
      onChange={onChange}
      placeholder={placeholder}
      options={(items.data?.items ?? []).map((item) => ({
        value: item.id,
        label: item.code ? `${item.code} — ${item.name}` : item.name,
      }))}
    />
  );
}

/**
 * Tên hiển thị của một giá trị được tham chiếu, dùng cho cột trong bảng danh sách.
 *
 * Hiện mã định danh thô ở bảng thì không ai đọc được; nạp danh mục một lần rồi tra tên là đủ, và
 * danh mục được tham chiếu bao giờ cũng ngắn.
 */
export function ReferenceLabel({ catalog, value }: { catalog: string; value?: string | null }) {
  const items = useQuery({
    queryKey: ['catalog-reference', catalog],
    queryFn: () =>
      api.get<PagedResult<CatalogItem>>(`/catalogs/${catalog}/items`, {
        params: { page: 1, pageSize: 500, isActive: true },
      }),
    staleTime: 5 * 60 * 1000,
    enabled: Boolean(value),
  });

  if (!value) {
    return <span style={{ color: MAU.chuMo }}>—</span>;
  }

  const match = (items.data?.items ?? []).find((item) => item.id === value);

  return <span>{match?.name ?? '—'}</span>;
}
