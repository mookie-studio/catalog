import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link } from 'react-router'
import { api } from '@/api/client'
import { call } from '@/api/problem'
import { Button } from '@/components/ui/button'
import { flattenCategories, useCategories } from '@/features/admin/admin-api'
import { type Product, productQueryKey } from '@/features/admin/products/products-api'

/** Zero or more Categories of the Product, picked from the tree. */
export function ProductCategoriesEditor({ product }: { product: Product }) {
  const categories = useCategories()
  const queryClient = useQueryClient()
  const [selected, setSelected] = useState<string[]>(product.categoryIds)
  const [saved, setSaved] = useState(false)
  const save = useMutation({
    mutationFn: () =>
      call(api.PUT('/api/admin/products/{id}/categories', { params: { path: { id: product.id } }, body: { categoryIds: selected } })),
    onSuccess: (updated) => {
      queryClient.setQueryData(productQueryKey(product.id), updated)
      queryClient.invalidateQueries({ queryKey: ['admin', 'products'] })
      setSaved(true)
    },
  })

  if (categories.isPending) return null
  if (categories.isError) return <p role="alert">The Categories could not be loaded.</p>

  const toggle = (id: string, checked: boolean) => {
    setSaved(false)
    setSelected((current) => (checked ? [...current, id] : current.filter((c) => c !== id)))
  }

  return (
    <section aria-label="Categories" className="flex flex-col gap-4 border-t pt-6">
      <h2 className="text-lg font-semibold">Categories</h2>
      {categories.data.length === 0 ? (
        <p className="text-sm text-muted-foreground">
          This Tenant has no Categories. <Link to="/admin/categories" className="underline">Create one</Link> if you need them.
        </p>
      ) : (
        <>
          <ul className="flex flex-col gap-1 text-sm">
            {flattenCategories(categories.data).map((category) => (
              <li key={category.id} style={{ paddingLeft: `${category.depth * 1.25}rem` }}>
                <label className="flex items-center gap-2">
                  <input type="checkbox" checked={selected.includes(category.id)} onChange={(e) => toggle(category.id, e.target.checked)} />
                  {category.name}
                </label>
              </li>
            ))}
          </ul>
          {save.isError && (
            <p role="alert" className="text-sm text-destructive">
              {save.error.message}
            </p>
          )}
          <div className="flex items-center gap-3">
            <Button disabled={save.isPending} onClick={() => save.mutate()}>
              Save categories
            </Button>
            {saved && (
              <span role="status" className="text-sm text-muted-foreground">
                Categories saved
              </span>
            )}
          </div>
        </>
      )}
    </section>
  )
}
