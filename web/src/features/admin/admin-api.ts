import { useQuery } from '@tanstack/react-query'
import { api, type components } from '@/api/client'
import { call } from '@/api/problem'

export type CategoryNode = components['schemas']['CategoryNode']

export function useCurrentTenant() {
  return useQuery({ queryKey: ['admin', 'tenant'], queryFn: () => call(api.GET('/api/admin/tenant')) })
}

export const categoriesQueryKey = ['admin', 'categories'] as const

export function useCategories() {
  return useQuery({ queryKey: categoriesQueryKey, queryFn: () => call(api.GET('/api/admin/categories')) })
}

/** The Category tree as a flat list in display order, with each Category's depth. */
export function flattenCategories(nodes: CategoryNode[], depth = 0): { id: string; name: string; depth: number }[] {
  return nodes.flatMap((node) => [{ id: node.id, name: node.name, depth }, ...flattenCategories(node.children, depth + 1)])
}
