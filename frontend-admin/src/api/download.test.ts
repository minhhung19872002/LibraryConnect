import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

const sourceRoot = join(process.cwd(), 'src');

function allSourceFiles(directory: string): string[] {
  return readdirSync(directory).flatMap((entry) => {
    const full = join(directory, entry);

    if (statSync(full).isDirectory()) {
      return allSourceFiles(full);
    }

    return /\.tsx?$/.test(entry) && !/\.test\.tsx?$/.test(entry) ? [full] : [];
  });
}

/**
 * Xác thực của hệ thống là JWT đặt trong tiêu đề yêu cầu, không phải cookie. Vì thế mọi địa chỉ tự
 * ghép trỏ thẳng vào API — đặt vào `href` của thẻ liên kết, vào `src` của thẻ ảnh, vào `action` của
 * biểu mẫu — đều đi ra ngoài lớp gọi API và không mang theo mã đăng nhập: người dùng bấm nút xuất
 * báo cáo nhận về một trang trắng in dòng JSON báo hết phiên, còn ảnh thì không hiện.
 *
 * Luật: ngoài thư mục `src/api`, mã nguồn không được chứa địa chỉ bắt đầu bằng `/api/`. Lớp gọi API
 * đã tự thêm tiền tố ấy, nên hễ thấy nó viết tay là có một lượt gọi đi vòng ra ngoài.
 *
 * Chặn bằng phép thử quét mã nguồn vì viết `href` nhanh hơn viết một lượt tải có xác thực — lỗi này
 * rất dễ quay lại.
 *
 * **Một ngoại lệ, hẹp và có lý do:** nhóm `/api/public/` không đòi đăng nhập — đó là nhóm endpoint
 * dành cho trang tra cứu công khai và cho ảnh (logo, ảnh bìa, ảnh tin). Đặt địa chỉ ấy vào `src` của
 * thẻ ảnh là cách dùng đúng: không cần mã đăng nhập, mà lại để trình duyệt tự đặt bộ nhớ đệm. Tải
 * qua lớp gọi API rồi dựng địa chỉ tạm thì mất hẳn bộ nhớ đệm, mà một trang danh sách có hai chục
 * ảnh bìa.
 */
describe('Không có chỗ nào trỏ thẳng vào API ngoài lớp gọi API', () => {
  it('mọi lượt gọi đều đi qua lớp gọi API có mã đăng nhập', () => {
    const viPham: string[] = [];
    const thuMucApi = join(sourceRoot, 'api');

    for (const file of allSourceFiles(sourceRoot)) {
      if (file.startsWith(thuMucApi)) {
        continue;
      }

      for (const [index, line] of readFileSync(file, 'utf8').split('\n').entries()) {
        const laChuThich = /^\s*(?:\/\/|\*|\/\*)/.test(line);

        // Nhóm công khai không đòi đăng nhập nên đặt thẳng vào thẻ ảnh được — xem chú thích trên.
        const congKhai = /["'`]\/api\/public\//.test(line);

        if (!laChuThich && !congKhai && /["'`]\/api\//.test(line)) {
          viPham.push(`${file.slice(sourceRoot.length + 1)}:${index + 1} — ${line.trim()}`);
        }
      }
    }

    expect(viPham, viPham.join('\n')).toEqual([]);
  });
});

describe('Lưu tệp về máy', () => {
  it('đặt đúng tên tệp và dọn lại địa chỉ tạm', async () => {
    const { saveBlob } = await import('./download');

    const created: string[] = [];
    const revoked: string[] = [];
    let downloadName = '';

    const originalCreate = URL.createObjectURL;
    const originalRevoke = URL.revokeObjectURL;
    const originalClick = HTMLAnchorElement.prototype.click;

    URL.createObjectURL = () => {
      created.push('blob:gia-lap');
      return 'blob:gia-lap';
    };
    URL.revokeObjectURL = (url: string) => {
      revoked.push(url);
    };
    HTMLAnchorElement.prototype.click = function click(this: HTMLAnchorElement) {
      downloadName = this.download;
    };

    try {
      saveBlob(new Blob(['noi dung']), 'bao-cao-tong-quan.xlsx');
    } finally {
      URL.createObjectURL = originalCreate;
      URL.revokeObjectURL = originalRevoke;
      HTMLAnchorElement.prototype.click = originalClick;
    }

    expect(downloadName).toBe('bao-cao-tong-quan.xlsx');
    expect(created).toHaveLength(1);
    expect(revoked).toEqual(['blob:gia-lap']);
    expect(document.querySelectorAll('a')).toHaveLength(0);
  });
});
