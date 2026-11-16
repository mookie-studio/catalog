import { Navigate, NavLink, Route, Routes } from 'react-router'
import { NotFoundPage } from '@/components/NotFoundPage'
import { useCurrentTenant } from '@/features/admin/admin-api'
import { AttributesPage } from '@/features/admin/attributes/AttributesPage'
import { NewProductPage } from '@/features/admin/products/NewProductPage'
import { ProductEditPage } from '@/features/admin/products/ProductEditPage'
import { ProductListPage } from '@/features/admin/products/ProductListPage'
import { RequireAccess } from '@/features/auth/RequireAccess'
import { SignOutButton } from '@/features/auth/SignOutButton'
import { cn } from '@/lib/utils'

const sections = [
  { to: 'products', label: 'Products' },
  { to: 'attributes', label: 'Attributes' },
  { to: 'categories', label: 'Categories' },
  { to: 'team', label: 'Team' },
]

// Area where an admin manages the catalog of their tenant. Lazy-loaded.
export function AdminArea() {
  return (
    <RequireAccess area="admin">
      <AdminLayout />
    </RequireAccess>
  )
}

function AdminLayout() {
  const tenant = useCurrentTenant()
  return (
    <div className="min-h-svh">
      <header className="border-b">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-3 px-4 py-3 sm:px-6">
          <span className="font-semibold">{tenant.data?.name ?? 'Admin'}</span>
          <SignOutButton />
        </div>
        <nav aria-label="Admin" className="mx-auto flex max-w-6xl gap-1 overflow-x-auto px-2 sm:px-4">
          {sections.map((section) => (
            <NavLink
              key={section.to}
              to={`/admin/${section.to}`}
              className={({ isActive }) =>
                cn(
                  'border-b-2 border-transparent px-3 py-2 text-sm text-muted-foreground hover:text-foreground',
                  isActive && 'border-foreground font-medium text-foreground',
                )
              }
            >
              {section.label}
            </NavLink>
          ))}
        </nav>
      </header>
      <div className="mx-auto max-w-6xl px-4 py-6 sm:px-6">
        <Routes>
          <Route index element={<Navigate to="products" replace />} />
          <Route path="products" element={<ProductListPage />} />
          <Route path="products/new" element={<NewProductPage />} />
          <Route path="products/:id" element={<ProductEditPage />} />
          <Route path="attributes" element={<AttributesPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </div>
    </div>
  )
}
