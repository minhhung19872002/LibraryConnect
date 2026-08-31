import { useEffect, useMemo } from 'react';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { Badge, Button, Dropdown, Layout, Menu, Space } from 'antd';
import type { MenuProps } from 'antd';
import {
  BookOutlined,
  LoginOutlined,
  LogoutOutlined,
  ShoppingCartOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { FALLBACK_LIBRARY_NAME, useSiteMenus, useSiteSettings } from '@/hooks/useSite';
import { useAuthStore } from '@/stores/authStore';
import { useCartStore } from '@/stores/cartStore';
import { activeKey, toMenuItem } from '@/components/menuTree';

const { Header, Content, Footer } = Layout;

/**
 * Khung chung của trang tra cứu: đầu trang có tên thư viện và thanh điều hướng do cán bộ tự cấu
 * hình, chân trang có thông tin liên hệ và giờ mở cửa.
 *
 * Tên, logo, địa chỉ đều lấy từ cấu hình — sản phẩm không nhớ tên khách hàng nào.
 */
export function SiteLayout() {
  const { data: settings } = useSiteSettings();
  const { data: menus } = useSiteMenus();
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const cartCount = useCartStore((state) => state.items.length);
  const location = useLocation();
  const navigate = useNavigate();

  const libraryName = settings?.libraryName ?? FALLBACK_LIBRARY_NAME;

  useEffect(() => {
    document.title = libraryName;
  }, [libraryName]);

  useEffect(() => {
    if (!settings?.faviconUrl) return;

    const link = document.querySelector<HTMLLinkElement>('link[rel="icon"]');
    if (link) {
      link.href = settings.faviconUrl;
    }
  }, [settings?.faviconUrl]);

  const items = useMemo(() => (menus ?? []).map(toMenuItem), [menus]);

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header className="lc-header">
        <div className="lc-container lc-header__top">
          <Link to="/" className="lc-header__brand">
            {settings?.logoUrl ? (
              <img className="lc-header__logo" src={settings.logoUrl} alt={libraryName} />
            ) : (
              <BookOutlined style={{ fontSize: 30, color: 'var(--lc-green)' }} />
            )}
            <span>
              <div className="lc-header__name">{libraryName}</div>
              {settings?.slogan ? (
                <div className="lc-header__slogan">{settings.slogan}</div>
              ) : null}
            </span>
          </Link>

          <span className="lc-header__spacer" />

          <Space size="small">
            <Link to="/gio-tai-lieu">
              <Badge count={cartCount} size="small">
                <Button icon={<ShoppingCartOutlined />}>Giỏ tài liệu</Button>
              </Badge>
            </Link>

            {user ? (
              <Dropdown
                menu={{
                  items: [
                    { key: 'account', label: 'Tài khoản của tôi', icon: <UserOutlined /> },
                    { key: 'logout', label: 'Đăng xuất', icon: <LogoutOutlined /> },
                  ],
                  onClick: ({ key }) => {
                    if (key === 'logout') {
                      logout();
                      navigate('/');
                      return;
                    }
                    navigate('/tai-khoan');
                  },
                }}
              >
                <Button type="primary" icon={<UserOutlined />}>
                  {user.fullName}
                </Button>
              </Dropdown>
            ) : (
              <Link to="/dang-nhap">
                <Button type="primary" icon={<LoginOutlined />}>
                  Đăng nhập
                </Button>
              </Link>
            )}
          </Space>
        </div>

        <div className="lc-header__nav">
          <div className="lc-container">
            <Menu
              mode="horizontal"
              selectedKeys={[activeKey(location.pathname, items)]}
              items={items as MenuProps['items']}
              onClick={({ key }) => {
                if (key.startsWith('http')) {
                  window.open(key, '_blank', 'noopener,noreferrer');
                  return;
                }
                navigate(key);
              }}
              style={{ borderBottom: 'none' }}
            />
          </div>
        </div>
      </Header>

      <Content>
        <Outlet />
      </Content>

      <Footer className="lc-footer" style={{ background: 'var(--lc-green-dark)' }}>
        <div className="lc-container">
          <div
            style={{
              display: 'grid',
              gap: 24,
              gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
            }}
          >
            <div>
              <div className="lc-footer__title">{libraryName}</div>
              {settings?.address ? <div>{settings.address}</div> : null}
              {settings?.phone ? <div>Điện thoại: {settings.phone}</div> : null}
              {settings?.email ? (
                <div>
                  Email: <a href={`mailto:${settings.email}`}>{settings.email}</a>
                </div>
              ) : null}
            </div>

            {settings?.openingHours ? (
              <div>
                <div className="lc-footer__title">Giờ mở cửa</div>
                {settings.openingHours.split('\n').map((line) => (
                  <div key={line}>{line}</div>
                ))}
              </div>
            ) : null}

            <div>
              <div className="lc-footer__title">Liên kết nhanh</div>
              <div>
                <Link to="/tra-cuu">Tra cứu tài liệu</Link>
              </div>
              <div>
                <Link to="/tra-cuu-nang-cao">Tra cứu nâng cao</Link>
              </div>
              <div>
                <Link to="/duyet/chu-de">Duyệt theo chủ đề</Link>
              </div>
              <div>
                <Link to="/tin-tuc">Tin tức</Link>
              </div>
            </div>

            {settings?.facebook || settings?.youtube || settings?.zalo ? (
              <div>
                <div className="lc-footer__title">Kết nối</div>
                {settings.facebook ? (
                  <div>
                    <a href={settings.facebook} target="_blank" rel="noopener noreferrer">
                      Facebook
                    </a>
                  </div>
                ) : null}
                {settings.youtube ? (
                  <div>
                    <a href={settings.youtube} target="_blank" rel="noopener noreferrer">
                      YouTube
                    </a>
                  </div>
                ) : null}
                {settings.zalo ? (
                  <div>
                    <a href={settings.zalo} target="_blank" rel="noopener noreferrer">
                      Zalo
                    </a>
                  </div>
                ) : null}
              </div>
            ) : null}
          </div>

          <div className="lc-footer__bottom">
            <span>{settings?.footerText}</span>
            {settings?.showPoweredBy ? <span>Powered by LibraryConnect</span> : null}
          </div>
        </div>
      </Footer>
    </Layout>
  );
}
