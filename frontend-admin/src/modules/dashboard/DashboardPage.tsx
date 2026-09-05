import { Button, Card, Col, Row, Statistic, Tag, Tooltip, Typography } from 'antd';
import { BarChartOutlined, KeyOutlined, PartitionOutlined, TeamOutlined } from '@ant-design/icons';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { useAuthStore } from '@/stores/authStore';
import { usePublicSettings } from '@/hooks/useLibraryName';
import { messages } from '@/i18n/messages';
import { PERMISSIONS } from '@/api/permissions';
import { reportsApi } from '@/modules/reports/api';
import { formatMetric, type OverviewMetric } from '@/modules/reports/types';

/** Quyền nào trong số này cũng đủ để máy chủ trả báo cáo tổng quan (ReportsController). */
const REPORT_PERMISSIONS = [
  PERMISSIONS.acquisition.reportView,
  PERMISSIONS.circulation.reportView,
  PERMISSIONS.reader.reportView,
  PERMISSIONS.digital.reportView,
  PERMISSIONS.serial.reportView,
  PERMISSIONS.course.reportView,
] as const;

/**
 * Màn hình đầu tiên sau khi đăng nhập: toàn cảnh hoạt động của thư viện từ đầu năm tới hôm nay,
 * lấy từ đúng báo cáo tổng quan mà mục Báo cáo thống kê dùng. Tài khoản không có quyền xem báo cáo
 * nào thì chỉ thấy quyền hạn của chính mình.
 */
export function DashboardPage() {
  const user = useAuthStore((state) => state.user);
  const hasAnyPermission = useAuthStore((state) => state.hasAnyPermission);
  const { data: settings } = usePublicSettings();

  const canSeeReports = hasAnyPermission(REPORT_PERMISSIONS);
  const from = dayjs().startOf('year').format('YYYY-MM-DD');
  const to = dayjs().format('YYYY-MM-DD');

  const overview = useQuery({
    queryKey: ['report-overview', from, to],
    queryFn: () => reportsApi.overview(from, to),
    enabled: canSeeReports,
  });

  if (!user) {
    return null;
  }

  return (
    <div className="lc-page">
      <Typography.Title level={3}>{messages.dashboard.welcome(user.fullName)}</Typography.Title>
      <Typography.Paragraph type="secondary">
        {settings?.libraryName ?? messages.dashboard.subtitle}
      </Typography.Paragraph>

      {canSeeReports ? (
        <>
          <div className="lc-dashboard__period">
            <Typography.Text type="secondary">{messages.dashboard.periodYear}</Typography.Text>
            <Link to="/bao-cao">
              <Button size="small" icon={<BarChartOutlined />}>
                {messages.dashboard.openReports}
              </Button>
            </Link>
          </div>

          {(overview.data?.sections ?? []).map((section) => (
            <Card key={section.key} title={section.title} loading={overview.isLoading} className="lc-page-card">
              <Row gutter={[16, 16]}>
                {section.metrics.map((metric) => (
                  <Col key={metric.key} xs={12} sm={12} md={8} lg={6} xl={4}>
                    <MetricCard metric={metric} />
                  </Col>
                ))}
              </Row>
            </Card>
          ))}

          {overview.isLoading && <Card loading className="lc-page-card" />}
        </>
      ) : (
        <Typography.Paragraph type="secondary">{messages.dashboard.noReportRight}</Typography.Paragraph>
      )}

      <Card title={messages.dashboard.yourGroups} className="lc-page-card" variant="borderless">
        <Row gutter={[16, 16]}>
          <Col xs={24} sm={8}>
            <Statistic
              title={messages.dashboard.permissionCount}
              value={user.permissions.length}
              prefix={<KeyOutlined />}
            />
          </Col>
          <Col xs={24} sm={8}>
            <Statistic title={messages.dashboard.groupCount} value={user.groups.length} prefix={<TeamOutlined />} />
          </Col>
          <Col xs={24} sm={8}>
            <Statistic
              title={messages.dashboard.scopeCount}
              value={user.dataScopes.length}
              prefix={<PartitionOutlined />}
            />
          </Col>
        </Row>

        <div className="lc-dashboard__groups">
          {user.groups.length > 0 ? (
            user.groups.map((group) => (
              <Tag color="blue" key={group}>
                {group}
              </Tag>
            ))
          ) : (
            <Typography.Text type="secondary">{messages.table.empty}</Typography.Text>
          )}
        </div>

        <Typography.Paragraph type="secondary" className="lc-scope-note">
          {user.dataScopes.length === 0
            ? messages.dashboard.noScope
            : user.dataScopes.map((scope) => `${scope.scopeType}: ${scope.scopeName ?? scope.scopeId}`).join(', ')}
        </Typography.Paragraph>
      </Card>
    </div>
  );
}

function MetricCard({ metric }: { metric: OverviewMetric }) {
  const card = (
    <Statistic title={metric.label} value={formatMetric(metric)} valueStyle={{ fontSize: 22, fontWeight: 600 }} />
  );

  return metric.hint ? <Tooltip title={metric.hint}>{card}</Tooltip> : card;
}
