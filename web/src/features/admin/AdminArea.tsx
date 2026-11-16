import { Route, Routes } from 'react-router'
import { NotFoundPage } from '@/components/NotFoundPage'
import { RequireAccess } from '@/features/auth/RequireAccess'
import { SignOutButton } from '@/features/auth/SignOutButton'

// Area where an admin manages the catalog of their tenant. Lazy-loaded.
export function AdminArea() {
  return (
    <RequireAccess area="admin">
      <div className="min-h-svh">
        <header className="flex items-center justify-between border-b px-6 py-3">
          <span className="font-semibold">Admin</span>
          <SignOutButton />
        </header>
        <Routes>
          <Route index element={<main className="p-6">Admin</main>} />
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </div>
    </RequireAccess>
  )
}
