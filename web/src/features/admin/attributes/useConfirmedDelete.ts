import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { ApiProblem } from '@/api/problem'
import { attributeDefinitionsQueryKey } from '@/features/admin/attributes/attributes-api'

/**
 * Deletes an Attribute Definition or Option. When products use it the API answers 409 with usageCount;
 * the caller shows that count and, once confirmed, the delete is retried with confirm=true.
 */
export function useConfirmedDelete(remove: (confirm: boolean) => Promise<unknown>) {
  const queryClient = useQueryClient()
  const [usageCount, setUsageCount] = useState<number | null>(null)
  const mutation = useMutation({
    mutationFn: remove,
    onSuccess: () => {
      setUsageCount(null)
      queryClient.invalidateQueries({ queryKey: attributeDefinitionsQueryKey })
      queryClient.invalidateQueries({ queryKey: ['admin', 'product'] })
    },
    onError: (error) => {
      if (error instanceof ApiProblem && error.status === 409 && typeof error.body.usageCount === 'number') {
        setUsageCount(error.body.usageCount)
      }
    },
  })

  return {
    usageCount,
    busy: mutation.isPending,
    error: mutation.error && usageCount === null ? mutation.error.message : null,
    start: () => mutation.mutate(false),
    confirm: () => mutation.mutate(true),
    cancel: () => {
      setUsageCount(null)
      mutation.reset()
    },
  }
}
