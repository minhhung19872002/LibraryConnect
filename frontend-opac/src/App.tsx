import { useEffect } from 'react';
import { Link, Navigate, Route, Routes, useNavigate } from 'react-router-dom';
import { App as AntdApp, ConfigProvider, Result } from 'antd';
import viVN from 'antd/locale/vi_VN';
import dayjs from 'dayjs';
import 'dayjs/locale/vi';
import { setSessionExpiredHandler } from '@/api/client';
import { useAuthStore } from '@/stores/authStore';
import { SiteLayout } from '@/components/SiteLayout';
import { HomePage } from '@/pages/HomePage';
import { SearchPage } from '@/pages/SearchPage';
import { AdvancedSearchPage } from '@/pages/AdvancedSearchPage';
import { BibDetailPage } from '@/pages/BibDetailPage';
import { BrowsePage, MajorCoursesPage } from '@/pages/BrowsePage';
import { SerialsPage, ThesesPage } from '@/pages/CatalogPages';
import { NewsDetailPage, NewsListPage, StaticPageView } from '@/pages/ContentPages';
import { GalleryPage } from '@/pages/GalleryPage';
import { InterlibraryPage } from '@/pages/InterlibraryPage';
import { DigitalPage, DigitalViewerPage } from '@/pages/DigitalPage';
import { LoginPage } from '@/pages/LoginPage';
import { AccountPage } from '@/pages/AccountPage';
import { CartPage } from '@/pages/CartPage';
import { theme } from '@/theme';

dayjs.locale('vi');

export function App() {
  const restore = useAuthStore((state) => state.restore);

  useEffect(() => {
    void restore();
  }, [restore]);

  return (
    <ConfigProvider locale={viVN} theme={theme}>
      <AntdApp>
        <AppRoutes />
      </AntdApp>
    </ConfigProvider>
  );
}

function AppRoutes() {
  const navigate = useNavigate();

  useEffect(() => {
    setSessionExpiredHandler(() => {
      useAuthStore.setState({ user: null });
      navigate('/dang-nhap', { replace: true });
    });
  }, [navigate]);

  return (
    <Routes>
      <Route element={<SiteLayout />}>
        <Route index element={<HomePage />} />

        <Route path="/tra-cuu" element={<SearchPage />} />
        <Route path="/tra-cuu-nang-cao" element={<AdvancedSearchPage />} />
        <Route path="/tai-lieu/:id" element={<BibDetailPage />} />

        <Route path="/duyet" element={<Navigate to="/duyet/chu-de" replace />} />
        <Route path="/duyet/nganh/:majorId" element={<MajorCoursesPage />} />
        <Route path="/duyet/:kind" element={<BrowsePage />} />

        <Route path="/luan-van" element={<ThesesPage />} />
        <Route path="/an-pham-dinh-ky" element={<SerialsPage />} />
        <Route path="/tai-lieu-so" element={<DigitalPage />} />
        <Route path="/tai-lieu-so/:id" element={<DigitalViewerPage />} />
        <Route path="/thu-vien-khac" element={<InterlibraryPage />} />

        <Route path="/tin-tuc" element={<NewsListPage />} />
        <Route path="/tin-tuc/:slug" element={<NewsDetailPage />} />
        <Route path="/thu-vien-anh" element={<GalleryPage />} />
        <Route path="/trang/:slug" element={<StaticPageView />} />

        <Route path="/dang-nhap" element={<LoginPage />} />
        <Route path="/tai-khoan" element={<AccountPage />} />
        <Route path="/gio-tai-lieu" element={<CartPage />} />

        <Route
          path="*"
          element={
            <Result
              status="404"
              title="Không tìm thấy trang"
              subTitle="Đường dẫn không tồn tại hoặc đã được thay đổi."
              extra={<Link to="/">Về trang chủ</Link>}
            />
          }
        />
      </Route>
    </Routes>
  );
}
