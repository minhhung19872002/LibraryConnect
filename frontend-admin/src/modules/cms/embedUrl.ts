/**
 * Đổi địa chỉ xem video sang địa chỉ nhúng.
 *
 * Cán bộ dán địa chỉ trên thanh trình duyệt, nhưng khung nhúng chỉ nhận địa chỉ dạng /embed/ —
 * dán thẳng thì khung hiện trắng và không ai hiểu vì sao. Máy chủ cũng chỉ cho nhúng đúng hai nơi
 * này, nên trả về null cho mọi địa chỉ khác thay vì để nó bị lọc mất sau khi lưu.
 */
export function toEmbedUrl(url: string): string | null {
  if (!url) {
    return null;
  }

  const youtube = url.match(
    /(?:youtube\.com\/(?:watch\?v=|embed\/)|youtu\.be\/)([A-Za-z0-9_-]{6,})/,
  );

  if (youtube?.[1]) {
    return `https://www.youtube.com/embed/${youtube[1]}`;
  }

  const vimeo = url.match(/vimeo\.com\/(?:video\/)?(\d+)/);

  if (vimeo?.[1]) {
    return `https://player.vimeo.com/video/${vimeo[1]}`;
  }

  return null;
}
