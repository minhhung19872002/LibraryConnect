import { useEffect } from 'react';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { Button, Dropdown, Layout } from 'antd';
import { LogoutOutlined, UserOutlined } from '@ant-design/icons';
import { FALLBACK_LIBRARY_NAME, useSiteMenus, useSiteSettings } from '@/hooks/useSite';
import { useAuthStore } from '@/stores/authStore';
import { useCartStore } from '@/stores/cartStore';
import { activeKey, toMenuItem } from '@/components/menuTree';
import type { MenuItem } from '@/types/api';

const { Header, Content, Footer } = Layout;

/**
 * Khung chung của trang tra cứu, theo bản thiết kế "LibraryConnect Layout".
 *
 * Đầu trang là một hàng: huy hiệu và tên thư viện, các đường dẫn do cán bộ cấu hình, rồi nút đăng
 * nhập bạn đọc. Chân trang nền xanh rêu đậm ba cột: giới thiệu, giờ mở cửa, liên hệ.
 *
 * Tên, logo, địa chỉ đều lấy từ cấu hình — sản phẩm không nhớ tên khách hàng nào. Hai chữ đầu
 * của tên sản phẩm chỉ hiện trên huy hiệu khi thư viện chưa tải logo lên.
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

  const items = (menus ?? []).filter((menu) => menu.isActive);
  const current = activeKey(location.pathname, items.map(toMenuItem));

  const open = (menu: MenuItem) => {
    if (!menu.url) return;

    if (menu.url.startsWith('http')) {
      window.open(menu.url, '_blank', 'noopener,noreferrer');
      return;
    }

    navigate(menu.url);
  };

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header className="lc-header">
        <div className="lc-container lc-header__row">
          <Link to="/" className="lc-header__brand">
            {settings?.logoUrl ? (
              <img className="lc-header__logo" src={settings.logoUrl} alt={libraryName} />
            ) : (
              <span className="lc-header__badge" aria-hidden="true">
                {libraryName.trim().charAt(0).toUpperCase() || 'T'}
              </span>
            )}
            <span className="lc-header__name">{libraryName}</span>
          </Link>

          <nav className="lc-header__nav" aria-label="Điều hướng chính">
            {items.map((menu) => {
              const isActive =
                current === (menu.url ?? menu.id) ||
                menu.children.some((child) => current === (child.url ?? child.id));
              const className = ['lc-header__link', isActive ? 'lc-header__link--active' : ''].join(' ');

              if (menu.children.length > 0) {
                return (
                  <Dropdown
                    key={menu.id}
                    menu={{
                      items: menu.children
                        .filter((child) => child.isActive)
                        .map((child) => ({ key: child.id, label: child.name })),
                      onClick: ({ key }) => {
                        const child = menu.children.find((entry) => entry.id === key);
                        if (child) open(child);
                      },
                    }}
                  >
                    <span className={className} onClick={() => open(menu)}>
                      {menu.name}
                    </span>
                  </Dropdown>
                );
              }

              return (
                <span key={menu.id} className={className} onClick={() => open(menu)}>
                  {menu.name}
                </span>
              );
            })}
          </nav>

          <div className="lc-header__actions">
            <Link to="/gio-tai-lieu" className="lc-header__cart">
              Giỏ{cartCount > 0 ? ` (${cartCount})` : ''}
            </Link>

            {user ? (
              <Dropdown
                menu={{
                  items: [
                    { key: 'account', label: `Tài khoản của tôi (${user.username})`, icon: <UserOutlined /> },
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
                <Button className="lc-header__login" icon={<UserOutlined />}>
                  {user.fullName}
                </Button>
              </Dropdown>
            ) : (
              <Link to="/dang-nhap">
                <Button className="lc-header__login">Đăng nhập bạn đọc</Button>
              </Link>
            )}
          </div>
        </div>
      </Header>

      <Content style={{ display: 'flex', flexDirection: 'column' }}>
        <Outlet />
      </Content>

      <Footer className="lc-footer" style={{ background: 'var(--lc-green-dark)' }}>
        <div className="lc-container">
          <div className="lc-footer__grid">
            <div>
              <div className="lc-footer__name">{libraryName}</div>
              {settings?.contactNote ? <div>{settings.contactNote}</div> : null}
              {settings?.address ? <div>{settings.address}</div> : null}
              <div>Kết nối liên thư viện qua Z39.50 / SRU / OAI-PMH.</div>
            </div>

            <div>
              <div className="lc-footer__title">Giờ mở cửa</div>
              {settings?.openingHours ? (
                settings.openingHours.split('\n').map((line) => <div key={line}>{line}</div>)
              ) : (
                <div>Xem thông báo tại quầy phục vụ.</div>
              )}
            </div>

            <div>
              <div className="lc-footer__title">Liên hệ</div>
              {settings?.email ? (
                <div>
                  <a href={`mailto:${settings.email}`}>{settings.email}</a>
                </div>
              ) : null}
              {settings?.phone ? <div>{settings.phone}</div> : null}
              {settings?.facebook ? (
                <div>
                  <a href={settings.facebook} target="_blank" rel="noopener noreferrer">
                    Facebook
                  </a>
                </div>
              ) : null}
              {settings?.youtube ? (
                <div>
                  <a href={settings.youtube} target="_blank" rel="noopener noreferrer">
                    YouTube
                  </a>
                </div>
              ) : null}
              {settings?.zalo ? (
                <div>
                  <a href={settings.zalo} target="_blank" rel="noopener noreferrer">
                    Zalo
                  </a>
                </div>
              ) : null}
              <div>
                <Link to="/thu-vien-khac">Tìm ở thư viện khác</Link>
              </div>
            </div>
          </div>

          <div className="lc-footer__bottom">
            <span>{settings?.footerText}</span>
            {settings?.showPoweredBy ? (
              <span>Vận hành bởi LibraryConnect · Tra cứu không dấu theo TCVN 6909:2001</span>
            ) : null}
          </div>
        </div>
      </Footer>
    </Layout>
  );
}
