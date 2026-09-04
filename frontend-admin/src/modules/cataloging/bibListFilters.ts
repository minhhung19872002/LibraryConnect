import type { BibListParams } from './api';
import { RECORD_STATUS_LABELS, type RecordStatus } from './types';

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/** Các bộ lọc nhận diện bằng mã định danh mà một màn hình khác có thể gửi sang qua địa chỉ. */
const ID_FILTERS = [
  'customIndexValueId',
  'documentTypeId',
  'languageId',
  'publisherId',
  'authorId',
  'subjectId',
  'classificationId',
  'collectionId',
] as const;

type IdFilterKey = (typeof ID_FILTERS)[number];

/** Tên hiện trên thẻ bộ lọc cho từng loại liên kết đến. */
const ID_FILTER_LABELS: Record<IdFilterKey, string> = {
  customIndexValueId: 'Danh mục tự tạo',
  documentTypeId: 'Dạng tài liệu',
  languageId: 'Ngôn ngữ',
  publisherId: 'Nhà xuất bản',
  authorId: 'Tác giả',
  subjectId: 'Chủ đề',
  classificationId: 'Phân loại',
  collectionId: 'Bộ sưu tập',
};

/**
 * Tên tham số mang nhãn đọc được của bộ lọc — màn hình gửi liên kết biết giá trị ấy tên gì, còn
 * màn hình nhận chỉ có mã định danh. Không có nhãn thì thẻ hiện mã rút gọn, vẫn bỏ được.
 */
export const FILTER_LABEL_PARAM = 'nhan';

/**
 * Dựng bộ lọc danh sách biểu ghi từ chuỗi truy vấn trên địa chỉ.
 *
 * Màn hình danh mục tự tạo dẫn sang `/bien-muc?customIndexValueId=…`, và mọi màn hình khác cũng có
 * thể dẫn sang bằng cùng cách. Chỉ nhận giá trị đúng dạng: mã định danh phải là GUID, năm phải là
 * số, cờ phải là `true` — địa chỉ là thứ người dùng sửa được bằng tay.
 */
export function parseBibListParams(search: URLSearchParams): BibListParams {
  const filter: BibListParams = {};

  for (const key of ID_FILTERS) {
    const value = search.get(key);

    if (value && GUID.test(value)) {
      filter[key] = value;
    }
  }

  const keyword = search.get('keyword')?.trim();

  if (keyword) {
    filter.keyword = keyword;
  }

  const status = search.get('status');

  if (status && status in RECORD_STATUS_LABELS) {
    filter.status = status as RecordStatus;
  }

  const from = Number.parseInt(search.get('publishYearFrom') ?? '', 10);
  const to = Number.parseInt(search.get('publishYearTo') ?? '', 10);

  if (Number.isFinite(from)) {
    filter.publishYearFrom = from;
  }

  if (Number.isFinite(to)) {
    filter.publishYearTo = to;
  }

  if (search.get('withoutItems') === 'true') {
    filter.withoutItems = true;
  }

  if (search.get('availableOnly') === 'true') {
    filter.availableOnly = true;
  }

  return filter;
}

/** Một bộ lọc đến từ liên kết, đủ để hiện thành thẻ có nút bỏ. */
export interface LinkedFilter {
  key: IdFilterKey;
  label: string;
  value: string;
}

/**
 * Những bộ lọc đến từ liên kết mà thanh lọc của trang không có ô riêng để hiện — người dùng phải
 * nhìn thấy chúng, nếu không thì bảng "thiếu biểu ghi" mà không hiểu vì sao.
 */
export function linkedFilters(search: URLSearchParams): LinkedFilter[] {
  const filter = parseBibListParams(search);
  const label = search.get(FILTER_LABEL_PARAM)?.trim();
  const keys = ID_FILTERS.filter((key) => filter[key] !== undefined);

  // The label names one value; with several linked filters there is no telling which, so each
  // falls back to its shortened identifier.
  const named = keys.length === 1 && label ? label : undefined;

  return keys.map((key) => ({
    key,
    label: ID_FILTER_LABELS[key],
    value: named ?? `#${filter[key]!.slice(0, 8)}`,
  }));
}
