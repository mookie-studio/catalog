import { useMutation, useQueryClient } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { useNavigate } from 'react-router'
import { api } from '@/api/client'
import { ApiProblem, call } from '@/api/problem'
import { Button, ButtonLink } from '@/components/ui/button'
import { ProductFields, type ProductFieldValues } from '@/features/admin/products/ProductFields'
import { productQueryKey } from '@/features/admin/products/products-api'

export function NewProductPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [values, setValues] = useState<ProductFieldValues>({ name: '', sku: '', description: '', slug: '' })
  const create = useMutation({
    mutationFn: () =>
      call(api.POST('/api/admin/products', { body: { name: values.name, sku: values.sku, description: values.description || null } })),
    onSuccess: (product) => {
      queryClient.setQueryData(productQueryKey(product.id), product)
      queryClient.invalidateQueries({ queryKey: ['admin', 'products'] })
      navigate(`/admin/products/${product.id}`)
    },
  })
  const problem = create.error instanceof ApiProblem ? create.error : null

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    create.mutate()
  }

  return (
    <main className="flex max-w-3xl flex-col gap-6">
      <h1 className="text-2xl font-semibold tracking-tight">New product</h1>
      <p className="text-sm text-muted-foreground">The product is created as a Draft. You can complete and publish it afterwards.</p>
      <form className="flex flex-col gap-6" onSubmit={handleSubmit}>
        <ProductFields values={values} errors={problem?.fields ?? {}} showSlug={false} onChange={setValues} />
        {create.isError && Object.keys(problem?.fields ?? {}).length === 0 && (
          <p role="alert" className="text-sm text-destructive">
            {create.error.message}
          </p>
        )}
        <div className="flex gap-2">
          <Button type="submit" disabled={create.isPending}>
            Create product
          </Button>
          <ButtonLink to="/admin/products" variant="ghost">
            Cancel
          </ButtonLink>
        </div>
      </form>
    </main>
  )
}
