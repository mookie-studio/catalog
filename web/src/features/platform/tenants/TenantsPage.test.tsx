import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { created, noContent } from '@/test/admin'
import { json, lastBody, mockApi, paged, problem, urlsTo } from '@/test/api'
import { renderApp } from '@/test/render'

vi.mock('@/features/auth/session', () => import('@/test/session'))

const platformAdmin = { userId: 'u9', email: 'platform@test', isPlatformAdmin: true, tenantId: null }
type Tenant = { id: string; name: string; slug: string; isActive: boolean; createdAt: string }
const acme: Tenant = { id: 't1', name: 'Acme Hardware', slug: 'acme', isActive: true, createdAt: '2026-11-16T10:00:00Z' }

/** A small in-memory fake of the platform endpoints. */
function platformApi(routes: Record<string, unknown> = {}) {
  let tenants = [acme]
  let admins: { id: string; userId: string; email: string }[] = []
  const update = (id: string, change: Partial<Tenant>) => (tenants = tenants.map((t) => (t.id === id ? { ...t, ...change } : t)))
  mockApi({
    '/api/me': platformAdmin,
    'GET /api/platform/tenants': (url: URL) =>
      json({ items: tenants, page: Number(url.searchParams.get('page')), pageSize: 20, totalCount: tenants.length }),
    'POST /api/platform/tenants': async (_url: URL, request: Request) => {
      const body = (await request.json()) as { name: string; slug: string }
      const tenant = { id: 't2', ...body, isActive: true, createdAt: '2026-11-16T11:00:00Z' }
      tenants = [...tenants, tenant]
      return created(tenant)
    },
    'GET /api/platform/tenants/t1/admins': () => json(paged(admins)),
    'POST /api/platform/tenants/t1/admins': async (_url: URL, request: Request) => {
      const { email } = (await request.json()) as { email: string }
      const admin = { id: 'm1', userId: 'u1', email }
      admins = [...admins, admin]
      return created(admin)
    },
    'POST /api/platform/tenants/t1/deactivate': () => (update('t1', { isActive: false }), noContent()),
    'POST /api/platform/tenants/t1/reactivate': () => (update('t1', { isActive: true }), noContent()),
    'PUT /api/platform/tenants/t1/slug': async (_url: URL, request: Request) => {
      const { slug } = (await request.json()) as { slug: string }
      update('t1', { slug })
      return json(tenants[0])
    },
    ...routes,
  })
}

async function openTenant(name: string) {
  const row = await screen.findByRole('listitem', { name })
  await userEvent.click(within(row).getByRole('button', { name: 'Manage' }))
  return row
}

describe('platform tenants', () => {
  it('lists Tenants with name, slug and active state, paged from the URL', async () => {
    platformApi()

    renderApp('/platform/tenants?page=2')

    const row = await screen.findByRole('listitem', { name: 'Acme Hardware' })
    expect(within(row).getByText('/acme')).toBeInTheDocument()
    expect(within(row).getByText('Active')).toBeInTheDocument()
    expect(urlsTo('/api/platform/tenants').at(-1)!.searchParams.get('page')).toBe('2')
  })

  it('creates a Tenant', async () => {
    platformApi()
    renderApp('/platform/tenants')
    const form = await screen.findByRole('form', { name: 'New Tenant' })

    await userEvent.type(within(form).getByLabelText('Name'), 'Garden Shop')
    await userEvent.type(within(form).getByLabelText('Tenant slug'), 'garden-shop')
    await userEvent.click(within(form).getByRole('button', { name: 'Create tenant' }))

    expect(await screen.findByRole('listitem', { name: 'Garden Shop' })).toBeInTheDocument()
    expect(await lastBody('/api/platform/tenants', 'POST')).toEqual({ name: 'Garden Shop', slug: 'garden-shop' })
  })

  it('shows the error when the Tenant slug is taken', async () => {
    platformApi({ 'POST /api/platform/tenants': () => problem(409, { slug: ['The Tenant slug is already in use.'] }) })
    renderApp('/platform/tenants')
    const form = await screen.findByRole('form', { name: 'New Tenant' })

    await userEvent.type(within(form).getByLabelText('Name'), 'Acme Two')
    await userEvent.type(within(form).getByLabelText('Tenant slug'), 'acme')
    await userEvent.click(within(form).getByRole('button', { name: 'Create tenant' }))

    expect(await within(form).findByLabelText('Tenant slug')).toHaveAccessibleDescription('The Tenant slug is already in use.')
  })

  it("invites an Admin and shows them in the Tenant's Admin list", async () => {
    platformApi()
    renderApp('/platform/tenants')
    const row = await openTenant('Acme Hardware')
    const admins = await within(row).findByRole('region', { name: 'Admins' })

    expect(await within(admins).findByText('No Admins yet. Invite the first one.')).toBeInTheDocument()
    await userEvent.type(within(admins).getByLabelText('Admin email'), 'owner@acme.test')
    await userEvent.click(within(admins).getByRole('button', { name: 'Invite' }))

    expect(await within(admins).findByText('owner@acme.test')).toBeInTheDocument()
  })

  it('deactivates and reactivates after confirming, updating the active state', async () => {
    platformApi()
    renderApp('/platform/tenants')
    const row = await openTenant('Acme Hardware')

    await userEvent.click(within(row).getByRole('button', { name: 'Deactivate' }))
    await userEvent.click(within(within(row).getByRole('alertdialog', { name: 'Deactivate Acme Hardware?' })).getByRole('button', { name: 'Deactivate' }))
    expect(await within(row).findByText('Deactivated')).toBeInTheDocument()

    await userEvent.click(within(row).getByRole('button', { name: 'Reactivate' }))
    await userEvent.click(within(within(row).getByRole('alertdialog')).getByRole('button', { name: 'Reactivate' }))
    expect(await within(row).findByText('Active')).toBeInTheDocument()
  })

  it('warns that public links break and shows the new slug', async () => {
    platformApi()
    renderApp('/platform/tenants')
    const row = await openTenant('Acme Hardware')
    const section = within(row).getByRole('region', { name: 'Tenant slug' })
    const input = within(section).getByLabelText('Tenant slug')

    await userEvent.clear(input)
    await userEvent.type(input, 'acme-tools')
    expect(input).toHaveAccessibleDescription('Existing public links to /acme will stop working.')
    await userEvent.click(within(section).getByRole('button', { name: 'Change slug' }))

    expect(await within(section).findByText('The public catalog is now at /acme-tools.')).toBeInTheDocument()
    expect(await within(row).findByText('/acme-tools')).toBeInTheDocument()
  })
})

describe("paging a Tenant's Admins", () => {
  it('moves between pages with buttons', async () => {
    platformApi({
      'GET /api/platform/tenants/t1/admins': (url: URL) => {
        const page = Number(url.searchParams.get('page'))
        return json(paged([{ id: `m${page}`, userId: `u${page}`, email: `admin${page}@test` }], page, 20, 25))
      },
    })
    renderApp('/platform/tenants')

    const row = await openTenant('Acme Hardware')
    expect(await within(row).findByText('admin1@test')).toBeInTheDocument()
    await userEvent.click(within(row).getByRole('button', { name: 'Next' }))

    expect(await within(row).findByText('admin2@test')).toBeInTheDocument()
    expect(within(row).getByText('Page 2 of 2')).toBeInTheDocument()
  })
})
