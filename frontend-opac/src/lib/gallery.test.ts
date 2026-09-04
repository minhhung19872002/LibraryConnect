import { describe, expect, it } from 'vitest';
import { galleryCover, sortGalleries, sortedImages } from './gallery';
import type { Gallery } from '@/types/api';

function album(id: string, eventDate?: string, coverUrl?: string): Gallery {
  return {
    id,
    title: id,
    coverUrl,
    eventDate,
    isPublished: true,
    images: [
      { id: 'b', imageUrl: '/b.jpg', sortOrder: 20 },
      { id: 'a', imageUrl: '/a.jpg', caption: 'Ảnh đầu', sortOrder: 10 },
    ],
  };
}

describe('Thư viện ảnh', () => {
  it('ảnh trong album theo thứ tự cán bộ sắp, không theo thứ tự máy chủ trả', () => {
    expect(sortedImages(album('x')).map((image) => image.id)).toEqual(['a', 'b']);
  });

  it('không chọn bìa thì lấy ảnh đầu tiên, album rỗng thì không có bìa', () => {
    expect(galleryCover(album('x', undefined, '/bia.jpg'))).toBe('/bia.jpg');
    expect(galleryCover(album('x'))).toBe('/a.jpg');
    expect(galleryCover({ coverUrl: undefined, images: [] })).toBeNull();
  });

  it('album mới diễn ra trước, album không ghi ngày xếp cuối', () => {
    const sorted = sortGalleries([
      album('khong-ngay'),
      album('cu', '2025-03-01'),
      album('moi', '2026-08-15'),
    ]);

    expect(sorted.map((item) => item.id)).toEqual(['moi', 'cu', 'khong-ngay']);
  });
});
