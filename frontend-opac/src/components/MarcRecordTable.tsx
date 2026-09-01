import { Empty, Table, Tooltip, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { docBieuGhiMarc, type MarcFieldView } from '../lib/marcView';

const MONOSPACE = { fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Consolas, monospace' };

/**
 * Bảng biểu ghi MARC 21 cho trang tra cứu.
 *
 * Bày đúng lối các phần mềm thư viện vẫn bày — Nhãn trường · Chỉ thị 1 · Chỉ thị 2 · Trường con —
 * kèm tên trường bằng tiếng Việt. Cán bộ thư viện đọc quen dạng này, và bạn đọc ít nhất cũng nhận
 * ra dòng nào là nhan đề, dòng nào là tác giả.
 */
export function MarcRecordTable({ marcJson }: { marcJson: string | null | undefined }) {
  const bieuGhi = docBieuGhiMarc(marcJson);

  if (!bieuGhi) {
    return <Empty description="Không đọc được biểu ghi MARC của tài liệu này." />;
  }

  const columns: ColumnsType<MarcFieldView> = [
    {
      title: 'Nhãn',
      dataIndex: 'tag',
      width: 70,
      render: (tag: string, field) => (
        <Tooltip title={field.name}>
          <span style={{ ...MONOSPACE, fontWeight: 600 }}>{tag}</span>
        </Tooltip>
      ),
    },
    {
      title: 'Tên trường',
      dataIndex: 'name',
      width: 240,
      responsive: ['md'],
      render: (name: string) => (
        <Typography.Text type="secondary" style={{ fontSize: 13 }}>
          {name}
        </Typography.Text>
      ),
    },
    {
      title: 'CT1',
      dataIndex: 'ind1',
      width: 52,
      align: 'center',
      render: (value: string) => <span style={MONOSPACE}>{value}</span>,
    },
    {
      title: 'CT2',
      dataIndex: 'ind2',
      width: 52,
      align: 'center',
      render: (value: string) => <span style={MONOSPACE}>{value}</span>,
    },
    {
      title: 'Nội dung',
      render: (_, field) =>
        field.isControl ? (
          <span style={{ ...MONOSPACE, whiteSpace: 'pre-wrap' }}>{field.value}</span>
        ) : (
          <div>
            {field.subfields.map((subfield, index) => (
              <div key={`${subfield.code}-${index}`} style={{ marginBottom: 2 }}>
                <span style={{ ...MONOSPACE, color: '#1668dc', marginRight: 6 }}>
                  ${subfield.code}
                </span>
                <span>{subfield.value}</span>
              </div>
            ))}
          </div>
        ),
    },
  ];

  return (
    <>
      <Typography.Paragraph type="secondary" style={{ fontSize: 13 }}>
        Biểu ghi theo khổ mẫu MARC 21. Cột <b>CT1</b>, <b>CT2</b> là hai chỉ thị của trường; dấu gạch
        dưới nghĩa là chỉ thị để trống. Ký hiệu <b>$a</b>, <b>$b</b>… là mã trường con.
      </Typography.Paragraph>

      <div style={{ marginBottom: 12 }}>
        <Typography.Text type="secondary" style={{ fontSize: 13 }}>
          Đầu biểu ghi:{' '}
        </Typography.Text>
        <span style={MONOSPACE}>{bieuGhi.leader}</span>
      </div>

      <Table
        rowKey={(field) => `${field.tag}-${field.subfields.map((s) => s.code).join('')}-${field.value}`}
        size="small"
        pagination={false}
        columns={columns}
        dataSource={bieuGhi.fields}
        scroll={{ x: 720 }}
        locale={{ emptyText: 'Biểu ghi chưa có trường nào.' }}
      />
    </>
  );
}
