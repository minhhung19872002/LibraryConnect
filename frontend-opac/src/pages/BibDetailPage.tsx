import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  App,
  Button,
  Card,
  Col,
  Descriptions,
  Divider,
  Empty,
  Input,
  List,
  Modal,
  Rate,
  Row,
  Select,
  Skeleton,
  Space,
  Table,
  Tabs,
  Tag,
  Typography,
} from 'antd';
import {
  BookOutlined,
  CopyOutlined,
  HeartOutlined,
  ShoppingCartOutlined,
} from '@ant-design/icons';
import { opacApi, readerApi } from '@/api/opac';
import { Availability, Cover, ResultShelf } from '@/components/ResultList';
import { useAuthStore } from '@/stores/authStore';
import { useCartStore } from '@/stores/cartStore';
import { useSiteSettings } from '@/hooks/useSite';
import type { BibDetail, BibItem } from '@/types/api';
import { formatDate } from '@/lib/datetime';
import { MarcRecordTable } from '../components/MarcRecordTable';

const { Paragraph, Title } = Typography;

const CITATION_STYLES = [
  { value: 'Apa', label: 'APA' },
  { value: 'Mla', label: 'MLA' },
  { value: 'Chicago', label: 'Chicago' },
  { value: 'BibTex', label: 'BibTeX' },
  { value: 'Ris', label: 'RIS' },
  { value: 'EndNote', label: 'EndNote' },
];

