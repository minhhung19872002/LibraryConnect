import type { ThemeConfig } from 'antd';

/**
 * Bảng màu của trang tra cứu.
 *
 * Khác màu giao diện quản trị một cách có chủ ý: cán bộ mở cả hai cửa sổ cạnh nhau cả ngày, nhìn
 * màu là biết đang ở trang nào. Xanh lá đậm cũng là màu quen thuộc của biển hiệu thư viện.
 */
export const theme: ThemeConfig = {
  token: {
    colorPrimary: '#0b6b4f',
    colorLink: '#0b6b4f',
    colorInfo: '#0b6b4f',
    borderRadius: 8,
    fontFamily:
      "'Be Vietnam Pro', 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif",
    fontSize: 15,
  },
  components: {
    Layout: {
      headerBg: '#ffffff',
      bodyBg: '#f5f7f6',
      footerBg: '#0f2a22',
    },
    Menu: {
      horizontalItemSelectedColor: '#0b6b4f',
    },
    Card: {
      paddingLG: 20,
    },
  },
};
