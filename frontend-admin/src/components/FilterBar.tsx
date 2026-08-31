import type { ReactNode } from 'react';
import { Button, Card, Space } from 'antd';
import { ReloadOutlined, SearchOutlined } from '@ant-design/icons';
import { messages } from '@/i18n/messages';

interface FilterBarProps {
  children: ReactNode;
  onSearch: () => void;
  onReset: () => void;
  loading?: boolean;
  /** Extra buttons on the right, e.g. an export action that depends on the current filter. */
  extra?: ReactNode;
}

/**
 * Filter strip that sits above every list screen. Section 6.6 asks for the same layout everywhere —
 * filters on top, table in the middle, pagination below — and this is the top band of that shape.
 */
export function FilterBar({ children, onSearch, onReset, loading, extra }: FilterBarProps) {
  return (
    <Card
      className="lc-filter-bar"
      variant="borderless"
      styles={{ body: { padding: 12 } }}
    >
      <form
        className="lc-filter-form"
        onSubmit={(event) => {
          // Enter anywhere in the filter strip runs the search, which is how staff work at speed.
          event.preventDefault();
          onSearch();
        }}
      >
        <div className="lc-filter-fields">{children}</div>

        <Space>
          {extra}
          <Button icon={<ReloadOutlined />} onClick={onReset} disabled={loading}>
            {messages.actions.reset}
          </Button>
          <Button type="primary" icon={<SearchOutlined />} htmlType="submit" loading={loading}>
            {messages.actions.search}
          </Button>
        </Space>
      </form>
    </Card>
  );
}
