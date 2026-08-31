import type { ReactNode } from 'react';
import { Space, Typography } from 'antd';

interface PageHeaderProps {
  title: string;
  description?: string;
  /** Primary and secondary actions, rendered right-aligned on the same line as the title. */
  actions?: ReactNode;
}

/**
 * Title block every business screen starts with. Keeping it in one component is what makes the
 * heading size, spacing and action placement identical across the eleven subsystems (section 6.6).
 */
export function PageHeader({ title, description, actions }: PageHeaderProps) {
  return (
    <div className="lc-page-header">
      <div>
        <Typography.Title level={4} className="lc-page-title">
          {title}
        </Typography.Title>
        {description && (
          <Typography.Text type="secondary" className="lc-page-description">
            {description}
          </Typography.Text>
        )}
      </div>
      {actions && <Space wrap>{actions}</Space>}
    </div>
  );
}
