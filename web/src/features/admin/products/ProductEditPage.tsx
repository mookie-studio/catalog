import { useMutation, useQueryClient } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router'
import { api } from '@/api/client'
import { ApiProblem, call, isNotFound } from '@/api/problem'
import { NotFoundPage } from '@/components/NotFoundPage'
import { Button } from '@/components/ui/button'
import { ProductAttributesEditor } from '@/features/admin/products/ProductAttributesEditor'
import { ProductCategoriesEditor } from '@/features/admin/products/ProductCategoriesEditor'
import { ProductImagesEditor } from '@/features/admin/products/ProductImagesEditor'
import { ProductFields, type ProductFieldValues } from '@/features/admin/products/ProductFields'
import {
  type Product,
  type ProductStatus,
  productQueryKey,
  useProduct,
} from '@/features/admin/products/products-api'
import { ProductStatusBadge } from '@/features/admin/products/ProductStatusBadge'

export function ProductEditPage() {
  const id = useParams().id ?? ''
  const product = useProduct(id)

  if (isNotFound(product.error)) return <NotFoundPage />
  if (product.isError) return <p role="alert">The product could not be loaded.</p>
  if (product.isPending) return <p role="status" className="text-muted-foreground">Loading…</p>

  return <ProductEditor key={product.data.id} product={product.data} />
}

/** Status actions offered for each Product status. */
const statusActions: Record<ProductStatus, { label: string; status: ProductStatus }[]> = {
  Draft: [
    { label: 'Publish', status: 'Published' },
    { label: 'Archive', status: 'Archived' },
  ],
  Published: [
    { label: 'Move to draft', status: 'Draft' },
    { label: 'Archive', status: 'Archived' },
  ],
  Archived: [{ label: 'Restore', status: 'Draft' }],
}

function toValues(product: Product): ProductFieldValues {
  return { name: product.name, sku: product.sku, description: product.description ?? '', slug: product.slug }
}

/** Message shown once on the edit page after navigating to it, for example after duplicating. */
type EditPageState = { notice?: string } | null

function ProductEditor({ product }: { product: Product }) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const notice = (useLocation().state as EditPageState)?.notice
  const [values, setValues] = useState(() => toValues(product))
  const [saved, setSaved] = useState(false)
  const path = { params: { path: { id: product.id } } }

  const save = useMutation({
    mutationFn: () =>
      call(
        api.PUT('/api/admin/products/{id}', {
          ...path,
          body: { name: values.name, sku: values.sku, slug: values.slug, description: values.description || null },
        }),
      ),
    onSuccess: (updated) => {
      queryClient.setQueryData(productQueryKey(product.id), updated)
      queryClient.invalidateQueries({ queryKey: ['admin', 'products'] })
      setValues(toValues(updated))
      setSaved(true)
    },
  })

  const changeStatus = useMutation({
    mutationFn: (status: ProductStatus) => call(api.POST('/api/admin/products/{id}/status', { ...path, body: { status } })),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productQueryKey(product.id) })
      queryClient.invalidateQueries({ queryKey: ['admin', 'products'] })
    },
  })

  const feature = useMutation({
    mutationFn: (featured: boolean) => call(api.PUT('/api/admin/products/{id}/featured', { ...path, body: { featured } })),
    onSuccess: (updated) => {
      queryClient.setQueryData(productQueryKey(product.id), updated)
      queryClient.invalidateQueries({ queryKey: ['admin', 'products'] })
    },
  })

  const duplicate = useMutation({
    mutationFn: () => call(api.POST('/api/admin/products/{id}/duplicate', path)),
    onSuccess: (copy) => {
      queryClient.setQueryData(productQueryKey(copy.id), copy)
      queryClient.invalidateQueries({ queryKey: ['admin', 'products'] })
      navigate(`/admin/products/${copy.id}`, { state: { notice: 'Copy created as Draft' } satisfies EditPageState })
    },
  })

  // Errors of the last action, shown next to their fields (for example the description required to publish).
  const lastError = changeStatus.error ?? save.error ?? duplicate.error
  const problem = lastError instanceof ApiProblem ? lastError : null
  const fieldErrors = problem?.fields ?? {}
  const generalError = lastError && Object.keys(fieldErrors).length === 0 ? lastError.message : null

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSaved(false)
    changeStatus.reset()
    save.mutate()
  }

  function handleStatus(status: ProductStatus) {
    setSaved(false)
    save.reset()
    changeStatus.mutate(status)
  }

  return (
    <main className="flex max-w-3xl flex-col gap-6">
      <div className="flex flex-col gap-1">
        <Link to="/admin/products" className="text-sm text-muted-foreground hover:underline">
          ← Products
        </Link>
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="display-m">{product.name}</h1>
          <ProductStatusBadge status={product.status} />
          <Button variant="outline" size="sm" className="ml-auto" disabled={duplicate.isPending} onClick={() => duplicate.mutate()}>
            Duplicate
          </Button>
        </div>
        {notice && (
          <p role="status" className="text-sm text-muted-foreground">
            {notice}
          </p>
        )}
      </div>
      <section aria-label="Status" className="flex flex-wrap gap-2">
        {statusActions[product.status].map((action) => (
          <Button key={action.status} variant="outline" size="sm" disabled={changeStatus.isPending} onClick={() => handleStatus(action.status)}>
            {action.label}
          </Button>
        ))}
        <label className="ml-auto flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            className="size-4"
            checked={product.isFeatured}
            disabled={feature.isPending}
            onChange={(event) => feature.mutate(event.target.checked)}
          />
          Featured
        </label>
      </section>
      <form className="flex flex-col gap-6" onSubmit={handleSubmit}>
        <ProductFields values={values} errors={fieldErrors} showSlug onChange={setValues} />
        {generalError && (
          <p role="alert" className="text-sm text-destructive">
            {generalError}
          </p>
        )}
        <div className="flex items-center gap-3">
          <Button type="submit" disabled={save.isPending}>
            Save
          </Button>
          {saved && (
            <span role="status" className="text-sm text-muted-foreground">
              Saved
            </span>
          )}
        </div>
      </form>
      <ProductImagesEditor product={product} />
      <ProductAttributesEditor product={product} />
      <ProductCategoriesEditor product={product} />
    </main>
  )
}
