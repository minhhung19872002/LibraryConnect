import { useMemo, useState } from 'react';
import {
  Alert,
  App,
  Button,
  Card,
  Checkbox,
  Descriptions,
  Empty,
  InputNumber,
  Progress,
  Radio,
  Select,
  Space,
  Steps,
  Table,
  Tag,
  Typography,
  Upload,
} from 'antd';
import { InboxOutlined, ReloadOutlined } from '@ant-design/icons';
import { useMutation, useQuery } from '@tanstack/react-query';
import { PageHeader } from '@/components/PageHeader';
import { errorMessage } from '@/api/formErrors';
import { importApi, locationsApi } from './api';
import { useCatalogOptions, toOptions } from './useCatalogOptions';
import {
  DUPLICATE_ACTION_LABELS,
  JOB_STATUS_LABELS,
  MATCH_BY_LABELS,
  RECORD_STATUS_LABELS,
  type BibImportOptions,
  type BibImportPreview,
  type BibImportPreviewRow,
  type DuplicateAction,
  type DuplicateMatchBy,
  type ImportJob,
  type RecordStatus,
} from './importTypes';

const MONOSPACE = { fontFamily: 'ui-monospace, Consolas, monospace' } as const;

/**
 * Nhập biểu ghi từ tệp trao đổi (II.6).
 *
 * Four steps, in the order the decisions actually have to be made: choose the file, look at what is
 * in it and what it collides with, say what to do about the collisions and where the copies go, then
 * watch it run. The middle two steps exist because an import that silently overwrote a catalogue
 * would be unusable — the librarian has to see the damage before agreeing to it.
 */
