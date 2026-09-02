import { useEffect, useRef, useState } from 'react';
import { App, Button, Input, Modal, Space, Tooltip, Upload } from 'antd';
import {
  BoldOutlined,
  CodeOutlined,
  ItalicOutlined,
  LinkOutlined,
  OrderedListOutlined,
  PictureOutlined,
  RedoOutlined,
  TableOutlined,
  UnderlineOutlined,
  UndoOutlined,
  UnorderedListOutlined,
  VideoCameraOutlined,
} from '@ant-design/icons';
import { cmsApi } from './api';
import { toEmbedUrl } from './embedUrl';
import { MAU } from '@/lib/palette';

/**
 * Trình soạn thảo nội dung cho trang tĩnh và bản tin (VIII.1, VIII.2).
 *
 * Viết bằng vùng soạn thảo sẵn có của trình duyệt thay vì kéo thêm một thư viện soạn thảo: phần
 * việc ở đây là chữ đậm, tiêu đề, danh sách, bảng, ảnh và khung video nhúng — vừa đúng những gì
 * trình duyệt đã làm được, mà lại không thêm vài trăm KB vào gói tải về của cán bộ.
 *
 * Ảnh chèn vào bài được tải lên kho đối tượng ngay lúc chọn, nên nội dung lưu xuống chỉ chứa đường
 * dẫn — không nhét ảnh dạng chuỗi vào giữa bài, thứ làm bài viết phình lên vài MB.
 */
