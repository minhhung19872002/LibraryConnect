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
  /**
   * Khối 18–34 của loại hình nào. Không khai thì là vị trí chung, hiện cho mọi loại hình;
   * `booksOnly` là cách viết cũ của `material: 'books'`.
   */
  material?: Control008Material;
  /** Máy chủ tự ghi khi lưu, người dùng không cần sửa. */
  readOnly?: boolean;
}

/**
 * Loại hình quyết định nghĩa của khối 18–34, theo bảng "008 — Configuration" của MARC 21:
 * Leader/06 = a với Leader/07 ∈ {b, i, s} là ấn phẩm định kỳ; a với {a, c, d, m} là sách;
 * e hoặc f là bản đồ. Luận văn là sách (mã `m` ở vị trí 24–27), không có khối riêng.
 */
export type Control008Material = 'books' | 'continuing' | 'maps' | 'other';

export const CONTROL_008_MATERIAL_LABELS: Record<Control008Material, string> = {
  books: 'Sách',
  continuing: 'Ấn phẩm định kỳ',
  maps: 'Bản đồ',
  other: 'Loại hình khác',
};

export function materialOf(leader: string): Control008Material {
  const padded = leader.padEnd(24, ' ');
  const type = padded[6] ?? ' ';
  const level = padded[7] ?? ' ';

  if (type === 'e' || type === 'f') {
    return 'maps';
  }

  if (type === 'a') {
    return 'b' === level || 'i' === level || 's' === level ? 'continuing' : 'books';
  }

  return 'other';
}

