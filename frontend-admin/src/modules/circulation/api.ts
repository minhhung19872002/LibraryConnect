import { api } from '@/api/client';
import type { PagedResult } from '@/types/api';
import type {
  CheckoutStationDto,
  CheckoutResultDto,
  CirculationPolicyDto,
  CirculationReportFilter,
  DeskReaderDto,
  DueDatePreviewDto,
  EffectivePolicyDto,
  FineRowDto,
  GateScanResultDto,
  HoldRowDto,
  HolidayDto,
  LoanDetailDto,
  LoanRowDto,
  LockerMapDto,
  LockerReportDto,
  LockerRowDto,
  LockerUsageRowDto,
  LoanHistoryReportDto,
  OverdueReportDto,
  PendingRenewalDto,
  ReaderFineSummaryDto,
  ReturnResultDto,
  ScanForLoanDto,
  TopItemRowDto,
  TopReaderRowDto,
  VisitReportDto,
  VisitRowDto,
} from './types';

/** Phân hệ VII — Lưu thông. */
export const circulationApi = {
  // --- Chính sách và lịch nghỉ --------------------------------------------
  policies: (includeInactive = false) =>
    api.get<CirculationPolicyDto[]>('/circulation/policies', { params: { includeInactive } }),

  savePolicy: (payload: Record<string, unknown>) =>
    api.post<string>('/circulation/policies', payload),

  deletePolicy: (id: string) => api.delete<null>(`/circulation/policies/${id}`),

  previewPolicy: (params: Record<string, unknown>) =>
    api.get<EffectivePolicyDto>('/circulation/policies/preview', { params }),

  holidays: (year?: number) => api.get<HolidayDto[]>('/circulation/holidays', { params: { year } }),

  saveHoliday: (payload: Record<string, unknown>) =>
    api.post<string>('/circulation/holidays', payload),

  deleteHoliday: (id: string) => api.delete<null>(`/circulation/holidays/${id}`),

  previewDueDate: (loanDate: string, loanDays: number) =>
    api.get<DueDatePreviewDto>('/circulation/holidays/preview-due-date', {
      params: { loanDate, loanDays },
    }),

  // --- Quầy ---------------------------------------------------------------
  deskReader: (cardNumber: string) =>
    api.get<DeskReaderDto>('/circulation/desk/reader', { params: { cardNumber } }),

  deskReaderById: (readerId: string) =>
    api.get<DeskReaderDto>(`/circulation/desk/reader/${readerId}`),

  scan: (readerId: string, barcode: string, pending: string[]) =>
    api.post<ScanForLoanDto>('/circulation/desk/scan', { readerId, barcode, pending }),

  checkout: (payload: Record<string, unknown>) =>
    api.post<CheckoutResultDto>('/circulation/desk/checkout', payload),

  returnItems: (payload: Record<string, unknown>) =>
    api.post<ReturnResultDto>('/circulation/desk/return', payload),

  renew: (loanId: string) => api.post<LoanRowDto>(`/circulation/loans/${loanId}/renew`, {}),

  renewByBarcode: (barcode: string) =>
    api.post<LoanRowDto>('/circulation/desk/renew-by-barcode', { barcode }),

  closeAsLost: (loanId: string, payload: Record<string, unknown>) =>
    api.post<FineRowDto>(`/circulation/loans/${loanId}/close-as-lost`, payload),

  loans: (params: Record<string, unknown>) =>
    api.get<PagedResult<LoanRowDto>>('/circulation/loans', { params }),

  loan: (id: string) => api.get<LoanDetailDto>(`/circulation/loans/${id}`),

  pendingRenewals: () => api.get<PendingRenewalDto[]>('/circulation/renewals/pending'),

  processRenewal: (id: string, payload: Record<string, unknown>) =>
    api.post<LoanRowDto>(`/circulation/renewals/${id}/process`, payload),

  // --- Đặt giữ ------------------------------------------------------------
  holds: (params: Record<string, unknown>) =>
    api.get<PagedResult<HoldRowDto>>('/circulation/holds', { params }),

  placeHold: (payload: Record<string, unknown>) =>
    api.post<HoldRowDto>('/circulation/holds', payload),

  cancelHold: (id: string, reason?: string) =>
    api.delete<null>(`/circulation/holds/${id}`, { params: { reason } }),

  holdQueue: (bibId: string) => api.get<HoldRowDto[]>(`/circulation/holds/queue/${bibId}`),

  // --- Tiền phạt ----------------------------------------------------------
  fines: (params: Record<string, unknown>) =>
    api.get<PagedResult<FineRowDto>>('/circulation/fines', { params }),

  readerFines: (readerId: string) =>
    api.get<ReaderFineSummaryDto>(`/circulation/fines/reader/${readerId}`),

  createFine: (payload: Record<string, unknown>) =>
    api.post<FineRowDto>('/circulation/fines', payload),

  payFine: (id: string, payload: Record<string, unknown>) =>
    api.post<FineRowDto>(`/circulation/fines/${id}/pay`, payload),

  waiveFine: (id: string, reason: string) =>
    api.post<FineRowDto>(`/circulation/fines/${id}/waive`, { reason }),

  // --- Ra vào thư viện ----------------------------------------------------
  scanGate: (payload: Record<string, unknown>) =>
    api.post<GateScanResultDto>('/circulation/gate/scan', payload),

  visits: (params: Record<string, unknown>) =>
    api.get<PagedResult<VisitRowDto>>('/circulation/visits', { params }),

  closeOpenVisits: (date?: string) =>
    api.post<number>('/circulation/visits/close-open', {}, { params: { date } }),

  // --- Tủ gửi đồ ----------------------------------------------------------
  stations: (includeInactive = true) =>
    api.get<CheckoutStationDto[]>('/circulation/stations', { params: { includeInactive } }),
  saveStation: (payload: {
    id?: string;
    code: string;
    name: string;
    warehouseId?: string | null;
    location?: string;
    isActive: boolean;
  }) => api.post<CheckoutStationDto>('/circulation/stations', payload),
  deleteStation: (id: string) => api.delete<null>(`/circulation/stations/${id}`),

  lockerMap: (params: Record<string, unknown>) =>
    api.get<LockerMapDto>('/circulation/lockers', { params }),

  saveLocker: (payload: Record<string, unknown>) =>
    api.post<string>('/circulation/lockers', payload),

  deleteLocker: (id: string) => api.delete<null>(`/circulation/lockers/${id}`),

  assignLocker: (id: string, payload: Record<string, unknown>) =>
    api.post<LockerRowDto>(`/circulation/lockers/${id}/assign`, payload),

  releaseLocker: (payload: Record<string, unknown>) =>
    api.post<LockerUsageRowDto>('/circulation/lockers/release', payload),

  lockerUsages: (params: Record<string, unknown>) =>
    api.get<PagedResult<LockerUsageRowDto>>('/circulation/lockers/usages', { params }),

  // --- Báo cáo ------------------------------------------------------------
  visitReport: (filter: CirculationReportFilter) =>
    api.get<VisitReportDto>('/circulation/reports/visits', { params: filter }),

  currentLoansReport: (filter: CirculationReportFilter) =>
    api.get<LoanRowDto[]>('/circulation/reports/current-loans', { params: filter }),

  historyReport: (filter: CirculationReportFilter) =>
    api.get<LoanHistoryReportDto>('/circulation/reports/history', { params: filter }),

  overdueReport: (filter: CirculationReportFilter) =>
    api.get<OverdueReportDto>('/circulation/reports/overdue', { params: filter }),

  sendOverdueReminders: (filter: CirculationReportFilter, loanIds: string[]) =>
    api.post<number>('/circulation/reports/overdue/remind', { filter, loanIds }),

  lockerReport: (filter: CirculationReportFilter) =>
    api.get<LockerReportDto>('/circulation/reports/lockers', { params: filter }),

  topReaders: (filter: CirculationReportFilter) =>
    api.get<TopReaderRowDto[]>('/circulation/reports/top-readers', { params: filter }),

  topItems: (filter: CirculationReportFilter) =>
    api.get<TopItemRowDto[]>('/circulation/reports/top-items', { params: filter }),

  exportReport: (payload: Record<string, unknown>) =>
    api.downloadPost('/circulation/reports/export', payload, 'bao-cao-luu-thong'),

  /** In phiếu mượn, phiếu trả, biên lai phạt và giấy xác nhận qua trình in biểu mẫu dùng chung. */
  printForm: (formType: string, documentId: string) =>
    api.download(`/acquisition/forms/print/${formType}/${encodeURIComponent(documentId)}`),
};
