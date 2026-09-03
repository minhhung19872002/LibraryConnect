/** Shapes returned by the `/api/admin/*` endpoints of subsystem I. */

export interface UserGroupListItem {
  id: string;
  code: string;
  name: string;
  description?: string;
  isSystem: boolean;
  isActive: boolean;
  memberCount: number;
  permissionCount: number;
  createdAt: string;
}

export interface PermissionTreeNode {
  key: string;
  title: string;
  /** Present only on leaves; equals the permission code. */
  code?: string;
  children: PermissionTreeNode[];
}

export interface GroupPermissions {
  groupId: string;
  groupName: string;
  isSystem: boolean;
  tree: PermissionTreeNode[];
  grantedCodes: string[];
}

export interface GroupMember {
  userId: string;
  username: string;
  fullName: string;
  email?: string;
  department?: string;
  isActive: boolean;
}

export interface UserListItem {
  id: string;
  username: string;
  fullName: string;
  email?: string;
  phone?: string;
  position?: string;
  department?: string;
  isActive: boolean;
  mustChangePassword: boolean;
  lockedUntil?: string;
  lastLoginAt?: string;
  groupNames: string[];
  createdAt: string;
}

export type DataScopeType = 'Library' | 'Warehouse' | 'DocumentType';

export interface UserDataScope {
  scopeType: DataScopeType;
  scopeId: string;
  scopeName?: string;
}

export interface UserDetail extends UserListItem {
  avatarUrl?: string;
  groupIds: string[];
  dataScopes: UserDataScope[];
  passwordChangedAt?: string;
  failedLoginCount: number;
}

export interface LoginHistoryItem {
  id: string;
  username: string;
  success: boolean;
  failureReason?: string;
  ip?: string;
  userAgent?: string;
  occurredAt: string;
}

export interface ImportRowError {
  row: number;
  column?: string;
  value?: string;
  message: string;
}

export interface UserImportResult {
  totalRows: number;
  successRows: number;
  errorRows: number;
  errors: ImportRowError[];
  generatedPasswords: Record<string, string>;
}

export type ParameterDataType =
  | 'Text'
  | 'Number'
  | 'Boolean'
  | 'Date'
  | 'Json'
  | 'File'
  | 'Password'
  | 'Cron';

export interface SystemParameter {
  id: string;
  key: string;
  name: string;
  description?: string;
  dataType: ParameterDataType;
  value?: string;
  defaultValue?: string;
  options?: string;
  isEditable: boolean;
  isSecret: boolean;
  hasValue: boolean;
  sortOrder: number;
}

export interface ParameterGroup {
  groupCode: string;
  groupName: string;
  parameters: SystemParameter[];
}

export interface ParameterHistoryItem {
  id: string;
  key: string;
  parameterName?: string;
  oldValue?: string;
  newValue?: string;
  changedByName?: string;
  changedAt: string;
}

export type AuditAction =
  | 'Create'
  | 'Update'
  | 'Delete'
  | 'Read'
  | 'Login'
  | 'LoginFailed'
  | 'Logout'
  | 'Export'
  | 'Import'
  | 'Approve'
  | 'Restore'
  | 'Backup'
  | 'PermissionChange'
  | 'ParameterChange';

export interface AuditLogItem {
  id: string;
  username?: string;
  action: AuditAction;
  actionLabel: string;
  entity: string;
  entityLabel: string;
  entityId?: string;
  entityDisplay?: string;
  result: boolean;
  message?: string;
  ip?: string;
  occurredAt: string;
}

export interface AuditLogDetail extends AuditLogItem {
  userAgent?: string;
  requestPath?: string;
  oldValue?: string;
  newValue?: string;
}

export interface AuditFilterOptions {
  entities: string[];
  actions: { value: string; label: string }[];
  usernames: string[];
}

export interface AuditSetting {
  id: string;
  entity: string;
  displayName: string;
  logCreate: boolean;
  logUpdate: boolean;
  logDelete: boolean;
  logRead: boolean;
  /** Null means keep forever. */
  retentionDays: number | null;
}

export type BackupType = 'Full' | 'DataOnly' | 'Incremental';
export type BackupStatus = 'Pending' | 'Running' | 'Success' | 'Failed' | 'Restored';

export interface BackupJob {
  id: string;
  type: BackupType;
  typeLabel: string;
  status: BackupStatus;
  statusLabel: string;
  fileName?: string;
  sizeBytes: number;
  checksum?: string;
  includesObjectStorage: boolean;
  startedAt: string;
  finishedAt?: string;
  message?: string;
  isAuto: boolean;
  triggeredByName?: string;
  fileAvailable: boolean;
}

export interface BackupStorage {
  totalBytes: number;
  freeBytes: number;
  usedByBackupsBytes: number;
  backupCount: number;
  autoEnabled: boolean;
  scheduleCron?: string;
  keepCount: number;
  lastSuccessAt?: string;
}

export type RestoreState = 'Running' | 'Succeeded' | 'Failed';

/**
 * Tiến độ lượt phục hồi gần nhất.
 *
 * Máy chủ giữ ở bộ nhớ đệm chứ không trong cơ sở dữ liệu: chính cơ sở dữ liệu đang bị ghi đè.
 */
export interface RestoreStatus {
  state: RestoreState;
  archiveName: string;
  message: string | null;
  startedAt: string;
  finishedAt: string | null;
  startedByName: string | null;
}
