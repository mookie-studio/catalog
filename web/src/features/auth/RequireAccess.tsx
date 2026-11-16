import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import { type CurrentUser, useCurrentUser } from '@/features/auth/current-user'
import { NoAccessPage } from '@/features/auth/NoAccessPage'

export type Area = 'admin' | 'platform'

function canEnter(user: CurrentUser, area: Area): boolean {
  return area === 'admin' ? user.tenantId !== null : user.isPlatformAdmin
}

/** Renders the area only for people allowed in; sends signed-out people to /login. */
export function RequireAccess({ area, children }: { area: Area; children: ReactNode }) {
  const location = useLocation()
  const { data: user, isPending, isError } = useCurrentUser()

  if (isPending) {
    return <p role="status" className="p-6 text-muted-foreground">Loading…</p>
  }
  if (isError) {
    return <p role="alert" className="p-6">Something went wrong. Please reload the page.</p>
  }
  if (user === null) {
    const redirectTo = encodeURIComponent(location.pathname + location.search)
    return <Navigate to={`/login?redirectTo=${redirectTo}`} replace />
  }
  if (!canEnter(user, area)) {
    return <NoAccessPage />
  }
  return children
}
