import { type FormEvent, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { Pagination } from '@/components/Pagination'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { CategoryNav } from '@/features/public/CategoryNav'
import { type CatalogQuery, readCatalogQuery, refine, writeCatalogQuery } from '@/features/public/catalog-query'
import { FilterPanel } from '@/features/public/FilterPanel'
import { ProductImage } from '@/features/public/ProductImage'
import { usePublicProducts } from '@/features/public/public-api'
import { cn } from '@/lib/utils'

export function CatalogPage({ tenantSlug }: { tenantSlug: string }) {
  const [searchParams, setSearchParams] = useSearchParams()
  const query = useMemo(() => readCatalogQuery(searchParams), [searchParams])
  const products = usePublicProducts(tenantSlug, query)
  const [showRefinements, setShowRefinements] = useState(false)

  function navigate(next: CatalogQuery) {
    setSearchParams(writeCatalogQuery(next))
  }

  function handleSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const q = String(new FormData(event.currentTarget).get('q') ?? '').trim()
    navigate(refine(query, { q }))
  }

  return (
    <div className="mx-auto flex max-w-6xl flex-col gap-6 px-4 py-6 sm:px-6 md:flex-row">
      <aside className="shrink-0 md:w-60">
        {/* Categories and filters are a sidebar on wide screens and collapsed on small ones. */}
        <Button
          variant="outline"
          className="w-full md:hidden"
          aria-expanded={showRefinements}
          aria-controls="catalog-refinements"
          onClick={() => setShowRefinements((shown) => !shown)}
        >
          {showRefinements ? 'Hide categories and filters' : 'Show categories and filters'}
        </Button>
        <div id="catalog-refinements" className={cn('mt-4 flex-col gap-6 md:mt-0 md:flex', showRefinements ? 'flex' : 'hidden')}>
          <CategoryNav tenantSlug={tenantSlug} query={query} onChange={navigate} />
          <FilterPanel tenantSlug={tenantSlug} query={query} onChange={navigate} />
        </div>
      </aside>
      <main className="flex min-w-0 flex-1 flex-col gap-6">
        <form role="search" className="flex gap-2" onSubmit={handleSearch}>
          <Input key={query.q} name="q" type="search" defaultValue={query.q} placeholder="Search products" aria-label="Search products" />
          <Button type="submit">Search</Button>
        </form>
        {products.isPending ? (
          <p role="status" className="text-muted-foreground">Loading…</p>
        ) : products.isError ? (
          <p role="alert">The products could not be loaded.</p>
        ) : products.data.items.length === 0 ? (
          <p className="text-muted-foreground">No products found.</p>
        ) : (
          <>
            <ul className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
              {products.data.items.map((product) => (
                <li key={product.slug}>
                  <Link to={`/${tenantSlug}/p/${product.slug}`} className="group flex flex-col gap-2">
                    <ProductImage src={product.mainImageUrl} alt={product.name} />
                    <span className="text-sm font-medium group-hover:underline">{product.name}</span>
                  </Link>
                </li>
              ))}
            </ul>
            <Pagination
              page={products.data.page}
              pageSize={products.data.pageSize}
              totalCount={products.data.totalCount}
              hrefFor={(page) => `?${writeCatalogQuery({ ...query, page }).toString()}`}
            />
          </>
        )}
      </main>
    </div>
  )
}
