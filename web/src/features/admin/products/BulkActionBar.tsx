import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Select } from '@/components/ui/select'
import { flattenCategories, useCategories } from '@/features/admin/admin-api'
import { type BulkAction, type BulkResult, useBulkAction } from '@/features/admin/products/bulk-api'

type Props = {
  selectedIds: string[]
  onClear: () => void
  onResult: (result: BulkResult) => void
}

const actions: { action: BulkAction; label: string }[] = [
  { action: 'Publish', label: 'Publish' },
  { action: 'Archive', label: 'Archive' },
  { action: 'MoveToDraft', label: 'Move to draft' },
  { action: 'Feature', label: 'Mark as Featured' },
  { action: 'Unfeature', label: 'Unmark as Featured' },
]

/** The bar shown over the product list while products are selected: one Bulk action for all of them. */
export function BulkActionBar({ selectedIds, onClear, onResult }: Props) {
  const bulk = useBulkAction()
  const categories = useCategories()
  const [categoryAction, setCategoryAction] = useState<'AddToCategory' | 'RemoveFromCategory' | null>(null)
  const [categoryId, setCategoryId] = useState('')
  const hasCategories = (categories.data ?? []).length > 0

  function run(action: BulkAction, category?: string) {
    bulk.mutate(
      { productIds: selectedIds, action, categoryId: category },
      {
        onSuccess: (result) => {
          setCategoryAction(null)
          setCategoryId('')
          onClear()
          onResult(result)
        },
      },
    )
  }

  return (
    <section aria-label="Bulk actions" className="flex flex-col gap-3 rounded-lg border bg-muted/40 p-3">
      <div className="flex flex-wrap items-center gap-2">
        <span className="mr-2 text-sm font-medium">{selectedIds.length} selected</span>
        {actions.map(({ action, label }) => (
          <Button key={action} variant="outline" size="sm" disabled={bulk.isPending} onClick={() => run(action)}>
            {label}
          </Button>
        ))}
        {hasCategories && (
          <>
            <Button variant="outline" size="sm" disabled={bulk.isPending} onClick={() => setCategoryAction('AddToCategory')}>
              Add to category…
            </Button>
            <Button variant="outline" size="sm" disabled={bulk.isPending} onClick={() => setCategoryAction('RemoveFromCategory')}>
              Remove from category…
            </Button>
          </>
        )}
        <Button variant="ghost" size="sm" onClick={onClear}>
          Clear selection
        </Button>
      </div>
      {categoryAction && (
        <form
          className="flex flex-wrap items-center gap-2"
          onSubmit={(event) => {
            event.preventDefault()
            run(categoryAction, categoryId)
          }}
        >
          <Select aria-label="Category to apply" className="w-56" value={categoryId} onChange={(event) => setCategoryId(event.target.value)}>
            <option value="">Choose a category</option>
            {flattenCategories(categories.data ?? []).map((category) => (
              <option key={category.id} value={category.id}>
                {'  '.repeat(category.depth) + category.name}
              </option>
            ))}
          </Select>
          <Button type="submit" size="sm" disabled={bulk.isPending || categoryId === ''}>
            {categoryAction === 'AddToCategory' ? 'Add' : 'Remove'}
          </Button>
          <Button type="button" variant="ghost" size="sm" onClick={() => setCategoryAction(null)}>
            Cancel
          </Button>
        </form>
      )}
      {bulk.isError && (
        <p role="alert" className="text-sm text-destructive">
          {bulk.error.message}
        </p>
      )}
    </section>
  )
}

/** What a Bulk action did: how many products changed and, for the others, why not. */
export function BulkSummary({ result, names, onDismiss }: { result: BulkResult; names: Record<string, string>; onDismiss: () => void }) {
  return (
    <div role="status" className="flex flex-col gap-2 rounded-lg border p-3 text-sm">
      <div className="flex items-center justify-between gap-3">
        <p className="font-medium">
          {result.succeeded.length} updated{result.failed.length > 0 && `, ${result.failed.length} failed`}
        </p>
        <Button variant="ghost" size="sm" onClick={onDismiss}>
          Dismiss
        </Button>
      </div>
      {result.failed.length > 0 && (
        <ul className="list-disc pl-5 text-destructive">
          {result.failed.map((failure) => (
            <li key={failure.productId}>
              {names[failure.productId] ?? 'Unknown product'}: {Object.values(failure.errors).flat().join(' ')}
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
