import { useMutation, useQueryClient } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { Link } from 'react-router'
import { api } from '@/api/client'
import { ApiProblem, call, hasFieldErrors } from '@/api/problem'
import { Field } from '@/components/Field'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { type AttributeDefinition, useAttributeDefinitions } from '@/features/admin/attributes/attributes-api'
import { type AttributeInput, toInputs, toValues } from '@/features/admin/products/attribute-values'
import { type Product, productQueryKey } from '@/features/admin/products/products-api'

/** Every Attribute Definition of the Tenant with an input by type; all are optional. */
export function ProductAttributesEditor({ product }: { product: Product }) {
  const definitions = useAttributeDefinitions()
  const queryClient = useQueryClient()
  const [inputs, setInputs] = useState(() => toInputs(product))
  const [saved, setSaved] = useState(false)
  const save = useMutation({
    mutationFn: (values: Record<string, unknown>) =>
      call(api.PUT('/api/admin/products/{id}/attributes', { params: { path: { id: product.id } }, body: { values } })),
    onSuccess: (updated) => {
      queryClient.setQueryData(productQueryKey(product.id), updated)
      setSaved(true)
    },
  })
  const errors = save.error instanceof ApiProblem ? save.error.fields : {}

  if (definitions.isPending) return null
  if (definitions.isError) return <p role="alert">The Attribute Definitions could not be loaded.</p>

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSaved(false)
    save.mutate(toValues(definitions.data ?? [], inputs))
  }

  const set = (id: string) => (value: AttributeInput) => setInputs((current) => ({ ...current, [id]: value }))

  return (
    <section aria-label="Attributes" className="flex flex-col gap-4 border-t pt-6">
      <h2 className="text-lg font-semibold">Attributes</h2>
      {definitions.data.length === 0 ? (
        <p className="text-sm text-muted-foreground">
          This Tenant has no Attribute Definitions yet. <Link to="/admin/attributes" className="underline">Create one</Link>.
        </p>
      ) : (
        <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
          <div className="grid gap-4 md:grid-cols-2">
            {definitions.data.map((definition) => (
              <AttributeInputField
                key={definition.id}
                definition={definition}
                value={inputs[definition.id]}
                error={errors[definition.id]}
                onChange={set(definition.id)}
              />
            ))}
          </div>
          {save.isError && !hasFieldErrors(save.error) && (
            <p role="alert" className="text-sm text-destructive">
              {save.error.message}
            </p>
          )}
          <div className="flex items-center gap-3">
            <Button type="submit" disabled={save.isPending}>
              Save attributes
            </Button>
            {saved && (
              <span role="status" className="text-sm text-muted-foreground">
                Attributes saved
              </span>
            )}
          </div>
        </form>
      )}
    </section>
  )
}

type InputProps = { definition: AttributeDefinition; value: AttributeInput | undefined; error?: string; onChange: (value: AttributeInput) => void }

function AttributeInputField({ definition, value, error, onChange }: InputProps) {
  const text = typeof value === 'string' ? value : ''
  switch (definition.type) {
    case 'Number':
      return (
        <Field label={definition.name} error={error}>
          <Input type="number" step="any" value={text} onChange={(e) => onChange(e.target.value)} />
        </Field>
      )
    case 'Date':
      return (
        <Field label={definition.name} error={error}>
          <Input type="date" value={text} onChange={(e) => onChange(e.target.value)} />
        </Field>
      )
    case 'Boolean':
      return (
        <Field label={definition.name} error={error}>
          <Select value={text} onChange={(e) => onChange(e.target.value)}>
            <option value="">Not set</option>
            <option value="true">Yes</option>
            <option value="false">No</option>
          </Select>
        </Field>
      )
    case 'OptionList':
      if (definition.allowsMultiple) {
        const selected = Array.isArray(value) ? value : []
        return (
          <fieldset className="space-y-2" aria-describedby={error ? `${definition.id}-error` : undefined}>
            <legend className="text-sm font-medium">{definition.name}</legend>
            <div className="flex flex-wrap gap-x-4 gap-y-1 text-sm">
              {definition.options.map((option) => (
                <label key={option.id} className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    checked={selected.includes(option.id)}
                    onChange={(e) => onChange(e.target.checked ? [...selected, option.id] : selected.filter((id) => id !== option.id))}
                  />
                  {option.value}
                </label>
              ))}
            </div>
            {error && (
              <p id={`${definition.id}-error`} className="text-sm text-destructive">
                {error}
              </p>
            )}
          </fieldset>
        )
      }
      return (
        <Field label={definition.name} error={error}>
          <Select value={text} onChange={(e) => onChange(e.target.value)}>
            <option value="">Not set</option>
            {definition.options.map((option) => (
              <option key={option.id} value={option.id}>
                {option.value}
              </option>
            ))}
          </Select>
        </Field>
      )
    default:
      return (
        <Field label={definition.name} error={error}>
          <Input value={text} onChange={(e) => onChange(e.target.value)} />
        </Field>
      )
  }
}
