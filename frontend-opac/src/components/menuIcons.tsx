import type { ReactNode } from 'react';
import {
  BookOutlined,
  CalendarOutlined,
  DatabaseOutlined,
  FileTextOutlined,
  GlobalOutlined,
  HomeOutlined,
  InfoCircleOutlined,
  NotificationOutlined,
  PhoneOutlined,
  PictureOutlined,
  QuestionCircleOutlined,
  ReadOutlined,
  SearchOutlined,
  StarOutlined,
  TeamOutlined,
} from '@ant-design/icons';

/**
 * Biểu tượng của mục menu (VIII.1): cán bộ chọn tên trong giao diện quản trị, trang tra cứu vẽ.
 *
 * Danh sách cố định thay vì nạp động theo tên — nạp động là kéo cả bộ hơn 700 biểu tượng vào gói
 * tải về của bạn đọc chỉ để vẽ vài cái trên thanh điều hướng.
 */
const ICONS: Record<string, () => ReactNode> = {
  HomeOutlined: () => <HomeOutlined />,
  SearchOutlined: () => <SearchOutlined />,
  ReadOutlined: () => <ReadOutlined />,
  BookOutlined: () => <BookOutlined />,
  FileTextOutlined: () => <FileTextOutlined />,
  NotificationOutlined: () => <NotificationOutlined />,
  PictureOutlined: () => <PictureOutlined />,
  InfoCircleOutlined: () => <InfoCircleOutlined />,
  QuestionCircleOutlined: () => <QuestionCircleOutlined />,
  PhoneOutlined: () => <PhoneOutlined />,
  GlobalOutlined: () => <GlobalOutlined />,
  TeamOutlined: () => <TeamOutlined />,
  CalendarOutlined: () => <CalendarOutlined />,
  StarOutlined: () => <StarOutlined />,
  DatabaseOutlined: () => <DatabaseOutlined />,
};

/** Tên biểu tượng mà trang tra cứu vẽ được — dùng để kiểm cho khớp với danh sách bên quản trị. */
export const MENU_ICON_NAMES: readonly string[] = Object.keys(ICONS);

/** Biểu tượng cho một mục menu, hoặc null khi mục không đặt hay đặt tên lạ — không vẽ bừa. */
export function menuIcon(name?: string | null): ReactNode {
  if (!name) return null;

  const render = ICONS[name];
  return render ? <span className="lc-header__icon">{render()}</span> : null;
}
