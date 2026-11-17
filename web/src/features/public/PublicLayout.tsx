import { useQueryClient } from '@tanstack/react-query'
import { useEffect } from 'react'
import { Link, Route, Routes, useLocation, useNavigate, useParams } from 'react-router'
import { NotFoundPage } from '@/components/NotFoundPage'
import { cn } from '@/lib/utils'
import { CatalogPage } from '@/features/public/CatalogPage'
import { ProductPage } from '@/features/public/ProductPage'
import { brandStyle } from '@/features/public/brand'
import { NotFoundError, type PublicTenant, publicTenantKey, usePublicTenant } from '@/features/public/public-api'
import { TenantContact } from '@/features/public/TenantContact'

// Public catalog of one tenant, browsed by visitors.
export function PublicLayout() {
  const tenantSlug = useParams().tenantSlug ?? ''
  const tenant = usePublicTenant(tenantSlug)
  useCurrentTenantAddress(tenantSlug, tenant.data)

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

/**
 * An Old Tenant slug still loads the catalog (ADR 0009); the address bar is then replaced with the
 * current slug, keeping the rest of the path and the query string.
 */
function useCurrentTenantAddress(urlSlug: string, tenant: PublicTenant | undefined) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const { pathname, search } = useLocation()

  useEffect(() => {
    if (!tenant || tenant.slug === urlSlug) return
    // Seed the current address's cache so the page does not flash a loading state.
    queryClient.setQueryData(publicTenantKey(tenant.slug), tenant)
    const rest = pathname.slice(`/${urlSlug}`.length)
    navigate(`/${tenant.slug}${rest}${search}`, { replace: true })
  }, [tenant, urlSlug, pathname, search, navigate, queryClient])
}
