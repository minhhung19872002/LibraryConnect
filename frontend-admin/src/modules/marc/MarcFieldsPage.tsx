import { useMemo, useState } from 'react';
import { App, Button, Input, Popconfirm, Space, Switch, Table, Tag, Tooltip, Typography } from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PageHeader } from '@/components/PageHeader';
import { FilterBar } from '@/components/FilterBar';
import { Can } from '@/components/PermissionGate';
import { PERMISSIONS } from '@/api/permissions';
import { ApiRequestError } from '@/api/client';
import { marcApi } from './api';
import type { MarcFieldDefinition } from './types';
import { MarcFieldFormDrawer } from './MarcFieldFormDrawer';

const MONOSPACE = { fontFamily: 'ui-monospace, Consolas, monospace' } as const;

/**
 * Bộ định nghĩa trường MARC 21 (II.5).
 *
 * The standard set ships with the system, so this screen is mostly for reading — a cataloguer looks
 * up what 773$g means without leaving the application. It is editable because libraries do declare
 * local fields and do adjust which fields their own workflow treats as mandatory.
 */
export function MarcFieldsPage() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [keyword, setKeyword] = useState('');
  const [appliedKeyword, setAppliedKeyword] = useState('');
  const [includeInactive, setIncludeInactive] = useState(false);
  const [editing, setEditing] = useState<MarcFieldDefinition | null>(null);
  const [drawerOpen, setDrawerOpen] = useState(false);

  const { data, isFetching } = useQuery({
    queryKey: ['marc-fields', appliedKeyword, includeInactive],
    queryFn: () => marcApi.getFields({ keyword: appliedKeyword || undefined, includeInactive }),
  });

  const importStandard = useMutation({
    mutationFn: (overwrite: boolean) => marcApi.importStandardFields(overwrite),
    onSuccess: async (result) => {
      message.success(
        result.updated > 0
          ? `Đã khôi phục bộ chuẩn: thêm ${result.added}, ghi đè ${result.updated}; giữ nguyên ${result.custom} trường riêng của thư viện.`
          : `Đã nạp thêm ${result.added} trường còn thiếu; ${result.unchanged} trường giữ nguyên.`,
      );
      await queryClient.invalidateQueries({ queryKey: ['marc-fields'] });
    },
    onError: (error: unknown) => {
      message.error(
        error instanceof ApiRequestError ? error.message : 'Không nạp được bộ định nghĩa chuẩn.',
      );
    },
  });

  const remove = useMutation({
    mutationFn: (id: string) => marcApi.deleteField(id),
    onSuccess: async () => {
      message.success('Đã xóa định nghĩa trường.');
      await queryClient.invalidateQueries({ queryKey: ['marc-fields'] });
    },
    onError: (error: unknown) => {
      message.error(error instanceof ApiRequestError ? error.message : 'Không xóa được định nghĩa trường.');
    },
  });

  // Giữ tham chiếu ổn định khi truy vấn chưa trả về: một mảng rỗng dựng mới mỗi lần vẽ sẽ khiến
  // mọi phép ghi nhớ bên dưới chạy lại vô ích.
  const fields = useMemo(() => data ?? [], [data]);

  const counts = useMemo(
    () => ({
      total: fields.length,
      control: fields.filter((field) => field.isControl).length,
      required: fields.filter((field) => field.isRequired).length,
    }),
    [fields],
  );

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <PageHeader
        title="Định nghĩa trường MARC 21"
        description={`Bộ định nghĩa đang dùng để gợi ý và kiểm tra biểu ghi: ${counts.total} trường, trong đó ${counts.control} trường điều khiển và ${counts.required} trường bắt buộc.`}
        actions={
          <Can permission={PERMISSIONS.cataloging.marcDefinition}>
            <Space>
              {/*
                II.5: nạp bộ định nghĩa MARC 21 chuẩn. Hai nút vì hai việc khác hẳn nhau — nạp bổ
                sung là an toàn và chạy được bất cứ lúc nào, còn khôi phục thì ghi đè lên cả những
                trường thư viện đã sửa, nên phải hỏi lại.
              */}
              <Button loading={importStandard.isPending} onClick={() => importStandard.mutate(false)}>
                Nạp trường còn thiếu
              </Button>
              <Popconfirm
                title="Khôi phục bộ định nghĩa chuẩn?"
                description="Mọi sửa đổi của thư viện trên các trường chuẩn sẽ bị ghi đè. Trường do thư viện tự thêm được giữ nguyên."
                okText="Khôi phục"
                cancelText="Hủy"
                onConfirm={() => importStandard.mutate(true)}
              >
                <Button danger loading={importStandard.isPending}>
                  Khôi phục bộ chuẩn
                </Button>
              </Popconfirm>
              <Button
                type="primary"
                icon={<PlusOutlined />}
                onClick={() => {
                  setEditing(null);
                  setDrawerOpen(true);
                }}
              >
                Thêm trường
              </Button>
            </Space>
          </Can>
        }
      />

      <FilterBar
        loading={isFetching}
        onSearch={() => setAppliedKeyword(keyword.trim())}
        onReset={() => {
          setKeyword('');
          setAppliedKeyword('');
          setIncludeInactive(false);
        }}
        extra={
          <Space>
            <Typography.Text type="secondary">Hiện cả trường đã tắt</Typography.Text>
            <Switch checked={includeInactive} onChange={setIncludeInactive} />
          </Space>
        }
      >
        <Input
          value={keyword}
          onChange={(event) => setKeyword(event.target.value)}
          placeholder="Tìm theo nhãn trường hoặc tên trường"
          allowClear
          style={{ width: 360 }}
        />
      </FilterBar>

      <Table<MarcFieldDefinition>
        rowKey="id"
        dataSource={fields}
        loading={isFetching}
        size="small"
        pagination={{ pageSize: 50, showSizeChanger: true, pageSizeOptions: [20, 50, 100, 250] }}
        expandable={{
          expandedRowRender: (field) => <FieldDetail field={field} />,
          rowExpandable: (field) => field.indicators.length > 0 || field.subfields.length > 0,
        }}
        columns={[
          {
            title: 'Nhãn',
            dataIndex: 'tag',
            width: 80,
            render: (tag: string) => <Tag style={MONOSPACE}>{tag}</Tag>,
          },
          {
            title: 'Tên trường',
            dataIndex: 'name',
            render: (name: string, field) => (
              <Space direction="vertical" size={0}>
                <Typography.Text>{name}</Typography.Text>
                {field.nameEn && (
                  <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                    {field.nameEn}
                  </Typography.Text>
                )}
              </Space>
            ),
          },
          {
            title: 'Tính chất',
            width: 260,
            render: (_, field) => (
              <Space size={4} wrap>
                {field.isControl && <Tag color="purple">Trường điều khiển</Tag>}
                {field.isRepeatable && <Tag>Lặp lại</Tag>}
                {field.isRequired && <Tag color="red">Bắt buộc</Tag>}
                {field.isRecommended && <Tag color="gold">Nên có</Tag>}
                {!field.isActive && <Tag color="default">Đã tắt</Tag>}
              </Space>
            ),
          },
          {
            title: 'Trường con',
            width: 90,
            align: 'right',
            render: (_, field) => field.subfields.length || '—',
          },
          {
            title: '',
            width: 90,
            align: 'right',
            render: (_, field) => (
              <Space size={0}>
                <Can permission={PERMISSIONS.cataloging.marcDefinition}>
                  <Tooltip title="Sửa">
                    <Button
                      type="text"
                      icon={<EditOutlined />}
                      onClick={() => {
                        setEditing(field);
                        setDrawerOpen(true);
                      }}
                    />
                  </Tooltip>
                </Can>
                <Can permission={PERMISSIONS.cataloging.marcDefinition}>
                  <Popconfirm
                    title={`Xóa định nghĩa trường ${field.tag}?`}
                    description="Biểu ghi đang dùng trường này vẫn giữ nguyên dữ liệu, nhưng sẽ không còn được gợi ý và kiểm tra."
                    okText="Xóa"
                    cancelText="Không"
                    onConfirm={() => remove.mutate(field.id)}
                  >
                    <Button type="text" danger icon={<DeleteOutlined />} />
                  </Popconfirm>
                </Can>
              </Space>
            ),
          },
        ]}
      />

      <MarcFieldFormDrawer
        open={drawerOpen}
        field={editing}
        onClose={() => setDrawerOpen(false)}
        onSaved={async () => {
          setDrawerOpen(false);
          await queryClient.invalidateQueries({ queryKey: ['marc-fields'] });
        }}
      />
    </Space>
  );
}

