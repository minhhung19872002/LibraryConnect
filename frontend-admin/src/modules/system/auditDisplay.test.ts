import { describe, expect, it } from 'vitest';
import { maRutGon, moTaBanGhi } from './auditDisplay';

describe('Tên bản ghi trong nhật ký hệ thống', () => {
  it('dùng tên đã chép lại khi có', () => {
    expect(moTaBanGhi('Giáo trình cơ sở dữ liệu', 'Biểu ghi thư mục'))
      .toBe('Giáo trình cơ sở dữ liệu');
  });

  it('không có tên thì nói bằng tiếng Việt, không đổ mã định danh máy ra', () => {
    const mo_ta = moTaBanGhi(null, 'Liên kết biểu ghi – tác giả');

    expect(mo_ta).toBe('(một liên kết biểu ghi – tác giả không có tên)');
    expect(mo_ta).not.toMatch(/[0-9a-f]{8}-[0-9a-f]{4}/);
  });

  it('không có cả tên lẫn loại đối tượng thì vẫn ra câu đọc được', () => {
    expect(moTaBanGhi('', '')).toBe('(không có tên)');
    expect(moTaBanGhi(undefined, undefined)).toBe('(không có tên)');
  });

  it('bỏ khoảng trắng thừa', () => {
    expect(moTaBanGhi('  Nguyễn Văn An  ', 'Bạn đọc')).toBe('Nguyễn Văn An');
  });
});

describe('Mã định danh rút gọn', () => {
  it('cắt ngắn để đối chiếu được mà không chiếm cả cột', () => {
    expect(maRutGon('1b4c4855-804f-400d-a3f3-f493908256bf')).toBe('1b4c4855…');
  });

  it('mã ngắn thì giữ nguyên', () => {
    expect(maRutGon('LC000123')).toBe('LC000123');
    expect(maRutGon(null)).toBe('');
  });
});
