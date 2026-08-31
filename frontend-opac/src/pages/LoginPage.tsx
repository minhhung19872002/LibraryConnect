import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { App, Button, Card, Form, Input, Typography } from 'antd';
import { LockOutlined, IdcardOutlined } from '@ant-design/icons';
import { useAuthStore } from '@/stores/authStore';
import { FALLBACK_LIBRARY_NAME, useSiteSettings } from '@/hooks/useSite';

const { Paragraph, Title } = Typography;

/** IX.3 — Bạn đọc đăng nhập bằng số thẻ và mật khẩu. */
export function LoginPage() {
  const navigate = useNavigate();
  const { message } = App.useApp();
  const login = useAuthStore((state) => state.login);
  const { data: settings } = useSiteSettings();
  const [loading, setLoading] = useState(false);

  const submit = async (values: { cardNumber: string; password: string }) => {
    setLoading(true);

    try {
      await login(values.cardNumber.trim(), values.password);
      message.success('Đăng nhập thành công.');
      navigate('/tai-khoan');
    } catch (error) {
      message.error((error as Error).message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      className="lc-container"
      style={{ padding: '48px 16px', maxWidth: 460, display: 'block' }}
    >
      <Card>
        <Title level={3} style={{ marginTop: 0 }}>
          Đăng nhập bạn đọc
        </Title>
        <Paragraph type="secondary">
          Dùng số thẻ thư viện và mật khẩu do {settings?.libraryName ?? FALLBACK_LIBRARY_NAME} cấp.
          Quên mật khẩu thì liên hệ quầy phục vụ để được đặt lại.
        </Paragraph>

        <Form layout="vertical" onFinish={submit} requiredMark={false}>
          <Form.Item
            label="Số thẻ thư viện"
            name="cardNumber"
            rules={[{ required: true, message: 'Chưa nhập số thẻ thư viện.' }]}
          >
            <Input
              size="large"
              prefix={<IdcardOutlined />}
              placeholder="Ví dụ: TV2025000123"
              autoFocus
            />
          </Form.Item>

          <Form.Item
            label="Mật khẩu"
            name="password"
            rules={[{ required: true, message: 'Chưa nhập mật khẩu.' }]}
          >
            <Input.Password size="large" prefix={<LockOutlined />} placeholder="Mật khẩu" />
          </Form.Item>

          <Button type="primary" size="large" htmlType="submit" block loading={loading}>
            Đăng nhập
          </Button>
        </Form>
      </Card>
    </div>
  );
}
