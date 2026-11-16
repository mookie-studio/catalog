import { vi } from 'vitest'

// The API is mocked at the fetch level; src/test/setup.ts installs this mock
// before any test imports the API client.
export const fetchMock = vi.fn<(request: Request) => Promise<Response>>()

export function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

type Handler = unknown | ((url: URL, request: Request) => Response)

/** Answers requests by path (https://api.test/<path>); unknown paths get a 404. */
export function mockApi(routes: Record<string, Handler>) {
  fetchMock.mockImplementation(async (request: Request) => {
    const url = new URL(request.url)
    const handler = routes[url.pathname]
    if (handler === undefined) return new Response(null, { status: 404 })
    return typeof handler === 'function' ? handler(url, request) : json(handler)
  })
}

/** URLs of the requests made to a path, in order. */
export function requestsTo(pathname: string): URL[] {
  return fetchMock.mock.calls.map(([request]) => new URL(request.url)).filter((url) => url.pathname === pathname)
}
