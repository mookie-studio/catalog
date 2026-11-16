import { Route, Routes } from 'react-router'
import { NotFoundPage } from '@/components/NotFoundPage'

// Area where an admin manages the catalog of their tenant. Lazy-loaded.
export function AdminArea() {
  return (
    <Routes>
      <Route index element={<main className="p-6">Admin</main>} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}
