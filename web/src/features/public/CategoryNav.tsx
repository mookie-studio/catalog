import { cn } from '@/lib/utils'
import { type CatalogQuery, refine } from '@/features/public/catalog-query'
import { type CategoryNode, usePublicCategories } from '@/features/public/public-api'

type Props = { tenantSlug: string; query: CatalogQuery; onChange: (query: CatalogQuery) => void }

/** Category tree; hidden when the tenant has no Categories. */
export function CategoryNav({ tenantSlug, query, onChange }: Props) {
  const categories = usePublicCategories(tenantSlug)
  if (!categories.data || categories.data.length === 0) return null

  const select = (categoryId: string | null) => onChange(refine(query, { categoryId }))

  return (
    <nav aria-label="Categories">
      <h2 className="text-sm font-semibold">Categories</h2>
      <ul className="mt-2 space-y-1 text-sm">
        <li>
          <CategoryButton selected={query.categoryId === null} onClick={() => select(null)}>
            All products
          </CategoryButton>
        </li>
        <CategoryList nodes={categories.data} selectedId={query.categoryId} onSelect={select} />
      </ul>
    </nav>
  )
}

function CategoryList({ nodes, selectedId, onSelect }: { nodes: CategoryNode[]; selectedId: string | null; onSelect: (id: string) => void }) {
  return nodes.map((node) => (
    <li key={node.id}>
      <CategoryButton selected={node.id === selectedId} onClick={() => onSelect(node.id)}>
        {node.name}
      </CategoryButton>
      {node.children.length > 0 && (
        <ul className="ml-3 mt-1 space-y-1 border-l pl-2">
          <CategoryList nodes={node.children} selectedId={selectedId} onSelect={onSelect} />
        </ul>
      )}
    </li>
  ))
}

function CategoryButton({ selected, onClick, children }: { selected: boolean; onClick: () => void; children: string }) {
  return (
    <button
      type="button"
      aria-current={selected ? 'true' : undefined}
      onClick={onClick}
      className={cn('w-full rounded px-2 py-1 text-left hover:bg-accent', selected && 'bg-accent font-medium')}
    >
      {children}
    </button>
  )
}
