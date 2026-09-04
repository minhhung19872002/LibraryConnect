import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { buildDataScopes, splitDataScopes } from './dataScopes';

/**
 * Phạm vi dữ liệu (I.2): máy chủ nhận `dataScopes` từ phase 2, nhưng màn hình người dùng vẫn gửi
 * lại nguyên danh sách cũ với chú thích "Phase 6" — không có cách nào gán thư viện/kho cho một cán
 * bộ từ giao diện.
 */
describe('Phạm vi dữ liệu của người dùng', () => {
  it('hai ô chọn thành danh sách phạm vi có kiểu đúng', () => {
    expect(buildDataScopes({ libraryIds: ['L1'], warehouseIds: ['W1', 'W2'] })).toEqual([
      { scopeType: 'Library', scopeId: 'L1' },
      { scopeType: 'Warehouse', scopeId: 'W1' },
      { scopeType: 'Warehouse', scopeId: 'W2' },
    ]);
  });

  it('bỏ trống cả hai ô là danh sách rỗng — nghĩa là không giới hạn', () => {
    expect(buildDataScopes({})).toEqual([]);
  });

  it('tách phạm vi máy chủ trả về thành ba ô', () => {
    expect(
      splitDataScopes([
        { scopeType: 'Library', scopeId: 'L1' },
        { scopeType: 'DocumentType', scopeId: 'D1' },
        { scopeType: 'Warehouse', scopeId: 'W1' },
      ]),
    ).toEqual({ libraryIds: ['L1'], warehouseIds: ['W1'], documentTypeIds: ['D1'] });

    expect(splitDataScopes(undefined)).toEqual({
      libraryIds: [],
      warehouseIds: [],
      documentTypeIds: [],
    });
  });

  it('đi vòng: dựng rồi tách ra đúng ba ô ban đầu', () => {
    const values = { libraryIds: ['L1', 'L2'], warehouseIds: ['W1'], documentTypeIds: ['D1'] };

    expect(splitDataScopes(buildDataScopes(values))).toEqual(values);
  });

  it('dạng tài liệu cũng thành phạm vi gửi lên máy chủ', () => {
    // Đặc tả liệt kê đủ ba chiều (kho, thư viện, loại tài liệu). Bộ lọc toàn cục trên biểu ghi đã
    // đọc chiều thứ ba từ lâu, nhưng màn hình không có ô nào nên nó chưa bao giờ bật được.
    expect(buildDataScopes({ documentTypeIds: ['D1', 'D2'] })).toEqual([
      { scopeType: 'DocumentType', scopeId: 'D1' },
      { scopeType: 'DocumentType', scopeId: 'D2' },
    ]);
  });
});

describe('Màn hình người dùng có ô chọn phạm vi', () => {
  const trang = readFileSync(join(process.cwd(), 'src/modules/system/UsersPage.tsx'), 'utf8');

  it('gửi phạm vi từ biểu mẫu, không còn chú thích hoãn sang phase sau', () => {
    expect(trang).toContain('buildDataScopes(values)');
    expect(trang).toContain('name="libraryIds"');
    expect(trang).toContain('name="warehouseIds"');
    expect(trang).not.toContain('Phase 6');
  });

  it('có đủ ô chọn cho cả ba chiều phạm vi', () => {
    // Chiều dạng tài liệu từng chỉ tồn tại ở tầng dữ liệu: màn hình giữ lại giá trị cũ khi lưu
    // nhưng không cho tạo cũng không cho sửa, nên trên thực tế không ai bật được nó.
    expect(trang).toContain('name="documentTypeIds"');
  });
});
