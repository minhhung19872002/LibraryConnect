import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Checkbox,
  Col,
  Popconfirm,
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
  DownloadOutlined,
  FileExcelOutlined,
  InboxOutlined,
  UploadOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { digitalApi } from './api';
import { accessLevelLabels, formatDateTime } from './labels';
import {
  describeExportCounts,
  exportProgressPercent,
  exportStatusColors,
  exportStatusLabels,
  formatPackageSize,
  isExportOpen,
} from './fullExportView';
import type {
  DigitalAccessLevel,
  DigitalCollectionDto,
  DigitalImportRowDto,
  FullSystemExportJobDto,
} from './types';
import { MAU } from '@/lib/palette';

/** Lưu tệp nhận về từ máy chủ xuống máy người dùng. */
function saveBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');

  anchor.href = url;
  anchor.download = fileName;
  anchor.click();

  URL.revokeObjectURL(url);
}

const accessOptions = (Object.keys(accessLevelLabels) as DigitalAccessLevel[]).map((value) => ({
  value,
  label: accessLevelLabels[value],
}));

/**
 * V.3 — Nhập hàng loạt từ tệp nén và xuất gói tài liệu kèm metadata.
 *
 * Nút "Kiểm tra trước" chạy đúng đường đi của lần nhập thật nhưng không ghi gì vào hệ thống, nên
 * cán bộ biết trước tệp nào hỏng và tệp nào chưa khớp được biểu ghi.
 */
