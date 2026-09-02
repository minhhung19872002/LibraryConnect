import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Carousel, Col, Row, Skeleton, Typography } from 'antd';
import { opacApi } from '@/api/opac';
import { Hero } from '@/components/Hero';
import { ResultShelf } from '@/components/ResultList';
import { useSiteSettings } from '@/hooks/useSite';
import type { HomePayload } from '@/types/api';
import { formatDate } from '@/lib/datetime';

const { Paragraph } = Typography;

/** Một khối giấy có tiêu đề chữ có chân, dùng cho mọi mục của trang chủ. */
function Section({
  title,
  extra,
  children,
}: {
  title: string;
  extra?: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <section className="lc-paper lc-section">
      <div
        className="lc-section__title"
        style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}
      >
        <span>{title}</span>
        {extra ? <span style={{ fontSize: 13.5, fontWeight: 400 }}>{extra}</span> : null}
      </div>
      <div className="lc-section__body">{children}</div>
    </section>
  );
}

/** IX.1 — Trang chủ: khối tra cứu lớn, banner, sách mới, sách được mượn nhiều, tin tức, liên kết. */
export function HomePage() {
  const { data: settings } = useSiteSettings();
  const { data, isLoading } = useQuery<HomePayload>({
    queryKey: ['home'],
    queryFn: () => opacApi.home(),
    staleTime: 5 * 60 * 1000,
  });

  return (
    <>
      <Hero />

      <div className="lc-container" style={{ padding: '16px 24px 50px' }}>
        {isLoading ? <Skeleton active paragraph={{ rows: 8 }} /> : null}

        {data && data.banners.length > 0 ? (
          <div className="lc-paper lc-section" style={{ overflow: 'hidden' }}>
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
          </div>
        ) : null}

        {data && settings?.showNewBooks !== false ? (
          <Section title="Sách mới bổ sung" extra={<Link to="/tra-cuu?sort=Newest">Xem tất cả</Link>}>
            <ResultShelf items={data.newBooks} />
          </Section>
        ) : null}

        {data && settings?.showPopularBooks !== false ? (
          <Section title="Được mượn nhiều" extra={<Link to="/tra-cuu?sort=Popular">Xem tất cả</Link>}>
            <ResultShelf items={data.popularBooks} />
          </Section>
        ) : null}

        {data ? (
          <Row gutter={[24, 0]}>
            <Col xs={24} lg={16}>
              <Section title="Tin tức – Sự kiện" extra={<Link to="/tin-tuc">Xem tất cả</Link>}>
                {data.news.length > 0 ? (
                  <Row gutter={[16, 16]}>
                    {data.news.map((news) => (
                      <Col xs={24} sm={12} key={news.id}>
                        <div className="lc-paper" style={{ padding: 12, height: '100%' }}>
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
                          <div className="lc-result__title" style={{ fontSize: 15 }}>
                            <Link to={`/tin-tuc/${news.slug}`}>{news.title}</Link>
                          </div>
                          <div className="lc-result__meta" style={{ fontSize: 12 }}>
                            {news.categoryName ? `${news.categoryName} · ` : ''}
                            {news.publishedAt ? formatDate(news.publishedAt) : ''}
                          </div>
                          <Paragraph
                            ellipsis={{ rows: 2 }}
                            style={{ marginTop: 6, marginBottom: 0, fontSize: 13 }}
                          >
                            {news.summary}
                          </Paragraph>
                        </div>
                      </Col>
                    ))}
                  </Row>
                ) : (
                  <div className="lc-empty">Chưa có bản tin nào được đăng.</div>
                )}
              </Section>
            </Col>

            <Col xs={24} lg={8}>
              <Section title="Liên kết hữu ích">
                {data.links.length > 0 ? (
                  <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                    {data.links.map((link) => (
                      <div key={link.id}>
                        <a href={link.url} target="_blank" rel="noopener noreferrer">
                          {link.name}
                        </a>
                        {link.groupName ? (
                          <div className="lc-result__meta" style={{ fontSize: 12 }}>
                            {link.groupName}
                          </div>
                        ) : null}
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="lc-empty" style={{ padding: 20 }}>
                    Chưa khai báo liên kết nào.
                  </div>
                )}
              </Section>

              <Section title="Danh mục tra cứu">
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '6px 12px' }}>
                  {[
                    { to: '/duyet/chu-de', label: 'Chủ đề' },
                    { to: '/duyet/tac-gia', label: 'Tác giả' },
                    { to: '/duyet/phan-loai', label: 'Khung phân loại' },
                    { to: '/duyet/bo-suu-tap', label: 'Bộ sưu tập' },
                    { to: '/duyet/nganh', label: 'Ngành – Môn học' },
                    { to: '/luan-van', label: 'Luận văn – Luận án' },
                    { to: '/an-pham-dinh-ky', label: 'Báo – Tạp chí' },
                    { to: '/tai-lieu-so', label: 'Tài liệu số' },
                    { to: '/tra-cuu-nang-cao', label: 'Tra cứu nâng cao' },
                    ...(settings?.showInterlibrary
                      ? [{ to: '/thu-vien-khac', label: 'Tìm ở thư viện khác' }]
                      : []),
                  ].map((entry) => (
                    <Link key={entry.to} to={entry.to}>
                      {entry.label}
                    </Link>
                  ))}
                </div>
              </Section>
            </Col>
          </Row>
        ) : null}
      </div>
    </>
  );
}
