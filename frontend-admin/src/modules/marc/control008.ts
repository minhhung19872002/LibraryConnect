/**
 * Bảng tra 40 vị trí của trường 008 — MARC 21 Bibliographic, phần "All Materials" và phần riêng
 * cho sách (Books, vị trí 18–34).
 *
 * The field is forty characters where meaning comes from position, so a cataloguer editing it as
 * raw text has to remember that position 33 is the literary form and that `1` there means fiction.
 * This table turns each position into a labelled control with the legal values spelled out, which
 * is what section II.2 of the tender asks for.
 *
 * Vị trí 18–34 phụ thuộc loại tài liệu (Leader/06). Bảng dưới đây khai phần của **sách** — dạng tài
 * liệu chiếm gần hết kho của một thư viện đại học. Với dạng khác, trình hướng dẫn chỉ hiện phần
 * chung và để nguyên khoảng 18–34 cho người dùng gõ tay, không đoán bừa.
 */

export const CONTROL_008_LENGTH = 40;

export interface Control008Option {
  code: string;
  label: string;
}

export interface Control008Position {
  /** Vị trí bắt đầu, đếm từ 0 như chuẩn MARC. */
  start: number;
  length: number;
  label: string;
  hint?: string;
  /** Không khai thì ô nhập là ô chữ tự do. */
  options?: Control008Option[];
  /** Chỉ hiện khi Leader/06 là `a` (tài liệu chữ viết) — nhóm vị trí riêng của sách. */
  booksOnly?: boolean;
  /** Máy chủ tự ghi khi lưu, người dùng không cần sửa. */
  readOnly?: boolean;
}

/** `#` trong tài liệu MARC là một khoảng trắng có nghĩa. */
export const BLANK = '#';

const YES_NO: Control008Option[] = [
  { code: '0', label: '0 — Không' },
  { code: '1', label: '1 — Có' },
  { code: '|', label: '| — Không xác định' },
];

