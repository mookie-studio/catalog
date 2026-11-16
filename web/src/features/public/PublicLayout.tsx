import { Route, Routes, useParams } from 'react-router'
import { NotFoundPage } from '@/components/NotFoundPage'

// Public catalog of one tenant, browsed by visitors.
export function PublicLayout() {
  const { tenantSlug } = useParams()

  return (
    <div className="min-h-svh">
      <header className="border-b px-6 py-4">
        <span className="font-semibold">{tenantSlug}</span>
      </header>
      <Routes>
        <Route index element={<main className="p-6">Catalog</main>} />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </div>
  )
}
