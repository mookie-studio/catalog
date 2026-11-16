import { Navigate, NavLink, Route, Routes } from 'react-router'
import { NotFoundPage } from '@/components/NotFoundPage'
import { RequireAccess } from '@/features/auth/RequireAccess'
import { SignOutButton } from '@/features/auth/SignOutButton'
import { TenantsPage } from '@/features/platform/tenants/TenantsPage'

// Area where a platform admin manages tenants. Lazy-loaded.
export function PlatformArea() {
  return (
    <RequireAccess area="platform">
      <div className="min-h-svh">
        <header className="border-b">
          <div className="mx-auto flex max-w-6xl items-center justify-between gap-3 px-4 py-3 sm:px-6">
            <span className="font-semibold">Platform</span>
            <SignOutButton />
          </div>
          <nav aria-label="Platform" className="mx-auto flex max-w-6xl gap-1 px-2 sm:px-4">
            <NavLink
              to="/platform/tenants"
              className={({ isActive }) =>
                isActive
                  ? 'border-b-2 border-foreground px-3 py-2 text-sm font-medium'
                  : 'border-b-2 border-transparent px-3 py-2 text-sm text-muted-foreground hover:text-foreground'
              }
            >
              Tenants
            </NavLink>
          </nav>
        </header>
        <div className="mx-auto max-w-6xl px-4 py-6 sm:px-6">
          <Routes>
            <Route index element={<Navigate to="tenants" replace />} />
            <Route path="tenants" element={<TenantsPage />} />
            <Route path="*" element={<NotFoundPage />} />
          </Routes>
        </div>
      </div>
    </RequireAccess>
  )
}
