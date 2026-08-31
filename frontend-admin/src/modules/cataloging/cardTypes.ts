/** Mẫu phích và in phích thư mục (II.10). */

export type CardType = 'MAIN' | 'TITLE' | 'SUBJECT' | 'CLASSIFICATION';

export interface CardBox {
  x: number;
  y: number;
  width: number;
  height: number;
  /**
   * Nội dung của ô: một trường tổng hợp (heading, isbd, callNumber, tracings…), một đường dẫn MARC
   * (245$a), hoặc văn bản cố định nếu đặt trong dấu nháy kép.
   */
  source: string;
  fontSize: number;
  bold: boolean;
  italic: boolean;
  align: 'left' | 'center' | 'right';
  border: boolean;
  prefix?: string | null;
}

export interface CardLayout {
  boxes: CardBox[];
  /** Lề trong của phích, tính bằng milimét. */
  padding: number;
  showBorder: boolean;
}

export interface CardTemplate {
  id: string;
  code: string;
  name: string;
  cardType: CardType;
  cardTypeName: string;
  widthMm: number;
  heightMm: number;
  isDefault: boolean;
  isActive: boolean;
  layout: CardLayout;
}

export const CARD_TYPE_LABELS: Record<CardType, string> = {
  MAIN: 'Phích chính (tác giả)',
  TITLE: 'Phích nhan đề',
  SUBJECT: 'Phích chủ đề',
  CLASSIFICATION: 'Phích phân loại',
};

/**
 * Các nguồn nội dung chọn được cho một ô.
 *
 * The composed values come first because they are what a card is actually made of; the raw MARC
 * paths are there for the field a particular library wants that the composed list does not cover.
 */
export const CARD_SOURCES: Array<{ value: string; label: string; group: string }> = [
  { value: 'heading', label: 'Tiêu đề của phích (thay đổi theo loại phích)', group: 'Tổng hợp' },
  { value: 'isbd', label: 'Mô tả ISBD đầy đủ', group: 'Tổng hợp' },
  { value: 'callNumber', label: 'Ký hiệu xếp giá', group: 'Tổng hợp' },
  { value: 'tracings', label: 'Dòng truy hồi', group: 'Tổng hợp' },
  { value: 'title', label: 'Nhan đề', group: 'Tổng hợp' },
  { value: 'author', label: 'Tác giả chính', group: 'Tổng hợp' },
  { value: 'publication', label: 'Thông tin xuất bản', group: 'Tổng hợp' },
  { value: 'physical', label: 'Mô tả vật lý', group: 'Tổng hợp' },
  { value: 'isbn', label: 'Chỉ số ISBN', group: 'Tổng hợp' },
  { value: 'ddc', label: 'Chỉ số DDC', group: 'Tổng hợp' },
  { value: 'abstract', label: 'Tóm tắt', group: 'Tổng hợp' },
  { value: 'controlNumber', label: 'Số kiểm soát biểu ghi', group: 'Tổng hợp' },
  { value: '245$a', label: '245$a — Nhan đề chính', group: 'Trường MARC' },
  { value: '245$c', label: '245$c — Thông tin trách nhiệm', group: 'Trường MARC' },
  { value: '100$a', label: '100$a — Tác giả cá nhân', group: 'Trường MARC' },
  { value: '260$b', label: '260$b — Nhà xuất bản', group: 'Trường MARC' },
  { value: '300$a', label: '300$a — Số trang', group: 'Trường MARC' },
  { value: '520$a', label: '520$a — Tóm tắt', group: 'Trường MARC' },
];

/** Ô mới thêm vào mẫu: đủ lớn để đọc được, đặt ở góc trên trái. */
export function newBox(): CardBox {
  return {
    x: 4,
    y: 4,
    width: 60,
    height: 8,
    source: 'title',
    fontSize: 9,
    bold: false,
    italic: false,
    align: 'left',
    border: false,
  };
}

/** Mẫu phích chuẩn 12,5 × 7,5 cm dùng làm điểm khởi đầu khi tạo mẫu mới. */
export function defaultLayout(): CardLayout {
  return {
    padding: 5,
    showBorder: true,
    boxes: [
      { x: 0, y: 0, width: 28, height: 20, source: 'callNumber', fontSize: 9, bold: true, italic: false, align: 'left', border: false },
      { x: 30, y: 0, width: 85, height: 8, source: 'heading', fontSize: 10, bold: true, italic: false, align: 'left', border: false },
      { x: 30, y: 9, width: 85, height: 32, source: 'isbd', fontSize: 8, bold: false, italic: false, align: 'left', border: false },
      { x: 0, y: 45, width: 115, height: 14, source: 'tracings', fontSize: 7, bold: false, italic: false, align: 'left', border: false },
      { x: 0, y: 60, width: 60, height: 5, source: 'controlNumber', fontSize: 7, bold: false, italic: false, align: 'left', border: false, prefix: 'SKS: ' },
    ],
  };
}