/** Các vị trí của một loại hình: vị trí chung cộng khối 18–34 của đúng loại hình ấy. */
export function positionsFor(material: Control008Material): Control008Position[] {
  return CONTROL_008_POSITIONS.filter((entry) => {
    const own = entry.material ?? (entry.booksOnly ? 'books' : undefined);
    return own === undefined || own === material;
  });
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
  // ---- Ấn phẩm định kỳ (Leader/06 = a, Leader/07 ∈ {b, i, s}) ----
  {
    start: 18,
    length: 1,
    label: '18 Kỳ hạn xuất bản',
    material: 'continuing',
    options: [
      { code: BLANK, label: '# — Không xác định được kỳ hạn' },
      { code: 'a', label: 'a — Hằng năm' },
      { code: 'b', label: 'b — Hai tháng một kỳ' },
      { code: 'c', label: 'c — Hai kỳ một tuần' },
      { code: 'd', label: 'd — Nhật báo' },
      { code: 'e', label: 'e — Hai tuần một kỳ' },
      { code: 'f', label: 'f — Nửa năm một kỳ' },
      { code: 'g', label: 'g — Hai năm một kỳ' },
      { code: 'h', label: 'h — Ba năm một kỳ' },
      { code: 'i', label: 'i — Ba kỳ một tuần' },
      { code: 'j', label: 'j — Ba kỳ một tháng' },
      { code: 'k', label: 'k — Cập nhật liên tục' },
      { code: 'm', label: 'm — Hằng tháng' },
      { code: 'q', label: 'q — Hằng quý' },
      { code: 's', label: 's — Nửa tháng một kỳ' },
      { code: 't', label: 't — Ba kỳ một năm' },
      { code: 'u', label: 'u — Không rõ' },
      { code: 'w', label: 'w — Hằng tuần' },
      { code: 'z', label: 'z — Khác' },
      { code: '|', label: '| — Không xác định' },
    ],
  },
  {
    start: 19,
    length: 1,
    label: '19 Tính đều đặn',
    material: 'continuing',
    options: [
      { code: 'n', label: 'n — Không đều nhưng có quy luật' },
      { code: 'r', label: 'r — Đều đặn' },
      { code: 'u', label: 'u — Không rõ' },
      { code: 'x', label: 'x — Hoàn toàn không đều' },
      { code: '|', label: '| — Không xác định' },
    ],
  },
  {
    start: 21,
    length: 1,
    label: '21 Loại ấn phẩm định kỳ',
    material: 'continuing',
    options: [
      { code: BLANK, label: '# — Không thuộc loại nào dưới đây' },
      { code: 'd', label: 'd — Cơ sở dữ liệu cập nhật' },
      { code: 'l', label: 'l — Tài liệu tờ rời cập nhật' },
      { code: 'm', label: 'm — Tùng thư chuyên khảo' },
      { code: 'n', label: 'n — Báo' },
      { code: 'p', label: 'p — Tạp chí' },
      { code: 'w', label: 'w — Trang web cập nhật' },
      { code: '|', label: '| — Không xác định' },
    ],
  },
  {
    start: 22,
    length: 1,
    label: '22 Hình thức bản gốc',
    material: 'continuing',
    options: [
      { code: BLANK, label: '# — Bản in thường' },
      { code: 'a', label: 'a — Vi phim' },
      { code: 'b', label: 'b — Vi phiếu' },
      { code: 'd', label: 'd — Bản in chữ lớn' },
      { code: 'e', label: 'e — Khổ báo' },
      { code: 'o', label: 'o — Trực tuyến' },
      { code: 'q', label: 'q — Điện tử, vật mang tin trực tiếp' },
      { code: 's', label: 's — Điện tử' },
      { code: '|', label: '| — Không xác định' },
    ],
  },
  {
    start: 23,
    length: 1,
    label: '23 Hình thức tài liệu',
    material: 'continuing',
    options: [
      { code: BLANK, label: '# — Bản in thường' },
      { code: 'a', label: 'a — Vi phim' },
      { code: 'b', label: 'b — Vi phiếu' },
      { code: 'd', label: 'd — Bản in chữ lớn' },
      { code: 'o', label: 'o — Trực tuyến' },
      { code: 'q', label: 'q — Điện tử, vật mang tin trực tiếp' },
      { code: 's', label: 's — Điện tử' },
      { code: '|', label: '| — Không xác định' },
    ],
  },
  {
    start: 24,
    length: 1,
    label: '24 Bản chất toàn bộ ấn phẩm',
    hint: 'Một mã: a = tóm tắt, b = thư mục, c = mục lục, d = từ điển, e = bách khoa, p = kỷ yếu, # = không nêu.',
    material: 'continuing',
  },
  {
    start: 25,
    length: 3,
    label: '25–27 Bản chất nội dung',
    hint: 'Tối đa ba mã, cùng bảng mã với vị trí 24.',
    material: 'continuing',
  },
  { start: 28, length: 1, label: '28 Xuất bản của cơ quan nhà nước', material: 'continuing' },
  { start: 29, length: 1, label: '29 Kỷ yếu hội nghị', material: 'continuing', options: YES_NO },
  {
    start: 33,
    length: 1,
    label: '33 Bảng chữ cái của nhan đề gốc',
    material: 'continuing',
    options: [
      { code: BLANK, label: '# — Không có / không áp dụng' },
      { code: 'a', label: 'a — La-tinh cơ bản' },
      { code: 'b', label: 'b — La-tinh mở rộng' },
      { code: 'c', label: 'c — Ki-rin' },
      { code: 'd', label: 'd — Nhật' },
      { code: 'e', label: 'e — Hán' },
      { code: 'f', label: 'f — Ả Rập' },
      { code: 'g', label: 'g — Hy Lạp' },
      { code: 'h', label: 'h — Hê-brơ' },
      { code: 'i', label: 'i — Thái' },
      { code: 'j', label: 'j — Devanagari' },
      { code: 'k', label: 'k — Hàn' },
      { code: 'l', label: 'l — Tamil' },
      { code: 'u', label: 'u — Không rõ' },
      { code: 'z', label: 'z — Khác' },
      { code: '|', label: '| — Không xác định' },
    ],
  },
  {
    start: 34,
    length: 1,
    label: '34 Quy ước lập tiêu đề',
    material: 'continuing',
    options: [
      { code: '0', label: '0 — Tiêu đề kế tiếp (mỗi lần đổi tên một biểu ghi)' },
      { code: '1', label: '1 — Tiêu đề mới nhất' },
      { code: '2', label: '2 — Tiêu đề tích hợp' },
      { code: '|', label: '| — Không xác định' },
    ],
  },

  // ---- Bản đồ (Leader/06 = e hoặc f) ----
  {
    start: 18,
    length: 4,
    label: '18–21 Địa hình',
    hint: 'Tối đa bốn mã: a = đường bình độ, b = tô bóng, e = tô màu theo độ cao, # = không.',
    material: 'maps',
  },
  {
    start: 22,
    length: 2,
    label: '22–23 Phép chiếu',
    hint: 'Hai ký tự theo bảng mã MARC, ví dụ bd = Mercator; ## = không nêu.',
    material: 'maps',
  },
  {
    start: 25,
    length: 1,
    label: '25 Loại tài liệu bản đồ',
    material: 'maps',
    options: [
      { code: 'a', label: 'a — Bản đồ đơn' },
      { code: 'b', label: 'b — Bộ bản đồ' },
      { code: 'c', label: 'c — Bản đồ định kỳ' },
      { code: 'd', label: 'd — Quả địa cầu' },
      { code: 'e', label: 'e — Tập bản đồ (atlas)' },
      { code: 'f', label: 'f — Phụ bản rời của tài liệu khác' },
      { code: 'g', label: 'g — Đóng kèm trong tài liệu khác' },
      { code: 'u', label: 'u — Không rõ' },
      { code: 'z', label: 'z — Khác' },
      { code: '|', label: '| — Không xác định' },
    ],
  },
  { start: 28, length: 1, label: '28 Xuất bản của cơ quan nhà nước', material: 'maps' },
  {
    start: 29,
    length: 1,
    label: '29 Hình thức tài liệu',
    material: 'maps',
    options: [
      { code: BLANK, label: '# — Bản in thường' },
      { code: 'a', label: 'a — Vi phim' },
      { code: 'b', label: 'b — Vi phiếu' },
      { code: 'd', label: 'd — Bản in chữ lớn' },
      { code: 'o', label: 'o — Trực tuyến' },
      { code: 'q', label: 'q — Điện tử, vật mang tin trực tiếp' },
      { code: 'r', label: 'r — Bản in lại' },
      { code: '|', label: '| — Không xác định' },
    ],
  },
  { start: 31, length: 1, label: '31 Có bảng tra', material: 'maps', options: YES_NO },
  {
    start: 33,
    length: 2,
    label: '33–34 Đặc điểm định dạng riêng',
    hint: 'Tối đa hai mã: e = bản khắc, j = tranh vẽ, k = bưu ảnh, n = trò chơi, p = câu đố, # = không.',
    material: 'maps',
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
