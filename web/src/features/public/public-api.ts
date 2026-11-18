import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { api, type components } from '@/api/client'
import type { CatalogQuery } from '@/features/public/catalog-query'
import { toApiQuery } from '@/features/public/catalog-query'

export type PublicProductItem = components['schemas']['ListPublicProductsItem']
export type PublicFilter = components['schemas']['ListPublicFiltersFilter']
export type CategoryNode = components['schemas']['CategoryNode']
export type PublicProduct = components['schemas']['GetPublicProductResponse']
export type PublicTenant = components['schemas']['GetPublicTenantResponse']
export type PublicProductAttribute = components['schemas']['GetPublicProductAttribute']

/** Thrown for a 404 so pages can show the not-found page. */
export class NotFoundError extends Error {
  constructor() {
    super('Not found')
  }
}

function unwrap<T>(data: T | undefined, response: Response): T {
  if (response.status === 404) throw new NotFoundError()
  if (data === undefined) throw new Error(`Request failed (HTTP ${response.status}).`)
  return data
}

const retryUnlessNotFound = (failureCount: number, error: Error) => !(error instanceof NotFoundError) && failureCount < 1

export const publicTenantKey = (tenantSlug: string) => ['public', tenantSlug, 'tenant'] as const

export function usePublicTenant(tenantSlug: string) {
  return useQuery({
    queryKey: publicTenantKey(tenantSlug),
    queryFn: async () => {
      const { data, response } = await api.GET('/api/public/{tenantSlug}', { params: { path: { tenantSlug } } })
      return unwrap(data, response)
    },
    retry: retryUnlessNotFound,
  })
}

export function usePublicProducts(tenantSlug: string, query: CatalogQuery) {
  return useQuery({
    queryKey: ['public', tenantSlug, 'products', query],
    queryFn: async () => {
      const { data, response } = await api.GET('/api/public/{tenantSlug}/products', {
        params: { path: { tenantSlug }, query: toApiQuery(query) },
      })
      return unwrap(data, response)
    },
    placeholderData: keepPreviousData,
    retry: retryUnlessNotFound,
  })
}

export function useFeaturedProducts(tenantSlug: string) {
  return useQuery({
    queryKey: ['public', tenantSlug, 'featured'],
    queryFn: async () => {
      const { data, response } = await api.GET('/api/public/{tenantSlug}/products', {
        params: { path: { tenantSlug }, query: { featured: true, pageSize: 8 } },
      })
      return unwrap(data, response)
    },
    retry: retryUnlessNotFound,
  })
}

export function usePublicFilters(tenantSlug: string) {
  return useQuery({
    queryKey: ['public', tenantSlug, 'filters'],
    queryFn: async () => {
      const { data, response } = await api.GET('/api/public/{tenantSlug}/filters', { params: { path: { tenantSlug } } })
      return unwrap(data, response)
    },
    retry: retryUnlessNotFound,
  })
}

export function usePublicCategories(tenantSlug: string) {
  return useQuery({
    queryKey: ['public', tenantSlug, 'categories'],
    queryFn: async () => {
      const { data, response } = await api.GET('/api/public/{tenantSlug}/categories', { params: { path: { tenantSlug } } })
      return unwrap(data, response)
    },
    retry: retryUnlessNotFound,
  })
}

export const publicProductKey = (tenantSlug: string, productSlug: string) =>
  ['public', tenantSlug, 'product', productSlug] as const

export function usePublicProduct(tenantSlug: string, productSlug: string) {
  return useQuery({
    queryKey: publicProductKey(tenantSlug, productSlug),
    queryFn: async () => {
      const { data, response } = await api.GET('/api/public/{tenantSlug}/products/{productSlug}', {
        params: { path: { tenantSlug, productSlug } },
      })
      return unwrap(data, response)
    },
    retry: retryUnlessNotFound,
  })
}

/** Related products of a product (they share a Category), a first page of `limit`. */
export function useRelatedProducts(tenantSlug: string, productSlug: string, limit = 4) {
  return useQuery({
    queryKey: ['public', tenantSlug, 'product', productSlug, 'related', limit],
    queryFn: async () => {
      const { data, response } = await api.GET('/api/public/{tenantSlug}/products/{productSlug}/related', {
        params: { path: { tenantSlug, productSlug }, query: { pageSize: limit } },
      })
      return unwrap(data, response)
    },
    retry: retryUnlessNotFound,
  })
}
