import { Route, Routes } from 'react-router'
import { NotFoundPage } from '@/components/NotFoundPage'

// Area where a platform admin manages tenants. Lazy-loaded.
export function PlatformArea() {
  return (
    <Routes>
      <Route index element={<main className="p-6">Platform</main>} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}
