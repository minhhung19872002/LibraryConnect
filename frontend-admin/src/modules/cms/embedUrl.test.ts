import { describe, expect, it } from 'vitest';
import { toEmbedUrl } from './embedUrl';

/**
 * Máy chủ chỉ giữ khung nhúng từ YouTube và Vimeo (bộ lọc HTML ở yêu cầu 6.4).
 *
 * Nếu ở đây sinh ra địa chỉ dạng khác thì khung video biến mất ngay khi lưu bài, mà cán bộ soạn tin
 * không nhận được lời giải thích nào — nên chặn tại chỗ và báo lỗi rõ ràng.
 */
describe('Địa chỉ nhúng video', () => {
  it('đổi địa chỉ xem YouTube thành địa chỉ nhúng', () => {
    expect(toEmbedUrl('https://www.youtube.com/watch?v=dQw4w9WgXcQ')).toBe(
      'https://www.youtube.com/embed/dQw4w9WgXcQ',
    );
  });

  it('nhận cả địa chỉ rút gọn youtu.be', () => {
    expect(toEmbedUrl('https://youtu.be/dQw4w9WgXcQ')).toBe(
      'https://www.youtube.com/embed/dQw4w9WgXcQ',
    );
  });

  it('giữ nguyên địa chỉ đã ở dạng nhúng', () => {
    expect(toEmbedUrl('https://www.youtube.com/embed/dQw4w9WgXcQ')).toBe(
      'https://www.youtube.com/embed/dQw4w9WgXcQ',
    );
  });

  it('đổi địa chỉ Vimeo thành địa chỉ trình phát', () => {
    expect(toEmbedUrl('https://vimeo.com/76979871')).toBe('https://player.vimeo.com/video/76979871');
  });

  it('từ chối nơi khác thay vì sinh khung sẽ bị lọc mất', () => {
    expect(toEmbedUrl('https://example.com/video.mp4')).toBeNull();
    expect(toEmbedUrl('')).toBeNull();
  });
});
