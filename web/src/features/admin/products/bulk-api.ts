import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api, type components } from '@/api/client'
import { call } from '@/api/problem'
import { dashboardQueryKey } from '@/features/admin/dashboard/dashboard-api'

export type BulkAction = NonNullable<components['schemas']['NullableOfBulkAction']>
export type BulkResult = components['schemas']['BulkProductActionResponse']

/** Applies a Bulk action to the given products and refreshes the lists that show them. */
export function useBulkAction() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (variables: { productIds: string[]; action: BulkAction; categoryId?: string }) =>
      call(api.POST('/api/admin/products/bulk', { body: { ...variables, categoryId: variables.categoryId ?? null } })),
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'products'] })
      queryClient.invalidateQueries({ queryKey: dashboardQueryKey })
    },
  })
}
