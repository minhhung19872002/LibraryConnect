import { useState } from 'react';
import { Alert, App, Button, Card, Drawer, Empty, Radio, Space, Spin, Tag, Typography } from 'antd';
import { MergeCellsOutlined } from '@ant-design/icons';
import { useMutation, useQuery } from '@tanstack/react-query';
import { api } from '@/api/client';
import { errorMessage } from '@/api/formErrors';
import { messages } from '@/i18n/messages';
import type { CatalogMergeResult, CatalogMetadata, DuplicateGroup } from './types';

/**
 * Gộp các giá trị trùng của một danh mục (II.9).
 *
 * The same author or publisher regularly gets entered several times with different spellings. The
 * screen groups the suspects by their accent-stripped name and shows how many records each one is
 * used by, because that usage count is what decides which spelling to keep.
 */
export function CatalogMergeDrawer({
  catalog,
  metadata,
  onClose,
  onMerged,
}: {
  catalog: string;
  metadata: CatalogMetadata;
  onClose: () => void;
  onMerged: () => void | Promise<void>;
}) {
  const { message } = App.useApp();
  const [targets, setTargets] = useState<Record<string, string>>({});

  const duplicates = useQuery({
    queryKey: ['catalog-duplicates', catalog],
    queryFn: () => api.get<DuplicateGroup[]>(`/catalogs/${catalog}/duplicates`),
  });

  const mutation = useMutation({
    mutationFn: ({ targetId, sourceIds }: { targetId: string; sourceIds: string[] }) =>
      api.post<CatalogMergeResult>(`/catalogs/${catalog}/merge`, { targetId, sourceIds }),
    onSuccess: async (result) => {
      message.success(
        `Đã gộp ${result.mergedCount} giá trị vào "${result.targetName}" và chuyển ${result.updatedReferences} tham chiếu.`,
      );
      await duplicates.refetch();
      await onMerged();
    },
    onError: (error: unknown) => message.error(errorMessage(error)),
  });

  const groups = duplicates.data ?? [];

  return (
    <Drawer title={`Gộp trùng: ${metadata.pluralName}`} open width={720} onClose={onClose}>
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <Alert
          type="info"
          showIcon
          message="So sánh theo tên đã bỏ dấu và bỏ phân biệt hoa thường"
          description="Chọn giá trị muốn giữ lại trong mỗi nhóm. Toàn bộ biểu ghi đang tham chiếu tới các giá trị còn lại sẽ được chuyển sang giá trị giữ lại, sau đó các giá trị trùng bị xóa."
        />

        <Spin spinning={duplicates.isLoading}>
          {groups.length === 0 && !duplicates.isLoading ? (
            <Empty description={`Không tìm thấy giá trị trùng trong danh mục ${metadata.pluralName}.`} />
          ) : (
            groups.map((group) => {
              // The most-used spelling is the sensible default to keep.
              const suggested = [...group.items].sort(
                (a, b) => (b.usageCount ?? 0) - (a.usageCount ?? 0),
              )[0];
              const targetId = targets[group.normalisedName] ?? suggested?.id;

              return (
                <Card
                  key={group.normalisedName}
                  size="small"
                  className="lc-page-card"
                  title={
                    <Space>
                      <Typography.Text strong>{group.normalisedName}</Typography.Text>
                      <Tag>{group.items.length} giá trị</Tag>
                    </Space>
                  }
                  extra={
                    <Button
                      type="primary"
                      size="small"
                      icon={<MergeCellsOutlined />}
                      loading={mutation.isPending && mutation.variables?.targetId === targetId}
                      disabled={!targetId}
                      onClick={() =>
                        mutation.mutate({
                          targetId: targetId!,
                          sourceIds: group.items.filter((item) => item.id !== targetId).map((item) => item.id),
                        })
                      }
                    >
                      Gộp nhóm này
                    </Button>
                  }
                >
                  <Radio.Group
                    value={targetId}
                    onChange={(event) =>
                      setTargets((current) => ({ ...current, [group.normalisedName]: event.target.value }))
                    }
                    style={{ width: '100%' }}
                  >
                    <Space direction="vertical" style={{ width: '100%' }}>
                      {group.items.map((item) => (
                        <Radio key={item.id} value={item.id}>
                          <Space>
                            <Typography.Text>{item.name}</Typography.Text>
                            {metadata.showCode && (
                              <Typography.Text type="secondary" className="lc-mono lc-small">
                                {item.code}
                              </Typography.Text>
                            )}
                            <Tag color={(item.usageCount ?? 0) > 0 ? 'blue' : 'default'}>
                              {item.usageCount ?? 0} bản ghi đang dùng
                            </Tag>
                            {!item.isActive && <Tag>Ngưng dùng</Tag>}
                          </Space>
                        </Radio>
                      ))}
                    </Space>
                  </Radio.Group>

                  <Typography.Paragraph type="secondary" className="lc-small lc-scope-note">
                    Giá trị được chọn sẽ giữ lại; {group.items.length - 1} giá trị còn lại bị gộp vào.
                  </Typography.Paragraph>
                </Card>
              );
            })
          )}
        </Spin>

        <Button onClick={onClose}>{messages.actions.close}</Button>
      </Space>
    </Drawer>
  );
}
