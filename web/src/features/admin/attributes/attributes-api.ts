import { useQuery } from '@tanstack/react-query'
import { api, type components } from '@/api/client'
import { call } from '@/api/problem'

export type AttributeDefinition = components['schemas']['AttributeDefinitionResponse']
export type AttributeType = components['schemas']['AttributeType']

export const attributeTypes: { value: AttributeType; label: string }[] = [
  { value: 'Text', label: 'Text' },
  { value: 'Number', label: 'Number' },
  { value: 'Boolean', label: 'Yes / no' },
  { value: 'Date', label: 'Date' },
  { value: 'OptionList', label: 'Option list' },
]

export const attributeDefinitionsQueryKey = ['admin', 'attribute-definitions'] as const

export function useAttributeDefinitions() {
  return useQuery({ queryKey: attributeDefinitionsQueryKey, queryFn: () => call(api.GET('/api/admin/attribute-definitions')) })
}
