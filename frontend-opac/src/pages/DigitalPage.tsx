import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery } from '@tanstack/react-query';
import {
  Alert,
  App,
  Button,
  Card,
  Checkbox,
  Descriptions,
  Empty,
  Input,
  List,
  Modal,
  Pagination,
  Result,
  Select,
  Skeleton,
  Space,
  Tag,
  Typography,
} from 'antd';
import { DownloadOutlined, FileTextOutlined, ReadOutlined } from '@ant-design/icons';
import { readerApi } from '@/api/opac';
import { useAuthStore } from '@/stores/authStore';
import type { DigitalDocumentRow } from '@/types/api';
import type { DigitalFilter } from '@/types/api';
import { MAU } from '@/lib/palette';
import { elapsedSeconds, REPORT_INTERVAL_MS } from '@/lib/readingTime';
import {
  MUC_TRUY_CAP,
  NHOM_DINH_DANG,
  dangLoc,
  traiCayBoSuuTap,
} from '@/lib/digitalFilters';

const { Paragraph, Title } = Typography;

const ACCESS_LABELS: Record<DigitalDocumentRow['accessLevel'], string> = {
  Public: 'Công khai',
  Internal: 'Cần đăng nhập',
  Restricted: 'Phải xin phép',
  Forbidden: 'Không phục vụ',
};

function sizeOf(bytes: number): string {
  return bytes >= 1024 * 1024
    ? `${(bytes / 1024 / 1024).toFixed(1)} MB`
    : `${Math.max(1, Math.round(bytes / 1024))} KB`;
}

/**
 * IX.4 — Danh sách tài liệu số của thư viện.
 *
 * Liệt kê chính các tài liệu số chứ không phải các biểu ghi có đính kèm tài liệu số: thư viện số
 * hóa nhiều thứ không gắn với biểu ghi thư mục nào — bài giảng, đề tài nghiên cứu, ảnh tư liệu — và
 * bạn đọc vẫn phải tìm ra chúng.
 */
export function DigitalPage() {
  const [keyword, setKeyword] = useState('');
  const [page, setPage] = useState(1);
  const [filter, setFilter] = useState<DigitalFilter>({});

  const collections = useQuery({
    queryKey: ['digital-collections'],
    queryFn: () => readerApi.digitalCollections(),
    staleTime: 5 * 60 * 1000,
  });

  const { data, isLoading } = useQuery({
    queryKey: ['digital-documents', page, keyword, filter],
    queryFn: () => readerApi.digitalDocuments(page, keyword || undefined, filter),
  });

  const doiLoc = (thayDoi: Partial<DigitalFilter>) => {
    setFilter((truoc) => ({ ...truoc, ...thayDoi }));
    setPage(1);
  };

  return (
    <div className="lc-container" style={{ padding: '24px 16px 48px' }}>
      <Card title="Tài liệu số">
        <Paragraph type="secondary">
          Tài liệu công khai đọc được ngay; tài liệu nội bộ cần đăng nhập bằng số thẻ; tài liệu hạn
          chế phải gửi yêu cầu và chờ thư viện duyệt.
        </Paragraph>

        <Space direction="vertical" size={12} style={{ width: '100%', marginBottom: 16 }}>
          <Input.Search
            placeholder={
              filter.fullText
                ? 'Tìm trong nội dung tài liệu'
                : 'Tìm theo nhan đề tài liệu số'
            }
            allowClear
            enterButton
            style={{ maxWidth: 520 }}
            onSearch={(value) => {
              setKeyword(value);
              setPage(1);
            }}
          />

          <Space size={[8, 8]} wrap>
            <Select
              allowClear
              style={{ minWidth: 240 }}
              placeholder="Bộ sưu tập"
              value={filter.collectionId}
              options={traiCayBoSuuTap(collections.data)}
              loading={collections.isLoading}
              onChange={(value) => doiLoc({ collectionId: value })}
            />

            <Select
              allowClear
              style={{ minWidth: 180 }}
              placeholder="Định dạng"
              value={filter.formatGroup}
              options={NHOM_DINH_DANG}
              onChange={(value) => doiLoc({ formatGroup: value })}
            />

            <Select
              allowClear
              style={{ minWidth: 210 }}
              placeholder="Mức truy cập"
              value={filter.accessLevel}
              options={MUC_TRUY_CAP}
              onChange={(value) => doiLoc({ accessLevel: value })}
            />

            <Checkbox
              checked={filter.fullText ?? false}
              onChange={(event) => doiLoc({ fullText: event.target.checked })}
            >
              Tìm trong nội dung
            </Checkbox>

            {dangLoc(filter) ? (
              <Button
                type="link"
                onClick={() => {
                  setFilter({});
                  setPage(1);
                }}
              >
                Bỏ lọc
              </Button>
            ) : null}
          </Space>
        </Space>

        <List
          loading={isLoading}
          dataSource={data?.items ?? []}
          locale={{ emptyText: <Empty description="Chưa có tài liệu số nào." /> }}
          renderItem={(item) => (
            <List.Item
              actions={[
                <Link key="open" to={`/tai-lieu-so/${item.id}`}>
                  {item.accessLevel === 'Restricted' ? 'Xin quyền đọc' : 'Đọc trực tuyến'}
                </Link>,
              ]}
            >
              <List.Item.Meta
                avatar={<FileTextOutlined style={{ fontSize: 26, color: 'var(--lc-green)' }} />}
                title={<Link to={`/tai-lieu-so/${item.id}`}>{item.title}</Link>}
                description={
                  <Space size={[8, 4]} wrap>
                    <Tag color={item.accessLevel === 'Public' ? 'green' : 'blue'}>
                      {ACCESS_LABELS[item.accessLevel]}
                    </Tag>
                    {item.collectionName ? <Tag>{item.collectionName}</Tag> : null}
                    {item.pageCount ? <span>{item.pageCount} trang</span> : null}
                    <span>{sizeOf(item.fileSize)}</span>
                    {item.bibTitle ? <span>Thuộc: {item.bibTitle}</span> : null}
                  </Space>
                }
              />
            </List.Item>
          )}
        />

        {data && data.totalCount > data.pageSize ? (
          <div style={{ textAlign: 'right', marginTop: 16 }}>
            <Pagination
              current={data.page}
              pageSize={data.pageSize}
              total={data.totalCount}
              showSizeChanger={false}
              onChange={setPage}
            />
          </div>
        ) : null}
      </Card>
    </div>
  );
}

