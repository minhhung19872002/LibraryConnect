import { describe, expect, it } from 'vitest';
import { buildReadingTimeRequest, elapsedSeconds, REPORT_INTERVAL_MS } from './readingTime';

describe('Thời lượng đọc tài liệu số', () => {
  it('đếm giây làm tròn xuống và không bao giờ âm', () => {
    expect(elapsedSeconds(1_000, 3_999)).toBe(2);
    expect(elapsedSeconds(1_000, 1_000)).toBe(0);
    expect(elapsedSeconds(5_000, 1_000)).toBe(0);
  });

  it('báo định kỳ đủ dày để phiên đọc dài không mất số khi trình duyệt bị tắt', () => {
    expect(REPORT_INTERVAL_MS).toBeLessThanOrEqual(60_000);
  });

  it('yêu cầu cuối đi bằng keepalive, mang mã đăng nhập và tổng số giây', () => {
    const request = buildReadingTimeRequest('/api/', 'abc', 42, 'token-123');

    expect(request.url).toBe('/api/reader/digital/abc/reading-time');
    expect(request.init.method).toBe('POST');
    expect(request.init.keepalive).toBe(true);
    expect(request.init.body).toBe(JSON.stringify({ seconds: 42 }));
    expect((request.init.headers as Record<string, string>).Authorization).toBe('Bearer token-123');
  });

  it('khách chưa đăng nhập vẫn báo được, không gửi tiêu đề Authorization rỗng', () => {
    const request = buildReadingTimeRequest('/api', 'abc', 7, null);

    expect((request.init.headers as Record<string, string>).Authorization).toBeUndefined();
  });
});
