import { useMutation, useQueryClient } from '@tanstack/react-query'
import { type FormEvent, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router'
import { api } from '@/api/client'
import { ApiProblem, call, hasFieldErrors } from '@/api/problem'
import { ConfirmPanel } from '@/components/ConfirmPanel'
import { Field } from '@/components/Field'
import { Pagination } from '@/components/Pagination'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import {
  type AttributeDefinition,
  type AttributeType,
  attributeDefinitionsQueryKey,
  attributeTypes,
  useAttributeDefinitionsPage,
} from '@/features/admin/attributes/attributes-api'
import { useConfirmedDelete } from '@/features/admin/attributes/useConfirmedDelete'
import { pageParam, pageSearch } from '@/lib/page-param'

const typeLabel = (type: AttributeType) => attributeTypes.find((t) => t.value === type)?.label ?? type
const usedBy = (count: number) => `${count} ${count === 1 ? 'product uses' : 'products use'} it. Deleting it removes those values from them.`

export function AttributesPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const page = pageParam(searchParams)
  const definitions = useAttributeDefinitionsPage(page)

  // After deleting the last definition of a page, go back one page.
  const emptyPage = definitions.data?.items.length === 0 && page > 1
  useEffect(() => {
    if (emptyPage) setSearchParams(pageSearch(page - 1), { replace: true })
  }, [emptyPage, page, setSearchParams])

  return (
    <main className="flex max-w-3xl flex-col gap-6">
      <h1 className="text-2xl font-semibold tracking-tight">Attributes</h1>
      <p className="text-sm text-muted-foreground">
        Attribute Definitions describe what matters in your business. Any product can fill in the ones that apply.
      </p>
      <CreateDefinitionForm />
      {definitions.isPending ? (
        <p role="status" className="text-muted-foreground">Loading…</p>
      ) : definitions.isError ? (
        <p role="alert">The Attribute Definitions could not be loaded.</p>
      ) : definitions.data.totalCount === 0 ? (
        <p className="text-muted-foreground">No Attribute Definitions yet.</p>
      ) : (
        <>
          <ul className="flex flex-col gap-4">
            {definitions.data.items.map((definition) => (
              <li key={definition.id}>
                <DefinitionCard definition={definition} />
              </li>
            ))}
          </ul>
          <Pagination
            page={definitions.data.page}
            pageSize={definitions.data.pageSize}
            totalCount={definitions.data.totalCount}
            hrefFor={pageSearch}
          />
        </>
      )}
    </main>
  )
}

function CreateDefinitionForm() {
  const queryClient = useQueryClient()
  const empty = { name: '', type: 'Text' as AttributeType, isFilterable: false, allowsMultiple: false }
  const [values, setValues] = useState(empty)
  const [created, setCreated] = useState<string | null>(null)
  const create = useMutation({
    mutationFn: () =>
      call(
        api.POST('/api/admin/attribute-definitions', {
          body: { ...values, allowsMultiple: values.type === 'OptionList' && values.allowsMultiple },
        }),
      ),
    onSuccess: (definition) => {
      setValues(empty)
      // The list is ordered by name, so the new definition may land on another page.
      setCreated(definition.name)
      queryClient.invalidateQueries({ queryKey: attributeDefinitionsQueryKey })
    },
  })
  const fields = create.error instanceof ApiProblem ? create.error.fields : {}

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setCreated(null)
    create.mutate()
  }

  return (
    <form aria-label="New Attribute Definition" className="grid gap-4 rounded-lg border p-4 sm:grid-cols-2" onSubmit={handleSubmit}>
      <Field label="Name" error={fields.name}>
        <Input value={values.name} onChange={(e) => setValues({ ...values, name: e.target.value })} required />
      </Field>
      <Field label="Type" error={fields.type}>
        <Select value={values.type} onChange={(e) => setValues({ ...values, type: e.target.value as AttributeType })}>
          {attributeTypes.map((type) => (
            <option key={type.value} value={type.value}>
              {type.label}
            </option>
          ))}
        </Select>
      </Field>
      <div className="flex flex-wrap gap-4 text-sm sm:col-span-2">
        <label className="flex items-center gap-2">
          <input type="checkbox" checked={values.isFilterable} onChange={(e) => setValues({ ...values, isFilterable: e.target.checked })} />
          Filterable
        </label>
        {values.type === 'OptionList' && (
          <label className="flex items-center gap-2">
            <input
              type="checkbox"
              checked={values.allowsMultiple}
              onChange={(e) => setValues({ ...values, allowsMultiple: e.target.checked })}
            />
            Multi-select
          </label>
        )}
      </div>
      {create.isError && !hasFieldErrors(create.error) && (
        <p role="alert" className="text-sm text-destructive sm:col-span-2">
          {create.error.message}
        </p>
      )}
      <div className="flex items-center gap-3 sm:col-span-2">
        <Button type="submit" disabled={create.isPending}>
          Add attribute
        </Button>
        {created && (
          <span role="status" className="text-sm text-muted-foreground">
            {created} added.
          </span>
        )}
      </div>
    </form>
  )
}

