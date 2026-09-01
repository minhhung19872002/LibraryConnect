import { api } from '@/api/client';
import type { PagedResult } from '@/types/api';
import type {
  AcquisitionListRowDto,
  AcquisitionPivotDto,
  AcquisitionReportFilter,
  AcquisitionStatReportDto,
  BarcodeTemplateDto,
  BulkItemResultDto,
  CreateItemsFromOrderResultDto,
  DisposalReportRowDto,
  FormTemplateDto,
  FormTypeMetadataDto,
  HandoverDto,
  ImportInventoryScansResultDto,
  ImportPurchaseLinesResultDto,
  InventoryPeriodDto,
  InventoryResultRowDto,
  InventoryResultType,
  InventoryScanResultDto,
  InventorySummaryDto,
  LabelTemplateDto,
  LibraryDetailDto,
  LibraryDto,
  PurchaseApprovalReportDto,
  PurchaseDuplicateDto,
  PurchaseOrderDetailDto,
  PurchaseOrderDto,
  PurchaseRequestDetailDto,
  PurchaseRequestDto,
  QuickCatalogResultDto,
  ShelfDto,
  ShelfMapDto,
  StockItemDetailDto,
  StockItemDto,
  StockItemFilter,
  StockOverviewDto,
  StockSummaryDto,
  SupplierHistoryDto,
  TransferSlipDto,
  WarehouseDetailDto,
  WarehouseDto,
} from './types';

/** III.3 — Thư viện, kho và giá. */
export const locationsApi = {
  libraries: (includeInactive = false) =>
    api.get<LibraryDto[]>('/locations/libraries', { params: { includeInactive } }),

  library: (id: string) => api.get<LibraryDetailDto>(`/locations/libraries/${id}`),

  saveLibrary: (id: string | null, payload: Record<string, unknown>) =>
    id
      ? api.put<string>(`/locations/libraries/${id}`, payload)
      : api.post<string>('/locations/libraries', payload),

  deleteLibrary: (id: string) => api.delete<null>(`/locations/libraries/${id}`),

  warehouses: (libraryId?: string | null, includeInactive = false) =>
    api.get<WarehouseDto[]>('/locations/warehouses', { params: { libraryId, includeInactive } }),

  warehouse: (id: string) => api.get<WarehouseDetailDto>(`/locations/warehouses/${id}`),

  saveWarehouse: (id: string | null, payload: Record<string, unknown>) =>
    id
      ? api.put<string>(`/locations/warehouses/${id}`, payload)
      : api.post<string>('/locations/warehouses', payload),

  deleteWarehouse: (id: string) => api.delete<null>(`/locations/warehouses/${id}`),

  shelves: (warehouseId?: string | null, includeInactive = false) =>
    api.get<ShelfDto[]>('/locations/shelves', { params: { warehouseId, includeInactive } }),

  saveShelf: (id: string | null, payload: Record<string, unknown>) =>
    id
      ? api.put<string>(`/locations/shelves/${id}`, payload)
      : api.post<string>('/locations/shelves', payload),

  deleteShelf: (id: string) => api.delete<null>(`/locations/shelves/${id}`),

  shelfMap: (warehouseId: string) => api.get<ShelfMapDto>(`/locations/warehouses/${warehouseId}/map`),
};

/** III.2 và III.5 — Ấn phẩm trong kho. */
export const stockApi = {
  search: (request: { page: number; pageSize: number; sortBy?: string; sortDescending?: boolean; filter: StockItemFilter }) =>
    api.post<PagedResult<StockItemDto>>('/stock/items/search', request),

  summary: (filter: StockItemFilter) => api.post<StockSummaryDto>('/stock/items/summary', filter),

  item: (id: string) => api.get<StockItemDetailDto>(`/stock/items/${id}`),

  shelve: (payload: Record<string, unknown>) =>
    api.post<BulkItemResultDto>('/stock/items/shelve', payload),

  inspect: (payload: Record<string, unknown>) =>
    api.post<BulkItemResultDto>('/stock/items/inspect', payload),

  setLock: (payload: Record<string, unknown>) =>
    api.post<BulkItemResultDto>('/stock/items/lock', payload),

  transfer: (payload: Record<string, unknown>) =>
    api.post<BulkItemResultDto>('/stock/items/transfer', payload),

  dispose: (payload: Record<string, unknown>) =>
    api.post<BulkItemResultDto>('/stock/items/dispose', payload),

  exportItems: (filter: StockItemFilter, ids?: string[]) =>
    api.downloadPost('/stock/items/export', { filter, ids }, 'dkcb.xlsx'),

  transfers: (params: { from?: string | null; to?: string | null; warehouseId?: string | null }) =>
    api.get<TransferSlipDto[]>('/stock/transfers', { params }),

  transferSlip: (batchCode: string) => api.get<TransferSlipDto>(`/stock/transfers/${batchCode}`),

  barcodeTemplates: (includeInactive = false) =>
    api.get<BarcodeTemplateDto[]>('/stock/barcode-templates', { params: { includeInactive } }),

  saveBarcodeTemplate: (id: string | null, payload: Record<string, unknown>) =>
    id
      ? api.put<string>(`/stock/barcode-templates/${id}`, payload)
      : api.post<string>('/stock/barcode-templates', payload),

  deleteBarcodeTemplate: (id: string) => api.delete<null>(`/stock/barcode-templates/${id}`),

  labelTemplates: (includeInactive = false) =>
    api.get<LabelTemplateDto[]>('/stock/label-templates', { params: { includeInactive } }),

  saveLabelTemplate: (id: string | null, payload: Record<string, unknown>) =>
    id
      ? api.put<string>(`/stock/label-templates/${id}`, payload)
      : api.post<string>('/stock/label-templates', payload),

  deleteLabelTemplate: (id: string) => api.delete<null>(`/stock/label-templates/${id}`),

  printBarcodes: (payload: Record<string, unknown>) =>
    api.downloadPost('/stock/print/barcodes', payload, 'tem-ma-vach.pdf'),

  printLabels: (payload: Record<string, unknown>) =>
    api.downloadPost('/stock/print/labels', payload, 'nhan-gay.pdf'),

};

