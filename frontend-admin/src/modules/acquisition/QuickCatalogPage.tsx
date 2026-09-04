import { useEffect, useRef, useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Col,
  Form,
  Input,
  InputNumber,
  Row,
  Select,
  Space,
  Statistic,
  Table,
  Typography,
  type InputRef,
} from 'antd';
import { SaveOutlined } from '@ant-design/icons';
import { useMutation, useQuery } from '@tanstack/react-query';
import { PageHeader } from '@/components/PageHeader';
import { PERMISSIONS } from '@/api/permissions';
import { Can } from '@/components/PermissionGate';
import { ApiRequestError } from '@/api/client';
import { useCatalogOptions, toOptions } from '@/modules/cataloging/useCatalogOptions';
import { MAU } from '@/lib/palette';
import { locationsApi, purchaseApi } from './api';
import { acquisitionTypeLabels } from './labels';
import { nextQuickCatalogValues, type QuickCatalogValues } from './quickEntry';
import type { QuickCatalogResultDto } from './types';

interface EntryLog {
  key: number;
  title: string;
  controlNumber: string;
  reusedExisting: boolean;
  createdItems: number;
  barcodes: string[];
}

/**
 * III.2 — Biên mục sơ lược, nhập nhanh liên tục.
 *
 * Đây là màn hình cho một chồng sách tặng hay một thùng sách mua ngoài đơn đặt: không có dòng đơn
 * nào để mở ra biên mục. Lưu xong, form giữ lại kho, dạng tài liệu, nhà xuất bản của cả đợt, xóa
 * phần thuộc riêng cuốn vừa nhập và trả tiêu điểm về ô nhan đề — tay không rời bàn phím.
 */
