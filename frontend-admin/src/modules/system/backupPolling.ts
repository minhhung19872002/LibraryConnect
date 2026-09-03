import type { BackupJob } from './types';

/**
 * Bao lâu hỏi lại máy chủ một lần khi màn hình sao lưu đang mở.
 *
 * Sao lưu chạy ở tiến trình nền (Hangfire) từ khi sửa lỗi H9, nên trang phải tự hỏi lại mới thấy
 * lượt đang chạy chuyển sang xong. Hết việc thì thôi hỏi, tránh gõ cửa máy chủ suốt ngày khi không
 * có gì chạy. Cùng khuôn với `harvestPollInterval` của phân hệ liên thư viện.
 */
export function backupPollInterval(
  jobs: Pick<BackupJob, 'status'>[] | undefined,
): number | false {
  const dangChay = (jobs ?? []).some(
    (job) => job.status === 'Pending' || job.status === 'Running',
  );

  return dangChay ? 3000 : false;
}
