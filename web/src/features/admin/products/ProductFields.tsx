import { Field } from '@/components/Field'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'

export type ProductFieldValues = { name: string; sku: string; description: string; slug: string }

type Props = {
  values: ProductFieldValues
  errors: Record<string, string>
  showSlug: boolean
  onChange: (values: ProductFieldValues) => void
}

/** The core fields of a Product, with the API's field errors next to each one. */
export function ProductFields({ values, errors, showSlug, onChange }: Props) {
  const set = (field: keyof ProductFieldValues) => (event: { target: { value: string } }) =>
    onChange({ ...values, [field]: event.target.value })

  return (
    <div className="grid gap-4 md:grid-cols-2">
      <Field label="Name" error={errors.name}>
        <Input value={values.name} onChange={set('name')} required />
      </Field>
      <Field label="SKU" error={errors.sku}>
        <Input mono value={values.sku} onChange={set('sku')} required />
      </Field>
      {showSlug && (
        <div className="md:col-span-2">
          <Field label="Product slug" error={errors.slug} hint="Part of the product's public address. Changing it breaks existing links.">
            <Input mono value={values.slug} onChange={set('slug')} required />
          </Field>
        </div>
      )}
      <div className="md:col-span-2">
        <Field label="Description" error={errors.description}>
          <Textarea value={values.description} onChange={set('description')} rows={6} />
        </Field>
      </div>
    </div>
  )
}
