import createClient, { type ClientOptions } from 'openapi-fetch'
import type { paths } from '@/api/schema'

export function createApiClient(options: ClientOptions = {}) {
  return createClient<paths>({ baseUrl: import.meta.env.VITE_API_URL ?? '', ...options })
}

export const api = createApiClient()

export type { components, paths } from '@/api/schema'
