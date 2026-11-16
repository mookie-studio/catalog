import { cn } from '@/lib/utils'

// Served from public/ so it is cached once for every product without images.
const placeholder = '/product-placeholder.svg'

/** A product image, or the default placeholder when the product has none. */
export function ProductImage({ src, alt, className }: { src: string | null | undefined; alt: string; className?: string }) {
  return (
    <img
      src={src ?? placeholder}
      alt={alt}
      loading="lazy"
      className={cn('aspect-square w-full rounded-lg border bg-muted object-cover', className)}
    />
  )
}
