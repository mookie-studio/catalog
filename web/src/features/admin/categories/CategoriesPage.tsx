import { useMutation, useQueryClient } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { api } from '@/api/client'
import { ApiProblem, call } from '@/api/problem'
import { Field } from '@/components/Field'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { type CategoryNode, categoriesQueryKey, flattenCategories, useCategories } from '@/features/admin/admin-api'

/** Ids of a Category and all its descendants: places it cannot be moved under. */
function subtreeIds(node: CategoryNode): string[] {
  return [node.id, ...node.children.flatMap(subtreeIds)]
}

function useRefreshTree() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: categoriesQueryKey })
}

export function CategoriesPage() {
  const categories = useCategories()

  return (
    <main className="flex max-w-3xl flex-col gap-6">
      <h1 className="text-2xl font-semibold tracking-tight">Categories</h1>
      <p className="text-sm text-muted-foreground">Categories are optional. Products can belong to several Categories or none.</p>
      <CreateCategoryForm parentId={null} label="New top-level Category" />
      {categories.isPending ? (
        <p role="status" className="text-muted-foreground">Loading…</p>
      ) : categories.isError ? (
        <p role="alert">The Categories could not be loaded.</p>
      ) : categories.data.length === 0 ? (
        <p className="text-muted-foreground">No Categories yet.</p>
      ) : (
        <ul aria-label="Category tree" className="flex flex-col gap-1">
          {categories.data.map((node, index) => (
            <CategoryItem key={node.id} node={node} parentId={null} index={index} siblingCount={categories.data.length} tree={categories.data} />
          ))}
        </ul>
      )}
    </main>
  )
}

function CreateCategoryForm({ parentId, label, onDone }: { parentId: string | null; label: string; onDone?: () => void }) {
  const refresh = useRefreshTree()
  const [name, setName] = useState('')
  const create = useMutation({
    mutationFn: () => call(api.POST('/api/admin/categories', { body: { name, parentId } })),
    onSuccess: () => {
      setName('')
      refresh()
      onDone?.()
    },
  })
  const error = create.error instanceof ApiProblem ? (create.error.fields.name ?? create.error.message) : undefined

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    create.mutate()
  }

  return (
    <form aria-label={label} className="flex items-start gap-2" onSubmit={handleSubmit}>
      <div className="flex-1">
        <Field label={label} error={error}>
          <Input value={name} onChange={(e) => setName(e.target.value)} required />
        </Field>
      </div>
      <Button type="submit" variant="outline" className="mt-6" disabled={create.isPending}>
        Add
      </Button>
    </form>
  )
}

type ItemProps = { node: CategoryNode; parentId: string | null; index: number; siblingCount: number; tree: CategoryNode[] }

function CategoryItem({ node, parentId, index, siblingCount, tree }: ItemProps) {
  const refresh = useRefreshTree()
  const [mode, setMode] = useState<'view' | 'rename' | 'add-child' | 'move'>('view')
  const [name, setName] = useState(node.name)
  const path = { params: { path: { id: node.id } } }

  const rename = useMutation({
    mutationFn: () => call(api.PUT('/api/admin/categories/{id}', { ...path, body: { name } })),
    onSuccess: () => {
      setMode('view')
      refresh()
    },
  })
  const move = useMutation({
    mutationFn: (target: { parentId: string | null; position: number }) =>
      call(api.POST('/api/admin/categories/{id}/move', { ...path, body: target })),
    onSuccess: () => {
      setMode('view')
      refresh()
    },
  })
  const remove = useMutation({
    mutationFn: () => call(api.DELETE('/api/admin/categories/{id}', path)),
    onSuccess: refresh,
  })

  const error = [rename.error, move.error, remove.error].find((e) => e)?.message
  const excluded = new Set(subtreeIds(node))
  const targets = flattenCategories(tree).filter((category) => !excluded.has(category.id))
  const childCount = (id: string | null) =>
    id === null ? tree.length : (flattenNodes(tree).find((n) => n.id === id)?.children.length ?? 0)

  function handleRename(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    rename.mutate()
  }

  function handleMove(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const value = String(new FormData(event.currentTarget).get('parent') ?? '')
    const target = value === '' ? null : value
    move.mutate({ parentId: target, position: childCount(target) })
  }

  return (
    <li aria-label={node.name}>
      <div className="flex flex-col gap-2 rounded-md border px-3 py-2">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <span className="font-medium">{node.name}</span>
          <div className="flex flex-wrap gap-1">
            <Button size="sm" variant="ghost" aria-label={`Move ${node.name} up`} disabled={index === 0 || move.isPending} onClick={() => move.mutate({ parentId, position: index - 1 })}>
              ↑
            </Button>
            <Button
              size="sm"
              variant="ghost"
              aria-label={`Move ${node.name} down`}
              disabled={index === siblingCount - 1 || move.isPending}
              onClick={() => move.mutate({ parentId, position: index + 1 })}
            >
              ↓
            </Button>
            <Button size="sm" variant="ghost" onClick={() => setMode(mode === 'rename' ? 'view' : 'rename')}>
              Rename
            </Button>
            <Button size="sm" variant="ghost" onClick={() => setMode(mode === 'add-child' ? 'view' : 'add-child')}>
              Add subcategory
            </Button>
            <Button size="sm" variant="ghost" onClick={() => setMode(mode === 'move' ? 'view' : 'move')}>
              Move
            </Button>
            <Button size="sm" variant="ghost" aria-label={`Delete ${node.name}`} disabled={remove.isPending} onClick={() => remove.mutate()}>
              Delete
            </Button>
          </div>
        </div>
        {mode === 'rename' && (
          <form aria-label={`Rename ${node.name}`} className="flex items-start gap-2" onSubmit={handleRename}>
            <div className="flex-1">
              <Field label="Name">
                <Input value={name} onChange={(e) => setName(e.target.value)} required />
              </Field>
            </div>
            <Button type="submit" size="sm" className="mt-6" disabled={rename.isPending}>
              Save
            </Button>
          </form>
        )}
        {mode === 'add-child' && <CreateCategoryForm parentId={node.id} label={`New subcategory of ${node.name}`} onDone={() => setMode('view')} />}
        {mode === 'move' && (
          <form aria-label={`Move ${node.name}`} className="flex items-start gap-2" onSubmit={handleMove}>
            <div className="flex-1">
              <Field label="New parent">
                <Select name="parent" defaultValue={parentId ?? ''}>
                  <option value="">Top level</option>
                  {targets.map((category) => (
                    <option key={category.id} value={category.id}>
                      {'  '.repeat(category.depth) + category.name}
                    </option>
                  ))}
                </Select>
              </Field>
            </div>
            <Button type="submit" size="sm" className="mt-6" disabled={move.isPending}>
              Move
            </Button>
          </form>
        )}
        {error && (
          <p role="alert" className="text-sm text-destructive">
            {error}
          </p>
        )}
      </div>
      {node.children.length > 0 && (
        <ul className="ml-4 mt-1 flex flex-col gap-1 border-l pl-3">
          {node.children.map((child, childIndex) => (
            <CategoryItem key={child.id} node={child} parentId={node.id} index={childIndex} siblingCount={node.children.length} tree={tree} />
          ))}
        </ul>
      )}
    </li>
  )
}

function flattenNodes(nodes: CategoryNode[]): CategoryNode[] {
  return nodes.flatMap((node) => [node, ...flattenNodes(node.children)])
}
