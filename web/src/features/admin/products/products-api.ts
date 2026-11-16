import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { api, type components } from '@/api/client'
import { call } from '@/api/problem'

export type Product = components['schemas']['ProductResponse']
export type ProductStatus = components['schemas']['ProductStatus']
export type ProductListItem = components['schemas']['ListProductsItem']

export type ProductListQuery = { q: string; status: ProductStatus | null; categoryId: string | null; page: number }

const statuses: ProductStatus[] = ['Draft', 'Published', 'Archived']

export function readProductListQuery(params: URLSearchParams): ProductListQuery {
  const page = Number.parseInt(params.get('page') ?? '', 10)
  const status = params.get('status') as ProductStatus | null
  return {
    q: params.get('q') ?? '',
    status: status && statuses.includes(status) ? status : null,
    categoryId: params.get('categoryId') || null,
    page: Number.isFinite(page) && page > 0 ? page : 1,
  }
}

export function writeProductListQuery(query: ProductListQuery): URLSearchParams {
  const params = new URLSearchParams()
  if (query.q) params.set('q', query.q)
  if (query.status) params.set('status', query.status)
  if (query.categoryId) params.set('categoryId', query.categoryId)
  if (query.page > 1) params.set('page', String(query.page))
  return params
}

export function useProducts(query: ProductListQuery) {
  return useQuery({
    queryKey: ['admin', 'products', query],
    queryFn: () =>
      call(
        api.GET('/api/admin/products', {
          params: {
            query: {
              page: query.page,
              q: query.q || undefined,
              status: query.status ?? undefined,
              categoryId: query.categoryId ?? undefined,
            },
          },
        }),
      ),
    placeholderData: keepPreviousData,
  })
}

export const productQueryKey = (id: string) => ['admin', 'product', id] as const

export function useProduct(id: string) {
  return useQuery({
    queryKey: productQueryKey(id),
    queryFn: () => call(api.GET('/api/admin/products/{id}', { params: { path: { id } } })),
  })
}
