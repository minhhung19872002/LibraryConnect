import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Badge, Button, Dropdown, Empty, List, Spin, Typography } from 'antd';
import { BellOutlined } from '@ant-design/icons';
import { api } from '@/api/client';
import { formatDateTime } from '@/lib/datetime';
import type { PagedResult } from '@/types/api';

/** Một dòng thông báo gửi cho cán bộ đang đăng nhập. */
export interface StaffNotification {
  id: string;
  type: string;
  title: string;
  body?: string | null;
  link?: string | null;
  isRead: boolean;
  createdAt: string;
}

interface StaffNotificationPage {
  items: PagedResult<StaffNotification>;
  unreadCount: number;
}

/**
 * Chuông thông báo trên thanh trên của giao diện quản trị.
 *
 * Hỏi lại mỗi phút chứ không mở kết nối đẩy: một yêu cầu chờ duyệt không gấp tới mức phải biết
 * trong một giây, mà giữ kết nối mở cho vài trăm cán bộ thì tốn hơn nhiều so với thứ nó đổi lại.
 */
export function NotificationBell() {
  const [open, setOpen] = useState(false);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const notifications = useQuery({
    queryKey: ['staff-notifications'],
    queryFn: () => api.get<StaffNotificationPage>('/notifications', { params: { pageSize: 10 } }),
    refetchInterval: 60_000,
  });

  const markRead = useMutation({
    mutationFn: (id?: string) => api.post(id ? `/notifications/${id}/read` : '/notifications/read-all'),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['staff-notifications'] }),
  });

  const rows = notifications.data?.items.items ?? [];
  const unread = notifications.data?.unreadCount ?? 0;

  const panel = (
    <div className="lc-notification-panel">
      <div className="lc-notification-head">
        <Typography.Text strong>Thông báo</Typography.Text>
        <Button
          type="link"
          size="small"
          disabled={unread === 0 || markRead.isPending}
          onClick={() => markRead.mutate(undefined)}
        >
          Đánh dấu đã đọc tất cả
        </Button>
      </div>

      {notifications.isLoading ? (
        <div className="lc-notification-empty">
          <Spin size="small" />
        </div>
      ) : rows.length === 0 ? (
        <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="Chưa có thông báo nào" />
      ) : (
        <List
          size="small"
          dataSource={rows}
          renderItem={(row) => (
            <List.Item
              className={row.isRead ? 'lc-notification-item' : 'lc-notification-item lc-notification-item--moi'}
              onClick={() => {
                if (!row.isRead) markRead.mutate(row.id);
                setOpen(false);
                if (row.link) navigate(row.link);
              }}
            >
              <List.Item.Meta
                title={<span>{row.title}</span>}
                description={
                  <>
                    {row.body && <div>{row.body}</div>}
                    <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                      {formatDateTime(row.createdAt)}
                    </Typography.Text>
                  </>
                }
              />
            </List.Item>
          )}
        />
      )}
    </div>
  );

  return (
    <Dropdown
      open={open}
      onOpenChange={setOpen}
      trigger={['click']}
      placement="bottomRight"
      dropdownRender={() => panel}
    >
      <button type="button" className="lc-notification-button" aria-label="Thông báo">
        <Badge count={unread} size="small" offset={[-2, 2]}>
          <BellOutlined />
        </Badge>
      </button>
    </Dropdown>
  );
}
