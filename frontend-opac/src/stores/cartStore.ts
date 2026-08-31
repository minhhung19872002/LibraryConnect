import { create } from 'zustand';
import type { SearchResult } from '@/types/api';

const STORAGE_KEY = 'lc.opac.cart';

interface CartState {
  items: SearchResult[];
  add: (item: SearchResult) => void;
  remove: (id: string) => void;
  clear: () => void;
  has: (id: string) => boolean;
}

/**
 * Giỏ tài liệu (IX.2).
 *
 * Giữ trong bộ nhớ trình duyệt chứ không gửi lên máy chủ: bạn đọc chưa đăng nhập vẫn phải gom được
 * danh sách sách cần tìm rồi mới quyết định làm gì với nó. Chỉ khi bấm gửi email thì danh sách mới
 * đi lên, và lúc đó mới cần tài khoản.
 */
function load(): SearchResult[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as SearchResult[]) : [];
  } catch {
    // Dữ liệu hỏng thì bỏ đi và bắt đầu lại; giỏ tài liệu không đáng để chặn cả trang.
    return [];
  }
}

function save(items: SearchResult[]) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
  } catch {
    // Trình duyệt chặn lưu (chế độ riêng tư, hết dung lượng) thì giỏ chỉ sống trong phiên này.
  }
}

export const useCartStore = create<CartState>((set, get) => ({
  items: load(),

  add(item) {
    if (get().items.some((row) => row.id === item.id)) {
      return;
    }

    const items = [...get().items, item];
    save(items);
    set({ items });
  },

  remove(id) {
    const items = get().items.filter((row) => row.id !== id);
    save(items);
    set({ items });
  },

  clear() {
    save([]);
    set({ items: [] });
  },

  has(id) {
    return get().items.some((row) => row.id === id);
  },
}));
