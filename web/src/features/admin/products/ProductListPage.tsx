import { EmptyState } from '@/components/EmptyState'
import { type FormEvent, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { Pagination } from '@/components/Pagination'
import { Button, ButtonLink } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { flattenCategories, useCategories } from '@/features/admin/admin-api'
import {
  type ProductListQuery,
  type ProductStatus,
  readProductListQuery,
  useProducts,
  writeProductListQuery,
} from '@/features/admin/products/products-api'
import { BulkActionBar, BulkSummary } from '@/features/admin/products/BulkActionBar'
import type { BulkResult } from '@/features/admin/products/bulk-api'
import { useSelection } from '@/features/admin/products/use-selection'
import { downloadProductsCsv } from '@/features/admin/products/product-csv'
import { ProductStatusBadge } from '@/features/admin/products/ProductStatusBadge'
import { StatusBadge } from '@/components/StatusBadge'

const dateFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' })

export function ProductListPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const query = useMemo(() => readProductListQuery(searchParams), [searchParams])
  const products = useProducts(query)
  const categories = useCategories()
  const selection = useSelection(searchParams.toString())
  const [result, setResult] = useState<BulkResult | null>(null)
  const [exporting, setExporting] = useState(false)
  const [exportError, setExportError] = useState<string | null>(null)

  async function exportCsv() {
    setExporting(true)
    setExportError(null)
    try {
      await downloadProductsCsv()
    } catch (error) {
      setExportError(error instanceof Error ? error.message : 'The export failed.')
    } finally {
      setExporting(false)
    }
  }

  const pageItems = products.data?.items ?? []
  const pageIds = pageItems.map((product) => product.id)
  const pageState = selection.stateOf(pageIds)
  const names = Object.fromEntries(pageItems.map((product) => [product.id, product.name]))

  const filtered = query.q !== '' || query.status !== null || query.categoryId !== null || query.featured ||
    query.hasImages !== null || query.hasCategories !== null

  // Changing the search or a filter goes back to the first page.
  const refine = (change: Partial<ProductListQuery>) => setSearchParams(writeProductListQuery({ ...query, ...change, page: 1 }))

  function handleSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    refine({ q: String(new FormData(event.currentTarget).get('q') ?? '').trim() })
  }

  return (
    <main className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-4">
        <h1 className="display-m">Products</h1>
        <div className="flex flex-wrap gap-2">
          <Button variant="outline" disabled={exporting} onClick={exportCsv}>
            Export CSV
          </Button>
          <ButtonLink variant="outline" to="/admin/products/import">
            Import CSV
          </ButtonLink>
          <ButtonLink to="/admin/products/new">New product</ButtonLink>
        </div>
      </div>
      {exportError && (
        <p role="alert" className="text-sm text-destructive">
          {exportError}
        </p>
      )}
      <div className="flex flex-col gap-3 md:flex-row">
        <form role="search" className="flex flex-1 gap-2" onSubmit={handleSearch}>
          <Input key={query.q} name="q" type="search" defaultValue={query.q} placeholder="Search by name or SKU" aria-label="Search products" />
          <Button type="submit" variant="outline">
            Search
          </Button>
        </form>
        <Select
          aria-label="Status"
          className="md:w-40"
          value={query.status ?? ''}
          onChange={(event) => refine({ status: (event.target.value || null) as ProductStatus | null })}
        >
          <option value="">All statuses</option>
          <option value="Draft">Draft</option>
          <option value="Published">Published</option>
          <option value="Archived">Archived</option>
        </Select>
        <Select
          aria-label="Category"
          className="md:w-52"
          value={query.categoryId ?? ''}
          onChange={(event) => refine({ categoryId: event.target.value || null })}
        >
          <option value="">All categories</option>
          {flattenCategories(categories.data ?? []).map((category) => (
            <option key={category.id} value={category.id}>
              {'  '.repeat(category.depth) + category.name}
            </option>
          ))}
        </Select>
        <Select
          aria-label="Images"
          className="md:w-40"
          value={query.hasImages === null ? '' : String(query.hasImages)}
          onChange={(event) => refine({ hasImages: event.target.value === '' ? null : event.target.value === 'true' })}
        >
          <option value="">Any images</option>
          <option value="true">With images</option>
          <option value="false">Without images</option>
        </Select>
        <Select
          aria-label="Has categories"
          className="md:w-44"
          value={query.hasCategories === null ? '' : String(query.hasCategories)}
          onChange={(event) => refine({ hasCategories: event.target.value === '' ? null : event.target.value === 'true' })}
        >
          <option value="">Any categories</option>
          <option value="true">With categories</option>
          <option value="false">Without categories</option>
        </Select>
        <label className="flex items-center gap-2 whitespace-nowrap text-sm">
          <input type="checkbox" className="size-4" checked={query.featured} onChange={(event) => refine({ featured: event.target.checked })} />
          Featured only
        </label>
      </div>
      {selection.selected.size > 0 && (
        <BulkActionBar selectedIds={[...selection.selected]} onClear={selection.clear} onResult={(r) => setResult({ ...r })} />
      )}
      {result && <BulkSummary result={result} names={names} onDismiss={() => setResult(null)} />}
      {products.isPending ? (
        <p role="status" className="text-muted-foreground">Loading…</p>
      ) : products.isError ? (
        <p role="alert">The products could not be loaded.</p>
      ) : products.data.items.length === 0 ? (
        filtered ? (
          <div className="rounded-l border border-dashed p-10 text-center">
            <p className="text-muted-foreground">No products match these filters.</p>
          </div>
        ) : (
          <EmptyState
            title="No products yet"
            action={
              <ButtonLink to="/admin/products/new">New product</ButtonLink>
            }
          >
            Create your first product to start your catalog.
          </EmptyState>
        )
      ) : (
        <>
          <div className="overflow-x-auto rounded-lg border">
            <table className="w-full text-sm">
              <thead className="bg-muted/50 text-left text-muted-foreground">
                <tr>
                  <th className="w-10 px-3 py-2">
                    <input
                      type="checkbox"
                      className="size-4"
                      aria-label="Select all on this page"
                      checked={pageState === 'all'}
                      ref={(input) => {
                        if (input) input.indeterminate = pageState === 'some'
                      }}
                      onChange={() => selection.togglePage(pageIds)}
                    />
                  </th>
                  <th className="px-3 py-2 font-medium">Name</th>
                  <th className="px-3 py-2 font-medium">SKU</th>
                  <th className="px-3 py-2 font-medium">Status</th>
                  <th className="px-3 py-2 font-medium">Last update</th>
                </tr>
              </thead>
              <tbody>
                {products.data.items.map((product) => (
                  <tr key={product.id} className="border-t">
                    <td className="px-3 py-2">
                      <input
                        type="checkbox"
                        className="size-4"
                        aria-label={`Select ${product.name}`}
                        checked={selection.isSelected(product.id)}
                        onChange={() => selection.toggle(product.id)}
                      />
                    </td>
                    <td className="px-3 py-2">
                      <Link to={`/admin/products/${product.id}`} className="font-medium hover:underline">
                        {product.name}
                      </Link>
                      {product.isFeatured && (
                        <StatusBadge status="Featured" className="ml-2" />
                      )}
                    </td>
                    <td className="px-3 py-2 font-mono text-xs">{product.sku}</td>
                    <td className="px-3 py-2">
                      <ProductStatusBadge status={product.status} />
                    </td>
                    <td className="whitespace-nowrap px-3 py-2 text-muted-foreground">{dateFormat.format(new Date(product.updatedAt))}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination
            page={products.data.page}
            pageSize={products.data.pageSize}
            totalCount={products.data.totalCount}
            hrefFor={(page) => `?${writeProductListQuery({ ...query, page }).toString()}`}
          />
        </>
      )}
    </main>
  )
}
