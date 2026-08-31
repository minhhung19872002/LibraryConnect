import { Col, Input, Row, Select, Space, Tooltip, Typography } from 'antd';
import { InfoCircleOutlined } from '@ant-design/icons';
import { getLeaderPosition, setLeaderPosition } from './marcRecord';

/**
 * Mỗi vị trí có ý nghĩa của đầu biểu, kèm các giá trị thông dụng.
 *
 * A cataloguer refers to these by position — "Leader/06" — so the position is part of the label. The
 * lists are the values that occur in Vietnamese university collections; the free-text box below
 * still accepts anything for the rare case.
 */
const POSITIONS: Array<{
  position: number;
  label: string;
  hint: string;
  options: Array<{ value: string; label: string }>;
}> = [
  {
    position: 5,
    label: 'Trạng thái biểu ghi (05)',
    hint: 'Biểu ghi mới tạo dùng "n". Khi sửa một biểu ghi đã hoàn chỉnh thì chuyển sang "c".',
    options: [
      { value: 'n', label: 'n — Biểu ghi mới' },
      { value: 'c', label: 'c — Đã sửa chữa' },
      { value: 'a', label: 'a — Nâng cấp mức biên mục' },
      { value: 'p', label: 'p — Tăng mức từ sơ lược lên đầy đủ' },
      { value: 'd', label: 'd — Đã hủy' },
    ],
  },
  {
    position: 6,
    label: 'Loại hình biểu ghi (06)',
    hint: 'Sách và giáo trình dùng "a". Đây là căn cứ để hệ thống chọn cách hiển thị và cách lập phích.',
    options: [
      { value: 'a', label: 'a — Tài liệu chữ in' },
      { value: 't', label: 't — Bản thảo chữ viết' },
      { value: 'e', label: 'e — Bản đồ' },
      { value: 'c', label: 'c — Bản nhạc in' },
      { value: 'i', label: 'i — Ghi âm phi âm nhạc' },
      { value: 'j', label: 'j — Ghi âm âm nhạc' },
      { value: 'g', label: 'g — Tài liệu nhìn chiếu' },
      { value: 'k', label: 'k — Tài liệu đồ họa' },
      { value: 'm', label: 'm — Tệp tin điện tử' },
      { value: 'r', label: 'r — Vật thể ba chiều' },
      { value: 'p', label: 'p — Tài liệu hỗn hợp' },
    ],
  },
  {
    position: 7,
    label: 'Cấp thư mục (07)',
    hint: 'Sách lẻ dùng "m". Tạp chí dùng "s". Bài trích trong sách hoặc tạp chí dùng "a".',
    options: [
      { value: 'm', label: 'm — Chuyên khảo, sách lẻ' },
      { value: 's', label: 's — Xuất bản phẩm nhiều kỳ' },
      { value: 'a', label: 'a — Phần chuyên khảo (bài trích)' },
      { value: 'b', label: 'b — Phần của xuất bản phẩm nhiều kỳ' },
      { value: 'c', label: 'c — Bộ sưu tập' },
      { value: 'd', label: 'd — Phần của bộ sưu tập' },
      { value: 'i', label: 'i — Nguồn tin cập nhật liên tục' },
    ],
  },
  {
    position: 17,
    label: 'Mức độ biên mục (17)',
    hint: 'Để trống nghĩa là biên mục đầy đủ. Biên mục sơ lược nhập nhanh dùng "7".',
    options: [
      { value: ' ', label: '# — Mức đầy đủ' },
      { value: '1', label: '1 — Mức đầy đủ, chưa kiểm tra tài liệu' },
      { value: '2', label: '2 — Mức chưa đầy đủ' },
      { value: '4', label: '4 — Mức cơ bản, chưa kiểm tra tài liệu' },
      { value: '5', label: '5 — Mức sơ lược' },
      { value: '7', label: '7 — Mức tối thiểu' },
      { value: '8', label: '8 — Biểu ghi trước khi xuất bản' },
      { value: 'u', label: 'u — Không xác định' },
    ],
  },
  {
    position: 18,
    label: 'Quy tắc mô tả (18)',
    hint: 'Thư viện Việt Nam thường biên mục theo AACR2, chọn "a".',
    options: [
      { value: 'a', label: 'a — Theo AACR2' },
      { value: 'i', label: 'i — Theo ISBD' },
      { value: 'c', label: 'c — ISBD rút gọn' },
      { value: 'n', label: 'n — Không theo ISBD' },
      { value: ' ', label: '# — Không xác định' },
    ],
  },
];

interface LeaderEditorProps {
  leader: string;
  onChange: (leader: string) => void;
  readOnly?: boolean;
}

/**
 * Soạn đầu biểu theo từng vị trí có ý nghĩa thay vì bắt cán bộ đếm ký tự.
 *
 * The two numeric areas — record length and base address — are deliberately not editable: the
 * server recomputes them whenever it writes the exchange format, so any value typed here would be
 * discarded and showing them as editable would be a lie.
 */
export function LeaderEditor({ leader, onChange, readOnly }: LeaderEditorProps) {
  const update = (position: number, value: string) => onChange(setLeaderPosition(leader, position, value));

  return (
    <div>
      <Row gutter={[16, 12]}>
        {POSITIONS.map((entry) => (
          <Col key={entry.position} xs={24} sm={12} lg={8}>
            <Space direction="vertical" size={4} style={{ width: '100%' }}>
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                {entry.label}{' '}
                <Tooltip title={entry.hint}>
                  <InfoCircleOutlined />
                </Tooltip>
              </Typography.Text>
              <Select
                value={getLeaderPosition(leader, entry.position)}
                onChange={(value) => update(entry.position, value)}
                options={entry.options}
                disabled={readOnly}
                style={{ width: '100%' }}
              />
            </Space>
          </Col>
        ))}
      </Row>

      <Space direction="vertical" size={4} style={{ width: '100%', marginTop: 16 }}>
        <Typography.Text type="secondary" style={{ fontSize: 12 }}>
          Toàn bộ đầu biểu (24 ký tự)
        </Typography.Text>
        <Input
          value={leader}
          onChange={(event) => onChange(event.target.value.padEnd(24, ' ').slice(0, 24))}
          disabled={readOnly}
          style={{ fontFamily: 'ui-monospace, Consolas, monospace' }}
          maxLength={24}
        />
        <Typography.Text type="secondary" style={{ fontSize: 12 }}>
          Vị trí 00–04 (độ dài biểu ghi) và 12–16 (địa chỉ vùng dữ liệu) do hệ thống tự tính khi xuất
          ra tệp trao đổi, nhập tay ở đây không có tác dụng.
        </Typography.Text>
      </Space>
    </div>
  );
}
