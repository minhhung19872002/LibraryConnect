import { useState } from 'react';
import {
  Alert,
  App,
  Button,
  Card,
  Col,
  Descriptions,
  Input,
  Modal,
  Progress,
  Row,
  Select,
  Space,
  Statistic,
  Table,
  Tag,
  Typography,
} from 'antd';
import type { ColumnsType } from 'antd/es/table';
import {
  CloudDownloadOutlined,
  CloudUploadOutlined,
  DeleteOutlined,
  ExclamationCircleFilled,
  UndoOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/api/client';
import { errorMessage } from '@/api/formErrors';
import { PERMISSIONS } from '@/api/permissions';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { usePagedQuery } from '@/hooks/usePagedQuery';
import { messages } from '@/i18n/messages';
import { downloadFile, formatBytes, formatDateTime } from './helpers';
import { backupPollInterval } from './backupPolling';
import type { BackupJob, BackupStorage, BackupType, RestoreStatus } from './types';
import { MAU } from '@/lib/palette';

/**
 * Phân hệ I.5 — sao lưu và phục hồi.
 *
 * Restoring overwrites the whole database, so the flow deliberately takes two steps and asks for the
 * operator's own password: holding the permission is not treated as enough on its own.
 */
export function BackupsPage() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [createOpen, setCreateOpen] = useState(false);
  const [restoreTarget, setRestoreTarget] = useState<BackupJob | null>(null);

  // Sao lưu chạy nền từ khi sửa lỗi H9, nên bảng phải tự hỏi lại mới thấy lượt đang chạy xong.
  const list = usePagedQuery<BackupJob>({
    queryKey: 'backups',
    url: '/admin/backups',
    refetchInterval: backupPollInterval,
  });

  const storage = useQuery({
    queryKey: ['backup-storage'],
    queryFn: () => api.get<BackupStorage>('/admin/backups/storage'),
  });

  const refresh = async () => {
    await queryClient.invalidateQueries({ queryKey: ['backups'] });
    await storage.refetch();
  };

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/admin/backups/${id}`),
    onSuccess: async () => {
      message.success('Đã xóa bản sao lưu.');
      await refresh();
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const download = async (job: BackupJob) => {
    try {
      const { blob, fileName } = await api.download(`/admin/backups/${job.id}/download`);
      downloadFile(blob, fileName);
    } catch (error) {
      message.error(errorMessage(error));
    }
  };

  const used = storage.data ? storage.data.totalBytes - storage.data.freeBytes : 0;
  const usedPercent = storage.data && storage.data.totalBytes > 0
    ? Math.round((used / storage.data.totalBytes) * 100)
    : 0;

  const columns: ColumnsType<BackupJob> = [
    {
      title: 'Thời điểm',
      dataIndex: 'startedAt',
      width: 175,
      render: (value: string) => formatDateTime(value),
    },
    {
      title: 'Tệp',
      dataIndex: 'fileName',
      ellipsis: true,
      render: (fileName: string | undefined, record) => (
        <Space direction="vertical" size={0}>
          <Typography.Text className="lc-mono">{fileName ?? '—'}</Typography.Text>
          {record.checksum && (
            <Typography.Text type="secondary" className="lc-mono lc-small">
              SHA-256: {record.checksum.slice(0, 24)}…
            </Typography.Text>
          )}
        </Space>
      ),
    },
    { title: 'Loại', dataIndex: 'typeLabel', width: 120 },
    {
      title: 'Dung lượng',
      dataIndex: 'sizeBytes',
      width: 130,
      align: 'right',
      render: (bytes: number) => (bytes > 0 ? formatBytes(bytes) : '—'),
    },
    {
      title: 'Nguồn',
      dataIndex: 'isAuto',
      width: 140,
      render: (isAuto: boolean, record) => (
        <Space direction="vertical" size={0}>
          <Tag color={isAuto ? 'blue' : 'default'}>{isAuto ? 'Tự động' : 'Thủ công'}</Tag>
          {record.triggeredByName && (
            <Typography.Text type="secondary" className="lc-small">
              {record.triggeredByName}
            </Typography.Text>
          )}
        </Space>
      ),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'statusLabel',
      width: 150,
      render: (label: string, record) => (
        <Space direction="vertical" size={0}>
          <Tag color={statusColor(record.status)}>{label}</Tag>
          {!record.fileAvailable && record.status === 'Success' && (
            <Typography.Text type="secondary" className="lc-small">
              Tệp đã bị xóa
            </Typography.Text>
          )}
        </Space>
      ),
    },
    {
      title: 'Thao tác',
      key: 'actions',
      width: 190,
      fixed: 'right',
      render: (_, record) => (
        <Space size={4}>
          <Can permission={PERMISSIONS.system.backupView}>
            <Button
              type="link"
              size="small"
              icon={<CloudDownloadOutlined />}
              disabled={!record.fileAvailable}
              onClick={() => download(record)}
            >
              Tải về
            </Button>
          </Can>
          <Can permission={PERMISSIONS.system.backupRestore}>
            <Button
              type="link"
              size="small"
              icon={<UndoOutlined />}
              disabled={!record.fileAvailable}
              onClick={() => setRestoreTarget(record)}
            >
              Phục hồi
            </Button>
          </Can>
          <Can permission={PERMISSIONS.system.backupDelete}>
            <Button
              type="link"
              size="small"
              danger
              icon={<DeleteOutlined />}
              onClick={() =>
                modal.confirm({
                  title: 'Xóa bản sao lưu',
                  content: `Xóa tệp "${record.fileName}"? Thao tác này không thể hoàn tác.`,
                  okText: messages.confirm.yes,
                  cancelText: messages.confirm.no,
                  okButtonProps: { danger: true },
                  onOk: () => deleteMutation.mutateAsync(record.id),
                })
              }
            />
          </Can>
        </Space>
      ),
    },
  ];

  return (
    <div className="lc-page">
      <PageHeader
        title={messages.menu.backups}
        description="Sao lưu bằng pg_dump và phục hồi bằng pg_restore. Bản sao lưu có thể tải về để lưu trữ ngoài máy chủ."
        actions={
          <Can permission={PERMISSIONS.system.backupCreate}>
            <Button type="primary" icon={<CloudUploadOutlined />} onClick={() => setCreateOpen(true)}>
              Sao lưu ngay
            </Button>
          </Can>
        }
      />

      <Row gutter={[16, 16]} className="lc-page-alert">
        <Col xs={24} md={8}>
          <Card variant="borderless">
            <Statistic title="Số bản sao lưu" value={storage.data?.backupCount ?? 0} />
            <Typography.Text type="secondary">
              Giữ lại {storage.data?.keepCount ?? 0} bản gần nhất
            </Typography.Text>
          </Card>
        </Col>
        <Col xs={24} md={8}>
          <Card variant="borderless">
            <Statistic
              title="Dung lượng bản sao lưu"
              value={formatBytes(storage.data?.usedByBackupsBytes ?? 0)}
            />
            <Typography.Text type="secondary">
              Ổ đĩa còn trống {formatBytes(storage.data?.freeBytes ?? 0)}
            </Typography.Text>
            <Progress percent={usedPercent} size="small" status={usedPercent > 90 ? 'exception' : 'normal'} />
          </Card>
        </Col>
        <Col xs={24} md={8}>
          <Card variant="borderless">
            <Statistic
              title="Sao lưu tự động"
              value={storage.data?.autoEnabled ? 'Đang bật' : 'Đang tắt'}
              valueStyle={{ color: storage.data?.autoEnabled ? MAU.tot : MAU.chuMo }}
            />
            <Typography.Text type="secondary" className="lc-mono">
              Lịch: {storage.data?.scheduleCron ?? '—'}
            </Typography.Text>
            <br />
            <Typography.Text type="secondary">
              Gần nhất: {formatDateTime(storage.data?.lastSuccessAt) ?? 'chưa có'}
            </Typography.Text>
          </Card>
        </Col>
      </Row>

      <Card variant="borderless" styles={{ body: { padding: 0 } }}>
        <Table<BackupJob>
          rowKey="id"
          columns={columns}
          dataSource={list.items}
          loading={list.isLoading}
          pagination={list.pagination}
          onChange={list.handleTableChange}
          scroll={{ x: 1150 }}
          size="middle"
          locale={{ emptyText: 'Chưa có bản sao lưu nào.' }}
          expandable={{
            rowExpandable: (record) => Boolean(record.message),
            expandedRowRender: (record) => (
              <Alert type={record.status === 'Failed' ? 'error' : 'info'} message={record.message} />
            ),
          }}
        />
      </Card>

      {createOpen && (
        <CreateBackupModal
          onClose={() => setCreateOpen(false)}
          onCreated={async () => {
            setCreateOpen(false);
            await refresh();
          }}
        />
      )}

      {restoreTarget && (
        <RestoreModal
          job={restoreTarget}
          onClose={() => setRestoreTarget(null)}
          onRestored={async () => {
            setRestoreTarget(null);
            await refresh();
          }}
        />
      )}
    </div>
  );
}

function statusColor(status: BackupJob['status']): string {
  switch (status) {
    case 'Success':
      return 'green';
    case 'Failed':
      return 'red';
    case 'Pending':
      return 'default';
    case 'Running':
      return 'processing';
    case 'Restored':
      return 'gold';
    default:
      return 'default';
  }
}

// ---------------------------------------------------------------------------

function CreateBackupModal({ onClose, onCreated }: { onClose: () => void; onCreated: () => Promise<void> }) {
  const { message } = App.useApp();
  const [type, setType] = useState<BackupType>('Full');
  const [includeFiles, setIncludeFiles] = useState(true);

  const mutation = useMutation({
    mutationFn: () => api.post<BackupJob>('/admin/backups', { type, includeObjectStorage: includeFiles }),
    onSuccess: async () => {
      message.success('Đã xếp lượt sao lưu vào hàng đợi. Tiến độ hiện ở bảng bên dưới.');
      await onCreated();
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  return (
    <Modal
      open
      title="Sao lưu cơ sở dữ liệu"
      onCancel={onClose}
      onOk={() => mutation.mutate()}
      okText="Bắt đầu sao lưu"
      cancelText={messages.actions.cancel}
      confirmLoading={mutation.isPending}
    >
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <Alert
          type="info"
          showIcon
          message="Sao lưu chạy ở tiến trình nền. Đóng trang này cũng không ảnh hưởng; trạng thái tự cập nhật ở bảng danh sách."
        />

        <div>
          <Typography.Text strong>Loại sao lưu</Typography.Text>
          <Select
            value={type}
            onChange={setType}
            style={{ width: '100%', marginTop: 6 }}
            options={[
              { value: 'Full', label: 'Toàn bộ — cấu trúc và dữ liệu (khuyến nghị)' },
              { value: 'DataOnly', label: 'Chỉ dữ liệu — không kèm cấu trúc bảng' },
            ]}
          />
        </div>

        <div>
          <Typography.Text strong>Tệp tài liệu số</Typography.Text>
          <Select
            value={includeFiles}
            onChange={setIncludeFiles}
            style={{ width: '100%', marginTop: 6 }}
            options={[
              { value: true, label: 'Sao lưu kèm tệp tài liệu số' },
              { value: false, label: 'Chỉ sao lưu cơ sở dữ liệu' },
            ]}
          />
        </div>
      </Space>
    </Modal>
  );
}

// ---------------------------------------------------------------------------

function RestoreModal({
  job,
  onClose,
  onRestored,
}: {
  job: BackupJob;
  onClose: () => void;
  onRestored: () => Promise<void>;
}) {
  const { message } = App.useApp();
  const [step, setStep] = useState<'warn' | 'confirm' | 'running'>('warn');
  const [password, setPassword] = useState('');

  const [watching, setWatching] = useState(false);

  const mutation = useMutation({
    mutationFn: () =>
      api.post<RestoreStatus>(`/admin/backups/${job.id}/restore`, { confirmPassword: password }),
    onSuccess: () => {
      // Không đóng hộp thoại: phục hồi chạy hàng chục phút và trong lúc ấy hệ thống không dùng được,
      // nên người bấm cần thấy nó chạy tới đâu ngay tại chỗ.
      setWatching(true);
      setStep('running');
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const status = useQuery({
    queryKey: ['restore-status'],
    queryFn: () => api.get<RestoreStatus | null>('/admin/backups/restore-status'),
    enabled: watching,
    refetchInterval: (query) => (query.state.data?.state === 'Running' ? 3000 : false),
  });

  const done = status.data && status.data.state !== 'Running';

  return (
    <Modal
      open
      title={
        <Space>
          <ExclamationCircleFilled style={{ color: MAU.luuY }} />
          Phục hồi cơ sở dữ liệu
        </Space>
      }
      onCancel={onClose}
      width={560}
      closable={step !== 'running'}
      maskClosable={step !== 'running'}
      footer={
        step === 'running' ? (
          <Button
            type="primary"
            disabled={!done}
            onClick={async () => {
              await onRestored();
              onClose();
            }}
          >
            Đóng
          </Button>
        ) : step === 'warn' ? (
          <Space>
            <Button onClick={onClose}>{messages.actions.cancel}</Button>
            <Button danger type="primary" onClick={() => setStep('confirm')}>
              Tôi hiểu, tiếp tục
            </Button>
          </Space>
        ) : (
          <Space>
            <Button onClick={() => setStep('warn')}>{messages.actions.back}</Button>
            <Button
              danger
              type="primary"
              disabled={password.length === 0}
              loading={mutation.isPending}
              onClick={() => mutation.mutate()}
            >
              Phục hồi ngay
            </Button>
          </Space>
        )
      }
    >
      {step === 'running' ? (
        <Space direction="vertical" size="middle" style={{ width: '100%' }}>
          {status.data?.state === 'Succeeded' ? (
            <Alert type="success" showIcon message="Phục hồi hoàn tất" description={status.data.message} />
          ) : status.data?.state === 'Failed' ? (
            <Alert
              type="error"
              showIcon
              message="Phục hồi thất bại — cơ sở dữ liệu giữ nguyên như trước"
              description={status.data.message}
            />
          ) : (
            <Alert
              type="info"
              showIcon
              message="Đang phục hồi, đừng tắt máy chủ"
              description={
                'Quá trình chạy ở tiến trình nền và có thể mất nhiều phút. Trong lúc này các màn hình '
                + 'khác tạm thời không dùng được. Đóng trình duyệt cũng không làm nó dừng.'
              }
            />
          )}

          <Progress
            percent={done ? 100 : 60}
            status={status.data?.state === 'Failed' ? 'exception' : done ? 'success' : 'active'}
            showInfo={false}
          />

          <Descriptions bordered column={1} size="small">
            <Descriptions.Item label="Tệp">{status.data?.archiveName ?? job.fileName}</Descriptions.Item>
            <Descriptions.Item label="Bắt đầu">
              {status.data ? formatDateTime(status.data.startedAt) : '—'}
            </Descriptions.Item>
            {status.data?.finishedAt && (
              <Descriptions.Item label="Kết thúc">{formatDateTime(status.data.finishedAt)}</Descriptions.Item>
            )}
          </Descriptions>

          {status.data?.state === 'Succeeded' && (
            <Typography.Text type="secondary">
              Đăng xuất rồi đăng nhập lại để làm việc trên dữ liệu vừa khôi phục.
            </Typography.Text>
          )}
        </Space>
      ) : step === 'warn' ? (
        <Space direction="vertical" size="middle" style={{ width: '100%' }}>
          <Alert
            type="warning"
            showIcon
            message="Toàn bộ dữ liệu hiện tại sẽ bị ghi đè"
            description="Mọi biểu ghi, bạn đọc và giao dịch phát sinh sau thời điểm của bản sao lưu này sẽ mất. Nếu chưa chắc chắn, hãy sao lưu hiện trạng trước khi phục hồi."
          />

          <Descriptions bordered column={1} size="small">
            <Descriptions.Item label="Tệp">{job.fileName}</Descriptions.Item>
            <Descriptions.Item label="Thời điểm sao lưu">{formatDateTime(job.startedAt)}</Descriptions.Item>
            <Descriptions.Item label="Dung lượng">{formatBytes(job.sizeBytes)}</Descriptions.Item>
            <Descriptions.Item label="Loại">{job.typeLabel}</Descriptions.Item>
          </Descriptions>
        </Space>
      ) : (
        <Space direction="vertical" size="middle" style={{ width: '100%' }}>
          <Typography.Text>
            Nhập lại mật khẩu của tài khoản đang đăng nhập để xác nhận phục hồi từ{' '}
            <Typography.Text strong>{job.fileName}</Typography.Text>.
          </Typography.Text>

          <Input.Password
            autoFocus
            value={password}
            placeholder="Mật khẩu của bạn"
            autoComplete="current-password"
            onChange={(event) => setPassword(event.target.value)}
            onPressEnter={() => password.length > 0 && mutation.mutate()}
          />
        </Space>
      )}
    </Modal>
  );
}