/** IX.2 — Chi tiết tài liệu: mô tả thư mục, bản in trong kho, tài liệu số, nhận xét, trích dẫn. */
export function BibDetailPage() {
  const { id = '' } = useParams();
  const { message } = App.useApp();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const user = useAuthStore((state) => state.user);
  const { data: settings } = useSiteSettings();
  const addToCart = useCartStore((state) => state.add);

  const [citationStyle, setCitationStyle] = useState('Apa');
  const [citationOpen, setCitationOpen] = useState(false);
  const [reviewOpen, setReviewOpen] = useState(false);
  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState('');

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

  const hold = useMutation({
    mutationFn: () => readerApi.createHold({ bibId: id }),
    onSuccess: (result) => {
      message.success(
        result.queuePosition <= 1
          ? 'Đã đặt giữ, bạn đang đứng đầu hàng đợi.'
          : `Đã đặt giữ, bạn đứng thứ ${result.queuePosition} trong hàng đợi.`,
      );
      void queryClient.invalidateQueries({ queryKey: ['bib', id] });
    },
    onError: (error: Error) => message.error(error.message),
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
      <div className="lc-container" style={{ padding: 24 }}>
        <Skeleton active paragraph={{ rows: 10 }} />
      </div>
    );
  }

  if (!data) {
    return (
      <div className="lc-container" style={{ padding: 48 }}>
        <Empty description="Không tìm thấy tài liệu." />
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

  // Cột tình trạng đứng đầu vì đó là điều bạn đọc cần biết trước tiên: mượn được hay chưa. Ký hiệu
  // xếp giá đứng ngay sau để họ cầm đi tìm sách trên giá. Số ĐKCB và mã vạch xếp cuối — chỉ cán bộ
  // và máy quét dùng tới.
  const itemColumns = [
    {
      title: 'Tình trạng',
      dataIndex: 'statusLabel',
      width: 190,
      render: (_: unknown, item: BibItem) => (
        <Space direction="vertical" size={0}>
          <Tag color={item.isAvailable ? 'green' : 'orange'}>{item.statusLabel}</Tag>
          {item.dueDate ? (
            <span style={{ fontSize: 12, color: 'var(--lc-muted)' }}>
              Dự kiến trả {formatDate(item.dueDate)}
            </span>
          ) : null}
        </Space>
      ),
    },
    { title: 'Ký hiệu xếp giá', dataIndex: 'callNumber', width: 150 },
    { title: 'Kho', dataIndex: 'warehouseName', width: 130 },
    { title: 'Giá', dataIndex: 'shelfName', width: 100 },
    { title: 'Thư viện', dataIndex: 'libraryName', width: 190 },
    { title: 'Số ĐKCB', dataIndex: 'registerNumber', width: 150 },
    { title: 'Mã vạch', dataIndex: 'barcode', width: 130 },
  ];

  return (
    <div className="lc-container" style={{ padding: '24px 16px 48px' }}>
      <Row gutter={24}>
        <Col xs={24} md={6}>
          <Card>
            <Cover
              item={{
                id: data.id,
                controlNumber: data.controlNumber,
                title: data.title,
                coverImageUrl: data.coverImageUrl,
                documentTypeName: data.documentTypeName,
                itemCount: data.itemCount,
                availableItemCount: data.availableItemCount,
                digitalDocumentCount: data.digitalDocuments.length,
                loanCount: 0,
              }}
            />

            <Space direction="vertical" style={{ width: '100%', marginTop: 16 }}>
              <Availability
                item={{
                  id: data.id,
                  controlNumber: data.controlNumber,
                  title: data.title,
                  itemCount: data.itemCount,
                  availableItemCount: data.availableItemCount,
                  digitalDocumentCount: data.digitalDocuments.length,
                  loanCount: 0,
                }}
              />

              {settings?.allowHold !== false ? (
                <Button
                  type="primary"
                  block
                  icon={<BookOutlined />}
                  loading={hold.isPending}
                  onClick={() => requireLogin(() => hold.mutate())}
                >
                  Đặt giữ chỗ
                </Button>
              ) : null}

              <Button
                block
                icon={<HeartOutlined />}
                onClick={() => requireLogin(() => favorite.mutate())}
              >
                Yêu thích
              </Button>

              <Button
                block
                icon={<ShoppingCartOutlined />}
                onClick={() => {
                  addToCart({
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
                  });
                  message.success('Đã thêm vào giỏ tài liệu.');
                }}
              >
                Thêm vào giỏ
              </Button>

              <Button block icon={<CopyOutlined />} onClick={() => setCitationOpen(true)}>
                Xuất trích dẫn
              </Button>
            </Space>
          </Card>
        </Col>

        <Col xs={24} md={18}>
          <Card>
            <Title level={3} style={{ marginTop: 0 }}>
              {data.title}
              {data.subtitle ? <span style={{ fontWeight: 400 }}>: {data.subtitle}</span> : null}
            </Title>

            {data.statementOfResponsibility ? (
              <Paragraph type="secondary">{data.statementOfResponsibility}</Paragraph>
            ) : null}

            <Space size={[8, 8]} wrap style={{ marginBottom: 12 }}>
              {data.authors.map((author) => (
                <Link key={author.id ?? author.name} to={`/tra-cuu?authorId=${author.id ?? ''}`}>
                  <Tag color="green">
                    {author.name}
                    {author.note ? ` (${author.note})` : ''}
                  </Tag>
                </Link>
              ))}
            </Space>

            <Tabs
              items={[
                {
                  key: 'info',
                  label: 'Thông tin thư mục',
                  children: (
                    <>
                      <Descriptions column={{ xs: 1, sm: 2 }} size="small" bordered>
                        {data.publisherName ? (
                          <Descriptions.Item label="Nhà xuất bản">
                            {[data.publishPlace, data.publisherName, data.publishYear]
                              .filter(Boolean)
                              .join(', ')}
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
                        {data.isbn ? (
                          <Descriptions.Item label="ISBN">{data.isbn}</Descriptions.Item>
                        ) : null}
                        {data.issn ? (
                          <Descriptions.Item label="ISSN">{data.issn}</Descriptions.Item>
                        ) : null}
                        {data.ddc ? (
                          <Descriptions.Item label="Phân loại">{data.ddc}</Descriptions.Item>
                        ) : null}
                        {data.languageName ? (
                          <Descriptions.Item label="Ngôn ngữ">{data.languageName}</Descriptions.Item>
                        ) : null}
                        {data.documentTypeName ? (
                          <Descriptions.Item label="Dạng tài liệu">
                            {data.documentTypeName}
                          </Descriptions.Item>
                        ) : null}
                        {data.seriesName ? (
                          <Descriptions.Item label="Tùng thư">{data.seriesName}</Descriptions.Item>
                        ) : null}
                        <Descriptions.Item label="Số kiểm soát">
                          {data.controlNumber}
                        </Descriptions.Item>
                      </Descriptions>

                      {data.subjects.length > 0 ? (
                        <>
                          <Divider orientation="left" plain>
                            Chủ đề
                          </Divider>
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

                      {data.abstract ? (
                        <>
                          <Divider orientation="left" plain>
                            Tóm tắt
                          </Divider>
                          <Paragraph>{data.abstract}</Paragraph>
                        </>
                      ) : null}

                      <Divider orientation="left" plain>
                        Mô tả theo ISBD
                      </Divider>
                      <Paragraph type="secondary">{data.isbd}</Paragraph>
                    </>
                  ),
                },
                {
                  key: 'items',
                  label: `Bản in trong kho (${data.items.length})`,
                  children: (
                    <Table
                      rowKey="id"
                      size="small"
                      columns={itemColumns}
                      dataSource={data.items}
                      pagination={false}
                      scroll={{ x: 1040 }}
                      locale={{ emptyText: 'Tài liệu này chưa có bản in trong kho.' }}
                    />
                  ),
                },
                {
                  key: 'digital',
                  label: `Tài liệu số (${data.digitalDocuments.length})`,
                  children:
                    data.digitalDocuments.length === 0 ? (
                      <Empty description="Tài liệu này chưa có bản số." />
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
                                  {document.pageCount ? (
                                    <span>{document.pageCount} trang</span>
                                  ) : null}
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
          </Card>

          {data.related.length > 0 ? (
            <Card title="Tài liệu liên quan" style={{ marginTop: 24 }}>
              <ResultShelf items={data.related} />
            </Card>
          ) : null}
        </Col>
      </Row>

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
