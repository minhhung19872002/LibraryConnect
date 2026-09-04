import { useMemo } from 'react';
import { Modal, Space, Table, Tabs, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import { formatRecordAsText } from '@/modules/marc/marcRecord';
import type { RemoteRecordDto } from '@/modules/interlibrary/types';
import { catalogingApi, parseMarc } from './api';
import { compareMarcFields, MARC_COMPARE_LABELS, type MarcCompareLine } from './marcCompare';

const MONOSPACE = { fontFamily: 'ui-monospace, Consolas, monospace' } as const;

const KIND_COLORS: Record<MarcCompareLine['kind'], string> = {
  same: 'default',
  different: 'orange',
  remoteOnly: 'blue',
  localOnly: 'purple',
};

/**
 * Xem MARC của một biểu ghi lấy về từ thư viện bạn (II.7), và khi kho mình đã có biểu ghi cùng tài
 * liệu thì đặt hai bên cạnh nhau, trường-với-trường.
 *
 * "Đã có trong kho" chỉ là một cái thẻ; câu hỏi thật của cán bộ là "bản của họ có gì hơn bản của
 * mình không" — đề mục chủ đề đầy đủ hơn, chỉ số DDC, tóm tắt — và bảng so sánh trả lời đúng câu ấy.
 */
export function RemoteMarcModal({
  record,
  onClose,
}: {
  record: RemoteRecordDto | null;
  onClose: () => void;
}) {
  const remote = useMemo(() => (record ? parseMarc(record.marcJson) : null), [record]);

  const existing = useQuery({
    queryKey: ['bib-record', record?.existingBibId],
    queryFn: () => catalogingApi.get(record!.existingBibId!),
    enabled: Boolean(record?.existingBibId),
  });

  const comparison = useMemo(
    () => (remote && existing.data ? compareMarcFields(remote, parseMarc(existing.data.marcJson)) : []),
    [remote, existing.data],
  );

  const differences = comparison.filter((line) => line.kind !== 'same').length;

  return (
    <Modal
      open={record !== null}
      onCancel={onClose}
      footer={null}
      width={record?.existingBibId ? 1080 : 760}
      title={
        <Space wrap>
          <span>Biểu ghi MARC từ {record?.targetName}</span>
          {record?.existingBibId && (
            <Tag color="orange">Kho mình đã có: {record.existingBibTitle}</Tag>
          )}
        </Space>
      }
    >
      {remote && (
        <Tabs
          defaultActiveKey={record?.existingBibId ? 'compare' : 'marc'}
          items={[
            ...(record?.existingBibId
              ? [
                  {
                    key: 'compare',
                    label: `So sánh với biểu ghi trong kho (${differences} trường khác)`,
                    children: (
                      <Table<MarcCompareLine>
                        rowKey="tag"
                        size="small"
                        loading={existing.isLoading}
                        dataSource={comparison}
                        pagination={false}
                        scroll={{ y: 460 }}
                        rowClassName={(line) => (line.kind === 'same' ? '' : 'ant-table-row-selected')}
                        columns={[
                          {
                            title: 'Trường',
                            dataIndex: 'tag',
                            width: 80,
                            render: (value: string) => <span style={MONOSPACE}>{value}</span>,
                          },
                          {
                            title: `Từ ${record?.targetName}`,
                            dataIndex: 'remote',
                            render: (value: string) => (
                              <Typography.Text style={{ ...MONOSPACE, fontSize: 12, whiteSpace: 'pre-wrap' }}>
                                {value || '—'}
                              </Typography.Text>
                            ),
                          },
                          {
                            title: 'Trong kho mình',
                            dataIndex: 'local',
                            render: (value: string) => (
                              <Typography.Text style={{ ...MONOSPACE, fontSize: 12, whiteSpace: 'pre-wrap' }}>
                                {value || '—'}
                              </Typography.Text>
                            ),
                          },
                          {
                            title: '',
                            dataIndex: 'kind',
                            width: 130,
                            render: (value: MarcCompareLine['kind']) => (
                              <Tag color={KIND_COLORS[value]}>{MARC_COMPARE_LABELS[value]}</Tag>
                            ),
                          },
                        ]}
                      />
                    ),
                  },
                ]
              : []),
            {
              key: 'marc',
              label: 'MARC thô',
              children: (
                <Typography.Paragraph
                  style={{ ...MONOSPACE, whiteSpace: 'pre-wrap', fontSize: 12, marginBottom: 0, maxHeight: 480, overflow: 'auto' }}
                >
                  {formatRecordAsText(remote)}
                </Typography.Paragraph>
              ),
            },
          ]}
        />
      )}
    </Modal>
  );
}