export function BibImportPage() {
  const { message } = App.useApp();

  const [step, setStep] = useState(0);
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<BibImportPreview | null>(null);
  const [jobId, setJobId] = useState<string | null>(null);

  const [options, setOptions] = useState<BibImportOptions>({
    matchBy: 'Isbn',
    onDuplicate: 'Skip',
    status: 'Published',
    addToCatalogQueue: false,
    createItems: false,
    itemQuantity: 1,
  });

  const documentTypes = useCatalogOptions('document-types');
  const fundingSources = useCatalogOptions('funding-sources');

  const warehouses = useQuery({
    queryKey: ['warehouses'],
    queryFn: () => locationsApi.warehouses(),
  });

  const runPreview = useMutation({
    mutationFn: (selected: File) => importApi.preview(selected, options.matchBy),
    onSuccess: (result) => {
      setPreview(result);
      setStep(1);
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const start = useMutation({
    mutationFn: () => importApi.start(file!, options),
    onSuccess: (id) => {
      setJobId(id);
      setStep(3);
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const job = useQuery({
    queryKey: ['import-job', jobId],
    queryFn: () => importApi.job(jobId!),
    enabled: Boolean(jobId),
    // The job runs in another process, so the screen asks it how far it has got until it stops.
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      return status === 'Pending' || status === 'Running' ? 1000 : false;
    },
  });

  const jobs = useQuery({
    queryKey: ['import-jobs'],
    queryFn: () => importApi.jobs(20),
  });

  const reset = () => {
    setStep(0);
    setFile(null);
    setPreview(null);
    setJobId(null);
  };

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Nhập biểu ghi từ tệp trao đổi"
        description="Đọc tệp ISO 2709 (.mrc, .iso) hoặc MARCXML (.xml), đối chiếu trùng, rồi nhập vào cơ sở dữ liệu. Tác vụ chạy nền nên tệp lớn không làm treo màn hình."
        actions={
          step > 0 && (
            <Button icon={<ReloadOutlined />} onClick={reset}>
              Nhập tệp khác
            </Button>
          )
        }
      />

      <Steps
        current={step}
        items={[
          { title: 'Chọn tệp' },
          { title: 'Xem trước' },
          { title: 'Tùy chọn nhập' },
          { title: 'Kết quả' },
        ]}
      />

      {step === 0 && (
        <Card>
          <Upload.Dragger
            accept=".mrc,.marc,.iso,.xml,.mrx"
            showUploadList={false}
            beforeUpload={(selected) => {
              const chosen = selected as unknown as File;
              setFile(chosen);
              runPreview.mutate(chosen);
              return false;
            }}
            disabled={runPreview.isPending}
          >
            <p className="ant-upload-drag-icon">
              <InboxOutlined />
            </p>
            <p className="ant-upload-text">Kéo tệp vào đây hoặc bấm để chọn</p>
            <p className="ant-upload-hint">
              Chấp nhận tệp .mrc, .iso theo ISO 2709 và tệp .xml theo MARCXML. Bước này chỉ đọc tệp,
              chưa ghi gì vào cơ sở dữ liệu.
            </p>
          </Upload.Dragger>
        </Card>
      )}

      {step === 1 && preview && (
        <PreviewStep
          preview={preview}
          matchBy={options.matchBy}
          onChangeMatchBy={(value) => {
            setOptions((current) => ({ ...current, matchBy: value }));
            if (file) {
              runPreview.mutate(file);
            }
          }}
          loading={runPreview.isPending}
          onNext={() => setStep(2)}
        />
      )}

      {step === 2 && preview && (
        <Card title="Tùy chọn nhập">
          <Space direction="vertical" size={20} style={{ width: '100%' }}>
            <div>
              <Typography.Text strong>Xử lý biểu ghi trùng</Typography.Text>
              <Typography.Paragraph type="secondary" style={{ marginBottom: 8 }}>
                Tệp này có {preview.duplicateCount} biểu ghi trùng với dữ liệu đã có.
              </Typography.Paragraph>
              <Radio.Group
                value={options.onDuplicate}
                onChange={(event) =>
                  setOptions((current) => ({ ...current, onDuplicate: event.target.value as DuplicateAction }))
                }
              >
                <Space direction="vertical">
                  {(Object.keys(DUPLICATE_ACTION_LABELS) as DuplicateAction[]).map((value) => (
                    <Radio key={value} value={value}>
                      <Typography.Text>{DUPLICATE_ACTION_LABELS[value].title}</Typography.Text>
                      <br />
                      <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                        {DUPLICATE_ACTION_LABELS[value].hint}
                      </Typography.Text>
                    </Radio>
                  ))}
                </Space>
              </Radio.Group>
            </div>

            <Space size={16} wrap align="start">
              <div style={{ width: 240 }}>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  Dạng tài liệu gán cho biểu ghi nhập vào
                </Typography.Text>
                <Select
                  value={options.documentTypeId}
                  onChange={(value) => setOptions((current) => ({ ...current, documentTypeId: value }))}
                  options={toOptions(documentTypes.data)}
                  placeholder="Giữ nguyên như trong tệp"
                  allowClear
                  showSearch
                  optionFilterProp="label"
                  style={{ width: '100%' }}
                />
              </div>

              <div style={{ width: 200 }}>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  Trạng thái sau khi nhập
                </Typography.Text>
                <Select<RecordStatus>
                  value={options.status}
                  onChange={(value) => setOptions((current) => ({ ...current, status: value }))}
                  options={Object.entries(RECORD_STATUS_LABELS).map(([value, label]) => ({
                    value: value as RecordStatus,
                    label,
                  }))}
                  style={{ width: '100%' }}
                />
              </div>
            </Space>

            <Checkbox
              checked={options.addToCatalogQueue}
              onChange={(event) =>
                setOptions((current) => ({ ...current, addToCatalogQueue: event.target.checked }))
              }
            >
              Đưa vào hàng đợi biên mục chi tiết
              <Typography.Text type="secondary" style={{ fontSize: 12, display: 'block' }}>
                Biểu ghi nhập từ nơi khác thường cần cán bộ rà lại trước khi đưa ra phục vụ.
              </Typography.Text>
            </Checkbox>

            <div>
              <Checkbox
                checked={options.createItems}
                onChange={(event) =>
                  setOptions((current) => ({ ...current, createItems: event.target.checked }))
                }
              >
                Tạo sẵn đăng ký cá biệt cho mỗi biểu ghi
              </Checkbox>

              {options.createItems && (
                <Space size={12} wrap style={{ marginTop: 12, marginLeft: 24 }}>
                  <InputNumber
                    min={1}
                    max={100}
                    value={options.itemQuantity}
                    onChange={(value) =>
                      setOptions((current) => ({ ...current, itemQuantity: value ?? 1 }))
                    }
                    addonBefore="Số bản"
                    style={{ width: 160 }}
                  />
                  <Select
                    value={options.warehouseId}
                    onChange={(value) => setOptions((current) => ({ ...current, warehouseId: value }))}
                    options={(warehouses.data ?? []).map((warehouse) => ({
                      value: warehouse.id,
                      label: warehouse.name,
                    }))}
                    placeholder="Kho lưu giữ"
                    style={{ width: 220 }}
                  />
                  <Select
                    value={options.fundingSourceId}
                    onChange={(value) => setOptions((current) => ({ ...current, fundingSourceId: value }))}
                    options={toOptions(fundingSources.data)}
                    placeholder="Nguồn kinh phí"
                    allowClear
                    style={{ width: 220 }}
                  />
                </Space>
              )}

              {options.createItems && !options.warehouseId && (
                <Alert
                  type="warning"
                  showIcon
                  style={{ marginTop: 12, marginLeft: 24 }}
                  message="Chưa chọn kho nên hệ thống sẽ không tạo đăng ký cá biệt."
                />
              )}
            </div>

            <Space>
              <Button onClick={() => setStep(1)}>Quay lại</Button>
              <Button type="primary" loading={start.isPending} onClick={() => start.mutate()}>
                Bắt đầu nhập {preview.totalRecords} biểu ghi
              </Button>
            </Space>
          </Space>
        </Card>
      )}

      {step === 3 && job.data && <JobResult job={job.data} />}

      <Card size="small" title="Các lần nhập gần đây" loading={jobs.isFetching}>
        <Table<ImportJob>
          rowKey="id"
          size="small"
          dataSource={jobs.data ?? []}
          pagination={false}
          locale={{ emptyText: <Empty description="Chưa có lần nhập nào" /> }}
          onRow={(row) => ({
            onClick: () => {
              setJobId(row.id);
              setStep(3);
            },
            style: { cursor: 'pointer' },
          })}
          columns={[
            { title: 'Tệp', dataIndex: 'fileName' },
            {
              title: 'Thời điểm',
              dataIndex: 'createdAt',
              width: 170,
              render: (value: string) => new Date(value).toLocaleString('vi-VN'),
            },
            { title: 'Người thực hiện', dataIndex: 'createdByName', width: 170 },
            {
              title: 'Trạng thái',
              dataIndex: 'status',
              width: 140,
              render: (value: ImportJob['status']) => (
                <Tag color={value === 'Completed' ? 'green' : value === 'Failed' ? 'red' : 'blue'}>
                  {JOB_STATUS_LABELS[value] ?? value}
                </Tag>
              ),
            },
            {
              title: 'Kết quả',
              width: 260,
              render: (_, row) => (
                <Typography.Text style={{ fontSize: 12 }}>
                  {row.success} thành công · {row.skipped} bỏ qua · {row.failed} lỗi
                </Typography.Text>
              ),
            },
          ]}
        />
      </Card>
    </Space>
  );
}

function PreviewStep({
  preview,
  matchBy,
  onChangeMatchBy,
  loading,
  onNext,
}: {
  preview: BibImportPreview;
  matchBy: DuplicateMatchBy;
  onChangeMatchBy: (value: DuplicateMatchBy) => void;
  loading: boolean;
  onNext: () => void;
}) {
  const columns = useMemo(
    () => [
      { title: '#', dataIndex: 'recordNumber', width: 60 },
      {
        title: 'Nhan đề',
        dataIndex: 'title',
        render: (value: string, row: BibImportPreviewRow) => (
          <Space direction="vertical" size={0}>
            <Typography.Text>{value}</Typography.Text>
            {row.author && (
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                {row.author}
              </Typography.Text>
            )}
          </Space>
        ),
      },
      {
        title: 'ISBN',
        dataIndex: 'isbn',
        width: 160,
        render: (value?: string) => <span style={MONOSPACE}>{value}</span>,
      },
      { title: 'Năm', dataIndex: 'publishYear', width: 80 },
      {
        title: 'Đối chiếu',
        width: 280,
        render: (_: unknown, row: BibImportPreviewRow) =>
          row.duplicateOfId ? (
            <Space direction="vertical" size={0}>
              <Tag color="orange">Trùng biểu ghi đã có</Tag>
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                {row.duplicateOfControlNumber} — {row.duplicateOfTitle}
              </Typography.Text>
            </Space>
          ) : (
            <Tag color="green">Biểu ghi mới</Tag>
          ),
      },
      {
        title: 'Kiểm tra',
        width: 300,
        render: (_: unknown, row: BibImportPreviewRow) =>
          row.errors.length > 0 ? (
            <Typography.Text type="danger" style={{ fontSize: 12 }}>
              {row.errors.join(' ')}
            </Typography.Text>
          ) : row.warnings.length > 0 ? (
            <Typography.Text type="warning" style={{ fontSize: 12 }}>
              {row.warnings.length} cảnh báo
            </Typography.Text>
          ) : (
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              Hợp lệ
            </Typography.Text>
          ),
      },
    ],
    [],
  );

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <Card size="small">
        <Descriptions size="small" column={{ xs: 1, md: 4 }}>
          <Descriptions.Item label="Định dạng">{preview.format}</Descriptions.Item>
          <Descriptions.Item label="Số biểu ghi">{preview.totalRecords}</Descriptions.Item>
          <Descriptions.Item label="Trùng dữ liệu đã có">{preview.duplicateCount}</Descriptions.Item>
          <Descriptions.Item label="Không hợp lệ">{preview.invalidCount}</Descriptions.Item>
        </Descriptions>

        <Space style={{ marginTop: 12 }} wrap>
          <Typography.Text type="secondary">Đối chiếu trùng theo</Typography.Text>
          <Radio.Group
            value={matchBy}
            onChange={(event) => onChangeMatchBy(event.target.value as DuplicateMatchBy)}
            disabled={loading}
          >
            {(Object.keys(MATCH_BY_LABELS) as DuplicateMatchBy[]).map((value) => (
              <Radio.Button key={value} value={value}>
                {MATCH_BY_LABELS[value]}
              </Radio.Button>
            ))}
          </Radio.Group>
        </Space>
      </Card>

      {preview.fileErrors.length > 0 && (
        <Alert
          type="warning"
          showIcon
          message={`${preview.fileErrors.length} biểu ghi trong tệp không đọc được`}
          description={
            <Space direction="vertical" size={2}>
              {preview.fileErrors.map((error) => (
                <Typography.Text key={error.recordNumber} style={{ fontSize: 13 }}>
                  Biểu ghi số {error.recordNumber}: {error.message}
                </Typography.Text>
              ))}
            </Space>
          }
        />
      )}

      <Table<BibImportPreviewRow>
        rowKey="recordNumber"
        size="small"
        loading={loading}
        dataSource={preview.records}
        columns={columns}
        pagination={{ pageSize: 20, showSizeChanger: true }}
      />

      <Space>
        <Button type="primary" onClick={onNext} disabled={preview.totalRecords === 0}>
          Tiếp tục
        </Button>
      </Space>
    </Space>
  );
}

function JobResult({ job }: { job: ImportJob }) {
  const done = job.status === 'Completed' || job.status === 'Failed' || job.status === 'Cancelled';
  const processed = job.success + job.skipped + job.failed;
  const percent = job.total === 0 ? 0 : Math.round((processed / job.total) * 100);

  return (
    <Card
      title={
        <Space>
          <span>Kết quả nhập</span>
          <Tag color={job.status === 'Completed' ? 'green' : job.status === 'Failed' ? 'red' : 'blue'}>
            {JOB_STATUS_LABELS[job.status] ?? job.status}
          </Tag>
        </Space>
      }
    >
      <Space direction="vertical" size={16} style={{ width: '100%' }}>
        <Progress
          percent={percent}
          status={job.status === 'Failed' ? 'exception' : done ? 'success' : 'active'}
        />

        <Descriptions size="small" column={{ xs: 1, md: 4 }} bordered>
          <Descriptions.Item label="Tổng số">{job.total}</Descriptions.Item>
          <Descriptions.Item label="Thành công">{job.success}</Descriptions.Item>
          <Descriptions.Item label="Bỏ qua vì trùng">{job.skipped}</Descriptions.Item>
          <Descriptions.Item label="Lỗi">{job.failed}</Descriptions.Item>
        </Descriptions>

        {job.errors.length > 0 && (
          <Table
            rowKey={(row) => `${row.row}-${row.message}`}
            size="small"
            dataSource={job.errors}
            pagination={{ pageSize: 10 }}
            columns={[
              { title: 'Biểu ghi số', dataIndex: 'row', width: 110 },
              {
                title: 'Số kiểm soát',
                dataIndex: 'identifier',
                width: 160,
                render: (value?: string) => <span style={MONOSPACE}>{value}</span>,
              },
              { title: 'Lý do', dataIndex: 'message' },
            ]}
          />
        )}
      </Space>
    </Card>
  );
}
