import { type KeyboardEvent, useState } from 'react'
import { Input } from '@/components/ui/input'
import { type CatalogQuery, withAttribute } from '@/features/public/catalog-query'
import { type PublicFilter, usePublicFilters } from '@/features/public/public-api'

type Props = { tenantSlug: string; query: CatalogQuery; onChange: (query: CatalogQuery) => void }

/** One control per Filterable attribute; selected values are kept in the URL. */
export function FilterPanel({ tenantSlug, query, onChange }: Props) {
  const filters = usePublicFilters(tenantSlug)
  if (!filters.data || filters.data.length === 0) return null

  return (
    <section aria-label="Filters" className="flex flex-col gap-5">
      {filters.data.map((filter) => (
        <FilterControl
          key={filter.definitionId}
          filter={filter}
          values={query.attributes[filter.definitionId] ?? []}
          onChange={(values) => onChange(withAttribute(query, filter.definitionId, values))}
        />
      ))}
    </section>
  )
}

type ControlProps = { filter: PublicFilter; values: string[]; onChange: (values: string[]) => void }

function FilterControl({ filter, values, onChange }: ControlProps) {
  const id = `filter-${filter.definitionId}`
  switch (filter.type) {
    case 'OptionList':
      return (
        <fieldset className="space-y-1 text-sm">
          <legend className="mb-1 font-semibold">{filter.name}</legend>
          {(filter.options ?? []).map((option) => (
            <label key={option.id} className="flex items-center gap-2">
              <input
                type="checkbox"
                checked={values.includes(option.id)}
                onChange={(event) =>
                  onChange(event.target.checked ? [...values, option.id] : values.filter((v) => v !== option.id))
                }
              />
              {option.value}
            </label>
          ))}
        </fieldset>
      )
    case 'Boolean':
      return (
        <div className="space-y-1 text-sm">
          <label htmlFor={id} className="block font-semibold">
            {filter.name}
          </label>
          <select
            id={id}
            value={values[0] ?? ''}
            onChange={(event) => onChange(event.target.value ? [event.target.value] : [])}
            className="h-9 w-full rounded-md border bg-transparent px-2"
          >
            <option value="">Any</option>
            <option value="true">Yes</option>
            <option value="false">No</option>
          </select>
        </div>
      )
    default:
      return <ValueFilter id={id} filter={filter} value={values[0] ?? ''} onChange={onChange} />
  }
}

const inputTypes = { Text: 'text', Number: 'number', Date: 'date' } as const

/** Text, number and date filters apply when the visitor presses Enter or leaves the field. */
function ValueFilter({ id, filter, value, onChange }: { id: string; filter: PublicFilter; value: string; onChange: (values: string[]) => void }) {
  const [draft, setDraft] = useState(value)
  const [applied, setApplied] = useState(value)
  if (applied !== value) {
    setApplied(value)
    setDraft(value)
  }

  function commit() {
    const next = draft.trim()
    if (next !== value) onChange(next ? [next] : [])
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Enter') commit()
  }

  return (
    <div className="space-y-1 text-sm">
      <label htmlFor={id} className="block font-semibold">
        {filter.name}
      </label>
      <Input
        id={id}
        type={inputTypes[filter.type as keyof typeof inputTypes] ?? 'text'}
        step={filter.type === 'Number' ? 'any' : undefined}
        value={draft}
        onChange={(event) => setDraft(event.target.value)}
        onBlur={commit}
        onKeyDown={handleKeyDown}
      />
    </div>
  )
}