export const CONTROL_008_POSITIONS: Control008Position[] = [
  {
    start: 0,
    length: 6,
    label: '00–05 Ngày nhập biểu ghi',
    hint: 'Dạng YYMMDD. Hệ thống điền khi tạo biểu ghi.',
    readOnly: true,
  },
  {
    start: 6,
    length: 1,
    label: '06 Loại ngày xuất bản',
    hint: 'Quyết định ý nghĩa của hai ô ngày bên dưới.',
    options: [
      { code: 's', label: 's — Một năm xuất bản duy nhất' },
      { code: 'm', label: 'm — Nhiều năm (bộ nhiều tập)' },
      { code: 'r', label: 'r — In lại, kèm năm bản gốc' },
      { code: 't', label: 't — Năm xuất bản và năm bản quyền' },
      { code: 'c', label: 'c — Ấn phẩm định kỳ đang tiếp tục' },
      { code: 'd', label: 'd — Ấn phẩm định kỳ đã ngừng' },
      { code: 'n', label: 'n — Không rõ năm' },
      { code: 'q', label: 'q — Năm không chắc chắn' },
      { code: '|', label: '| — Không xác định' },
    ],
  },
  { start: 7, length: 4, label: '07–10 Ngày 1', hint: 'Năm xuất bản, bốn chữ số.' },
  {
    start: 11,
    length: 4,
    label: '11–14 Ngày 2',
    hint: 'Năm thứ hai khi vị trí 06 đòi hỏi; không có thì để trống.',
  },
  {
    start: 15,
    length: 3,
    label: '15–17 Nơi xuất bản',
    hint: 'Mã nước theo MARC. Việt Nam là vm.',
  },
  {
    start: 18,
    length: 4,
    label: '18–21 Minh họa',
    hint: 'Tối đa bốn mã, ví dụ a = có tranh ảnh minh họa.',
    booksOnly: true,
  },
  {
    start: 22,
    length: 1,
    label: '22 Đối tượng đọc',
    booksOnly: true,
    options: [
      { code: BLANK, label: '# — Không rõ hoặc không áp dụng' },
      { code: 'a', label: 'a — Tuổi mẫu giáo' },
      { code: 'b', label: 'b — Tiểu học' },
      { code: 'c', label: 'c — Trung học cơ sở' },
      { code: 'd', label: 'd — Trung học phổ thông' },
      { code: 'e', label: 'e — Người lớn' },
      { code: 'f', label: 'f — Chuyên ngành' },
      { code: 'g', label: 'g — Phổ thông' },
      { code: 'j', label: 'j — Thiếu nhi' },
      { code: '|', label: '| — Không xác định' },
    ],
  },
  {
    start: 23,
    length: 1,
    label: '23 Hình thức tài liệu',
    booksOnly: true,
    options: [
      { code: BLANK, label: '# — Bản in thường' },
      { code: 'a', label: 'a — Vi phim' },
      { code: 'b', label: 'b — Vi phiếu' },
      { code: 'd', label: 'd — Bản in chữ lớn' },
      { code: 'o', label: 'o — Trực tuyến' },
      { code: 'q', label: 'q — Điện tử, vật mang tin trực tiếp' },
      { code: 'r', label: 'r — Bản in lại từ bản gốc' },
      { code: '|', label: '| — Không xác định' },
    ],
  },
  {
    start: 24,
    length: 4,
    label: '24–27 Nội dung',
    hint: 'Tối đa bốn mã, ví dụ b = thư mục, d = từ điển, m = luận văn.',
    booksOnly: true,
  },
  { start: 28, length: 1, label: '28 Xuất bản của cơ quan nhà nước', booksOnly: true },
  { start: 29, length: 1, label: '29 Kỷ yếu hội nghị', booksOnly: true, options: YES_NO },
  { start: 30, length: 1, label: '30 Sách kỷ niệm', booksOnly: true, options: YES_NO },
  { start: 31, length: 1, label: '31 Có bảng tra', booksOnly: true, options: YES_NO },
  {
    start: 33,
    length: 1,
    label: '33 Thể loại văn học',
    booksOnly: true,
    options: [
      { code: '0', label: '0 — Không phải văn học' },
      { code: '1', label: '1 — Tác phẩm văn học' },
      { code: 'd', label: 'd — Kịch' },
      { code: 'e', label: 'e — Tiểu luận' },
      { code: 'f', label: 'f — Tiểu thuyết' },
      { code: 'h', label: 'h — Truyện cười, châm biếm' },
      { code: 'i', label: 'i — Thư từ' },
      { code: 'j', label: 'j — Truyện ngắn' },
      { code: 'p', label: 'p — Thơ' },
      { code: 'u', label: 'u — Không rõ' },
      { code: '|', label: '| — Không xác định' },
    ],
  },
  {
    start: 34,
    length: 1,
    label: '34 Tiểu sử',
    booksOnly: true,
    options: [
      { code: BLANK, label: '# — Không phải tài liệu tiểu sử' },
      { code: 'a', label: 'a — Tự truyện' },
      { code: 'b', label: 'b — Tiểu sử một người' },
      { code: 'c', label: 'c — Tiểu sử nhiều người' },
      { code: 'd', label: 'd — Có phần tiểu sử' },
      { code: '|', label: '| — Không xác định' },
    ],
  },
  {
    start: 35,
    length: 3,
    label: '35–37 Ngôn ngữ',
    hint: 'Mã ISO 639-2. Tiếng Việt là vie.',
  },
  {
    start: 38,
    length: 1,
    label: '38 Biểu ghi đã sửa',
    options: [
      { code: BLANK, label: '# — Chưa sửa' },
      { code: 'd', label: 'd — Có ký tự bị bỏ bớt' },
      { code: 'o', label: 'o — Chuyển tự hoàn toàn' },
      { code: 'r', label: 'r — Hoàn nguyên từ bản chuyển tự' },
      { code: 'x', label: 'x — Thiếu ký tự' },
      { code: '|', label: '| — Không xác định' },
    ],
  },
  {
    start: 39,
    length: 1,
    label: '39 Nguồn biên mục',
    options: [
      { code: BLANK, label: '# — Cơ quan biên mục quốc gia' },
      { code: 'c', label: 'c — Chương trình biên mục hợp tác' },
      { code: 'd', label: 'd — Cơ quan khác' },
      { code: 'u', label: 'u — Không rõ' },
      { code: '|', label: '| — Không xác định' },
    ],
  },
];

/** Leader/06 = `a` là tài liệu chữ viết; chỉ khi ấy khối 18–34 mới mang nghĩa của sách. */
export function isBookMaterial(leader: string): boolean {
  return (leader.padEnd(24, ' ')[6] ?? ' ') === 'a';
}

/** Đổi khoảng trắng thật thành `#` để hiện trên màn hình, và ngược lại khi ghi xuống. */
export function toDisplay(value: string): string {
  return value.replace(/ /g, BLANK);
}

export function fromDisplay(value: string): string {
  return value.replace(new RegExp(BLANK, 'g'), ' ');
}
