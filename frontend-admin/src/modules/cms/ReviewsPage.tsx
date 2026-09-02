import { useState } from 'react';
import { App, Button, Card, Popconfirm, Radio, Rate, Space, Table, Tag } from 'antd';
import { CheckOutlined, DeleteOutlined, StopOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import { PageHeader } from '@/components/PageHeader';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { cmsApi } from './api';
import type { CmsReviewRow } from './types';
import { MAU } from '@/lib/palette';

/**
 * Kiểm duyệt nhận xét bạn đọc gửi từ trang tra cứu.
 *
 * Nhận xét chỉ hiện công khai sau khi cán bộ duyệt; bạn đọc sửa lại nhận xét đã duyệt thì nó quay
 * về hàng chờ, nên màn hình này mặc định mở ở nhóm chờ duyệt.
 */
export function CmsReviewsPage() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [approved, setApproved] = useState<boolean | undefined>(false);
  const [page, setPage] = useState(1);

  const reviews = useQuery({
    queryKey: ['cms-reviews', approved, page],
    queryFn: () => cmsApi.reviews({ isApproved: approved, page, pageSize: 20 }),
  });

  const moderate = useMutation({
    mutationFn: ({ id, approve }: { id: string; approve: boolean }) =>
      cmsApi.moderateReview(id, approve),
    onSuccess: (_result, variables) => {
      message.success(variables.approve ? 'Đã duyệt nhận xét.' : 'Đã bỏ duyệt nhận xét.');
      void queryClient.invalidateQueries({ queryKey: ['cms-reviews'] });
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không xử lý được.'),
  });

  const remove = useMutation({
    mutationFn: (id: string) => cmsApi.deleteReview(id),
    onSuccess: () => {
      message.success('Đã xóa nhận xét.');
      void queryClient.invalidateQueries({ queryKey: ['cms-reviews'] });
    },
  });

  const columns: ColumnsType<CmsReviewRow> = [
    { title: 'Tài liệu', dataIndex: 'bibTitle', width: 300 },
    {
      title: 'Bạn đọc',
      dataIndex: 'readerName',
      width: 220,
      render: (name: string, row) => (
        <Space direction="vertical" size={0}>
          <span>{name}</span>
          <span style={{ fontSize: 12, color: MAU.chuMo }}>{row.readerCardNumber}</span>
        </Space>
      ),
    },
    {
      title: 'Đánh giá',
      dataIndex: 'rating',
      width: 140,
      render: (rating: number) => <Rate disabled value={rating} style={{ fontSize: 14 }} />,
    },
    { title: 'Nội dung', dataIndex: 'comment', width: 380 },
    {
      title: 'Gửi lúc',
      dataIndex: 'createdAt',
      width: 150,
      render: (value: string) => dayjs(value).format('DD/MM/YYYY HH:mm'),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'isApproved',
      width: 130,
      render: (value: boolean) =>
        value ? <Tag color="green">Đã duyệt</Tag> : <Tag color="orange">Chờ duyệt</Tag>,
    },
    {
      title: '',
      dataIndex: 'id',
      width: 160,
      render: (id: string, row) => (
        <Space>
          <Can permission={PERMISSIONS.cms.reviewModerate}>
            <Button
              size="small"
              type={row.isApproved ? 'default' : 'primary'}
              icon={row.isApproved ? <StopOutlined /> : <CheckOutlined />}
              onClick={() => moderate.mutate({ id, approve: !row.isApproved })}
            >
              {row.isApproved ? 'Bỏ duyệt' : 'Duyệt'}
            </Button>
          </Can>
          <Can permission={PERMISSIONS.cms.reviewModerate}>
            <Popconfirm
              title="Xóa hẳn nhận xét này?"
              okText="Xóa"
              cancelText="Không"
              onConfirm={() => remove.mutate(id)}
            >
              <Button size="small" danger icon={<DeleteOutlined />} />
            </Popconfirm>
          </Can>
        </Space>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Nhận xét bạn đọc"
        description="Nhận xét gửi từ trang tra cứu, chỉ hiển thị công khai sau khi được duyệt."
      />

      <Card>
        <Radio.Group
          value={approved}
          onChange={(event) => {
            setApproved(event.target.value);
            setPage(1);
          }}
          style={{ marginBottom: 16 }}
          options={[
            { value: false, label: 'Chờ duyệt' },
            { value: true, label: 'Đã duyệt' },
            { value: undefined, label: 'Tất cả' },
          ]}
          optionType="button"
        />

        <Table
          rowKey="id"
          size="small"
          loading={reviews.isLoading}
          columns={columns}
          dataSource={reviews.data?.items ?? []}
          scroll={{ x: 1480 }}
          pagination={{
            current: reviews.data?.page ?? 1,
            pageSize: reviews.data?.pageSize ?? 20,
            total: reviews.data?.totalCount ?? 0,
            showSizeChanger: false,
            onChange: setPage,
          }}
          locale={{ emptyText: 'Không có nhận xét nào trong nhóm này.' }}
        />
      </Card>
    </>
  );
}
