import { useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  App,
  Button,
  Descriptions,
  Dropdown,
  Empty,
  Input,
  List,
  Modal,
  Rate,
  Select,
  Skeleton,
  Space,
  Tabs,
  Tag,
  Typography,
} from 'antd';
import {
  CopyOutlined,
  HeartOutlined,
  ShareAltOutlined,
  ShoppingCartOutlined,
} from '@ant-design/icons';
import { canNativeShare, copyText, shareTargets } from '@/lib/share';
import { opacApi, readerApi } from '@/api/opac';
import { Availability, Cover, HoldButton, ResultShelf } from '@/components/ResultList';
import { useAuthStore } from '@/stores/authStore';
import { useCartStore } from '@/stores/cartStore';
import { useSiteSettings } from '@/hooks/useSite';
import type { BibDetail, SearchResult } from '@/types/api';
import { formatDate } from '@/lib/datetime';
import { MarcRecordTable } from '../components/MarcRecordTable';

const { Paragraph } = Typography;

const CITATION_STYLES = [
  { value: 'Apa', label: 'APA' },
  { value: 'Mla', label: 'MLA' },
  { value: 'Chicago', label: 'Chicago' },
  { value: 'BibTex', label: 'BibTeX' },
  { value: 'Ris', label: 'RIS' },
  { value: 'EndNote', label: 'EndNote' },
];

/** Biểu ghi chi tiết nhìn như một dòng kết quả, để dùng chung bìa, nhãn tình trạng và giỏ. */
function asResult(data: BibDetail): SearchResult {
  return {
    id: data.id,
    controlNumber: data.controlNumber,
    title: data.title,
    authorMain: data.authorMain,
    publisherName: data.publisherName,
    publishYear: data.publishYear,
    isbn: data.isbn,
    ddc: data.ddc,
    documentTypeName: data.documentTypeName,
    languageName: data.languageName,
    coverImageUrl: data.coverImageUrl,
    itemCount: data.itemCount,
    availableItemCount: data.availableItemCount,
    digitalDocumentCount: data.digitalDocuments.length,
    loanCount: 0,
  };
}

/**
 * IX.2 — Chi tiết tài liệu theo bản thiết kế: thẻ đầu (bìa lớn, nhan đề, tác giả, bảng thông tin,
 * nút hành động), rồi "Bản sẵn có tại thư viện", rồi các mục còn lại — tài liệu số, mô tả đầy đủ,
 * biểu ghi MARC, nhận xét — và tài liệu liên quan.
 */
