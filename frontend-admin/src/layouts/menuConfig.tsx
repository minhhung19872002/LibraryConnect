import type { ReactNode } from 'react';
import {
  AppstoreOutlined,
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
        key: 'cataloging-cards',
        label: messages.menu.cards,
        icon: <FileTextOutlined />,
        path: '/bien-muc/phich',
        permissions: [PERMISSIONS.cataloging.cardPrint],
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
    path: '/bo-sung',
    permissions: [PERMISSIONS.acquisition.requestView, PERMISSIONS.acquisition.itemView],
    comingSoon: true,
  },
  {
    key: 'serials',
    label: messages.menu.serials,
    icon: <ContainerOutlined />,
    path: '/an-pham-dinh-ky',
    permissions: [PERMISSIONS.serial.view],
    comingSoon: true,
  },
  {
    key: 'digital',
    label: messages.menu.digital,
    icon: <FileTextOutlined />,
    path: '/tai-lieu-so',
    permissions: [PERMISSIONS.digital.view],
    comingSoon: true,
  },
  {
    key: 'readers',
    label: messages.menu.readers,
    icon: <IdcardOutlined />,
    path: '/ban-doc',
    permissions: [PERMISSIONS.reader.view],
    comingSoon: true,
  },
  {
    key: 'circulation',
    label: messages.menu.circulation,
    icon: <SwapOutlined />,
    path: '/luu-thong',
    permissions: [PERMISSIONS.circulation.loanCreate, PERMISSIONS.circulation.loanView],
    comingSoon: true,
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
    path: '/lien-thu-vien',
    permissions: [PERMISSIONS.cataloging.z3950Search, PERMISSIONS.cataloging.oaiManage],
    comingSoon: true,
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