export function DigitalImportExportPage() {
  const { message } = App.useApp();

  const [file, setFile] = useState<File | null>(null);
  const [collectionId, setCollectionId] = useState<string | undefined>(undefined);
  const [accessLevel, setAccessLevel] = useState<DigitalAccessLevel | undefined>(undefined);
  const [allowDownload, setAllowDownload] = useState(false);
  const [allowPrint, setAllowPrint] = useState(false);
  const [rows, setRows] = useState<DigitalImportRowDto[]>([]);
  const [summary, setSummary] = useState<{ total: number; success: number; failed: number } | null>(
    null,
  );

  const [exportCollectionId, setExportCollectionId] = useState<string | undefined>(undefined);
  const [includeFiles, setIncludeFiles] = useState(true);

  const collections = useQuery({
    queryKey: ['digital-collections'],
    queryFn: () => digitalApi.collections(true),
  });

  const runImport = useMutation({
    mutationFn: (dryRun: boolean) =>
      digitalApi.importArchive(file!, {
        collectionId,
        accessLevel,
        allowDownload: String(allowDownload),
        allowPrint: String(allowPrint),
        dryRun: String(dryRun),
      }),
    onSuccess: (result, dryRun) => {
      setRows(result.rows);
      setSummary({ total: result.total, success: result.success, failed: result.failed });

      if (dryRun) {
        message.info(`Kiểm tra xong ${result.total} tệp, ${result.failed} tệp có vấn đề.`);
      } else {
        message.success(`Đã nhập ${result.success}/${result.total} tệp.`);
      }
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không nhập được.'),
  });

  const runExport = useMutation({
    mutationFn: () =>
      digitalApi.exportArchive({
        documentIds: [],
        collectionId: exportCollectionId,
        includeFiles,
      }),
    onSuccess: ({ blob, fileName }) => {
      saveBlob(blob, fileName);
      message.success('Đã tải gói xuống.');
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xuất được.'),
  });

  const downloadTemplate = useMutation({
    mutationFn: () => digitalApi.importTemplate(),
    onSuccess: ({ blob, fileName }) => saveBlob(blob, fileName || 'metadata.xlsx'),
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không tải được tệp mẫu.'),
  });

  // --- Xuất toàn bộ dữ liệu hệ thống (mục 4 E-HSMT) ---------------------------------------
  const queryClient = useQueryClient();

  const fullExports = useQuery({
    queryKey: ['digital-full-exports'],
    queryFn: () => digitalApi.fullExports(),
    // Còn lượt đang chạy thì hỏi lại tiến độ mỗi 3 giây; xong hết thì thôi.
    refetchInterval: (query) =>
      (query.state.data ?? []).some(isExportOpen) ? 3000 : false,
  });

  const queueFullExport = useMutation({
    mutationFn: () => digitalApi.queueFullExport(),
    onSuccess: () => {
      message.success('Đã xếp lượt xuất toàn bộ dữ liệu vào hàng đợi. Theo dõi tiến độ ở bảng bên dưới.');
      void queryClient.invalidateQueries({ queryKey: ['digital-full-exports'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xếp được lượt xuất.'),
  });

  const downloadFullExport = useMutation({
    mutationFn: (id: string) => digitalApi.downloadFullExport(id),
    onSuccess: ({ blob, fileName }) => saveBlob(blob, fileName || 'libraryconnect-ban-giao.zip'),
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không tải được gói.'),
  });

  const hasOpenExport = (fullExports.data ?? []).some(isExportOpen);

  const exportColumns: ColumnsType<FullSystemExportJobDto> = [
    {
      title: 'Thời điểm',
      dataIndex: 'createdAt',
      width: 150,
      render: (value: string) => formatDateTime(value),
    },
    { title: 'Người yêu cầu', dataIndex: 'createdByName', width: 160, ellipsis: true },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      width: 120,
      render: (_: unknown, job) => (
        <Tag color={exportStatusColors[job.status]}>{exportStatusLabels[job.status]}</Tag>
      ),
    },
    {
      title: 'Tiến độ',
      key: 'progress',
      width: 260,
      render: (_: unknown, job) =>
        job.status === 'Failed' ? (
          <Typography.Text type="danger">{job.message ?? 'Thất bại'}</Typography.Text>
        ) : (
          <Space direction="vertical" size={0} style={{ width: '100%' }}>
            <Progress
              percent={exportProgressPercent(job)}
              size="small"
              status={isExportOpen(job) ? 'active' : undefined}
            />
            {isExportOpen(job) && job.currentStep && (
              <Typography.Text type="secondary">{job.currentStep}</Typography.Text>
            )}
          </Space>
        ),
    },
    {
      title: 'Nội dung gói',
      key: 'counts',
      ellipsis: true,
      render: (_: unknown, job) =>
        job.status === 'Completed' ? describeExportCounts(job) : '—',
    },
    {
      title: 'Dung lượng',
      dataIndex: 'sizeBytes',
      width: 110,
      render: (value: number | null) => formatPackageSize(value),
    },
    {
      title: 'Tải về',
      key: 'download',
      width: 110,
      render: (_: unknown, job) =>
        job.hasFile ? (
          <Button
            size="small"
            icon={<DownloadOutlined />}
            loading={downloadFullExport.isPending && downloadFullExport.variables === job.id}
            onClick={() => downloadFullExport.mutate(job.id)}
          >
            Tải gói
          </Button>
        ) : null,
    },
  ];

  const columns: ColumnsType<DigitalImportRowDto> = [
    { title: 'Tệp', dataIndex: 'fileName', width: 340, ellipsis: true },
    {
      title: 'Kết quả',
      dataIndex: 'success',
      width: 110,
      render: (success: boolean) =>
        success ? <Tag color="green">Đạt</Tag> : <Tag color="red">Lỗi</Tag>,
    },
    { title: 'Ghi chú', dataIndex: 'message', ellipsis: true },
  ];

  const options = flatten(collections.data ?? []);

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Nhập xuất tài liệu số"
        description="Nhập hàng loạt từ tệp nén và xuất gói tài liệu kèm metadata Excel, Dublin Core."
      />

      <Row gutter={16}>
        <Col span={14}>
          <Card size="small" title="Nhập từ tệp nén ZIP">
            <Space direction="vertical" size={12} style={{ width: '100%' }}>
              <Alert
                type="info"
                showIcon
                message="Cách hệ thống khớp tệp với biểu ghi thư mục"
                description={
                  <Space direction="vertical" size={4}>
                    <span>
                      Đặt tên tệp theo số ĐKCB, số kiểm soát 001 hoặc ISBN thì tệp tự gắn vào biểu
                      ghi tương ứng. Tệp không khớp vẫn được nhập và đứng riêng để gắn tay sau.
                    </span>
                    <span>
                      Muốn khai nhan đề, mô tả, mức truy cập hay biểu ghi cho từng tệp thì bỏ thêm
                      bảng <code>metadata.xlsx</code> vào gói — tải tệp mẫu để xem đúng cột.
                    </span>
                    <Button
                      size="small"
                      icon={<FileExcelOutlined />}
                      loading={downloadTemplate.isPending}
                      onClick={() => downloadTemplate.mutate()}
                    >
                      Tải tệp mẫu metadata.xlsx
                    </Button>
                  </Space>
                }
              />

              <Upload.Dragger
                maxCount={1}
                beforeUpload={(selected) => {
                  setFile(selected);
                  setRows([]);
                  setSummary(null);
                  return false;
                }}
                onRemove={() => {
                  setFile(null);
                  setRows([]);
                  setSummary(null);
                }}
                fileList={file ? [{ uid: '1', name: file.name, status: 'done' as const }] : []}
              >
                <p className="ant-upload-drag-icon">
                  <InboxOutlined />
                </p>
                <p className="ant-upload-text">Kéo tệp .zip vào đây hoặc bấm để chọn</p>
              </Upload.Dragger>

              <Space wrap>
                <Select
                  allowClear
                  style={{ width: 240 }}
                  placeholder="Bộ sưu tập đích"
                  options={options}
                  value={collectionId}
                  onChange={setCollectionId}
                />
                <Select
                  allowClear
                  style={{ width: 190 }}
                  placeholder="Mức truy cập"
                  options={accessOptions}
                  value={accessLevel}
                  onChange={setAccessLevel}
                />
                <Checkbox checked={allowDownload} onChange={(e) => setAllowDownload(e.target.checked)}>
                  Cho tải về
                </Checkbox>
                <Checkbox checked={allowPrint} onChange={(e) => setAllowPrint(e.target.checked)}>
                  Cho in
                </Checkbox>
              </Space>

              <Can permission={PERMISSIONS.digital.import}>
                <Space>
                  <Button
                    disabled={!file}
                    loading={runImport.isPending && runImport.variables === true}
                    onClick={() => runImport.mutate(true)}
                  >
                    Kiểm tra trước
                  </Button>
                  <Button
                    type="primary"
                    icon={<UploadOutlined />}
                    disabled={!file}
                    loading={runImport.isPending && runImport.variables === false}
                    onClick={() => runImport.mutate(false)}
                  >
                    Nhập vào hệ thống
                  </Button>
                </Space>
              </Can>

              {summary && (
                <Space size={16} wrap>
                  <Statistic title="Tổng tệp" value={summary.total} />
                  <Statistic
                    title="Đạt"
                    value={summary.success}
                    valueStyle={{ color: MAU.tot }}
                  />
                  <Statistic
                    title="Lỗi"
                    value={summary.failed}
                    valueStyle={{ color: summary.failed > 0 ? MAU.loi : undefined }}
                  />
                </Space>
              )}

              {rows.length > 0 && (
                <Table
                  rowKey="fileName"
                  size="small"
                  dataSource={rows}
                  columns={columns}
                  scroll={{ x: 800 }}
                  pagination={{ pageSize: 10 }}
                />
              )}
            </Space>
          </Card>
        </Col>

        <Col span={10}>
          <Card size="small" title="Xuất gói tài liệu số">
            <Space direction="vertical" size={12} style={{ width: '100%' }}>
              <Typography.Paragraph type="secondary">
                Gói tải về gồm thư mục <code>files/</code> chứa tệp gốc và thư mục{' '}
                <code>metadata/</code> chứa danh mục Excel cùng tệp Dublin Core, dùng được cho việc
                bàn giao dữ liệu khi kết thúc hợp đồng.
              </Typography.Paragraph>

              <Select
                allowClear
                style={{ width: '100%' }}
                placeholder="Toàn bộ kho, hoặc chọn một bộ sưu tập"
                options={options}
                value={exportCollectionId}
                onChange={setExportCollectionId}
              />

              <Checkbox checked={includeFiles} onChange={(e) => setIncludeFiles(e.target.checked)}>
                Kèm cả tệp gốc (bỏ chọn thì chỉ xuất metadata)
              </Checkbox>

              <Can permission={PERMISSIONS.digital.export}>
                <Button
                  type="primary"
                  icon={<DownloadOutlined />}
                  loading={runExport.isPending}
                  onClick={() => runExport.mutate()}
                >
                  Xuất gói
                </Button>
              </Can>
            </Space>
          </Card>
        </Col>
      </Row>

      <Can permission={PERMISSIONS.exchange.fullExport}>
        <Card size="small" title="Xuất toàn bộ dữ liệu hệ thống (bàn giao khi kết thúc hợp đồng)">
          <Space direction="vertical" size={12} style={{ width: '100%' }}>
            <Typography.Paragraph type="secondary" style={{ marginBottom: 0 }}>
              Gói ZIP gồm toàn bộ biểu ghi MARC (<code>marc/</code>, ISO 2709 và MARCXML), toàn bộ tệp
              tài liệu số (<code>digital/</code>) kèm metadata Excel, Dublin Core, MARCXML
              (<code>metadata/</code>), và bạn đọc, ĐKCB, lượt mượn, phạt, đặt giữ dạng CSV
              (<code>du-lieu/</code>). Việc chạy nền trên máy chủ, gói đặt trong thư mục sao lưu;
              kho lớn có thể mất hàng chục phút — cứ để trang này mở hoặc quay lại sau.
            </Typography.Paragraph>

            <Popconfirm
              title="Xuất toàn bộ dữ liệu hệ thống?"
              description="Gói chứa cả hồ sơ bạn đọc và toàn văn tài liệu số. Việc này được ghi vào nhật ký hệ thống."
              okText="Xuất"
              cancelText="Hủy"
              onConfirm={() => queueFullExport.mutate()}
            >
              <Button
                type="primary"
                icon={<DownloadOutlined />}
                loading={queueFullExport.isPending}
                disabled={hasOpenExport}
              >
                {hasOpenExport ? 'Đang có lượt xuất chạy…' : 'Xuất toàn bộ dữ liệu'}
              </Button>
            </Popconfirm>

            <Table
              rowKey="id"
              size="small"
              loading={fullExports.isLoading}
              dataSource={fullExports.data ?? []}
              columns={exportColumns}
              scroll={{ x: 1100 }}
              pagination={false}
              locale={{ emptyText: 'Chưa có lượt xuất toàn bộ nào.' }}
            />
          </Space>
        </Card>
      </Can>
    </Space>
  );
}

function flatten(nodes: DigitalCollectionDto[], depth = 0): { value: string; label: string }[] {
  return nodes.flatMap((node) => [
    { value: node.id, label: `${'— '.repeat(depth)}${node.name}` },
    ...flatten(node.children, depth + 1),
  ]);
}
