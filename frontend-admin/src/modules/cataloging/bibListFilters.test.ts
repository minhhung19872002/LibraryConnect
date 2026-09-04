import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { FILTER_LABEL_PARAM, linkedFilters, parseBibListParams } from './bibListFilters';

const ID = '7f3c2a10-5b6d-4e8f-9a1b-2c3d4e5f6a7b';

/**
 * Màn hình danh mục tự tạo dẫn sang `/bien-muc?customIndexValueId=…` từ phase 5, mà trang danh
 * sách chưa bao giờ đọc chuỗi truy vấn: bấm số biểu ghi thì ra trang danh sách đầy đủ, không lọc
 * gì (J2). Bộ dựng bộ lọc từ địa chỉ là chỗ chặn lỗi ấy quay lại.
 */
describe('Dựng bộ lọc danh sách biểu ghi từ địa chỉ', () => {
  it('đọc mã giá trị danh mục tự tạo', () => {
    const filter = parseBibListParams(new URLSearchParams({ customIndexValueId: ID }));

    expect(filter.customIndexValueId).toBe(ID);
  });

  it('đọc các bộ lọc khác: dạng tài liệu, trạng thái, năm, cờ', () => {
    const filter = parseBibListParams(
      new URLSearchParams({
        documentTypeId: ID,
        status: 'Published',
        publishYearFrom: '2020',
        publishYearTo: '2024',
        withoutItems: 'true',
        keyword: ' cơ sở dữ liệu ',
      }),
    );

    expect(filter).toEqual({
      documentTypeId: ID,
      status: 'Published',
      publishYearFrom: 2020,
      publishYearTo: 2024,
      withoutItems: true,
      keyword: 'cơ sở dữ liệu',
    });
  });

  it('bỏ qua giá trị sai dạng — địa chỉ là thứ người dùng sửa tay được', () => {
    const filter = parseBibListParams(
      new URLSearchParams({
        customIndexValueId: 'khong-phai-guid',
        status: 'KhongCo',
        publishYearFrom: 'abc',
        availableOnly: 'yes',
      }),
    );

    expect(filter).toEqual({});
  });

  it('thẻ bộ lọc mang nhãn đọc được khi liên kết gửi kèm', () => {
    const tags = linkedFilters(
      new URLSearchParams({ customIndexValueId: ID, [FILTER_LABEL_PARAM]: 'Hà Nội' }),
    );

    expect(tags).toEqual([{ key: 'customIndexValueId', label: 'Danh mục tự tạo', value: 'Hà Nội' }]);
  });

  it('không có nhãn thì thẻ hiện mã rút gọn, vẫn bỏ được', () => {
    const tags = linkedFilters(new URLSearchParams({ authorId: ID }));

    expect(tags).toEqual([{ key: 'authorId', label: 'Tác giả', value: '#7f3c2a10' }]);
  });
});

describe('Trang danh sách biểu ghi thật sự đọc địa chỉ', () => {
  const trang = readFileSync(join(process.cwd(), 'src/modules/cataloging/BibListPage.tsx'), 'utf8');

  it('dùng useSearchParams và bộ dựng bộ lọc từ địa chỉ', () => {
    expect(trang).toContain('useSearchParams');
    expect(trang).toContain('parseBibListParams');
    expect(trang).toContain('linkedFilters');
  });

  it('màn hình danh mục tự tạo gửi kèm nhãn để thẻ bộ lọc đọc được', () => {
    const danhMuc = readFileSync(
      join(process.cwd(), 'src/modules/cataloging/CustomIndexPage.tsx'),
      'utf8',
    );

    expect(danhMuc).toContain('FILTER_LABEL_PARAM');
  });
});
