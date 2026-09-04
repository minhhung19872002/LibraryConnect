import { Alert, Col, Input, Row, Select, Space, Tooltip, Typography } from 'antd';
import { InfoCircleOutlined } from '@ant-design/icons';
import {
  CONTROL_008_LENGTH,
  CONTROL_008_MATERIAL_LABELS,
  fromDisplay,
  materialOf,
  positionsFor,
  toDisplay,
} from './control008';
import { getFieldRange, setFieldRange } from './marcRecord';

interface Control008WizardProps {
  value: string;
  leader: string;
  onChange: (value: string) => void;
  readOnly?: boolean;
}

const MONOSPACE = { fontFamily: 'ui-monospace, Consolas, monospace' } as const;

/**
 * Trình hướng dẫn nhập trường 008 (đặc tả II.2).
 *
 * Forty characters where the meaning is the position: nobody edits that as a string without a chart
 * open beside them. Each position becomes a labelled control with the legal values written out in
 * Vietnamese, and the raw string stays visible underneath so a cataloguer who does know the layout
 * can still type straight into it — and so it is obvious the two are the same field.
 */
export function Control008Wizard({ value, leader, onChange, readOnly }: Control008WizardProps) {
  const padded = value.padEnd(CONTROL_008_LENGTH, ' ').slice(0, CONTROL_008_LENGTH);
  const material = materialOf(leader);
  const visible = positionsFor(material);

  const update = (start: number, length: number, next: string) =>
    onChange(setFieldRange(padded, start, length, fromDisplay(next), CONTROL_008_LENGTH));

  return (
    <div>
      {material === 'other' ? (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message="Đầu biểu đang khai một loại hình chưa có bảng riêng"
          description={
            'Trình hướng dẫn có bảng cho sách, ấn phẩm định kỳ và bản đồ (theo Đầu biểu/06–07). '
            + 'Với loại hình này, vị trí 18–34 sửa ở ô chuỗi đầy đủ bên dưới.'
          }
        />
      ) : (
        <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 12 }}>
          Khối 18–34 đang hiện theo loại hình <strong>{CONTROL_008_MATERIAL_LABELS[material]}</strong>,
          suy từ Đầu biểu vị trí 06–07. Đổi Đầu biểu thì bảng đổi theo.
        </Typography.Text>
      )}

      <Row gutter={[16, 12]}>
        {visible.map((entry) => {
          const current = toDisplay(getFieldRange(padded, entry.start, entry.length, CONTROL_008_LENGTH));

          return (
            <Col key={entry.start} xs={24} sm={12} lg={8}>
              <Space direction="vertical" size={4} style={{ width: '100%' }}>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  {entry.label}
                  {entry.hint && (
                    <>
                      {' '}
                      <Tooltip title={entry.hint}>
                        <InfoCircleOutlined />
                      </Tooltip>
                    </>
                  )}
                </Typography.Text>

                {entry.options ? (
                  <Select
                    value={current}
                    onChange={(next) => update(entry.start, entry.length, next)}
                    options={entry.options.map((option) => ({
                      value: option.code,
                      label: option.label,
                    }))}
                    disabled={readOnly || entry.readOnly}
                    style={{ width: '100%' }}
                  />
                ) : (
                  <Input
                    value={current}
                    onChange={(event) => update(entry.start, entry.length, event.target.value)}
                    disabled={readOnly || entry.readOnly}
                    maxLength={entry.length}
                    style={{ ...MONOSPACE, width: '100%' }}
                  />
                )}
              </Space>
            </Col>
          );
        })}
      </Row>

      <Space direction="vertical" size={4} style={{ width: '100%', marginTop: 16 }}>
        <Typography.Text type="secondary" style={{ fontSize: 12 }}>
          Toàn bộ trường 008 ({CONTROL_008_LENGTH} ký tự)
        </Typography.Text>
        <Input
          value={padded}
          onChange={(event) =>
            onChange(event.target.value.padEnd(CONTROL_008_LENGTH, ' ').slice(0, CONTROL_008_LENGTH))
          }
          disabled={readOnly}
          maxLength={CONTROL_008_LENGTH}
          style={MONOSPACE}
        />
        <Typography.Text type="secondary" style={{ fontSize: 12 }}>
          Ký tự # ở các ô trên là một khoảng trắng có nghĩa, không phải dấu thăng.
        </Typography.Text>
      </Space>
    </div>
  );
}
