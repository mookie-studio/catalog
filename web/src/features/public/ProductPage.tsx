import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router'
import { NotFoundPage } from '@/components/NotFoundPage'
import { cn } from '@/lib/utils'
import { ProductImage } from '@/features/public/ProductImage'
import { formatAttribute } from '@/features/public/format-attribute'
import { RelatedProducts } from '@/features/public/RelatedProducts'
import { toMetaDescription } from '@/features/public/meta'
import { useRecordProductView } from '@/features/public/use-record-product-view'
import { InquiryForm } from '@/features/public/InquiryForm'
import { NotFoundError, publicProductKey, usePublicProduct, usePublicTenant } from '@/features/public/public-api'

export function ProductPage({ tenantSlug }: { tenantSlug: string }) {
  const productSlug = useParams().productSlug ?? ''
  const product = usePublicProduct(tenantSlug, productSlug)
  const tenant = usePublicTenant(tenantSlug)
  useRecordProductView(tenantSlug, product.data?.slug)
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const { search } = useLocation()

  // An Old Product slug still loads the product (ADR 0009); replace the address with the current slug,
  // seeding its cache so the page does not flash a loading state.
  useEffect(() => {
    const current = product.data
    if (current && current.slug !== productSlug) {
      queryClient.setQueryData(publicProductKey(tenantSlug, current.slug), current)
      navigate(`/${tenantSlug}/p/${current.slug}${search}`, { replace: true })
    }
  }, [product.data, productSlug, tenantSlug, search, navigate, queryClient])

  if (product.error instanceof NotFoundError) return <NotFoundPage />
  if (product.isError) return <p role="alert" className="p-6">The product could not be loaded.</p>
  if (product.isPending) return <p role="status" className="p-6 text-muted-foreground">Loading…</p>

  const { name, description, images, attributes } = product.data
  const metaDescription = toMetaDescription(description) ?? toMetaDescription(tenant.data?.description)
  const mainImage = images[0]?.url
  const title = tenant.data ? `${name} · ${tenant.data.name}` : name
  return (
    <>
      <title>{title}</title>
      {metaDescription && <meta name="description" content={metaDescription} />}
      <meta property="og:title" content={title} />
      {metaDescription && <meta property="og:description" content={metaDescription} />}
      <meta property="og:type" content="product" />
      {mainImage && <meta property="og:image" content={mainImage} />}
      <link rel="canonical" href={`${window.location.origin}/${tenantSlug}/p/${product.data.slug}`} />
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
          {tenant.data?.inquiriesEnabled && <InquiryForm key={productSlug} tenantSlug={tenantSlug} productSlug={productSlug} productName={name} />}
        </div>
      </main>
      <RelatedProducts tenantSlug={tenantSlug} productSlug={product.data.slug} />
    </>
  )
}

type Image = { url: string; thumbnailUrl: string }

/** Images in their order; the first one is the Main image. The strip shows Thumbnails, the large view the original. */
function Gallery({ name, images }: { name: string; images: Image[] }) {
  const [selected, setSelected] = useState(0)
  if (images.length === 0) return <ProductImage src={null} alt={name} />

  return (
    <div className="flex flex-col gap-3">
      <ProductImage src={images[selected].url} alt={name} />
      {images.length > 1 && (
        <ul aria-label="Images" className="grid grid-cols-5 gap-2">
          {images.map((image, index) => (
            <li key={image.url}>
              <button
                type="button"
                aria-label={`Show image ${index + 1}`}
                aria-current={index === selected ? 'true' : undefined}
                onClick={() => setSelected(index)}
                className={cn('block w-full rounded-lg', index === selected && 'ring-2 ring-ring')}
              >
                <ProductImage src={image.thumbnailUrl} alt="" />
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
