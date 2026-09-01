import type { DigitalCollectionNode } from '@/types/api';

/**
 * Bộ lọc của trang Tài liệu số.
 *
 * Đặc tả IX.4 đòi bạn đọc thu hẹp được danh sách tài liệu số, chứ không chỉ gõ đúng nhan đề mới
 * tìm ra. Ba bộ lọc dưới đây là ba câu hỏi bạn đọc thật sự đặt ra: tài liệu thuộc mảng nào, ở dạng
 * tệp gì, và mình có mở được không.
 */

export interface LuaChon {
  value: string;
  label: string;
}

/** Nhóm định dạng, đặt tên đúng như máy chủ phân nhóm. */
export const NHOM_DINH_DANG: LuaChon[] = [
  { value: 'PDF', label: 'Tệp PDF' },
  { value: 'OFFICE', label: 'Tài liệu Office' },
  { value: 'VIDEO', label: 'Video' },
  { value: 'AUDIO', label: 'Âm thanh' },
  { value: 'IMAGE', label: 'Hình ảnh' },
  { value: 'OTHER', label: 'Định dạng khác' },
];

/**
 * Mức truy cập bạn đọc chọn được.
 *
 * Không nêu mức "Cấm": tài liệu ấy không bao giờ hiện ra ngoài, cho vào bộ lọc chỉ tổ làm bạn đọc
 * chọn rồi nhận danh sách rỗng mà không hiểu vì sao.
 */
export const MUC_TRUY_CAP: LuaChon[] = [
  { value: 'Public', label: 'Công khai — đọc ngay' },
  { value: 'Internal', label: 'Nội bộ — cần đăng nhập' },
  { value: 'Restricted', label: 'Hạn chế — phải xin phép' },
];

/**
 * Trải cây bộ sưu tập thành danh sách chọn, thụt lề theo cấp.
 *
 * Ô chọn một cấp dễ dùng hơn cây bấm mở trên trang công khai, nhưng vẫn phải thấy nhánh con thuộc
 * về nhánh cha nào. Thụt lề bằng khoảng trắng không ngắt (U+00A0) vì khoảng trắng thường bị trình
 * duyệt gộp lại thành một khi hiển thị trong ô chọn.
 */
export function traiCayBoSuuTap(
  nodes: DigitalCollectionNode[] | undefined,
  cap = 0,
): LuaChon[] {
  return (nodes ?? []).flatMap((node) => [
    {
      value: node.id,
      label: `${'  '.repeat(cap)}${node.name}`
        + (node.documentCount > 0 ? ` (${node.documentCount})` : ''),
    },
    ...traiCayBoSuuTap(node.children, cap + 1),
  ]);
}

/** Bộ lọc nào đang bật — dùng để hiện nút "Bỏ lọc" đúng lúc. */
export function dangLoc(filter: {
  collectionId?: string;
  formatGroup?: string;
  accessLevel?: string;
  fullText?: boolean;
}): boolean {
  return Boolean(
    filter.collectionId || filter.formatGroup || filter.accessLevel || filter.fullText,
  );
}
