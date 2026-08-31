import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Checkbox,
  Col,
  Empty,
  Progress,
  Row,
  Select,
  Space,
  Statistic,
  Table,
  Tag,
  Typography,
  Upload,
} from 'antd';
import {
  CheckCircleOutlined,
  CloudUploadOutlined,
  DownloadOutlined,
  FileExcelOutlined,
  PictureOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import type { UploadProps } from 'antd';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { saveBlob } from '@/modules/marc/api';
import { useCatalogOptions, toOptions } from '@/modules/cataloging/useCatalogOptions';
import { readersApi } from './api';
import { duplicateActionOptions, formatDateTime, importFieldLabels } from './labels';
import type {
  ReaderImportBatchDto,
  ReaderImportDuplicateAction,
  ReaderImportErrorDto,
  ReaderImportOptions,
  ReaderImportPreviewDto,
} from './types';

const statusLabels: Record<ReaderImportBatchDto['status'], string> = {
  Pending: 'Đang chờ',
  Running: 'Đang chạy',
  Completed: 'Hoàn thành',
  Failed: 'Thất bại',
  Cancelled: 'Đã hủy',
};

const statusColors: Record<ReaderImportBatchDto['status'], string> = {
  Pending: 'default',
  Running: 'processing',
  Completed: 'green',
  Failed: 'red',
  Cancelled: 'default',
};

/**
 * VI.4 — Nhập, xuất và đồng bộ dữ liệu bạn đọc.
 *
 * Ba bước theo đúng thứ tự cán bộ làm: tải tệp mẫu, kiểm tra tệp và sửa cho hết lỗi, rồi mới nhập
 * thật. Bước kiểm tra không ghi gì vào hệ thống nên chạy lại bao nhiêu lần cũng được.
 */
export function ReaderImportPage() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<ReaderImportPreviewDto | null>(null);
  const [options, setOptions] = useState<ReaderImportOptions>({
    onDuplicate: 0,
    createMissingCatalogs: true,
    setInitialPassword: false,
  });

  const readerTypes = useCatalogOptions('reader-types');

  const mapping = useQuery({
    queryKey: ['reader-import-mapping'],
    queryFn: () => readersApi.importMapping(),
  });

  const batches = useQuery({
    queryKey: ['reader-import-batches'],
    queryFn: () => readersApi.importBatches({ pageSize: 20 }),
    // Đợt nhập chạy nền, nên màn hình tự hỏi lại tiến độ khi còn đợt đang chạy.
    refetchInterval: (query) =>
      query.state.data?.items.some((batch) => batch.status === 'Pending' || batch.status === 'Running')
        ? 2000
        : false,
  });

  const downloadTemplate = useMutation({
    mutationFn: () => readersApi.importTemplate(),
    onSuccess: ({ blob, fileName }) => saveBlob(blob, fileName),
    onError: (error: Error) => message.error(error.message),
  });

  const validate = useMutation({
    mutationFn: () => readersApi.validateImport(file as File, currentOptions()),
    onSuccess: (result) => {
      setPreview(result);

      if (result.errorRows === 0) {
        message.success(`Tệp hợp lệ: ${result.validRows} dòng sẵn sàng nhập.`);
      } else {
        message.warning(`Có ${result.errorRows} dòng lỗi cần sửa trước khi nhập.`);
      }
    },
    onError: (error: Error) => message.error(error.message),
  });

  const startImport = useMutation({
    mutationFn: () => readersApi.startImport(file as File, currentOptions()),
    onSuccess: () => {
      message.success('Đã xếp hàng đợt nhập. Tiến độ hiện ở bảng bên dưới.');
      setPreview(null);
      setFile(null);
      void queryClient.invalidateQueries({ queryKey: ['reader-import-batches'] });
      void queryClient.invalidateQueries({ queryKey: ['readers'] });
    },
    onError: (error: Error) => message.error(error.message),
  });

  const saveMapping = useMutation({
    mutationFn: (next: Record<string, string>) => readersApi.saveImportMapping(next),
    onSuccess: () => {
      message.success('Đã lưu ánh xạ cột cho lần nhập sau.');
      void queryClient.invalidateQueries({ queryKey: ['reader-import-mapping'] });
    },
    onError: (error: Error) => message.error(error.message),
  });

  const importPhotos = useMutation({
    mutationFn: ({ zip, dryRun }: { zip: File; dryRun: boolean }) =>
      readersApi.importPhotos(zip, dryRun),
    onSuccess: (result, variables) => {
      const summary = `Khớp ${result.matched}/${result.totalFiles} ảnh` +
        (result.unmatched > 0 ? `, ${result.unmatched} ảnh không tìm được bạn đọc` : '') +
        (result.invalid > 0 ? `, ${result.invalid} tệp không phải ảnh` : '');

      if (variables.dryRun) {
        message.info(`${summary} (mới chỉ kiểm tra, chưa ghi).`);
      } else {
        message.success(summary);
        void queryClient.invalidateQueries({ queryKey: ['readers'] });
      }
    },
    onError: (error: Error) => message.error(error.message),
  });

  const currentOptions = (): ReaderImportOptions => ({
    ...options,
    mapping: mapping.data,
  });

  const excelUpload: UploadProps = {
    accept: '.xlsx,.xls',
    showUploadList: false,
    beforeUpload: (selected) => {
      setFile(selected);
      setPreview(null);
      return false;
    },
  };

  const zipUpload = (dryRun: boolean): UploadProps => ({
    accept: '.zip',
    showUploadList: false,
    beforeUpload: (selected) => {
      importPhotos.mutate({ zip: selected, dryRun });
      return false;
    },
  });

  const errorColumns: ColumnsType<ReaderImportErrorDto> = [
    { title: 'Dòng', dataIndex: 'row', width: 80 },
    { title: 'Cột', dataIndex: 'column', width: 160 },
    { title: 'Giá trị', dataIndex: 'value', width: 180, ellipsis: true },
    { title: 'Lỗi', dataIndex: 'message' },
  ];

  const batchColumns: ColumnsType<ReaderImportBatchDto> = [
    { title: 'Tệp', dataIndex: 'fileName', ellipsis: true },
    { title: 'Lúc', dataIndex: 'createdAt', width: 170, render: formatDateTime },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      width: 130,
      render: (status: ReaderImportBatchDto['status']) => (
        <Tag color={statusColors[status]}>{statusLabels[status]}</Tag>
      ),
    },
    {
      title: 'Kết quả',
      width: 260,
      render: (_, row) =>
        row.status === 'Completed' || row.status === 'Failed' ? (
          <Space direction="vertical" size={0} style={{ width: '100%' }}>
            <Progress
              percent={row.totalRows === 0 ? 0 : Math.round((row.successRows / row.totalRows) * 100)}
              size="small"
              status={row.errorRows > 0 ? 'exception' : 'success'}
            />
            <Typography.Text style={{ fontSize: 12 }}>
              {row.successRows}/{row.totalRows} dòng vào hệ thống
              {row.errorRows > 0 ? `, ${row.errorRows} dòng lỗi` : ''}
            </Typography.Text>
          </Space>
        ) : (
          <Typography.Text type="secondary">Đang xử lý…</Typography.Text>
        ),
    },
    {
      title: '',
      width: 130,
      render: (_, row) =>
        row.errorRows > 0 ? (
          <Button
            type="link"
            size="small"
            icon={<DownloadOutlined />}
            onClick={async () => {
              const { blob, fileName } = await readersApi.importErrors(row.id);
              saveBlob(blob, fileName);
            }}
          >
            Nhật ký lỗi
          </Button>
        ) : null,
    },
  ];

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Nhập xuất dữ liệu bạn đọc"
        description="Nhập danh sách bạn đọc từ Excel, nhập ảnh hàng loạt và khai ánh xạ trường cho hệ thống quản lý đào tạo."
        actions={
          <Button
            icon={<FileExcelOutlined />}
            loading={downloadTemplate.isPending}
            onClick={() => downloadTemplate.mutate()}
          >
            Tải tệp mẫu
          </Button>
        }
      />

      <Row gutter={16}>
        <Col span={14}>
          <Card size="small" title="1. Chọn tệp và khai tùy chọn">
            <Space direction="vertical" size={12} style={{ width: '100%' }}>
              <Space wrap>
                <Upload {...excelUpload}>
                  <Button icon={<CloudUploadOutlined />}>Chọn tệp Excel</Button>
                </Upload>
                {file && <Tag color="blue">{file.name}</Tag>}
              </Space>

              <Space wrap>
                <Space size={4}>
                  <span>Khi trùng mã sinh viên:</span>
                  <Select
                    style={{ width: 260 }}
                    value={options.onDuplicate}
                    options={duplicateActionOptions}
                    onChange={(value) =>
                      setOptions({ ...options, onDuplicate: value as ReaderImportDuplicateAction })
                    }
                  />
                </Space>
                <Space size={4}>
                  <span>Loại bạn đọc mặc định:</span>
                  <Select
                    allowClear
                    style={{ width: 180 }}
                    placeholder="Lấy theo cột trong tệp"
                    options={toOptions(readerTypes.data)}
                    value={options.defaultReaderTypeId ?? undefined}
                    onChange={(value) => setOptions({ ...options, defaultReaderTypeId: value })}
                  />
                </Space>
              </Space>

              <Space wrap>
                <Checkbox
                  checked={options.createMissingCatalogs}
                  onChange={(event) =>
                    setOptions({ ...options, createMissingCatalogs: event.target.checked })
                  }
                >
                  Tự tạo khoa, ngành, lớp, khóa chưa có trong danh mục
                </Checkbox>
                <Checkbox
                  checked={options.setInitialPassword}
                  onChange={(event) =>
                    setOptions({ ...options, setInitialPassword: event.target.checked })
                  }
                >
                  Đặt mật khẩu tra cứu ban đầu bằng ngày sinh (ddMMyyyy)
                </Checkbox>
              </Space>

              <Space>
                <Button
                  type="primary"
                  disabled={!file}
                  loading={validate.isPending}
                  onClick={() => validate.mutate()}
                >
                  2. Kiểm tra tệp
                </Button>
                <Can permission={PERMISSIONS.reader.import}>
                  <Button
                    icon={<CheckCircleOutlined />}
                    disabled={!file || !preview || preview.validRows === 0}
                    loading={startImport.isPending}
                    onClick={() => startImport.mutate()}
                  >
                    3. Nhập vào hệ thống
                  </Button>
                </Can>
              </Space>
            </Space>
          </Card>
        </Col>

        <Col span={10}>
          <Card size="small" title="Ánh xạ cột của tệp">
            <Typography.Paragraph type="secondary" style={{ fontSize: 12 }}>
              Tệp của phòng đào tạo thường đặt tên cột khác tệp mẫu. Khai một lần ở đây, lần nhập sau
              hệ thống dùng lại.
            </Typography.Paragraph>

            <Table
              rowKey="field"
              size="small"
              pagination={false}
              scroll={{ y: 260 }}
              dataSource={Object.entries(mapping.data ?? {}).map(([field, header]) => ({
                field,
                header,
              }))}
              columns={[
                {
                  title: 'Trường dữ liệu',
                  dataIndex: 'field',
                  width: 150,
                  render: (field: string) => importFieldLabels[field] ?? field,
                },
                {
                  title: 'Tên cột trong tệp',
                  dataIndex: 'header',
                  render: (header: string, row) => (
                    <Select
                      size="small"
                      style={{ width: '100%' }}
                      value={header}
                      showSearch
                      options={(preview?.headers ?? [header]).map((item) => ({
                        value: item,
                        label: item,
                      }))}
                      onChange={(value) =>
                        saveMapping.mutate({ ...(mapping.data ?? {}), [row.field]: value })
                      }
                    />
                  ),
                },
              ]}
            />
          </Card>
        </Col>
      </Row>

      {preview && (
        <Card size="small" title={`Kết quả kiểm tra tệp ${preview.fileName}`}>
          <Row gutter={16} style={{ marginBottom: 12 }}>
            <Col span={6}>
              <Statistic title="Tổng số dòng" value={preview.totalRows} />
            </Col>
            <Col span={6}>
              <Statistic
                title="Sẵn sàng nhập"
                value={preview.validRows}
                valueStyle={{ color: '#389e0d' }}
              />
            </Col>
            <Col span={6}>
              <Statistic
                title="Dòng lỗi"
                value={preview.errorRows}
                valueStyle={{ color: preview.errorRows > 0 ? '#cf1322' : undefined }}
              />
            </Col>
          </Row>

          {preview.errorRows === 0 ? (
            <Alert
              type="success"
              showIcon
              message="Tệp không có lỗi. Bấm bước 3 để nhập vào hệ thống."
            />
          ) : (
            <Table
              rowKey={(row) => `${row.row}-${row.column}-${row.message}`}
              size="small"
              dataSource={preview.errors}
              columns={errorColumns}
              pagination={{ pageSize: 10 }}
            />
          )}

          {preview.sample.length > 0 && (
            <>
              <Typography.Title level={5} style={{ marginTop: 16 }}>
                Xem trước dữ liệu đã tách cột
              </Typography.Title>
              <Table
                rowKey="row"
                size="small"
                pagination={false}
                scroll={{ x: 900 }}
                dataSource={preview.sample.slice(0, 10)}
                rowClassName={(row) => (row.hasError ? 'lc-row-error' : '')}
                columns={[
                  { title: 'Dòng', dataIndex: 'row', width: 70 },
                  { title: 'Mã SV', dataIndex: 'studentCode', width: 120 },
                  { title: 'Họ và tên', dataIndex: 'fullName', width: 180 },
                  { title: 'Loại bạn đọc', dataIndex: 'readerType', width: 120 },
                  { title: 'Khoa', dataIndex: 'faculty', width: 160, ellipsis: true },
                  { title: 'Lớp', dataIndex: 'className', width: 100 },
                  { title: 'Khóa', dataIndex: 'courseYear', width: 80 },
                  {
                    title: 'Ghi chú',
                    width: 140,
                    render: (_, row) =>
                      row.hasError ? (
                        <Tag color="red">Có lỗi</Tag>
                      ) : row.isExisting ? (
                        <Tag color="orange">Đã có hồ sơ</Tag>
                      ) : (
                        <Tag color="green">Thêm mới</Tag>
                      ),
                  },
                ]}
              />
            </>
          )}
        </Card>
      )}

      <Card size="small" title="Các đợt nhập gần đây">
        <Table
          rowKey="id"
          size="small"
          loading={batches.isLoading}
          dataSource={batches.data?.items ?? []}
          columns={batchColumns}
          pagination={false}
          locale={{ emptyText: <Empty description="Chưa có đợt nhập nào" /> }}
        />
      </Card>

      <Card
        size="small"
        title={
          <Space>
            <PictureOutlined />
            Nhập ảnh bạn đọc hàng loạt
          </Space>
        }
      >
        <Space direction="vertical" size={8} style={{ width: '100%' }}>
          <Typography.Text type="secondary">
            Nén thư mục ảnh thành tệp ZIP, mỗi ảnh đặt tên theo mã sinh viên hoặc số thẻ, ví dụ
            <Typography.Text code>2151010101.jpg</Typography.Text>. Hệ thống tự khớp vào hồ sơ.
          </Typography.Text>

          <Space>
            <Upload {...zipUpload(true)}>
              <Button loading={importPhotos.isPending}>Kiểm tra tệp ZIP</Button>
            </Upload>
            <Can permission={PERMISSIONS.reader.import}>
              <Upload {...zipUpload(false)}>
                <Button type="primary" loading={importPhotos.isPending}>
                  Nhập ảnh vào hồ sơ
                </Button>
              </Upload>
            </Can>
          </Space>

          {importPhotos.data && importPhotos.data.issues.length > 0 && (
            <Table
              rowKey="fileName"
              size="small"
              pagination={{ pageSize: 5 }}
              dataSource={importPhotos.data.issues}
              columns={[
                { title: 'Tệp', dataIndex: 'fileName', width: 260 },
                { title: 'Vấn đề', dataIndex: 'message' },
              ]}
            />
          )}
        </Space>
      </Card>
    </Space>
  );
}
