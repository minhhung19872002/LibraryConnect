import { Alert, Card, Col, Row, Statistic, Tag, Typography } from 'antd';
import { KeyOutlined, PartitionOutlined, TeamOutlined } from '@ant-design/icons';
import { useAuthStore } from '@/stores/authStore';
import { usePublicSettings } from '@/hooks/useLibraryName';
import { messages } from '@/i18n/messages';

/**
 * Landing screen. The operational widgets (loans due today, catalogue queue, acquisition alerts)
 * arrive with the subsystems that produce them; until then it shows the signed-in account's
 * effective rights, which is what an acceptance reviewer checks first.
 */
export function DashboardPage() {
  const user = useAuthStore((state) => state.user);
  const { data: settings } = usePublicSettings();

  if (!user) {
    return null;
  }

  return (
    <div className="lc-page">
      <Typography.Title level={3}>{messages.dashboard.welcome(user.fullName)}</Typography.Title>
      <Typography.Paragraph type="secondary">
        {settings?.libraryName ?? messages.dashboard.subtitle}
      </Typography.Paragraph>

      <Alert type="info" showIcon message={messages.dashboard.phaseNotice} className="lc-page-alert" />

      <Row gutter={[16, 16]}>
        <Col xs={24} sm={12} lg={8}>
          <Card variant="borderless">
            <Statistic
              title={messages.dashboard.permissionCount}
              value={user.permissions.length}
              prefix={<KeyOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={8}>
          <Card variant="borderless">
            <Statistic title={messages.dashboard.groupCount} value={user.groups.length} prefix={<TeamOutlined />} />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={8}>
          <Card variant="borderless">
            <Statistic
              title={messages.dashboard.scopeCount}
              value={user.dataScopes.length}
              prefix={<PartitionOutlined />}
            />
          </Card>
        </Col>
      </Row>

      <Card title={messages.dashboard.yourGroups} className="lc-page-card" variant="borderless">
        {user.groups.length > 0 ? (
          user.groups.map((group) => (
            <Tag color="blue" key={group}>
              {group}
            </Tag>
          ))
        ) : (
          <Typography.Text type="secondary">{messages.table.empty}</Typography.Text>
        )}

        <Typography.Paragraph type="secondary" className="lc-scope-note">
          {user.dataScopes.length === 0
            ? messages.dashboard.noScope
            : user.dataScopes.map((scope) => `${scope.scopeType}: ${scope.scopeName ?? scope.scopeId}`).join(', ')}
        </Typography.Paragraph>
      </Card>
    </div>
  );
}
