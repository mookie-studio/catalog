import { Link, Route, Routes, useParams } from 'react-router'
import { NotFoundPage } from '@/components/NotFoundPage'
import { CatalogPage } from '@/features/public/CatalogPage'
import { ProductPage } from '@/features/public/ProductPage'
import { NotFoundError, usePublicTenant } from '@/features/public/public-api'

// Public catalog of one tenant, browsed by visitors.
export function PublicLayout() {
  const tenantSlug = useParams().tenantSlug ?? ''
  const tenant = usePublicTenant(tenantSlug)

  if (tenant.error instanceof NotFoundError) {
    return <NotFoundPage />
  }
  if (tenant.isError) {
    return <p role="alert" className="p-6">The catalog could not be loaded. Please reload the page.</p>
  }
  if (tenant.isPending) {
    return <p role="status" className="p-6 text-muted-foreground">Loading…</p>
  }

  return (
    <div className="min-h-svh">
      <header className="border-b px-4 py-4 sm:px-6">
        <Link to={`/${tenantSlug}`} className="text-lg font-semibold tracking-tight">
          {tenant.data.name}
        </Link>
      </header>
      <Routes>
        <Route index element={<CatalogPage tenantSlug={tenantSlug} />} />
        <Route path="p/:productSlug" element={<ProductPage tenantSlug={tenantSlug} />} />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </div>
  )
}
