import { useEffect, useState } from 'react';
import {
  Alert,
  App,
  Button,
  Card,
  Checkbox,
  Descriptions,
  Empty,
  Input,
  Modal,
  Select,
  Space,
  Steps,
  Table,
  Tag,
  Typography,
  Upload,
} from 'antd';
import { DownloadOutlined, InboxOutlined, ReloadOutlined, SaveOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PageHeader } from '@/components/PageHeader';
import { errorMessage } from '@/api/formErrors';
import { saveBlob } from '@/modules/marc/api';
import { marcApi } from '@/modules/marc/api';
import { excelApi, importApi } from './api';
import { useCatalogOptions, toOptions } from './useCatalogOptions';
import {
  DUPLICATE_ACTION_LABELS,
  JOB_STATUS_LABELS,
  MATCH_BY_LABELS,
  RECORD_STATUS_LABELS,
  type DuplicateAction,
  type DuplicateMatchBy,
  type ImportJob,
  type RecordStatus,
} from './importTypes';
import type { ExcelColumnMapping, ExcelPreview } from './excelTypes';

const MONOSPACE = { fontFamily: 'ui-monospace, Consolas, monospace' } as const;

/**
 * Nhập biểu ghi từ bảng tính Excel (II.8).
 *
 * The mapping step is what makes this usable on a library's own spreadsheet rather than only on the
 * template: the system guesses a mapping from the column names, and the librarian corrects what it
 * got wrong instead of building the whole thing by hand. A mapping that works can be saved and
 * reused, because the next file from the same supplier will have the same columns.
 */
