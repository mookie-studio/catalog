import { useState } from 'react'
import { useParams } from 'react-router'
import { NotFoundPage } from '@/components/NotFoundPage'
import { cn } from '@/lib/utils'
import { ProductImage } from '@/features/public/ProductImage'
import { formatAttribute } from '@/features/public/format-attribute'
import { NotFoundError, usePublicProduct } from '@/features/public/public-api'

export function ProductPage({ tenantSlug }: { tenantSlug: string }) {
  const productSlug = useParams().productSlug ?? ''
  const product = usePublicProduct(tenantSlug, productSlug)

  if (product.error instanceof NotFoundError) return <NotFoundPage />
  if (product.isError) return <p role="alert" className="p-6">The product could not be loaded.</p>
  if (product.isPending) return <p role="status" className="p-6 text-muted-foreground">Loading…</p>

  const { name, description, images, attributes } = product.data
  return (
    <main className="mx-auto grid max-w-5xl gap-8 px-4 py-6 sm:px-6 md:grid-cols-2">
      <Gallery key={productSlug} name={name} images={images} />
      <div className="flex flex-col gap-6">
        <h1 className="text-2xl font-semibold tracking-tight">{name}</h1>
        {description && <p className="whitespace-pre-line text-muted-foreground">{description}</p>}
        {attributes.length > 0 && (
          <dl className="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2 text-sm">
            {attributes.map((attribute) => (
              <div key={attribute.name} className="contents">
                <dt className="font-medium">{attribute.name}</dt>
                <dd className="text-muted-foreground">{formatAttribute(attribute)}</dd>
              </div>
            ))}
          </dl>
        )}
      </div>
    </main>
  )
}

/** Images in their order; the first one is the Main image. */
function Gallery({ name, images }: { name: string; images: string[] }) {
  const [selected, setSelected] = useState(0)
  if (images.length === 0) return <ProductImage src={null} alt={name} />

  return (
    <div className="flex flex-col gap-3">
      <ProductImage src={images[selected]} alt={name} />
      {images.length > 1 && (
        <ul aria-label="Images" className="grid grid-cols-5 gap-2">
          {images.map((url, index) => (
            <li key={url}>
              <button
                type="button"
                aria-label={`Show image ${index + 1}`}
                aria-current={index === selected ? 'true' : undefined}
                onClick={() => setSelected(index)}
                className={cn('block w-full rounded-lg', index === selected && 'ring-2 ring-ring')}
              >
                <ProductImage src={url} alt="" />
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
