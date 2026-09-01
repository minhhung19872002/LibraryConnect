import { Link } from 'react-router-dom';
import { App, Button, Card, Empty, List, Space, Tag, Tooltip } from 'antd';
import { FileTextOutlined, ShoppingCartOutlined } from '@ant-design/icons';
import { useCartStore } from '@/stores/cartStore';
import type { SearchResult } from '@/types/api';
import { coverPlaceholder } from '@/lib/cover';

/**
 * Ảnh bìa của tài liệu.
 *
 * Phần lớn biểu ghi của một thư viện Việt Nam không có ảnh bìa — sách cũ, luận văn, đề tài nghiên
 * cứu đều không có ảnh trên mạng. Thay vì để một ô xám trống, dựng bìa mang nhan đề và tác giả:
 * trang kết quả đọc được, và bạn đọc nhớ mặt được cuốn mình vừa xem khi quay lại danh sách.
 */
export function Cover({ item }: { item: SearchResult }) {
  if (item.coverImageUrl) {
    return <img className="lc-cover" src={item.coverImageUrl} alt={item.title} loading="lazy" />;
  }

  const bia = coverPlaceholder(item);

  return (
    <div
      className="lc-cover lc-cover--generated"
      style={{ background: bia.background }}
      role="img"
      aria-label={`Bìa thay thế của tài liệu ${bia.title}`}
    >
      <div className="lc-cover__title">{bia.title}</div>
      {bia.author && <div className="lc-cover__author">{bia.author}</div>}
      <div className="lc-cover__label">{bia.label}</div>
    </div>
  );
}

/**
 * Dòng chữ cho biết tài liệu còn mượn được hay không — thứ bạn đọc nhìn trước tiên.
 *
 * Kho hết bản rảnh vì nhiều lý do khác nhau: người khác đang mượn, bản mới nhập chưa kiểm nhận,
 * bản đang sửa chữa. Ở danh sách kết quả thì chỉ nói đúng điều chắc chắn là "chưa có bản sẵn sàng";
 * lý do của từng bản nằm ở trang chi tiết, nơi có đủ thông tin để nói chính xác.
 */
export function Availability({ item }: { item: SearchResult }) {
  if (item.availableItemCount > 0) {
    return <Tag color="green">Còn {item.availableItemCount} bản sẵn sàng</Tag>;
  }

  if (item.itemCount > 0) {
    return <Tag color="orange">Chưa có bản sẵn sàng</Tag>;
  }

  if (item.digitalDocumentCount > 0) {
    return <Tag color="blue">Chỉ có bản số</Tag>;
  }

  return <Tag>Chưa có bản in trong kho</Tag>;
}

export function ResultRow({ item }: { item: SearchResult }) {
  const { message } = App.useApp();
  const add = useCartStore((state) => state.add);
  const inCart = useCartStore((state) => state.items.some((row) => row.id === item.id));

  return (
    <div className="lc-result">
      <div className="lc-result__cover">
        <Link to={`/tai-lieu/${item.id}`}>
          <Cover item={item} />
        </Link>
      </div>

      <div className="lc-result__body">
        <h3 className="lc-result__title">
          <Link to={`/tai-lieu/${item.id}`}>{item.title}</Link>
        </h3>

        <p className="lc-result__meta">
          {[
            item.authorMain,
            item.publisherName,
            item.publishYear?.toString(),
            item.ddc ? `Phân loại ${item.ddc}` : undefined,
          ]
            .filter(Boolean)
            .join(' • ')}
        </p>

        <Space size={[8, 8]} wrap>
          <Availability item={item} />
          {item.documentTypeName ? <Tag>{item.documentTypeName}</Tag> : null}
          {item.languageName ? <Tag>{item.languageName}</Tag> : null}
          {item.digitalDocumentCount > 0 ? (
            <Tag icon={<FileTextOutlined />} color="blue">
              {item.digitalDocumentCount} tệp số
            </Tag>
          ) : null}

          <Tooltip title={inCart ? 'Tài liệu đã ở trong giỏ' : 'Thêm vào giỏ tài liệu'}>
            <Button
              size="small"
              icon={<ShoppingCartOutlined />}
              disabled={inCart}
              onClick={() => {
                add(item);
                message.success('Đã thêm vào giỏ tài liệu.');
              }}
            >
              {inCart ? 'Đã trong giỏ' : 'Thêm vào giỏ'}
            </Button>
          </Tooltip>
        </Space>
      </div>
    </div>
  );
}

/** Danh sách kết quả dùng chung cho tra cứu, duyệt danh mục và trang cá nhân. */
export function ResultList({
  items,
  loading,
  emptyText = 'Không tìm thấy tài liệu nào phù hợp.',
  showTips = true,
}: {
  items: SearchResult[];
  loading?: boolean;
  emptyText?: string;
  showTips?: boolean;
}) {
  return (
    <List
      loading={loading}
      dataSource={items}
      locale={{
        emptyText: (
          <Empty
            description={
              <div style={{ textAlign: 'left', maxWidth: 460, margin: '0 auto' }}>
                <div style={{ fontWeight: 600, marginBottom: 8, textAlign: 'center' }}>
                  {emptyText}
                </div>
                {showTips && (
                  <ul style={{ paddingLeft: 20, margin: 0, lineHeight: 1.8 }}>
                    <li>Thử bớt từ khóa, giữ lại một hai từ chính.</li>
                    <li>Gõ không dấu vẫn tìm được, nhưng hãy kiểm tra lại chính tả.</li>
                    <li>
                      Dùng <Link to="/tra-cuu-nang-cao">tra cứu nâng cao</Link> để tìm theo tác giả,
                      chủ đề hoặc năm xuất bản.
                    </li>
                    <li>
                      Chưa thấy ở đây thì <Link to="/thu-vien-khac">tìm ở thư viện khác</Link>.
                    </li>
                  </ul>
                )}
              </div>
            }
          />
        ),
      }}
      renderItem={(item) => (
        <List.Item key={item.id} style={{ padding: '16px 0' }}>
          <ResultRow item={item} />
        </List.Item>
      )}
    />
  );
}

/** Kệ sách nhỏ dùng ở trang chủ và mục tài liệu liên quan. */
export function ResultShelf({ items }: { items: SearchResult[] }) {
  if (items.length === 0) {
    return <Empty description="Chưa có tài liệu để hiển thị." />;
  }

  return (
    <div
      style={{
        display: 'grid',
        gap: 16,
        gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))',
      }}
    >
      {items.map((item) => (
        <Card key={item.id} size="small" styles={{ body: { padding: 12 } }}>
          <Link to={`/tai-lieu/${item.id}`}>
            <Cover item={item} />
          </Link>
          <div style={{ marginTop: 8, fontWeight: 600, fontSize: 14, lineHeight: 1.35 }}>
            <Link to={`/tai-lieu/${item.id}`}>{item.title}</Link>
          </div>
          <div style={{ color: 'var(--lc-muted)', fontSize: 12, marginTop: 4 }}>
            {item.authorMain ?? '—'}
          </div>
          <div style={{ marginTop: 6 }}>
            <Availability item={item} />
          </div>
        </Card>
      ))}
    </div>
  );
}
