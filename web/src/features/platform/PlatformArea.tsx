import { Route, Routes } from 'react-router'
import { NotFoundPage } from '@/components/NotFoundPage'
import { RequireAccess } from '@/features/auth/RequireAccess'
import { SignOutButton } from '@/features/auth/SignOutButton'

// Area where a platform admin manages tenants. Lazy-loaded.
export function PlatformArea() {
  return (
    <RequireAccess area="platform">
      <div className="min-h-svh">
        <header className="flex items-center justify-between border-b px-6 py-3">
          <span className="font-semibold">Platform</span>
          <SignOutButton />
        </header>
        <Routes>
          <Route index element={<main className="p-6">Platform</main>} />
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </div>
    </RequireAccess>
  )
}
