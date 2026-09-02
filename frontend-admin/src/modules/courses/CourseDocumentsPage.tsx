import { useState } from 'react';
import {
  App,
  Button,
  Card,
  Col,
  Divider,
  Empty,
  Input,
  List,
  Modal,
  Popconfirm,
  Row,
  Select,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
  Upload,
} from 'antd';
import {
  DeleteOutlined,
  DownloadOutlined,
  PlusOutlined,
  UploadOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import { clickable } from '@/components/clickable';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { api, ApiRequestError } from '@/api/client';
import type { PagedResult } from '@/types/api';
import type { CatalogItem } from '@/modules/catalogs/types';
import { coursesApi } from './api';
import {
  RELATION_OPTIONS,
  type CourseDocument,
  type CourseImportResult,
  type CourseRelationType,
  type CourseRow,
} from './types';
import { downloadFile } from '@/api/download';
import { MAU } from '@/lib/palette';

const { Paragraph } = Typography;

/** Một dòng kết quả tra cứu biểu ghi ở cột phải. */
interface BibRow {
  id: string;
  title: string;
  authorMain?: string;
  publishYear?: number;
  isbn?: string;
  itemCount: number;
  availableItemCount: number;
}

/**
 * X.3 — Gán tài liệu cho môn học.
 *
 * Hai cột đúng như đặc tả mô tả: chọn môn ở bên trái, tìm và gán tài liệu ở bên phải. Cán bộ làm
 * việc này theo từng môn một, nên môn đang chọn phải luôn nhìn thấy được trong lúc tra cứu tài liệu.
 */
export function CourseDocumentsPage() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [keyword, setKeyword] = useState('');
  const [majorId, setMajorId] = useState<string | undefined>();
  const [withoutDocuments, setWithoutDocuments] = useState(false);
  const [selected, setSelected] = useState<CourseRow | null>(null);

  const [bibKeyword, setBibKeyword] = useState('');
  const [checked, setChecked] = useState<string[]>([]);
  const [relation, setRelation] = useState<CourseRelationType>('RequiredReference');
  const [note, setNote] = useState('');

  const [majorsOpen, setMajorsOpen] = useState(false);
  const [majorDraft, setMajorDraft] = useState<string[]>([]);
  const [importResult, setImportResult] = useState<CourseImportResult | null>(null);

  const majors = useQuery({
    queryKey: ['catalog', 'majors'],
    queryFn: () =>
      api.get<PagedResult<CatalogItem>>('/catalogs/majors/items', {
        params: { page: 1, pageSize: 200, isActive: true },
      }),
  });

  const courses = useQuery({
    queryKey: ['courses', keyword, majorId, withoutDocuments],
    queryFn: () =>
      coursesApi.list({
        keyword: keyword || undefined,
        majorId,
        withoutDocuments: withoutDocuments || undefined,
        isActive: true,
        page: 1,
        pageSize: 200,
      }),
  });

  const documents = useQuery({
    queryKey: ['course-documents', selected?.id],
    queryFn: () => coursesApi.documents(selected!.id),
    enabled: Boolean(selected),
  });

  const bibs = useQuery({
    queryKey: ['bib-search', bibKeyword],
    queryFn: () =>
      api.get<PagedResult<BibRow>>('/cataloging/bibs', {
        params: { keyword: bibKeyword, page: 1, pageSize: 20 },
      }),
    enabled: bibKeyword.trim().length >= 2,
  });

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['courses'] });
    void queryClient.invalidateQueries({ queryKey: ['course-documents'] });
    void queryClient.invalidateQueries({ queryKey: ['course-report'] });
  };

  const assign = useMutation({
    mutationFn: () => coursesApi.assign(selected!.id, checked, relation, note || undefined),
    onSuccess: (added) => {
      message.success(
        added === checked.length
          ? `Đã gán ${added} tài liệu cho môn ${selected?.name}.`
          : `Đã gán ${added} tài liệu mới, các tài liệu còn lại được cập nhật mức độ.`,
      );
      setChecked([]);
      setNote('');
      refresh();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không gán được.'),
  });

  const updateDocument = useMutation({
    mutationFn: (values: { id: string; relationType: CourseRelationType }) =>
      coursesApi.updateDocument(values.id, values.relationType),
    onSuccess: () => {
      message.success('Đã đổi mức độ liên quan.');
      refresh();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  const removeDocument = useMutation({
    mutationFn: (id: string) => coursesApi.removeDocument(id),
    onSuccess: () => {
      message.success('Đã bỏ tài liệu khỏi môn học.');
      refresh();
    },
  });

  const saveMajors = useMutation({
    mutationFn: () => coursesApi.setMajors(selected!.id, majorDraft),
    onSuccess: () => {
      message.success('Đã lưu danh sách ngành của môn học.');
      setMajorsOpen(false);
      refresh();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  const runImport = useMutation({
    mutationFn: (values: { file: File; dryRun: boolean }) =>
      coursesApi.import(values.file, values.dryRun),
    onSuccess: (result, values) => {
      setImportResult(result);

      if (values.dryRun) {
        message.info(`Đã kiểm tra ${result.totalRows} dòng, ${result.failedRows} dòng có lỗi.`);
      } else {
        message.success(`Đã nhập ${result.successRows} dòng.`);
        refresh();
      }
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không đọc được tệp.'),
  });

  const documentColumns: ColumnsType<CourseDocument> = [
    { title: 'Nhan đề', dataIndex: 'title', width: 300 },
    { title: 'Tác giả', dataIndex: 'authorMain', width: 170 },
    {
      title: 'Mức độ',
      dataIndex: 'relationType',
      width: 230,
      render: (value: CourseRelationType, row) => (
        <Can permission={PERMISSIONS.course.documentLink} mode="disable">
          <Select
            size="small"
            value={value}
            style={{ width: 210 }}
            options={RELATION_OPTIONS.map((option) => ({
              value: option.value,
              label: option.label,
            }))}
            onChange={(next) => updateDocument.mutate({ id: row.id, relationType: next })}
          />
        </Can>
      ),
    },
    {
      title: 'Trong kho',
      dataIndex: 'availableItemCount',
      width: 140,
      render: (available: number, row) =>
        available > 0 ? (
          <Tag color="green">Còn {available} bản</Tag>
        ) : row.itemCount > 0 ? (
          // Có bản in nhưng không bản nào sẵn sàng: có thể đang cho mượn hết, mà cũng có thể chưa
          // kiểm nhận hoặc đang khóa. Nói "hết bản rảnh" là đoán sai một nửa số trường hợp.
          <Tooltip title="Bản in đang được mượn, chưa kiểm nhận hoặc đang bị khóa">
            <Tag color="orange">Chưa có bản sẵn sàng</Tag>
          </Tooltip>
        ) : (
          <Tag>Chưa có bản in</Tag>
        ),
    },
    { title: 'Ghi chú', dataIndex: 'note', width: 200 },
    {
      title: '',
      dataIndex: 'id',
      width: 60,
      render: (id: string) => (
        <Can permission={PERMISSIONS.course.documentLink}>
          <Popconfirm
            title="Bỏ tài liệu này khỏi môn học?"
            okText="Bỏ"
            cancelText="Không"
            onConfirm={() => removeDocument.mutate(id)}
          >
            <Button size="small" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Can>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Tài liệu môn học"
        description="Gán giáo trình và tài liệu tham khảo cho từng môn học; bạn đọc duyệt theo ngành và môn trên trang tra cứu."
        actions={
          <Space>
            <Button
              icon={<DownloadOutlined />}
              onClick={() => downloadFile('/courses/documents/import/template', 'mau-tai-lieu-mon-hoc.xlsx')}
            >
              Tải tệp mẫu
            </Button>
            <Can permission={PERMISSIONS.course.documentLink}>
              <Upload
                accept=".xlsx,.xls"
                showUploadList={false}
                beforeUpload={(file) => {
                  runImport.mutate({ file, dryRun: true });
                  return false;
                }}
              >
                <Button icon={<UploadOutlined />} loading={runImport.isPending}>
                  Nhập từ Excel
                </Button>
              </Upload>
            </Can>
          </Space>
        }
      />

      <Row gutter={16}>
        <Col xs={24} lg={9}>
          <Card title="Môn học" size="small">
            <Space direction="vertical" style={{ width: '100%', marginBottom: 12 }}>
              <Input.Search
                placeholder="Tìm theo mã môn, tên môn, giảng viên"
                allowClear
                onSearch={setKeyword}
              />
              <Select
                allowClear
                placeholder="Mọi ngành đào tạo"
                style={{ width: '100%' }}
                value={majorId}
                onChange={setMajorId}
                options={(majors.data?.items ?? []).map((item) => ({
                  value: item.id,
                  label: `${item.code} — ${item.name}`,
                }))}
              />
              <Button
                size="small"
                type={withoutDocuments ? 'primary' : 'default'}
                onClick={() => setWithoutDocuments((current) => !current)}
              >
                Chỉ môn chưa có tài liệu
              </Button>
            </Space>

            <List
              loading={courses.isLoading}
              dataSource={courses.data?.items ?? []}
              locale={{ emptyText: <Empty description="Không có môn học nào phù hợp." /> }}
              style={{ maxHeight: 560, overflowY: 'auto' }}
              renderItem={(course) => (
                <List.Item
                  {...clickable(() => {
                    setSelected(course);
                    setChecked([]);
                  }, `${course.code} — ${course.name}`)}
                  style={{
                    cursor: 'pointer',
                    padding: '10px 12px',
                    borderRadius: 6,
                    background: selected?.id === course.id ? MAU.chinhNhat : undefined,
                  }}
                >
                  <List.Item.Meta
                    title={`${course.code} — ${course.name}`}
                    description={
                      <Space size={[6, 4]} wrap>
                        <span>{course.credits} tín chỉ</span>
                        {course.semester ? <span>{course.semester}</span> : null}
                        {course.documentCount > 0 ? (
                          <Tag color="blue">{course.documentCount} tài liệu</Tag>
                        ) : (
                          <Tag color="orange">Chưa có tài liệu</Tag>
                        )}
                        {course.majors.map((major) => (
                          <Tooltip key={major.id} title={major.name}>
                            <Tag>{major.code}</Tag>
                          </Tooltip>
                        ))}
                      </Space>
                    }
                  />
                </List.Item>
              )}
            />
          </Card>
        </Col>

        <Col xs={24} lg={15}>
          {!selected ? (
            <Card size="small">
              <Empty description="Chọn một môn học ở bên trái để xem và gán tài liệu." />
            </Card>
          ) : (
            <Card
              size="small"
              title={`${selected.code} — ${selected.name}`}
              extra={
                <Can permission={PERMISSIONS.course.courseManage}>
                  <Button
                    size="small"
                    onClick={() => {
                      setMajorDraft(selected.majors.map((major) => major.id));
                      setMajorsOpen(true);
                    }}
                  >
                    Ngành đào tạo ({selected.majors.length})
                  </Button>
                </Can>
              }
            >
              <Table
                rowKey="id"
                size="small"
                loading={documents.isLoading}
                columns={documentColumns}
                dataSource={documents.data ?? []}
                pagination={false}
                scroll={{ x: 1100 }}
                locale={{ emptyText: 'Môn học này chưa được gán tài liệu nào.' }}
              />

              <Divider orientation="left" plain>
                Tìm và gán tài liệu
              </Divider>

              <Space direction="vertical" style={{ width: '100%' }}>
                <Input.Search
                  placeholder="Tìm theo nhan đề, tác giả, ISBN — gõ từ hai ký tự"
                  allowClear
                  onSearch={setBibKeyword}
                />

                <List
                  loading={bibs.isFetching}
                  dataSource={bibs.data?.items ?? []}
                  locale={{
                    emptyText:
                      bibKeyword.trim().length < 2
                        ? 'Nhập từ khóa để tìm tài liệu.'
                        : 'Không tìm thấy tài liệu nào.',
                  }}
                  style={{ maxHeight: 280, overflowY: 'auto' }}
                  renderItem={(bib) => {
                    const already = (documents.data ?? []).some((row) => row.bibId === bib.id);
                    const picked = checked.includes(bib.id);

                    return (
                      <List.Item
                        {...clickable(
                          () =>
                            setChecked((current) =>
                              picked
                                ? current.filter((id) => id !== bib.id)
                                : [...current, bib.id],
                            ),
                          bib.title,
                        )}
                        style={{
                          cursor: 'pointer',
                          padding: '8px 12px',
                          borderRadius: 6,
                          background: picked ? MAU.totNhat : undefined,
                        }}
                      >
                        <List.Item.Meta
                          title={bib.title}
                          description={
                            <Space size={[8, 4]} wrap>
                              <span>{bib.authorMain}</span>
                              {bib.publishYear ? <span>{bib.publishYear}</span> : null}
                              {bib.isbn ? <span>ISBN {bib.isbn}</span> : null}
                              {already ? <Tag color="blue">Đã gán cho môn này</Tag> : null}
                            </Space>
                          }
                        />
                      </List.Item>
                    );
                  }}
                />

                <Space wrap>
                  <Select
                    value={relation}
                    style={{ width: 240 }}
                    onChange={setRelation}
                    options={RELATION_OPTIONS.map((option) => ({
                      value: option.value,
                      label: option.label,
                    }))}
                  />
                  <Input
                    value={note}
                    placeholder="Ghi chú, ví dụ đọc chương 1–4"
                    style={{ width: 280 }}
                    onChange={(event) => setNote(event.target.value)}
                  />
                  <Can permission={PERMISSIONS.course.documentLink}>
                    <Tooltip title={checked.length === 0 ? 'Chọn tài liệu ở danh sách trên' : ''}>
                      <Button
                        type="primary"
                        icon={<PlusOutlined />}
                        disabled={checked.length === 0}
                        loading={assign.isPending}
                        onClick={() => assign.mutate()}
                      >
                        Gán {checked.length > 0 ? `${checked.length} tài liệu` : 'tài liệu'}
                      </Button>
                    </Tooltip>
                  </Can>
                </Space>
              </Space>
            </Card>
          )}
        </Col>
      </Row>

      <Modal
        open={majorsOpen}
        title={`Ngành đào tạo dạy môn ${selected?.name ?? ''}`}
        okText="Lưu"
        cancelText="Hủy"
        confirmLoading={saveMajors.isPending}
        onCancel={() => setMajorsOpen(false)}
        onOk={() => saveMajors.mutate()}
      >
        <Paragraph type="secondary">
          Một môn có thể do nhiều ngành cùng dạy. Bạn đọc duyệt theo ngành sẽ thấy môn này trong mọi
          ngành được chọn.
        </Paragraph>
        <Select
          mode="multiple"
          style={{ width: '100%' }}
          value={majorDraft}
          onChange={setMajorDraft}
          placeholder="Chọn ngành đào tạo"
          options={(majors.data?.items ?? []).map((item) => ({
            value: item.id,
            label: `${item.code} — ${item.name}`,
          }))}
        />
      </Modal>

      <Modal
        open={importResult !== null}
        title="Kết quả đọc tệp Excel"
        width={880}
        onCancel={() => setImportResult(null)}
        footer={
          importResult && importResult.successRows > 0 ? (
            <Space>
              <Button onClick={() => setImportResult(null)}>Đóng</Button>
              <Upload
                accept=".xlsx,.xls"
                showUploadList={false}
                beforeUpload={(file) => {
                  runImport.mutate({ file, dryRun: false });
                  return false;
                }}
              >
                <Button type="primary" loading={runImport.isPending}>
                  Chọn lại tệp và nhập thật
                </Button>
              </Upload>
            </Space>
          ) : (
            <Button onClick={() => setImportResult(null)}>Đóng</Button>
          )
        }
      >
        {importResult ? (
          <>
            <Space style={{ marginBottom: 12 }} wrap>
              <Tag>Tổng {importResult.totalRows} dòng</Tag>
              <Tag color="green">Hợp lệ {importResult.successRows}</Tag>
              <Tag color={importResult.failedRows > 0 ? 'red' : 'default'}>
                Lỗi {importResult.failedRows}
              </Tag>
            </Space>

            <Table
              rowKey="rowNumber"
              size="small"
              dataSource={importResult.rows}
              pagination={{ pageSize: 10 }}
              scroll={{ x: 760 }}
              columns={[
                { title: 'Dòng', dataIndex: 'rowNumber', width: 70 },
                { title: 'Mã môn', dataIndex: 'courseCode', width: 120 },
                { title: 'Mã tài liệu', dataIndex: 'bibKey', width: 180 },
                { title: 'Mức độ', dataIndex: 'relationType', width: 200 },
                {
                  title: 'Kết quả',
                  dataIndex: 'success',
                  width: 240,
                  render: (success: boolean, row) =>
                    success ? <Tag color="green">Hợp lệ</Tag> : <Tag color="red">{row.message}</Tag>,
                },
              ]}
            />
          </>
        ) : null}
      </Modal>
    </>
  );
}
