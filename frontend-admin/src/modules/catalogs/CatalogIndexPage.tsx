import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import { Card, Col, Empty, Row, Space, Spin, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { api } from '@/api/client';
import { PageHeader } from '@/components/PageHeader';
import { messages } from '@/i18n/messages';
import type { CatalogMetadata } from './types';

/**
 * Trang tổng hợp toàn bộ danh mục nghiệp vụ.
 *
 * The list comes from the backend registry, so a catalogue added to the system appears here on its
 * own. Grouping matches how the work is divided in the library rather than how the tables are laid
 * out in the database.
 */
export function CatalogIndexPage() {
  const catalogs = useQuery({
    queryKey: ['catalogs'],
    queryFn: () => api.get<CatalogMetadata[]>('/catalogs'),
  });

  const groups = useMemo(() => groupCatalogs(catalogs.data ?? []), [catalogs.data]);

  return (
    <div className="lc-page">
      <PageHeader
        title={messages.menu.catalogs}
        description="Các danh mục nghiệp vụ dùng chung cho toàn hệ thống. Mọi danh mục đều thêm, sửa, nhập và xuất được từ giao diện, không có giá trị nào viết cứng trong mã nguồn."
      />

      <Spin spinning={catalogs.isLoading}>
        {groups.length === 0 && !catalogs.isLoading ? (
          <Empty description={messages.table.empty} />
        ) : (
          groups.map((group) => (
            <div key={group.title} className="lc-catalog-group">
              <Typography.Title level={5}>{group.title}</Typography.Title>

              <Row gutter={[16, 16]}>
                {group.items.map((catalog) => (
                  <Col key={catalog.code} xs={24} sm={12} lg={8} xxl={6}>
                    <Link to={`/danh-muc/${catalog.code}`}>
                      <Card hoverable className="lc-catalog-card" variant="borderless">
                        <Space direction="vertical" size={4} style={{ width: '100%' }}>
                          <Space size={6} wrap>
                            <Typography.Text strong>{catalog.pluralName}</Typography.Text>
                            {catalog.isHierarchical && <Tag color="purple">Phân cấp</Tag>}
                            {catalog.supportsMerge && <Tag color="blue">Gộp trùng</Tag>}
                          </Space>

                          <Typography.Text type="secondary" className="lc-catalog-card-description">
                            {catalog.description ?? `Quản lý danh mục ${catalog.singularName}.`}
                          </Typography.Text>
                        </Space>
                      </Card>
                    </Link>
                  </Col>
                ))}
              </Row>
            </div>
          ))
        )}
      </Spin>
    </div>
  );
}

interface CatalogGroup {
  title: string;
  items: CatalogMetadata[];
}

/** Groups the catalogues the way a librarian thinks about them, not the way the schema is split. */
function groupCatalogs(catalogs: CatalogMetadata[]): CatalogGroup[] {
  const membership: Record<string, string[]> = {
    'Biên mục': [
      'document-types',
      'carrier-types',
      'languages',
      'countries',
      'publishers',
      'authors',
      'subjects',
      'keywords',
      'classifications',
      'series',
      'collections',
    ],
    'Bạn đọc và đào tạo': ['reader-types', 'faculties', 'majors', 'courses', 'violation-types'],
    'Bổ sung': ['suppliers', 'funding-sources'],
    'Tài liệu số và nội dung': ['digital-collections', 'news-categories'],
  };

  const byCode = new Map(catalogs.map((catalog) => [catalog.code, catalog]));
  const groups: CatalogGroup[] = [];
  const placed = new Set<string>();

  for (const [title, codes] of Object.entries(membership)) {
    const items = codes
      .map((code) => byCode.get(code))
      .filter((catalog): catalog is CatalogMetadata => catalog !== undefined);

    items.forEach((catalog) => placed.add(catalog.code));

    if (items.length > 0) {
      groups.push({ title, items });
    }
  }

  // A catalogue added to the backend but not yet placed in a group still shows up, rather than
  // silently disappearing from the screen.
  const ungrouped = catalogs.filter((catalog) => !placed.has(catalog.code));

  if (ungrouped.length > 0) {
    groups.push({ title: 'Danh mục khác', items: ungrouped });
  }

  return groups;
}