/** III.1 — Yêu cầu đặt mua, đơn đặt, biên bản bàn giao, biên mục sơ lược. */
export const purchaseApi = {
  requests: (params: Record<string, unknown>) =>
    api.get<PagedResult<PurchaseRequestDto>>('/acquisition/requests', { params }),

  request: (id: string) => api.get<PurchaseRequestDetailDto>(`/acquisition/requests/${id}`),

  saveRequest: (id: string | null, payload: Record<string, unknown>) =>
    id
      ? api.put<string>(`/acquisition/requests/${id}`, payload)
      : api.post<string>('/acquisition/requests', payload),

  deleteRequest: (id: string) => api.delete<null>(`/acquisition/requests/${id}`),

  submitRequest: (id: string) => api.post<null>(`/acquisition/requests/${id}/submit`),

  approveRequest: (id: string, payload: Record<string, unknown>) =>
    api.post<string>(`/acquisition/requests/${id}/approve`, payload),

  rejectRequest: (id: string, reason: string) =>
    api.post<null>(`/acquisition/requests/${id}/reject`, { reason }),

  checkDuplicate: (isbn?: string, title?: string) =>
    api.get<PurchaseDuplicateDto | null>('/acquisition/requests/duplicate-check', {
      params: { isbn, title },
    }),

  requestTemplate: () => api.download('/acquisition/requests/excel-template'),

  importRequestLines: (file: File, requestId?: string) => {
    const form = new FormData();
    form.append('file', file);
    if (requestId) form.append('requestId', requestId);

    return api.post<ImportPurchaseLinesResultDto>('/acquisition/requests/import', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  orders: (params: Record<string, unknown>) =>
    api.get<PagedResult<PurchaseOrderDto>>('/acquisition/orders', { params }),

  order: (id: string) => api.get<PurchaseOrderDetailDto>(`/acquisition/orders/${id}`),

  createOrdersFromRequests: (payload: Record<string, unknown>) =>
    api.post<string[]>('/acquisition/orders/from-requests', payload),

  saveOrder: (id: string | null, payload: Record<string, unknown>) =>
    id
      ? api.put<string>(`/acquisition/orders/${id}`, payload)
      : api.post<string>('/acquisition/orders', payload),

  setOrderStatus: (id: string, status: string, reason?: string) =>
    api.post<null>(`/acquisition/orders/${id}/status`, { status, reason }),

  receiveOrder: (id: string, lines: { itemId: string; receivedQuantity: number }[], note?: string) =>
    api.post<string>(`/acquisition/orders/${id}/receive`, { lines, note }),

  createItemsFromOrder: (id: string, payload: Record<string, unknown>) =>
    api.post<CreateItemsFromOrderResultDto>(`/acquisition/orders/${id}/create-items`, payload),

  quickCatalog: (payload: Record<string, unknown>) =>
    api.post<QuickCatalogResultDto>('/acquisition/quick-catalog', payload),

  handovers: (params: Record<string, unknown>) =>
    api.get<PagedResult<HandoverDto>>('/acquisition/handovers', { params }),

  handover: (id: string) => api.get<HandoverDto>(`/acquisition/handovers/${id}`),

  saveHandover: (id: string | null, payload: Record<string, unknown>) =>
    id
      ? api.put<string>(`/acquisition/handovers/${id}`, payload)
      : api.post<string>('/acquisition/handovers', payload),

  deleteHandover: (id: string) => api.delete<null>(`/acquisition/handovers/${id}`),

  attachHandoverScan: (id: string, file: File) => {
    const form = new FormData();
    form.append('file', file);

    return api.post<string>(`/acquisition/handovers/${id}/scan`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  handoverScan: (id: string) => api.download(`/acquisition/handovers/${id}/scan`),
};

/** III.4 — Kiểm kê. */
export const inventoryApi = {
  setWarehouseClosed: (warehouseId: string, closed: boolean) =>
    api.post<null>(`/inventory/warehouses/${warehouseId}/closed`, { closed }),

  periods: (params: Record<string, unknown>) =>
    api.get<PagedResult<InventoryPeriodDto>>('/inventory/periods', { params }),

  period: (id: string) => api.get<InventoryPeriodDto>(`/inventory/periods/${id}`),

  createPeriod: (payload: Record<string, unknown>) =>
    api.post<string>('/inventory/periods', payload),

  scan: (id: string, barcode: string, device = 'Web') =>
    api.post<InventoryScanResultDto>(`/inventory/periods/${id}/scan`, { barcode, device }),

  importScans: (id: string, file: File) => {
    const form = new FormData();
    form.append('file', file);

    return api.post<ImportInventoryScansResultDto>(`/inventory/periods/${id}/scan-file`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  summary: (id: string) => api.get<InventorySummaryDto>(`/inventory/periods/${id}/summary`),

  results: (id: string, params: Record<string, unknown>) =>
    api.get<PagedResult<InventoryResultRowDto>>(`/inventory/periods/${id}/results`, { params }),

  exportResults: (id: string, result?: InventoryResultType | null) =>
    api.download(`/inventory/periods/${id}/results/export`, { params: { result } }),

  close: (id: string, payload: Record<string, unknown>) =>
    api.post<InventorySummaryDto>(`/inventory/periods/${id}/close`, payload),

  resolveMissing: (id: string, payload: Record<string, unknown>) =>
    api.post<BulkItemResultDto>(`/inventory/periods/${id}/resolve-missing`, payload),
};

/** III.6 — Mẫu biểu in. */
export const formsApi = {
  types: () => api.get<FormTypeMetadataDto[]>('/acquisition/forms/types'),

  templates: (formType?: string | null, includeInactive = false) =>
    api.get<FormTemplateDto[]>('/acquisition/forms', { params: { formType, includeInactive } }),

  save: (id: string | null, payload: Record<string, unknown>) =>
    id
      ? api.put<string>(`/acquisition/forms/${id}`, payload)
      : api.post<string>('/acquisition/forms', payload),

  remove: (id: string) => api.delete<null>(`/acquisition/forms/${id}`),

  print: (formType: string, documentId: string, templateId?: string) =>
    api.download(`/acquisition/forms/print/${formType}/${encodeURIComponent(documentId)}`, {
      params: { templateId },
    }),
};

/** III.2 và III.7 — Báo cáo bổ sung. */
export const acqReportsApi = {
  dimensions: () => api.get<Record<string, string>>('/acquisition/reports/dimensions'),

  statistics: (dimension: string, grouping: string, filter: AcquisitionReportFilter) =>
    api.post<AcquisitionStatReportDto>(
      `/acquisition/reports/statistics?dimension=${dimension}&grouping=${grouping}`,
      filter,
    ),

  pivot: (
    rowDimension: string,
    columnDimension: string,
    measure: string,
    grouping: string,
    filter: AcquisitionReportFilter,
  ) =>
    api.post<AcquisitionPivotDto>(
      `/acquisition/reports/pivot?rowDimension=${rowDimension}&columnDimension=${columnDimension}` +
        `&measure=${measure}&grouping=${grouping}`,
      filter,
    ),

  overview: (filter: AcquisitionReportFilter) =>
    api.post<StockOverviewDto>('/acquisition/reports/overview', filter),

  acquisitionList: (filter: AcquisitionReportFilter) =>
    api.post<AcquisitionListRowDto[]>('/acquisition/reports/acquisition-list', filter),

  disposals: (filter: AcquisitionReportFilter) =>
    api.post<DisposalReportRowDto[]>('/acquisition/reports/disposals', filter),

  purchaseApproval: (from?: string | null, to?: string | null) =>
    api.get<PurchaseApprovalReportDto>('/acquisition/reports/purchase-approval', {
      params: { from, to },
    }),

  supplierHistory: (id: string, from?: string | null, to?: string | null) =>
    api.get<SupplierHistoryDto>(`/acquisition/reports/suppliers/${id}`, { params: { from, to } }),

  export: (
    kind: string,
    format: string,
    filter: AcquisitionReportFilter,
    options: { dimension?: string; columnDimension?: string; measure?: string; grouping?: string } = {},
  ) => {
    const query = new URLSearchParams({
      kind,
      format,
      measure: options.measure ?? 'Items',
      grouping: options.grouping ?? 'Month',
    });

    if (options.dimension) query.set('dimension', options.dimension);
    if (options.columnDimension) query.set('columnDimension', options.columnDimension);

    return api.downloadPost(
      `/acquisition/reports/export?${query.toString()}`,
      filter,
      'bao-cao-bo-sung',
    );
  },
};
