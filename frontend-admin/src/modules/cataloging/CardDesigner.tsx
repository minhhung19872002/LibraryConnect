import { useCallback, useRef, useState } from 'react';
import { Button, Checkbox, Col, InputNumber, Row, Select, Space, Typography } from 'antd';
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons';
import { CARD_SOURCES, newBox, type CardBox, type CardLayout } from './cardTypes';

/** Số điểm ảnh trên một milimét khi vẽ phích trên màn hình. */
const SCALE = 3.2;

interface CardDesignerProps {
  widthMm: number;
  heightMm: number;
  layout: CardLayout;
  onChange: (layout: CardLayout) => void;
}

/**
 * Trình thiết kế mẫu phích: kéo thả các ô nội dung lên khổ phích (II.10).
 *
 * The canvas is the card at a fixed scale, so what the designer sees is the proportions of the real
 * thing — a box that overflows on screen overflows on paper. Positions are kept in millimetres, not
 * pixels, because that is what the printer and the guillotine work in; the scale exists only so the
 * card is large enough to work with on a screen.
 */
export function CardDesigner({ widthMm, heightMm, layout, onChange }: CardDesignerProps) {
  const canvasRef = useRef<HTMLDivElement>(null);
  const [selected, setSelected] = useState<number | null>(null);
  const dragState = useRef<{ index: number; offsetX: number; offsetY: number } | null>(null);

  // Bọc lại để tham chiếu ổn định: hàm kéo thả bên dưới phụ thuộc vào nó, mà một hàm dựng mới mỗi
  // lần vẽ sẽ khiến bộ xử lý kéo bị gắn lại giữa chừng thao tác.
  const update = useCallback(
    (index: number, change: Partial<CardBox>) => {
      onChange({
        ...layout,
        boxes: layout.boxes.map((box, position) =>
          position === index ? { ...box, ...change } : box,
        ),
      });
    },
    [layout, onChange],
  );

  const onMouseDown = (event: React.MouseEvent, index: number) => {
    event.preventDefault();
    setSelected(index);

    const box = layout.boxes[index]!;
    const canvas = canvasRef.current!.getBoundingClientRect();

    dragState.current = {
      index,
      offsetX: event.clientX - canvas.left - box.x * SCALE,
      offsetY: event.clientY - canvas.top - box.y * SCALE,
    };
  };

  const onMouseMove = useCallback(
    (event: React.MouseEvent) => {
      const drag = dragState.current;

      if (!drag || !canvasRef.current) {
        return;
      }

      const canvas = canvasRef.current.getBoundingClientRect();
      const box = layout.boxes[drag.index]!;

      // Clamped to the card: a box dragged off the edge would silently print half-missing.
      const x = Math.max(0, Math.min(widthMm - box.width, (event.clientX - canvas.left - drag.offsetX) / SCALE));
      const y = Math.max(0, Math.min(heightMm - box.height, (event.clientY - canvas.top - drag.offsetY) / SCALE));

      update(drag.index, { x: Math.round(x * 10) / 10, y: Math.round(y * 10) / 10 });
    },
    [layout, widthMm, heightMm, update],
  );

  const onMouseUp = () => {
    dragState.current = null;
  };

  const box = selected === null ? null : layout.boxes[selected];

  return (
    <Row gutter={16}>
      <Col xs={24} lg={14}>
        <Space direction="vertical" size={8} style={{ width: '100%' }}>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Kéo các ô để đặt vị trí. Khổ phích {widthMm} × {heightMm} mm.
          </Typography.Text>

          <div
            ref={canvasRef}
            onMouseMove={onMouseMove}
            onMouseUp={onMouseUp}
            onMouseLeave={onMouseUp}
            style={{
              position: 'relative',
              width: widthMm * SCALE,
              height: heightMm * SCALE,
              border: '1px solid #d9d9d9',
              background: '#fff',
              userSelect: 'none',
            }}
          >
            {/* The inner rectangle shows the printable area once the card's padding is taken off. */}
            <div
              style={{
                position: 'absolute',
                left: layout.padding * SCALE,
                top: layout.padding * SCALE,
                width: (widthMm - 2 * layout.padding) * SCALE,
                height: (heightMm - 2 * layout.padding) * SCALE,
                border: '1px dashed #e0e0e0',
                pointerEvents: 'none',
              }}
            />

            {layout.boxes.map((item, index) => (
              <div
                key={index}
                onMouseDown={(event) => onMouseDown(event, index)}
                style={{
                  position: 'absolute',
                  left: (layout.padding + item.x) * SCALE,
                  top: (layout.padding + item.y) * SCALE,
                  width: item.width * SCALE,
                  height: item.height * SCALE,
                  border: index === selected ? '2px solid #1668dc' : '1px dashed #bfbfbf',
                  background: index === selected ? 'rgba(22,104,220,0.06)' : 'rgba(0,0,0,0.02)',
                  cursor: 'move',
                  overflow: 'hidden',
                  fontSize: Math.max(7, item.fontSize),
                  fontWeight: item.bold ? 600 : 400,
                  fontStyle: item.italic ? 'italic' : 'normal',
                  textAlign: item.align,
                  padding: 1,
                  lineHeight: 1.2,
                }}
              >
                {item.prefix}
                {sampleFor(item.source)}
              </div>
            ))}
          </div>

          <Space>
            <Button
              size="small"
              icon={<PlusOutlined />}
              onClick={() => {
                onChange({ ...layout, boxes: [...layout.boxes, newBox()] });
                setSelected(layout.boxes.length);
              }}
            >
              Thêm ô
            </Button>

            <Space size={4}>
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                Lề trong (mm)
              </Typography.Text>
              <InputNumber
                size="small"
                min={0}
                max={20}
                value={layout.padding}
                onChange={(value) => onChange({ ...layout, padding: value ?? 0 })}
                style={{ width: 70 }}
              />
            </Space>

            <Checkbox
              checked={layout.showBorder}
              onChange={(event) => onChange({ ...layout, showBorder: event.target.checked })}
            >
              Vẽ viền phích
            </Checkbox>
          </Space>
        </Space>
      </Col>

      <Col xs={24} lg={10}>
        {box && selected !== null ? (
          <Space direction="vertical" size={10} style={{ width: '100%' }}>
            <Typography.Text strong>Ô đang chọn</Typography.Text>

            <div>
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                Nội dung
              </Typography.Text>
              <Select
                value={box.source}
                onChange={(value) => update(selected, { source: value })}
                options={Object.entries(
                  CARD_SOURCES.reduce<Record<string, typeof CARD_SOURCES>>((groups, item) => {
                    groups[item.group] = [...(groups[item.group] ?? []), item];
                    return groups;
                  }, {}),
                ).map(([group, items]) => ({
                  label: group,
                  options: items.map((item) => ({ value: item.value, label: item.label })),
                }))}
                style={{ width: '100%' }}
                showSearch
                optionFilterProp="label"
              />
            </div>

            <Row gutter={8}>
              <Col span={6}>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  X (mm)
                </Typography.Text>
                <InputNumber
                  size="small"
                  min={0}
                  max={widthMm}
                  value={box.x}
                  onChange={(value) => update(selected, { x: value ?? 0 })}
                  style={{ width: '100%' }}
                />
              </Col>
              <Col span={6}>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  Y (mm)
                </Typography.Text>
                <InputNumber
                  size="small"
                  min={0}
                  max={heightMm}
                  value={box.y}
                  onChange={(value) => update(selected, { y: value ?? 0 })}
                  style={{ width: '100%' }}
                />
              </Col>
              <Col span={6}>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  Rộng
                </Typography.Text>
                <InputNumber
                  size="small"
                  min={5}
                  max={widthMm}
                  value={box.width}
                  onChange={(value) => update(selected, { width: value ?? 5 })}
                  style={{ width: '100%' }}
                />
              </Col>
              <Col span={6}>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  Cao
                </Typography.Text>
                <InputNumber
                  size="small"
                  min={3}
                  max={heightMm}
                  value={box.height}
                  onChange={(value) => update(selected, { height: value ?? 3 })}
                  style={{ width: '100%' }}
                />
              </Col>
            </Row>

            <Row gutter={8}>
              <Col span={8}>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  Cỡ chữ
                </Typography.Text>
                <InputNumber
                  size="small"
                  min={5}
                  max={24}
                  value={box.fontSize}
                  onChange={(value) => update(selected, { fontSize: value ?? 9 })}
                  style={{ width: '100%' }}
                />
              </Col>
              <Col span={16}>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  Căn lề
                </Typography.Text>
                <Select
                  size="small"
                  value={box.align}
                  onChange={(value) => update(selected, { align: value })}
                  options={[
                    { value: 'left', label: 'Trái' },
                    { value: 'center', label: 'Giữa' },
                    { value: 'right', label: 'Phải' },
                  ]}
                  style={{ width: '100%' }}
                />
              </Col>
            </Row>

            <div>
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                Nhãn in trước nội dung
              </Typography.Text>
              <input
                className="ant-input ant-input-sm"
                value={box.prefix ?? ''}
                onChange={(event) => update(selected, { prefix: event.target.value })}
                placeholder="Ví dụ: ĐKCB: "
                style={{ width: '100%' }}
              />
            </div>

            <Space size={16}>
              <Checkbox checked={box.bold} onChange={(event) => update(selected, { bold: event.target.checked })}>
                Đậm
              </Checkbox>
              <Checkbox checked={box.italic} onChange={(event) => update(selected, { italic: event.target.checked })}>
                Nghiêng
              </Checkbox>
              <Checkbox checked={box.border} onChange={(event) => update(selected, { border: event.target.checked })}>
                Viền ô
              </Checkbox>
            </Space>

            <Button
              danger
              size="small"
              icon={<DeleteOutlined />}
              onClick={() => {
                onChange({ ...layout, boxes: layout.boxes.filter((_, index) => index !== selected) });
                setSelected(null);
              }}
            >
              Xóa ô
            </Button>
          </Space>
        ) : (
          <Typography.Text type="secondary">Bấm vào một ô trên phích để sửa nội dung và vị trí.</Typography.Text>
        )}
      </Col>
    </Row>
  );
}

/** Nội dung mẫu hiển thị trong ô lúc thiết kế, để cán bộ hình dung được phích in ra sẽ thế nào. */
function sampleFor(source: string): string {
  if (source.startsWith('"')) {
    return source.replace(/"/g, '');
  }

  const samples: Record<string, string> = {
    heading: 'Nguyễn Văn Ánh',
    isbd: 'Giáo trình cơ sở dữ liệu / Nguyễn Văn Ánh. — Hà Nội : Nxb ĐHQG, 2023. — 356 tr. ; 24 cm',
    callNumber: '005.74 NGU',
    tracings: 'I. Cơ sở dữ liệu. 1. Trần Thị Bưởi. 2. Giáo trình cơ sở dữ liệu.',
    title: 'Giáo trình cơ sở dữ liệu',
    author: 'Nguyễn Văn Ánh',
    publication: 'Hà Nội : Nxb ĐHQG, 2023',
    physical: '356 tr.',
    isbn: '9786040123456',
    ddc: '005.74',
    abstract: 'Trình bày mô hình quan hệ và ngôn ngữ SQL.',
    controlNumber: 'LC00000001',
  };

  return samples[source] ?? source;
}
