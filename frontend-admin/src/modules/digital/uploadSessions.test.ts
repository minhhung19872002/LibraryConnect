import { describe, expect, it } from 'vitest';
import {
  findSession,
  forgetSession,
  pruneSessions,
  rememberSession,
} from './uploadSessions';

/** localStorage giả, đủ dùng cho bộ nhớ khoá–giá trị. */
function fakeStorage(): Storage {
  const data = new Map<string, string>();
  return {
    get length() {
      return data.size;
    },
    clear: () => data.clear(),
    getItem: (key: string) => data.get(key) ?? null,
    key: (index: number) => [...data.keys()][index] ?? null,
    removeItem: (key: string) => void data.delete(key),
    setItem: (key: string, value: string) => void data.set(key, value),
  } as Storage;
}

/** localStorage bị trình duyệt chặn: mọi lượt gọi đều ném. */
function blockedStorage(): Storage {
  const throwing = () => {
    throw new Error('Bộ nhớ trang bị chặn');
  };
  return {
    length: 0,
    clear: throwing,
    getItem: throwing,
    key: throwing,
    removeItem: throwing,
    setItem: throwing,
  } as unknown as Storage;
}

const NOW = 1_700_000_000_000;

describe('phiên tải tệp lớn', () => {
  it('nhớ phiên rồi tìm lại được bằng đúng tên và dung lượng tệp', () => {
    const storage = fakeStorage();

    rememberSession(storage, {
      sessionId: 'abc',
      fileName: 'luan-van.pdf',
      fileSize: 300_000_000,
      startedAt: NOW,
    });

    expect(findSession(storage, 'luan-van.pdf', 300_000_000, NOW)?.sessionId).toBe('abc');
    // Cùng tên nhưng khác dung lượng là tệp khác: không được nối tiếp nhầm phiên.
    expect(findSession(storage, 'luan-van.pdf', 299_999_999, NOW)).toBeUndefined();
    expect(findSession(storage, 'khac.pdf', 300_000_000, NOW)).toBeUndefined();
  });

  it('chọn lại cùng tệp thì thay phiên cũ, không chồng thêm dòng', () => {
    const storage = fakeStorage();
    const file = { fileName: 'sach.pdf', fileSize: 100 };

    rememberSession(storage, { sessionId: 'cu', ...file, startedAt: NOW });
    rememberSession(storage, { sessionId: 'moi', ...file, startedAt: NOW + 1000 });

    expect(pruneSessions(storage, NOW + 2000)).toHaveLength(1);
    expect(findSession(storage, file.fileName, file.fileSize, NOW + 2000)?.sessionId).toBe('moi');
  });

  it('bỏ phiên quá bảy ngày vì máy chủ đã dọn', () => {
    const storage = fakeStorage();
    rememberSession(storage, {
      sessionId: 'qua-han',
      fileName: 'cu.pdf',
      fileSize: 10,
      startedAt: NOW,
    });

    const eightDays = NOW + 8 * 24 * 60 * 60 * 1000;

    expect(findSession(storage, 'cu.pdf', 10, eightDays)).toBeUndefined();
    expect(pruneSessions(storage, eightDays)).toHaveLength(0);
  });

  it('tải xong thì quên phiên đi', () => {
    const storage = fakeStorage();
    rememberSession(storage, {
      sessionId: 'xong',
      fileName: 'a.pdf',
      fileSize: 5,
      startedAt: NOW,
    });

    forgetSession(storage, 'a.pdf', 5);

    expect(findSession(storage, 'a.pdf', 5, NOW)).toBeUndefined();
  });

  it('trình duyệt chặn bộ nhớ trang thì im lặng bỏ qua, không làm hỏng lượt tải', () => {
    const storage = blockedStorage();

    expect(() =>
      rememberSession(storage, {
        sessionId: 'x',
        fileName: 'a.pdf',
        fileSize: 1,
        startedAt: NOW,
      }),
    ).not.toThrow();
    expect(findSession(storage, 'a.pdf', 1, NOW)).toBeUndefined();
    expect(pruneSessions(storage, NOW)).toEqual([]);
  });

  it('dữ liệu trong bộ nhớ trang bị hỏng thì coi như chưa có phiên nào', () => {
    const storage = fakeStorage();
    storage.setItem('lc.digital.uploads', 'không phải JSON');

    expect(pruneSessions(storage, NOW)).toEqual([]);

    storage.setItem('lc.digital.uploads', '[{"sessionId":123}]');
    expect(pruneSessions(storage, NOW)).toEqual([]);
  });
});
