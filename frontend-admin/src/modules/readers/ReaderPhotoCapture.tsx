import { useCallback, useEffect, useRef, useState } from 'react';
import { App, Alert, Button, Modal, Slider, Space, Upload } from 'antd';
import { CameraOutlined, UploadOutlined } from '@ant-design/icons';
import type { UploadProps } from 'antd';
import { readersApi } from './api';

interface ReaderPhotoCaptureProps {
  readerId: string;
  open: boolean;
  onClose: () => void;
  onSaved: () => void;
}

/** Khung cắt theo tỷ lệ ảnh thẻ 3×4, đơn vị điểm ảnh trên màn hình. */
const FRAME_WIDTH = 240;
const FRAME_HEIGHT = 320;

/** Ảnh lưu xuống: 480×640, đủ nét để in lên thẻ nhựa mà tệp vẫn nhỏ. */
const OUTPUT_WIDTH = 480;
const OUTPUT_HEIGHT = 640;

/**
 * Lấy ảnh chân dung bạn đọc: chọn tệp hoặc chụp từ webcam, cắt theo khung 3×4 rồi tải lên (VI.1).
 *
 * Cắt ngay tại đây chứ không cắt ở máy chủ vì người đứng chụp mới biết cần lấy phần nào của khung
 * hình; máy chủ cắt giữa ảnh thì mười tấm có đến vài tấm mất nửa cái đầu.
 */
