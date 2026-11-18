import { Navigate, NavLink, Route, Routes } from 'react-router'
import { NotFoundPage } from '@/components/NotFoundPage'
import { useCurrentTenant } from '@/features/admin/admin-api'
import { AttributesPage } from '@/features/admin/attributes/AttributesPage'
import { CategoriesPage } from '@/features/admin/categories/CategoriesPage'
import { ImportProductsPage } from '@/features/admin/products/ImportProductsPage'
import { NewProductPage } from '@/features/admin/products/NewProductPage'
import { DashboardPage } from '@/features/admin/dashboard/DashboardPage'
import { InquiriesPage } from '@/features/admin/inquiries/InquiriesPage'
import { useOpenInquiryCount } from '@/features/admin/inquiries/inquiries-api'
import { ProductEditPage } from '@/features/admin/products/ProductEditPage'
import { ProductListPage } from '@/features/admin/products/ProductListPage'
import { ProfilePage } from '@/features/admin/profile/ProfilePage'
import { TeamPage } from '@/features/admin/team/TeamPage'
import { RequireAccess } from '@/features/auth/RequireAccess'
import { SignOutButton } from '@/features/auth/SignOutButton'
import { cn } from '@/lib/utils'

const sections = [
  { to: 'dashboard', label: 'Dashboard' },
  { to: 'products', label: 'Products' },
  { to: 'attributes', label: 'Attributes' },
  { to: 'categories', label: 'Categories' },
  { to: 'inquiries', label: 'Inquiries' },
  { to: 'team', label: 'Team' },
  { to: 'profile', label: 'Profile' },
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
  const openInquiries = useOpenInquiryCount()
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
              {section.to === 'inquiries' && !!openInquiries.data && (
                <span aria-label={`${openInquiries.data} open`} className="ml-1.5 rounded-full bg-primary px-1.5 py-0.5 text-xs text-primary-foreground">
                  {openInquiries.data}
                </span>
              )}
            </NavLink>
          ))}
        </nav>
      </header>
      <div className="mx-auto max-w-6xl px-4 py-6 sm:px-6">
        <Routes>
          <Route index element={<Navigate to="dashboard" replace />} />
          <Route path="dashboard" element={<DashboardPage />} />
          <Route path="products" element={<ProductListPage />} />
          <Route path="products/new" element={<NewProductPage />} />
          <Route path="products/import" element={<ImportProductsPage />} />
          <Route path="products/:id" element={<ProductEditPage />} />
          <Route path="attributes" element={<AttributesPage />} />
          <Route path="categories" element={<CategoriesPage />} />
          <Route path="inquiries" element={<InquiriesPage />} />
          <Route path="team" element={<TeamPage />} />
          <Route path="profile" element={<ProfilePage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </div>
    </div>
  )
}
