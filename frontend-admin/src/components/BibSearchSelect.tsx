import { useEffect, useMemo, useState } from 'react';
import { Select, Spin } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { api } from '@/api/client';
import type { PagedResult } from '@/types/api';

interface BibHit {
  id: string;
  title: string;
  authorMain?: string | null;
  publishYear?: number | null;
  isbn?: string | null;
}

interface BibSearchSelectProps {
  value?: string | null;
  onChange?: (value: string | null) => void;
  /** Nhan đề của biểu ghi đang gắn, để ô chọn hiện chữ ngay cả khi chưa tìm gì. */
  initialLabel?: string | null;
  placeholder?: string;
  disabled?: boolean;
}

/**
 * Ô chọn biểu ghi thư mục: gõ nhan đề / tác giả / ISBN, chọn một biểu ghi trong kho. Dùng ở mọi chỗ
 * cần "gắn vào biểu ghi" (tài liệu số, bài trích, tài liệu môn học) thay vì mỗi màn hình tự viết.
 * Tìm qua `/cataloging/bibs?keyword=` với độ trễ 300 ms để không gọi máy chủ theo từng phím.
 */
export function BibSearchSelect({
  value,
  onChange,
  initialLabel,
  placeholder = 'Gõ nhan đề, tác giả hoặc ISBN để tìm biểu ghi',
  disabled,
}: BibSearchSelectProps) {
  const [term, setTerm] = useState('');
  const [debounced, setDebounced] = useState('');

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(term.trim()), 300);
    return () => window.clearTimeout(handle);
  }, [term]);

  const hits = useQuery({
    queryKey: ['bib-search-select', debounced],
    queryFn: () =>
      api.get<PagedResult<BibHit>>('/cataloging/bibs', {
        params: { keyword: debounced, page: 1, pageSize: 10 },
      }),
    enabled: debounced.length >= 2,
    staleTime: 30_000,
  });

  const options = useMemo(() => {
    const rows = hits.data?.items ?? [];
    const list = rows.map((row) => ({
      value: row.id,
      label: [row.title, row.authorMain, row.publishYear, row.isbn]
        .filter((part) => part !== null && part !== undefined && `${part}`.length > 0)
        .join(' · '),
    }));
    // Giá trị đang chọn không nằm trong kết quả tìm thì vẫn phải có nhãn để hiện.
    if (value && initialLabel && !list.some((option) => option.value === value)) {
      list.unshift({ value, label: initialLabel });
    }
    return list;
  }, [hits.data, value, initialLabel]);

  return (
    <Select
      allowClear
      showSearch
      disabled={disabled}
      value={value ?? undefined}
      placeholder={placeholder}
      filterOption={false}
      onSearch={setTerm}
      onChange={(next) => onChange?.((next as string | undefined) ?? null)}
      notFoundContent={
        hits.isFetching ? (
          <Spin size="small" />
        ) : debounced.length < 2 ? (
          'Gõ ít nhất 2 ký tự'
        ) : (
          'Không có biểu ghi nào khớp'
        )
      }
      options={options}
      style={{ width: '100%' }}
    />
  );
}
