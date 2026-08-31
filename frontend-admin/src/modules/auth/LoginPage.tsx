import { useState } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { Alert, Button, Card, Checkbox, Form, Input, Typography } from 'antd';
import { LockOutlined, UserOutlined } from '@ant-design/icons';
import { ApiRequestError } from '@/api/client';
import { useAuthStore } from '@/stores/authStore';
import { usePublicSettings } from '@/hooks/useLibraryName';
import { messages } from '@/i18n/messages';

interface LoginFormValues {
  username: string;
  password: string;
  remember: boolean;
}

export function LoginPage() {
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const navigate = useNavigate();
  const location = useLocation();
  const login = useAuthStore((state) => state.login);
  const user = useAuthStore((state) => state.user);
  const { data: settings } = usePublicSettings();

  if (user) {
    const from = (location.state as { from?: string } | null)?.from ?? '/';
    return <Navigate to={from} replace />;
  }

  const handleSubmit = async (values: LoginFormValues) => {
    setError(null);
    setSubmitting(true);

    try {
      await login(values.username, values.password);
      navigate('/', { replace: true });
    } catch (caught) {
      // The backend already phrases the reason in Vietnamese, including the lock-out message.
      setError(caught instanceof ApiRequestError ? caught.message : messages.errors.unexpected);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="lc-login-page">
      <Card className="lc-login-card" variant="borderless">
        <div className="lc-login-brand">
          {settings?.logoUrl ? (
            <img src={settings.logoUrl} alt="" className="lc-login-logo" />
          ) : (
            <span className="lc-brand-mark lc-brand-mark--large" aria-hidden="true">
              LC
            </span>
          )}
          <Typography.Title level={3} className="lc-login-title">
            {settings?.libraryName ?? messages.app.adminTitle}
          </Typography.Title>
          <Typography.Text type="secondary">{messages.auth.loginSubtitle}</Typography.Text>
        </div>

        {error && (
          <Alert type="error" showIcon message={error} className="lc-login-alert" closable onClose={() => setError(null)} />
        )}

        <Form<LoginFormValues>
          layout="vertical"
          size="large"
          requiredMark={false}
          initialValues={{ remember: true }}
          onFinish={handleSubmit}
          autoComplete="on"
        >
          <Form.Item
            name="username"
            label={messages.auth.username}
            rules={[{ required: true, message: messages.auth.usernameRequired }]}
          >
            <Input prefix={<UserOutlined />} placeholder={messages.auth.usernamePlaceholder} autoFocus autoComplete="username" />
          </Form.Item>

          <Form.Item
            name="password"
            label={messages.auth.password}
            rules={[{ required: true, message: messages.auth.passwordRequired }]}
          >
            <Input.Password
              prefix={<LockOutlined />}
              placeholder={messages.auth.passwordPlaceholder}
              autoComplete="current-password"
            />
          </Form.Item>

          <Form.Item name="remember" valuePropName="checked">
            <Checkbox>{messages.auth.rememberMe}</Checkbox>
          </Form.Item>

          <Button type="primary" htmlType="submit" block loading={submitting}>
            {messages.auth.login}
          </Button>
        </Form>

        <Typography.Paragraph type="secondary" className="lc-login-footer">
          {messages.app.poweredBy}
        </Typography.Paragraph>
      </Card>
    </div>
  );
}