export function BibExcelImportPage() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [step, setStep] = useState(0);
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<ExcelPreview | null>(null);
  const [mapping, setMapping] = useState<ExcelColumnMapping[]>([]);
  const [jobId, setJobId] = useState<string | null>(null);
  const [saveProfileOpen, setSaveProfileOpen] = useState(false);
  const [profileName, setProfileName] = useState('');

  const [matchBy, setMatchBy] = useState<DuplicateMatchBy>('Isbn');
  const [onDuplicate, setOnDuplicate] = useState<DuplicateAction>('Skip');
  const [status, setStatus] = useState<RecordStatus>('Published');
  const [documentTypeId, setDocumentTypeId] = useState<string | undefined>();
  const [addToQueue, setAddToQueue] = useState(false);

  const documentTypes = useCatalogOptions('document-types');

  const definitions = useQuery({
    queryKey: ['marc-fields', '', false],
    queryFn: () => marcApi.getFields(),
    staleTime: 10 * 60 * 1000,
  });

  const profiles = useQuery({
    queryKey: ['mapping-profiles'],
    queryFn: () => excelApi.profiles(),
  });

  const template = useMutation({
    mutationFn: () => excelApi.template(),
    onSuccess: ({ blob, fileName }) => {
      saveBlob(blob, fileName);
      message.success('Đã tải tệp mẫu.');
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const runPreview = useMutation({
    mutationFn: (selected: File) => excelApi.preview(selected),
    onSuccess: (result) => {
      setPreview(result);
      setMapping(result.suggestedMapping);
      setStep(1);
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const start = useMutation({
    mutationFn: () =>
      excelApi.start(file!, {
        matchBy,
        onDuplicate,
        status,
        documentTypeId,
        addToCatalogQueue: addToQueue,
        createItems: false,
        itemQuantity: 1,
        mapping,
      }),
    onSuccess: (id) => {
      setJobId(id);
      setStep(3);
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const saveProfile = useMutation({
    mutationFn: () => excelApi.saveProfile(null, { name: profileName, isDefault: false, mapping }),
    onSuccess: async () => {
      message.success('Đã lưu hồ sơ ánh xạ để dùng lại.');
      setSaveProfileOpen(false);
      setProfileName('');
      await queryClient.invalidateQueries({ queryKey: ['mapping-profiles'] });
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const job = useQuery({
    queryKey: ['import-job', jobId],
    queryFn: () => importApi.job(jobId!),
    enabled: Boolean(jobId),
    refetchInterval: (query) => {
      const state = query.state.data?.status;
      return state === 'Pending' || state === 'Running' ? 1000 : false;
    },
  });

  useEffect(() => {
    if (job.data?.status === 'Completed') {
      queryClient.invalidateQueries({ queryKey: ['bib-records'] });
    }
  }, [job.data?.status, queryClient]);

  const setColumnMapping = (column: string, change: Partial<ExcelColumnMapping>) => {
    setMapping((current) => {
      const existing = current.find((item) => item.column === column);

      if (!existing) {
        return [...current, { column, tag: '', subfield: 'a', ...change }];
      }

      return current.map((item) => (item.column === column ? { ...item, ...change } : item));
    });
  };

  const clearColumn = (column: string) =>
    setMapping((current) => current.filter((item) => item.column !== column));

  const titleMapped = mapping.some((item) => item.tag === '245' && item.subfield === 'a');

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Nhập biểu ghi từ Excel"
        description="Dùng cho thư viện đang giữ danh mục trong bảng tính. Tải tệp mẫu để nhập theo khuôn có sẵn, hoặc dùng chính tệp của thư viện rồi ánh xạ cột sang trường MARC."
        actions={
          <Space>
            <Button
              icon={<DownloadOutlined />}
              loading={template.isPending}
              onClick={() => template.mutate()}
            >
              Tải tệp mẫu
            </Button>
            {step > 0 && (
              <Button
                icon={<ReloadOutlined />}
                onClick={() => {
                  setStep(0);
                  setFile(null);
                  setPreview(null);
                  setJobId(null);
                }}
              >
                Nhập tệp khác
              </Button>
            )}
          </Space>
        }
      />

      <Steps
        current={step}
        items={[
          { title: 'Chọn tệp' },
          { title: 'Ánh xạ cột' },
          { title: 'Tùy chọn nhập' },
          { title: 'Kết quả' },
        ]}
      />

      {step === 0 && (
        <Card>
          <Upload.Dragger
            accept=".xlsx,.xls"
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
            <p className="ant-upload-text">Kéo tệp Excel vào đây hoặc bấm để chọn</p>
            <p className="ant-upload-hint">
              Dòng đầu tiên của sheet phải là tiêu đề cột. Bước này chỉ đọc tệp, chưa ghi gì vào cơ sở
              dữ liệu.
            </p>
          </Upload.Dragger>
        </Card>
      )}

      {step === 1 && preview && (
        <Space direction="vertical" size={16} style={{ width: '100%' }}>
          <Card size="small">
            <Descriptions size="small" column={{ xs: 1, md: 3 }}>
              <Descriptions.Item label="Số cột">{preview.columns.length}</Descriptions.Item>
              <Descriptions.Item label="Số dòng dữ liệu">{preview.totalRows}</Descriptions.Item>
              <Descriptions.Item label="Cột đã ánh xạ">{mapping.length}</Descriptions.Item>
            </Descriptions>

            {profiles.data && profiles.data.length > 0 && (
              <Space style={{ marginTop: 12 }}>
                <Typography.Text type="secondary">Dùng hồ sơ ánh xạ đã lưu</Typography.Text>
                <Select
                  options={profiles.data.map((profile) => ({
                    value: profile.id,
                    label: `${profile.name}${profile.isDefault ? ' (mặc định)' : ''}`,
                  }))}
                  onChange={(value) => {
                    const profile = profiles.data!.find((item) => item.id === value);
                    if (profile) {
                      setMapping(profile.mapping);
                      message.success(`Đã áp dụng hồ sơ "${profile.name}".`);
                    }
                  }}
                  placeholder="Chọn hồ sơ"
                  style={{ width: 260 }}
                  allowClear
                />
              </Space>
            )}
          </Card>

          {!titleMapped && (
            <Alert
              type="warning"
              showIcon
              message="Chưa ánh xạ cột nào sang nhan đề (245$a)"
              description="Biểu ghi không có nhan đề thì không lưu được, nên phải chọn một cột làm nhan đề trước khi nhập."
            />
          )}

          <Table
            rowKey="column"
            size="small"
            dataSource={preview.columns.map((column) => ({
              column,
              mapping: mapping.find((item) => item.column === column),
              sample: preview.sampleRows[0]?.[column] ?? '',
            }))}
            pagination={false}
            columns={[
              {
                title: 'Cột trong tệp',
                dataIndex: 'column',
                width: 220,
                render: (value: string) => <Typography.Text strong>{value}</Typography.Text>,
              },
              {
                title: 'Giá trị dòng đầu',
                dataIndex: 'sample',
                render: (value: string) => (
                  <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                    {value}
                  </Typography.Text>
                ),
              },
              {
                title: 'Trường MARC',
                width: 260,
                render: (_, row) => (
                  <Select
                    value={row.mapping?.tag}
                    onChange={(value) =>
                      value
                        ? setColumnMapping(row.column, { tag: value })
                        : clearColumn(row.column)
                    }
                    options={(definitions.data ?? [])
                      .filter((field) => !field.isControl)
                      .map((field) => ({ value: field.tag, label: `${field.tag} — ${field.name}` }))}
                    placeholder="Không nhập cột này"
                    allowClear
                    showSearch
                    optionFilterProp="label"
                    style={{ width: '100%' }}
                  />
                ),
              },
              {
                title: 'Trường con',
                width: 200,
                render: (_, row) => {
                  const field = (definitions.data ?? []).find((item) => item.tag === row.mapping?.tag);

                  return (
                    <Select
                      value={row.mapping?.subfield}
                      onChange={(value) => setColumnMapping(row.column, { subfield: value })}
                      options={(field?.subfields ?? []).map((subfield) => ({
                        value: subfield.code,
                        label: `$${subfield.code} — ${subfield.name}`,
                      }))}
                      placeholder={row.mapping?.tag ? 'Chọn trường con' : '—'}
                      disabled={!row.mapping?.tag}
                      showSearch
                      optionFilterProp="label"
                      style={{ width: '100%' }}
                    />
                  );
                },
              },
              {
                title: 'Ký tự tách',
                width: 150,
                render: (_, row) => (
                  <Input
                    value={row.mapping?.separator ?? ''}
                    onChange={(event) => setColumnMapping(row.column, { separator: event.target.value })}
                    placeholder="Ví dụ: ;"
                    disabled={!row.mapping?.tag}
                    style={{ ...MONOSPACE, width: '100%' }}
                  />
                ),
              },
            ]}
          />

          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Ký tự tách dùng khi một ô chứa nhiều giá trị — ví dụ ba đề mục chủ đề ngăn nhau bằng dấu
            chấm phẩy sẽ thành ba trường 650 riêng.
          </Typography.Text>

          <Space>
            <Button onClick={() => setStep(0)}>Quay lại</Button>
            <Button icon={<SaveOutlined />} onClick={() => setSaveProfileOpen(true)} disabled={mapping.length === 0}>
              Lưu hồ sơ ánh xạ
            </Button>
            <Button type="primary" onClick={() => setStep(2)} disabled={!titleMapped}>
              Tiếp tục
            </Button>
          </Space>
        </Space>
      )}

      {step === 2 && preview && (
        <Card title="Tùy chọn nhập">
          <Space direction="vertical" size={20} style={{ width: '100%' }}>
            <Space size={16} wrap align="start">
              <div style={{ width: 220 }}>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  Đối chiếu trùng theo
                </Typography.Text>
                <Select
                  value={matchBy}
                  onChange={setMatchBy}
                  options={(Object.keys(MATCH_BY_LABELS) as DuplicateMatchBy[]).map((value) => ({
                    value,
                    label: MATCH_BY_LABELS[value],
                  }))}
                  style={{ width: '100%' }}
                />
              </div>

              <div style={{ width: 240 }}>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  Khi trùng thì
                </Typography.Text>
                <Select
                  value={onDuplicate}
                  onChange={setOnDuplicate}
                  options={(Object.keys(DUPLICATE_ACTION_LABELS) as DuplicateAction[]).map((value) => ({
                    value,
                    label: DUPLICATE_ACTION_LABELS[value].title,
                  }))}
                  style={{ width: '100%' }}
                />
                <Typography.Text type="secondary" style={{ fontSize: 11 }}>
                  {DUPLICATE_ACTION_LABELS[onDuplicate].hint}
                </Typography.Text>
              </div>

              <div style={{ width: 220 }}>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  Dạng tài liệu
                </Typography.Text>
                <Select
                  value={documentTypeId}
                  onChange={setDocumentTypeId}
                  options={toOptions(documentTypes.data)}
                  placeholder="Không gán"
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
                  value={status}
                  onChange={setStatus}
                  options={Object.entries(RECORD_STATUS_LABELS).map(([value, label]) => ({
                    value: value as RecordStatus,
                    label,
                  }))}
                  style={{ width: '100%' }}
                />
              </div>
            </Space>

            <Checkbox checked={addToQueue} onChange={(event) => setAddToQueue(event.target.checked)}>
              Đưa vào hàng đợi biên mục chi tiết
              <Typography.Text type="secondary" style={{ fontSize: 12, display: 'block' }}>
                Biểu ghi nhập từ bảng tính thường mới chỉ có mức mô tả tối thiểu.
              </Typography.Text>
            </Checkbox>

            <Space>
              <Button onClick={() => setStep(1)}>Quay lại</Button>
              <Button type="primary" loading={start.isPending} onClick={() => start.mutate()}>
                Bắt đầu nhập {preview.totalRows} dòng
              </Button>
            </Space>
          </Space>
        </Card>
      )}

      {step === 3 && job.data && <ExcelJobResult job={job.data} />}

      <Modal
        open={saveProfileOpen}
        title="Lưu hồ sơ ánh xạ"
        okText="Lưu"
        cancelText="Hủy"
        confirmLoading={saveProfile.isPending}
        onCancel={() => setSaveProfileOpen(false)}
        onOk={() => saveProfile.mutate()}
      >
        <Space direction="vertical" style={{ width: '100%' }}>
          <Typography.Text type="secondary">
            Lần sau nhận tệp cùng khuôn từ nguồn này, chọn lại hồ sơ là xong.
          </Typography.Text>
          <Input
            value={profileName}
            onChange={(event) => setProfileName(event.target.value)}
            placeholder="Ví dụ: Danh mục nhà cung cấp Fahasa"
          />
        </Space>
      </Modal>
    </Space>
  );
}

function ExcelJobResult({ job }: { job: ImportJob }) {
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
        <Descriptions size="small" column={{ xs: 1, md: 4 }} bordered>
          <Descriptions.Item label="Tổng số dòng">{job.total}</Descriptions.Item>
          <Descriptions.Item label="Thành công">{job.success}</Descriptions.Item>
          <Descriptions.Item label="Bỏ qua vì trùng">{job.skipped}</Descriptions.Item>
          <Descriptions.Item label="Lỗi">{job.failed}</Descriptions.Item>
        </Descriptions>

        {job.errors.length > 0 ? (
          <Table
            rowKey={(row) => `${row.row}-${row.message}`}
            size="small"
            dataSource={job.errors}
            pagination={{ pageSize: 10 }}
            columns={[
              { title: 'Dòng trong bảng tính', dataIndex: 'row', width: 180 },
              { title: 'Lý do', dataIndex: 'message' },
            ]}
          />
        ) : (
          <Empty description="Không có dòng nào lỗi" image={null} />
        )}
      </Space>
    </Card>
  );
}
