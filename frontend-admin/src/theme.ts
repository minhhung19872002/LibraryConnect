import type { ThemeConfig } from 'antd';

/**
 * Shared visual language of the admin application (section 6.6): one font that renders Vietnamese
 * diacritics correctly, a slightly denser layout than the Ant Design default because the business
 * screens are table heavy, and consistent control sizing across every module.
 */
export const theme: ThemeConfig = {
  token: {
    colorPrimary: '#1668dc',
    colorLink: '#1668dc',
    borderRadius: 6,
    fontFamily:
      "'Be Vietnam Pro', 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif",
    fontSize: 14,
    controlHeight: 34,
  },
  components: {
    Layout: {
      headerBg: '#ffffff',
      headerHeight: 56,
      headerPadding: '0 16px',
      bodyBg: '#f5f6f8',
      siderBg: '#ffffff',
    },
    Menu: {
      itemHeight: 38,
      itemMarginInline: 8,
    },
    Table: {
      headerBg: '#fafafa',
      cellPaddingBlock: 10,
      rowHoverBg: '#f0f6ff',
    },
    Card: {
      paddingLG: 16,
    },
  },
};
