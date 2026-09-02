import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Checkbox,
  Col,
  Row,
  Select,
  Space,
  Statistic,
  Table,
  Tag,
  Typography,
  Upload,
} from 'antd';
import { DownloadOutlined, InboxOutlined, UploadOutlined } from '@ant-design/icons';
import { useMutation, useQuery } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { digitalApi } from './api';
import { accessLevelLabels } from './labels';
import type { DigitalAccessLevel, DigitalCollectionDto, DigitalImportRowDto } from './types';
import { MAU } from '@/lib/palette';

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
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');

      anchor.href = url;
      anchor.download = fileName;
      anchor.click();

      URL.revokeObjectURL(url);
      message.success('Đã tải gói xuống.');
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xuất được.'),
  });

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
                description="Đặt tên tệp theo số ĐKCB, số kiểm soát 001 hoặc ISBN thì tệp tự gắn vào biểu ghi tương ứng. Tệp không khớp vẫn được nhập và đứng riêng để gắn tay sau."
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
    </Space>
  );
}

function flatten(nodes: DigitalCollectionDto[], depth = 0): { value: string; label: string }[] {
  return nodes.flatMap((node) => [
    { value: node.id, label: `${'— '.repeat(depth)}${node.name}` },
    ...flatten(node.children, depth + 1),
  ]);
}
