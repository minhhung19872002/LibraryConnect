/**
 * Thời lượng đọc tài liệu số (V.2).
 *
 * Trình đọc báo về máy chủ tổng số giây kể từ lúc mở, định kỳ trong lúc đọc và một lần cuối khi
 * rời trang. Lần cuối phải đi bằng `fetch` với `keepalive: true`: trang đang đóng thì trình duyệt
 * huỷ mọi yêu cầu thường, chỉ yêu cầu keepalive mới được gửi nốt. `navigator.sendBeacon` không
 * dùng được vì không gắn được tiêu đề Authorization.
 */

/** Khoảng cách giữa hai lần báo định kỳ, để phiên đọc dài vẫn có số dù trình duyệt bị tắt đột ngột. */
export const REPORT_INTERVAL_MS = 60_000;

/** Số giây đã đọc, làm tròn xuống và không âm — đồng hồ máy khách lùi thì báo 0 chứ không báo âm. */
export function elapsedSeconds(startedAt: number, now: number): number {
  return Math.max(0, Math.floor((now - startedAt) / 1000));
}

export interface ReadingTimeRequest {
  url: string;
  init: RequestInit;
}

/** Dựng yêu cầu báo thời lượng — tách ra để kiểm được keepalive và tiêu đề mà không cần trình duyệt. */
export function buildReadingTimeRequest(
  baseUrl: string,
  documentId: string,
  seconds: number,
  accessToken: string | null,
): ReadingTimeRequest {
  const headers: Record<string, string> = { 'Content-Type': 'application/json' };

  if (accessToken) {
    headers.Authorization = `Bearer ${accessToken}`;
  }

  return {
    url: `${baseUrl.replace(/\/$/, '')}/reader/digital/${documentId}/reading-time`,
    init: {
      method: 'POST',
      headers,
      body: JSON.stringify({ seconds }),
      keepalive: true,
    },
  };
}
