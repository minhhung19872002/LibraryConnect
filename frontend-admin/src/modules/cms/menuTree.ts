import type { CmsMenu } from './types';

/**
 * Cây menu của trang tra cứu (VIII.1): kéo thả để sắp lại thứ tự và cấp bậc, rồi gửi toàn bộ vị
 * trí mới về máy chủ trong một lượt. Phần tính toán tách ra đây để kiểm bằng vitest mà không phải
 * dựng cây Ant Design.
 */

/** Thả vào đâu so với mục đích: thành con của nó, đứng trước nó, hay đứng sau nó. */
export type DropMode = 'inside' | 'before' | 'after';

/**
 * Suy ra vị trí thả từ thông tin Ant Design đưa ra: `dropToGap` là thả vào khe giữa hai mục, còn
 * `dropPosition` so với `pos` của mục đích cho biết khe trên hay khe dưới.
 */
export function dropModeOf(dropToGap: boolean, dropPosition: number, nodePos: string): DropMode {
  if (!dropToGap) return 'inside';

  const segments = nodePos.split('-');
  const own = Number(segments[segments.length - 1]);

  return dropPosition - own === -1 ? 'before' : 'after';
}

/** Mục có mã này, tìm ở mọi cấp. */
export function findMenu(tree: CmsMenu[], id: string): CmsMenu | undefined {
  for (const item of tree) {
    if (item.id === id) return item;
    const found = findMenu(item.children, id);
    if (found) return found;
  }

  return undefined;
}

function contains(item: CmsMenu, id: string): boolean {
  return item.children.some((child) => child.id === id || contains(child, id));
}

function remove(tree: CmsMenu[], id: string): CmsMenu[] {
  return tree
    .filter((item) => item.id !== id)
    .map((item) => ({ ...item, children: remove(item.children, id) }));
}

function insert(tree: CmsMenu[], moved: CmsMenu, dropId: string, mode: DropMode): CmsMenu[] {
  const result: CmsMenu[] = [];

  for (const item of tree) {
    if (item.id === dropId) {
      if (mode === 'before') result.push(moved, item);
      else if (mode === 'after') result.push(item, moved);
      else result.push({ ...item, children: [moved, ...item.children] });
      continue;
    }

    result.push({ ...item, children: insert(item.children, moved, dropId, mode) });
  }

  return result;
}

/**
 * Cây sau khi kéo mục `dragId` thả vào `dropId` theo `mode`, hoặc `null` nếu thao tác vô nghĩa:
 * thả vào chính nó hay vào một mục con của nó thì cây thành vòng.
 */
export function moveMenu(tree: CmsMenu[], dragId: string, dropId: string, mode: DropMode): CmsMenu[] | null {
  if (dragId === dropId) return null;

  const dragged = findMenu(tree, dragId);
  if (!dragged || !findMenu(tree, dropId)) return null;
  if (contains(dragged, dropId)) return null;

  const without = remove(tree, dragId);
  const moved: CmsMenu = { ...dragged };

  return insert(without, moved, dropId, mode);
}

export interface MenuPosition {
  id: string;
  parentId?: string;
  sortOrder: number;
}

/** Vị trí phẳng của mọi mục theo cây hiện tại — đúng thân yêu cầu `PUT /api/content/menus/order`. */
export function toPositions(tree: CmsMenu[], parentId?: string): MenuPosition[] {
  return tree.flatMap((item, index) => [
    { id: item.id, parentId, sortOrder: (index + 1) * 10 },
    ...toPositions(item.children, item.id),
  ]);
}

/** Biểu tượng chọn được cho mục menu; tên trùng với tên trong @ant-design/icons để trang tra cứu vẽ. */
export const MENU_ICONS: { value: string; label: string }[] = [
  { value: 'HomeOutlined', label: 'Trang chủ' },
  { value: 'SearchOutlined', label: 'Tra cứu' },
  { value: 'ReadOutlined', label: 'Đọc' },
  { value: 'BookOutlined', label: 'Sách' },
  { value: 'FileTextOutlined', label: 'Tài liệu' },
  { value: 'NotificationOutlined', label: 'Thông báo' },
  { value: 'PictureOutlined', label: 'Ảnh' },
  { value: 'InfoCircleOutlined', label: 'Giới thiệu' },
  { value: 'QuestionCircleOutlined', label: 'Hỏi đáp' },
  { value: 'PhoneOutlined', label: 'Liên hệ' },
  { value: 'GlobalOutlined', label: 'Liên kết ngoài' },
  { value: 'TeamOutlined', label: 'Bạn đọc' },
  { value: 'CalendarOutlined', label: 'Sự kiện' },
  { value: 'StarOutlined', label: 'Nổi bật' },
  { value: 'DatabaseOutlined', label: 'Cơ sở dữ liệu' },
];
