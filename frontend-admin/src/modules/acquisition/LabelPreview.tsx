import { useEffect, useState } from 'react';
import { Typography } from 'antd';
import { MAU } from '@/lib/palette';
import { stockApi } from './api';
import { resolveBarcodeValue, resolveLabelText, type LabelData } from './labelContent';
import type { LabelLayoutDto } from './types';

/** Điểm ảnh cho một milimét trên màn hình — đủ lớn để đọc được chữ cỡ 7pt. */
const PX_PER_MM = 5;

/**
 * Ảnh mô phỏng một tem theo đúng bố cục sẽ in (III.2: "xem trước").
 *
 * Mọi ô đặt theo milimét như trên mẫu; mã vạch là ảnh PNG thật từ máy chủ, logo là ảnh đã tải lên
 * ở tham số hệ thống. Không phải bản in — cỡ chữ điểm in đổi sang điểm ảnh gần đúng — nhưng đủ để
 * thấy ô nào đè lên ô nào trước khi tốn một tờ tem.
 */
export function LabelPreview({
  layout,
  widthMm,
  heightMm,
  data,
  barcodeType = 'Code128',
}: {
  layout: LabelLayoutDto;
  widthMm: number;
  heightMm: number;
  data: LabelData;
  barcodeType?: string;
}) {
  const barcodeValue = layout.barcode ? resolveBarcodeValue(data, layout.barcode.source) : '';
  const [barcodeUrl, setBarcodeUrl] = useState<string | null>(null);
  const [logoUrl, setLogoUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!layout.barcode || !barcodeValue) {
      setBarcodeUrl(null);
      return;
    }

    let url: string | null = null;
    let cancelled = false;

    stockApi
      .barcodeImage(barcodeValue, layout.barcode.type ?? barcodeType, 400, 120)
      .then(({ blob }) => {
        if (cancelled) return;
        url = URL.createObjectURL(blob);
        setBarcodeUrl(url);
      })
      .catch(() => setBarcodeUrl(null));

    return () => {
      cancelled = true;
      if (url) URL.revokeObjectURL(url);
    };
  }, [barcodeValue, barcodeType, layout.barcode]);

  useEffect(() => {
    if (!layout.logo) {
      setLogoUrl(null);
      return;
    }

    let url: string | null = null;
    let cancelled = false;

    stockApi
      .libraryLogo()
      .then(({ blob }) => {
        if (cancelled) return;
        url = URL.createObjectURL(blob);
        setLogoUrl(url);
      })
      // Chưa tải logo lên thì máy chủ trả 404: khối logo hiện ô trống có chữ, như lúc in.
      .catch(() => setLogoUrl(null));

    return () => {
      cancelled = true;
      if (url) URL.revokeObjectURL(url);
    };
  }, [layout.logo]);

  const mm = (value: number) => value * PX_PER_MM;
  const pad = mm(layout.padding);

  return (
    <div
      style={{
        position: 'relative',
        width: mm(widthMm),
        height: mm(heightMm),
        background: MAU.giay,
        border: `1px ${layout.showBorder ? 'solid' : 'dashed'} ${MAU.vienDam}`,
        overflow: 'hidden',
        boxSizing: 'content-box',
      }}
    >
      {layout.logo && (
        <div
          style={{
            position: 'absolute',
            left: pad + mm(layout.logo.x),
            top: pad + mm(layout.logo.y),
            width: mm(layout.logo.width),
            height: mm(layout.logo.height),
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            border: logoUrl ? 'none' : `1px dashed ${MAU.vien}`,
            overflow: 'hidden',
          }}
        >
          {logoUrl ? (
            <img src={logoUrl} alt="Logo thư viện" style={{ maxWidth: '100%', maxHeight: '100%' }} />
          ) : (
            <Typography.Text type="secondary" style={{ fontSize: 9 }}>
              Logo
            </Typography.Text>
          )}
        </div>
      )}

      {layout.barcode && (
        <div
          style={{
            position: 'absolute',
            left: pad + mm(layout.barcode.x),
            top: pad + mm(layout.barcode.y),
            width: mm(layout.barcode.width),
            textAlign: 'center',
          }}
        >
          <div
            style={{
              height: mm(layout.barcode.height),
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              border: barcodeUrl ? 'none' : `1px dashed ${MAU.vien}`,
            }}
          >
            {barcodeUrl ? (
              <img src={barcodeUrl} alt={barcodeValue} style={{ width: '100%', height: '100%' }} />
            ) : (
              <Typography.Text type="secondary" style={{ fontSize: 9 }}>
                {barcodeValue || 'Mã vạch'}
              </Typography.Text>
            )}
          </div>
          {layout.barcode.showText && (
            <div style={{ fontSize: layout.barcode.fontSize * 1.4, letterSpacing: 1, lineHeight: 1.2 }}>
              {barcodeValue}
            </div>
          )}
        </div>
      )}

      {layout.boxes.map((box, index) => {
        const text = resolveLabelText(data, box.source);

        if (!text) return null;

        return (
          <div
            key={index}
            style={{
              position: 'absolute',
              left: pad + mm(box.x),
              top: pad + mm(box.y),
              width: mm(box.width),
              height: mm(box.height),
              fontSize: box.fontSize * 1.4,
              fontWeight: box.bold ? 700 : 400,
              fontStyle: box.italic ? 'italic' : 'normal',
              textAlign: box.align,
              border: box.border ? `1px solid ${MAU.vien}` : 'none',
              overflow: 'hidden',
              whiteSpace: 'nowrap',
              textOverflow: 'ellipsis',
              lineHeight: 1.15,
              color: MAU.chu,
            }}
          >
            {(box.prefix ?? '') + text}
          </div>
        );
      })}
    </div>
  );
}
