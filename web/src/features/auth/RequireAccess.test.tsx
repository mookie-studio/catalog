import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { routes } from '@/app/routes'

// Supabase is replaced by this in-memory session; the API by a mocked fetch.
const { fetchMock, session } = vi.hoisted(() => {
  const fetchMock = vi.fn()
  globalThis.fetch = fetchMock
  return { fetchMock, session: { token: null as string | null } }
})

vi.mock('@/features/auth/session', async () => {
  const { setAccessTokenProvider } = await import('@/api/access-token')
  setAccessTokenProvider(async () => session.token)
  return {
    hasSession: async () => session.token !== null,
    signIn: vi.fn(async () => {
      session.token = 'signed-in'
      return null
    }),
    setPassword: vi.fn(async () => null),
    signOut: vi.fn(async () => {
      session.token = null
    }),
  }
})

type Me = { userId: string; email: string; isPlatformAdmin: boolean; tenantId: string | null }

const admin: Me = { userId: 'u1', email: 'admin@test', isPlatformAdmin: false, tenantId: 't1' }
const platformAdmin: Me = { userId: 'u2', email: 'platform@test', isPlatformAdmin: true, tenantId: null }

function signedInAs(me: Me) {
  session.token = 'token'
  fetchMock.mockImplementation(async (request: Request) => {
    expect(request.url).toBe('https://api.test/api/me')
    expect(request.headers.get('Authorization')).toBe('Bearer token')
    return new Response(JSON.stringify(me), { status: 200, headers: { 'Content-Type': 'application/json' } })
  })
}

function renderAt(path: string) {
  const router = createMemoryRouter(routes, { initialEntries: [path] })
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  )
  return router
}

beforeEach(() => {
  session.token = null
  fetchMock.mockReset()
})

describe('route guards', () => {
  it.each(['/admin', '/platform'])('sends a signed-out person from %s to the sign-in page', async (path) => {
    const router = renderAt(path)

    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/login')
    expect(router.state.location.search).toBe(`?redirectTo=${encodeURIComponent(path)}`)
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('lets an admin into the admin area', async () => {
    signedInAs(admin)

    renderAt('/admin')

    expect(await screen.findByRole('button', { name: 'Sign out' })).toBeInTheDocument()
    expect(screen.getByRole('main')).toHaveTextContent('Admin')
  })

  it('shows no access to an admin in the platform area', async () => {
    signedInAs(admin)

    renderAt('/platform')

    expect(await screen.findByRole('heading', { name: 'No access' })).toBeInTheDocument()
  })

  it('lets a platform admin into the platform area', async () => {
    signedInAs(platformAdmin)

    renderAt('/platform')

    expect(await screen.findByRole('button', { name: 'Sign out' })).toBeInTheDocument()
    expect(screen.getByRole('main')).toHaveTextContent('Platform')
  })

  it('shows no access to a platform admin in the admin area', async () => {
    signedInAs(platformAdmin)

    renderAt('/admin')

    expect(await screen.findByRole('heading', { name: 'No access' })).toBeInTheDocument()
  })

  it('sends a person whose token the API rejects to the sign-in page', async () => {
    session.token = 'expired'
    fetchMock.mockResolvedValue(new Response(null, { status: 401 }))

    const router = renderAt('/admin')

    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/login')
  })

  it('returns to the requested page after signing in', async () => {
    const router = renderAt('/admin')
    await screen.findByRole('heading', { name: 'Sign in' })
    fetchMock.mockImplementation(
      async () => new Response(JSON.stringify(admin), { status: 200, headers: { 'Content-Type': 'application/json' } }),
    )

    await userEvent.type(screen.getByLabelText('Email'), 'admin@test')
    await userEvent.type(screen.getByLabelText('Password'), 'secret-password')
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('button', { name: 'Sign out' })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/admin')
  })

  it('signs out and returns to the sign-in page', async () => {
    signedInAs(admin)
    const router = renderAt('/admin')

    await userEvent.click(await screen.findByRole('button', { name: 'Sign out' }))

    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/login')
  })

  it('shows the public catalog without signing in', async () => {
    fetchMock.mockResolvedValue(new Response('{}', { status: 200, headers: { 'Content-Type': 'application/json' } }))

    renderAt('/acme')

    expect(await screen.findByText('acme')).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Sign in' })).not.toBeInTheDocument()
  })
})
