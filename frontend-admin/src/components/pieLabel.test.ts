import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

function tepTsx(thuMuc: string): string[] {
  return readdirSync(thuMuc).flatMap((ten) => {
    const duong = join(thuMuc, ten);
    if (statSync(duong).isDirectory()) return tepTsx(duong);
    return duong.endsWith('.tsx') && !duong.endsWith('.test.tsx') ? [duong] : [];
  });
}

/**
 * Biểu đồ tròn không được vẽ nhãn ra ngoài lát.
 *
 * Recharts đặt nhãn ngoài ở toạ độ nằm ngoài khung SVG, và tên chỉ mục tiếng Việt thì dài. Đo trên
 * máy chủ thật ở đúng khổ màn hình tối thiểu mà mục 6.6 cam kết (1366×768): trang Báo cáo bổ sung
 * cuộn ngang 18 px, nhãn chạy tới x = 1427. Ở 1440 trang không cuộn nhưng nhãn bị cắt cụt thành
 * ": 3621" và ":508" — nên chín đợt rà trước, vốn chụp ở 1440, đều không thấy gì.
 *
 * Bốn trang báo cáo cùng mắc một lỗi. Luật này canh chỗ thứ năm.
 */
describe('Nhãn biểu đồ tròn', () => {
  it('không màn hình nào tự vẽ nhãn ngoài lát', () => {
    const pham: string[] = [];

    for (const duong of tepTsx('src')) {
      if (duong.split(String.fromCharCode(92)).join('/').endsWith('components/PieLabel.tsx')) continue;

      const noiDung = readFileSync(duong, 'utf8');
      if (!noiDung.includes('<Pie')) continue;

      // Mọi <Pie> phải khai nhãn bằng hàm dùng chung, hoặc không khai nhãn nào.
      const soPie = (noiDung.match(/<Pie\b/g) ?? []).length;
      const soNhanChung = (noiDung.match(/label=\{nhanTrongLat\}/g) ?? []).length;
      const nhanKhac = noiDung.match(/label=\{(?!nhanTrongLat\})/g) ?? [];
      const nhanTran = noiDung.match(/\n\s*label\s*\n/g) ?? [];

      if (nhanKhac.length > 0 || nhanTran.length > 0 || soNhanChung !== soPie) {
        pham.push(
          `${duong}: ${soPie} biểu đồ tròn, ${soNhanChung} dùng nhanTrongLat` +
            (nhanKhac.length ? `, ${nhanKhac.length} nhãn tự viết` : '') +
            (nhanTran.length ? `, ${nhanTran.length} nhãn mặc định` : ''),
        );
      }
    }

    expect(pham).toEqual([]);
  });

  it('mỗi biểu đồ tròn đều có chú giải để biết lát nào là gì', () => {
    const pham: string[] = [];

    for (const duong of tepTsx('src')) {
      const noiDung = readFileSync(duong, 'utf8');
      if (!noiDung.includes('<Pie')) continue;

      const soPie = (noiDung.match(/<Pie\b/g) ?? []).length;
      const soChuGiai = (noiDung.match(/<Legend\b/g) ?? []).length;

      if (soChuGiai < soPie) {
        pham.push(`${duong}: ${soPie} biểu đồ tròn nhưng chỉ ${soChuGiai} chú giải`);
      }
    }

    expect(pham).toEqual([]);
  });
});
