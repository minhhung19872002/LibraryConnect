/**
 * Đọc biểu ghi MARC 21 dạng JSON thành các dòng để bày ra bảng cho bạn đọc.
 *
 * Trang tra cứu là trang công khai; đổ thẳng JSON ra màn hình là bắt bạn đọc tự dịch dấu ngoặc.
 * Mọi phần mềm thư viện đều bày biểu ghi MARC thành bảng Nhãn trường · Chỉ thị · Trường con, kèm
 * tên trường bằng tiếng bản ngữ — cán bộ thư viện đọc quen dạng ấy, còn bạn đọc thường thì ít ra
 * cũng hiểu được dòng nào là nhan đề, dòng nào là tác giả.
 */

export interface MarcSubfieldView {
  code: string;
  value: string;
}

export interface MarcFieldView {
  tag: string;
  name: string;
  /** Trường điều khiển (001–009) không có chỉ thị và trường con, chỉ có một chuỗi giá trị. */
  isControl: boolean;
  ind1: string;
  ind2: string;
  value: string;
  subfields: MarcSubfieldView[];
}

export interface MarcRecordView {
  leader: string;
  fields: MarcFieldView[];
}

/**
 * Tên tiếng Việt của những trường MARC 21 hay gặp.
 *
 * Chỉ giữ ở mức thông dụng: trường lạ thì bày số nhãn là đủ, không cần chép cả bộ định nghĩa hai
 * trăm trường sang trang công khai.
 */
const TEN_TRUONG: Record<string, string> = {
  '001': 'Số kiểm soát',
  '003': 'Mã cơ quan cấp số kiểm soát',
  '005': 'Ngày giờ giao dịch gần nhất',
  '006': 'Yếu tố dữ liệu bổ sung',
  '007': 'Mô tả vật lý dạng mã',
  '008': 'Yếu tố dữ liệu có độ dài cố định',
  '010': 'Số kiểm soát Thư viện Quốc hội Mỹ',
  '020': 'Chỉ số ISBN',
  '022': 'Chỉ số ISSN',
  '024': 'Mã định danh khác',
  '035': 'Số kiểm soát của hệ thống khác',
  '040': 'Nguồn biên mục',
  '041': 'Mã ngôn ngữ',
  '044': 'Mã nước xuất bản',
  '082': 'Chỉ số phân loại DDC',
  '084': 'Chỉ số phân loại khác',
  '100': 'Tác giả cá nhân',
  '110': 'Tác giả tập thể',
  '111': 'Tên hội nghị',
  '130': 'Nhan đề đồng nhất',
  '210': 'Nhan đề viết tắt',
  '242': 'Nhan đề dịch',
  '245': 'Nhan đề và thông tin trách nhiệm',
  '246': 'Nhan đề khác',
  '250': 'Lần xuất bản',
  '260': 'Thông tin xuất bản',
  '264': 'Thông tin xuất bản, phát hành',
  '300': 'Mô tả vật lý',
  '310': 'Kỳ hạn xuất bản',
  '336': 'Loại nội dung',
  '337': 'Loại phương tiện',
  '338': 'Loại vật mang tin',
  '490': 'Thông tin tùng thư',
  '500': 'Phụ chú chung',
  '502': 'Phụ chú luận văn, luận án',
  '504': 'Phụ chú thư mục',
  '505': 'Phụ chú nội dung',
  '520': 'Tóm tắt',
  '546': 'Phụ chú ngôn ngữ',
  '600': 'Đề mục chủ đề — tên cá nhân',
  '610': 'Đề mục chủ đề — tên tập thể',
  '650': 'Đề mục chủ đề',
  '651': 'Đề mục chủ đề — địa danh',
  '653': 'Từ khóa tự do',
  '700': 'Tác giả bổ sung — cá nhân',
  '710': 'Tác giả bổ sung — tập thể',
  '711': 'Tác giả bổ sung — hội nghị',
  '773': 'Nguồn tài liệu chủ',
  '830': 'Tùng thư — tiêu đề bổ sung',
  '852': 'Ký hiệu xếp giá',
  '856': 'Địa chỉ điện tử',
};

export function tenTruong(tag: string): string {
  return TEN_TRUONG[tag] ?? 'Trường ' + tag;
}

/** Trường điều khiển theo MARC 21 là nhãn 001–009. */
export function laTruongDieuKhien(tag: string): boolean {
  return /^00\d$/.test(tag);
}

interface MarcJsonShape {
  leader?: string;
  controlFields?: { tag?: string; value?: string }[];
  dataFields?: {
    tag?: string;
    ind1?: string;
    ind2?: string;
    subfields?: { code?: string; value?: string }[];
  }[];
}

/**
 * Đọc chuỗi JSON của biểu ghi. Trả về null khi không đọc được — trang chi tiết vẫn phải hiện, chỉ
 * thẻ MARC báo là không đọc được biểu ghi, chứ không để cả trang trắng.
 */
export function docBieuGhiMarc(marcJson: string | null | undefined): MarcRecordView | null {
  if (!marcJson) {
    return null;
  }

  let parsed: MarcJsonShape;

  try {
    parsed = JSON.parse(marcJson) as MarcJsonShape;
  } catch {
    return null;
  }

  if (typeof parsed !== 'object' || parsed === null) {
    return null;
  }

  const fields: MarcFieldView[] = [];

  for (const field of parsed.controlFields ?? []) {
    const tag = (field.tag ?? '').trim();

    if (!tag) {
      continue;
    }

    fields.push({
      tag,
      name: tenTruong(tag),
      isControl: true,
      ind1: '',
      ind2: '',
      value: field.value ?? '',
      subfields: [],
    });
  }

  for (const field of parsed.dataFields ?? []) {
    const tag = (field.tag ?? '').trim();

    if (!tag) {
      continue;
    }

    fields.push({
      tag,
      name: tenTruong(tag),
      isControl: false,
      // Chỉ thị bỏ trống hiện bằng dấu gạch dưới, đúng quy ước nhà nghề cho ký tự trắng.
      ind1: hienChiThi(field.ind1),
      ind2: hienChiThi(field.ind2),
      value: '',
      subfields: (field.subfields ?? [])
        .filter((subfield) => (subfield.code ?? '').trim().length > 0)
        .map((subfield) => ({ code: (subfield.code ?? '').trim(), value: subfield.value ?? '' })),
    });
  }

  // Sắp theo nhãn trường như mọi phần mềm thư viện: người đọc quen tìm 245 ở gần đầu, 8xx ở cuối.
  fields.sort((a, b) => a.tag.localeCompare(b.tag));

  return { leader: parsed.leader ?? '', fields };
}

function hienChiThi(value: string | undefined): string {
  const chiThi = (value ?? '').trim();

  return chiThi.length > 0 ? chiThi : '_';
}
