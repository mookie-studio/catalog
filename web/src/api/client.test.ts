import { afterEach, describe, expect, it, vi } from 'vitest'
import { setAccessTokenProvider } from '@/api/access-token'
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

describe('api client authentication', () => {
  afterEach(() => setAccessTokenProvider(null))

  it('attaches the access token as a bearer token', async () => {
    setAccessTokenProvider(async () => 'token-123')
    const fetch = vi.fn(async (request: Request) => {
      expect(request.headers.get('Authorization')).toBe('Bearer token-123')
      return new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } })
    })
    const client = createApiClient({ baseUrl: 'https://api.test', fetch })

    await client.GET('/api/me')

    expect(fetch).toHaveBeenCalledOnce()
  })

  it('sends no authorization header without a token', async () => {
    const fetch = vi.fn(async (request: Request) => {
      expect(request.headers.has('Authorization')).toBe(false)
      return new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } })
    })
    const client = createApiClient({ baseUrl: 'https://api.test', fetch })

    await client.GET('/api/health')

    expect(fetch).toHaveBeenCalledOnce()
  })
})
