import { Link } from 'react-router'
import { ProductImage } from '@/features/public/ProductImage'
import type { PublicProductItem } from '@/features/public/public-api'

export function FeaturedBadge() {
  return <span className="rounded bg-primary px-1.5 py-0.5 text-xs font-medium text-primary-foreground">Featured</span>
}

export function ProductCard({ tenantSlug, product }: { tenantSlug: string; product: PublicProductItem }) {
  return (
    <Link to={`/${tenantSlug}/p/${product.slug}`} className="group flex flex-col gap-2">
      <ProductImage src={product.mainImageUrl} alt={product.name} />
      <span className="text-sm font-medium group-hover:underline">{product.name}</span>
    </Link>
  )
}
