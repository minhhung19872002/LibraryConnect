import { useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import {
  App,
  Button,
  Card,
  Col,
  Input,
  Modal,
  Row,
  Select,
  Space,
  Spin,
  Switch,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import {
  ArrowLeftOutlined,
  BarcodeOutlined,
  CloudDownloadOutlined,
  ReadOutlined,
  SafetyCertificateOutlined,
  SaveOutlined,
  SnippetsOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PageHeader } from '@/components/PageHeader';
import { errorMessage } from '@/api/formErrors';
import { ApiRequestError } from '@/api/client';
import { MarcEditor, MarcValidationSummary } from '@/modules/marc/MarcEditor';
import { marcApi } from '@/modules/marc/api';
import { formatRecordAsText } from '@/modules/marc/marcRecord';
import type { MarcPreview, MarcRecord, MarcValidationResult } from '@/modules/marc/types';
import type { RemoteSearchField } from '@/modules/interlibrary/types';
import { RemoteRecordPicker } from './RemoteRecordPicker';
import { catalogingApi, parseMarc } from './api';
import { useCatalogOptions, toOptions } from './useCatalogOptions';
import { RECORD_STATUS_LABELS, type RecordStatus } from './types';

const MONOSPACE = { fontFamily: 'ui-monospace, Consolas, monospace' } as const;

/**
 * Soạn biểu ghi thư mục (II.2, II.3).
 *
 * The MARC editor is the whole screen because that is what cataloguing is; the few things that are
 * not part of the record — document type, collections, publication status — sit in a narrow column
 * beside it rather than above, so the record stays in view while they are set.
 *
 * Ctrl+S saves, which is how cataloguers work: they type a record, save, and start the next one
 * without reaching for the mouse.
 */
export function BibEditorPage() {
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const isEdit = Boolean(id);

  const [record, setRecord] = useState<MarcRecord | null>(null);
  const [documentTypeId, setDocumentTypeId] = useState<string | undefined>(
    searchParams.get('documentTypeId') ?? undefined,
  );
  const [templateId, setTemplateId] = useState<string | undefined>();
  const [collectionIds, setCollectionIds] = useState<string[]>([]);
  const [status, setStatus] = useState<RecordStatus>('Published');
  const [changeNote, setChangeNote] = useState('');
  const [validation, setValidation] = useState<MarcValidationResult | null>(null);
  const [templateName, setTemplateName] = useState<string | null>(null);

  const documentTypes = useCatalogOptions('document-types');
  const collections = useCatalogOptions('collections');

  const definitions = useQuery({
    queryKey: ['marc-fields', '', false],
    queryFn: () => marcApi.getFields(),
    staleTime: 10 * 60 * 1000,
  });

  const templates = useQuery({
    queryKey: ['marc-templates', documentTypeId],
    queryFn: () => catalogingApi.templates(documentTypeId),
    enabled: !isEdit,
  });

  const existing = useQuery({
    queryKey: ['bib-record', id],
    queryFn: () => catalogingApi.get(id!),
    enabled: isEdit,
  });

  // Biểu ghi gửi kèm khi điều hướng từ trang tra cứu liên thư viện: bỏ qua bước chọn dạng tài liệu
  // vì khung biểu ghi đã có sẵn từ thư viện nguồn.
  const handedOver = (useLocation().state as { marcJson?: string } | null)?.marcJson;

  // Rebuilding the skeleton after the cataloguer has started typing would throw their work away,
  // so it is only fetched while the chooser is still open.
  const [started, setStarted] = useState(isEdit || Boolean(handedOver));

  const blank = useQuery({
    queryKey: ['bib-blank', documentTypeId, templateId],
    queryFn: () => catalogingApi.blank(documentTypeId, templateId),
    enabled: !isEdit && !started && !handedOver,
  });

  useEffect(() => {
    if (!isEdit && handedOver) {
      setRecord(parseMarc(handedOver));
    }
  }, [handedOver, isEdit]);

  useEffect(() => {
    if (isEdit && existing.data) {
      setRecord(parseMarc(existing.data.marcJson));
      setDocumentTypeId(existing.data.documentTypeId ?? undefined);
      setCollectionIds(existing.data.collectionIds);
      setStatus(existing.data.status);
    }
  }, [isEdit, existing.data]);

  useEffect(() => {
    if (!isEdit && blank.data) {
      setRecord(parseMarc(blank.data.marcJson));
      setTemplateName(blank.data.templateName ?? null);
    }
  }, [isEdit, blank.data]);

  const save = useMutation({
    mutationFn: async () => {
      const payload = {
        marcJson: JSON.stringify(record),
        documentTypeId: documentTypeId ?? null,
        collectionIds,
        status,
        changeNote: changeNote.trim() || null,
      };

      return isEdit ? catalogingApi.update(id!, payload) : catalogingApi.create(payload);
    },
    onSuccess: async (result) => {
      message.success(
        isEdit ? 'Đã cập nhật biểu ghi.' : `Đã lưu biểu ghi ${result.controlNumber}.`,
      );

      await queryClient.invalidateQueries({ queryKey: ['bib-records'] });
      await queryClient.invalidateQueries({ queryKey: ['bib-record', result.id] });

      navigate(`/bien-muc/${result.id}`);
    },
    onError: (error: unknown) => {
      if (error instanceof ApiRequestError && error.errors.length > 0) {
        // The server returns one error per MARC field, so they are shown the same way the editor
        // shows its own validation instead of being flattened into a single toast.
        setValidation({
          isValid: false,
          errorCount: error.errors.length,
          warningCount: 0,
          issues: error.errors.map((item) => ({
            severity: 'Error' as const,
            message: item.message,
            tag: item.field,
          })),
        });
      }

      message.error(errorMessage(error));
    },
  });

  const validate = useMutation({
    mutationFn: () => marcApi.validate(record!),
    onSuccess: (result) => {
      setValidation(result);

      if (result.isValid && result.warningCount === 0) {
        message.success('Biểu ghi hợp lệ.');
      }
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 's') {
        event.preventDefault();

        if (record && !save.isPending) {
          save.mutate();
        }
      }
    };

    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [record, save]);

  const preview = useMemo(() => (record ? formatRecordAsText(record) : ''), [record]);

  const [isbd, setIsbd] = useState<MarcPreview | null>(null);
  const [pickerField, setPickerField] = useState<RemoteSearchField | null>(null);

  // Lưu biểu ghi đang soạn thành mẫu biên mục (II.5): cách tự nhiên nhất để có một mẫu là soạn
  // một biểu ghi ưng ý rồi giữ lại khung của nó.
  const [templateOpen, setTemplateOpen] = useState(false);
  const [templateForm, setTemplateForm] = useState({ name: '', isDefault: false, keepValues: false });

  const saveAsTemplate = useMutation({
    mutationFn: () =>
      catalogingApi.saveTemplate(null, {
        name: templateForm.name.trim(),
        documentTypeId: documentTypeId ?? null,
        isDefault: templateForm.isDefault,
        isActive: true,
        fields: JSON.stringify(record),
        clearValues: !templateForm.keepValues,
      }),
    onSuccess: async () => {
      message.success(`Đã lưu mẫu biên mục "${templateForm.name.trim()}".`);
      setTemplateOpen(false);
      await queryClient.invalidateQueries({ queryKey: ['marc-templates'] });
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  // Đọc soát mô tả thư mục **trước khi lưu** (II.2): trước đây phải lưu xuống rồi mới xem được,
  // nghĩa là lưu rồi mới biết nó đọc sai chỗ nào.
  const describe = useMutation({
    mutationFn: () => marcApi.preview(record!),
    onSuccess: setIsbd,
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  if ((isEdit && existing.isLoading) || (!isEdit && blank.isLoading) || !record) {
    return (
      <Card>
        <Spin tip="Đang tải biểu ghi...">
          <div style={{ height: 120 }} />
        </Spin>
      </Card>
    );
  }

  if (!started) {
    return (
      <Space direction="vertical" size={16} style={{ width: '100%' }}>
        <PageHeader
          title="Biên mục mới"
          description="Chọn dạng tài liệu và mẫu biên mục trước, vì đó là thứ quyết định khung biểu ghi."
        />

        <Card style={{ maxWidth: 640 }}>
          <Space direction="vertical" size={16} style={{ width: '100%' }}>
            <div>
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                Dạng tài liệu
              </Typography.Text>
              <Select
                value={documentTypeId}
                onChange={(value) => {
                  setDocumentTypeId(value);
                  setTemplateId(undefined);
                }}
                options={toOptions(documentTypes.data)}
                placeholder="Chọn dạng tài liệu"
                allowClear
                showSearch
                optionFilterProp="label"
                style={{ width: '100%' }}
              />
            </div>

            <div>
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                Mẫu biên mục
              </Typography.Text>
              <Select
                value={templateId}
                onChange={setTemplateId}
                options={(templates.data ?? []).map((template) => ({
                  value: template.id,
                  label: `${template.name}${template.isDefault ? ' (mặc định)' : ''} \u2014 ${template.fieldCount} trường`,
                }))}
                placeholder="Dùng mẫu mặc định"
                allowClear
                style={{ width: '100%' }}
              />
            </div>

            <Typography.Text type="secondary">
              {templateName
                ? `Khung biểu ghi lấy từ mẫu "${templateName}", các trường có giá trị ngầm định được điền sẵn.`
                : 'Chưa có mẫu biên mục nào phù hợp; khung biểu ghi sẽ chỉ gồm các trường bắt buộc.'}
            </Typography.Text>

            <Space>
              <Button onClick={() => navigate('/bien-muc')}>Hủy</Button>
              <Button type="primary" onClick={() => setStarted(true)}>
                Bắt đầu biên mục
              </Button>
            </Space>
          </Space>
        </Card>
      </Space>
    );
  }

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title={isEdit ? `Sửa biểu ghi ${existing.data?.controlNumber ?? ''}` : 'Biên mục mới'}
        description={
          isEdit
            ? 'Phiên bản trước được lưu lại trước khi ghi đè, có thể xem so sánh và khôi phục.'
            : templateName
              ? `Khung biểu ghi lấy từ mẫu "${templateName}", các trường có giá trị ngầm định đã được điền sẵn.`
              : 'Chưa có mẫu biên mục cho dạng tài liệu này nên khung biểu ghi để trống.'
        }
        actions={
          <Space wrap>
            <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/bien-muc')}>
              Về danh sách
            </Button>
            <Button icon={<CloudDownloadOutlined />} onClick={() => setPickerField('Any')}>
              Lấy từ Z39.50
            </Button>
            <Button icon={<BarcodeOutlined />} onClick={() => setPickerField('Isbn')}>
              Lấy từ ISBN
            </Button>
            <Button
              icon={<ReadOutlined />}
              loading={describe.isPending}
              onClick={() => describe.mutate()}
            >
              Xem trước ISBD
            </Button>
            <Button
              icon={<SafetyCertificateOutlined />}
              loading={validate.isPending}
              onClick={() => validate.mutate()}
            >
              Kiểm tra
            </Button>
            <Tooltip title="Giữ lại khung trường của biểu ghi này làm mẫu biên mục">
              <Button icon={<SnippetsOutlined />} onClick={() => setTemplateOpen(true)}>
                Lưu thành mẫu
              </Button>
            </Tooltip>
            <Tooltip title="Ctrl + S">
              <Button
                type="primary"
                icon={<SaveOutlined />}
                loading={save.isPending}
                onClick={() => save.mutate()}
              >
                Lưu biểu ghi
              </Button>
            </Tooltip>
          </Space>
        }
      />

      {validation && <MarcValidationSummary issues={validation.issues} isValid={validation.isValid} />}

      <Row gutter={16}>
        <Col xs={24} xl={18}>
          <MarcEditor
            record={record}
            onChange={(next) => {
              setRecord(next);
              setValidation(null);
            }}
            definitions={definitions.data ?? []}
            issues={validation?.issues ?? []}
          />
        </Col>

        <Col xs={24} xl={6}>
          <Space direction="vertical" size={12} style={{ width: '100%' }}>
            <Card size="small" title="Thông tin quản lý">
              <Space direction="vertical" size={10} style={{ width: '100%' }}>
                <div>
                  <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                    Dạng tài liệu
                  </Typography.Text>
                  {/* Only a property of the record now: the skeleton has already been built. */}
                  <Select
                    value={documentTypeId}
                    onChange={setDocumentTypeId}
                    options={toOptions(documentTypes.data)}
                    placeholder="Chọn dạng tài liệu"
                    allowClear
                    showSearch
                    optionFilterProp="label"
                    style={{ width: '100%' }}
                  />
                </div>

                <div>
                  <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                    Bộ sưu tập
                  </Typography.Text>
                  <Select
                    mode="multiple"
                    value={collectionIds}
                    onChange={setCollectionIds}
                    options={toOptions(collections.data)}
                    placeholder="Không thuộc bộ sưu tập nào"
                    style={{ width: '100%' }}
                    showSearch
                    optionFilterProp="label"
                  />
                </div>

                <div>
                  <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                    Trạng thái biểu ghi
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

                {isEdit && (
                  <div>
                    <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                      Ghi chú thay đổi
                    </Typography.Text>
                    <Input.TextArea
                      rows={2}
                      value={changeNote}
                      onChange={(event) => setChangeNote(event.target.value)}
                      placeholder="Ghi lại vì sao sửa, để người sau đọc lịch sử hiểu được"
                      maxLength={500}
                    />
                  </div>
                )}
              </Space>
            </Card>

            {isEdit && existing.data && (
              <Card size="small" title="Biểu ghi này">
                <Space direction="vertical" size={4} style={{ width: '100%' }}>
                  <Typography.Text style={MONOSPACE}>{existing.data.controlNumber}</Typography.Text>
                  <Space size={4} wrap>
                    <Tag>{existing.data.itemCount} bản</Tag>
                    <Tag>{existing.data.versionCount} phiên bản</Tag>
                    {existing.data.loanCount > 0 && <Tag>{existing.data.loanCount} lượt mượn</Tag>}
                  </Space>
                  {existing.data.createdByName && (
                    <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                      Người tạo: {existing.data.createdByName}
                    </Typography.Text>
                  )}
                </Space>
              </Card>
            )}

            {isbd && (
              <Card
                size="small"
                title="Mô tả thư mục (ISBD)"
                extra={
                  <Button type="link" size="small" onClick={() => setIsbd(null)}>
                    Ẩn
                  </Button>
                }
              >
                <Space direction="vertical" size={8} style={{ width: '100%' }}>
                  {isbd.isbd.map((area) => (
                    <div key={area.label}>
                      <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                        {area.label}
                      </Typography.Text>
                      <Typography.Paragraph style={{ marginBottom: 0 }}>{area.content}</Typography.Paragraph>
                    </div>
                  ))}

                  <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                    Gộp một đoạn, đúng cách nó lên phích mục lục
                  </Typography.Text>
                  <Typography.Paragraph style={{ marginBottom: 0 }}>{isbd.paragraph}</Typography.Paragraph>
                </Space>
              </Card>
            )}

            <Card size="small" title="Biểu ghi ở dạng văn bản">
              <Typography.Paragraph
                style={{ ...MONOSPACE, whiteSpace: 'pre-wrap', fontSize: 12, marginBottom: 0 }}
              >
                {preview}
              </Typography.Paragraph>
            </Card>
          </Space>
        </Col>
      </Row>

      <Modal
        open={templateOpen}
        title="Lưu biểu ghi này thành mẫu biên mục"
        okText="Lưu mẫu"
        cancelText="Hủy"
        confirmLoading={saveAsTemplate.isPending}
        onCancel={() => setTemplateOpen(false)}
        onOk={() => {
          if (!templateForm.name.trim()) {
            message.error('Chưa đặt tên cho mẫu.');
            return;
          }

          saveAsTemplate.mutate();
        }}
      >
        <Space direction="vertical" size={12} style={{ width: '100%' }}>
          <div>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              Tên mẫu
            </Typography.Text>
            <Input
              value={templateForm.name}
              onChange={(event) => setTemplateForm((current) => ({ ...current, name: event.target.value }))}
              placeholder="Ví dụ: Luận văn thạc sĩ"
              maxLength={200}
              autoFocus
            />
          </div>

          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Mẫu áp dụng cho dạng tài liệu đang chọn ở cột bên phải
            {documentTypeId ? '' : ' (chưa chọn: áp dụng cho mọi dạng)'}.
          </Typography.Text>

          <Space size={16}>
            <Space size={6}>
              <Switch
                checked={templateForm.keepValues}
                onChange={(checked) => setTemplateForm((current) => ({ ...current, keepValues: checked }))}
              />
              <Typography.Text>Giữ cả nội dung các trường</Typography.Text>
            </Space>
            <Space size={6}>
              <Switch
                checked={templateForm.isDefault}
                onChange={(checked) => setTemplateForm((current) => ({ ...current, isDefault: checked }))}
              />
              <Typography.Text>Đặt làm mẫu mặc định</Typography.Text>
            </Space>
          </Space>

          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Tắt "giữ nội dung" thì mẫu chỉ còn khung: nhãn trường, chỉ thị và mã trường con.
          </Typography.Text>
        </Space>
      </Modal>

      <RemoteRecordPicker
        open={pickerField !== null}
        initialField={pickerField ?? 'Any'}
        onClose={() => setPickerField(null)}
        onPicked={(next) => {
          setRecord(next);
          setValidation(null);
          setIsbd(null);
        }}
      />
    </Space>
  );
}