/**
 * IX.4 — Trình đọc trực tuyến.
 *
 * Từng trang tải về dưới dạng ảnh đã đóng chữ chìm ở máy chủ, không phải tệp gốc — đó là cách duy
 * nhất giữ được tài liệu không cho tải khi thư viện không cho phép. Ảnh phải lấy bằng lời gọi có
 * mang mã đăng nhập, nên không đặt thẳng vào thẻ ảnh được.
 */
export function DigitalViewerPage() {
  const { id = '' } = useParams();
  const navigate = useNavigate();
  const { message } = App.useApp();
  const user = useAuthStore((state) => state.user);

  const [page, setPage] = useState(1);
  const [requestOpen, setRequestOpen] = useState(false);
  const [reason, setReason] = useState('');

  const detail = useQuery({
    queryKey: ['digital-document', id],
    queryFn: () => readerApi.digitalDocument(id),
    enabled: Boolean(id),
  });

  const canRead = detail.data?.permission.canRead === true;

  const session = useQuery({
    queryKey: ['digital-session', id],
    queryFn: () => readerApi.openDigital(id),
    enabled: Boolean(id) && canRead,
  });

  // Thời lượng đọc (V.2): tính từ lúc máy chủ mở lượt đọc; báo định kỳ và một lần cuối khi rời
  // trang hoặc ẩn thẻ — tổng số giây, nên gọi lặp không cộng dồn sai.
  const sessionOpened = session.isSuccess;

  useEffect(() => {
    if (!id || !sessionOpened) return undefined;

    const startedAt = Date.now();
    const report = () => readerApi.reportReadingTime(id, elapsedSeconds(startedAt, Date.now()));
    const onHide = () => {
      if (window.document.visibilityState === 'hidden') report();
    };

    const timer = window.setInterval(report, REPORT_INTERVAL_MS);
    window.addEventListener('pagehide', report);
    window.document.addEventListener('visibilitychange', onHide);

    return () => {
      window.clearInterval(timer);
      window.removeEventListener('pagehide', report);
      window.document.removeEventListener('visibilitychange', onHide);
      report();
    };
  }, [id, sessionOpened]);

  const pageImage = useQuery({
    queryKey: ['digital-page', id, page],
    queryFn: () => readerApi.digitalPage(id, page),
    enabled: Boolean(id) && canRead,
  });

  const request = useMutation({
    mutationFn: () => readerApi.requestDigitalAccess(id, reason),
    onSuccess: () => {
      setRequestOpen(false);
      setReason('');
      message.success('Đã gửi yêu cầu, thư viện sẽ phản hồi sớm.');
      void detail.refetch();
    },
    onError: (error: Error) => message.error(error.message),
  });

  if (detail.isLoading) {
    return (
      <div className="lc-container" style={{ padding: 24 }}>
        <Skeleton active paragraph={{ rows: 8 }} />
      </div>
    );
  }

  if (!detail.data) {
    return (
      <Result
        status="404"
        title="Không tìm thấy tài liệu số"
        extra={<Link to="/tai-lieu-so">Về danh sách tài liệu số</Link>}
      />
    );
  }

  const document = detail.data.document;
  const permission = detail.data.permission;
  const readable = session.data?.readablePages ?? permission.readablePages;

  return (
    <div className="lc-container" style={{ padding: '24px 16px 48px' }}>
      <Card>
        <Title level={3} style={{ marginTop: 0 }}>
          {document.title}
        </Title>

        <Descriptions column={{ xs: 1, sm: 2 }} size="small" style={{ marginBottom: 16 }}>
          <Descriptions.Item label="Mức truy cập">
            {ACCESS_LABELS[document.accessLevel]}
          </Descriptions.Item>
          {document.collectionName ? (
            <Descriptions.Item label="Bộ sưu tập">{document.collectionName}</Descriptions.Item>
          ) : null}
          {document.pageCount ? (
            <Descriptions.Item label="Số trang">{document.pageCount}</Descriptions.Item>
          ) : null}
          <Descriptions.Item label="Dung lượng">{sizeOf(document.fileSize)}</Descriptions.Item>
          {document.bibId ? (
            <Descriptions.Item label="Thuộc tài liệu">
              <Link to={`/tai-lieu/${document.bibId}`}>{document.bibTitle}</Link>
            </Descriptions.Item>
          ) : null}
        </Descriptions>

        {!canRead ? (
          <Alert
            type="warning"
            showIcon
            message={permission.reason}
            action={
              permission.needsRequest ? (
                <Button
                  type="primary"
                  onClick={() => {
                    if (!user) {
                      message.info('Bạn cần đăng nhập bằng số thẻ thư viện để gửi yêu cầu.');
                      navigate('/dang-nhap');
                      return;
                    }
                    setRequestOpen(true);
                  }}
                >
                  Gửi yêu cầu đọc
                </Button>
              ) : undefined
            }
          />
        ) : (
          <>
            <Space wrap style={{ marginBottom: 12 }}>
              <Tag icon={<ReadOutlined />} color="green">
                {readable == null
                  ? 'Đọc được toàn bộ tài liệu'
                  : `Chỉ xem thử ${readable} trang đầu`}
              </Tag>

              {permission.canDownload ? (
                <Button
                  icon={<DownloadOutlined />}
                  onClick={async () => {
                    try {
                      const file = await readerApi.downloadDigital(id);
                      const url = URL.createObjectURL(file);
                      const anchor = window.document.createElement('a');
                      anchor.href = url;
                      anchor.download = document.fileName;
                      anchor.click();
                      URL.revokeObjectURL(url);
                    } catch (error) {
                      message.error((error as Error).message);
                    }
                  }}
                >
                  Tải về
                </Button>
              ) : (
                <Tag>Thư viện không cho tải tài liệu này về</Tag>
              )}
            </Space>

            <div
              style={{
                background: MAU.nen,
                padding: 16,
                borderRadius: 8,
                textAlign: 'center',
                minHeight: 360,
              }}
            >
              {pageImage.isFetching ? (
                <Skeleton active paragraph={{ rows: 10 }} />
              ) : pageImage.data ? (
                <img
                  src={pageImage.data}
                  alt={`Trang ${page}`}
                  style={{ maxWidth: '100%', boxShadow: '0 2px 12px rgba(0,0,0,.15)' }}
                />
              ) : (
                <Empty description="Không mở được trang này." />
              )}
            </div>

            <div style={{ textAlign: 'center', marginTop: 16 }}>
              <Pagination
                current={page}
                pageSize={1}
                total={readable ?? session.data?.pageCount ?? 1}
                showSizeChanger={false}
                showQuickJumper
                onChange={setPage}
              />
            </div>
          </>
        )}
      </Card>

      <Modal
        open={requestOpen}
        title="Gửi yêu cầu đọc tài liệu hạn chế"
        okText="Gửi yêu cầu"
        cancelText="Hủy"
        confirmLoading={request.isPending}
        onCancel={() => setRequestOpen(false)}
        onOk={() => request.mutate()}
      >
        <p>Cho thư viện biết bạn cần tài liệu này để làm gì:</p>
        <Input.TextArea
          value={reason}
          onChange={(event) => setReason(event.target.value)}
          rows={3}
          maxLength={500}
          showCount
        />
      </Modal>
    </div>
  );
}
