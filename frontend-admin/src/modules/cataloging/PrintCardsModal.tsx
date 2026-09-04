import { useEffect, useState } from 'react';
import { App, Button, Checkbox, Input, Modal, Radio, Select, Space, Typography } from 'antd';
import { EyeOutlined } from '@ant-design/icons';
import { useMutation, useQuery } from '@tanstack/react-query';
import { errorMessage } from '@/api/formErrors';
import { saveBlob } from '@/modules/marc/api';
import { cardApi, type BibListParams } from './api';
import { CARD_TYPE_LABELS, type CardType } from './cardTypes';

/**
 * In phích thư mục (II.10) — dùng chung cho ba chỗ: màn hình mẫu phích (in theo từ khoá), danh
 * sách biểu ghi (in các dòng đã tick, hoặc theo bộ lọc đang xem) và chi tiết một biểu ghi.
 *
 * Trước khi xuất cả lượt có bước xem trước: máy chủ dựng vài biểu ghi đầu bằng đúng mẫu và dữ
 * liệu thật rồi trả PDF, hiện ngay trong hộp thoại. Nhìn thấy phích đã điền chữ mới biết ô nào
 * tràn, ô nào trống — bản vẽ mẫu trên màn hình thiết kế không nói được điều đó.
 */
export function PrintCardsModal({
  open,
  bibIds,
  filter,
  onClose,
}: {
  open: boolean;
  /** Các biểu ghi đã chọn; rỗng thì in theo `filter` (hoặc theo từ khoá gõ trong hộp thoại). */
  bibIds?: string[];
  /** Bộ lọc đang dùng trên màn hình danh sách, áp khi không tick biểu ghi nào. */
  filter?: BibListParams;
  onClose: () => void;
}) {
  const { message } = App.useApp();
  const [templateId, setTemplateId] = useState<string | undefined>();
  const [cardTypes, setCardTypes] = useState<CardType[]>(['MAIN']);
  const [multiplePerPage, setMultiplePerPage] = useState(true);
  const [keyword, setKeyword] = useState('');
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);

  const selected = bibIds ?? [];
  const bySelection = selected.length > 0;
  const byScreenFilter = !bySelection && filter !== undefined;

  const templates = useQuery({
    queryKey: ['card-templates'],
    queryFn: () => cardApi.templates(),
    enabled: open,
  });

  // A blob URL lives until revoked; the preview is replaced or the dialog closes, either way it goes.
  useEffect(
    () => () => {
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl);
      }
    },
    [previewUrl],
  );

  useEffect(() => {
    if (!open) {
      setPreviewUrl(null);
    }
  }, [open]);

  const request = (preview: boolean) =>
    cardApi.print({
      bibIds: selected,
      filter: bySelection
        ? undefined
        : byScreenFilter
          ? filter
          : { keyword: keyword.trim() || undefined },
      templateId,
      cardTypes,
      multiplePerPage,
      preview,
      previewRecords: preview ? 3 : undefined,
    });

  const print = useMutation({
    mutationFn: () => request(false),
    onSuccess: ({ blob, fileName }) => {
      saveBlob(blob, fileName);
      message.success(`Đã tạo tệp ${fileName}.`);
      onClose();
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const preview = useMutation({
    mutationFn: () => request(true),
    onSuccess: ({ blob }) => setPreviewUrl(URL.createObjectURL(blob)),
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const scope = bySelection
    ? `${selected.length} biểu ghi đã chọn`
    : byScreenFilter
      ? 'Theo bộ lọc đang dùng trên danh sách'
      : null;

  return (
    <Modal
      open={open}
      title="In phích thư mục"
      cancelText="Hủy"
      onCancel={onClose}
      width={previewUrl ? 960 : 560}
      footer={
        <Space>
          <Button onClick={onClose}>Hủy</Button>
          <Button
            icon={<EyeOutlined />}
            loading={preview.isPending}
            disabled={cardTypes.length === 0}
            onClick={() => preview.mutate()}
          >
            Xem trước
          </Button>
          <Button
            type="primary"
            loading={print.isPending}
            disabled={cardTypes.length === 0}
            onClick={() => print.mutate()}
          >
            Tạo tệp PDF
          </Button>
        </Space>
      }
    >
      <div style={{ display: 'flex', gap: 16, alignItems: 'flex-start' }}>
        <Space direction="vertical" size={14} style={{ width: 512, flex: 'none' }}>
          <div>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              Biểu ghi cần in
            </Typography.Text>
            {scope ? (
              <Typography.Paragraph style={{ marginBottom: 0 }}>{scope}</Typography.Paragraph>
            ) : (
              <Input
                value={keyword}
                onChange={(event) => setKeyword(event.target.value)}
                placeholder="Từ khóa lọc biểu ghi; bỏ trống để in toàn bộ"
              />
            )}
          </div>

          <div>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              Loại phích
            </Typography.Text>
            <Checkbox.Group
              value={cardTypes}
              onChange={(values) => setCardTypes(values as CardType[])}
              options={Object.entries(CARD_TYPE_LABELS).map(([value, label]) => ({ value, label }))}
              style={{ display: 'flex', flexDirection: 'column', gap: 4 }}
            />
            <Typography.Text type="secondary" style={{ fontSize: 11 }}>
              Một biểu ghi có ba đề mục chủ đề sẽ cho ba phích chủ đề.
            </Typography.Text>
          </div>

          <div>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              Mẫu phích
            </Typography.Text>
            <Select
              value={templateId}
              onChange={setTemplateId}
              loading={templates.isFetching}
              options={(templates.data ?? []).map((template) => ({
                value: template.id,
                label: `${template.name} (${template.widthMm}×${template.heightMm} mm)`,
              }))}
              placeholder="Dùng mẫu mặc định"
              allowClear
              style={{ width: '100%' }}
            />
          </div>

          <div>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              Cách xếp giấy
            </Typography.Text>
            <Radio.Group
              value={multiplePerPage}
              onChange={(event) => setMultiplePerPage(event.target.value)}
            >
              <Space direction="vertical">
                <Radio value={true}>
                  Nhiều phích trên một tờ A4
                  <Typography.Text type="secondary" style={{ fontSize: 11, display: 'block' }}>
                    In lên giấy A4 thường rồi cắt rời.
                  </Typography.Text>
                </Radio>
                <Radio value={false}>
                  Mỗi phích một trang đúng khổ
                  <Typography.Text type="secondary" style={{ fontSize: 11, display: 'block' }}>
                    In thẳng lên bìa phích in sẵn.
                  </Typography.Text>
                </Radio>
              </Space>
            </Radio.Group>
          </div>
        </Space>

        {previewUrl && (
          <div style={{ flex: 1, minWidth: 0 }}>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              Xem trước — ba biểu ghi đầu, dữ liệu thật trên mẫu đã chọn
            </Typography.Text>
            <iframe
              title="Xem trước phích"
              src={previewUrl}
              style={{ width: '100%', height: 480, border: 0, display: 'block' }}
            />
          </div>
        )}
      </div>
    </Modal>
  );
}
