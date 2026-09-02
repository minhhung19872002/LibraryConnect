import { useCallback, useEffect, useRef, useState } from 'react';
import {
  App,
  Alert,
  Avatar,
  Button,
  Card,
  Col,
  Descriptions,
  Empty,
  Input,
  Row,
  Space,
  Statistic,
  Table,
  Tag,
  Typography,
} from 'antd';
import {
  BookOutlined,
  CheckCircleOutlined,
  PrinterOutlined,
  RollbackOutlined,
  SyncOutlined,
} from '@ant-design/icons';
import type { InputRef } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { saveBlob } from '@/modules/marc/api';
import { useReaderPhoto } from '@/modules/readers/useReaderPhoto';
import { initials } from '@/modules/readers/labels';
import { circulationApi } from './api';
import { MAU } from '@/lib/palette';
import {
  beep,
  describeDue,
  formatDate,
  loanStatusColors,
  loanStatusLabels,
  money,
} from './labels';
import type {
  CheckoutResultDto,
  DeskReaderDto,
  LoanRowDto,
  ReturnedItemDto,
  ScanForLoanDto,
} from './types';

type DeskMode = 'checkout' | 'return' | 'renew';

const modeTitles: Record<DeskMode, string> = {
  checkout: 'Ghi mượn',
  return: 'Ghi trả',
  renew: 'Gia hạn',
};

/**
 * VII.2 — Màn hình ghi mượn / ghi trả.
 *
 * Đây là màn hình cán bộ dùng nhiều nhất trong ngày nên nó được làm cho tay và máy quét, không phải
 * cho chuột: F2 về ô quét thẻ, F3 về ô quét mã vạch, F4 hoàn tất, Esc dọn màn hình. Mỗi lần quét có
 * tiếng bíp riêng cho được và không được, vì mắt cán bộ đang nhìn chồng sách chứ không nhìn màn hình.
 *
 * Mọi quyết định — cho mượn hay không, hạn trả ngày nào, phạt bao nhiêu — đều do máy chủ trả về;
 * màn hình chỉ hiển thị.
 */
