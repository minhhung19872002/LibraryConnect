import { useEffect } from 'react';
import { Navigate, Route, Routes, useNavigate } from 'react-router-dom';
import { App as AntdApp, Button, ConfigProvider, Result } from 'antd';
import viVN from 'antd/locale/vi_VN';
import dayjs from 'dayjs';
import 'dayjs/locale/vi';
import { setSessionExpiredHandler } from '@/api/client';
import { useAuthStore } from '@/stores/authStore';
import { MainLayout } from '@/layouts/MainLayout';
import { LoginPage } from '@/modules/auth/LoginPage';
import { ChangePasswordPage } from '@/modules/auth/ChangePasswordPage';
import { DashboardPage } from '@/modules/dashboard/DashboardPage';
import { AuditLogsPage } from '@/modules/system/AuditLogsPage';
import { BackupsPage } from '@/modules/system/BackupsPage';
import { ParametersPage } from '@/modules/system/ParametersPage';
import { UserGroupsPage } from '@/modules/system/UserGroupsPage';
import { UsersPage } from '@/modules/system/UsersPage';
import { CatalogIndexPage } from '@/modules/catalogs/CatalogIndexPage';
import { CatalogPage } from '@/modules/catalogs/CatalogPage';
import { RequirePermissionRoute } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { messages } from '@/i18n/messages';
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

  // The HTTP client cannot import the router; it signals an unrecoverable 401 through this hook.
  useEffect(() => {
    setSessionExpiredHandler(() => {
      useAuthStore.setState({ user: null });
      navigate('/dang-nhap', { replace: true });
    });
  }, [navigate]);

  return (
    <Routes>
      <Route path="/dang-nhap" element={<LoginPage />} />

      <Route element={<MainLayout />}>
        <Route index element={<DashboardPage />} />
        <Route path="/doi-mat-khau" element={<ChangePasswordPage />} />

        {/* Phân hệ I — Quản trị hệ thống. Each screen is also guarded server-side by the same codes. */}
        <Route
          path="/he-thong/nhom-nguoi-dung"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.system.groupView}>
              <UserGroupsPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/he-thong/nguoi-dung"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.system.userView}>
              <UsersPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/he-thong/tham-so"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.system.parameterView}>
              <ParametersPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/he-thong/nhat-ky"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.system.auditView}>
              <AuditLogsPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/he-thong/sao-luu"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.system.backupView}>
              <BackupsPage />
            </RequirePermissionRoute>
          }
        />

        {/* Danh mục — một màn hình dùng chung cho mọi bảng danh mục nghiệp vụ. */}
        <Route
          path="/danh-muc"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.catalogList.view}>
              <CatalogIndexPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/danh-muc/:catalog"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.catalogList.view}>
              <CatalogPage />
            </RequirePermissionRoute>
          }
        />
      </Route>

      <Route path="/khong-du-quyen" element={<ForbiddenPage />} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}

function ForbiddenPage() {
  const navigate = useNavigate();
  return (
    <Result
      status="403"
      title={messages.errors.forbiddenPageTitle}
      subTitle={messages.errors.forbiddenPageContent}
      extra={
        <Button type="primary" onClick={() => navigate('/')}>
          {messages.errors.backToHome}
        </Button>
      }
    />
  );
}

function NotFoundPage() {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const initialising = useAuthStore((state) => state.initialising);

  if (!initialising && !user) {
    return <Navigate to="/dang-nhap" replace />;
  }

  return (
    <Result
      status="404"
      title="404"
      subTitle={messages.errors.pageNotFound}
      extra={
        <Button type="primary" onClick={() => navigate('/')}>
          {messages.errors.backToHome}
        </Button>
      }
    />
  );
}