export function QuickCatalogPage() {
  const { message } = App.useApp();
  const [form] = Form.useForm<QuickCatalogValues>();
  const titleRef = useRef<InputRef>(null);
  const [log, setLog] = useState<EntryLog[]>([]);

  const documentTypes = useCatalogOptions('document-types');
  const languages = useCatalogOptions('languages');
  const fundingSources = useCatalogOptions('funding-sources');
  const warehouses = useQuery({ queryKey: ['acq-warehouses', null], queryFn: () => locationsApi.warehouses() });

  const warehouseId = Form.useWatch('warehouseId', form);
  const itemQuantity = Form.useWatch('itemQuantity', form) ?? 0;

  const shelves = useQuery({
    queryKey: ['acq-shelves', warehouseId],
    queryFn: () => locationsApi.shelves(warehouseId),
    enabled: Boolean(warehouseId),
  });

  useEffect(() => {
    titleRef.current?.focus();
  }, []);

  const save = useMutation({
    mutationFn: (values: QuickCatalogValues) => purchaseApi.quickCatalog({ ...values }),
    onSuccess: (result: QuickCatalogResultDto, values) => {
      setLog((current) => [
        {
          key: Date.now(),
          title: values.title ?? '',
          controlNumber: result.controlNumber,
          reusedExisting: result.reusedExisting,
          createdItems: result.createdItems,
          barcodes: result.barcodes,
        },
        ...current,
      ]);

      message.success(
        result.reusedExisting
          ? `Đã thêm bản cho biểu ghi ${result.controlNumber} sẵn có.`
          : `Đã tạo biểu ghi ${result.controlNumber} và đưa vào hàng đợi biên mục.`,
      );

      // Giữ bối cảnh của đợt, xóa phần của cuốn vừa nhập, quay về ô nhan đề.
      form.setFieldsValue({
        title: undefined,
        subTitle: undefined,
        author: undefined,
        isbn: undefined,
        pages: undefined,
        ddc: undefined,
        price: undefined,
        note: undefined,
        ...nextQuickCatalogValues(values),
      });
      titleRef.current?.focus();
    },
    onError: (error) =>
      message.error(error instanceof ApiRequestError ? error.message : 'Không lưu được.'),
  });

  const createdItems = log.reduce((sum, entry) => sum + entry.createdItems, 0);

  return (
    <div className="lc-page">
      <PageHeader
        title="Biên mục sơ lược"
        description="Nhập nhanh liên tục: mười trường, lưu đúng cấu trúc MARC 21, tự đẩy vào hàng đợi biên mục chi tiết. Lưu xong form giữ nguyên bối cảnh và quay về ô nhan đề."
      />

      <Row gutter={12}>
        <Col span={15}>
          <Card variant="borderless">
            <Form
              form={form}
              layout="vertical"
              initialValues={{ reuseDuplicate: true, itemQuantity: 1, acquisitionType: 'Purchase' }}
              onFinish={(values) => save.mutate(values)}
            >
              <Form.Item
                name="title"
                label="Nhan đề"
                rules={[{ required: true, message: 'Chưa nhập nhan đề.' }]}
              >
                <Input ref={titleRef} placeholder="Giáo trình cơ sở dữ liệu" autoFocus />
              </Form.Item>
              <Row gutter={12}>
                <Col span={12}>
                  <Form.Item name="subTitle" label="Phụ đề">
                    <Input />
                  </Form.Item>
                </Col>
                <Col span={12}>
                  <Form.Item name="author" label="Tác giả">
                    <Input />
                  </Form.Item>
                </Col>
              </Row>
              <Row gutter={12}>
                <Col span={8}>
                  <Form.Item name="publishPlace" label="Nơi xuất bản">
                    <Input placeholder="Hà Nội" />
                  </Form.Item>
                </Col>
                <Col span={10}>
                  <Form.Item name="publisherName" label="Nhà xuất bản">
                    <Input />
                  </Form.Item>
                </Col>
                <Col span={6}>
                  <Form.Item name="publishYear" label="Năm">
                    <InputNumber min={1400} max={2200} style={{ width: '100%' }} />
                  </Form.Item>
                </Col>
              </Row>
              <Row gutter={12}>
                <Col span={8}>
                  <Form.Item name="isbn" label="ISBN">
                    <Input />
                  </Form.Item>
                </Col>
                <Col span={5}>
                  <Form.Item name="pages" label="Số trang">
                    <InputNumber min={1} style={{ width: '100%' }} />
                  </Form.Item>
                </Col>
                <Col span={5}>
                  <Form.Item name="ddc" label="Chỉ số DDC">
                    <Input placeholder="005.74" />
                  </Form.Item>
                </Col>
                <Col span={6}>
                  <Form.Item name="price" label="Giá bìa">
                    <InputNumber min={0} step={1000} style={{ width: '100%' }} />
                  </Form.Item>
                </Col>
              </Row>
              <Row gutter={12}>
                <Col span={12}>
                  <Form.Item name="documentTypeId" label="Dạng tài liệu">
                    <Select allowClear options={toOptions(documentTypes.data)} />
                  </Form.Item>
                </Col>
                <Col span={12}>
                  <Form.Item name="languageId" label="Ngôn ngữ">
                    <Select allowClear options={toOptions(languages.data)} />
                  </Form.Item>
                </Col>
              </Row>

              <Typography.Title level={5}>Bản in (ĐKCB)</Typography.Title>
              <Row gutter={12}>
                <Col span={5}>
                  <Form.Item name="itemQuantity" label="Số bản" extra="0 thì chỉ tạo biểu ghi.">
                    <InputNumber min={0} max={500} style={{ width: '100%' }} />
                  </Form.Item>
                </Col>
                <Col span={7}>
                  <Form.Item
                    name="warehouseId"
                    label="Kho"
                    rules={[{ required: itemQuantity > 0, message: 'Tạo ĐKCB thì phải chọn kho.' }]}
                  >
                    <Select
                      allowClear
                      options={(warehouses.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
                    />
                  </Form.Item>
                </Col>
                <Col span={6}>
                  <Form.Item name="shelfId" label="Vị trí giá">
                    <Select
                      allowClear
                      disabled={!warehouseId}
                      options={(shelves.data ?? []).map((item) => ({ value: item.id, label: item.name }))}
                    />
                  </Form.Item>
                </Col>
                <Col span={6}>
                  <Form.Item name="acquisitionType" label="Hình thức">
                    <Select
                      options={Object.entries(acquisitionTypeLabels).map(([value, label]) => ({ value, label }))}
                    />
                  </Form.Item>
                </Col>
              </Row>
              <Row gutter={12}>
                <Col span={12}>
                  <Form.Item name="fundingSourceId" label="Nguồn kinh phí">
                    <Select allowClear options={toOptions(fundingSources.data)} />
                  </Form.Item>
                </Col>
                <Col span={12}>
                  <Form.Item name="reuseDuplicate" label="Khi thư viện đã có tài liệu này">
                    <Select
                      options={[
                        { value: true, label: 'Dùng lại biểu ghi đã có, chỉ thêm bản' },
                        { value: false, label: 'Vẫn tạo biểu ghi mới' },
                      ]}
                    />
                  </Form.Item>
                </Col>
              </Row>
              <Form.Item name="note" label="Ghi chú">
                <Input.TextArea rows={2} />
              </Form.Item>

              <Space>
                <Can permission={PERMISSIONS.acquisition.itemCreate}>
                  <Button
                    type="primary"
                    icon={<SaveOutlined />}
                    htmlType="submit"
                    loading={save.isPending}
                  >
                    Lưu và nhập cuốn tiếp theo
                  </Button>
                </Can>
                <Typography.Text type="secondary">
                  Enter ở ô cuối cũng lưu; kho, dạng tài liệu, nhà xuất bản được giữ cho cuốn sau.
                </Typography.Text>
              </Space>
            </Form>
          </Card>
        </Col>

        <Col span={9}>
          <Space direction="vertical" size="middle" style={{ width: '100%' }}>
            <Row gutter={12}>
              <Col span={12}>
                <Card size="small">
                  <Statistic title="Đã nhập" value={log.length} suffix="biểu ghi" valueStyle={{ color: MAU.chinh }} />
                </Card>
              </Col>
              <Col span={12}>
                <Card size="small">
                  <Statistic title="ĐKCB đã sinh" value={createdItems} />
                </Card>
              </Col>
            </Row>

            {log.length === 0 ? (
              <Alert
                type="info"
                showIcon
                message="Chưa nhập cuốn nào trong phiên này."
                description="Biểu ghi lưu xong sẽ liệt kê ở đây kèm mã vạch đã sinh, để đối chiếu khi dán tem."
              />
            ) : (
              <Card variant="borderless" size="small" title="Đã nhập trong phiên">
                <Table
                  rowKey="key"
                  size="small"
                  pagination={false}
                  dataSource={log}
                  scroll={{ y: 420 }}
                  columns={[
                    {
                      title: 'Nhan đề',
                      dataIndex: 'title',
                      width: 200,
                      ellipsis: true,
                      render: (value: string, row) => (
                        <Space direction="vertical" size={0}>
                          <span>{value}</span>
                          <Typography.Text type="secondary">
                            {row.controlNumber}
                            {row.reusedExisting ? ' · dùng lại' : ''}
                          </Typography.Text>
                        </Space>
                      ),
                    },
                    {
                      title: 'Mã vạch',
                      dataIndex: 'barcodes',
                      width: 150,
                      render: (value: string[]) =>
                        value.length === 0 ? (
                          <Typography.Text type="secondary">—</Typography.Text>
                        ) : (
                          value.join(', ')
                        ),
                    },
                  ]}
                />
              </Card>
            )}
          </Space>
        </Col>
      </Row>
    </div>
  );
}