export function ReaderPhotoCapture({ readerId, open, onClose, onSaved }: ReaderPhotoCaptureProps) {
  const { message } = App.useApp();

  const [image, setImage] = useState<HTMLImageElement | null>(null);
  const [zoom, setZoom] = useState(1);
  const [offset, setOffset] = useState({ x: 0, y: 0 });
  const [saving, setSaving] = useState(false);
  const [camera, setCamera] = useState(false);
  const [cameraError, setCameraError] = useState<string | null>(null);

  const videoRef = useRef<HTMLVideoElement | null>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const dragRef = useRef<{ x: number; y: number } | null>(null);

  const stopCamera = useCallback(() => {
    streamRef.current?.getTracks().forEach((track) => track.stop());
    streamRef.current = null;
    setCamera(false);
  }, []);

  // Tắt webcam khi đóng hộp thoại: để đèn camera sáng sau khi cán bộ đã đóng màn hình là điều không
  // ai chấp nhận được.
  useEffect(() => {
    if (!open) {
      stopCamera();
      setImage(null);
      setZoom(1);
      setOffset({ x: 0, y: 0 });
      setCameraError(null);
    }

    return () => stopCamera();
  }, [open, stopCamera]);

  const loadFromBlob = useCallback((blob: Blob) => {
    const url = URL.createObjectURL(blob);
    const element = new Image();

    element.onload = () => {
      setImage(element);

      // Thu ảnh vừa khít khung cắt, rồi cán bộ tự phóng to phần cần lấy.
      const scale = Math.max(FRAME_WIDTH / element.width, FRAME_HEIGHT / element.height);
      setZoom(scale);
      setOffset({ x: 0, y: 0 });

      URL.revokeObjectURL(url);
    };

    element.src = url;
  }, []);

  const startCamera = useCallback(async () => {
    setCameraError(null);

    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { width: 640, height: 480, facingMode: 'user' },
      });

      streamRef.current = stream;
      setCamera(true);
      setImage(null);

      // Thẻ video chỉ tồn tại sau khi state đổi, nên gán nguồn ở nhịp kế tiếp.
      window.setTimeout(() => {
        if (videoRef.current) {
          videoRef.current.srcObject = stream;
          void videoRef.current.play();
        }
      }, 0);
    } catch {
      setCameraError(
        'Không mở được webcam. Kiểm tra quyền truy cập camera của trình duyệt, hoặc chọn ảnh từ tệp.',
      );
    }
  }, []);

  const capture = useCallback(() => {
    const video = videoRef.current;

    if (!video) return;

    const canvas = document.createElement('canvas');
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;

    canvas.getContext('2d')?.drawImage(video, 0, 0);

    canvas.toBlob((blob) => {
      if (blob) {
        loadFromBlob(blob);
        stopCamera();
      }
    }, 'image/jpeg', 0.92);
  }, [loadFromBlob, stopCamera]);

  const uploadProps: UploadProps = {
    accept: 'image/png,image/jpeg',
    showUploadList: false,
    beforeUpload: (file) => {
      stopCamera();
      loadFromBlob(file);
      return false;
    },
  };

  const save = useCallback(async () => {
    if (!image) return;

    const canvas = document.createElement('canvas');
    canvas.width = OUTPUT_WIDTH;
    canvas.height = OUTPUT_HEIGHT;

    const context = canvas.getContext('2d');

    if (!context) {
      message.error('Trình duyệt không hỗ trợ cắt ảnh.');
      return;
    }

    context.fillStyle = '#FFFFFF';
    context.fillRect(0, 0, OUTPUT_WIDTH, OUTPUT_HEIGHT);

    // Khung xem trước và ảnh lưu xuống cùng tỷ lệ, nên chỉ cần nhân theo một hệ số duy nhất.
    const ratio = OUTPUT_WIDTH / FRAME_WIDTH;
    const drawWidth = image.width * zoom * ratio;
    const drawHeight = image.height * zoom * ratio;
    const drawX = (OUTPUT_WIDTH - drawWidth) / 2 + offset.x * ratio;
    const drawY = (OUTPUT_HEIGHT - drawHeight) / 2 + offset.y * ratio;

    context.drawImage(image, drawX, drawY, drawWidth, drawHeight);

    const blob = await new Promise<Blob | null>((resolve) =>
      canvas.toBlob((result) => resolve(result), 'image/jpeg', 0.9),
    );

    if (!blob) {
      message.error('Không tạo được tệp ảnh.');
      return;
    }

    setSaving(true);

    try {
      await readersApi.uploadPhoto(readerId, blob, 'anh-ban-doc.jpg');
      message.success('Đã cập nhật ảnh bạn đọc.');
      onSaved();
      onClose();
    } catch (error) {
      message.error(error instanceof Error ? error.message : 'Không tải được ảnh lên.');
    } finally {
      setSaving(false);
    }
  }, [image, message, offset, onClose, onSaved, readerId, zoom]);

  return (
    <Modal
      open={open}
      onCancel={onClose}
      title="Ảnh bạn đọc"
      width={420}
      okText="Lưu ảnh"
      cancelText="Hủy"
      onOk={save}
      okButtonProps={{ disabled: !image, loading: saving }}
    >
      <Space direction="vertical" size={12} style={{ width: '100%' }}>
        <Space>
          <Upload {...uploadProps}>
            <Button icon={<UploadOutlined />}>Chọn tệp ảnh</Button>
          </Upload>
          <Button icon={<CameraOutlined />} onClick={() => void startCamera()}>
            Chụp từ webcam
          </Button>
        </Space>

        {cameraError && <Alert type="warning" showIcon message={cameraError} />}

        {camera && (
          <Space direction="vertical" align="center" style={{ width: '100%' }}>
            <video
              ref={videoRef}
              width={FRAME_WIDTH}
              height={FRAME_HEIGHT}
              style={{ objectFit: 'cover', background: '#000', borderRadius: 4 }}
              muted
              playsInline
            />
            <Button type="primary" icon={<CameraOutlined />} onClick={capture}>
              Chụp
            </Button>
          </Space>
        )}

        {image && (
          <Space direction="vertical" align="center" style={{ width: '100%' }}>
            <div
              role="presentation"
              style={{
                width: FRAME_WIDTH,
                height: FRAME_HEIGHT,
                overflow: 'hidden',
                position: 'relative',
                border: '1px solid #d9d9d9',
                borderRadius: 4,
                cursor: 'move',
                background: '#fafafa',
              }}
              onPointerDown={(event) => {
                dragRef.current = { x: event.clientX - offset.x, y: event.clientY - offset.y };
                event.currentTarget.setPointerCapture(event.pointerId);
              }}
              onPointerMove={(event) => {
                if (!dragRef.current) return;
                setOffset({
                  x: event.clientX - dragRef.current.x,
                  y: event.clientY - dragRef.current.y,
                });
              }}
              onPointerUp={() => {
                dragRef.current = null;
              }}
            >
              <img
                src={image.src}
                alt="Ảnh bạn đọc"
                draggable={false}
                style={{
                  position: 'absolute',
                  left: '50%',
                  top: '50%',
                  width: image.width * zoom,
                  height: image.height * zoom,
                  transform: `translate(-50%, -50%) translate(${offset.x}px, ${offset.y}px)`,
                  userSelect: 'none',
                }}
              />
            </div>

            <Space style={{ width: FRAME_WIDTH }}>
              <span>Phóng to</span>
              <Slider
                min={0.2}
                max={3}
                step={0.02}
                value={zoom}
                onChange={setZoom}
                style={{ width: 160 }}
              />
            </Space>

            <span style={{ color: '#8c8c8c', fontSize: 12 }}>
              Kéo ảnh để chỉnh khuôn mặt vào giữa khung 3×4.
            </span>
          </Space>
        )}
      </Space>
    </Modal>
  );
}
