import { useState } from 'react';
import { Alert, App, Button, Modal, Space, Table, Upload } from 'antd';
import type { UploadFile } from 'antd/es/upload/interface';
import { DownloadOutlined, UploadOutlined } from '@ant-design/icons';
import { useMutation } from '@tanstack/react-query';
import { api, http } from '@/api/client';
import { errorMessage } from '@/api/formErrors';
import { messages } from '@/i18n/messages';
import { downloadFile } from '@/modules/system/helpers';
import type { CatalogImportResult, CatalogMetadata } from './types';

/**
 * Nhập một danh mục từ Excel.
 *
 * The same three-step shape as the user import: download the template, check the file, then import.
 * A row whose code already exists updates that value, which makes export → edit → import the natural
 * way to correct a whole list at once.
 */
export function CatalogImportModal({
  catalog,
  metadata,
  onClose,
  onImported,
}: {
  catalog: string;
  metadata: CatalogMetadata;
  onClose: () => void;
  onImported: () => void | Promise<void>;
}) {
  const { message } = App.useApp();
  const [file, setFile] = useState<UploadFile | null>(null);
  const [checkResult, setCheckResult] = useState<CatalogImportResult | null>(null);
  const [imported, setImported] = useState<CatalogImportResult | null>(null);

  const upload = useMutation({
    mutationFn: async ({ dryRun }: { dryRun: boolean }) => {
      const form = new FormData();
      form.append('file', file!.originFileObj as Blob);

      const response = await http.post<{ data: CatalogImportResult }>(
        `/catalogs/${catalog}/import?dryRun=${dryRun}`,
        form,
        { headers: { 'Content-Type': 'multipart/form-data' } },
      );

      return response.data.data;
    },
    onSuccess: async (result, variables) => {
      if (variables.dryRun) {
        setCheckResult(result);
        message.info(
          `Kiểm tra xong: thêm mới ${result.createdRows}, cập nhật ${result.updatedRows}, lỗi ${result.errorRows}.`,
        );
      } else {
        setImported(result);
        message.success(`Đã nhập xong: thêm mới ${result.createdRows}, cập nhật ${result.updatedRows}.`);
        await onImported();
      }
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const downloadTemplate = async () => {
    try {
      const { blob, fileName } = await api.download(`/catalogs/${catalog}/import-template`);
      downloadFile(blob, fileName);
    } catch (error) {
      message.error(errorMessage(error));
    }
  };

  const result = imported ?? checkResult;
  const usableRows = (checkResult?.createdRows ?? 0) + (checkResult?.updatedRows ?? 0);

  return (
    <Modal
      open
      width={820}
      title={`Nhập danh mục ${metadata.pluralName} từ Excel`}
      onCancel={onClose}
      footer={
        <Space>
          <Button onClick={onClose}>{messages.actions.close}</Button>
          <Button
            disabled={!file || imported !== null}
            loading={upload.isPending && upload.variables?.dryRun === true}
            onClick={() => upload.mutate({ dryRun: true })}
          >
            Kiểm tra tệp
          </Button>
          <Button
            type="primary"
            disabled={!file || imported !== null || usableRows === 0}
            loading={upload.isPending && upload.variables?.dryRun === false}
            onClick={() => upload.mutate({ dryRun: false })}
          >
            Nhập dữ liệu
          </Button>
        </Space>
      }
    >
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <Alert
          type="info"
          showIcon
          message="Trình tự nhập"
          description={
            <>
              Tải tệp mẫu → điền dữ liệu → chọn tệp → <b>Kiểm tra tệp</b> để xem lỗi → <b>Nhập dữ liệu</b>.
              Bước kiểm tra không ghi bất kỳ bản ghi nào. Dòng có mã đã tồn tại sẽ cập nhật giá trị hiện có
              thay vì tạo thêm bản ghi trùng.
            </>
          }
        />

        <Space>
          <Button icon={<DownloadOutlined />} onClick={downloadTemplate}>
            Tải tệp mẫu
          </Button>

          <Upload
            accept=".xlsx,.xls"
            maxCount={1}
            beforeUpload={() => false}
            fileList={file ? [file] : []}
            onChange={({ fileList }) => {
              setFile(fileList[0] ?? null);
              setCheckResult(null);
              setImported(null);
            }}
          >
            <Button icon={<UploadOutlined />}>Chọn tệp Excel</Button>
          </Upload>
        </Space>

        {result && (
          <Alert
            type={result.errorRows > 0 ? 'warning' : 'success'}
            showIcon
            message={
              `Tổng ${result.totalRows} dòng · thêm mới ${result.createdRows} · ` +
              `cập nhật ${result.updatedRows} · lỗi ${result.errorRows}`
            }
          />
        )}

        {result && result.errors.length > 0 && (
          <Table
            size="small"
            rowKey={(row) => `${row.row}-${row.column}-${row.message}`}
            dataSource={result.errors}
            pagination={{ pageSize: 8 }}
            columns={[
              {
                title: 'Dòng',
                dataIndex: 'row',
                width: 70,
                render: (row: number) => (row > 0 ? row : '—'),
              },
              { title: 'Cột', dataIndex: 'column', width: 170 },
              { title: 'Giá trị', dataIndex: 'value', width: 160, ellipsis: true },
              { title: 'Lỗi', dataIndex: 'message' },
            ]}
          />
        )}
      </Space>
    </Modal>
  );
}
