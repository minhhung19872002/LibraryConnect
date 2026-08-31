import { useMemo, useState } from 'react';
import { Link, Navigate, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { Avatar, Breadcrumb, Dropdown, Layout, Menu, Spin, Tag, Typography } from 'antd';
import type { MenuProps } from 'antd';
import { KeyOutlined, LogoutOutlined, MenuFoldOutlined, MenuUnfoldOutlined, UserOutlined } from '@ant-design/icons';
import { useAuthStore } from '@/stores/authStore';
import { messages } from '@/i18n/messages';
import { filterMenuByPermission, findMenuByPath, menuTree, type MenuNode } from './menuConfig';
import { useLibraryName } from '@/hooks/useLibraryName';

const { Header, Sider, Content, Footer } = Layout;

/**
 * Shell of the admin application: permission-filtered sidebar, breadcrumb, account menu.
 *
 * Branding follows section 0.1 — the LibraryConnect mark stays small in the corner while the
 * customer's library name is the headline, and both come from configuration rather than the code.
 */
export function MainLayout() {
  const [collapsed, setCollapsed] = useState(false);
  const location = useLocation();
  const navigate = useNavigate();

  const user = useAuthStore((state) => state.user);
  const initialising = useAuthStore((state) => state.initialising);
  const mustChangePassword = useAuthStore((state) => state.mustChangePassword);
  const hasAnyPermission = useAuthStore((state) => state.hasAnyPermission);
  const logout = useAuthStore((state) => state.logout);

  const libraryName = useLibraryName();

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

  return (
    <Layout className="lc-layout">
      <Sider collapsible collapsed={collapsed} trigger={null} width={248} theme="light" className="lc-sider">
        <div className="lc-brand">
          <span className="lc-brand-mark" aria-hidden="true">
            LC
          </span>
          {!collapsed && <span className="lc-brand-name">{messages.app.productName}</span>}
        </div>
        <Menu
          mode="inline"
          items={antdItems}
          selectedKeys={selectedKeys}
          defaultOpenKeys={openKeys}
          onClick={({ key }) => {
            const target = findByKey(visibleMenu, key);
            if (target?.path) {
              navigate(target.path);
            }
          }}
        />
      </Sider>

      <Layout>
        <Header className="lc-header">
          <button
            type="button"
            className="lc-collapse-button"
            onClick={() => setCollapsed((value) => !value)}
            aria-label={collapsed ? 'Mở rộng menu' : 'Thu gọn menu'}
          >
            {collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
          </button>

          <Typography.Title level={4} className="lc-library-name">
            {libraryName}
          </Typography.Title>

          <Dropdown menu={{ items: accountMenu }} placement="bottomRight" trigger={['click']}>
            <button type="button" className="lc-account-button">
              <Avatar size="small" icon={<UserOutlined />} src={user.avatarUrl} />
              <span className="lc-account-name">{user.fullName}</span>
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
          {libraryName} · <Tag color="blue">{messages.app.poweredBy}</Tag>
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
