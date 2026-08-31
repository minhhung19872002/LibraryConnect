import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { App, Button, Card, Empty, Input, List, Popconfirm, Space, Tag } from 'antd';
import { DeleteOutlined, MailOutlined } from '@ant-design/icons';
import { readerApi } from '@/api/opac';
import { useCartStore } from '@/stores/cartStore';
import { useAuthStore } from '@/stores/authStore';

/**
 * IX.2 — Giỏ tài liệu.
 *
 * Gom sách trong lúc tra cứu rồi gửi cả danh sách về email. Giỏ nằm ở máy người dùng nên chưa đăng
 * nhập vẫn gom được; chỉ khi bấm gửi mới cần tài khoản, vì thư đi tới đúng địa chỉ trong hồ sơ.
 */
export function CartPage() {
  const items = useCartStore((state) => state.items);
  const remove = useCartStore((state) => state.remove);
  const clear = useCartStore((state) => state.clear);
  const user = useAuthStore((state) => state.user);
  const navigate = useNavigate();
  const { message } = App.useApp();

  const [note, setNote] = useState('');
  const [sending, setSending] = useState(false);

  const send = async () => {
    if (!user) {
      message.info('Bạn cần đăng nhập để gửi danh sách về email của mình.');
      navigate('/dang-nhap');
      return;
    }

    setSending(true);

    try {
      const email = await readerApi.emailCart(
        items.map((item) => item.id),
        note || undefined,
      );
      message.success(`Đã gửi danh sách tới ${email}.`);
    } catch (error) {
      message.error((error as Error).message);
    } finally {
      setSending(false);
    }
  };

  return (
    <div className="lc-container" style={{ padding: '24px 16px 48px' }}>
      <Card
        title={`Giỏ tài liệu (${items.length})`}
        extra={
          items.length > 0 ? (
            <Popconfirm
              title="Xóa toàn bộ giỏ tài liệu?"
              okText="Xóa hết"
              cancelText="Không"
              onConfirm={clear}
            >
              <Button danger size="small">
                Xóa hết
              </Button>
            </Popconfirm>
          ) : null
        }
      >
        {items.length === 0 ? (
          <Empty description="Giỏ tài liệu đang trống. Thêm sách từ trang kết quả tra cứu.">
            <Link to="/tra-cuu">
              <Button type="primary">Tra cứu tài liệu</Button>
            </Link>
          </Empty>
        ) : (
          <>
            <List
              dataSource={items}
              renderItem={(item) => (
                <List.Item
                  actions={[
                    <Button
                      key="remove"
                      size="small"
                      icon={<DeleteOutlined />}
                      onClick={() => remove(item.id)}
                    />,
                  ]}
                >
                  <List.Item.Meta
                    title={<Link to={`/tai-lieu/${item.id}`}>{item.title}</Link>}
                    description={
                      <Space size={[8, 4]} wrap>
                        <span>
                          {[item.authorMain, item.publisherName, item.publishYear]
                            .filter(Boolean)
                            .join(' • ')}
                        </span>
                        {item.availableItemCount > 0 ? (
                          <Tag color="green">Còn {item.availableItemCount} bản</Tag>
                        ) : (
                          <Tag color="orange">Hết bản rảnh</Tag>
                        )}
                      </Space>
                    }
                  />
                </List.Item>
              )}
            />

            <Input.TextArea
              value={note}
              onChange={(event) => setNote(event.target.value)}
              placeholder="Lời nhắn kèm theo (không bắt buộc)"
              rows={2}
              style={{ marginTop: 16 }}
            />

            <Button
              type="primary"
              icon={<MailOutlined />}
              style={{ marginTop: 12 }}
              loading={sending}
              onClick={send}
            >
              Gửi danh sách về email của tôi
            </Button>
          </>
        )}
      </Card>
    </div>
  );
}
