import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { AutoComplete, Button, Input, Select, Space } from 'antd';
import { SearchOutlined } from '@ant-design/icons';
import { opacApi } from '@/api/opac';
import { SCOPE_OPTIONS } from '@/components/searchScopes';
import type { SearchScope } from '@/types/api';


/**
 * Ô tìm kiếm dùng ở trang chủ và ở đầu trang kết quả.
 *
 * Gợi ý chỉ gọi máy chủ khi đã gõ từ hai ký tự trở lên và sau một nhịp dừng ngắn — gõ tới đâu gọi
 * tới đó thì một câu tìm kiếm mười chữ thành mười lượt truy vấn toàn kho.
 */
export function SearchBox({
  size = 'large',
  initialKeyword = '',
  initialScope = 'All',
}: {
  size?: 'middle' | 'large';
  initialKeyword?: string;
  initialScope?: SearchScope;
}) {
  const navigate = useNavigate();
  const [keyword, setKeyword] = useState(initialKeyword);
  const [scope, setScope] = useState<SearchScope>(initialScope);
  const [options, setOptions] = useState<{ value: string; label: string }[]>([]);
  const [timer, setTimer] = useState<ReturnType<typeof setTimeout> | null>(null);

  const submit = (value: string) => {
    const term = value.trim();
    const params = new URLSearchParams();

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
    <Space.Compact style={{ width: '100%' }}>
      <Select
        size={size}
        value={scope}
        options={SCOPE_OPTIONS}
        onChange={setScope}
        style={{ width: 160, flex: 'none' }}
      />
      <AutoComplete
        value={keyword}
        options={options}
        onChange={onType}
        onSelect={(value: string) => {
          setKeyword(value);
          submit(value);
        }}
        style={{ width: '100%' }}
      >
        <Input
          size={size}
          placeholder="Nhập nhan đề, tác giả, chủ đề… (gõ không dấu vẫn tìm được)"
          onPressEnter={() => submit(keyword)}
          allowClear
        />
      </AutoComplete>
      <Button size={size} type="primary" icon={<SearchOutlined />} onClick={() => submit(keyword)}>
        Tìm kiếm
      </Button>
    </Space.Compact>
  );
}
