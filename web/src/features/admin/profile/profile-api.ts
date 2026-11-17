import { useQuery } from '@tanstack/react-query'
import { api, type components } from '@/api/client'
import { call } from '@/api/problem'

export type TenantProfile = components['schemas']['TenantProfileResponse']

export const profileQueryKey = ['admin', 'profile'] as const

export function useTenantProfile() {
  return useQuery({ queryKey: profileQueryKey, queryFn: () => call(api.GET('/api/admin/tenant/profile')) })
}

export const maxLogoBytes = 1024 * 1024
export const logoTypes = ['image/jpeg', 'image/png', 'image/webp']

/** Returns why a file cannot be uploaded as the Logo, or null when it can. */
export function checkLogo(file: File): string | null {
  if (!logoTypes.includes(file.type)) return 'Only JPG, PNG and WebP images are allowed.'
  if (file.size > maxLogoBytes) return 'The logo must be at most 1 MB.'
  return null
}
