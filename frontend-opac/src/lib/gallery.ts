import type { Gallery, GalleryImage } from '@/types/api';

/**
 * Trang Thư viện ảnh (VIII.2): ảnh của một album theo đúng thứ tự cán bộ sắp, và ảnh bìa để hiện
 * ngoài lưới — cán bộ không chọn bìa thì lấy ảnh đầu tiên, để album không bao giờ là ô trống.
 */

export function sortedImages(gallery: Pick<Gallery, 'images'>): GalleryImage[] {
  return [...gallery.images].sort((a, b) => a.sortOrder - b.sortOrder);
}

export function galleryCover(gallery: Pick<Gallery, 'coverUrl' | 'images'>): string | null {
  if (gallery.coverUrl) return gallery.coverUrl;

  const first = sortedImages(gallery)[0];
  return first?.imageUrl ?? null;
}

/** Album mới diễn ra trước; album không ghi ngày xếp cuối, giữ thứ tự máy chủ trả. */
export function sortGalleries(galleries: Gallery[]): Gallery[] {
  return [...galleries].sort((a, b) => {
    if (!a.eventDate && !b.eventDate) return 0;
    if (!a.eventDate) return 1;
    if (!b.eventDate) return -1;
    return b.eventDate.localeCompare(a.eventDate);
  });
}
