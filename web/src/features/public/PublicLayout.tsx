import { Link, Route, Routes, useParams } from 'react-router'
import { NotFoundPage } from '@/components/NotFoundPage'
import { cn } from '@/lib/utils'
import { CatalogPage } from '@/features/public/CatalogPage'
import { ProductPage } from '@/features/public/ProductPage'
import { brandStyle } from '@/features/public/brand'
import { NotFoundError, usePublicTenant } from '@/features/public/public-api'
import { TenantContact } from '@/features/public/TenantContact'

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

  const style = brandStyle(tenant.data.brandColor)

  return (
    <div className="min-h-svh" style={style}>
      <header
        className={cn('border-b px-4 py-4 sm:px-6', style && 'border-transparent bg-[var(--brand)] text-[var(--brand-foreground)]')}
      >
        <Link to={`/${tenantSlug}`} className="flex items-center gap-3 text-lg font-semibold tracking-tight">
          {tenant.data.logoUrl && <img src={tenant.data.logoUrl} alt="" className="h-10 w-auto max-w-40 object-contain" />}
          <span>{tenant.data.name}</span>
        </Link>
      </header>
      <Routes>
        <Route index element={<CatalogPage tenantSlug={tenantSlug} />} />
        <Route path="p/:productSlug" element={<ProductPage tenantSlug={tenantSlug} />} />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
      <TenantContact tenant={tenant.data} />
    </div>
  )
}
