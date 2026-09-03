import { describe, expect, it } from 'vitest';
import { backupPollInterval } from './backupPolling';
import type { BackupStatus } from './types';

const job = (status: BackupStatus) => ({ status });

describe('nhịp hỏi lại của màn hình sao lưu', () => {
  it('không hỏi lại khi mọi lượt đã kết thúc', () => {
    expect(backupPollInterval([job('Success'), job('Failed')])).toBe(false);
  });

  it('hỏi lại khi còn lượt đang xếp hàng hoặc đang chạy', () => {
    expect(backupPollInterval([job('Success'), job('Pending')])).toBe(3000);
    expect(backupPollInterval([job('Running')])).toBe(3000);
  });

  it('chưa có dữ liệu thì chưa cần hỏi lại', () => {
    expect(backupPollInterval(undefined)).toBe(false);
    expect(backupPollInterval([])).toBe(false);
  });
});