export function RichTextEditor({
  value,
  onChange,
  folder = 'page',
  minHeight = 360,
}: {
  value?: string;
  onChange?: (html: string) => void;
  folder?: string;
  minHeight?: number;
}) {
  const editorRef = useRef<HTMLDivElement>(null);
  const { message } = App.useApp();
  const [showSource, setShowSource] = useState(false);
  const [linkOpen, setLinkOpen] = useState(false);
  const [linkUrl, setLinkUrl] = useState('');
  const [videoOpen, setVideoOpen] = useState(false);
  const [videoUrl, setVideoUrl] = useState('');

  // Chỉ đổ nội dung vào vùng soạn thảo khi nó khác thứ đang hiển thị: gán lại mỗi lần gõ sẽ đẩy
  // con trỏ về đầu bài sau từng ký tự.
  useEffect(() => {
    const editor = editorRef.current;
    if (editor && editor.innerHTML !== (value ?? '')) {
      editor.innerHTML = value ?? '';
    }
  }, [value]);

  const emit = () => onChange?.(editorRef.current?.innerHTML ?? '');

  const run = (command: string, argument?: string) => {
    editorRef.current?.focus();
    document.execCommand(command, false, argument);
    emit();
  };

  const insertHtml = (html: string) => {
    editorRef.current?.focus();
    document.execCommand('insertHTML', false, html);
    emit();
  };

  const upload = async (file: File) => {
    try {
      const media = await cmsApi.uploadMedia(file, folder);
      insertHtml(`<img src="${media.url}" alt="${file.name}" />`);
      message.success('Đã chèn ảnh vào bài.');
    } catch (error) {
      message.error((error as Error).message);
    }

    return false;
  };

  const insertTable = () => {
    const rows = 3;
    const columns = 3;
    const header = Array.from({ length: columns }, (_, index) => `<th>Cột ${index + 1}</th>`).join('');
    const body = Array.from({ length: rows }, () => `<tr>${'<td> </td>'.repeat(columns)}</tr>`).join('');

    insertHtml(`<table><thead><tr>${header}</tr></thead><tbody>${body}</tbody></table><p></p>`);
  };

  const insertVideo = () => {
    const embed = toEmbedUrl(videoUrl.trim());

    if (!embed) {
      message.error('Chỉ nhúng được video từ YouTube hoặc Vimeo.');
      return;
    }

    insertHtml(
      `<iframe src="${embed}" width="640" height="360" frameborder="0" allowfullscreen></iframe><p></p>`,
    );

    setVideoOpen(false);
    setVideoUrl('');
  };

  return (
    <div style={{ border: `1px solid ${MAU.vien}`, borderRadius: 8, overflow: 'hidden' }}>
      <Space
        size={4}
        wrap
        style={{ padding: 8, borderBottom: `1px solid ${MAU.vien}`, background: MAU.nenDam }}
      >
        <Tooltip title="Đậm">
          <Button size="small" icon={<BoldOutlined />} onClick={() => run('bold')} />
        </Tooltip>
        <Tooltip title="Nghiêng">
          <Button size="small" icon={<ItalicOutlined />} onClick={() => run('italic')} />
        </Tooltip>
        <Tooltip title="Gạch chân">
          <Button size="small" icon={<UnderlineOutlined />} onClick={() => run('underline')} />
        </Tooltip>

        <Button size="small" onClick={() => run('formatBlock', '<h2>')}>
          Tiêu đề lớn
        </Button>
        <Button size="small" onClick={() => run('formatBlock', '<h3>')}>
          Tiêu đề nhỏ
        </Button>
        <Button size="small" onClick={() => run('formatBlock', '<p>')}>
          Đoạn văn
        </Button>

        <Tooltip title="Danh sách gạch đầu dòng">
          <Button
            size="small"
            icon={<UnorderedListOutlined />}
            onClick={() => run('insertUnorderedList')}
          />
        </Tooltip>
        <Tooltip title="Danh sách đánh số">
          <Button
            size="small"
            icon={<OrderedListOutlined />}
            onClick={() => run('insertOrderedList')}
          />
        </Tooltip>

        <Tooltip title="Chèn liên kết">
          <Button size="small" icon={<LinkOutlined />} onClick={() => setLinkOpen(true)} />
        </Tooltip>

        <Upload accept="image/*" showUploadList={false} beforeUpload={upload}>
          <Tooltip title="Chèn ảnh">
            <Button size="small" icon={<PictureOutlined />} />
          </Tooltip>
        </Upload>

        <Tooltip title="Chèn bảng 3×3">
          <Button size="small" icon={<TableOutlined />} onClick={insertTable} />
        </Tooltip>

        <Tooltip title="Nhúng video">
          <Button size="small" icon={<VideoCameraOutlined />} onClick={() => setVideoOpen(true)} />
        </Tooltip>

        <Tooltip title="Hoàn tác">
          <Button size="small" icon={<UndoOutlined />} onClick={() => run('undo')} />
        </Tooltip>
        <Tooltip title="Làm lại">
          <Button size="small" icon={<RedoOutlined />} onClick={() => run('redo')} />
        </Tooltip>

        <Tooltip title="Xem mã HTML">
          <Button
            size="small"
            icon={<CodeOutlined />}
            type={showSource ? 'primary' : 'default'}
            onClick={() => setShowSource((current) => !current)}
          />
        </Tooltip>
      </Space>

      {showSource ? (
        <Input.TextArea
          value={value ?? ''}
          onChange={(event) => onChange?.(event.target.value)}
          autoSize={{ minRows: 12, maxRows: 30 }}
          style={{ border: 'none', borderRadius: 0, fontFamily: 'monospace', fontSize: 13 }}
        />
      ) : (
        <div
          ref={editorRef}
          contentEditable
          suppressContentEditableWarning
          onInput={emit}
          onBlur={emit}
          className="lc-editor"
          style={{ minHeight, padding: 16, outline: 'none', lineHeight: 1.7 }}
        />
      )}

      <Modal
        open={linkOpen}
        title="Chèn liên kết"
        onCancel={() => setLinkOpen(false)}
        onOk={() => {
          if (linkUrl.trim()) {
            run('createLink', linkUrl.trim());
          }
          setLinkOpen(false);
          setLinkUrl('');
        }}
        okText="Chèn"
        cancelText="Hủy"
      >
        <p>Bôi đen đoạn chữ trước khi chèn, rồi nhập địa chỉ:</p>
        <Input
          value={linkUrl}
          onChange={(event) => setLinkUrl(event.target.value)}
          placeholder="https://..."
        />
      </Modal>

      <Modal
        open={videoOpen}
        title="Nhúng video"
        onCancel={() => setVideoOpen(false)}
        onOk={insertVideo}
        okText="Nhúng"
        cancelText="Hủy"
      >
        <p>Dán địa chỉ video từ YouTube hoặc Vimeo:</p>
        <Input
          value={videoUrl}
          onChange={(event) => setVideoUrl(event.target.value)}
          placeholder="https://www.youtube.com/watch?v=..."
        />
      </Modal>
    </div>
  );
}
