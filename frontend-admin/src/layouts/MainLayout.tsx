import { useMemo, useState } from 'react';
import { Link, Navigate, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useTableScrollHint } from './useTableScrollHint';
import { Avatar, Breadcrumb, Drawer, Dropdown, Grid, Layout, Menu, Spin, Tag, Typography } from 'antd';
import type { MenuProps } from 'antd';
import { KeyOutlined, LogoutOutlined, MenuFoldOutlined, MenuUnfoldOutlined, UserOutlined } from '@ant-design/icons';
import { useAuthStore } from '@/stores/authStore';
import { messages } from '@/i18n/messages';
import { useDrawerMenu } from './layoutBreakpoints';
import { filterMenuByPermission, findMenuByPath, menuTree, type MenuNode } from './menuConfig';
import { useLibraryName } from '@/hooks/useLibraryName';
import { useServerStatus } from '@/hooks/useServerStatus';
import { NotificationBell } from '@/components/NotificationBell';

const { Header, Sider, Content, Footer } = Layout;
const { useBreakpoint } = Grid;

/**
 * Shell of the admin application: permission-filtered sidebar, breadcrumb, account menu.
 *
 * Branding follows section 0.1 — the LibraryConnect mark stays small in the corner while the
 * customer's library name is the headline, and both come from configuration rather than the code.
 */
