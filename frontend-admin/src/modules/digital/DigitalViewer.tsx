import { useCallback, useEffect, useMemo, useState } from 'react';
import { Alert, Button, Empty, InputNumber, Modal, Space, Spin, Tag, Typography } from 'antd';
import {
  DownloadOutlined,
  LeftOutlined,
  RightOutlined,
  ZoomInOutlined,
  ZoomOutOutlined,
} from '@ant-design/icons';
import { http } from '@/api/client';
import { digitalApi } from './api';
import { describeReadable } from './labels';
import type { DigitalReaderSessionDto } from './types';

interface Props {
  documentId: string | null;
  onClose: () => void;
}

/**
 * V.1 — Trình đọc trực tuyến.
 *
 * Nội dung không đi xuống trình duyệt dưới dạng tệp: mỗi trang là một ảnh do máy chủ kết xuất và
 * đóng chữ chìm sẵn. Nhờ vậy tài liệu không cho tải thì trên máy bạn đọc không có tệp gốc nào để
 * lưu lại, và ảnh chụp màn hình vẫn mang dấu vết người xem.
 *
 * Ảnh nằm sau endpoint có kiểm tra quyền, mà thẻ img không gửi kèm mã thông báo, nên phải tải bằng
 * bộ gọi API rồi dựng địa chỉ tạm trong bộ nhớ — giống cách ảnh chân dung bạn đọc đang làm.
 */
export function DigitalViewer({ documentId, onClose }: Props) {
  const [session, setSession] = useState<DigitalReaderSessionDto | null>(null);
  const [page, setPage] = useState(1);
  const [zoom, setZoom] = useState(1);
  const [imageUrl, setImageUrl] = useState<string | undefined>(undefined);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!documentId) {
      setSession(null);
      setError(null);
      return;
    }

    setPage(1);
    setZoom(1);
    setError(null);

    let cancelled = false;

    digitalApi
      .openReader(documentId)
      .then((result) => {
        if (!cancelled) setSession(result);
      })
      .catch((reason: Error) => {
        if (!cancelled) {
          setSession(null);
          setError(reason.message);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [documentId]);

  // Số trang thực sự mở được: giới hạn xem thử do máy chủ trả về, không phải do màn hình tự đặt.
  const lastPage = useMemo(() => {
    if (!session) return 1;
    const total = session.pageCount ?? 1;
    // Máy chủ bỏ trường này khi cho đọc toàn văn, nên phải so lỏng để bắt cả undefined.
    return session.readablePages == null ? total : Math.min(total, session.readablePages);
  }, [session]);

  useEffect(() => {
    if (!documentId || !session || !session.pageCount) {
      setImageUrl(undefined);
      return;
    }

    let objectUrl: string | undefined;
    let cancelled = false;

    setLoading(true);

    http
      .get<Blob>(`/digital/documents/${documentId}/pages/${page}`, { responseType: 'blob' })
      .then((response) => {
        if (cancelled) return;

        objectUrl = URL.createObjectURL(response.data);
        setImageUrl(objectUrl);
        setError(null);
      })
      .catch((reason: Error) => {
        if (!cancelled) {
          setImageUrl(undefined);
          setError(reason.message);
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [documentId, session, page]);

  const move = useCallback(
    (delta: number) => setPage((current) => Math.min(lastPage, Math.max(1, current + delta))),
    [lastPage],
  );

  useEffect(() => {
    if (!documentId) return;

    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'ArrowRight' || event.key === 'PageDown') move(1);
      if (event.key === 'ArrowLeft' || event.key === 'PageUp') move(-1);
    };

    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [documentId, move]);

  const handleDownload = async () => {
    if (!documentId) return;

    const { blob, fileName } = await digitalApi.download(documentId);
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');

    anchor.href = url;
    anchor.download = fileName;
    anchor.click();

    URL.revokeObjectURL(url);
  };

  return (
    <Modal
      open={documentId !== null}
      onCancel={onClose}
      width={1000}
      footer={null}
      title={session?.title ?? 'Đọc tài liệu số'}
      styles={{ body: { background: '#f0f2f5', padding: 16 } }}
    >
      {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 12 }} />}

      {session && (
        <Space direction="vertical" size={12} style={{ width: '100%' }}>
          <Space wrap>
            <Tag color={session.readablePages == null ? 'green' : 'orange'}>
              {describeReadable(session.readablePages)}
            </Tag>
            {session.watermarkEnabled && <Tag color="red">Có chữ chìm</Tag>}
            <Typography.Text type="secondary">{session.reason}</Typography.Text>
          </Space>

          <Space wrap>
            <Button icon={<LeftOutlined />} disabled={page <= 1} onClick={() => move(-1)}>
              Trang trước
            </Button>
            <InputNumber
              min={1}
              max={lastPage}
              value={page}
              onChange={(value) => setPage(Math.min(lastPage, Math.max(1, Number(value) || 1)))}
              style={{ width: 90 }}
            />
            <Typography.Text>/ {session.pageCount ?? '?'} trang</Typography.Text>
            <Button icon={<RightOutlined />} disabled={page >= lastPage} onClick={() => move(1)}>
              Trang sau
            </Button>
            <Button
              icon={<ZoomOutOutlined />}
              onClick={() => setZoom((value) => Math.max(0.5, value - 0.2))}
            />
            <Button
              icon={<ZoomInOutlined />}
              onClick={() => setZoom((value) => Math.min(3, value + 0.2))}
            />
            {session.canDownload && (
              <Button icon={<DownloadOutlined />} onClick={() => void handleDownload()}>
                Tải bản gốc
              </Button>
            )}
          </Space>

          <div
            style={{
              maxHeight: '65vh',
              overflow: 'auto',
              textAlign: 'center',
              background: '#fff',
              padding: 12,
              borderRadius: 6,
            }}
          >
            {loading && <Spin style={{ margin: 40 }} />}

            {!loading && imageUrl && (
              <img
                src={imageUrl}
                alt={`Trang ${page}`}
                style={{ width: `${zoom * 100}%`, maxWidth: 'none' }}
                // Kéo thả ảnh ra ngoài cũng là một cách lấy nội dung, nên chặn luôn.
                onDragStart={(event) => event.preventDefault()}
              />
            )}

            {!loading && !imageUrl && (
              <Empty description="Định dạng này không đọc theo trang được. Hãy tải tệp về để xem." />
            )}
          </div>
        </Space>
      )}
    </Modal>
  );
}
