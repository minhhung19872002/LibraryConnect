import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { messages } from '@/i18n/messages';

/**
 * Màn hình đầu tiên cán bộ nhìn thấy sau khi đăng nhập.
 *
 * Nghiệm thu thử ngày 05/09/2026 trên máy chủ thật: trang Tổng quan vẫn mang dòng giữ chỗ từ
 * phase 1 — "Hệ thống đang trong quá trình bàn giao theo từng phân hệ" — và ba con số về quyền
 * hạn của chính tài khoản. Hội đồng đọc dòng ấy sẽ kết luận phần mềm chưa xong, dù mười phân hệ
 * đứng sau đã chạy. Trang này phải nói về thư viện, không nói về tiến độ dự án.
 */
describe('Trang Tổng quan', () => {
  const source = readFileSync(join(__dirname, 'DashboardPage.tsx'), 'utf8');

  it('không còn dòng giữ chỗ về tiến độ bàn giao', () => {
    const all = JSON.stringify(messages);
    expect(all).not.toContain('quá trình bàn giao');
    expect(source).not.toContain('phaseNotice');
  });

  it('hiện số liệu hoạt động của thư viện lấy từ báo cáo tổng quan', () => {
    expect(source).toContain('reportsApi.overview');
  });
});
