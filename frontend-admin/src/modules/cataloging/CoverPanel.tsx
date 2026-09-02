import { useRef, useState } from 'react';
import { App, Button, Card, Space, Tag, Typography } from 'antd';
import { CloudDownloadOutlined, UploadOutlined } from '@ant-design/icons';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { errorMessage } from '@/api/formErrors';
import { catalogingApi } from './api';
import { COVER_SOURCE_LABELS } from './types';

/**
 * Ảnh bìa của một biểu ghi.
 *
 * Đo trên kho thật: 444 trên 7.675 biểu ghi có ISBN (5,8%). Không có ISBN thì không nguồn nào tra ra
 * ảnh bìa, mà luận văn, đề tài nghiên cứu và bài giảng điện tử — hơn hai phần ba kho — thì không bao
 * giờ có ảnh trên mạng. Nên ô này luôn hiện một bìa: ảnh thật nếu tra được, còn lại là bìa máy chủ
 * dựng từ nhan đề, tác giả, năm và dạng tài liệu.
 *
 * Ảnh cán bộ tự tải lên đứng trên mọi nguồn khác và không bao giờ bị lượt tra tự động ghi đè.
 */
export function CoverPanel({
  bibId,
  coverImageUrl,
  coverImageSource,
}: {
  bibId: string;
  coverImageUrl?: string | null;
  coverImageSource?: string | null;
}) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const inputRef = useRef<HTMLInputElement>(null);

  // Đổi khoá ảnh sau mỗi lần cập nhật để trình duyệt tải lại thay vì dùng bản trong bộ nhớ đệm —
  // ảnh bìa đặt bộ nhớ đệm cả tuần nên không ép thì cán bộ vừa tải lên vẫn thấy ảnh cũ.
  const [khoa, setKhoa] = useState(0);

  const tra = useMutation({
    mutationFn: () => catalogingApi.lookupCover(bibId),
    onSuccess: (ket) => {
      if (ket.found) {
        message.success(
          `Đã lấy ảnh bìa từ ${COVER_SOURCE_LABELS[ket.source ?? ''] ?? ket.source}.`,
        );
        setKhoa((value) => value + 1);
        void queryClient.invalidateQueries({ queryKey: ['bib', bibId] });
      } else {
        message.info(ket.reason ?? 'Không nguồn nào có ảnh bìa cho biểu ghi này.');
      }
    },
    onError: (error) => message.error(errorMessage(error)),
  });

  const tai = useMutation({
    mutationFn: (file: File) => catalogingApi.uploadCover(bibId, file),
    onSuccess: () => {
      message.success('Đã cập nhật ảnh bìa.');
      setKhoa((value) => value + 1);
      void queryClient.invalidateQueries({ queryKey: ['bib', bibId] });
    },
    onError: (error) => message.error(errorMessage(error)),
  });

  const src = `${catalogingApi.coverUrl(bibId)}?v=${khoa}`;

  return (
    <Card size="small" title="Ảnh bìa">
      <Space direction="vertical" size={12} style={{ width: '100%' }}>
        <img
          src={src}
          alt="Ảnh bìa của biểu ghi"
          style={{
            width: '100%',
            maxWidth: 200,
            aspectRatio: '2 / 3',
            objectFit: 'cover',
            borderRadius: 6,
            border: '1px solid ${MAU.vien}',
          }}
        />

        <Space size={4} wrap>
          {coverImageUrl ? (
            <Tag color="green">
              {COVER_SOURCE_LABELS[coverImageSource ?? ''] ?? 'Ảnh thật'}
            </Tag>
          ) : (
            <Tag>Bìa dựng sẵn</Tag>
          )}
        </Space>

        {!coverImageUrl && (
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Bìa dựng từ nhan đề, tác giả, năm và dạng tài liệu. Tài liệu không có ISBN thì không
            nguồn nào tra được ảnh thật.
          </Typography.Text>
        )}

        <Can permission={PERMISSIONS.cataloging.bibUpdate}>
          <Space wrap>
            <Button
              icon={<CloudDownloadOutlined />}
              loading={tra.isPending}
              onClick={() => tra.mutate()}
            >
              Tra ảnh bìa
            </Button>

            <Button
              icon={<UploadOutlined />}
              loading={tai.isPending}
              onClick={() => inputRef.current?.click()}
            >
              Tải ảnh lên
            </Button>

            <input
              ref={inputRef}
              type="file"
              accept="image/jpeg,image/png,image/gif,image/webp"
              style={{ display: 'none' }}
              onChange={(event) => {
                const file = event.target.files?.[0];

                if (file) {
                  tai.mutate(file);
                }

                event.target.value = '';
              }}
            />
          </Space>
        </Can>
      </Space>
    </Card>
  );
}
