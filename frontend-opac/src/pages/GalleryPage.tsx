import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Card, Empty, Image, Modal, Skeleton, Typography } from 'antd';
import { opacApi } from '@/api/opac';
import { formatDate } from '@/lib/datetime';
import { galleryCover, sortGalleries, sortedImages } from '@/lib/gallery';
import type { Gallery } from '@/types/api';

const { Paragraph } = Typography;

/**
 * VIII.2 / IX.1 — Thư viện ảnh: các album sự kiện đã đăng. Bấm một album thì mở toàn bộ ảnh của
 * nó trong hộp thoại, xem phóng to và lật ảnh bằng bộ xem của Ant Design.
 */
export function GalleryPage() {
  const [open, setOpen] = useState<Gallery | null>(null);

  const galleries = useQuery({
    queryKey: ['galleries'],
    queryFn: () => opacApi.galleries(),
    staleTime: 5 * 60 * 1000,
  });

  const albums = sortGalleries(galleries.data ?? []);

  return (
    <div className="lc-container" style={{ padding: '24px 16px 48px' }}>
      <Card title="Thư viện ảnh">
        {galleries.isLoading ? (
          <Skeleton active paragraph={{ rows: 6 }} />
        ) : albums.length === 0 ? (
          <Empty description="Thư viện chưa đăng album ảnh nào." />
        ) : (
          <div className="lc-gallery-grid">
            {albums.map((album) => {
              const cover = galleryCover(album);

              return (
                <div
                  key={album.id}
                  className="lc-gallery-card"
                  role="button"
                  tabIndex={0}
                  onClick={() => setOpen(album)}
                  onKeyDown={(event) => {
                    if (event.key === 'Enter' || event.key === ' ') {
                      event.preventDefault();
                      setOpen(album);
                    }
                  }}
                >
                  {cover ? (
                    <img className="lc-gallery-card__cover" src={cover} alt={album.title} loading="lazy" />
                  ) : (
                    <div className="lc-gallery-card__cover" />
                  )}
                  <div className="lc-gallery-card__body">
                    <div className="lc-gallery-card__title">{album.title}</div>
                    <div className="lc-gallery-card__meta">
                      {album.eventDate ? `${formatDate(album.eventDate)} · ` : ''}
                      {album.images.length} ảnh
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </Card>

      <Modal
        open={open !== null}
        title={open?.title}
        footer={null}
        width={960}
        onCancel={() => setOpen(null)}
      >
        {open ? (
          <>
            {open.description ? <Paragraph>{open.description}</Paragraph> : null}
            <Image.PreviewGroup>
              <div className="lc-gallery-photos">
                {sortedImages(open).map((image) => (
                  <div key={image.id}>
                    <Image
                      src={image.imageUrl}
                      alt={image.caption ?? open.title}
                      style={{ width: '100%', aspectRatio: '4 / 3', objectFit: 'cover', borderRadius: 6 }}
                    />
                    {image.caption ? (
                      <div className="lc-gallery-photos__caption">{image.caption}</div>
                    ) : null}
                  </div>
                ))}
              </div>
            </Image.PreviewGroup>
          </>
        ) : null}
      </Modal>
    </div>
  );
}
