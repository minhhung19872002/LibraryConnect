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
import { MarcFieldsPage } from '@/modules/marc/MarcFieldsPage';
import { MarcToolPage } from '@/modules/marc/MarcToolPage';
import { BibListPage } from '@/modules/cataloging/BibListPage';
import { BibEditorPage } from '@/modules/cataloging/BibEditorPage';
import { BibDetailPage } from '@/modules/cataloging/BibDetailPage';
import { BibImportPage } from '@/modules/cataloging/BibImportPage';
import { BibExcelImportPage } from '@/modules/cataloging/BibExcelImportPage';
import { PurchaseRequestPage } from '@/modules/acquisition/PurchaseRequestPage';
import { PurchaseOrderPage } from '@/modules/acquisition/PurchaseOrderPage';
import { StockItemsPage } from '@/modules/acquisition/StockItemsPage';
import { WarehousePage } from '@/modules/acquisition/WarehousePage';
import { InventoryPage } from '@/modules/acquisition/InventoryPage';
import { LabelTemplatePage } from '@/modules/acquisition/LabelTemplatePage';
import { FormTemplatePage } from '@/modules/acquisition/FormTemplatePage';
import { AcquisitionReportsPage } from '@/modules/acquisition/AcquisitionReportsPage';
import { SerialsPage } from '@/modules/serials/SerialsPage';
import { ReadersPage } from '@/modules/readers/ReadersPage';
import { ReaderCardTemplatePage } from '@/modules/readers/ReaderCardTemplatePage';
import { ReaderImportPage } from '@/modules/readers/ReaderImportPage';
import { ReaderReportsPage } from '@/modules/readers/ReaderReportsPage';
import { CirculationDeskPage } from '@/modules/circulation/CirculationDeskPage';
import { CirculationPolicyPage } from '@/modules/circulation/CirculationPolicyPage';
import { HoldsPage } from '@/modules/circulation/HoldsPage';
import { FinesPage } from '@/modules/circulation/FinesPage';
import { LockersAndGatePage } from '@/modules/circulation/LockersAndGatePage';
import { CirculationReportsPage } from '@/modules/circulation/CirculationReportsPage';
import { DigitalDocumentsPage } from '@/modules/digital/DigitalDocumentsPage';
import { DigitalRequestsPage } from '@/modules/digital/DigitalRequestsPage';
import { DigitalImportExportPage } from '@/modules/digital/DigitalImportExportPage';
import { DigitalReportsPage } from '@/modules/digital/DigitalReportsPage';
import { RemoteSearchPage } from '@/modules/interlibrary/RemoteSearchPage';
import { Z3950TargetsPage } from '@/modules/interlibrary/Z3950TargetsPage';
import { OaiRepositoriesPage } from '@/modules/interlibrary/OaiRepositoriesPage';
import { SiteSettingsPage } from '@/modules/cms/SiteSettingsPage';
import { CmsPagesPage } from '@/modules/cms/PagesPage';
import { CmsNewsPage } from '@/modules/cms/NewsPage';
import { CmsGalleriesPage } from '@/modules/cms/GalleriesPage';
import { CmsReviewsPage } from '@/modules/cms/ReviewsPage';
import { CourseDocumentsPage } from '@/modules/courses/CourseDocumentsPage';
import { CourseReportsPage } from '@/modules/courses/CourseReportsPage';
import { SerialReportsPage } from '@/modules/serials/SerialReportsPage';
import { CustomIndexPage } from '@/modules/cataloging/CustomIndexPage';
import { CatalogQueuePage } from '@/modules/cataloging/CatalogQueuePage';
import { CardTemplatePage } from '@/modules/cataloging/CardTemplatePage';
import { CatalogingConfigPage } from '@/modules/cataloging/CatalogingConfigPage';
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

        {/* Khổ mẫu MARC 21 — bộ định nghĩa trường và công cụ đọc/kiểm tra/xuất biểu ghi. */}
        <Route
          path="/marc/dinh-nghia-truong"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.cataloging.marcDefinitionView}>
              <MarcFieldsPage />
            </RequirePermissionRoute>
          }
        />
        {/* Phân hệ II — Biên mục. */}
        <Route
          path="/bien-muc"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.cataloging.bibView}>
              <BibListPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/bien-muc/cau-hinh"
          element={
            <RequirePermissionRoute
              permission={[PERMISSIONS.cataloging.defaultValue, PERMISSIONS.cataloging.template]}
            >
              <CatalogingConfigPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/bien-muc/phich"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.cataloging.cardPrint}>
              <CardTemplatePage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/bien-muc/hang-doi"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.cataloging.queueView}>
              <CatalogQueuePage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/bien-muc/danh-muc-tu-tao"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.catalogList.customIndex}>
              <CustomIndexPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/an-pham-dinh-ky/dau-bao"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.serial.view}>
              <SerialsPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/an-pham-dinh-ky/bao-cao"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.serial.reportView}>
              <SerialReportsPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/ban-doc/ho-so"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.reader.view}>
              <ReadersPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/ban-doc/mau-the"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.reader.printCard}>
              <ReaderCardTemplatePage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/ban-doc/nhap-xuat"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.reader.import}>
              <ReaderImportPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/ban-doc/bao-cao"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.reader.reportView}>
              <ReaderReportsPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/luu-thong/quay"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.circulation.loanCreate}>
              <CirculationDeskPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/luu-thong/dat-giu"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.circulation.holdManage}>
              <HoldsPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/luu-thong/tien-phat"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.circulation.fineView}>
              <FinesPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/luu-thong/cong-va-tu"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.circulation.lockerManage}>
              <LockersAndGatePage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/luu-thong/chinh-sach"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.circulation.policyView}>
              <CirculationPolicyPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/luu-thong/bao-cao"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.circulation.reportView}>
              <CirculationReportsPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/tai-lieu-so/kho"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.digital.view}>
              <DigitalDocumentsPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/tai-lieu-so/yeu-cau"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.digital.requestView}>
              <DigitalRequestsPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/tai-lieu-so/nhap-xuat"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.digital.import}>
              <DigitalImportExportPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/tai-lieu-so/bao-cao"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.digital.reportView}>
              <DigitalReportsPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/lien-thu-vien/tra-cuu"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.interlibrary.search}>
              <RemoteSearchPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/lien-thu-vien/may-chu"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.interlibrary.targetManage}>
              <Z3950TargetsPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/lien-thu-vien/oai"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.interlibrary.oaiManage}>
              <OaiRepositoriesPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/bo-sung/yeu-cau"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.acquisition.requestView}>
              <PurchaseRequestPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/bo-sung/don-dat"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.acquisition.orderView}>
              <PurchaseOrderPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/bo-sung/an-pham"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.acquisition.itemView}>
              <StockItemsPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/bo-sung/kho"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.acquisition.warehouseView}>
              <WarehousePage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/bo-sung/kiem-ke"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.acquisition.inventoryView}>
              <InventoryPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/bo-sung/mau-tem"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.acquisition.itemPrintBarcode}>
              <LabelTemplatePage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/bo-sung/mau-bieu"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.acquisition.formTemplate}>
              <FormTemplatePage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/bo-sung/bao-cao"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.acquisition.reportView}>
              <AcquisitionReportsPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/bien-muc/nhap-excel"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.cataloging.bibImport}>
              <BibExcelImportPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/bien-muc/nhap-tep"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.cataloging.bibImport}>
              <BibImportPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/bien-muc/moi"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.cataloging.bibCreate}>
              <BibEditorPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/bien-muc/:id"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.cataloging.bibView}>
              <BibDetailPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/bien-muc/:id/sua"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.cataloging.bibUpdate}>
              <BibEditorPage />
            </RequirePermissionRoute>
          }
        />

        <Route
          path="/marc/cong-cu"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.cataloging.bibView}>
              <MarcToolPage />
            </RequirePermissionRoute>
          }
        />

        {/* Phân hệ VIII — Quản trị nội dung. */}
        <Route
          path="/noi-dung/thong-tin"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.cms.settingManage}>
              <SiteSettingsPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/noi-dung/trang"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.cms.pageManage}>
              <CmsPagesPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/noi-dung/tin-tuc"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.cms.newsView}>
              <CmsNewsPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/noi-dung/thu-vien-anh"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.cms.galleryManage}>
              <CmsGalleriesPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/noi-dung/nhan-xet"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.cms.reviewModerate}>
              <CmsReviewsPage />
            </RequirePermissionRoute>
          }
        />

        {/* Phân hệ X — Tài liệu môn học. */}
        <Route
          path="/tai-lieu-mon-hoc/gan-tai-lieu"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.course.documentLink}>
              <CourseDocumentsPage />
            </RequirePermissionRoute>
          }
        />
        <Route
          path="/tai-lieu-mon-hoc/bao-cao"
          element={
            <RequirePermissionRoute permission={PERMISSIONS.course.reportView}>
              <CourseReportsPage />
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
