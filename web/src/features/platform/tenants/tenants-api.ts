import type { components } from '@/api/client'

export type Tenant = components['schemas']['TenantResponse']

export const tenantsQueryKey = ['platform', 'tenants'] as const
