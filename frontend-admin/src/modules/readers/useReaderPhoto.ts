import { useEffect, useState } from 'react';
import { http } from '@/api/client';

/**
 * Nạp ảnh chân dung của bạn đọc để hiển thị.
 *
 * Ảnh nằm sau endpoint có kiểm tra quyền, mà thẻ <img> của trình duyệt thì không gửi kèm mã thông
 * báo đăng nhập — trỏ thẳng src vào địa chỉ ảnh là chắc chắn nhận 401. Vì vậy ảnh được tải bằng
 * chính bộ gọi API đã gắn sẵn mã thông báo, rồi dựng thành địa chỉ tạm trong bộ nhớ trình duyệt.
 *
 * <paramref name="stamp"/> đổi giá trị là buộc tải lại — dùng sau khi cán bộ đổi ảnh.
 */
export function useReaderPhoto(
  readerId: string | null | undefined,
  hasPhoto: boolean,
  stamp?: number | string,
): string | undefined {
  const [url, setUrl] = useState<string | undefined>(undefined);

  useEffect(() => {
    if (!readerId || !hasPhoto) {
      setUrl(undefined);
      return;
    }

    let objectUrl: string | undefined;
    let cancelled = false;

    http
      .get<Blob>(`/readers/${readerId}/photo`, { responseType: 'blob' })
      .then((response) => {
        if (cancelled) return;

        objectUrl = URL.createObjectURL(response.data);
        setUrl(objectUrl);
      })
      .catch(() => {
        // Thiếu ảnh chỉ làm ô ảnh rơi về chữ cái viết tắt, không được làm hỏng cả màn hình.
        if (!cancelled) setUrl(undefined);
      });

    return () => {
      cancelled = true;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [readerId, hasPhoto, stamp]);

  return url;
}
