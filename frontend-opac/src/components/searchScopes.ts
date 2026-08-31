import type { SearchScope } from '@/types/api';

/** Phạm vi tìm kiếm hiện trên ô chọn cạnh ô nhập từ khóa (IX.2). */
export const SCOPE_OPTIONS: { value: SearchScope; label: string }[] = [
  { value: 'All', label: 'Tất cả' },
  { value: 'Title', label: 'Nhan đề' },
  { value: 'Author', label: 'Tác giả' },
  { value: 'Subject', label: 'Chủ đề' },
  { value: 'Keyword', label: 'Từ khóa' },
  { value: 'Publisher', label: 'Nhà xuất bản' },
  { value: 'Isbn', label: 'ISBN / ISSN' },
  { value: 'CallNumber', label: 'Ký hiệu xếp giá' },
];
