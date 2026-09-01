import { PERMISSIONS } from '@/api/permissions';

/** Một báo cáo của phân hệ, để trang tổng quan dẫn cán bộ tới đúng chỗ. */
export interface ReportLink {
  key: string;
  title: string;
  description: string;
  path: string;
  permission: string;
}

export interface ReportGroup {
  key: string;
  title: string;
  links: ReportLink[];
}

/**
 * Mục lục toàn bộ báo cáo của hệ thống.
 *
 * Báo cáo nằm rải trong bảy phân hệ, mỗi phân hệ một chỗ; cán bộ mới về thư viện không biết tìm báo
 * cáo quá hạn ở đâu. Gom mục lục vào một trang thì họ mở một chỗ là thấy hết, còn màn hình báo cáo
 * vẫn nằm nguyên trong phân hệ của nó.
 */
export const REPORT_CATALOGUE: ReportGroup[] = [
  {
    key: 'acquisition',
    title: 'Bổ sung và kho',
    links: [
      {
        key: 'acq-stats',
        title: 'Báo cáo bổ sung',
        description:
          'Theo dạng tài liệu, vật mang tin, thời gian, ngôn ngữ và nguồn kinh phí; kèm ĐKCB đã hủy bỏ.',
        path: '/bo-sung/bao-cao',
        permission: PERMISSIONS.acquisition.reportView,
      },
      {
        key: 'inventory',
        title: 'Kết quả kiểm kê',
        description: 'Khớp, thiếu, thừa và sai kho của từng kỳ kiểm kê.',
        path: '/bo-sung/kiem-ke',
        permission: PERMISSIONS.acquisition.inventoryView,
      },
    ],
  },
  {
    key: 'circulation',
    title: 'Lưu thông',
    links: [
      {
        key: 'circulation-reports',
        title: 'Bảy báo cáo lưu thông',
        description:
          'Ra vào thư viện, đang mượn, lịch sử, quá hạn, tủ gửi đồ, bạn đọc và ấn phẩm mượn nhiều nhất.',
        path: '/luu-thong/bao-cao',
        permission: PERMISSIONS.circulation.reportView,
      },
      {
        key: 'fines',
        title: 'Tiền phạt',
        description: 'Khoản phạt theo bạn đọc, tình trạng thu và miễn giảm.',
        path: '/luu-thong/tien-phat',
        permission: PERMISSIONS.circulation.fineView,
      },
    ],
  },
  {
    key: 'reader',
    title: 'Bạn đọc',
    links: [
      {
        key: 'reader-reports',
        title: 'Báo cáo bạn đọc',
        description:
          'Số lượng theo loại, khoa, ngành, khóa; bạn đọc mới, thẻ sắp hết hạn và bạn đọc tích cực.',
        path: '/ban-doc/bao-cao',
        permission: PERMISSIONS.reader.reportView,
      },
    ],
  },
  {
    key: 'serial',
    title: 'Ấn phẩm định kỳ',
    links: [
      {
        key: 'serial-reports',
        title: 'Báo cáo ấn phẩm định kỳ',
        description: 'Tổng hợp, theo môn loại, theo mức định kỳ và theo ngôn ngữ.',
        path: '/an-pham-dinh-ky/bao-cao',
        permission: PERMISSIONS.serial.reportView,
      },
    ],
  },
  {
    key: 'digital',
    title: 'Tài liệu số',
    links: [
      {
        key: 'digital-reports',
        title: 'Báo cáo tài liệu số',
        description: 'Số lượng theo bộ sưu tập và định dạng, lượt xem, lượt tải, dung lượng đã dùng.',
        path: '/tai-lieu-so/bao-cao',
        permission: PERMISSIONS.digital.reportView,
      },
    ],
  },
  {
    key: 'course',
    title: 'Tài liệu môn học',
    links: [
      {
        key: 'course-reports',
        title: 'Báo cáo tài liệu môn học',
        description: 'Môn chưa có tài liệu, tài liệu dùng chung nhiều môn và mức độ đáp ứng theo ngành.',
        path: '/tai-lieu-mon-hoc/bao-cao',
        permission: PERMISSIONS.course.reportView,
      },
    ],
  },
  {
    key: 'system',
    title: 'Hệ thống',
    links: [
      {
        key: 'audit',
        title: 'Nhật ký hệ thống',
        description: 'Ai làm gì, lúc nào, trên bản ghi nào — tra được cả giá trị trước và sau khi sửa.',
        path: '/he-thong/nhat-ky',
        permission: PERMISSIONS.system.auditView,
      },
    ],
  },
];
