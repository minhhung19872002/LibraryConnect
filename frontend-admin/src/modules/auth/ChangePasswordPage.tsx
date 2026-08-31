import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Alert, App, Button, Card, Form, Input, Typography } from 'antd';
import { LockOutlined } from '@ant-design/icons';
import { ApiRequestError, api } from '@/api/client';
import { useAuthStore } from '@/stores/authStore';
import { messages } from '@/i18n/messages';

interface ChangePasswordFormValues {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

/**
 * Password change, used both from the account menu and as the mandatory first-login step.
 * Every session is revoked server-side afterwards, so the user is sent back to the login screen.
 */
export function ChangePasswordPage() {
  const [form] = Form.useForm<ChangePasswordFormValues>();
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const navigate = useNavigate();
  const { message } = App.useApp();
  const mustChangePassword = useAuthStore((state) => state.mustChangePassword);
  const logout = useAuthStore((state) => state.logout);

  const handleSubmit = async (values: ChangePasswordFormValues) => {
    setError(null);
    setSubmitting(true);

    try {
      await api.post('/auth/change-password', values);
      message.success(messages.auth.changePasswordSuccess);
      await logout();
      navigate('/dang-nhap', { replace: true });
    } catch (caught) {
      if (caught instanceof ApiRequestError) {
        setError(caught.message);
        // Map field errors returned by the server onto the matching inputs.
        const fields = Object.entries(caught.fieldErrors).map(([name, errors]) => ({
          name: name as keyof ChangePasswordFormValues,
          errors,
        }));
        if (fields.length > 0) {
          form.setFields(fields);
        }
      } else {
        setError(messages.errors.unexpected);
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="lc-centered-page">
      <Card className="lc-login-card" variant="borderless">
        <Typography.Title level={4}>{messages.auth.changePassword}</Typography.Title>

        {mustChangePassword && (
          <Alert type="warning" showIcon message={messages.auth.mustChangePassword} className="lc-login-alert" />
        )}

        {error && <Alert type="error" showIcon message={error} className="lc-login-alert" />}

        <Form<ChangePasswordFormValues> form={form} layout="vertical" size="large" onFinish={handleSubmit}>
          <Form.Item
            name="currentPassword"
            label={messages.auth.currentPassword}
            rules={[{ required: true, message: messages.auth.passwordRequired }]}
          >
            <Input.Password prefix={<LockOutlined />} autoComplete="current-password" autoFocus />
          </Form.Item>

          <Form.Item
            name="newPassword"
            label={messages.auth.newPassword}
            rules={[{ required: true, message: messages.auth.passwordRequired }]}
          >
            <Input.Password prefix={<LockOutlined />} autoComplete="new-password" />
          </Form.Item>

          <Form.Item
            name="confirmPassword"
            label={messages.auth.confirmPassword}
            dependencies={['newPassword']}
            rules={[
              { required: true, message: messages.auth.passwordRequired },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || getFieldValue('newPassword') === value) {
                    return Promise.resolve();
                  }
                  return Promise.reject(new Error(messages.auth.confirmMismatch));
                },
              }),
            ]}
          >
            <Input.Password prefix={<LockOutlined />} autoComplete="new-password" />
          </Form.Item>

          <Button type="primary" htmlType="submit" block loading={submitting}>
            {messages.actions.save}
          </Button>
        </Form>
      </Card>
    </div>
  );
}