export function MainLayout() {
  // Đánh dấu bảng còn cột nằm ngoài khung để giao diện vẽ dấu hiệu cuộn ngang — làm một lần ở đây
  // thay vì sửa 18 màn hình, và che luôn cả bảng thêm về sau.
  useTableScrollHint();

  const [collapsed, setCollapsed] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const location = useLocation();
  const navigate = useNavigate();

  // Dưới 992px thì không còn chỗ cho một cột menu cố định: menu chuyển thành ngăn kéo mở từ nút ở
  // thanh trên, đúng cách mọi ứng dụng quản trị khác làm trên điện thoại.
  const compact = useDrawerMenu(useBreakpoint());

  const user = useAuthStore((state) => state.user);
  const initialising = useAuthStore((state) => state.initialising);
  const mustChangePassword = useAuthStore((state) => state.mustChangePassword);
  const hasAnyPermission = useAuthStore((state) => state.hasAnyPermission);
  const logout = useAuthStore((state) => state.logout);

  const libraryName = useLibraryName();
  const serverUp = useServerStatus();

  // hasAnyPermission là một tham chiếu cố định của kho trạng thái: nó đọc quyền hiện tại qua get()
  // chứ không đóng gói quyền vào chính nó. Vì vậy user phải nằm trong danh sách phụ thuộc — bỏ nó
  // ra thì menu được dựng một lần lúc chưa đăng nhập rồi không bao giờ dựng lại, và cán bộ chỉ
  // thấy mỗi mục Tổng quan.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  const visibleMenu = useMemo(() => filterMenuByPermission(menuTree, hasAnyPermission), [hasAnyPermission, user]);

  const antdItems = useMemo<MenuProps['items']>(() => visibleMenu.map(toAntdItem), [visibleMenu]);

  const trail = findMenuByPath(location.pathname);
  const selectedKeys = trail.length > 0 ? [trail[trail.length - 1]!.key] : [];
  const openKeys = trail.slice(0, -1).map((node) => node.key);

  if (initialising) {
    return (
      <div className="lc-centered-page">
        <Spin size="large" tip={messages.table.loading} />
      </div>
    );
  }

  if (!user) {
    return <Navigate to="/dang-nhap" replace state={{ from: location.pathname }} />;
  }

  // A first sign-in must go through the password change before anything else is reachable.
  if (mustChangePassword && location.pathname !== '/doi-mat-khau') {
    return <Navigate to="/doi-mat-khau" replace />;
  }

  const accountMenu: MenuProps['items'] = [
    {
      key: 'change-password',
      icon: <KeyOutlined />,
      label: messages.auth.changePassword,
      onClick: () => navigate('/doi-mat-khau'),
    },
    { type: 'divider' },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: messages.auth.logout,
      danger: true,
      onClick: async () => {
        await logout();
        navigate('/dang-nhap', { replace: true });
      },
    },
  ];

  const menu = (
    <Menu
      className="lc-sider-menu"
      mode="inline"
      items={antdItems}
      selectedKeys={selectedKeys}
      defaultOpenKeys={openKeys}
      onClick={({ key }) => {
        const target = findByKey(visibleMenu, key);
        if (target?.path) {
          navigate(target.path);
          setDrawerOpen(false);
        }
      }}
    />
  );

  const brand = (showName: boolean) => (
    <div className="lc-brand">
      <span className="lc-brand-mark" aria-hidden="true">
        LC
      </span>
      {showName && (
        <span className="lc-brand-text">
          <span className="lc-brand-name">{messages.app.productName}</span>
          <span className="lc-brand-role">{messages.app.adminTitle}</span>
        </span>
      )}
    </div>
  );

  return (
    <Layout className="lc-layout">
      {compact ? (
        <Drawer
          open={drawerOpen}
          placement="left"
          width={280}
          onClose={() => setDrawerOpen(false)}
          className="lc-sider-drawer"
          title={brand(true)}
          styles={{ body: { padding: 0 } }}
        >
          {menu}
        </Drawer>
      ) : (
        <Sider collapsible collapsed={collapsed} trigger={null} width={264} theme="light" className="lc-sider">
          {brand(!collapsed)}
          {menu}
          {/* Phiên bản và nền tảng: hai thứ người quản trị phải tra ngay khi gọi báo sự cố. */}
          {!collapsed && <div className="lc-sider-footer">{messages.app.platformLine}</div>}
        </Sider>
      )}

      <Layout>
        <Header className="lc-header">
          <button
            type="button"
            className="lc-collapse-button"
            onClick={() => (compact ? setDrawerOpen(true) : setCollapsed((value) => !value))}
            aria-label={compact || collapsed ? 'Mở rộng menu' : 'Thu gọn menu'}
          >
            {compact || collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
          </button>

          <Typography.Title level={4} className="lc-library-name">
            {libraryName}
          </Typography.Title>

          {/*
            Trang đơn không tự biết máy chủ đã chết: khung trang vẫn hiện từ bộ nhớ trình duyệt,
            chỉ nút Lưu là im lặng. Huy hiệu này đọc từ chính những lượt gọi thật của màn hình
            đang mở — xem `useServerStatus`. Màn hình hẹp thì bỏ chữ, giữ lại chấm màu.
          */}
          <span
            className={serverUp ? 'lc-status-pill' : 'lc-status-pill lc-status-pill--mat'}
            title={serverUp ? messages.app.serverUp : messages.app.serverDown}
          >
            <span className="lc-status-dot" aria-hidden="true">
              ●
            </span>
            {!compact && <span>{serverUp ? messages.app.serverUp : messages.app.serverDown}</span>}
          </span>

          <NotificationBell />

          <Dropdown menu={{ items: accountMenu }} placement="bottomRight" trigger={['click']}>
            <button type="button" className="lc-account-button">
              <Avatar size="small" icon={<UserOutlined />} src={user.avatarUrl} />
              {/* Trên điện thoại, tên người dùng nhường chỗ cho tên thư viện. */}
              {!compact && <span className="lc-account-name">{user.fullName}</span>}
            </button>
          </Dropdown>
        </Header>

        <Content className="lc-content">
          {/* The dashboard is the root crumb, so it is never repeated when it is also the page. */}
          {trail.length > 0 && trail[0]?.key !== 'dashboard' && (
            <Breadcrumb
              className="lc-breadcrumb"
              items={[
                { title: <Link to="/">{messages.menu.dashboard}</Link> },
                ...trail.map((node) => ({ title: node.label })),
              ]}
            />
          )}
          <Outlet />
        </Content>

        <Footer className="lc-footer">
          {libraryName} · <Tag>{messages.app.poweredBy}</Tag>
        </Footer>
      </Layout>
    </Layout>
  );
}

function toAntdItem(node: MenuNode): NonNullable<MenuProps['items']>[number] {
  return {
    key: node.key,
    icon: node.icon,
    label: node.comingSoon ? `${node.label} …` : node.label,
    disabled: node.comingSoon,
    children: node.children?.map(toAntdItem),
  };
}

function findByKey(nodes: MenuNode[], key: string): MenuNode | undefined {
  for (const node of nodes) {
    if (node.key === key) return node;
    const found = node.children ? findByKey(node.children, key) : undefined;
    if (found) return found;
  }
  return undefined;
}
