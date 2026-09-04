import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Card,
  Col,
  Empty,
  Input,
  List,
  Pagination,
  Result,
  Row,
  Skeleton,
  Space,
  Tag,
  Typography,
} from 'antd';
import { opacApi } from '@/api/opac';
import { useSiteSettings } from '@/hooks/useSite';
import { formatDate } from '@/lib/datetime';

const { Paragraph, Title } = Typography;

/** IX.1 — Danh sách tin tức, lọc theo chuyên mục. */
export function NewsListPage() {
  const [page, setPage] = useState(1);
  const [categoryId, setCategoryId] = useState<string | undefined>();
  const [keyword, setKeyword] = useState('');
  const { data: settings } = useSiteSettings();

  const pageSize = settings?.newsPerPage ?? 9;

  const news = useQuery({
    queryKey: ['news', page, categoryId, keyword, pageSize],
    queryFn: () => opacApi.news({ page, pageSize, categoryId, keyword: keyword || undefined }),
  });

  const categories = useQuery({
    queryKey: ['news-categories'],
    queryFn: () => opacApi.newsCategories(),
  });

  return (
    <div className="lc-container" style={{ padding: '24px 16px 48px' }}>
      <Row gutter={24}>
        <Col xs={24} md={18}>
          <Card
            title="Tin tức – Sự kiện"
            extra={
              <Input.Search
                placeholder="Tìm tin"
                allowClear
                style={{ width: 220 }}
                onSearch={(value) => {
                  setKeyword(value);
                  setPage(1);
                }}
              />
            }
          >
            <List
              loading={news.isLoading}
              dataSource={news.data?.items ?? []}
              locale={{ emptyText: <Empty description="Chưa có bản tin nào." /> }}
              renderItem={(item) => (
                <List.Item key={item.id}>
                  <List.Item.Meta
                    avatar={
                      item.thumbnailUrl ? (
                        <img
                          src={item.thumbnailUrl}
                          alt={item.title}
                          style={{ width: 132, height: 88, objectFit: 'cover', borderRadius: 6 }}
                        />
                      ) : undefined
                    }
                    title={<Link to={`/tin-tuc/${item.slug}`}>{item.title}</Link>}
                    description={
                      <>
                        <Space size={[8, 4]} wrap style={{ marginBottom: 4 }}>
                          {item.categoryName ? <Tag>{item.categoryName}</Tag> : null}
                          {item.isFeatured ? <Tag color="green">Nổi bật</Tag> : null}
                          <span style={{ fontSize: 12, color: 'var(--lc-muted)' }}>
                            {item.publishedAt
                              ? formatDate(item.publishedAt)
                              : ''}
                          </span>
                        </Space>
                        <Paragraph ellipsis={{ rows: 2 }} style={{ marginBottom: 0 }}>
                          {item.summary}
                        </Paragraph>
                      </>
                    }
                  />
                </List.Item>
              )}
            />

            {news.data && news.data.totalCount > 0 ? (
              <div style={{ textAlign: 'right', marginTop: 16 }}>
                <Pagination
                  current={news.data.page}
                  pageSize={news.data.pageSize}
                  total={news.data.totalCount}
                  showSizeChanger={false}
                  onChange={setPage}
                />
              </div>
            ) : null}
          </Card>
        </Col>

        <Col xs={24} md={6}>
          <Card title="Chuyên mục" loading={categories.isLoading}>
            <Space direction="vertical" style={{ width: '100%' }}>
              <a
                onClick={() => {
                  setCategoryId(undefined);
                  setPage(1);
                }}
                style={{ fontWeight: categoryId ? 400 : 600 }}
              >
                Tất cả
              </a>
              {(categories.data ?? []).map((category) => (
                <a
                  key={category.id}
                  onClick={() => {
                    setCategoryId(category.id);
                    setPage(1);
                  }}
                  style={{ fontWeight: categoryId === category.id ? 600 : 400 }}
                >
                  {category.name}{' '}
                  <span style={{ color: 'var(--lc-muted)' }}>({category.newsCount})</span>
                </a>
              ))}
            </Space>
          </Card>
        </Col>
      </Row>
    </div>
  );
}

