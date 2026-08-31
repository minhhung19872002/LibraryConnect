import { useQuery } from '@tanstack/react-query';
import { api } from '@/api/client';
import type { PagedResult } from '@/types/api';
import type { CatalogItem } from '@/modules/catalogs/types';

/**
 * Nạp một danh mục để đổ vào ô chọn.
 *
 * The lookup lists a cataloguer picks from — document types, languages, collections — are short and
 * change rarely, so they are fetched whole once and cached for the session rather than searched on
 * every keystroke. The page size is capped so a library that has grown a list past that still gets a
 * usable box instead of a slow one.
 */
export function useCatalogOptions(catalog: string, enabled = true) {
  return useQuery({
    queryKey: ['catalog-options', catalog],
    queryFn: async () => {
      const page = await api.get<PagedResult<CatalogItem>>(`/catalogs/${catalog}/items`, {
        params: { pageSize: 500, sortBy: 'sortOrder' },
      });

      return page.items.filter((item) => item.isActive);
    },
    staleTime: 5 * 60 * 1000,
    enabled,
  });
}

/** Chuyển danh sách giá trị danh mục thành options của Ant Design. */
export function toOptions(items: CatalogItem[] | undefined) {
  return (items ?? []).map((item) => ({ value: item.id, label: item.name }));
}
