import { useMemo, useState } from 'react';
import {
  Alert,
  App,
  Button,
  Card,
  Col,
  Empty,
  List,
  Row,
  Space,
  Spin,
  Tag,
  Typography,
  Upload,
} from 'antd';
import {
  CheckCircleOutlined,
  DownloadOutlined,
  FileAddOutlined,
  SafetyCertificateOutlined,
  UploadOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery } from '@tanstack/react-query';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { marcApi, saveBlob } from './api';
import { MarcEditor, MarcValidationSummary } from './MarcEditor';
import { createEmptyRecord, formatRecordAsText } from './marcRecord';
import type { MarcRecord, MarcValidationResult, ParseMarcFileResult } from './types';

const MONOSPACE = { fontFamily: 'ui-monospace, Consolas, monospace' } as const;

/**
 * Công cụ biểu ghi MARC: đọc tệp trao đổi, soạn, kiểm tra và xuất lại.
 *
 * This is the working proof of the exchange requirement (section 2.4) and it is a tool librarians
 * genuinely use: a record arrives from a partner library as a .mrc or .xml file, and before it goes
 * anywhere near the catalogue somebody needs to see what is actually in it, fix what is wrong and
 * hand a corrected file back. Nothing here writes to the catalogue — saving records is part of the
 * cataloguing screens.
 */
