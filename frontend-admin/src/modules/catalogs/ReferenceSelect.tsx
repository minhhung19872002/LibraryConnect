import { useQuery } from '@tanstack/react-query';
import { Select } from 'antd';
import { api } from '@/api/client';
import type { PagedResult } from '@/types/api';
import { circulationApi } from '@/modules/circulation/api';
import type { CatalogItem } from './types';
import { MAU } from '@/lib/palette';

/**
 * Mã danh mục đặc biệt: chính sách lưu thông không nằm trong registry danh mục mà ở Phân hệ VII,
 * nhưng loại bạn đọc cần trỏ tới nó (chính sách mặc định, VI.3). Máy chủ chỉ đặt tên, còn nạp từ đâu
 * là việc của đây.
 */
const CIRCULATION_POLICIES = 'circulation-policies';

interface ReferenceOption {
  id: string;
  code?: string | null;
  name: string;
}

function useReferenceItems(catalog: string, enabled = true) {
  return useQuery({
    queryKey: ['catalog-reference', catalog],
    queryFn: async (): Promise<ReferenceOption[]> => {
      if (catalog === CIRCULATION_POLICIES) {
        const policies = await circulationApi.policies();
        return policies.map((policy) => ({ id: policy.id, name: policy.name }));
      }

      const page = await api.get<PagedResult<CatalogItem>>(`/catalogs/${catalog}/items`, {
        params: { page: 1, pageSize: 500, isActive: true },
      });

      return page.items;
    },
    staleTime: 5 * 60 * 1000,
    enabled,
  });
}

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
  const items = useReferenceItems(catalog);

  return (
    <Select
      allowClear
      showSearch
      optionFilterProp="label"
      loading={items.isLoading}
      value={value}
      onChange={onChange}
      placeholder={placeholder}
      options={(items.data ?? []).map((item) => ({
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
  const items = useReferenceItems(catalog, Boolean(value));

  if (!value) {
    return <span style={{ color: MAU.chuMo }}>—</span>;
  }

  const match = (items.data ?? []).find((item) => item.id === value);

  return <span>{match?.name ?? '—'}</span>;
}
