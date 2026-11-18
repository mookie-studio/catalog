import { ProductCard } from '@/features/public/ProductCard'
import { useRelatedProducts } from '@/features/public/public-api'

/** Up to four Related products under a product. Nothing is shown while loading, on error or when there are none. */
export function RelatedProducts({ tenantSlug, productSlug }: { tenantSlug: string; productSlug: string }) {
  const related = useRelatedProducts(tenantSlug, productSlug)
  if (!related.data || related.data.items.length === 0) return null

  return (
    <section aria-labelledby="related-products" className="mx-auto max-w-5xl px-4 pb-10 sm:px-6">
      <h2 id="related-products" className="mb-4 text-lg font-semibold tracking-tight">
        Related products
      </h2>
      <ul aria-label="Related products" className="grid grid-cols-2 gap-4 md:grid-cols-4">
        {related.data.items.map((product) => (
          <li key={product.slug}>
            <ProductCard tenantSlug={tenantSlug} product={product} />
          </li>
        ))}
      </ul>
    </section>
  )
}
