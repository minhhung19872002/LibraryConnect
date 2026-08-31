import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Checkbox,
  Drawer,
  Form,
  Input,
  Progress,
  Select,
  Space,
  Typography,
  Upload,
} from 'antd';
import { InboxOutlined } from '@ant-design/icons';
import { ApiRequestError } from '@/api/client';
import { digitalApi } from './api';
import { accessLevelHints, accessLevelLabels, formatSize } from './labels';
import type { DigitalAccessLevel, DigitalCollectionDto } from './types';

interface Props {
  open: boolean;
  collections: DigitalCollectionDto[];
  defaultCollectionId?: string;
  onClose: () => void;
  onUploaded: () => void;
}

/** Tệp lớn hơn mức này thì cắt thành mảnh; dưới mức này gửi một lần cho nhanh. */
const CHUNK_THRESHOLD = 16 * 1024 * 1024;

interface FileProgress {
  name: string;
  percent: number;
  status: 'chờ' | 'đang tải' | 'xong' | 'lỗi';
  message?: string;
}

/**
 * V.1 — Tải tài liệu số lên.
 *
 * Tệp lớn được cắt thành mảnh theo kích thước máy chủ khai báo. Mỗi mảnh gửi xong máy chủ trả về
 * danh sách mảnh đã nhận, nên nếu đường truyền đứt thì lần sau chỉ phải gửi phần còn thiếu. Mảnh
 * nào gửi lỗi thì thử lại đúng mảnh đó, không làm lại từ đầu.
 */
