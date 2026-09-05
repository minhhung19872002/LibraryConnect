import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { theme } from './theme';

/**
 * Bảng màu bị vẽ ở hai nơi, và hai nơi ấy phải nói cùng một điều.
 *
 * Ant Design vẽ bảng, biểu mẫu, thẻ và nút từ token trong `theme.ts`; còn cột menu, thanh trên,
 * trang đăng nhập và mọi dấu hiệu tự vẽ thì lấy màu từ biến `--lc-*` trong `styles.css`. Hai bên
 * nằm cạnh nhau trên cùng một màn hình: cột menu tự vẽ đứng sát bảng do Ant Design vẽ, viền của
 * hai bên chạm nhau ngay giữa trang.
 *
 * Lệch một sắc là lộ ra thành một đường kẻ mờ chạy dọc giữa màn hình, và không ai nhìn ảnh chụp
 * màn hình mà đoán ra đó là do hai tệp khác nhau. Nên chốt bằng phép thử thay vì bằng trí nhớ:
 * đổi màu ở một nơi mà quên nơi kia thì phép thử này đỏ ngay.
 *
 * Cùng họ với ba phép thử quét mã nguồn khác — chặn cả một lớp lỗi thay vì chặn một chỗ.
 */
describe('Bảng màu của theme.ts và styles.css khớp nhau', () => {
  const css = readFileSync(join(process.cwd(), 'src', 'styles.css'), 'utf8');

  /** Đọc giá trị của một biến CSS khai trong khối `:root`. */
  function bien(ten: string): string | undefined {
    return new RegExp(`--${ten}:\\s*([^;]+);`).exec(css)?.[1]?.trim();
  }

  const capDoi: [keyof NonNullable<typeof theme.token>, string][] = [
    ['colorPrimary', 'lc-green'],
    ['colorBgLayout', 'lc-page-bg'],
    ['colorBgContainer', 'lc-paper'],
    ['colorBorder', 'lc-border'],
    ['colorBorderSecondary', 'lc-border-soft'],
    ['colorText', 'lc-ink'],
    ['colorTextSecondary', 'lc-muted'],
    ['colorTextTertiary', 'lc-muted-light'],
    ['colorWarning', 'lc-gold'],
      ];

  it.each(capDoi)('token %s trùng biến --%s', (token, ten) => {
    const giaTriCss = bien(ten);

    expect(giaTriCss, `styles.css thiếu biến --${ten}`).toBeDefined();
    expect(theme.token?.[token]).toBe(giaTriCss);
  });

  it('nền bảng dùng đúng sắc giấy đậm khai trong styles.css', () => {
    expect(theme.components?.Table?.headerBg).toBe(bien('lc-panel'));
  });

  it('màu nền khi rê chuột của menu trùng biến --lc-hover', () => {
    expect(theme.components?.Menu?.itemHoverBg).toBe(bien('lc-hover'));
  });

  it('chân trang lấy đúng sắc xanh rêu đậm nhất của bảng màu', () => {
    expect(theme.components?.Layout?.footerBg).toBe(bien('lc-green-dark'));
  });

  /*
   * Hai bộ chữ phải được tải thật.
   *
   * Khai `font-family: 'Lora'` mà không có thẻ liên kết nào tải nó về thì trình duyệt lặng lẽ rơi
   * xuống phông dự phòng — trang vẫn hiện bình thường, chỉ là không còn chữ có chân, và không có
   * lỗi nào báo ra. Đúng loại hỏng chỉ phát hiện được bằng cách nhìn tận mắt, nên chốt lại ở đây.
   */
  it('index.html tải cả Lora lẫn Be Vietnam Pro', () => {
    const html = readFileSync(join(process.cwd(), 'index.html'), 'utf8');

    expect(html).toContain('fonts.googleapis.com');
    expect(html).toMatch(/family=Be\+Vietnam\+Pro/);
    expect(html).toMatch(/family=Lora/);
  });

  it('chữ thân bài là Be Vietnam Pro, chữ trình bày là Lora', () => {
    expect(theme.token?.fontFamily).toContain('Be Vietnam Pro');
    expect(bien('lc-chu-trinh-bay')).toContain('Lora');
  });
});