export function MarcToolPage() {
  const { message } = App.useApp();

  const [record, setRecord] = useState<MarcRecord>(createEmptyRecord());
  const [validation, setValidation] = useState<MarcValidationResult | null>(null);
  const [parsed, setParsed] = useState<ParseMarcFileResult | null>(null);
  const [selected, setSelected] = useState<number | null>(null);

  const { data: definitions = [], isLoading } = useQuery({
    queryKey: ['marc-fields', '', false],
    queryFn: () => marcApi.getFields(),
  });

  const validate = useMutation({
    mutationFn: () => marcApi.validate(record),
    onSuccess: (result) => {
      setValidation(result);
      if (result.isValid && result.warningCount === 0) {
        message.success('Biểu ghi hợp lệ.');
      }
    },
    onError: (error: unknown) => {
      message.error(error instanceof ApiRequestError ? error.message : 'Không kiểm tra được biểu ghi.');
    },
  });

  const parse = useMutation({
    mutationFn: (file: File) => marcApi.parseFile(file),
    onSuccess: (result) => {
      setParsed(result);

      if (result.records.length > 0) {
        openParsedRecord(result, 0);
        message.success(`Đã đọc ${result.totalRecords} biểu ghi theo định dạng ${result.format}.`);
      } else {
        message.warning('Tệp không chứa biểu ghi nào đọc được.');
      }
    },
    onError: (error: unknown) => {
      message.error(error instanceof ApiRequestError ? error.message : 'Không đọc được tệp.');
    },
  });

  const exportFile = useMutation({
    mutationFn: (format: 'iso2709' | 'marcxml') => marcApi.exportRecords([record], format),
    onSuccess: ({ blob, fileName }) => {
      saveBlob(blob, fileName);
      message.success(`Đã xuất tệp ${fileName}.`);
    },
    onError: (error: unknown) => {
      message.error(error instanceof ApiRequestError ? error.message : 'Không xuất được tệp.');
    },
  });

  const openParsedRecord = (result: ParseMarcFileResult, index: number) => {
    const item = result.records[index];

    if (!item) {
      return;
    }

    setRecord(JSON.parse(item.marcJson) as MarcRecord);
    setValidation(item.validation);
    setSelected(index);
  };

  const preview = useMemo(() => formatRecordAsText(record), [record]);

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Công cụ biểu ghi MARC 21"
        description="Đọc tệp trao đổi ISO 2709 hoặc MARCXML, soạn và kiểm tra biểu ghi, rồi xuất lại ra tệp. Công cụ này không ghi gì vào cơ sở dữ liệu."
        actions={
          <Space wrap>
            <Button
              icon={<FileAddOutlined />}
              onClick={() => {
                setRecord(createEmptyRecord());
                setValidation(null);
                setSelected(null);
              }}
            >
              Biểu ghi trống
            </Button>

            <Can permission={PERMISSIONS.cataloging.bibImport}>
              <Upload
                accept=".mrc,.marc,.iso,.xml,.mrx"
                showUploadList={false}
                beforeUpload={(file) => {
                  parse.mutate(file as unknown as File);
                  return false;
                }}
              >
                <Button icon={<UploadOutlined />} loading={parse.isPending}>
                  Đọc tệp .mrc / .xml
                </Button>
              </Upload>
            </Can>

            <Button
              type="primary"
              icon={<SafetyCertificateOutlined />}
              loading={validate.isPending}
              onClick={() => validate.mutate()}
            >
              Kiểm tra biểu ghi
            </Button>

            <Can permission={PERMISSIONS.cataloging.bibExport}>
              <Button
                icon={<DownloadOutlined />}
                loading={exportFile.isPending}
                onClick={() => exportFile.mutate('iso2709')}
              >
                Xuất .mrc
              </Button>
            </Can>

            <Can permission={PERMISSIONS.cataloging.bibExport}>
              <Button
                icon={<DownloadOutlined />}
                loading={exportFile.isPending}
                onClick={() => exportFile.mutate('marcxml')}
              >
                Xuất MARCXML
              </Button>
            </Can>
          </Space>
        }
      />

      {validation && <MarcValidationSummary issues={validation.issues} isValid={validation.isValid} />}

      {parsed && parsed.errors.length > 0 && (
        <Alert
          type="warning"
          showIcon
          message={`${parsed.errors.length} biểu ghi trong tệp không đọc được`}
          description={
            <Space direction="vertical" size={2}>
              {parsed.errors.map((error) => (
                <Typography.Text key={error.recordNumber} style={{ fontSize: 13 }}>
                  Biểu ghi số {error.recordNumber} (vị trí byte {error.position.toLocaleString('vi-VN')}):{' '}
                  {error.message}
                </Typography.Text>
              ))}
            </Space>
          }
        />
      )}

      <Row gutter={16}>
        {parsed && parsed.records.length > 1 && (
          <Col xs={24} lg={6}>
            <Card
              size="small"
              title={`Biểu ghi trong tệp (${parsed.totalRecords})`}
              styles={{ body: { padding: 0, maxHeight: 640, overflowY: 'auto' } }}
            >
              <List
                size="small"
                dataSource={parsed.records}
                renderItem={(item, index) => (
                  <List.Item
                    onClick={() => openParsedRecord(parsed, index)}
                    style={{
                      cursor: 'pointer',
                      background: selected === index ? '#e6f4ff' : undefined,
                      paddingInline: 12,
                    }}
                  >
                    <Space direction="vertical" size={0} style={{ width: '100%' }}>
                      <Space size={6}>
                        <Typography.Text type="secondary" style={MONOSPACE}>
                          {item.recordNumber}
                        </Typography.Text>
                        {item.validation.errorCount > 0 ? (
                          <Tag color="red">{item.validation.errorCount} lỗi</Tag>
                        ) : (
                          <CheckCircleOutlined style={{ color: '#52c41a' }} />
                        )}
                      </Space>
                      <Typography.Text ellipsis style={{ fontSize: 13 }}>
                        {item.title}
                      </Typography.Text>
                    </Space>
                  </List.Item>
                )}
              />
            </Card>
          </Col>
        )}

        <Col xs={24} lg={parsed && parsed.records.length > 1 ? 18 : 24}>
          {isLoading ? (
            <Card>
              <Spin tip="Đang tải bộ định nghĩa trường MARC...">
                <Empty description=" " image={null} />
              </Spin>
            </Card>
          ) : (
            <Space direction="vertical" size={16} style={{ width: '100%' }}>
              <MarcEditor
                record={record}
                onChange={(next) => {
                  setRecord(next);
                  // The displayed issues describe the record as it was; keeping them after an edit
                  // would point at fields that may no longer exist.
                  setValidation(null);
                }}
                definitions={definitions}
                issues={validation?.issues ?? []}
              />

              <Card size="small" title="Biểu ghi ở dạng văn bản">
                <Typography.Paragraph style={{ ...MONOSPACE, whiteSpace: 'pre-wrap', marginBottom: 0 }}>
                  {preview}
                </Typography.Paragraph>
              </Card>
            </Space>
          )}
        </Col>
      </Row>
    </Space>
  );
}
