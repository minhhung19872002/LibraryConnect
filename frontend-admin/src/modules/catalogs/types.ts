/** Shapes returned by the shared `/api/catalogs/*` endpoints. */

export type CatalogFieldType = 'Text' | 'LongText' | 'Number' | 'Decimal' | 'Boolean' | 'Select';

export interface CatalogFieldOption {
  value: string;
  label: string;
}

export interface CatalogField {
  key: string;
  label: string;
  type: CatalogFieldType;
  description?: string;
  required: boolean;
  showInList: boolean;
  options: CatalogFieldOption[];
}

export interface CatalogMetadata {
  code: string;
  singularName: string;
  pluralName: string;
  description?: string;
  isHierarchical: boolean;
  showCode: boolean;
  showNameEn: boolean;
  supportsMerge: boolean;
  fields: CatalogField[];
}

export interface CatalogItem {
  id: string;
  code: string;
  name: string;
  nameEn?: string;
  description?: string;
  sortOrder: number;
  isActive: boolean;
  parentId?: string;
  parentName?: string;
  level: number;
  extras: Record<string, string | null>;
  usageCount?: number;
}

export interface CatalogTreeNode {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
  children: CatalogTreeNode[];
}

export interface CatalogItemInput {
  code?: string;
  name: string;
  nameEn?: string;
  description?: string;
  sortOrder: number;
  isActive: boolean;
  parentId?: string | null;
  extras: Record<string, string | null>;
}

export interface DuplicateGroup {
  normalisedName: string;
  items: CatalogItem[];
}

export interface CatalogMergeResult {
  targetName: string;
  mergedCount: number;
  updatedReferences: number;
  mergedNames: string[];
}

export interface CatalogImportError {
  row: number;
  column?: string;
  value?: string;
  message: string;
}

export interface CatalogImportResult {
  totalRows: number;
  createdRows: number;
  updatedRows: number;
  errorRows: number;
  errors: CatalogImportError[];
}
