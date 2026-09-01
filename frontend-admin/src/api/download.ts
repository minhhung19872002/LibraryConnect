import { api } from './client';

/**
 * Tải một tệp từ API về máy người dùng.
 *
 * Không dùng thẻ liên kết trỏ thẳng vào API được: hệ thống xác thực bằng JWT đặt trong tiêu đề yêu
 * cầu, mà thẻ trình duyệt mở ra thì không mang tiêu đề nào. Người dùng bấm nút xuất sẽ nhận về một
 * trang trắng in dòng JSON "Phiên đăng nhập không hợp lệ" và tưởng mình đã bị đăng xuất.
 *
 * Lấy tệp bằng chính lớp gọi API đã gắn sẵn mã đăng nhập, rồi mới đưa cho trình duyệt lưu.
 */
export async function downloadFile(url: string, fallbackName: string): Promise<string> {
  const { blob, fileName } = await api.download(url);
  const name = fileName === 'download' ? fallbackName : fileName;

  saveBlob(blob, name);

  return name;
}

/** Lưu một blob về máy người dùng dưới tên đã cho. */
export function saveBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');

  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}
