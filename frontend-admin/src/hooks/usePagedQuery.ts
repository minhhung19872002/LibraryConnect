import { useCallback, useMemo, useState } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import type { TablePaginationConfig } from 'antd';
import type { SorterResult } from 'antd/es/table/interface';
import { api } from '@/api/client';
import type { PagedRequest, PagedResult } from '@/types/api';
import { messages } from '@/i18n/messages';

interface UsePagedQueryOptions<TItem, TFilter> {
  /** Cache key prefix; the current filter is appended so each filter combination caches separately. */
  queryKey: string;
  url: string;
  initialFilter?: TFilter;
  pageSize?: number;
  enabled?: boolean;
  /**
   * Nhịp tự hỏi lại máy chủ, tính bằng mili giây; `false` là thôi hỏi.
   *
   * Dạng hàm nhận trang hiện tại, để màn hình chỉ hỏi lại khi còn việc đang chạy — xem
   * `backupPollInterval` và `harvestPollInterval`.
   */
  refetchInterval?: number | false | ((items: TItem[] | undefined) => number | false);
}

/**
 * Backs every list screen: server-side paging, sorting and filtering wired to an Ant Design table.
 *
 * The previous page stays on screen while the next one loads, so paging through a long list does not
 * flash an empty table at the librarian — which matters at the circulation desk.
 */
export function usePagedQuery<TItem, TFilter extends object = Record<string, never>>({
  queryKey,
  url,
  initialFilter,
  pageSize = 20,
  enabled = true,
  refetchInterval,
}: UsePagedQueryOptions<TItem, TFilter>) {
  const [request, setRequest] = useState<PagedRequest & Partial<TFilter>>({
    page: 1,
    pageSize,
    ...(initialFilter ?? {}),
  } as PagedRequest & Partial<TFilter>);

  const query = useQuery({
    queryKey: [queryKey, request],
    queryFn: () => api.get<PagedResult<TItem>>(url, { params: request }),
    placeholderData: keepPreviousData,
    enabled,
    refetchInterval:
      typeof refetchInterval === 'function'
        ? (query) => refetchInterval(query.state.data?.items)
        : refetchInterval,
  });

  /** Replaces the filter and returns to page 1, since the old page number no longer means anything. */
  const applyFilter = useCallback((filter: Partial<TFilter> & { keyword?: string }) => {
    setRequest((current) => ({ ...current, ...filter, page: 1 }));
  }, []);

  const resetFilter = useCallback(() => {
    setRequest({ page: 1, pageSize, ...(initialFilter ?? {}) } as PagedRequest & Partial<TFilter>);
  }, [initialFilter, pageSize]);

  const handleTableChange = useCallback(
    (pagination: TablePaginationConfig, _filters: unknown, sorter: SorterResult<TItem> | SorterResult<TItem>[]) => {
      const single = Array.isArray(sorter) ? sorter[0] : sorter;

      setRequest((current) => ({
        ...current,
        page: pagination.current ?? 1,
        pageSize: pagination.pageSize ?? current.pageSize,
        sortBy: single?.order ? String(single.field) : undefined,
        sortDescending: single?.order === 'descend',
      }));
    },
    [],
  );

  const pagination = useMemo<TablePaginationConfig>(
    () => ({
      current: query.data?.page ?? request.page,
      pageSize: query.data?.pageSize ?? request.pageSize,
      total: query.data?.totalCount ?? 0,
      showSizeChanger: true,
      pageSizeOptions: [10, 20, 50, 100],
      showTotal: (total) => messages.table.total(total),
    }),
    [query.data, request.page, request.pageSize],
  );

  return {
    request,
    items: query.data?.items ?? [],
    total: query.data?.totalCount ?? 0,
    isLoading: query.isLoading,
    isFetching: query.isFetching,
    error: query.error,
    refetch: query.refetch,
    applyFilter,
    resetFilter,
    handleTableChange,
    pagination,
  };
}
