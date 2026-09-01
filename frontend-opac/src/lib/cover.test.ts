import { describe, expect, it } from 'vitest';
import { catChuoi, coverPlaceholder } from './cover';

describe('Ảnh bìa thay thế', () => {
  it('mang nhan đề và tác giả để bạn đọc phân biệt được cuốn nào với cuốn nào', () => {
    const bia = coverPlaceholder({
      title: 'Giáo trình cơ sở dữ liệu',
      authorMain: 'Nguyễn Văn An',
      documentTypeName: 'Giáo trình',
    });

    expect(bia.title).toBe('Giáo trình cơ sở dữ liệu');
    expect(bia.author).toBe('Nguyễn Văn An');
    expect(bia.label).toBe('Giáo trình');
  });

  it('cùng một nhan đề thì luôn ra cùng một màu', () => {
    const mot = coverPlaceholder({ title: 'Quản lý tài nguyên nước' });
    const hai = coverPlaceholder({ title: 'Quản lý tài nguyên nước' });

    expect(mot.background).toBe(hai.background);
  });

  it('nhan đề khác nhau thì phần lớn ra màu khác nhau', () => {
    const mau = new Set(
      ['Thủy văn', 'Trắc địa', 'Kinh tế học', 'Luật đất đai', 'Sinh thái học'].map(
        (title) => coverPlaceholder({ title }).background,
      ),
    );

    expect(mau.size).toBeGreaterThan(2);
  });

  it('biểu ghi chưa có nhan đề vẫn dựng được bìa, không để trống trơn', () => {
    const bia = coverPlaceholder({ title: null, authorMain: null, documentTypeName: null });

    expect(bia.title).toBe('Chưa có nhan đề');
    expect(bia.author).toBe('');
    expect(bia.label).toBe('Tài liệu');
    expect(bia.background).toMatch(/^#[0-9a-f]{6}$/i);
  });

  it('nhan đề luận văn dài bị cắt cho vừa bìa', () => {
    const dai =
      'Nghiên cứu nguyên nhân sự cố sạt trượt và đề xuất giải pháp ổn định tràn sự cố qua đê '
      + 'vùng đồng bằng sông Cửu Long trong điều kiện biến đổi khí hậu';

    const bia = coverPlaceholder({ title: dai });

    expect(bia.title.length).toBeLessThanOrEqual(71);
    expect(bia.title.endsWith('…')).toBe(true);
  });
});

describe('Cắt chuỗi cho vừa bìa', () => {
  it('không cắt khi chuỗi đã đủ ngắn', () => {
    expect(catChuoi('Thủy văn học', 30)).toBe('Thủy văn học');
  });

  it('cắt ở khoảng trắng chứ không đứt giữa từ', () => {
    expect(catChuoi('Giáo trình quản lý tài nguyên nước', 20)).toBe('Giáo trình quản lý…');
  });

  it('gộp khoảng trắng thừa', () => {
    expect(catChuoi('  Thủy   văn  ', 30)).toBe('Thủy văn');
  });
});
