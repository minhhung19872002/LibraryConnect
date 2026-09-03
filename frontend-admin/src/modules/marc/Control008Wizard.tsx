import { Alert, Col, Input, Row, Select, Space, Tooltip, Typography } from 'antd';
import { InfoCircleOutlined } from '@ant-design/icons';
import {
  CONTROL_008_LENGTH,
  CONTROL_008_POSITIONS,
  fromDisplay,
  isBookMaterial,
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
  const books = isBookMaterial(leader);
  const visible = CONTROL_008_POSITIONS.filter((entry) => !entry.booksOnly || books);

  const update = (start: number, length: number, next: string) =>
    onChange(setFieldRange(padded, start, length, fromDisplay(next), CONTROL_008_LENGTH));

  return (
    <div>
      {!books && (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message="Đầu biểu đang khai đây không phải tài liệu chữ in"
          description={
            'Vị trí 18–34 mang ý nghĩa khác theo từng loại hình tài liệu, nên trình hướng dẫn '
            + 'không đoán thay. Sửa các vị trí ấy ở ô chuỗi đầy đủ bên dưới.'
          }
        />
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
