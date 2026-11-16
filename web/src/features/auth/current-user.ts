import { useQuery } from '@tanstack/react-query'
import { api, type components } from '@/api/client'
import { hasSession } from '@/features/auth/session'

export type CurrentUser = components['schemas']['GetMeResponse']

export const currentUserQueryKey = ['me'] as const

/** The signed-in person as the API sees them, or null when nobody is signed in. */
export async function loadCurrentUser(): Promise<CurrentUser | null> {
  if (!(await hasSession())) {
    return null
  }
  const { data, response } = await api.GET('/api/me')
  if (response.status === 401) {
    return null
  }
  if (!data) {
    throw new Error(`Could not load the current user (HTTP ${response.status}).`)
  }
  return data
}

export function useCurrentUser() {
  return useQuery({ queryKey: currentUserQueryKey, queryFn: loadCurrentUser, retry: false })
}

/** Where a person lands after signing in. */
export function homeFor(user: CurrentUser): string | null {
  if (user.isPlatformAdmin) return '/platform'
  if (user.tenantId) return '/admin'
  return null
}
