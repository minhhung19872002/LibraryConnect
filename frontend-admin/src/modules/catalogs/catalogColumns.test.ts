import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { CATALOG_COLUMN_WIDTHS, catalogScrollX } from './catalogColumns';

const trangDanhMuc = readFileSync(
  join(process.cwd(), 'src/modules/catalogs/CatalogPage.tsx'),
  'utf8',
);

/**
 * Lỗi E1 đo được trên màn hình Danh mục → Tác giả: hai cột **Tên** và **Tên tiếng Anh** rộng đúng
 * 0 px, mười một ô tiêu đề bị bóp cao 91 px và chữ tiêu đề chồng lên nhau.
 *
 * Nguyên nhân là kiểu viết đã gây ra lỗi C6: mọi cột khác khai bề rộng cố định, riêng hai cột này
 * bỏ trống nên chỉ nhận phần còn thừa — mà danh mục tác giả khai thêm sáu cột riêng nên phần thừa
 * bằng không.
 */
describe('Bề rộng cột của bảng danh mục', () => {
  it('cột Tên có khai bề rộng và là cột rộng nhất', () => {
    const { ten, ...conLai } = CATALOG_COLUMN_WIDTHS;

    expect(ten).toBeGreaterThan(0);
    expect(ten).toBeGreaterThan(Math.max(...Object.values(conLai)));
  });

  it('cột Tên tiếng Anh cũng khai bề rộng', () => {
    expect(CATALOG_COLUMN_WIDTHS.tenTiengAnh).toBeGreaterThan(0);
  });

  it('danh mục tác giả với sáu cột riêng vẫn đủ chỗ cho mọi cột', () => {
    const rong = catalogScrollX({ coCotMa: true, coCotTenTiengAnh: true, soCotRieng: 6 });

    // Tổng bề rộng phải lớn hơn khung 1.290 px đo được, nghĩa là bảng cuộn ngang chứ không bóp cột.
    expect(rong).toBeGreaterThan(1290);
  });

  it('danh mục đơn giản nhất vẫn có bề ngang hợp lý', () => {
    const rong = catalogScrollX({ coCotMa: false, coCotTenTiengAnh: false, soCotRieng: 0 });

    expect(rong).toBeGreaterThan(500);
  });
});

/**
 * Chặn kiểu viết đã gây ra lỗi quay lại: cột khai trong `CatalogPage` phải lấy bề rộng từ bảng trên
 * chứ không được bỏ trống.
 */
describe('Bảng danh mục dùng đúng bảng bề rộng đã khai', () => {
  it('cột Tên và Tên tiếng Anh không được bỏ trống bề rộng', () => {
    const khoiTen = trangDanhMuc.slice(
      trangDanhMuc.indexOf("title: 'Tên',"),
      trangDanhMuc.indexOf("// Each catalogue contributes"),
    );

    expect(khoiTen).toContain('CATALOG_COLUMN_WIDTHS.ten');
    expect(khoiTen).toContain('CATALOG_COLUMN_WIDTHS.tenTiengAnh');
  });

  it('bảng khai cuộn ngang theo tổng bề rộng đã tính', () => {
    expect(trangDanhMuc).toContain('catalogScrollX');
  });
});
