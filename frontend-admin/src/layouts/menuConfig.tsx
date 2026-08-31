import type { ReactNode } from 'react';
import {
  AppstoreOutlined,
  BarcodeOutlined,
  AuditOutlined,
  BankOutlined,
  BarChartOutlined,
  BookOutlined,
  CloudServerOutlined,
  ContainerOutlined,
  DashboardOutlined,
  DatabaseOutlined,
  FileTextOutlined,
  GlobalOutlined,
  ImportOutlined,
  IdcardOutlined,
  ProfileOutlined,
  ReadOutlined,
  SettingOutlined,
  ShoppingCartOutlined,
  SwapOutlined,
  TeamOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { PERMISSIONS } from '@/api/permissions';
import { messages } from '@/i18n/messages';

export interface MenuNode {
  key: string;
  label: string;
  icon?: ReactNode;
  /** Route to navigate to. Absent on grouping nodes. */
  path?: string;
  /** The node is shown when the user holds at least one of these. Empty means always visible. */
  permissions?: readonly string[];
  children?: MenuNode[];
  /** Marks screens that belong to a phase not yet delivered, so the menu stays honest. */
  comingSoon?: boolean;
}

/**
 * The admin navigation tree, ordered exactly like the eleven subsystems of the specification so a
 * librarian reading the tender document finds each function where they expect it.
 *
 * Visibility is permission-driven; the backend independently enforces the same codes.
 */
export const menuTree: MenuNode[] = [
  {
    key: 'dashboard',
    label: messages.menu.dashboard,
    icon: <DashboardOutlined />,
    path: '/',
  },
  {
    key: 'system',
    label: messages.menu.system,
    icon: <SettingOutlined />,
    permissions: [
      PERMISSIONS.system.groupView,
      PERMISSIONS.system.userView,
      PERMISSIONS.system.parameterView,
      PERMISSIONS.system.auditView,
      PERMISSIONS.system.backupView,
    ],
    children: [
      {
        key: 'system-groups',
        label: messages.menu.userGroups,
        icon: <TeamOutlined />,
        path: '/he-thong/nhom-nguoi-dung',
        permissions: [PERMISSIONS.system.groupView],
      },
      {
        key: 'system-users',
        label: messages.menu.users,
        icon: <UserOutlined />,
        path: '/he-thong/nguoi-dung',
        permissions: [PERMISSIONS.system.userView],
      },
      {
        key: 'system-parameters',
        label: messages.menu.parameters,
        icon: <SettingOutlined />,
        path: '/he-thong/tham-so',
        permissions: [PERMISSIONS.system.parameterView],
      },
      {
        key: 'system-audit',
        label: messages.menu.auditLogs,
        icon: <AuditOutlined />,
        path: '/he-thong/nhat-ky',
        permissions: [PERMISSIONS.system.auditView],
      },
      {
        key: 'system-backup',
        label: messages.menu.backups,
        icon: <CloudServerOutlined />,
        path: '/he-thong/sao-luu',
        permissions: [PERMISSIONS.system.backupView],
      },
    ],
  },
  {
    key: 'catalogs',
    label: messages.menu.catalogs,
    icon: <DatabaseOutlined />,
    path: '/danh-muc',
    permissions: [PERMISSIONS.catalogList.view],
  },
  {
    key: 'marc',
    label: messages.menu.marc,
    icon: <ProfileOutlined />,
    permissions: [PERMISSIONS.cataloging.marcDefinitionView, PERMISSIONS.cataloging.bibView],
    children: [
      {
        key: 'marc-fields',
        label: messages.menu.marcFields,
        icon: <ProfileOutlined />,
        path: '/marc/dinh-nghia-truong',
        permissions: [PERMISSIONS.cataloging.marcDefinitionView],
      },
      {
        key: 'marc-tool',
        label: messages.menu.marcTool,
        icon: <SwapOutlined />,
        path: '/marc/cong-cu',
        permissions: [PERMISSIONS.cataloging.bibView],
      },
    ],
  },
  {
    key: 'cataloging',
    label: messages.menu.cataloging,
    icon: <BookOutlined />,
    permissions: [
      PERMISSIONS.cataloging.bibView,
      PERMISSIONS.cataloging.bibImport,
      PERMISSIONS.catalogList.customIndex,
      PERMISSIONS.cataloging.queueView,
      PERMISSIONS.cataloging.cardPrint,
      PERMISSIONS.cataloging.defaultValue,
    ],
    children: [
      {
        key: 'cataloging-bibs',
        label: messages.menu.bibRecords,
        icon: <BookOutlined />,
        path: '/bien-muc',
        permissions: [PERMISSIONS.cataloging.bibView],
      },
      {
        key: 'cataloging-queue',
        label: messages.menu.catalogQueue,
        icon: <ContainerOutlined />,
        path: '/bien-muc/hang-doi',
        permissions: [PERMISSIONS.cataloging.queueView],
      },
      {
        key: 'cataloging-import',
        label: messages.menu.bibImport,
        icon: <ImportOutlined />,
        path: '/bien-muc/nhap-tep',
        permissions: [PERMISSIONS.cataloging.bibImport],
      },
      {
        key: 'cataloging-excel',
        label: messages.menu.bibExcelImport,
        icon: <ImportOutlined />,
        path: '/bien-muc/nhap-excel',
        permissions: [PERMISSIONS.cataloging.bibImport],
      },
      {
        key: 'cataloging-cards',
        label: messages.menu.cards,
        icon: <FileTextOutlined />,
        path: '/bien-muc/phich',
        permissions: [PERMISSIONS.cataloging.cardPrint],
      },
      {
        key: 'cataloging-config',
        label: messages.menu.catalogingConfig,
        icon: <SettingOutlined />,
        path: '/bien-muc/cau-hinh',
        permissions: [PERMISSIONS.cataloging.defaultValue, PERMISSIONS.cataloging.template],
      },
      {
        key: 'cataloging-custom-index',
        label: messages.menu.customIndexes,
        icon: <DatabaseOutlined />,
        path: '/bien-muc/danh-muc-tu-tao',
        permissions: [PERMISSIONS.catalogList.customIndex],
      },
    ],
  },
  {
    key: 'acquisition',
    label: messages.menu.acquisition,
    icon: <ShoppingCartOutlined />,
    permissions: [
      PERMISSIONS.acquisition.requestView,
      PERMISSIONS.acquisition.orderView,
      PERMISSIONS.acquisition.itemView,
      PERMISSIONS.acquisition.warehouseView,
      PERMISSIONS.acquisition.inventoryView,
      PERMISSIONS.acquisition.reportView,
    ],
    children: [
      {
        key: 'acq-requests',
        label: messages.menu.purchaseRequests,
        icon: <ProfileOutlined />,
        path: '/bo-sung/yeu-cau',
        permissions: [PERMISSIONS.acquisition.requestView],
      },
      {
        key: 'acq-orders',
        label: messages.menu.purchaseOrders,
        icon: <ShoppingCartOutlined />,
        path: '/bo-sung/don-dat',
        permissions: [PERMISSIONS.acquisition.orderView],
      },
      {
        key: 'acq-items',
        label: messages.menu.stockItems,
        icon: <DatabaseOutlined />,
        path: '/bo-sung/an-pham',
        permissions: [PERMISSIONS.acquisition.itemView],
      },
      {
        key: 'acq-warehouses',
        label: messages.menu.warehouses,
        icon: <BankOutlined />,
        path: '/bo-sung/kho',
        permissions: [PERMISSIONS.acquisition.warehouseView],
      },
      {
        key: 'acq-inventory',
        label: messages.menu.inventory,
        icon: <AppstoreOutlined />,
        path: '/bo-sung/kiem-ke',
        permissions: [PERMISSIONS.acquisition.inventoryView],
      },
      {
        key: 'acq-labels',
        label: messages.menu.labelTemplates,
        icon: <BarcodeOutlined />,
        path: '/bo-sung/mau-tem',
        permissions: [PERMISSIONS.acquisition.itemPrintBarcode],
      },
      {
        key: 'acq-forms',
        label: messages.menu.formTemplates,
        icon: <FileTextOutlined />,
        path: '/bo-sung/mau-bieu',
        permissions: [PERMISSIONS.acquisition.formTemplate],
      },
      {
        key: 'acq-reports',
        label: messages.menu.acquisitionReports,
        icon: <BarChartOutlined />,
        path: '/bo-sung/bao-cao',
        permissions: [PERMISSIONS.acquisition.reportView],
      },
    ],
  },
  {
    key: 'serials',
    label: messages.menu.serials,
    icon: <ContainerOutlined />,
    permissions: [PERMISSIONS.serial.view, PERMISSIONS.serial.reportView],
    children: [
      {
        key: 'serial-titles',
        label: messages.menu.serialTitles,
        icon: <ContainerOutlined />,
        path: '/an-pham-dinh-ky/dau-bao',
        permissions: [PERMISSIONS.serial.view],
      },
      {
        key: 'serial-reports',
        label: messages.menu.serialReports,
        icon: <BarChartOutlined />,
        path: '/an-pham-dinh-ky/bao-cao',
        permissions: [PERMISSIONS.serial.reportView],
      },
    ],
  },
  {
    key: 'digital',
    label: messages.menu.digital,
    icon: <FileTextOutlined />,
    permissions: [
      PERMISSIONS.digital.view,
      PERMISSIONS.digital.requestView,
      PERMISSIONS.digital.reportView,
    ],
    children: [
      {
        key: 'digital-documents',
        label: messages.menu.digitalDocuments,
        icon: <FileTextOutlined />,
        path: '/tai-lieu-so/kho',
        permissions: [PERMISSIONS.digital.view],
      },
      {
        key: 'digital-requests',
        label: messages.menu.digitalRequests,
        icon: <ContainerOutlined />,
        path: '/tai-lieu-so/yeu-cau',
        permissions: [PERMISSIONS.digital.requestView],
      },
      {
        key: 'digital-import',
        label: messages.menu.digitalImport,
        icon: <ContainerOutlined />,
        path: '/tai-lieu-so/nhap-xuat',
        permissions: [PERMISSIONS.digital.import, PERMISSIONS.digital.export],
      },
      {
        key: 'digital-reports',
        label: messages.menu.digitalReports,
        icon: <BarChartOutlined />,
        path: '/tai-lieu-so/bao-cao',
        permissions: [PERMISSIONS.digital.reportView],
      },
    ],
  },
  {
    key: 'readers',
    label: messages.menu.readers,
    icon: <IdcardOutlined />,
    permissions: [
      PERMISSIONS.reader.view,
      PERMISSIONS.reader.printCard,
      PERMISSIONS.reader.import,
      PERMISSIONS.reader.reportView,
    ],
    children: [
      {
        key: 'reader-list',
        label: messages.menu.readerProfiles,
        icon: <IdcardOutlined />,
        path: '/ban-doc/ho-so',
        permissions: [PERMISSIONS.reader.view],
      },
      {
        key: 'reader-card-templates',
        label: messages.menu.readerCardTemplates,
        icon: <IdcardOutlined />,
        path: '/ban-doc/mau-the',
        permissions: [PERMISSIONS.reader.printCard],
      },
      {
        key: 'reader-import',
        label: messages.menu.readerImport,
        icon: <ImportOutlined />,
        path: '/ban-doc/nhap-xuat',
        permissions: [PERMISSIONS.reader.import],
      },
      {
        key: 'reader-reports',
        label: messages.menu.readerReports,
        icon: <BarChartOutlined />,
        path: '/ban-doc/bao-cao',
        permissions: [PERMISSIONS.reader.reportView],
      },
    ],
  },
  {
    key: 'circulation',
    label: messages.menu.circulation,
    icon: <SwapOutlined />,
    permissions: [
      PERMISSIONS.circulation.loanCreate,
      PERMISSIONS.circulation.loanView,
      PERMISSIONS.circulation.policyView,
      PERMISSIONS.circulation.reportView,
    ],
    children: [
      {
        key: 'circulation-desk',
        label: messages.menu.circulationDesk,
        icon: <SwapOutlined />,
        path: '/luu-thong/quay',
        permissions: [PERMISSIONS.circulation.loanCreate, PERMISSIONS.circulation.loanReturn],
      },
      {
        key: 'circulation-holds',
        label: messages.menu.circulationHolds,
        icon: <ContainerOutlined />,
        path: '/luu-thong/dat-giu',
        permissions: [PERMISSIONS.circulation.holdManage],
      },
      {
        key: 'circulation-fines',
        label: messages.menu.circulationFines,
        icon: <ContainerOutlined />,
        path: '/luu-thong/tien-phat',
        permissions: [PERMISSIONS.circulation.fineView],
      },
      {
        key: 'circulation-lockers',
        label: messages.menu.circulationLockers,
        icon: <ContainerOutlined />,
        path: '/luu-thong/cong-va-tu',
        permissions: [PERMISSIONS.circulation.lockerManage, PERMISSIONS.circulation.visitManage],
      },
      {
        key: 'circulation-policies',
        label: messages.menu.circulationPolicies,
        icon: <SettingOutlined />,
        path: '/luu-thong/chinh-sach',
        permissions: [PERMISSIONS.circulation.policyView],
      },
      {
        key: 'circulation-reports',
        label: messages.menu.circulationReports,
        icon: <BarChartOutlined />,
        path: '/luu-thong/bao-cao',
        permissions: [PERMISSIONS.circulation.reportView],
      },
    ],
  },
  {
    key: 'cms',
    label: messages.menu.cms,
    icon: <GlobalOutlined />,
    path: '/noi-dung',
    permissions: [PERMISSIONS.cms.newsView, PERMISSIONS.cms.pageManage],
    comingSoon: true,
  },
  {
    key: 'courses',
    label: messages.menu.courses,
    icon: <ReadOutlined />,
    path: '/tai-lieu-mon-hoc',
    permissions: [PERMISSIONS.course.courseManage, PERMISSIONS.course.documentLink],
    comingSoon: true,
  },
  {
    key: 'interlibrary',
    label: messages.menu.interlibrary,
    icon: <BankOutlined />,
    permissions: [
      PERMISSIONS.interlibrary.search,
      PERMISSIONS.interlibrary.targetManage,
      PERMISSIONS.interlibrary.oaiManage,
    ],
    children: [
      {
        key: 'ill-search',
        label: messages.menu.interlibrarySearch,
        icon: <BankOutlined />,
        path: '/lien-thu-vien/tra-cuu',
        permissions: [PERMISSIONS.interlibrary.search],
      },
      {
        key: 'ill-targets',
        label: messages.menu.interlibraryTargets,
        icon: <SettingOutlined />,
        path: '/lien-thu-vien/may-chu',
        permissions: [PERMISSIONS.interlibrary.targetManage],
      },
      {
        key: 'ill-oai',
        label: messages.menu.interlibraryOai,
        icon: <ContainerOutlined />,
        path: '/lien-thu-vien/oai',
        permissions: [PERMISSIONS.interlibrary.oaiManage],
      },
    ],
  },
  {
    key: 'reports',
    label: messages.menu.reports,
    icon: <BarChartOutlined />,
    path: '/bao-cao',
    permissions: [
      PERMISSIONS.acquisition.reportView,
      PERMISSIONS.circulation.reportView,
      PERMISSIONS.reader.reportView,
      PERMISSIONS.digital.reportView,
    ],
    comingSoon: true,
  },
];

export const fallbackMenuIcon = <AppstoreOutlined />;

/**
 * Removes the branches the signed-in user cannot reach. A grouping node disappears once none of its
 * children survive, so the sidebar never shows an empty folder.
 *
 * This only shapes the navigation — the backend re-checks the same codes on every request, so a
 * user who guesses a URL still gets HTTP 403.
 */
export function filterMenuByPermission(
  nodes: MenuNode[],
  hasAnyPermission: (codes: readonly string[]) => boolean,
): MenuNode[] {
  return nodes
    .map<MenuNode | null>((node) => {
      const children = node.children ? filterMenuByPermission(node.children, hasAnyPermission) : undefined;

      if (node.children && (!children || children.length === 0)) {
        return null;
      }

      if (node.permissions && !hasAnyPermission(node.permissions)) {
        return null;
      }

      return { ...node, children };
    })
    .filter((node): node is MenuNode => node !== null);
}

/** Depth-first lookup used to resolve the breadcrumb and the selected menu key from a URL. */
export function findMenuByPath(path: string, nodes: MenuNode[] = menuTree): MenuNode[] {
  for (const node of nodes) {
    if (node.path === path) {
      return [node];
    }

    if (node.children) {
      const trail = findMenuByPath(path, node.children);
      if (trail.length > 0) {
        return [node, ...trail];
      }
    }
  }

  return [];
}
