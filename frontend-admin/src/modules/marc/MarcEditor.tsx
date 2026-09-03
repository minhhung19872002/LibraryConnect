import { useEffect, useMemo, useRef, useState } from 'react';
import {
  Alert,
  AutoComplete,
  Button,
  Card,
  Collapse,
  Empty,
  Input,
  Modal,
  Popconfirm,
  Select,
  Space,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import {
  ArrowDownOutlined,
  ArrowUpOutlined,
  CopyOutlined,
  DeleteOutlined,
  HolderOutlined,
  InfoCircleOutlined,
  PlusOutlined,
  ScissorOutlined,
  SlidersOutlined,
  WarningOutlined,
} from '@ant-design/icons';
import type {
  MarcDataField,
  MarcFieldDefinition,
  MarcRecord,
  MarcValidationIssue,
} from './types';
import {
  addSubfield,
  buildFieldFromDefinition,
  displayIndicator,
  duplicateDataField,
  moveDataField,
  findDefinition,
  groupIssuesByField,
  insertDataField,
  issueKey,
  looksLikeSubfieldText,
  occurrenceNumbers,
  parseIndicator,
  parseSubfieldText,
  removeControlField,
  removeDataField,
  removeSubfield,
  setControlField,
  updateDataField,
  updateSubfield,
} from './marcRecord';
import { LeaderEditor } from './LeaderEditor';
import { Control008Wizard } from './Control008Wizard';
import { MAU } from '@/lib/palette';

interface MarcEditorProps {
  record: MarcRecord;
  onChange: (record: MarcRecord) => void;
  definitions: MarcFieldDefinition[];
  issues?: MarcValidationIssue[];
  readOnly?: boolean;
}

const MONOSPACE = { fontFamily: 'ui-monospace, Consolas, monospace' } as const;

/**
 * Trình soạn biểu ghi MARC 21.
 *
 * The editor works on the record as MARC actually is — a leader, control fields and repeatable data
 * fields with indicators and subfields — rather than on a flattened form. That is what the tender
 * requires and it is also what lets a cataloguer paste in a record from another library and see
 * exactly what arrived.
 *
 * Everything a cataloguer needs to know about a field comes from the definitions loaded from the
 * server: the Vietnamese name, whether the field repeats, the legal indicator values with their
 * meanings, and the subfield names. Nothing is hard-coded here, so a library that adds a local
 * field gets the same assistance for it.
 */
export function MarcEditor({ record, onChange, definitions, issues = [], readOnly }: MarcEditorProps) {
  const [newTag, setNewTag] = useState('');
  const [wizardOpen, setWizardOpen] = useState(false);
  const [dragIndex, setDragIndex] = useState<number | null>(null);

  /** Trường đang được gõ, để Ctrl+D biết nhân bản cái nào. */
  const focusedIndex = useRef<number | null>(null);

  // Ctrl+D nhân bản trường đang gõ (đặc tả II.2). Nghe ở cấp cửa sổ vì con trỏ lúc ấy nằm trong một
  // ô nhập bên trong trường, không phải trên chính khối trường.
  useEffect(() => {
    if (readOnly) {
      return undefined;
    }

    const onKeyDown = (event: KeyboardEvent) => {
      if (!(event.ctrlKey || event.metaKey) || event.key.toLowerCase() !== 'd') {
        return;
      }

      const index = focusedIndex.current;

      if (index === null) {
        return;
      }

      // Ctrl+D của trình duyệt là "đánh dấu trang" — ở màn hình biên mục thì nhân bản dòng hữu ích hơn.
      event.preventDefault();
      onChange(duplicateDataField(record, index));
    };

    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [onChange, readOnly, record]);

  const grouped = useMemo(() => groupIssuesByField(issues), [issues]);
  const occurrences = useMemo(() => occurrenceNumbers(record), [record]);

  const tagOptions = useMemo(
    () =>
      definitions
        .filter((definition) => !definition.isControl)
        .map((definition) => ({
          value: definition.tag,
          label: `${definition.tag} — ${definition.name}`,
        })),
    [definitions],
  );

  const controlTagOptions = useMemo(
    () =>
      definitions
        .filter((definition) => definition.isControl && definition.tag !== '001' && definition.tag !== '005')
        .map((definition) => ({
          value: definition.tag,
          label: `${definition.tag} — ${definition.name}`,
        })),
    [definitions],
  );

  const addField = (tag: string) => {
    const trimmed = tag.trim();

    if (!/^[0-9]{3}$/.test(trimmed)) {
      return;
    }

    const definition = findDefinition(definitions, trimmed);

    if (definition?.isControl || /^00[1-9]$/.test(trimmed)) {
      onChange(setControlField(record, trimmed, ''));
    } else if (definition) {
      onChange(insertDataField(record, buildFieldFromDefinition(definition)));
    } else {
      onChange(
        insertDataField(record, { tag: trimmed, ind1: ' ', ind2: ' ', subfields: [{ code: 'a', value: '' }] }),
      );
    }

    setNewTag('');
  };

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <Collapse
        items={[
          {
            key: 'leader',
            label: (
              <Space>
                <Typography.Text strong>Đầu biểu (Leader)</Typography.Text>
                <Typography.Text type="secondary" style={MONOSPACE}>
                  {record.leader}
                </Typography.Text>
              </Space>
            ),
            children: (
              <LeaderEditor
                leader={record.leader}
                onChange={(leader) => onChange({ ...record, leader })}
                readOnly={readOnly}
              />
            ),
          },
        ]}
      />

      <Card size="small" title="Trường điều khiển (001–009)" styles={{ body: { padding: 12 } }}>
        <Space direction="vertical" size={8} style={{ width: '100%' }}>
          {record.controlFields.length === 0 && (
            <Typography.Text type="secondary">Biểu ghi chưa có trường điều khiển nào.</Typography.Text>
          )}

          {record.controlFields.map((field) => {
            const definition = findDefinition(definitions, field.tag);
            const fieldIssues = grouped.get(issueKey(field.tag, 1)) ?? [];

            return (
              <div key={field.tag}>
                <Space align="start" style={{ width: '100%' }}>
                  <Tooltip title={definition?.name ?? 'Trường chưa khai báo trong bộ định nghĩa'}>
                    <Tag color="blue" style={{ ...MONOSPACE, width: 48, textAlign: 'center' }}>
                      {field.tag}
                    </Tag>
                  </Tooltip>
                  <Input
                    value={field.value}
                    onChange={(event) => onChange(setControlField(record, field.tag, event.target.value))}
                    disabled={readOnly || field.tag === '005'}
                    style={{ ...MONOSPACE, width: 480 }}
                    placeholder={
                      field.tag === '008'
                        ? 'Chuỗi 40 ký tự mã hóa'
                        : field.tag === '001'
                          ? 'Để trống thì hệ thống tự sinh khi lưu biểu ghi'
                          : undefined
                    }
                    suffix={
                      field.tag === '008' ? (
                        <Typography.Text type={field.value.length === 40 ? 'secondary' : 'danger'}>
                          {field.value.length}/40
                        </Typography.Text>
                      ) : undefined
                    }
                  />
                  {field.tag === '008' && (
                    <Button icon={<SlidersOutlined />} onClick={() => setWizardOpen(true)}>
                      Trình hướng dẫn
                    </Button>
                  )}
                  <Typography.Text type="secondary">{definition?.name}</Typography.Text>
                  {!readOnly && field.tag !== '001' && field.tag !== '008' && (
                    <Button
                      type="text"
                      danger
                      icon={<DeleteOutlined />}
                      onClick={() => onChange(removeControlField(record, field.tag))}
                    />
                  )}
                </Space>
                <FieldIssues issues={fieldIssues} />
              </div>
            );
          })}

          {!readOnly && controlTagOptions.length > 0 && (
            <Select<string>
              placeholder="Thêm trường điều khiển"
              options={controlTagOptions.filter(
                (option) => !record.controlFields.some((field) => field.tag === option.value),
              )}
              onChange={(value) => onChange(setControlField(record, value, ''))}
              value={undefined}
              style={{ width: 360 }}
              showSearch
              optionFilterProp="label"
            />
          )}
        </Space>
      </Card>

      <Card
        size="small"
        title="Trường dữ liệu (010–999)"
        styles={{ body: { padding: 12 } }}
        extra={
          !readOnly && (
            <Space.Compact>
              <AutoComplete
                value={newTag}
                onChange={setNewTag}
                options={tagOptions}
                style={{ width: 320 }}
                placeholder="Nhập nhãn trường hoặc tên trường, ví dụ 245"
                filterOption={(input, option) =>
                  (option?.label as string).toLowerCase().includes(input.toLowerCase())
                }
                onSelect={addField}
              />
              <Button type="primary" icon={<PlusOutlined />} onClick={() => addField(newTag)}>
                Thêm trường
              </Button>
            </Space.Compact>
          )
        }
      >
        {record.dataFields.length === 0 ? (
          <Empty description="Biểu ghi chưa có trường dữ liệu nào" />
        ) : (
          <Space direction="vertical" size={10} style={{ width: '100%' }}>
            {record.dataFields.map((field, index) => (
              <DataFieldRow
                key={`${field.tag}-${index}`}
                field={field}
                index={index}
                isDragging={dragIndex === index}
                canMoveUp={index > 0}
                canMoveDown={index < record.dataFields.length - 1}
                onFocusRow={() => {
                  focusedIndex.current = index;
                }}
                onDuplicate={() => onChange(duplicateDataField(record, index))}
                onMove={(to) => onChange(moveDataField(record, index, to))}
                onDragStart={() => setDragIndex(index)}
                onDragEnd={() => setDragIndex(null)}
                onDropOn={() => {
                  if (dragIndex !== null) {
                    onChange(moveDataField(record, dragIndex, index));
                  }
                  setDragIndex(null);
                }}
                definitions={definitions}
                issues={grouped.get(issueKey(field.tag, occurrences[index]!)) ?? []}
                readOnly={readOnly}
                onChange={(change) => onChange(updateDataField(record, index, change))}
                onChangeSubfield={(subfieldIndex, change) =>
                  onChange(updateSubfield(record, index, subfieldIndex, change))
                }
                onSplitSubfield={(subfieldIndex, text) => {
                  const parsed = parseSubfieldText(text);
                  const subfields = [...field.subfields];
                  subfields.splice(subfieldIndex, 1, ...parsed);
                  onChange(updateDataField(record, index, { subfields }));
                }}
                onAddSubfield={() => onChange(addSubfield(record, index))}
                onRemoveSubfield={(subfieldIndex) => onChange(removeSubfield(record, index, subfieldIndex))}
                onRemove={() => onChange(removeDataField(record, index))}
              />
            ))}
          </Space>
        )}
      </Card>

      <Modal
        open={wizardOpen}
        onCancel={() => setWizardOpen(false)}
        onOk={() => setWizardOpen(false)}
        title="Trường 008 — thông tin mã hóa độ dài cố định"
        okText="Xong"
        cancelText="Đóng"
        width={880}
        destroyOnHidden
      >
        <Control008Wizard
          value={record.controlFields.find((field) => field.tag === '008')?.value ?? ''}
          leader={record.leader}
          onChange={(value) => onChange(setControlField(record, '008', value))}
          readOnly={readOnly}
        />
      </Modal>
    </Space>
  );
}

interface DataFieldRowProps {
  field: MarcDataField;
  index: number;
  definitions: MarcFieldDefinition[];
  issues: MarcValidationIssue[];
  readOnly?: boolean;
  isDragging: boolean;
  canMoveUp: boolean;
  canMoveDown: boolean;
  onFocusRow: () => void;
  onDuplicate: () => void;
  onMove: (to: number) => void;
  onDragStart: () => void;
  onDragEnd: () => void;
  onDropOn: () => void;
  onChange: (change: Partial<MarcDataField>) => void;
  onChangeSubfield: (subfieldIndex: number, change: { code?: string; value?: string }) => void;
  onSplitSubfield: (subfieldIndex: number, text: string) => void;
  onAddSubfield: () => void;
  onRemoveSubfield: (subfieldIndex: number) => void;
  onRemove: () => void;
}

function DataFieldRow({
  field,
  index,
  definitions,
  issues,
  readOnly,
  isDragging,
  canMoveUp,
  canMoveDown,
  onFocusRow,
  onDuplicate,
  onMove,
  onDragStart,
  onDragEnd,
  onDropOn,
  onChange,
  onChangeSubfield,
  onSplitSubfield,
  onAddSubfield,
  onRemoveSubfield,
  onRemove,
}: DataFieldRowProps) {
  const definition = findDefinition(definitions, field.tag);
  const hasError = issues.some((issue) => issue.severity === 'Error');

  const indicatorOptions = (position: number) => {
    const rule = definition?.indicators.find((item) => item.position === position);

    if (!rule || rule.values.length === 0) {
      return undefined;
    }

    return rule.values.map((value) => ({
      value: value.code === '#' ? ' ' : value.code,
      label: `${value.code} — ${value.label}`,
    }));
  };

  const subfieldOptions = definition?.subfields.map((subfield) => ({
    value: subfield.code,
    label: `$${subfield.code} — ${subfield.name}`,
  }));

  return (
    <div
      onFocusCapture={onFocusRow}
      onDragOver={(event) => {
        // Không chặn mặc định thì trình duyệt từ chối thả.
        event.preventDefault();
      }}
      onDrop={(event) => {
        event.preventDefault();
        onDropOn();
      }}
      style={{
        border: `1px solid ${hasError ? MAU.loiVien : MAU.vien}`,
        borderRadius: 6,
        padding: 10,
        background: hasError ? MAU.loiNhat : undefined,
        opacity: isDragging ? 0.5 : undefined,
      }}
    >
      <Space align="center" wrap style={{ marginBottom: 8 }}>
        {!readOnly && (
          <Tooltip title="Kéo để đổi chỗ trường">
            <span
              draggable
              onDragStart={onDragStart}
              onDragEnd={onDragEnd}
              style={{ cursor: 'grab', color: MAU.chuMo, padding: '0 2px' }}
            >
              <HolderOutlined />
            </span>
          </Tooltip>
        )}

        <Tooltip title={definition?.description ?? undefined}>
          <Tag color={definition ? 'blue' : 'default'} style={{ ...MONOSPACE, width: 48, textAlign: 'center' }}>
            {field.tag}
          </Tag>
        </Tooltip>

        <Typography.Text strong>
          {definition?.name ?? 'Trường chưa khai báo trong bộ định nghĩa'}
        </Typography.Text>

        {definition?.isRepeatable && <Tag>Lặp lại được</Tag>}
        {definition?.isRequired && <Tag color="red">Bắt buộc</Tag>}

        <IndicatorInput
          label="Chỉ thị 1"
          value={field.ind1}
          options={indicatorOptions(1)}
          hint={definition?.indicators.find((item) => item.position === 1)?.name}
          readOnly={readOnly}
          onChange={(value) => onChange({ ind1: value })}
        />
        <IndicatorInput
          label="Chỉ thị 2"
          value={field.ind2}
          options={indicatorOptions(2)}
          hint={definition?.indicators.find((item) => item.position === 2)?.name}
          readOnly={readOnly}
          onChange={(value) => onChange({ ind2: value })}
        />

        {!readOnly && (
          <>
            {/* Kéo thả không dùng được bằng bàn phím, nên vẫn phải có nút lên xuống (mục 6.6). */}
            <Tooltip title="Chuyển lên trên">
              <Button
                type="text"
                icon={<ArrowUpOutlined />}
                disabled={!canMoveUp}
                onClick={() => onMove(index - 1)}
              />
            </Tooltip>
            <Tooltip title="Chuyển xuống dưới">
              <Button
                type="text"
                icon={<ArrowDownOutlined />}
                disabled={!canMoveDown}
                onClick={() => onMove(index + 1)}
              />
            </Tooltip>
            <Tooltip title="Nhân bản trường (Ctrl+D)">
              <Button type="text" icon={<CopyOutlined />} onClick={onDuplicate} />
            </Tooltip>
            <Popconfirm
              title="Xóa trường này khỏi biểu ghi?"
              okText="Xóa"
              cancelText="Không"
              onConfirm={onRemove}
            >
              <Button type="text" danger icon={<DeleteOutlined />} />
            </Popconfirm>
          </>
        )}
      </Space>

      <Space direction="vertical" size={6} style={{ width: '100%' }}>
        {field.subfields.map((subfield, subfieldIndex) => {
          const subfieldDefinition = definition?.subfields.find((item) => item.code === subfield.code);
          const splittable = looksLikeSubfieldText(subfield.value);

          return (
            <Space key={subfieldIndex} align="start" style={{ width: '100%' }}>
              {subfieldOptions ? (
                <Select
                  value={subfield.code}
                  onChange={(value) => onChangeSubfield(subfieldIndex, { code: value })}
                  options={subfieldOptions}
                  disabled={readOnly}
                  style={{ width: 220 }}
                  showSearch
                  optionFilterProp="label"
                />
              ) : (
                <Input
                  value={subfield.code}
                  onChange={(event) =>
                    onChangeSubfield(subfieldIndex, { code: event.target.value.slice(0, 1).toLowerCase() })
                  }
                  disabled={readOnly}
                  style={{ ...MONOSPACE, width: 60 }}
                  maxLength={1}
                />
              )}

              <Input.TextArea
                value={subfield.value}
                onChange={(event) => onChangeSubfield(subfieldIndex, { value: event.target.value })}
                disabled={readOnly}
                autoSize={{ minRows: 1, maxRows: 6 }}
                // Rộng cố định 560px là vừa vặn trên máy tính nhưng đẩy cả trang tràn ra trên màn
                // hình hẹp; đặt trần thay vì đặt cứng để ô co lại theo khung.
                style={{ width: '100%', maxWidth: 560 }}
                placeholder={subfieldDefinition?.name}
              />

              {splittable && !readOnly && (
                <Tooltip title="Chuỗi này chứa nhiều trường con viết liền. Bấm để tách thành các trường con riêng.">
                  <Button
                    icon={<ScissorOutlined />}
                    onClick={() => onSplitSubfield(subfieldIndex, subfield.value)}
                  />
                </Tooltip>
              )}

              {!readOnly && (
                <Button
                  type="text"
                  danger
                  icon={<DeleteOutlined />}
                  onClick={() => onRemoveSubfield(subfieldIndex)}
                />
              )}
            </Space>
          );
        })}

        {!readOnly && (
          <Button type="dashed" size="small" icon={<PlusOutlined />} onClick={onAddSubfield}>
            Thêm trường con
          </Button>
        )}
      </Space>

      <FieldIssues issues={issues} />
    </div>
  );
}

interface IndicatorInputProps {
  label: string;
  value: string;
  options?: Array<{ value: string; label: string }>;
  hint?: string;
  readOnly?: boolean;
  onChange: (value: string) => void;
}

function IndicatorInput({ label, value, options, hint, readOnly, onChange }: IndicatorInputProps) {
  if (!options) {
    // A field whose definition declares no indicator values still has two positions to fill; the
    // free-text box lets a cataloguer type whatever the source record used.
    return (
      <Tooltip title={`${label} — trường này không khai báo giá trị chỉ thị`}>
        <Input
          value={displayIndicator(value)}
          onChange={(event) => onChange(parseIndicator(event.target.value))}
          disabled={readOnly}
          style={{ ...MONOSPACE, width: 56, textAlign: 'center' }}
          maxLength={1}
        />
      </Tooltip>
    );
  }

  return (
    <Tooltip title={hint ? `${label}: ${hint}` : label}>
      <Select
        value={value}
        onChange={onChange}
        options={options}
        disabled={readOnly}
        style={{ width: 240 }}
        showSearch
        optionFilterProp="label"
      />
    </Tooltip>
  );
}

function FieldIssues({ issues }: { issues: MarcValidationIssue[] }) {
  if (issues.length === 0) {
    return null;
  }

  return (
    <Space direction="vertical" size={2} style={{ marginTop: 6, width: '100%' }}>
      {issues.map((issue, index) => (
        <Typography.Text
          key={index}
          type={issue.severity === 'Error' ? 'danger' : 'warning'}
          style={{ fontSize: 12 }}
        >
          {issue.severity === 'Error' ? <WarningOutlined /> : <InfoCircleOutlined />} {issue.message}
        </Typography.Text>
      ))}
    </Space>
  );
}

/** Bảng tổng hợp lỗi và cảnh báo của cả biểu ghi, hiện phía trên trình soạn thảo. */
export function MarcValidationSummary({
  issues,
  isValid,
}: {
  issues: MarcValidationIssue[];
  isValid: boolean;
}) {
  if (issues.length === 0) {
    return <Alert type="success" showIcon message="Biểu ghi hợp lệ, không có lỗi hay cảnh báo nào." />;
  }

  const errors = issues.filter((issue) => issue.severity === 'Error');
  const warnings = issues.filter((issue) => issue.severity === 'Warning');

  return (
    <Alert
      type={isValid ? 'warning' : 'error'}
      showIcon
      message={
        isValid
          ? `Biểu ghi lưu được nhưng có ${warnings.length} cảnh báo.`
          : `Biểu ghi có ${errors.length} lỗi phải sửa trước khi lưu.`
      }
      description={
        <Space direction="vertical" size={2}>
          {[...errors, ...warnings].map((issue, index) => (
            <Typography.Text key={index} style={{ fontSize: 13 }}>
              {issue.tag ? <Tag style={MONOSPACE}>{issue.tag}</Tag> : null}
              {issue.message}
            </Typography.Text>
          ))}
        </Space>
      }
    />
  );
}