export function CirculationDeskPage() {
  const { message, modal } = App.useApp();

  const [mode, setMode] = useState<DeskMode>('checkout');
  const [card, setCard] = useState('');
  const [barcode, setBarcode] = useState('');
  const [reader, setReader] = useState<DeskReaderDto | null>(null);
  const [pending, setPending] = useState<ScanForLoanDto[]>([]);
  const [returned, setReturned] = useState<ReturnedItemDto[]>([]);
  const [renewed, setRenewed] = useState<LoanRowDto[]>([]);
  const [busy, setBusy] = useState(false);
  const [lastResult, setLastResult] = useState<CheckoutResultDto | null>(null);
  const [lastReturnSlip, setLastReturnSlip] = useState<string | null>(null);

  const cardRef = useRef<InputRef | null>(null);
  const barcodeRef = useRef<InputRef | null>(null);

  const photo = useReaderPhoto(reader?.id, Boolean(reader?.hasPhoto));

  const focusCard = useCallback(() => {
    window.setTimeout(() => cardRef.current?.focus({ cursor: 'all' }), 0);
  }, []);

  const focusBarcode = useCallback(() => {
    window.setTimeout(() => barcodeRef.current?.focus({ cursor: 'all' }), 0);
  }, []);

  const clearAll = useCallback(() => {
    setCard('');
    setBarcode('');
    setReader(null);
    setPending([]);
    setReturned([]);
    setRenewed([]);
    setLastResult(null);
    setLastReturnSlip(null);
    focusCard();
  }, [focusCard]);

  useEffect(() => {
    focusCard();
  }, [focusCard]);

  const error = useCallback(
    (text: string) => {
      beep('error');
      message.error(text);
    },
    [message],
  );

  const scanCard = useCallback(async () => {
    const value = card.trim();

    if (!value) return;

    setBusy(true);

    try {
      const found = await circulationApi.deskReader(value);

      setReader(found);
      setCard('');
      beep('ok');
      focusBarcode();
    } catch (caught) {
      setReader(null);
      error(caught instanceof ApiRequestError ? caught.message : 'Không tra được thẻ bạn đọc.');
      focusCard();
    } finally {
      setBusy(false);
    }
  }, [card, error, focusBarcode, focusCard]);

  const scanForLoan = useCallback(async () => {
    const value = barcode.trim();

    if (!value || !reader) return;

    setBusy(true);

    try {
      const scan = await circulationApi.scan(
        reader.id,
        value,
        pending.map((row) => row.barcode),
      );

      setBarcode('');

      if (!scan.allowed) {
        error(scan.warnings.find((warning) => warning.blocking)?.message ?? 'Không cho mượn được.');
        focusBarcode();
        return;
      }

      setPending((current) => [...current, scan]);
      beep('ok');
      focusBarcode();
    } catch (caught) {
      error(caught instanceof ApiRequestError ? caught.message : 'Không quét được mã vạch.');
      focusBarcode();
    } finally {
      setBusy(false);
    }
  }, [barcode, error, focusBarcode, pending, reader]);

  const scanForReturn = useCallback(async () => {
    const value = barcode.trim();

    if (!value) return;

    setBusy(true);

    try {
      const result = await circulationApi.returnItems({ barcodes: [value] });

      setBarcode('');
      setReturned((current) => [...result.items, ...current]);
      setLastReturnSlip(result.slipCode);
      beep('ok');

      const item = result.items[0];

      if (item?.holdWaiting) {
        // Sách có người đợi thì phải giữ lại quầy — cán bộ cần thấy ngay, không thể để trôi qua.
        modal.warning({
          title: 'Giữ lại tại quầy',
          content: `Tài liệu "${item.title}" đã có bạn đọc ${item.holdForReaderName} đặt giữ. ` +
            `Để riêng tại ${item.holdPickupWarehouse ?? 'quầy'} thay vì xếp lên giá.`,
        });
      } else if (item && item.fine > 0) {
        message.warning(
          `Quá hạn ${item.overdueDays} ngày, tiền phạt ${money(item.fine)} đ (biên lai ${item.fineCode}).`,
        );
      }

      focusBarcode();
    } catch (caught) {
      error(caught instanceof ApiRequestError ? caught.message : 'Không ghi trả được.');
      focusBarcode();
    } finally {
      setBusy(false);
    }
  }, [barcode, error, focusBarcode, message, modal]);

  const scanForRenew = useCallback(async () => {
    const value = barcode.trim();

    if (!value) return;

    setBusy(true);

    try {
      const loan = await circulationApi.renewByBarcode(value);

      setBarcode('');
      setRenewed((current) => [loan, ...current]);
      beep('ok');
      message.success(`Đã gia hạn tới ngày ${formatDate(loan.dueDate)}.`);
      focusBarcode();
    } catch (caught) {
      error(caught instanceof ApiRequestError ? caught.message : 'Không gia hạn được.');
      focusBarcode();
    } finally {
      setBusy(false);
    }
  }, [barcode, error, focusBarcode, message]);

  const complete = useCallback(async () => {
    if (mode !== 'checkout' || !reader || pending.length === 0) return;

    setBusy(true);

    try {
      const result = await circulationApi.checkout({
        readerId: reader.id,
        barcodes: pending.map((row) => row.barcode),
      });

      setLastResult(result);
      setPending([]);
      beep('ok');

      message.success(`Đã ghi mượn ${result.loans.length} tài liệu.`);

      if (result.failures.length > 0) {
        modal.warning({
          title: `${result.failures.length} mã vạch không ghi mượn được`,
          content: (
            <ul style={{ margin: 0, paddingLeft: 18 }}>
              {result.failures.map((failure) => (
                <li key={failure.barcode}>
                  {failure.barcode}: {failure.message}
                </li>
              ))}
            </ul>
          ),
        });
      }

      setReader(await circulationApi.deskReaderById(reader.id));
      focusBarcode();
    } catch (caught) {
      error(caught instanceof ApiRequestError ? caught.message : 'Không hoàn tất được.');
    } finally {
      setBusy(false);
    }
  }, [error, focusBarcode, message, modal, mode, pending, reader]);

  // Phím tắt của quầy: cán bộ thao tác bằng bàn phím và máy quét, không cần chuột.
  useEffect(() => {
    const handler = (event: KeyboardEvent) => {
      if (event.key === 'F2') {
        event.preventDefault();
        focusCard();
      } else if (event.key === 'F3') {
        event.preventDefault();
        focusBarcode();
      } else if (event.key === 'F4') {
        event.preventDefault();
        void complete();
      } else if (event.key === 'Escape') {
        event.preventDefault();
        clearAll();
      }
    };

    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [clearAll, complete, focusBarcode, focusCard]);

  const printSlip = async (formType: string, documentId: string) => {
    try {
      const { blob, fileName } = await circulationApi.printForm(formType, documentId);
      saveBlob(blob, fileName);
    } catch (caught) {
      error(caught instanceof ApiRequestError ? caught.message : 'Không in được phiếu.');
    }
  };

  // Nhan đề là thứ cán bộ đối chiếu với quyển sách trên tay nên nó được ưu tiên chiều rộng; các cột
  // còn lại co lại vừa đủ đọc.
  const pendingColumns: ColumnsType<ScanForLoanDto> = [
    { title: 'Mã vạch', dataIndex: 'barcode', width: 130 },
    { title: 'Nhan đề', dataIndex: 'title', ellipsis: true, width: 320 },
    { title: 'Ký hiệu xếp giá', dataIndex: 'callNumber', width: 120 },
    { title: 'Kho', dataIndex: 'warehouseName', width: 110, ellipsis: true },
    {
      title: 'Hạn trả',
      dataIndex: 'dueDate',
      width: 110,
      render: (value: string | null) => (value ? formatDate(value) : ''),
    },
    {
      title: '',
      width: 50,
      render: (_, row) => (
        <Button
          type="link"
          danger
          size="small"
          onClick={() => setPending((current) => current.filter((item) => item.barcode !== row.barcode))}
        >
          Bỏ
        </Button>
      ),
    },
  ];

  const currentLoanColumns: ColumnsType<LoanRowDto> = [
    { title: 'Mã vạch', dataIndex: 'barcode', width: 130 },
    { title: 'Nhan đề', dataIndex: 'title', ellipsis: true },
    {
      title: 'Hạn trả',
      dataIndex: 'dueDate',
      width: 170,
      render: (value: string, row) => (
        <Space direction="vertical" size={0}>
          <span>{formatDate(value)}</span>
          <Typography.Text type={row.overdueDays > 0 ? 'danger' : 'secondary'} style={{ fontSize: 12 }}>
            {describeDue(value)}
          </Typography.Text>
        </Space>
      ),
    },
    {
      title: 'Gia hạn',
      dataIndex: 'renewedCount',
      width: 100,
      render: (value: number, row) => `${value}/${row.maxRenewals}`,
    },
    {
      title: 'Phạt tạm tính',
      dataIndex: 'estimatedFine',
      width: 130,
      align: 'right',
      render: (value: number) =>
        value > 0 ? <Typography.Text type="danger">{money(value)}</Typography.Text> : '',
    },
  ];

  const returnedColumns: ColumnsType<ReturnedItemDto> = [
    { title: 'Mã vạch', dataIndex: 'barcode', width: 130 },
    { title: 'Nhan đề', dataIndex: 'title', ellipsis: true },
    { title: 'Bạn đọc', dataIndex: 'readerName', width: 200 },
    { title: 'Hạn trả', dataIndex: 'dueDate', width: 120, render: formatDate },
    {
      title: 'Quá hạn',
      dataIndex: 'overdueDays',
      width: 100,
      align: 'right',
      render: (value: number) => (value > 0 ? `${value} ngày` : ''),
    },
    {
      title: 'Tiền phạt',
      dataIndex: 'fine',
      width: 120,
      align: 'right',
      render: (value: number) =>
        value > 0 ? <Typography.Text type="danger">{money(value)}</Typography.Text> : '',
    },
    {
      title: 'Ghi chú',
      width: 220,
      render: (_, row) =>
        row.holdWaiting ? (
          <Tag color="green">Giữ cho {row.holdForReaderName}</Tag>
        ) : (
          <Tag>Xếp lên giá</Tag>
        ),
    },
  ];

  const blocking = reader?.warnings.filter((warning) => warning.blocking) ?? [];
  const notices = reader?.warnings.filter((warning) => !warning.blocking) ?? [];

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Quầy lưu thông"
        description="F2 quét thẻ · F3 quét mã vạch · F4 hoàn tất · Esc làm mới màn hình."
        actions={
          <Space>
            {(['checkout', 'return', 'renew'] as DeskMode[]).map((value) => (
              <Button
                key={value}
                type={mode === value ? 'primary' : 'default'}
                icon={
                  value === 'checkout' ? (
                    <BookOutlined />
                  ) : value === 'return' ? (
                    <RollbackOutlined />
                  ) : (
                    <SyncOutlined />
                  )
                }
                onClick={() => {
                  setMode(value);
                  setPending([]);
                  setLastResult(null);
                  if (value === 'checkout') focusCard();
                  else focusBarcode();
                }}
              >
                {modeTitles[value]}
              </Button>
            ))}
          </Space>
        }
      />

      <Row gutter={16}>
        <Col span={8}>
          <Card size="small" title="1. Quét thẻ bạn đọc (F2)">
            <Space direction="vertical" size={12} style={{ width: '100%' }}>
              <Input
                ref={cardRef}
                size="large"
                allowClear
                placeholder="Số thẻ hoặc mã sinh viên"
                value={card}
                disabled={busy}
                onChange={(event) => setCard(event.target.value)}
                onPressEnter={() => void scanCard()}
              />

              {reader ? (
                <>
                  <Space align="start">
                    <Avatar
                      shape="square"
                      size={64}
                      src={photo}
                      style={{ backgroundColor: MAU.chinh }}
                    >
                      {initials(reader.fullName)}
                    </Avatar>
                    <Space direction="vertical" size={0}>
                      <Typography.Text strong style={{ fontSize: 16 }}>
                        {reader.fullName}
                      </Typography.Text>
                      <Typography.Text type="secondary">
                        {reader.cardNumber}
                        {reader.studentCode ? ` · ${reader.studentCode}` : ''}
                      </Typography.Text>
                      <Typography.Text type="secondary">
                        {[reader.readerTypeName, reader.className].filter(Boolean).join(' · ')}
                      </Typography.Text>
                    </Space>
                  </Space>

                  <Row gutter={8}>
                    <Col span={8}>
                      <Statistic title="Đang mượn" value={reader.currentLoanCount} />
                    </Col>
                    <Col span={8}>
                      <Statistic
                        title="Còn mượn được"
                        value={Math.max(0, reader.remainingQuota)}
                        valueStyle={{ color: reader.remainingQuota > 0 ? MAU.tot : MAU.loi }}
                      />
                    </Col>
                    <Col span={8}>
                      <Statistic
                        title="Nợ phí"
                        value={money(reader.outstandingFines)}
                        valueStyle={{ color: reader.outstandingFines > 0 ? MAU.loi : undefined }}
                      />
                    </Col>
                  </Row>

                  {blocking.map((warning) => (
                    <Alert key={warning.code} type="error" showIcon message={warning.message} />
                  ))}

                  {notices.map((warning) => (
                    <Alert key={warning.code} type="warning" showIcon message={warning.message} />
                  ))}

                  <Descriptions
                    size="small"
                    column={1}
                    items={[
                      {
                        key: 'expire',
                        label: 'Hạn thẻ',
                        children: `${formatDate(reader.cardExpireDate)} (${describeDue(reader.cardExpireDate)})`,
                      },
                      { key: 'faculty', label: 'Khoa', children: reader.facultyName },
                    ]}
                  />
                </>
              ) : (
                <Empty
                  image={Empty.PRESENTED_IMAGE_SIMPLE}
                  description="Quét thẻ để bắt đầu"
                />
              )}
            </Space>
          </Card>
        </Col>

        <Col span={16}>
          <Card
            size="small"
            title={
              mode === 'checkout'
                ? '2. Quét mã vạch tài liệu (F3)'
                : mode === 'return'
                  ? 'Quét mã vạch tài liệu cần trả (F3)'
                  : 'Quét mã vạch tài liệu cần gia hạn (F3)'
            }
            extra={
              mode === 'checkout' && (
                <Can permission={PERMISSIONS.circulation.loanCreate}>
                  <Button
                    type="primary"
                    icon={<CheckCircleOutlined />}
                    disabled={!reader || pending.length === 0}
                    loading={busy}
                    onClick={() => void complete()}
                  >
                    Hoàn tất (F4)
                  </Button>
                </Can>
              )
            }
          >
            <Space direction="vertical" size={12} style={{ width: '100%' }}>
              <Input
                ref={barcodeRef}
                size="large"
                allowClear
                placeholder={
                  mode === 'checkout' && !reader
                    ? 'Quét thẻ bạn đọc trước'
                    : 'Quét hoặc nhập mã vạch rồi bấm Enter'
                }
                value={barcode}
                disabled={busy || (mode === 'checkout' && !reader)}
                onChange={(event) => setBarcode(event.target.value)}
                onPressEnter={() => {
                  if (mode === 'checkout') void scanForLoan();
                  else if (mode === 'return') void scanForReturn();
                  else void scanForRenew();
                }}
              />

              {mode === 'checkout' && (
                <Table
                  rowKey="barcode"
                  size="small"
                  dataSource={pending}
                  columns={pendingColumns}
                  scroll={{ x: 1100 }}
                  pagination={false}
                  locale={{ emptyText: 'Chưa quét tài liệu nào' }}
                  summary={() =>
                    pending.length > 0 ? (
                      <Table.Summary.Row>
                        <Table.Summary.Cell index={0} colSpan={6}>
                          <strong>Đang chuẩn bị ghi mượn {pending.length} tài liệu</strong>
                        </Table.Summary.Cell>
                      </Table.Summary.Row>
                    ) : null
                  }
                />
              )}

              {mode === 'return' && (
                <Table
                  rowKey="loanId"
                  size="small"
                  dataSource={returned}
                  columns={returnedColumns}
                  scroll={{ x: 1100 }}
                  pagination={false}
                  locale={{ emptyText: 'Chưa ghi trả tài liệu nào' }}
                />
              )}

              {mode === 'renew' && (
                <Table
                  rowKey="id"
                  size="small"
                  dataSource={renewed}
                  pagination={false}
                  locale={{ emptyText: 'Chưa gia hạn tài liệu nào' }}
                  columns={[
                    { title: 'Mã vạch', dataIndex: 'barcode', width: 140 },
                    { title: 'Nhan đề', dataIndex: 'title', ellipsis: true },
                    { title: 'Bạn đọc', dataIndex: 'readerName', width: 200 },
                    {
                      title: 'Hạn trả mới',
                      dataIndex: 'dueDate',
                      width: 140,
                      render: formatDate,
                    },
                    {
                      title: 'Số lần gia hạn',
                      dataIndex: 'renewedCount',
                      width: 130,
                      render: (value: number, row: LoanRowDto) => `${value}/${row.maxRenewals}`,
                    },
                  ]}
                />
              )}

              {lastResult && (
                <Alert
                  type="success"
                  showIcon
                  message={`Đã ghi mượn ${lastResult.loans.length} tài liệu cho ${lastResult.readerName}.`}
                  action={
                    lastResult.slipCode && (
                      <Button
                        size="small"
                        icon={<PrinterOutlined />}
                        onClick={() => void printSlip('LOAN_SLIP', lastResult.slipCode as string)}
                      >
                        In phiếu mượn
                      </Button>
                    )
                  }
                />
              )}

              {mode === 'return' && lastReturnSlip && returned.length > 0 && (
                <Alert
                  type="info"
                  showIcon
                  message={`Đã ghi trả ${returned.length} tài liệu.`}
                  action={
                    <Button
                      size="small"
                      icon={<PrinterOutlined />}
                      onClick={() => void printSlip('RETURN_SLIP', lastReturnSlip)}
                    >
                      In phiếu trả
                    </Button>
                  }
                />
              )}
            </Space>
          </Card>

          {reader && reader.currentLoans.length > 0 && (
            <Card
              size="small"
              title={`Bạn đọc đang giữ ${reader.currentLoans.length} tài liệu`}
              style={{ marginTop: 16 }}
            >
              <Table
                rowKey="id"
                size="small"
                dataSource={reader.currentLoans}
                columns={currentLoanColumns}
                scroll={{ x: 1100 }}
                pagination={false}
              />
            </Card>
          )}

          {reader && reader.readyHolds.length > 0 && (
            <Card size="small" title="Tài liệu đặt giữ đang chờ nhận" style={{ marginTop: 16 }}>
              <Table
                rowKey="id"
                size="small"
                pagination={false}
                dataSource={reader.readyHolds}
                columns={[
                  { title: 'Nhan đề', dataIndex: 'title', ellipsis: true },
                  { title: 'Mã vạch', dataIndex: 'barcode', width: 140 },
                  { title: 'Nơi nhận', dataIndex: 'pickupWarehouseName', width: 180 },
                  {
                    title: 'Hạn nhận',
                    dataIndex: 'expireDate',
                    width: 140,
                    render: formatDate,
                  },
                  {
                    title: 'Trạng thái',
                    dataIndex: 'status',
                    width: 140,
                    render: () => <Tag color="green">Sẵn sàng nhận</Tag>,
                  },
                ]}
              />
            </Card>
          )}
        </Col>
      </Row>

      <Typography.Text type="secondary" style={{ fontSize: 12 }}>
        Trạng thái phiếu mượn:{' '}
        {Object.entries(loanStatusLabels).map(([status, label]) => (
          <Tag key={status} color={loanStatusColors[status as keyof typeof loanStatusColors]}>
            {label}
          </Tag>
        ))}
      </Typography.Text>
    </Space>
  );
}
