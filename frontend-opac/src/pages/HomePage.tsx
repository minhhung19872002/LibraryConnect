import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Card, Carousel, Col, Empty, Row, Skeleton, Space, Typography } from 'antd';
import { opacApi } from '@/api/opac';
import { SearchBox } from '@/components/SearchBox';
import { ResultShelf } from '@/components/ResultList';
import { FALLBACK_LIBRARY_NAME, useSiteSettings } from '@/hooks/useSite';
import type { HomePayload } from '@/types/api';

const { Title, Paragraph } = Typography;

/** IX.1 — Trang chủ: ô tìm kiếm lớn, banner, sách mới, sách được mượn nhiều, tin tức, liên kết. */
export function HomePage() {
  const { data: settings } = useSiteSettings();
  const { data, isLoading } = useQuery<HomePayload>({
    queryKey: ['home'],
    queryFn: () => opacApi.home(),
  });

  const libraryName = settings?.libraryName ?? FALLBACK_LIBRARY_NAME;

  return (
    <>
      <section
        className="lc-hero"
        style={
          settings?.heroImageUrl
            ? {
                backgroundImage: `linear-gradient(rgba(11,107,79,.82), rgba(15,42,34,.92)), url(${settings.heroImageUrl})`,
              }
            : undefined
        }
      >
        <div className="lc-container">
          <h1 className="lc-hero__title">{libraryName}</h1>
          <p className="lc-hero__subtitle">
            {settings?.slogan ?? 'Tra cứu tài liệu, tài liệu số và dịch vụ của thư viện'}
          </p>

          <SearchBox />

          <div style={{ marginTop: 12 }}>
            <Space size="middle" wrap>
              <Link to="/tra-cuu-nang-cao" style={{ color: '#fff' }}>
                Tra cứu nâng cao
              </Link>
              <Link to="/duyet/chu-de" style={{ color: '#fff' }}>
                Duyệt theo chủ đề
              </Link>
              <Link to="/duyet/nganh" style={{ color: '#fff' }}>
                Tài liệu theo ngành học
              </Link>
              {settings?.showInterlibrary ? (
                <Link to="/thu-vien-khac" style={{ color: '#fff' }}>
                  Tìm ở thư viện khác
                </Link>
              ) : null}
            </Space>
          </div>

          {data ? (
            <div className="lc-hero__stats">
              <div>
                <div className="lc-hero__stat-value">
                  {data.statistics.bibCount.toLocaleString('vi-VN')}
                </div>
                <div>biểu ghi thư mục</div>
              </div>
              <div>
                <div className="lc-hero__stat-value">
                  {data.statistics.itemCount.toLocaleString('vi-VN')}
                </div>
                <div>bản in trong kho</div>
              </div>
              <div>
                <div className="lc-hero__stat-value">
                  {data.statistics.digitalCount.toLocaleString('vi-VN')}
                </div>
                <div>tài liệu số</div>
              </div>
              <div>
                <div className="lc-hero__stat-value">
                  {data.statistics.readerCount.toLocaleString('vi-VN')}
                </div>
                <div>bạn đọc</div>
              </div>
            </div>
          ) : null}
        </div>
      </section>

      <div className="lc-container" style={{ padding: '24px 16px 48px' }}>
        {isLoading ? <Skeleton active paragraph={{ rows: 8 }} /> : null}

        {data && data.banners.length > 0 ? (
          <Card style={{ marginBottom: 24 }} styles={{ body: { padding: 0 } }}>
            <Carousel autoplay>
              {data.banners.map((banner) => (
                <div key={banner.id}>
                  {banner.link ? (
                    <a href={banner.link} target="_blank" rel="noopener noreferrer">
                      <img
                        src={banner.imageUrl}
                        alt={banner.title}
                        style={{ width: '100%', maxHeight: 320, objectFit: 'cover' }}
                      />
                    </a>
                  ) : (
                    <img
                      src={banner.imageUrl}
                      alt={banner.title}
                      style={{ width: '100%', maxHeight: 320, objectFit: 'cover' }}
                    />
                  )}
                </div>
              ))}
            </Carousel>
          </Card>
        ) : null}

        {data && settings?.showNewBooks !== false ? (
          <Card
            title="Sách mới bổ sung"
            extra={<Link to="/tra-cuu?sort=Newest">Xem tất cả</Link>}
            style={{ marginBottom: 24 }}
          >
            <ResultShelf items={data.newBooks} />
          </Card>
        ) : null}

        {data && settings?.showPopularBooks !== false ? (
          <Card
            title="Được mượn nhiều"
            extra={<Link to="/tra-cuu?sort=Popular">Xem tất cả</Link>}
            style={{ marginBottom: 24 }}
          >
            <ResultShelf items={data.popularBooks} />
          </Card>
        ) : null}

        <Row gutter={[24, 24]}>
          <Col xs={24} lg={16}>
            <Card title="Tin tức – Sự kiện" extra={<Link to="/tin-tuc">Xem tất cả</Link>}>
              {data && data.news.length > 0 ? (
                <Row gutter={[16, 16]}>
                  {data.news.map((news) => (
                    <Col xs={24} sm={12} key={news.id}>
                      <Card size="small" hoverable styles={{ body: { padding: 12 } }}>
                        {news.thumbnailUrl ? (
                          <img
                            src={news.thumbnailUrl}
                            alt={news.title}
                            style={{
                              width: '100%',
                              height: 140,
                              objectFit: 'cover',
                              borderRadius: 6,
                              marginBottom: 8,
                            }}
                          />
                        ) : null}
                        <div style={{ fontWeight: 600, marginBottom: 4 }}>
                          <Link to={`/tin-tuc/${news.slug}`}>{news.title}</Link>
                        </div>
                        <div style={{ fontSize: 12, color: 'var(--lc-muted)' }}>
                          {news.categoryName ? `${news.categoryName} • ` : ''}
                          {news.publishedAt
                            ? new Date(news.publishedAt).toLocaleDateString('vi-VN')
                            : ''}
                        </div>
                        <Paragraph
                          ellipsis={{ rows: 2 }}
                          style={{ marginTop: 6, marginBottom: 0, fontSize: 13 }}
                        >
                          {news.summary}
                        </Paragraph>
                      </Card>
                    </Col>
                  ))}
                </Row>
              ) : (
                <Empty description="Chưa có bản tin nào được đăng." />
              )}
            </Card>
          </Col>

          <Col xs={24} lg={8}>
            <Card title="Liên kết hữu ích">
              {data && data.links.length > 0 ? (
                <Space direction="vertical" size="small" style={{ width: '100%' }}>
                  {data.links.map((link) => (
                    <div key={link.id}>
                      <a href={link.url} target="_blank" rel="noopener noreferrer">
                        {link.name}
                      </a>
                      {link.groupName ? (
                        <div style={{ fontSize: 12, color: 'var(--lc-muted)' }}>
                          {link.groupName}
                        </div>
                      ) : null}
                    </div>
                  ))}
                </Space>
              ) : (
                <Empty description="Chưa khai báo liên kết nào." />
              )}
            </Card>

            {settings?.openingHours ? (
              <Card title="Giờ mở cửa" style={{ marginTop: 24 }}>
                {settings.openingHours.split('\n').map((line) => (
                  <div key={line}>{line}</div>
                ))}
              </Card>
            ) : null}
          </Col>
        </Row>

        <Title level={5} style={{ marginTop: 32, color: 'var(--lc-muted)' }}>
          Danh mục tra cứu
        </Title>
        <Row gutter={[16, 16]}>
          {[
            { to: '/duyet/chu-de', label: 'Chủ đề' },
            { to: '/duyet/tac-gia', label: 'Tác giả' },
            { to: '/duyet/phan-loai', label: 'Khung phân loại' },
            { to: '/duyet/bo-suu-tap', label: 'Bộ sưu tập' },
            { to: '/duyet/nganh', label: 'Ngành – Môn học' },
            { to: '/luan-van', label: 'Luận văn – Luận án' },
            { to: '/an-pham-dinh-ky', label: 'Báo – Tạp chí' },
            { to: '/tai-lieu-so', label: 'Tài liệu số' },
          ].map((entry) => (
            <Col xs={12} sm={8} md={6} key={entry.to}>
              <Link to={entry.to}>
                <Card size="small" hoverable style={{ textAlign: 'center' }}>
                  {entry.label}
                </Card>
              </Link>
            </Col>
          ))}
        </Row>
      </div>
    </>
  );
}
