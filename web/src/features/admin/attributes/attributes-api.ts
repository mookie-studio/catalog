import { useQuery } from '@tanstack/react-query'
import { api, type components } from '@/api/client'
import { call } from '@/api/problem'
import { fetchAllPages, maxPageSize } from '@/api/paging'

export type AttributeDefinition = components['schemas']['AttributeDefinitionResponse']
export type AttributeType = components['schemas']['AttributeType']

export const attributeTypes: { value: AttributeType; label: string }[] = [
  { value: 'Text', label: 'Text' },
  { value: 'Number', label: 'Number' },
  { value: 'Boolean', label: 'Yes / no' },
  { value: 'Date', label: 'Date' },
  { value: 'OptionList', label: 'Option list' },
]

/** Prefix of every Attribute Definition query; invalidate it after any change. */
export const attributeDefinitionsQueryKey = ['admin', 'attribute-definitions'] as const

const fetchPage = (page: number, pageSize?: number) =>
  call(api.GET('/api/admin/attribute-definitions', { params: { query: { page, pageSize } } }))

/** Every Attribute Definition, for screens that need them all (such as the product Attribute editor). */
export function useAttributeDefinitions() {
  return useQuery({
    queryKey: [...attributeDefinitionsQueryKey, 'all'],
    queryFn: () => fetchAllPages((page) => fetchPage(page, maxPageSize)),
  })
}

/** One page of Attribute Definitions, for the Attributes page. */
export function useAttributeDefinitionsPage(page: number) {
  return useQuery({ queryKey: [...attributeDefinitionsQueryKey, 'page', page], queryFn: () => fetchPage(page) })
}
