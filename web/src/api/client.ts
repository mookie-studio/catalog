import createClient, { type ClientOptions, type Middleware } from 'openapi-fetch'
import { getAccessToken } from '@/api/access-token'
import type { paths } from '@/api/schema'

const bearerToken: Middleware = {
  async onRequest({ request }) {
    const token = await getAccessToken()
    if (token) {
      request.headers.set('Authorization', `Bearer ${token}`)
    }
    return request
  },
}

export function createApiClient(options: ClientOptions = {}) {
  const client = createClient<paths>({ baseUrl: import.meta.env.VITE_API_URL ?? '', ...options })
  client.use(bearerToken)
  return client
}

export const api = createApiClient()

export type { components, paths } from '@/api/schema'
