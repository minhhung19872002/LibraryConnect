import type { ThemeConfig } from 'antd';

/**
 * Bảng màu và kiểu chữ của trang tra cứu.
 *
 * Cùng một bộ với giao diện quản trị — nền giấy ngà, chữ nâu đen, xanh rêu làm màu chính — vì bạn
 * đọc và cán bộ nhìn chung một sản phẩm, và cán bộ thì mở cả hai cửa sổ cạnh nhau cả ngày. Khác
 * nhau ở chỗ khác: OPAC rộng chữ hơn (15px thân bài, khung tra cứu 1120px) vì bạn đọc đọc chứ
 * không nhập liệu, còn quản trị thì dày đặc bảng biểu.
 *
 * Hai bộ chữ chia việc như bên quản trị: {@link CHU_TRINH_BAY} cho tên thư viện, nhan đề tài liệu
 * và con số thống kê; phần còn lại là Be Vietnam Pro.
 */

/** Chữ có chân, dành cho tên thư viện, nhan đề tài liệu và con số. */
export const CHU_TRINH_BAY = "'Lora', Georgia, 'Times New Roman', serif";

/** Chữ không chân của toàn bộ phần còn lại. */
export const CHU_NOI_DUNG =
  "'Be Vietnam Pro', 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif";

/** Chữ đều nét cho ĐKCB, ISBN và chỉ số phân loại. */
export const CHU_DEU_NET = "'Cascadia Mono', Consolas, 'Courier New', monospace";

export const theme: ThemeConfig = {
  token: {
    colorPrimary: '#35523f',
    colorLink: '#35523f',
    colorInfo: '#35523f',
    colorSuccess: '#4d6a42',
    colorWarning: '#9a6c1c',
    colorError: '#a03c2e',

    // Nền và viền của bốn loại thông báo. Phải khai thẳng: Ant Design suy nền của Alert ra từ
    // `colorInfo`, mà `colorInfo` nay là xanh rêu đậm — để nó tự suy thì hộp thông báo thành một
    // mảng xanh đậm chữ trắng mờ, đọc không nổi.
    colorInfoBg: '#eef2e4',
    colorInfoBorder: '#d5ddc4',
    colorSuccessBg: '#e7ecdb',
    colorSuccessBorder: '#cbd9bc',
    colorWarningBg: '#f7ecd8',
    colorWarningBorder: '#e6cfa4',
    colorErrorBg: '#f8e8e2',
    colorErrorBorder: '#d8b5ac',

    colorText: '#2a2118',
    colorTextSecondary: '#7a6f5f',
    colorTextTertiary: '#7f7461',
    colorTextDescription: '#7f7461',

    colorBgLayout: '#f4efe6',
    colorBgContainer: '#fffdf8',
    colorBgElevated: '#fffdf8',
    colorBorder: '#e3d9c7',
    colorBorderSecondary: '#f0e9da',

    borderRadius: 8,
    fontFamily: CHU_NOI_DUNG,
    fontFamilyCode: CHU_DEU_NET,
    fontSize: 15,
  },
  components: {
    Layout: {
      headerBg: '#fffdf8',
      bodyBg: '#f4efe6',
      footerBg: '#22301f',
    },
    Menu: {
      horizontalItemSelectedColor: '#35523f',
      horizontalItemHoverColor: '#35523f',
      itemColor: '#7a6f5f',
      itemSelectedColor: '#35523f',
      itemSelectedBg: '#eef2e4',
      itemHoverBg: '#f1ebdd',
      itemBorderRadius: 8,
    },
    Card: {
      paddingLG: 20,
      headerBg: 'transparent',
    },
    Input: {
      colorBorder: '#d8cdb6',
    },
    Select: {
      colorBorder: '#d8cdb6',
    },
    Button: {
      defaultBorderColor: '#cbbfa6',
      primaryShadow: 'none',
      defaultShadow: 'none',
    },
    Tag: {
      defaultBg: '#f1ebdd',
      // Đậm hơn `colorTextSecondary` một nấc, và có lý do đo được: chữ #7a6f5f trên nền thẻ
      // #f1ebdd chỉ đạt 4,14:1, dưới ngưỡng 4,5:1 của WCAG AA. Thẻ mang thông tin thật — dạng
      // tài liệu, ngôn ngữ, trạng thái bản in — nên phải đọc được, không phải là chữ trang trí.
      defaultColor: '#6e6252',
    },
    Table: {
      headerBg: '#f6f1e5',
      headerColor: '#7f7461',
      borderColor: '#f0e9da',
      rowHoverBg: '#f8f4ea',
    },
    Tabs: {
      itemSelectedColor: '#35523f',
      inkBarColor: '#35523f',
    },
    Descriptions: {
      labelBg: '#f6f1e5',
    },
  },
};