/** IX.1 — Một bản tin. */
export function NewsDetailPage() {
  const { slug = '' } = useParams();

  const { data, isLoading, isError } = useQuery({
    queryKey: ['news', slug],
    queryFn: () => opacApi.newsDetail(slug),
    enabled: Boolean(slug),
  });

  if (isLoading) {
    return (
      <div className="lc-container" style={{ padding: 24 }}>
        <Skeleton active paragraph={{ rows: 10 }} />
      </div>
    );
  }

  if (isError || !data) {
    return (
      <Result
        status="404"
        title="Không tìm thấy bản tin"
        subTitle="Bản tin có thể đã bị gỡ hoặc đường dẫn không đúng."
        extra={<Link to="/tin-tuc">Về trang tin tức</Link>}
      />
    );
  }

  return (
    <div className="lc-container" style={{ padding: '24px 16px 48px', maxWidth: 900 }}>
      <Card>
        <Title level={2} style={{ marginTop: 0 }}>
          {data.title}
        </Title>
        <Space size={[8, 4]} wrap style={{ marginBottom: 16 }}>
          {data.categoryName ? <Tag>{data.categoryName}</Tag> : null}
          <span style={{ color: 'var(--lc-muted)' }}>
            {formatDate(data.publishedAt)}
            {data.author ? ` • ${data.author}` : ''} • {data.viewCount} lượt xem
          </span>
        </Space>

        {data.thumbnailUrl ? (
          <img
            src={data.thumbnailUrl}
            alt={data.title}
            style={{ width: '100%', borderRadius: 8, marginBottom: 16 }}
          />
        ) : null}

        <div
          className="lc-richtext"
          // Nội dung đã được máy chủ lọc sạch thẻ nguy hiểm ngay khi cán bộ bấm lưu (yêu cầu 6.4),
          // nên tới đây chỉ còn thẻ định dạng an toàn.
          dangerouslySetInnerHTML={{ __html: data.content ?? '' }}
        />
      </Card>

      {data.related.length > 0 ? (
        <Card title="Tin liên quan" style={{ marginTop: 24 }}>
          <List
            dataSource={data.related}
            renderItem={(item) => (
              <List.Item>
                <List.Item.Meta
                  title={<Link to={`/tin-tuc/${item.slug}`}>{item.title}</Link>}
                  description={
                    item.publishedAt
                      ? formatDate(item.publishedAt)
                      : undefined
                  }
                />
              </List.Item>
            )}
          />
        </Card>
      ) : null}
    </div>
  );
}

/** IX.1 — Trang tĩnh: Giới thiệu, Nội quy, Hướng dẫn, Liên hệ, Hỏi đáp. */
export function StaticPageView() {
  const { slug = '' } = useParams();
  const { data: settings } = useSiteSettings();

  const { data, isLoading, isError } = useQuery({
    queryKey: ['page', slug],
    queryFn: () => opacApi.page(slug),
    enabled: Boolean(slug),
  });

  if (isLoading) {
    return (
      <div className="lc-container" style={{ padding: 24 }}>
        <Skeleton active paragraph={{ rows: 10 }} />
      </div>
    );
  }

  if (isError || !data) {
    return (
      <Result
        status="404"
        title="Không tìm thấy trang"
        subTitle="Trang có thể đã bị gỡ hoặc đường dẫn không đúng."
        extra={<Link to="/">Về trang chủ</Link>}
      />
    );
  }

  const isContact = slug === 'lien-he';

  return (
    <div className="lc-container" style={{ padding: '24px 16px 48px', maxWidth: 960 }}>
      <Card>
        <Title level={2} style={{ marginTop: 0 }}>
          {data.title}
        </Title>
        <div
          className="lc-richtext"
          dangerouslySetInnerHTML={{ __html: data.content ?? '' }}
        />

        {isContact && settings ? (
          <>
            <Card type="inner" title="Thông tin liên hệ" style={{ marginTop: 24 }}>
              <Space direction="vertical">
                <div>
                  <b>{settings.libraryName}</b>
                </div>
                {settings.address ? <div>Địa chỉ: {settings.address}</div> : null}
                {settings.phone ? <div>Điện thoại: {settings.phone}</div> : null}
                {settings.email ? <div>Email: {settings.email}</div> : null}
                {settings.openingHours
                  ? settings.openingHours.split('\n').map((line) => <div key={line}>{line}</div>)
                  : null}
                {settings.contactNote ? <div>{settings.contactNote}</div> : null}
              </Space>
            </Card>

            {settings.branches.length > 0 ? (
              <Card type="inner" title="Các cơ sở" style={{ marginTop: 16 }}>
                <Space direction="vertical" size={16} style={{ width: '100%' }}>
                  {settings.branches.map((branch) => (
                    <div key={branch.id}>
                      <div>
                        <b>{branch.name}</b>
                        {branch.isHeadquarters ? ' (Trụ sở chính)' : ''}
                      </div>
                      {branch.address ? <div>Địa chỉ: {branch.address}</div> : null}
                      {branch.phone ? <div>Điện thoại: {branch.phone}</div> : null}
                      {branch.openingHours ? <div>Giờ mở cửa: {branch.openingHours}</div> : null}
                      {branch.latitude != null && branch.longitude != null ? (
                        <a
                          href={`https://www.google.com/maps/dir/?api=1&destination=${branch.latitude},${branch.longitude}`}
                          target="_blank"
                          rel="noreferrer"
                        >
                          Chỉ đường tới cơ sở này
                        </a>
                      ) : null}
                    </div>
                  ))}
                </Space>
              </Card>
            ) : null}

            {settings.mapEmbedUrl ? (
              <iframe
                title="Bản đồ đường tới thư viện"
                src={settings.mapEmbedUrl}
                style={{ width: '100%', height: 320, border: 0, marginTop: 16, borderRadius: 8 }}
                loading="lazy"
              />
            ) : null}
          </>
        ) : null}
      </Card>
    </div>
  );
}
