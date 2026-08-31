import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { Tooltip } from 'antd';
import { useAuthStore } from '@/stores/authStore';
import { messages } from '@/i18n/messages';

interface RequirePermissionProps {
  permission: string | readonly string[];
  children: ReactNode;
}

/**
 * Route guard. A user who reaches a screen they have no permission for is sent to the 403 page
 * rather than shown an empty grid full of failed requests.
 *
 * This is convenience only: every endpoint behind the screen enforces the same codes server-side.
 */
export function RequirePermissionRoute({ permission, children }: RequirePermissionProps) {
  const hasAnyPermission = useAuthStore((state) => state.hasAnyPermission);
  const initialising = useAuthStore((state) => state.initialising);

  if (initialising) {
    return null;
  }

  const required = typeof permission === 'string' ? [permission] : permission;

  return hasAnyPermission(required) ? <>{children}</> : <Navigate to="/khong-du-quyen" replace />;
}

interface CanProps {
  permission: string | readonly string[];
  children: ReactNode;
  /** Render the child disabled with an explanatory tooltip instead of hiding it entirely. */
  mode?: 'hide' | 'disable';
}

/**
 * Wraps an action so it disappears (or greys out) for users without the permission.
 *
 * "disable" is the friendlier choice for a button a user might expect to see: it tells them the
 * action exists and why they cannot use it, rather than leaving them wondering where it went.
 */
export function Can({ permission, children, mode = 'hide' }: CanProps) {
  const hasAnyPermission = useAuthStore((state) => state.hasAnyPermission);
  const required = typeof permission === 'string' ? [permission] : permission;

  if (hasAnyPermission(required)) {
    return <>{children}</>;
  }

  if (mode === 'hide') {
    return null;
  }

  return (
    <Tooltip title={messages.errors.forbidden}>
      <span className="lc-disabled-action">{children}</span>
    </Tooltip>
  );
}

/** Imperative form for callbacks and column definitions, where a component would be awkward. */
export function usePermission() {
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const hasAnyPermission = useAuthStore((state) => state.hasAnyPermission);

  return { can: hasPermission, canAny: hasAnyPermission };
}
