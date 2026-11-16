import { describe, expect, it, vi } from 'vitest'
import { createApiClient } from '@/api/client'

describe('api client', () => {
  it('calls the health endpoint and returns the typed body', async () => {
    const fetch = vi.fn(async (request: Request) => {
      expect(request.url).toBe('https://api.test/api/health')
      return new Response(JSON.stringify({ status: 'ok' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      })
    })
    const client = createApiClient({ baseUrl: 'https://api.test', fetch })

    const { data } = await client.GET('/api/health')

    expect(data?.status).toBe('ok')
    expect(fetch).toHaveBeenCalledOnce()
  })
})