function DefinitionCard({ definition }: { definition: AttributeDefinition }) {
  const queryClient = useQueryClient()
  const [name, setName] = useState(definition.name)
  const [isFilterable, setIsFilterable] = useState(definition.isFilterable)
  const path = { params: { path: { id: definition.id } } }
  const update = useMutation({
    mutationFn: () => call(api.PUT('/api/admin/attribute-definitions/{id}', { ...path, body: { name, isFilterable } })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: attributeDefinitionsQueryKey }),
  })
  const remove = useConfirmedDelete((confirm) =>
    call(api.DELETE('/api/admin/attribute-definitions/{id}', { params: { path: { id: definition.id }, query: { confirm } } })),
  )
  const fields = update.error instanceof ApiProblem ? update.error.fields : {}
  const changed = name !== definition.name || isFilterable !== definition.isFilterable

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    update.mutate()
  }

  return (
    <section aria-label={definition.name} className="flex flex-col gap-4 rounded-lg border p-4">
      <div className="flex flex-wrap items-center gap-2">
        <h2 className="font-semibold">{definition.name}</h2>
        <Badge>{typeLabel(definition.type)}</Badge>
        {definition.type === 'OptionList' && <Badge>{definition.allowsMultiple ? 'Multi-select' : 'Single select'}</Badge>}
        {definition.isFilterable && <Badge>Filterable</Badge>}
      </div>
      <form className="grid gap-4 sm:grid-cols-2" onSubmit={handleSubmit}>
        <Field label="Name" error={fields.name}>
          <Input value={name} onChange={(e) => setName(e.target.value)} required />
        </Field>
        <Field label="Type" hint="The type cannot change once created.">
          <Select value={definition.type} disabled>
            {attributeTypes.map((type) => (
              <option key={type.value} value={type.value}>
                {type.label}
              </option>
            ))}
          </Select>
        </Field>
        <label className="flex items-center gap-2 text-sm sm:col-span-2">
          <input type="checkbox" checked={isFilterable} onChange={(e) => setIsFilterable(e.target.checked)} />
          Filterable
        </label>
        {update.isError && !hasFieldErrors(update.error) && (
          <p role="alert" className="text-sm text-destructive sm:col-span-2">
            {update.error.message}
          </p>
        )}
        <div className="flex gap-2 sm:col-span-2">
          <Button type="submit" size="sm" disabled={!changed || update.isPending}>
            Save
          </Button>
          <Button type="button" size="sm" variant="outline" disabled={remove.busy} onClick={remove.start}>
            Delete attribute
          </Button>
        </div>
      </form>
      {remove.usageCount !== null && (
        <ConfirmPanel title={`Delete ${definition.name}?`} confirmLabel="Delete anyway" busy={remove.busy} onConfirm={remove.confirm} onCancel={remove.cancel}>
          {usedBy(remove.usageCount)}
        </ConfirmPanel>
      )}
      {remove.error && (
        <p role="alert" className="text-sm text-destructive">
          {remove.error}
        </p>
      )}
      {definition.type === 'OptionList' && <OptionsEditor definition={definition} />}
    </section>
  )
}

function OptionsEditor({ definition }: { definition: AttributeDefinition }) {
  const queryClient = useQueryClient()
  const [value, setValue] = useState('')
  const add = useMutation({
    mutationFn: () =>
      call(api.POST('/api/admin/attribute-definitions/{id}/options', { params: { path: { id: definition.id } }, body: { value } })),
    onSuccess: () => {
      setValue('')
      queryClient.invalidateQueries({ queryKey: attributeDefinitionsQueryKey })
    },
  })
  const error = add.error instanceof ApiProblem ? (add.error.fields.value ?? add.error.message) : null

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    add.mutate()
  }

  return (
    <div className="flex flex-col gap-3 border-t pt-4">
      <h3 className="text-sm font-semibold">Options</h3>
      {definition.options.length === 0 ? (
        <p className="text-sm text-muted-foreground">No Options yet.</p>
      ) : (
        <ul className="flex flex-col gap-2">
          {definition.options.map((option) => (
            <OptionRow key={option.id} definitionId={definition.id} option={option} />
          ))}
        </ul>
      )}
      <form aria-label={`New Option of ${definition.name}`} className="flex items-start gap-2" onSubmit={handleSubmit}>
        <div className="flex-1">
          <Field label="New Option" error={error ?? undefined}>
            <Input value={value} onChange={(e) => setValue(e.target.value)} required />
          </Field>
        </div>
        <Button type="submit" size="sm" variant="outline" className="mt-6" disabled={add.isPending}>
          Add option
        </Button>
      </form>
    </div>
  )
}

function OptionRow({ definitionId, option }: { definitionId: string; option: AttributeDefinition['options'][number] }) {
  const remove = useConfirmedDelete((confirm) =>
    call(
      api.DELETE('/api/admin/attribute-definitions/{id}/options/{optionId}', {
        params: { path: { id: definitionId, optionId: option.id }, query: { confirm } },
      }),
    ),
  )

  return (
    <li className="flex flex-col gap-2">
      <div className="flex items-center justify-between gap-2 rounded-md border px-3 py-1.5 text-sm">
        <span>{option.value}</span>
        <Button size="sm" variant="ghost" aria-label={`Delete option ${option.value}`} disabled={remove.busy} onClick={remove.start}>
          Delete
        </Button>
      </div>
      {remove.usageCount !== null && (
        <ConfirmPanel title={`Delete ${option.value}?`} confirmLabel="Delete anyway" busy={remove.busy} onConfirm={remove.confirm} onCancel={remove.cancel}>
          {usedBy(remove.usageCount)}
        </ConfirmPanel>
      )}
      {remove.error && (
        <p role="alert" className="text-sm text-destructive">
          {remove.error}
        </p>
      )}
    </li>
  )
}