export function DigitalUploadDrawer({
  open,
  collections,
  defaultCollectionId,
  onClose,
  onUploaded,
}: Props) {
  const { message } = App.useApp();
  const [form] = Form.useForm();

  const [files, setFiles] = useState<File[]>([]);
  const [progress, setProgress] = useState<FileProgress[]>([]);
  const [busy, setBusy] = useState(false);

  const options = flatten(collections);

  const reset = () => {
    setFiles([]);
    setProgress([]);
    form.resetFields();
  };

  const update = (index: number, patch: Partial<FileProgress>) =>
    setProgress((current) => current.map((row, position) => (position === index ? { ...row, ...patch } : row)));

  const uploadOne = async (file: File, index: number, values: Record<string, unknown>) => {
    const fields = {
      title: files.length === 1 ? (values.title as string | undefined) : undefined,
      description: values.description as string | undefined,
      collectionId: values.collectionId as string | undefined,
      accessLevel: values.accessLevel as string | undefined,
      allowDownload: String(Boolean(values.allowDownload)),
      allowPrint: String(Boolean(values.allowPrint)),
    };

    update(index, { status: 'đang tải', percent: 1 });

    if (file.size <= CHUNK_THRESHOLD) {
      await digitalApi.upload(file, fields);
      update(index, { status: 'xong', percent: 100 });
      return;
    }

    const session = await digitalApi.startUpload({
      fileName: file.name,
      totalSize: file.size,
      title: fields.title,
      collectionId: fields.collectionId,
    });

    let missing = session.missingChunks;

    // Thử lại tối đa ba vòng: mỗi vòng chỉ gửi những mảnh máy chủ báo còn thiếu.
    for (let attempt = 0; attempt < 3 && missing.length > 0; attempt += 1) {
      for (const chunkIndex of missing) {
        const start = chunkIndex * session.chunkSize;
        const chunk = file.slice(start, Math.min(file.size, start + session.chunkSize));

        try {
          const state = await digitalApi.uploadChunk(session.id, chunkIndex, chunk);
          update(index, {
            percent: Math.round((state.receivedChunks.length / state.totalChunks) * 95),
          });
        } catch {
          // Bỏ qua ở đây; vòng sau sẽ hỏi lại máy chủ xem còn thiếu mảnh nào.
        }
      }

      missing = (await digitalApi.uploadSession(session.id)).missingChunks;
    }

    if (missing.length > 0) {
      throw new Error(`Còn ${missing.length} mảnh chưa gửi được, hãy thử lại.`);
    }

    await digitalApi.completeUpload(session.id, {
      title: fields.title,
      description: fields.description,
      collectionId: fields.collectionId,
      accessLevel: fields.accessLevel,
      allowDownload: Boolean(values.allowDownload),
      allowPrint: Boolean(values.allowPrint),
    });

    update(index, { status: 'xong', percent: 100 });
  };

  const submit = async () => {
    const values = await form.validateFields();

    if (files.length === 0) {
      message.warning('Chưa chọn tệp nào.');
      return;
    }

    setBusy(true);
    setProgress(files.map((file) => ({ name: file.name, percent: 0, status: 'chờ' })));

    let failed = 0;

    for (let index = 0; index < files.length; index += 1) {
      const current = files[index];

      if (!current) {
        continue;
      }

      try {
        await uploadOne(current, index, values);
      } catch (error) {
        failed += 1;
        update(index, {
          status: 'lỗi',
          message: error instanceof ApiRequestError || error instanceof Error
            ? error.message
            : 'Không tải lên được.',
        });
      }
    }

    setBusy(false);

    if (failed === 0) {
      message.success(`Đã tải lên ${files.length} tệp. Hệ thống đang xử lý ảnh bìa và nội dung.`);
      reset();
      onUploaded();
      onClose();
    } else {
      message.error(`${failed}/${files.length} tệp chưa tải lên được, xem chi tiết bên dưới.`);
      onUploaded();
    }
  };

  return (
    <Drawer
      open={open}
      onClose={() => {
        if (!busy) {
          reset();
          onClose();
        }
      }}
      width={640}
      title="Tải tài liệu số lên"
      maskClosable={!busy}
      extra={
        <Space>
          <Button onClick={onClose} disabled={busy}>
            Hủy
          </Button>
          <Button type="primary" loading={busy} onClick={() => void submit()}>
            Tải lên
          </Button>
        </Space>
      }
    >
      <Form
        form={form}
        layout="vertical"
        initialValues={{
          collectionId: defaultCollectionId,
          allowDownload: false,
          allowPrint: false,
        }}
      >
        <Form.Item label="Tệp">
          <Upload.Dragger
            multiple
            beforeUpload={(file) => {
              setFiles((current) => [...current, file]);
              return false;
            }}
            onRemove={(file) => setFiles((current) => current.filter((item) => item.name !== file.name))}
            fileList={files.map((file, index) => ({
              uid: String(index),
              name: `${file.name} — ${formatSize(file.size)}`,
              status: 'done' as const,
            }))}
          >
            <p className="ant-upload-drag-icon">
              <InboxOutlined />
            </p>
            <p className="ant-upload-text">Kéo tệp vào đây hoặc bấm để chọn</p>
            <p className="ant-upload-hint">
              PDF, DOCX, EPUB, MP4, MP3, ảnh. Tệp trên {formatSize(CHUNK_THRESHOLD)} được cắt thành
              nhiều mảnh, đứt mạng thì tải tiếp chứ không làm lại từ đầu.
            </p>
          </Upload.Dragger>
        </Form.Item>

        {files.length === 1 && (
          <Form.Item name="title" label="Nhan đề">
            <Input placeholder="Bỏ trống thì lấy theo tên tệp" />
          </Form.Item>
        )}

        <Form.Item name="description" label="Mô tả">
          <Input.TextArea rows={2} />
        </Form.Item>

        <Form.Item name="collectionId" label="Bộ sưu tập">
          <Select allowClear showSearch optionFilterProp="label" options={options} />
        </Form.Item>

        <Form.Item
          name="accessLevel"
          label="Mức truy cập"
          extra="Bỏ trống thì lấy theo mức mặc định của bộ sưu tập."
        >
          <Select
            allowClear
            options={(Object.keys(accessLevelLabels) as DigitalAccessLevel[]).map((value) => ({
              value,
              label: `${accessLevelLabels[value]} — ${accessLevelHints[value]}`,
            }))}
          />
        </Form.Item>

        <Space size={24}>
          <Form.Item name="allowDownload" valuePropName="checked" noStyle>
            <Checkbox>Cho phép tải về</Checkbox>
          </Form.Item>
          <Form.Item name="allowPrint" valuePropName="checked" noStyle>
            <Checkbox>Cho phép in</Checkbox>
          </Form.Item>
        </Space>
      </Form>

      {progress.length > 0 && (
        <Space direction="vertical" size={8} style={{ width: '100%', marginTop: 20 }}>
          {progress.map((row) => (
            <div key={row.name}>
              <Typography.Text>{row.name}</Typography.Text>
              <Progress
                percent={row.percent}
                status={row.status === 'lỗi' ? 'exception' : row.status === 'xong' ? 'success' : 'active'}
              />
              {row.message && <Alert type="error" showIcon message={row.message} />}
            </div>
          ))}
        </Space>
      )}
    </Drawer>
  );
}

/** Trải cây bộ sưu tập thành danh sách phẳng có thụt đầu dòng để chọn trong ô Select. */
function flatten(
  nodes: DigitalCollectionDto[],
  depth = 0,
): { value: string; label: string }[] {
  return nodes.flatMap((node) => [
    { value: node.id, label: `${'— '.repeat(depth)}${node.name}` },
    ...flatten(node.children, depth + 1),
  ]);
}
