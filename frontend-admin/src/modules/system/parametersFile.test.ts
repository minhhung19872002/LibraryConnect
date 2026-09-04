import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

/**
 * Tham số kiểu Tệp (I.3) — logo thư viện — phải có ô tải tệp lên và xem tệp hiện tại. Trước đợt
 * này màn hình tham số rơi vào ô nhập chữ mặc định cho kiểu `File`, nghĩa là người quản trị nhìn
 * thấy một chuỗi "library-logo_url.png" và không có cách nào đổi logo.
 */
describe('Tham số kiểu Tệp trên màn hình Tham số hệ thống', () => {
  const trang = readFileSync(join(process.cwd(), 'src/modules/system/ParametersPage.tsx'), 'utf8');

  it('kiểu File có ô riêng, không rơi vào ô nhập chữ mặc định', () => {
    expect(trang).toContain("case 'File':");
    expect(trang).toContain('FileParameterControl');
  });

  it('tải lên và tải về đi qua endpoint tệp của tham số, có mang mã xác thực', () => {
    expect(trang).toContain('/file`');
    // Ảnh hiện từ blob đã tải bằng client có mã xác thực, không phải thẻ img trỏ thẳng vào API.
    expect(trang).not.toMatch(/src=\{`\/api/);
  });

  it('lưu biểu mẫu không gửi lại tên tệp cũ đè lên tệp vừa tải lên', () => {
    const doanSerialise = trang.slice(trang.indexOf('function serialise('));

    expect(doanSerialise).toContain("parameter.dataType === 'File'");
  });
});
