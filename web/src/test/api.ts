import { vi } from 'vitest'

// The API is mocked at the fetch level; src/test/setup.ts installs this mock
// before any test imports the API client.
export const fetchMock = vi.fn<(request: Request) => Promise<Response>>()

export function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

export function problem(status: number, errors?: Record<string, string[]>, detail?: string): Response {
  return new Response(JSON.stringify({ status, title: 'Error', detail, errors }), {
    status,
    headers: { 'Content-Type': 'application/problem+json' },
  })
}

type Handler = unknown | ((url: URL, request: Request) => Response | Promise<Response>)

/**
 * Answers requests by "METHOD /path" or by "/path" (any method); unknown routes get a 404.
 * A plain value is returned as JSON.
 */
export function mockApi(routes: Record<string, Handler>) {
  fetchMock.mockImplementation(async (request: Request) => {
    const url = new URL(request.url)
    const handler = routes[`${request.method} ${url.pathname}`] ?? routes[url.pathname]
    if (handler === undefined) return new Response(null, { status: 404 })
    return typeof handler === 'function' ? handler(url, request.clone()) : json(handler)
  })
}

/** Requests made to a path (and method, when given), in order. */
export function requestsTo(pathname: string, method?: string): Request[] {
  return fetchMock.mock.calls
    .map(([request]) => request)
    .filter((request) => new URL(request.url).pathname === pathname && (!method || request.method === method))
}

export const urlsTo = (pathname: string) => requestsTo(pathname).map((request) => new URL(request.url))

/** The JSON body of the last request to a path and method. */
export async function lastBody(pathname: string, method: string): Promise<unknown> {
  const request = requestsTo(pathname, method).at(-1)
  if (!request) throw new Error(`No ${method} request to ${pathname}`)
  return request.clone().json()
}

/** A one-page paged response ({ items, page, pageSize, totalCount }) holding every item. */
export function paged<T>(items: T[], page = 1, pageSize = 20, totalCount = items.length) {
  return { items, page, pageSize, totalCount }
}
