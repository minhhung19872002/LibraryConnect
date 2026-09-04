import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Col,
  Form,
  Input,
  InputNumber,
  Modal,
  Popconfirm,
  Row,
  Select,
  Space,
  Statistic,
  Table,
  Tabs,
  Tag,
  Typography,
} from 'antd';
import { LoginOutlined, PlusOutlined, ReloadOutlined, ToolOutlined } from '@ant-design/icons';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { locationsApi } from '@/modules/acquisition/api';
import { circulationApi } from './api';
import {
  beep,
  buildLockerGrid,
  formatDateTime,
  lockerGridKey,
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
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [card, setCard] = useState('');
  const [lastScan, setLastScan] = useState<GateScanResultDto | null>(null);
  const [area, setArea] = useState<string | undefined>(undefined);
  const [assigning, setAssigning] = useState<LockerRowDto | null>(null);
  const [assignCard, setAssignCard] = useState('');
  const [keyNumber, setKeyNumber] = useState('');
  // Thêm/sửa tủ: null là đóng; { locker: null } là thêm mới.
  const [editing, setEditing] = useState<{ locker: LockerRowDto | null } | null>(null);
  const [lockerForm] = Form.useForm();

  const libraries = useQuery({
    queryKey: ['locations-libraries'],
    queryFn: () => locationsApi.libraries(),
    staleTime: 5 * 60 * 1000,
  });

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

  const saveLocker = useMutation({
    mutationFn: (payload: Record<string, unknown>) => circulationApi.saveLocker(payload),
    onSuccess: () => {
      message.success('Đã lưu tủ gửi đồ.');
      setEditing(null);
      lockerForm.resetFields();
      void queryClient.invalidateQueries({ queryKey: ['circulation-lockers'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được tủ.'),
  });

  const deleteLocker = useMutation({
    mutationFn: (id: string) => circulationApi.deleteLocker(id),
    onSuccess: () => {
      message.success('Đã xóa tủ.');
      void queryClient.invalidateQueries({ queryKey: ['circulation-lockers'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xóa được tủ.'),
  });

  /** Báo hỏng / sửa xong: chỉ đổi trạng thái, giữ nguyên mọi thông tin khác của tủ. */
  const setLockerStatus = (locker: LockerRowDto, status: LockerStatus, note?: string) =>
    saveLocker.mutate({
      id: locker.id,
      code: locker.code,
      libraryId: locker.libraryId,
      area: locker.area,
      size: locker.size,
      status,
      mapRow: locker.mapRow,
      mapColumn: locker.mapColumn,
      note: note ?? locker.note,
    });

  const openEditor = (locker: LockerRowDto | null) => {
    lockerForm.setFieldsValue(
      locker
        ? {
            code: locker.code,
            libraryId: locker.libraryId ?? undefined,
            area: locker.area ?? undefined,
            size: locker.size ?? undefined,
            status: locker.status,
            mapRow: locker.mapRow ?? undefined,
            mapColumn: locker.mapColumn ?? undefined,
            note: locker.note ?? undefined,
          }
        : { status: 'Free', area: area ?? undefined },
    );
    setEditing({ locker });
  };

  const grid = buildLockerGrid(lockers.data?.lockers ?? []);

  const renderLocker = (locker: LockerRowDto) => (
    <button
      key={locker.id}
      type="button"
      title={
        locker.readerName
          ? `${locker.readerName} — giữ ${locker.minutesInUse} phút`
          : locker.status === 'Broken' && locker.note
            ? `${lockerStatusLabels[locker.status]}: ${locker.note}`
            : lockerStatusLabels[locker.status]
      }
      onClick={() => {
        if (locker.status === 'Free') {
          setAssigning(locker);
        } else if (locker.status === 'InUse') {
          release.mutate(locker.id);
        } else {
          openEditor(locker);
        }
      }}
      style={{
        width: 84,
        height: 64,
        borderRadius: 6,
        border: locker.overdue ? `2px solid ${MAU.loi}` : `1px solid ${MAU.vien}`,
        background: lockerStatusColors[locker.status as LockerStatus],
        color: MAU.giay,
        cursor: 'pointer',
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
  );

  const lockerColumns: ColumnsType<LockerRowDto> = [
    { title: 'Số tủ', dataIndex: 'code', width: 100 },
    { title: 'Khu vực', dataIndex: 'area', width: 110 },
    { title: 'Cơ sở', dataIndex: 'libraryName', width: 160, ellipsis: true },
    { title: 'Cỡ', dataIndex: 'size', width: 80 },
    {
      title: 'Vị trí (hàng, cột)',
      width: 130,
      render: (_, row) => (row.mapRow != null && row.mapColumn != null ? `${row.mapRow}, ${row.mapColumn}` : 'Chưa xếp'),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      width: 120,
      render: (status: LockerStatus) => <Tag color={lockerStatusColors[status]}>{lockerStatusLabels[status]}</Tag>,
    },
    { title: 'Ghi chú', dataIndex: 'note', ellipsis: true },
    {
      title: '',
      width: 260,
      render: (_, row) => (
        <Space size={4} wrap>
          <Button type="link" size="small" onClick={() => openEditor(row)}>
            Sửa
          </Button>
          {row.status === 'Broken' ? (
            <Button type="link" size="small" onClick={() => setLockerStatus(row, 'Free')}>
              Đã sửa xong
            </Button>
          ) : (
            <Button
              type="link"
              size="small"
              danger
              disabled={row.status === 'InUse'}
              title={row.status === 'InUse' ? 'Tủ đang có người dùng; nhận lại tủ trước' : undefined}
              onClick={() => {
                let note = row.note ?? '';

                modal.confirm({
                  title: `Báo hỏng tủ ${row.code}`,
                  content: (
                    <Input.TextArea
                      rows={2}
                      defaultValue={note}
                      placeholder="Hỏng gì: khóa kẹt, mất chìa, bản lề gãy…"
                      onChange={(event) => {
                        note = event.target.value;
                      }}
                    />
                  ),
                  okText: 'Báo hỏng',
                  okButtonProps: { danger: true },
                  cancelText: 'Hủy',
                  onOk: () => setLockerStatus(row, 'Broken', note.trim() || row.note || undefined),
                });
              }}
            >
              Báo hỏng
            </Button>
          )}
          <Popconfirm
            title="Xóa tủ này? Tủ đã có lịch sử sử dụng thì không xóa được."
            okText="Xóa"
            cancelText="Hủy"
            onConfirm={() => deleteLocker.mutate(row.id)}
          >
            <Button type="link" size="small" danger>
              Xóa
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

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
                    Bấm vào ô tủ để giao hoặc nhận lại; tủ hỏng bấm vào để sửa thông tin.
                  </Typography.Text>
                  <Can permission={PERMISSIONS.circulation.lockerManage}>
                    <Button icon={<PlusOutlined />} onClick={() => openEditor(null)}>
                      Thêm tủ
                    </Button>
                  </Can>
                </Space>

                <Card size="small" title="Sơ đồ tủ">
                  {grid.rows > 0 && (
                    // Vẽ đúng hàng/cột đã khai cho từng tủ, để sơ đồ trên màn hình trùng với dãy tủ
                    // ngoài sảnh; ô trống là chỗ chưa đặt tủ hoặc lối đi.
                    <div
                      style={{
                        display: 'grid',
                        gridTemplateColumns: `repeat(${grid.columns}, 84px)`,
                        gap: 8,
                        overflowX: 'auto',
                        paddingBottom: 4,
                      }}
                    >
                      {Array.from({ length: grid.rows }, (_, rowIndex) =>
                        Array.from({ length: grid.columns }, (__, columnIndex) => {
                          const locker = grid.placed.get(lockerGridKey(rowIndex + 1, columnIndex + 1));

                          return locker ? (
                            renderLocker(locker)
                          ) : (
                            <div
                              key={`empty-${rowIndex}-${columnIndex}`}
                              style={{ width: 84, height: 64, borderRadius: 6, border: `1px dashed ${MAU.vien}` }}
                            />
                          );
                        }),
                      )}
                    </div>
                  )}

                  {grid.unplaced.length > 0 && (
                    <div style={{ marginTop: grid.rows > 0 ? 12 : 0 }}>
                      {grid.rows > 0 && (
                        <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 8 }}>
                          Tủ chưa khai vị trí trên sơ đồ — sửa tủ để đặt hàng và cột.
                        </Typography.Text>
                      )}
                      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
                        {grid.unplaced.map(renderLocker)}
                      </div>
                    </div>
                  )}

                  {grid.rows === 0 && grid.unplaced.length === 0 && (
                    <Typography.Text type="secondary">Chưa có tủ nào. Bấm Thêm tủ để khai.</Typography.Text>
                  )}

                  <Space style={{ marginTop: 12 }} wrap>
                    {(Object.keys(lockerStatusLabels) as LockerStatus[]).map((status) => (
                      <Tag key={status} color={lockerStatusColors[status]}>
                        {lockerStatusLabels[status]}
                      </Tag>
                    ))}
                  </Space>
                </Card>

                <Can permission={PERMISSIONS.circulation.lockerManage}>
                  <Card size="small" title="Danh sách tủ">
                    <Table
                      rowKey="id"
                      size="small"
                      loading={lockers.isFetching}
                      dataSource={lockers.data?.lockers ?? []}
                      columns={lockerColumns}
                      pagination={{ pageSize: 20 }}
                      scroll={{ x: 1100 }}
                    />
                  </Card>
                </Can>
              </Space>
            ),
          },
        ]}
      />

      <Modal
        open={editing !== null}
        title={editing?.locker ? `Sửa tủ ${editing.locker.code}` : 'Thêm tủ gửi đồ'}
        okText="Lưu"
        cancelText="Hủy"
        confirmLoading={saveLocker.isPending}
        onCancel={() => setEditing(null)}
        onOk={() => {
          lockerForm
            .validateFields()
            .then((values) => saveLocker.mutate({ ...values, id: editing?.locker?.id }))
            .catch(() => undefined);
        }}
      >
        <Form form={lockerForm} layout="vertical">
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="code" label="Số tủ" rules={[{ required: true, message: 'Chưa nhập số tủ.' }]}>
                <Input placeholder="Ví dụ A07" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="area" label="Khu vực">
                <Input placeholder="Ví dụ A, B hoặc Sảnh chính" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="libraryId" label="Cơ sở">
                <Select
                  allowClear
                  placeholder="Dùng chung"
                  options={(libraries.data ?? []).map((library) => ({ value: library.id, label: library.name }))}
                />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="size" label="Cỡ tủ">
                <Input placeholder="Nhỏ / Vừa / Lớn" />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="mapRow" label="Hàng trên sơ đồ">
                <InputNumber<number> min={1} max={50} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="mapColumn" label="Cột trên sơ đồ">
                <InputNumber<number> min={1} max={50} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="status" label="Trạng thái">
                <Select
                  options={(Object.keys(lockerStatusLabels) as LockerStatus[])
                    .filter((status) => status !== 'InUse')
                    .map((status) => ({ value: status, label: lockerStatusLabels[status] }))}
                />
              </Form.Item>
            </Col>
            <Col span={24}>
              <Form.Item name="note" label="Ghi chú">
                <Input.TextArea rows={2} placeholder="Ví dụ: khóa kẹt, chờ thay" />
              </Form.Item>
            </Col>
          </Row>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            <ToolOutlined /> Tủ đang có người dùng không đổi trạng thái được; nhận lại tủ trước.
          </Typography.Text>
        </Form>
      </Modal>

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
