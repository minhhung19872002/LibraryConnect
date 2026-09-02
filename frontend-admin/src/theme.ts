import type { ThemeConfig } from 'antd';

/**
 * Bảng màu và kiểu chữ của giao diện quản trị.
 *
 * Nền giấy ngà, chữ nâu đen, xanh rêu làm màu chính — nhìn gần với sổ nghiệp vụ của thư viện hơn là
 * với bảng điều khiển kỹ thuật. Cán bộ ngồi trước màn hình này tám tiếng một ngày, nền trắng tinh
 * cùng xanh dương chói là thứ mỏi mắt nhất.
 *
 * Hai bộ chữ chia việc rõ ràng: {@link CHU_TRINH_BAY} (Lora, chữ có chân) chỉ dùng cho tên thư
 * viện, tiêu đề trang và những con số lớn — chỗ cần đọc *một cái là thấy*. Toàn bộ phần còn lại —
 * bảng, biểu mẫu, nhãn nút — dùng Be Vietnam Pro vì chữ không chân đọc nhanh hơn ở cỡ nhỏ và có
 * đủ dấu tiếng Việt.
 *
 * Mọi giá trị ở đây phải trùng với biến `--lc-*` trong `styles.css`: hai nơi cùng vẽ một khung
 * hình, lệch nhau là viền của Ant Design không khớp viền của phần khung tự vẽ.
 */

/** Chữ có chân, dành riêng cho tiêu đề và con số. */
export const CHU_TRINH_BAY = "'Lora', Georgia, 'Times New Roman', serif";

/** Chữ không chân của toàn bộ phần còn lại. */
export const CHU_NOI_DUNG =
  "'Be Vietnam Pro', 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";

/** Chữ đều nét cho mã vạch, ĐKCB, chỉ số phân loại và MARC thô. */
export const CHU_DEU_NET = "'Cascadia Mono', Consolas, 'Courier New', monospace";

export const theme: ThemeConfig = {
  token: {
    colorPrimary: '#35523f',
    colorLink: '#35523f',
    colorInfo: '#35523f',
    colorSuccess: '#4d6a42',
    colorWarning: '#b9852f',
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
    colorTextTertiary: '#9a8f7c',
    colorTextDescription: '#9a8f7c',

    colorBgLayout: '#f4efe6',
    colorBgContainer: '#fffdf8',
    colorBgElevated: '#fffdf8',
    colorBorder: '#e3d9c7',
    colorBorderSecondary: '#f0e9da',

    borderRadius: 8,
    fontFamily: CHU_NOI_DUNG,
    fontFamilyCode: CHU_DEU_NET,
    fontSize: 14,
    controlHeight: 34,
  },
  components: {
    Layout: {
      headerBg: '#fffdf8',
      headerHeight: 58,
      headerPadding: '0 24px',
      bodyBg: '#f4efe6',
      siderBg: '#fffdf8',
      footerBg: '#f4efe6',
    },
    Menu: {
      itemHeight: 36,
      itemMarginInline: 10,
      itemBorderRadius: 8,
      itemSelectedBg: '#eef2e4',
      itemSelectedColor: '#35523f',
      itemHoverBg: '#f1ebdd',
      subMenuItemBg: 'transparent',
    },
    Table: {
      headerBg: '#f6f1e5',
      headerColor: '#9a8f7c',
      headerSplitColor: 'transparent',
      borderColor: '#f0e9da',
      cellPaddingBlock: 11,
      rowHoverBg: '#f8f4ea',
      rowSelectedBg: '#eef2e4',
      rowSelectedHoverBg: '#e7ecdb',
    },
    Card: {
      paddingLG: 16,
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
    Tabs: {
      itemSelectedColor: '#35523f',
      inkBarColor: '#35523f',
    },
    Statistic: {
      contentFontSize: 28,
    },
  },
};
