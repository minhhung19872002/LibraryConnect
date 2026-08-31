import { Link } from 'react-router-dom';
import { App, Button, Card, Empty, List, Space, Tag, Tooltip } from 'antd';
import { FileTextOutlined, ShoppingCartOutlined } from '@ant-design/icons';
import { useCartStore } from '@/stores/cartStore';
import type { SearchResult } from '@/types/api';

/** Ảnh bìa, hoặc một ô chữ thay thế khi biểu ghi chưa có ảnh. */
export function Cover({ item }: { item: SearchResult }) {
  if (item.coverImageUrl) {
    return <img className="lc-cover" src={item.coverImageUrl} alt={item.title} loading="lazy" />;
  }

  return (
    <div className="lc-cover lc-cover--placeholder">
      {item.documentTypeName ?? 'Chưa có ảnh bìa'}
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
}: {
  items: SearchResult[];
  loading?: boolean;
  emptyText?: string;
}) {
  return (
    <List
      loading={loading}
      dataSource={items}
      locale={{ emptyText: <Empty description={emptyText} /> }}
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
