import type { AttributeDefinition } from '@/features/admin/attributes/attributes-api'
import type { Product } from '@/features/admin/products/products-api'

/** What each input holds: a string for most types, the selected option ids for multi-select lists. */
export type AttributeInput = string | string[]

/** Input values from the Product's stored Attributes (option lists hold option ids). */
export function toInputs(product: Product): Record<string, AttributeInput> {
  const inputs: Record<string, AttributeInput> = {}
  for (const attribute of product.attributes) {
    const value = attribute.value
    if (Array.isArray(value)) inputs[attribute.definitionId] = value.map(String)
    else if (value !== null && value !== undefined) inputs[attribute.definitionId] = String(value)
  }
  return inputs
}

/** The body of PUT /api/admin/products/{id}/attributes: typed values, empty inputs omitted. */
export function toValues(definitions: AttributeDefinition[], inputs: Record<string, AttributeInput>): Record<string, unknown> {
  const values: Record<string, unknown> = {}
  for (const definition of definitions) {
    const input = inputs[definition.id]
    if (input === undefined) continue
    if (Array.isArray(input)) {
      if (input.length > 0) values[definition.id] = input
      continue
    }
    const text = input.trim()
    if (text === '') continue
    switch (definition.type) {
      case 'Number':
        values[definition.id] = Number(text)
        break
      case 'Boolean':
        values[definition.id] = text === 'true'
        break
      default:
        values[definition.id] = text
    }
  }
  return values
}