export function BibDetailPage() {
  const { id = '' } = useParams();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const user = useAuthStore((state) => state.user);
  const { data: settings } = useSiteSettings();
  const addToCart = useCartStore((state) => state.add);
  const moreRef = useRef<HTMLDivElement>(null);

  const [citationStyle, setCitationStyle] = useState('Apa');
  const [citationOpen, setCitationOpen] = useState(false);
  const [reviewOpen, setReviewOpen] = useState(false);
  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState('');
  const [tab, setTab] = useState('info');

  const { data, isLoading } = useQuery<BibDetail>({
    queryKey: ['bib', id],
    queryFn: () => opacApi.bib(id),
    enabled: Boolean(id),
  });

  const citation = useQuery({
    queryKey: ['citation', id, citationStyle],
    queryFn: () => opacApi.citation(id, citationStyle),
    enabled: citationOpen,
  });

  const favorite = useMutation({
    mutationFn: () => readerApi.toggleFavorite(id),
    onSuccess: (added) =>
      message.success(added ? 'Đã thêm vào danh sách yêu thích.' : 'Đã bỏ khỏi yêu thích.'),
    onError: (error: Error) => message.error(error.message),
  });

  const review = useMutation({
    mutationFn: () => readerApi.submitReview(id, rating, comment),
    onSuccess: () => {
      setReviewOpen(false);
      setComment('');
      message.success('Đã gửi nhận xét, thư viện sẽ duyệt trước khi hiển thị.');
    },
    onError: (error: Error) => message.error(error.message),
  });

  if (isLoading) {
    return (
      <div className="lc-detail">
        <Skeleton active paragraph={{ rows: 10 }} />
      </div>
    );
  }

  if (!data) {
    return (
      <div className="lc-detail">
        <div className="lc-empty">Không tìm thấy tài liệu.</div>
      </div>
    );
  }

  const requireLogin = (action: () => void) => {
    if (!user) {
      message.info('Bạn cần đăng nhập bằng số thẻ thư viện để dùng chức năng này.');
      navigate('/dang-nhap');
      return;
    }
    action();
  };

  const showTab = (key: string) => {
    setTab(key);
    moreRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  };

  const summary = asResult(data);

  return (
    <div className="lc-detail">
      <a
        className="lc-detail__back"
        onClick={() => (window.history.length > 1 ? navigate(-1) : navigate('/tra-cuu'))}
      >
        ← Quay lại kết quả tra cứu
      </a>

      <div className="lc-paper lc-detail__head">
        <div className="lc-detail__cover">
          <Cover item={summary} />
        </div>

        <div className="lc-detail__body">
          <h1 className="lc-detail__title">
            {data.title}
            {data.subtitle ? <span style={{ fontWeight: 400 }}>: {data.subtitle}</span> : null}
          </h1>

          <div className="lc-detail__author">
            {data.authors.length > 0
              ? data.authors.map((author, index) => (
                  <span key={author.id ?? author.name}>
                    {index > 0 ? ', ' : ''}
                    <Link to={`/tra-cuu?authorId=${author.id ?? ''}`}>{author.name}</Link>
                    {author.note ? ` (${author.note})` : ''}
                  </span>
                ))
              : (data.statementOfResponsibility ?? data.authorMain ?? 'Khuyết danh')}
          </div>

          <div className="lc-detail__grid">
            {data.publisherName || data.publishYear ? (
              <>
                <span className="lc-detail__label">Nhà xuất bản</span>
                <span>
                  {[data.publishPlace, data.publisherName, data.publishYear]
                    .filter(Boolean)
                    .join(', ')}
                </span>
              </>
            ) : null}
            {data.ddc ? (
              <>
                <span className="lc-detail__label">Phân loại DDC</span>
                <span>
                  <span className="lc-ma">{data.ddc}</span>
                </span>
              </>
            ) : null}
            {data.documentTypeName ? (
              <>
                <span className="lc-detail__label">Dạng tài liệu</span>
                <span>{data.documentTypeName}</span>
              </>
            ) : null}
            {data.languageName ? (
              <>
                <span className="lc-detail__label">Ngôn ngữ</span>
                <span>{data.languageName}</span>
              </>
            ) : null}
            {data.isbn ? (
              <>
                <span className="lc-detail__label">ISBN</span>
                <span>
                  <span className="lc-ma">{data.isbn}</span>
                </span>
              </>
            ) : null}
            {data.issn ? (
              <>
                <span className="lc-detail__label">ISSN</span>
                <span>
                  <span className="lc-ma">{data.issn}</span>
                </span>
              </>
            ) : null}
            <span className="lc-detail__label">Tình trạng</span>
            <span>
              <Availability item={summary} />
            </span>
          </div>

          <div className="lc-detail__actions">
            <HoldButton
              bibId={id}
              hasCopies={data.itemCount > 0}
              size="middle"
              onDone={() => void queryClient.invalidateQueries({ queryKey: ['bib', id] })}
            />
            <Button icon={<CopyOutlined />} onClick={() => setCitationOpen(true)}>
              Trích dẫn
            </Button>
            <Button onClick={() => showTab('marc')}>Xem MARC</Button>
            <Button
              icon={<HeartOutlined />}
              onClick={() => requireLogin(() => favorite.mutate())}
            >
              Yêu thích
            </Button>
            <Button
              icon={<ShoppingCartOutlined />}
              onClick={() => {
                addToCart(summary);
                message.success('Đã thêm vào giỏ tài liệu.');
              }}
            >
              Thêm vào giỏ
            </Button>
            <Dropdown
              menu={{
                items: [
                  { key: 'copy', label: 'Sao chép liên kết' },
                  ...(canNativeShare() ? [{ key: 'native', label: 'Chia sẻ…' }] : []),
                  { type: 'divider' as const },
                  { key: 'facebook', label: 'Facebook' },
                  { key: 'zalo', label: 'Zalo' },
                ],
                onClick: async ({ key }) => {
                  const url = window.location.href;
                  const title = data.title;
                  const targets = shareTargets(url, title);

                  if (key === 'copy') {
                    if (await copyText(url)) {
                      message.success('Đã sao chép liên kết.');
                    } else {
                      message.warning(`Trình duyệt không cho sao chép; liên kết là ${url}`);
                    }
                  } else if (key === 'native') {
                    try {
                      await navigator.share({ title, url });
                    } catch {
                      // Người dùng đóng hộp chia sẻ — không phải lỗi.
                    }
                  } else if (key === 'facebook' || key === 'zalo') {
                    window.open(targets[key], '_blank', 'noopener,noreferrer,width=640,height=480');
                  }
                },
              }}
            >
              <Button icon={<ShareAltOutlined />}>Chia sẻ</Button>
            </Dropdown>
          </div>
        </div>
      </div>

      {/*
        Bảng bản in đặt cột tình trạng cuối cùng theo bản thiết kế, nhưng ký hiệu xếp giá đứng ở
        cột thứ ba để bạn đọc cầm đi tìm sách trên giá; ĐKCB chữ đều nét để so từng ký tự.
      */}
      <section className="lc-paper lc-section">
        <div className="lc-section__title">Bản sẵn có tại thư viện</div>
        {data.items.length === 0 ? (
          <div className="lc-section__body" style={{ color: 'var(--lc-muted)' }}>
            Tài liệu này chưa có bản in trong kho.
          </div>
        ) : (
          <>
            <div className="lc-holdings__head">
              <span>Kho</span>
              <span>ĐKCB</span>
              <span>Vị trí xếp giá</span>
              <span>Trạng thái</span>
            </div>
            {data.items.map((item) => (
              <div key={item.id} className="lc-holdings__row">
                <span>
                  {item.warehouseName}
                  <div className="lc-holdings__sub">{item.libraryName}</div>
                </span>
                <span>
                  <span className="lc-ma">{item.registerNumber || item.barcode}</span>
                </span>
                <span>
                  {item.callNumber ?? '—'}
                  {item.shelfName ? <div className="lc-holdings__sub">{item.shelfName}</div> : null}
                </span>
                <span>
                  <Tag color={item.isAvailable ? 'green' : 'orange'}>{item.statusLabel}</Tag>
                  {item.dueDate ? (
                    <div className="lc-holdings__sub">Dự kiến trả {formatDate(item.dueDate)}</div>
                  ) : null}
                </span>
              </div>
            ))}
          </>
        )}
      </section>

      <section className="lc-paper lc-section" ref={moreRef}>
        <div className="lc-section__body">
          <Tabs
            activeKey={tab}
            onChange={setTab}
            items={[
              {
                key: 'info',
                label: 'Mô tả đầy đủ',
                children: (
                  <>
                    <Descriptions column={{ xs: 1, sm: 2 }} size="small" bordered>
                      {data.statementOfResponsibility ? (
                        <Descriptions.Item label="Thông tin trách nhiệm" span={2}>
                          {data.statementOfResponsibility}
                        </Descriptions.Item>
                      ) : null}
                      {data.edition ? (
                        <Descriptions.Item label="Lần xuất bản">{data.edition}</Descriptions.Item>
                      ) : null}
                      {data.pages ? (
                        <Descriptions.Item label="Mô tả vật lý">
                          {[data.pages, data.dimensions].filter(Boolean).join(' ; ')}
                        </Descriptions.Item>
                      ) : null}
                      {data.seriesName ? (
                        <Descriptions.Item label="Tùng thư">{data.seriesName}</Descriptions.Item>
                      ) : null}
                      <Descriptions.Item label="Số kiểm soát">{data.controlNumber}</Descriptions.Item>
                    </Descriptions>

                    {data.subjects.length > 0 ? (
                      <>
                        <div className="lc-nhan-nhom" style={{ margin: '16px 0 6px' }}>
                          Chủ đề
                        </div>
                        <Space size={[8, 8]} wrap>
                          {data.subjects.map((subject) => (
                            <Link
                              key={subject.id ?? subject.name}
                              to={`/tra-cuu?subjectId=${subject.id ?? ''}`}
                            >
                              <Tag>{subject.name}</Tag>
                            </Link>
                          ))}
                        </Space>
                      </>
                    ) : null}

                    {data.keywords.length > 0 ? (
                      <>
                        <div className="lc-nhan-nhom" style={{ margin: '16px 0 6px' }}>
                          Từ khóa
                        </div>
                        <Space size={[8, 8]} wrap>
                          {data.keywords.map((keyword) => (
                            <Tag key={keyword.id ?? keyword.name}>{keyword.name}</Tag>
                          ))}
                        </Space>
                      </>
                    ) : null}

                    {data.abstract ? (
                      <>
                        <div className="lc-nhan-nhom" style={{ margin: '16px 0 6px' }}>
                          Tóm tắt
                        </div>
                        <Paragraph>{data.abstract}</Paragraph>
                      </>
                    ) : null}

                    <div className="lc-nhan-nhom" style={{ margin: '16px 0 6px' }}>
                      Mô tả theo ISBD
                    </div>
                    <Paragraph type="secondary" style={{ marginBottom: 0 }}>
                      {data.isbd}
                    </Paragraph>
                  </>
                ),
              },
              {
                key: 'digital',
                label: `Tài liệu số (${data.digitalDocuments.length + data.externalLinks.length})`,
                children:
                  data.digitalDocuments.length === 0 ? (
                    data.externalLinks.length === 0 ? (
                      <Empty description="Tài liệu này chưa có bản số." />
                    ) : (
                      <List
                        header={
                          <Typography.Text type="secondary">
                            Bản toàn văn do thư viện nguồn phục vụ. Bấm để mở ở trang của họ.
                          </Typography.Text>
                        }
                        dataSource={data.externalLinks}
                        renderItem={(link) => (
                          <List.Item
                            actions={[
                              <a
                                key="open"
                                href={link.url}
                                target="_blank"
                                rel="noreferrer noopener"
                              >
                                Mở toàn văn
                              </a>,
                            ]}
                          >
                            <List.Item.Meta
                              title={link.label || link.note || 'Toàn văn tại thư viện nguồn'}
                              description={
                                <Space size={[8, 4]} wrap>
                                  <Tag color="blue">Liên kết ngoài</Tag>
                                  {link.mimeType ? <Tag>{link.mimeType}</Tag> : null}
                                  <Typography.Text type="secondary">{link.url}</Typography.Text>
                                </Space>
                              }
                            />
                          </List.Item>
                        )}
                      />
                    )
                  ) : (
                    <List
                      dataSource={data.digitalDocuments}
                      renderItem={(document) => (
                        <List.Item
                          actions={[
                            <Link key="read" to={`/tai-lieu-so/${document.id}`}>
                              {document.requiresRequest ? 'Xin quyền đọc' : 'Đọc trực tuyến'}
                            </Link>,
                          ]}
                        >
                          <List.Item.Meta
                            title={document.title}
                            description={
                              <Space size={[8, 4]} wrap>
                                <Tag>{document.accessLevelLabel}</Tag>
                                {document.pageCount ? <span>{document.pageCount} trang</span> : null}
                                <span>{(document.fileSize / 1024 / 1024).toFixed(1)} MB</span>
                                {document.allowDownload ? <Tag color="blue">Tải về được</Tag> : null}
                              </Space>
                            }
                          />
                        </List.Item>
                      )}
                    />
                  ),
              },
              {
                key: 'marc',
                label: 'Biểu ghi MARC',
                children: <MarcRecordTable marcJson={data.marcJson} />,
              },
              {
                key: 'reviews',
                label: `Nhận xét (${data.reviews.length})`,
                children: (
                  <>
                    {data.averageRating ? (
                      <Space style={{ marginBottom: 12 }}>
                        <Rate disabled allowHalf value={data.averageRating} />
                        <span>{data.averageRating}/5</span>
                      </Space>
                    ) : null}

                    {settings?.allowReview ? (
                      <Button
                        style={{ marginBottom: 12 }}
                        onClick={() => requireLogin(() => setReviewOpen(true))}
                      >
                        Viết nhận xét
                      </Button>
                    ) : null}

                    {data.reviews.length === 0 ? (
                      <Empty description="Chưa có nhận xét nào." />
                    ) : (
                      <List
                        dataSource={data.reviews}
                        renderItem={(item) => (
                          <List.Item>
                            <List.Item.Meta
                              title={
                                <Space>
                                  <span>{item.readerName}</span>
                                  <Rate disabled value={item.rating} style={{ fontSize: 14 }} />
                                </Space>
                              }
                              description={
                                <>
                                  <div>{item.comment}</div>
                                  <div style={{ fontSize: 12, color: 'var(--lc-muted)' }}>
                                    {formatDate(item.createdAt)}
                                  </div>
                                </>
                              }
                            />
                          </List.Item>
                        )}
                      />
                    )}
                  </>
                ),
              },
            ]}
          />
        </div>
      </section>

      {data.related.length > 0 ? (
        <section className="lc-paper lc-section">
          <div className="lc-section__title">Tài liệu liên quan</div>
          <div className="lc-section__body">
            <ResultShelf items={data.related} />
          </div>
        </section>
      ) : null}

      <Modal
        open={citationOpen}
        title="Xuất trích dẫn"
        onCancel={() => setCitationOpen(false)}
        footer={null}
      >
        <Select
          value={citationStyle}
          options={CITATION_STYLES}
          onChange={setCitationStyle}
          style={{ width: '100%', marginBottom: 12 }}
        />
        <Input.TextArea
          value={citation.data?.content ?? 'Đang tạo trích dẫn…'}
          autoSize={{ minRows: 4, maxRows: 14 }}
          readOnly
        />
        <Space style={{ marginTop: 12 }}>
          <Button
            type="primary"
            onClick={async () => {
              if (!citation.data) return;
              await navigator.clipboard.writeText(citation.data.content);
              message.success('Đã chép trích dẫn.');
            }}
          >
            Chép vào bộ nhớ tạm
          </Button>
          <Button
            href={`/api/bib/${id}/citation/download?style=${citationStyle}`}
            target="_blank"
            rel="noopener noreferrer"
          >
            Tải tệp trích dẫn
          </Button>
        </Space>
      </Modal>

      <Modal
        open={reviewOpen}
        title="Nhận xét về tài liệu"
        onCancel={() => setReviewOpen(false)}
        onOk={() => review.mutate()}
        confirmLoading={review.isPending}
        okText="Gửi nhận xét"
        cancelText="Hủy"
      >
        <Rate value={rating} onChange={setRating} />
        <Input.TextArea
          value={comment}
          onChange={(event) => setComment(event.target.value)}
          placeholder="Cảm nhận của bạn về tài liệu này"
          autoSize={{ minRows: 3, maxRows: 8 }}
          style={{ marginTop: 12 }}
          maxLength={2000}
          showCount
        />
      </Modal>
    </div>
  );
}
