import { MAU_CHU_TREN_LAT } from '@/lib/palette';

/**
 * Nhãn của biểu đồ tròn — vẽ **bên trong** lát, không vẽ ra ngoài.
 *
 * Recharts vẽ nhãn ngoài ở toạ độ nằm ngoài khung SVG, và tên chỉ mục tiếng Việt thì dài
 * ("Đề tài nghiên cứu khoa học: 3071"). Đo trên máy chủ thật ở đúng khổ màn hình tối thiểu mà mục
 * 6.6 cam kết (1366×768): nhãn của biểu đồ "Theo tình trạng" chạy tới x = 1427, đẩy cả trang cuộn
 * ngang 18 px. Ở 1440 thì trang không cuộn nhưng nhãn bị cắt cụt thành ": 3621", ":508" — nên chín
 * đợt rà trước chụp ở 1440 đều không thấy gì.
 *
 * Tên của từng lát đã có ở `<Legend />` ngay dưới biểu đồ và con số chính xác có ở tooltip, nên
 * nhãn trong lát chỉ cần tỉ lệ phần trăm. Lát nhỏ hơn 5% thì bỏ nhãn: chữ không đủ chỗ, mà thông
 * tin ấy vẫn đọc được ở chú giải.
 *
 * Chữ trắng trên bảng màu biểu đồ đạt thấp nhất 7,44 : 1 (đo cho cả mười màu).
 */
export const NHAN_TOI_THIEU = 0.05;

type ViTriLat = {
  cx?: number;
  cy?: number;
  midAngle?: number;
  innerRadius?: number;
  outerRadius?: number;
  percent?: number;
};

/**
 * Dùng cho `<Pie label={nhanTrongLat} labelLine={false}>`. Luôn kèm `<Legend />` để người đọc biết
 * lát nào là gì.
 */
export function nhanTrongLat(lat: ViTriLat) {
  const { cx, cy, midAngle, innerRadius, outerRadius, percent } = lat;

  if (
    cx === undefined ||
    cy === undefined ||
    midAngle === undefined ||
    innerRadius === undefined ||
    outerRadius === undefined ||
    percent === undefined ||
    percent < NHAN_TOI_THIEU
  ) {
    return null;
  }

  const radian = Math.PI / 180;
  const banKinh = innerRadius + (outerRadius - innerRadius) * 0.6;
  const x = cx + banKinh * Math.cos(-midAngle * radian);
  const y = cy + banKinh * Math.sin(-midAngle * radian);

  return (
    <text
      x={x}
      y={y}
      fill={MAU_CHU_TREN_LAT}
      textAnchor="middle"
      dominantBaseline="central"
      fontSize={11}
      fontWeight={600}
    >
      {`${Math.round(percent * 100)}%`}
    </text>
  );
}
