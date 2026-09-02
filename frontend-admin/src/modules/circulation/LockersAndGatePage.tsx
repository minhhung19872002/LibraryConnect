import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Col,
  Input,
  Modal,
  Row,
  Select,
  Space,
  Statistic,
  Table,
  Tabs,
  Tag,
  Typography,
} from 'antd';
import { LoginOutlined, ReloadOutlined } from '@ant-design/icons';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { circulationApi } from './api';
import {
  beep,
  formatDateTime,
  lockerStatusColors,
  lockerStatusLabels,
} from './labels';
import type { GateScanResultDto, LockerRowDto, LockerStatus, VisitRowDto } from './types';
import { MAU } from '@/lib/palette';

/**
 * VII.2 và VII.3 — Ghi nhận ra vào thư viện và quản lý tủ gửi đồ.
 *
 * Hai việc này đứng chung một màn hình vì chúng xảy ra ở cùng một chỗ: quầy trước cửa. Bạn đọc tới,
 * quét thẻ vào, gửi đồ; lúc về thì lấy đồ và quét thẻ ra.
 */
export function LockersAndGatePage() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [card, setCard] = useState('');
  const [lastScan, setLastScan] = useState<GateScanResultDto | null>(null);
  const [area, setArea] = useState<string | undefined>(undefined);
  const [assigning, setAssigning] = useState<LockerRowDto | null>(null);
  const [assignCard, setAssignCard] = useState('');
  const [keyNumber, setKeyNumber] = useState('');

  const lockers = useQuery({
    queryKey: ['circulation-lockers', area],
    queryFn: () => circulationApi.lockerMap({ area }),
    refetchInterval: 30_000,
  });

  const visits = useQuery({
    queryKey: ['circulation-visits-inside'],
    queryFn: () => circulationApi.visits({ insideOnly: true, page: 1, pageSize: 50 }),
    placeholderData: keepPreviousData,
    refetchInterval: 30_000,
  });

  const scanGate = useMutation({
    mutationFn: (cardNumber: string) => circulationApi.scanGate({ cardNumber, gate: 'Cổng chính' }),
    onSuccess: (result) => {
      setLastScan(result);
      setCard('');
      beep('ok');
      message.success(result.message);
      void queryClient.invalidateQueries({ queryKey: ['circulation-visits-inside'] });
    },
    onError: (error) => {
      beep('error');
      message.error(error instanceof ApiRequestError ? error.message : 'Không ghi nhận được.');
    },
  });

  const assign = useMutation({
    mutationFn: ({ id, cardNumber, key }: { id: string; cardNumber: string; key: string }) =>
      circulationApi.assignLocker(id, { cardNumber, keyNumber: key }),
    onSuccess: (locker) => {
      message.success(`Đã giao tủ ${locker.code} cho ${locker.readerName}.`);
      setAssigning(null);
      setAssignCard('');
      setKeyNumber('');
      void queryClient.invalidateQueries({ queryKey: ['circulation-lockers'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không giao tủ được.'),
  });

  const release = useMutation({
    mutationFn: (lockerId: string) => circulationApi.releaseLocker({ lockerId }),
    onSuccess: (usage) => {
      message.success(`Đã nhận lại tủ ${usage.lockerCode}.`);
      void queryClient.invalidateQueries({ queryKey: ['circulation-lockers'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không nhận lại được.'),
  });

  const visitColumns: ColumnsType<VisitRowDto> = [
    { title: 'Bạn đọc', dataIndex: 'readerName', width: 230 },
    { title: 'Số thẻ', dataIndex: 'readerCardNumber', width: 140 },
    { title: 'Loại bạn đọc', dataIndex: 'readerTypeName', width: 150 },
    { title: 'Khoa', dataIndex: 'facultyName', ellipsis: true },
    { title: 'Vào lúc', dataIndex: 'checkinAt', width: 170, render: formatDateTime },
    { title: 'Cổng', dataIndex: 'gate', width: 130 },
  ];

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Cổng và tủ gửi đồ"
        description="Quét thẻ tại cổng để ghi nhận vào ra; giao và nhận lại tủ gửi đồ theo sơ đồ."
      />

      <Tabs
        defaultActiveKey="gate"
        items={[
          {
            key: 'gate',
            label: `Ra vào thư viện (${visits.data?.totalCount ?? 0} người đang ở trong)`,
            children: (
              <Space direction="vertical" size={16} style={{ width: '100%' }}>
                <Card size="small" title="Quét thẻ tại cổng">
                  <Space direction="vertical" size={12} style={{ width: '100%' }}>
                    <Space>
                      <Input
                        autoFocus
                        size="large"
                        style={{ width: 320 }}
                        placeholder="Quét thẻ — lần đầu là vào, lần sau là ra"
                        value={card}
                        onChange={(event) => setCard(event.target.value)}
                        onPressEnter={() => scanGate.mutate(card.trim())}
                      />
                      <Button
                        type="primary"
                        size="large"
                        icon={<LoginOutlined />}
                        loading={scanGate.isPending}
                        onClick={() => scanGate.mutate(card.trim())}
                      >
                        Ghi nhận
                      </Button>
                    </Space>

                    {lastScan && (
                      <Alert
                        type={lastScan.checkedIn ? 'success' : 'info'}
                        showIcon
                        message={lastScan.message}
                        description={
                          lastScan.reader.warnings.length > 0 ? (
                            <ul style={{ margin: 0, paddingLeft: 18 }}>
                              {lastScan.reader.warnings.map((warning) => (
                                <li key={warning.code}>{warning.message}</li>
                              ))}
                            </ul>
                          ) : undefined
                        }
                      />
                    )}
                  </Space>
                </Card>

                <Card size="small" title="Bạn đọc đang ở trong thư viện">
                  <Table
                    rowKey="id"
                    size="small"
                    loading={visits.isFetching}
                    dataSource={visits.data?.items ?? []}
                    columns={visitColumns}
                    scroll={{ x: 1100 }}
                    pagination={false}
                    locale={{ emptyText: 'Chưa có bạn đọc nào trong thư viện' }}
                  />
                </Card>
              </Space>
            ),
          },
          {
            key: 'lockers',
            label: `Tủ gửi đồ (${lockers.data?.free ?? 0} tủ trống)`,
            children: (
              <Space direction="vertical" size={16} style={{ width: '100%' }}>
                <Row gutter={16}>
                  <Col span={6}>
                    <Card size="small">
                      <Statistic
                        title="Tủ trống"
                        value={lockers.data?.free ?? 0}
                        valueStyle={{ color: MAU.tot }}
                      />
                    </Card>
                  </Col>
                  <Col span={6}>
                    <Card size="small">
                      <Statistic title="Đang dùng" value={lockers.data?.inUse ?? 0} />
                    </Card>
                  </Col>
                  <Col span={6}>
                    <Card size="small">
                      <Statistic
                        title="Quá giờ chưa trả"
                        value={lockers.data?.overdue ?? 0}
                        valueStyle={{ color: (lockers.data?.overdue ?? 0) > 0 ? MAU.loi : undefined }}
                      />
                    </Card>
                  </Col>
                  <Col span={6}>
                    <Card size="small">
                      <Statistic title="Hỏng / khóa" value={lockers.data?.broken ?? 0} />
                    </Card>
                  </Col>
                </Row>

                <Space>
                  <Select
                    allowClear
                    style={{ width: 200 }}
                    placeholder="Tất cả khu vực"
                    options={(lockers.data?.areas ?? []).map((value) => ({ value, label: value }))}
                    value={area}
                    onChange={setArea}
                  />
                  <Button
                    icon={<ReloadOutlined />}
                    onClick={() => void queryClient.invalidateQueries({ queryKey: ['circulation-lockers'] })}
                  >
                    Làm mới
                  </Button>
                  <Typography.Text type="secondary">
                    Bấm vào ô tủ để giao hoặc nhận lại.
                  </Typography.Text>
                </Space>

                <Card size="small" title="Sơ đồ tủ">
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
                    {(lockers.data?.lockers ?? []).map((locker) => (
                      <button
                        key={locker.id}
                        type="button"
                        title={
                          locker.readerName
                            ? `${locker.readerName} — giữ ${locker.minutesInUse} phút`
                            : lockerStatusLabels[locker.status]
                        }
                        onClick={() => {
                          if (locker.status === 'Free') {
                            setAssigning(locker);
                          } else if (locker.status === 'InUse') {
                            release.mutate(locker.id);
                          }
                        }}
                        style={{
                          width: 84,
                          height: 64,
                          borderRadius: 6,
                          border: locker.overdue ? '2px solid ${MAU.loi}' : '1px solid ${MAU.vien}',
                          background: lockerStatusColors[locker.status as LockerStatus],
                          color: MAU.giay,
                          cursor: locker.status === 'Broken' ? 'not-allowed' : 'pointer',
                          display: 'flex',
                          flexDirection: 'column',
                          alignItems: 'center',
                          justifyContent: 'center',
                          fontSize: 12,
                        }}
                      >
                        <strong style={{ fontSize: 15 }}>{locker.code}</strong>
                        <span>{lockerStatusLabels[locker.status]}</span>
                        {/* Máy chủ bỏ hẳn trường này khi tủ đang trống, nên phải so lỏng để bắt cả
                            null lẫn undefined — nếu không ô tủ trống hiện trơ chữ "phút". */}
                        {locker.minutesInUse != null && <span>{locker.minutesInUse} phút</span>}
                      </button>
                    ))}
                  </div>

                  <Space style={{ marginTop: 12 }} wrap>
                    {(Object.keys(lockerStatusLabels) as LockerStatus[]).map((status) => (
                      <Tag key={status} color={lockerStatusColors[status]}>
                        {lockerStatusLabels[status]}
                      </Tag>
                    ))}
                  </Space>
                </Card>
              </Space>
            ),
          },
        ]}
      />

      <Modal
        open={assigning !== null}
        title={assigning ? `Giao tủ ${assigning.code}` : ''}
        okText="Giao tủ"
        cancelText="Hủy"
        confirmLoading={assign.isPending}
        onCancel={() => setAssigning(null)}
        onOk={() => {
          if (assigning) {
            assign.mutate({ id: assigning.id, cardNumber: assignCard.trim(), key: keyNumber.trim() });
          }
        }}
      >
        <Can permission={PERMISSIONS.circulation.lockerManage}>
          <Space direction="vertical" style={{ width: '100%' }}>
            <Input
              autoFocus
              placeholder="Quét thẻ bạn đọc"
              value={assignCard}
              onChange={(event) => setAssignCard(event.target.value)}
            />
            <Input
              placeholder="Số chìa khóa (nếu có)"
              value={keyNumber}
              onChange={(event) => setKeyNumber(event.target.value)}
            />
          </Space>
        </Can>
      </Modal>
    </Space>
  );
}
