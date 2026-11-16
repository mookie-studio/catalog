import type { PublicProductAttribute } from '@/features/public/public-api'

/** Attribute values as visitors read them; option lists arrive as their option text. */
export function formatAttribute({ type, value }: PublicProductAttribute): string {
  if (Array.isArray(value)) return value.join(', ')
  if (type === 'Boolean') return value ? 'Yes' : 'No'
  if (type === 'Date' && typeof value === 'string') {
    return new Date(`${value}T00:00:00`).toLocaleDateString(undefined, { dateStyle: 'medium' })
  }
  return String(value)
}
