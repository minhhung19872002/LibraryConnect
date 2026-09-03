import { useState } from 'react';
import {
  Alert,
  App,
  Button,
  Empty,
  Input,
  Modal,
  Select,
  Space,
  Table,
  Tag,
  Typography,
} from 'antd';
import { SearchOutlined } from '@ant-design/icons';
import { useMutation, useQuery } from '@tanstack/react-query';
import { interLibraryApi } from '@/modules/interlibrary/api';
import { searchFieldLabels } from '@/modules/interlibrary/labels';
import type { RemoteRecordDto, RemoteSearchField } from '@/modules/interlibrary/types';
import { errorMessage } from '@/api/formErrors';
import type { MarcRecord } from '@/modules/marc/types';

interface RemoteRecordPickerProps {
  open: boolean;
  /** Mở sẵn ở tiêu chí ISBN khi người dùng bấm "Lấy từ ISBN". */
  initialField?: RemoteSearchField;
  onClose: () => void;
  onPicked: (record: MarcRecord) => void;
}

/**
 * Lấy biểu ghi từ thư viện khác ngay trên trình soạn (đặc tả II.2).
 *
 * The cataloguing screen is where a librarian decides a record is worth copying, so the search
 * belongs there rather than on a separate page they have to navigate to and back from. The record
 * is put through the server's `prepare` step first — that is what strips the source library's local
 * fields and control numbers — and then dropped straight into the editor for correction, which is
 * the whole point of copy cataloguing.
 */
export function RemoteRecordPicker({
  open,
  initialField = 'Any',
  onClose,
  onPicked,
}: RemoteRecordPickerProps) {
  const { message } = App.useApp();
  const [field, setField] = useState<RemoteSearchField>(initialField);
  const [term, setTerm] = useState('');

  const targets = useQuery({
    queryKey: ['ill-targets'],
    queryFn: () => interLibraryApi.targets(false),
    enabled: open,
  });

  const search = useMutation({
    mutationFn: () => interLibraryApi.search({ targetIds: [], field, term: term.trim(), maxRecords: 20 }),
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const pick = useMutation({
    mutationFn: (row: RemoteRecordDto) => interLibraryApi.prepareRecord(row.targetId, row.marcJson),
    onSuccess: (marcJson: string) => {
      onPicked(JSON.parse(marcJson) as MarcRecord);
      message.success('Đã nạp biểu ghi vào trình soạn. Hiệu đính rồi hãy lưu.');
      onClose();
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const rows = (search.data?.targets ?? []).flatMap((target) =>
    target.records.map((row) => ({ ...row, targetName: target.targetName })),
  );

  const noTargets = targets.isSuccess && (targets.data?.length ?? 0) === 0;

  return (
    <Modal
      open={open}
      onCancel={onClose}
      footer={null}
      width={960}
      title="Lấy biểu ghi từ thư viện khác (Z39.50 / SRU)"
      destroyOnHidden
    >
      <Space direction="vertical" size={12} style={{ width: '100%' }}>
        {noTargets && (
          <Alert
            type="warning"
            showIcon
            message="Chưa khai máy chủ nào"
            description="Vào Liên thư viện → Máy chủ Z39.50 để thêm địa chỉ tra cứu trước."
          />
        )}

        <Space.Compact style={{ width: '100%' }}>
          <Select<RemoteSearchField>
            value={field}
            onChange={setField}
            style={{ width: 180 }}
            options={Object.entries(searchFieldLabels).map(([value, label]) => ({
              value: value as RemoteSearchField,
              label,
            }))}
          />
          <Input
            value={term}
            onChange={(event) => setTerm(event.target.value)}
            onPressEnter={() => term.trim() && search.mutate()}
            placeholder={field === 'Isbn' ? 'Nhập mã ISBN, ví dụ 9786041000100' : 'Nhập từ khóa tra cứu'}
            allowClear
          />
          <Button
            type="primary"
            icon={<SearchOutlined />}
            loading={search.isPending}
            disabled={!term.trim() || noTargets}
            onClick={() => search.mutate()}
          >
            Tra cứu
          </Button>
        </Space.Compact>

        {search.isSuccess && rows.length === 0 && (
          <Empty description="Không máy chủ nào trả về biểu ghi cho từ khóa này" />
        )}

        {rows.length > 0 && (
          <Table<RemoteRecordDto & { targetName: string }>
            rowKey={(row) => `${row.targetId}-${row.position}`}
            dataSource={rows}
            size="small"
            pagination={false}
            scroll={{ y: 380 }}
            columns={[
              { title: 'Nguồn', dataIndex: 'targetName', width: 160 },
              {
                title: 'Nhan đề',
                dataIndex: 'title',
                render: (title: string, row) => (
                  <Space direction="vertical" size={0}>
                    <Typography.Text strong>{title}</Typography.Text>
                    <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                      {[row.author, row.publisher, row.publishYear].filter(Boolean).join(' · ')}
                    </Typography.Text>
                  </Space>
                ),
              },
              { title: 'ISBN', dataIndex: 'isbn', width: 150 },
              {
                title: '',
                width: 190,
                render: (_: unknown, row) =>
                  row.existingBibId ? (
                    <Tag color="gold">Thư viện đã có</Tag>
                  ) : (
                    <Button
                      size="small"
                      type="primary"
                      loading={pick.isPending}
                      onClick={() => pick.mutate(row)}
                    >
                      Nạp vào trình soạn
                    </Button>
                  ),
              },
            ]}
          />
        )}
      </Space>
    </Modal>
  );
}
