/**
 * Dựng ảnh bìa thay thế cho tài liệu chưa có ảnh.
 *
 * Phần lớn biểu ghi trong một thư viện Việt Nam không có ảnh bìa: sách cũ, luận văn, đề tài nghiên
 * cứu, báo cáo nội bộ đều không có ảnh trên mạng. Để trống thì trang kết quả tra cứu thành một dãy
 * ô xám trông như trang hỏng. Các thư viện số vẫn sinh một bìa thay thế có nhan đề và tác giả — vừa
 * đọc được, vừa phân biệt được cuốn này với cuốn kia.
 *
 * Màu nền lấy theo nhan đề nên cùng một cuốn luôn ra cùng một màu; bạn đọc nhớ mặt được cuốn sách
 * mình vừa xem khi quay lại danh sách.
 */

/** Bảng màu dịu, đủ tương phản với chữ trắng để đạt mức AA của WCAG. */
const BANG_MAU: readonly string[] = [
  '#1f5f4b',
  '#2b5876',
  '#5b4b8a',
  '#7a4b3a',
  '#3f6212',
  '#7c2d4a',
  '#155e75',
  '#7c4a03',
];

export interface CoverPlaceholder {
  /** Màu nền của bìa. */
  background: string;
  /** Nhan đề đã cắt cho vừa bìa. */
  title: string;
  /** Tác giả đã cắt, hoặc chuỗi rỗng khi biểu ghi chưa có tác giả. */
  author: string;
  /** Nhãn nhỏ ở chân bìa: dạng tài liệu. */
  label: string;
}

/** Băm chuỗi thành một số nguyên ổn định — cùng nhan đề thì luôn cùng màu. */
function bam(text: string): number {
  let hash = 0;

  for (let index = 0; index < text.length; index += 1) {
    hash = (hash * 31 + text.charCodeAt(index)) | 0;
  }

  return Math.abs(hash);
}

/** Cắt chuỗi cho vừa bìa, cắt ở khoảng trắng gần nhất để không đứt giữa từ. */
export function catChuoi(text: string, gioiHan: number): string {
  const gon = text.trim().replace(/\s+/g, ' ');

  if (gon.length <= gioiHan) {
    return gon;
  }

  const cat = gon.slice(0, gioiHan);
  const khoangTrang = cat.lastIndexOf(' ');

  return (khoangTrang > gioiHan * 0.6 ? cat.slice(0, khoangTrang) : cat).trimEnd() + '…';
}

export function coverPlaceholder(item: {
  title?: string | null;
  authorMain?: string | null;
  documentTypeName?: string | null;
}): CoverPlaceholder {
  const title = (item.title ?? '').trim();

  return {
    background: BANG_MAU[bam(title || 'khong-co-nhan-de') % BANG_MAU.length]!,
    title: catChuoi(title || 'Chưa có nhan đề', 70),
    author: item.authorMain ? catChuoi(item.authorMain, 34) : '',
    label: (item.documentTypeName ?? 'Tài liệu').trim(),
  };
}
