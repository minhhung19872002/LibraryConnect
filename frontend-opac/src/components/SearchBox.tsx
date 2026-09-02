import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { AutoComplete, Button, Input, Select } from 'antd';
import { opacApi } from '@/api/opac';
import { SCOPE_OPTIONS } from '@/components/searchScopes';
import type { SearchScope } from '@/types/api';

/**
 * Ô tìm kiếm dùng ở trang chủ và ở đầu trang kết quả: phạm vi, ô gõ, nút "Tra cứu" vàng đồng —
 * ba mảnh dính liền trong một khung bo tròn theo bản thiết kế.
 *
 * Gợi ý chỉ gọi máy chủ khi đã gõ từ hai ký tự trở lên và sau một nhịp dừng ngắn — gõ tới đâu gọi
 * tới đó thì một câu tìm kiếm mười chữ thành mười lượt truy vấn toàn kho.
 */
export function SearchBox({
  initialKeyword = '',
  initialScope = 'All',
  extraParams,
}: {
  initialKeyword?: string;
  initialScope?: SearchScope;
  /** Tham số giữ nguyên khi tìm lại, ví dụ dạng tài liệu đang chọn. */
  extraParams?: Record<string, string>;
}) {
  const navigate = useNavigate();
  const [keyword, setKeyword] = useState(initialKeyword);
  const [scope, setScope] = useState<SearchScope>(initialScope);
  const [options, setOptions] = useState<{ value: string; label: string }[]>([]);
  const [timer, setTimer] = useState<ReturnType<typeof setTimeout> | null>(null);

  const submit = (value: string) => {
    const term = value.trim();
    const params = new URLSearchParams(extraParams);

    if (term) params.set('keyword', term);
    if (scope !== 'All') params.set('scope', scope);

    navigate(`/tra-cuu?${params.toString()}`);
  };

  const onType = (value: string) => {
    setKeyword(value);

    if (timer) clearTimeout(timer);

    if (value.trim().length < 2) {
      setOptions([]);
      return;
    }

    setTimer(
      setTimeout(async () => {
        try {
          const suggestions = await opacApi.suggest(value.trim());
          setOptions(
            suggestions.map((item) => ({
              value: item.text,
              label: `${item.text} — ${item.type}`,
            })),
          );
        } catch {
          // Gợi ý hỏng thì im lặng bỏ qua: người dùng vẫn gõ và bấm tìm được như thường.
          setOptions([]);
        }
      }, 300),
    );
  };

  return (
    <div className="lc-searchbox" role="search">
      <Select
        size="large"
        value={scope}
        options={SCOPE_OPTIONS}
        onChange={setScope}
        style={{ width: 150 }}
        aria-label="Phạm vi tra cứu"
      />
      <AutoComplete
        value={keyword}
        options={options}
        onChange={onType}
        onSelect={(value: string) => {
          setKeyword(value);
          submit(value);
        }}
      >
        <Input
          size="large"
          placeholder="Nhập nhan đề, tác giả, từ khóa…"
          onPressEnter={() => submit(keyword)}
          allowClear
          aria-label="Từ khóa tra cứu"
        />
      </AutoComplete>
      <Button size="large" type="primary" onClick={() => submit(keyword)}>
        Tra cứu
      </Button>
    </div>
  );
}
