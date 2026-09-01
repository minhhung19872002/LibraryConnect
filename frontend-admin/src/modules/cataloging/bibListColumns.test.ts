import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { BIB_LIST_COLUMN_WIDTHS, BIB_LIST_SCROLL_X } from './bibListColumns';

const trangDanhSach = readFileSync(
  join(process.cwd(), 'src/modules/cataloging/BibListPage.tsx'),
  'utf8',
);

/**
 * Nhan đề là thứ cán bộ đọc để nhận ra cuốn sách. Nếu nó hẹp hơn cột nhà xuất bản hay cột số kiểm
 * soát thì bảng sắp xếp sai thứ tự quan trọng, và với nhan đề tiếng Việt dài thì mỗi dòng bảng cao
 * hàng trăm điểm ảnh — một màn hình chỉ xem được vài biểu ghi.
 */
describe('Bề rộng cột của bảng biểu ghi', () => {
  it('nhan đề là cột rộng nhất', () => {
    const { nhanDe, ...conLai } = BIB_LIST_COLUMN_WIDTHS;

    expect(nhanDe).toBeGreaterThan(Math.max(...Object.values(conLai)));
  });

  it('nhan đề đủ chỗ cho một nhan đề tiếng Việt dài trên hai dòng', () => {
    // Nhan đề luận văn tiếng Việt thường 120–180 ký tự; khoảng 7 điểm ảnh một ký tự ở cỡ chữ nhỏ.
    expect(BIB_LIST_COLUMN_WIDTHS.nhanDe).toBeGreaterThanOrEqual(320);
  });

  it('bảng cuộn ngang theo đúng tổng bề rộng đã khai', () => {
    const tong = Object.values(BIB_LIST_COLUMN_WIDTHS).reduce((a, b) => a + b, 0);

    expect(BIB_LIST_SCROLL_X).toBe(tong);
    expect(BIB_LIST_SCROLL_X).toBeGreaterThan(1200);
  });
});

/**
 * Chặn kiểu viết đã gây ra lỗi: mọi cột khai bề rộng cố định, riêng cột nhan đề bỏ trống nên nhận
 * phần thừa — hết phần thừa thì nó co lại gần bằng không.
 */
describe('Bảng biểu ghi dùng đúng bảng bề rộng đã khai', () => {
  it('trang danh sách lấy bề rộng từ bibListColumns chứ không viết số rời rạc', () => {
    expect(trangDanhSach).toContain('BIB_LIST_COLUMN_WIDTHS');
    expect(trangDanhSach).toContain('BIB_LIST_SCROLL_X');
  });

  it('cột nhan đề có khai bề rộng', () => {
    const doanNhanDe = trangDanhSach.slice(
      trangDanhSach.indexOf("title: 'Nhan đề'"),
      trangDanhSach.indexOf("title: 'Tác giả'"),
    );

    expect(doanNhanDe).toContain('width');
  });
});