function FieldDetail({ field }: { field: MarcFieldDefinition }) {
  return (
    <Space direction="vertical" size={12} style={{ width: '100%' }}>
      {field.description && <Typography.Text type="secondary">{field.description}</Typography.Text>}

      {field.indicators.map((indicator) => (
        <div key={indicator.position}>
          <Typography.Text strong>
            Chỉ thị {indicator.position} — {indicator.name}
          </Typography.Text>
          <ul style={{ margin: '4px 0 0 20px' }}>
            {indicator.values.map((value) => (
              <li key={value.code}>
                <Typography.Text style={MONOSPACE}>{value.code}</Typography.Text> — {value.label}
              </li>
            ))}
          </ul>
        </div>
      ))}

      {field.subfields.length > 0 && (
        <div>
          <Typography.Text strong>Trường con</Typography.Text>
          <ul style={{ margin: '4px 0 0 20px' }}>
            {field.subfields.map((subfield) => (
              <li key={subfield.code}>
                <Typography.Text style={MONOSPACE}>${subfield.code}</Typography.Text> — {subfield.name}
                {subfield.repeatable && <Tag style={{ marginLeft: 8 }}>lặp lại</Tag>}
                {subfield.required && (
                  <Tag color="red" style={{ marginLeft: 4 }}>
                    bắt buộc
                  </Tag>
                )}
              </li>
            ))}
          </ul>
        </div>
      )}
    </Space>
  );
}
