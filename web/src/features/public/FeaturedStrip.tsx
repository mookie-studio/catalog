import { ProductCard } from '@/features/public/ProductCard'
import { useFeaturedProducts } from '@/features/public/public-api'

/** Up to 8 Featured products above the list on the catalog home; hidden when there are none. */
export function FeaturedStrip({ tenantSlug }: { tenantSlug: string }) {
  const featured = useFeaturedProducts(tenantSlug)
  if (!featured.data || featured.data.items.length === 0) return null

  return (
    <section aria-label="Featured" className="flex flex-col gap-3">
      <h2 className="text-lg font-semibold">Featured</h2>
      <ul className="grid grid-cols-2 gap-4 sm:grid-cols-4">
        {featured.data.items.map((product) => (
          <li key={product.slug}>
            <ProductCard tenantSlug={tenantSlug} product={product} />
          </li>
        ))}
      </ul>
    </section>
  )
}
