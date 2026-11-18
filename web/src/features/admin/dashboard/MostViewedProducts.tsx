import { useMemo } from 'react'
import { Link, useSearchParams } from 'react-router'
import { Pagination } from '@/components/Pagination'
import { Select } from '@/components/ui/select'
import { type Period, periods, useProductStats } from '@/features/admin/dashboard/dashboard-api'
import { ProductStatusBadge } from '@/features/admin/products/ProductStatusBadge'

function readQuery(params: URLSearchParams) {
  const period = Number.parseInt(params.get('period') ?? '', 10) as Period
  const page = Number.parseInt(params.get('page') ?? '', 10)
  return { period: periods.includes(period) ? period : 30, page: Number.isFinite(page) && page > 0 ? page : 1 }
}

function writeQuery(query: { period: Period; page: number }) {
  const params = new URLSearchParams()
  if (query.period !== 30) params.set('period', String(query.period))
  if (query.page > 1) params.set('page', String(query.page))
  return params
}

/** Products ranked by Product views (and Inquiries) over the chosen period. */
export function MostViewedProducts() {
  const [searchParams, setSearchParams] = useSearchParams()
  const query = useMemo(() => readQuery(searchParams), [searchParams])
  const stats = useProductStats(query.period, query.page)

  return (
    <section aria-label="Most viewed products" className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-lg font-semibold">Most viewed products</h2>
        <Select
          aria-label="Period"
          className="w-44"
          value={query.period}
          onChange={(event) => setSearchParams(writeQuery({ period: Number(event.target.value) as Period, page: 1 }))}
        >
          {periods.map((days) => (
            <option key={days} value={days}>
              Last {days} days
            </option>
          ))}
        </Select>
      </div>
      <p className="text-sm text-muted-foreground">Views are counted per day without storing anything about visitors.</p>
      {stats.isPending ? (
        <p role="status" className="text-muted-foreground">Loading…</p>
      ) : stats.isError ? (
        <p role="alert">The statistics could not be loaded.</p>
      ) : stats.data.items.length === 0 ? (
        <p className="rounded-lg border border-dashed p-6 text-center text-muted-foreground">No views yet in this period.</p>
      ) : (
        <>
          <div className="overflow-x-auto rounded-lg border">
            <table className="w-full text-sm">
              <thead className="bg-muted/50 text-left text-muted-foreground">
                <tr>
                  <th className="px-3 py-2 font-medium">Product</th>
                  <th className="px-3 py-2 font-medium">Status</th>
                  <th className="px-3 py-2 text-right font-medium">Views</th>
                  <th className="px-3 py-2 text-right font-medium">Inquiries</th>
                </tr>
              </thead>
              <tbody>
                {stats.data.items.map((item) => (
                  <tr key={item.productId} className="border-t">
                    <td className="px-3 py-2">
                      <Link to={`/admin/products/${item.productId}`} className="font-medium hover:underline">
                        {item.name}
                      </Link>
                    </td>
                    <td className="px-3 py-2">
                      <ProductStatusBadge status={item.status} />
                    </td>
                    <td className="px-3 py-2 text-right tabular-nums">{item.views}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{item.inquiries}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination
            page={stats.data.page}
            pageSize={stats.data.pageSize}
            totalCount={stats.data.totalCount}
            hrefFor={(page) => `?${writeQuery({ ...query, page }).toString()}`}
          />
        </>
      )}
    </section>
  )
}
