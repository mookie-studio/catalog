import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { fetchMock, json, mockApi } from '@/test/api'
import { renderApp } from '@/test/render'

// Supabase is replaced by this in-memory session; the API by the fetch mock.
const session = vi.hoisted(() => ({ token: null as string | null }))

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
    expect(request.headers.get('Authorization')).toBe('Bearer token')
    return request.url === 'https://api.test/api/me' ? json(me) : new Response(null, { status: 404 })
  })
}

const renderAt = renderApp

beforeEach(() => {
  session.token = null
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
    expect(screen.getByRole('navigation', { name: 'Admin' })).toBeInTheDocument()
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
    mockApi({ '/api/me': admin })

    await userEvent.type(screen.getByLabelText('Email'), 'admin@test')
    await userEvent.type(screen.getByLabelText('Password'), 'secret-password')
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('button', { name: 'Sign out' })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/admin/products')
  })

  it('signs out and returns to the sign-in page', async () => {
    signedInAs(admin)
    const router = renderAt('/admin')

    await userEvent.click(await screen.findByRole('button', { name: 'Sign out' }))

    expect(await screen.findByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/login')
  })

  it('shows the public catalog without signing in', async () => {
    mockApi({ '/api/public/acme': { name: 'Acme Hardware', slug: 'acme' }, '/api/public/acme/products': { items: [], page: 1, pageSize: 20, totalCount: 0 } })

    renderAt('/acme')

    expect(await screen.findByText('Acme Hardware')).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Sign in' })).not.toBeInTheDocument()
  })
})
