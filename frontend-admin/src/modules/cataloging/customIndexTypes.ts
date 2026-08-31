/** Danh mục tự tạo từ trường MARC 21 (II.9). */
export interface CustomIndex {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  marcTag: string;
  marcSubfield: string;
  showAsFacet: boolean;
  isActive: boolean;
  sortOrder: number;
  lastHarvestAt?: string | null;
  valueCount: number;
  /** Tên trường MARC nguồn, lấy từ bộ định nghĩa. */
  sourceFieldName?: string | null;
}

export interface CustomIndexValue {
  id: string;
  code: string;
  name: string;
  recordCount: number;
  isActive: boolean;
}

export interface HarvestResult {
  distinctValues: number;
  newValues: number;
  recordsScanned: number;
  harvestedAt: string;
}
