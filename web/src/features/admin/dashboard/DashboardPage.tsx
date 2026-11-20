import { Link } from 'react-router'
import { useDashboard } from '@/features/admin/dashboard/dashboard-api'
import { MostViewedProducts } from '@/features/admin/dashboard/MostViewedProducts'

function Card({ to, label, count }: { to: string; label: string; count: number }) {
  return (
    <Link to={to} className="rounded-lg border p-4 hover:bg-muted/50">
      <p className="text-3xl font-semibold tabular-nums">{count}</p>
      <p className="text-sm text-muted-foreground">{label}</p>
    </Link>
  )
}

/** The admin area's start page: catalog counts, what needs attention and the most viewed products. */
export function DashboardPage() {
  const dashboard = useDashboard()

  return (
    <main className="flex flex-col gap-8">
      <h1 className="display-m">Dashboard</h1>
      {dashboard.isPending ? (
        <p role="status" className="text-muted-foreground">Loading…</p>
      ) : dashboard.isError ? (
        <p role="alert">The dashboard could not be loaded.</p>
      ) : (
        <>
          <section aria-label="Catalog" className="grid grid-cols-2 gap-3 md:grid-cols-5">
            <Card to="/admin/products?status=Draft" label="Draft" count={dashboard.data.draftCount} />
            <Card to="/admin/products?status=Published" label="Published" count={dashboard.data.publishedCount} />
            <Card to="/admin/products?status=Archived" label="Archived" count={dashboard.data.archivedCount} />
            <Card to="/admin/products?status=Published&featured=true" label="Featured" count={dashboard.data.featuredCount} />
            <Card to="/admin/inquiries?status=Open" label="Open inquiries" count={dashboard.data.openInquiryCount} />
          </section>
          <NeedsAttention data={dashboard.data} />
        </>
      )}
      <MostViewedProducts />
    </main>
  )
}

function NeedsAttention({ data }: { data: NonNullable<ReturnType<typeof useDashboard>['data']> }) {
  const warnings = [
    {
      count: data.publishedWithoutImagesCount,
      to: '/admin/products?status=Published&hasImages=false',
      text: (n: number) => `${n} published ${n === 1 ? 'product has' : 'products have'} no images`,
    },
    {
      count: data.hasCategories ? data.publishedWithoutCategoriesCount : 0,
      to: '/admin/products?status=Published&hasCategories=false',
      text: (n: number) => `${n} published ${n === 1 ? 'product has' : 'products have'} no categories`,
    },
  ].filter((warning) => warning.count > 0)

  return (
    <section aria-label="Needs attention" className="flex flex-col gap-3">
      <h2 className="text-lg font-semibold">Needs attention</h2>
      {warnings.length === 0 ? (
        <p className="text-muted-foreground">Everything looks good.</p>
      ) : (
        <ul className="flex flex-col gap-2">
          {warnings.map((warning) => (
            <li key={warning.to}>
              <Link to={warning.to} className="text-sm underline-offset-4 hover:underline">
                {warning.text(warning.count)}
              </Link>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