/**
 * Độ tương phản của bảng màu.
 *
 * Mục 6.6 đòi đạt WCAG AA. Bảng màu giấy ngà rất dễ trượt chỗ này: nền không phải trắng nên mọi
 * cặp màu đều tối đi một chút so với lúc chọn trên nền trắng, mà mắt thì không đo được — nhìn ảnh
 * chụp thấy "đọc được" là cho qua.
 *
 * Đã trượt thật: chữ thẻ #7a6f5f trên nền thẻ #f1ebdd chỉ đạt 4,14:1. Nhìn thì vẫn đọc được, mà
 * thẻ lại đang mang thông tin thật — dạng tài liệu, ngôn ngữ, trạng thái bản in. Phải đo mới thấy.
 *
 * Ngưỡng: 4,5:1 cho chữ thường, 3:1 cho chữ chỉ mang tính phụ trợ (nhãn nhóm, dòng phiên bản ở
 * chân cột menu) — đúng hai ngưỡng WCAG đặt ra.
 */
describe('Bảng màu đạt WCAG AA', () => {
  function doSang(mau: string): number {
    const h = mau.replace('#', '');
    const kenh = [0, 2, 4].map((i) => {
      const v = parseInt(h.slice(i, i + 2), 16) / 255;
      return v <= 0.04045 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4;
    }) as [number, number, number];

    return 0.2126 * kenh[0] + 0.7152 * kenh[1] + 0.0722 * kenh[2];
  }

  function tuongPhan(chu: string, nen: string): number {
    const a = doSang(chu);
    const b = doSang(nen);

    return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
  }

  const cap: [string, string, string, number][] = [
    ['chữ thân bài trên giấy', '#2a2118', '#fffdf8', 4.5],
    ['chữ thân bài trên nền trang', '#2a2118', '#f4efe6', 4.5],
    ['chữ phụ trên giấy', '#7a6f5f', '#fffdf8', 4.5],
    ['chữ mờ nhất trên giấy', '#7f7461', '#fffdf8', 4.5],
    ['xanh rêu trên giấy', '#35523f', '#fffdf8', 4.5],
    ['chữ trên nút chính', '#f2ecdd', '#35523f', 4.5],
    ['thẻ trung tính', '#6e6252', '#f1ebdd', 4.5],
    ['thẻ xanh dương đã chỉnh', '#3b4f86', '#eaedf6', 4.5],
    ['thẻ xanh ngọc đã chỉnh', '#1c5f57', '#e4efec', 4.5],
    ['thẻ tím đã chỉnh', '#5f3f7e', '#f0eaf5', 4.5],
    ['chữ trên nền xanh rêu đậm', '#f2ecdd', '#2a3f2c', 4.5],
    ['chữ phụ trên nền xanh rêu đậm', '#c9c3ae', '#2a3f2c', 4.5],
    ['ô mã đều nét', '#2a2118', '#f1ebdd', 4.5],
  ];

  it.each(cap)('%s đạt ngưỡng', (_ten, chu, nen, nguong) => {
    expect(tuongPhan(chu, nen)).toBeGreaterThanOrEqual(nguong);
  });


  const capRieng: [string, string, string, number][] = [
    ['nhan đề kết quả tra cứu', '#35523f', '#fffdf8', 4.5],
    ['chân trang', '#c9c3ae', '#22301f', 4.5],
    // Lighthouse trên máy chủ thật ngày 05/09/2026 đo ba cặp dưới đây trượt: nhãn nhóm bộ lọc và bộ
    // đếm facet là chữ 11–13,5 px nên không được hưởng ngưỡng 3 của chữ lớn; nút Tra cứu vàng chữ
    // trắng chỉ đạt 3,25; dòng cuối chân trang 4,01. Bài học 19: nền giấy làm mọi cặp tối đi một chút.
    ['nhãn nhóm bộ lọc và bộ đếm facet', '#7f7461', '#fffdf8', 4.5],
    ['chữ trắng trên nút vàng', '#ffffff', '#9a6c1c', 4.5],
    ['chữ trắng trên nút vàng khi rê chuột', '#ffffff', '#8a6114', 4.5],
    ['dòng cuối chân trang', '#a8a28e', '#22301f', 4.5],
  ];

  it.each(capRieng)('%s đạt ngưỡng', (_ten, chu, nen, nguong) => {
    expect(tuongPhan(chu, nen)).toBeGreaterThanOrEqual(nguong);
  });

  it('màu chữ thẻ trong theme.ts đúng là màu đã đo', () => {
    // Chốt lại: bảng trên đo màu nào thì theme phải dùng đúng màu ấy, chứ không phải đo một đằng
    // dùng một nẻo.
    expect(theme.components?.Tag?.defaultColor).toBe('#6e6252');
  });
});
