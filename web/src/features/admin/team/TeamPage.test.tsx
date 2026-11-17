import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { adminSession, created, noContent } from '@/test/admin'
import { json, lastBody, mockApi, paged, problem, requestsTo } from '@/test/api'
import { renderApp } from '@/test/render'

vi.mock('@/features/auth/session', () => import('@/test/session'))

const me = { id: 'm1', userId: 'u1', email: 'admin@test' }
const colleague = { id: 'm2', userId: 'u2', email: 'colleague@test' }

function teamApi(routes: Record<string, unknown> = {}) {
  let admins = [me, colleague]
  mockApi({
    ...adminSession,
    'GET /api/admin/admins': () => json(paged(admins)),
    'POST /api/admin/admins': async (_url: URL, request: Request) => {
      const { email } = (await request.json()) as { email: string }
      const admin = { id: 'm3', userId: 'u3', email }
      admins = [...admins, admin]
      return created(admin)
    },
    'DELETE /api/admin/admins/m2': () => {
      admins = admins.filter((a) => a.id !== 'm2')
      return noContent()
    },
    ...routes,
  })
}

describe('admin team', () => {
  it('invites an Admin and shows them in the list', async () => {
    teamApi()
    renderApp('/admin/team')

    await userEvent.type(await screen.findByLabelText('Email'), 'new@test')
    await userEvent.click(screen.getByRole('button', { name: 'Invite' }))

    expect(await within(screen.getByRole('list', { name: 'Admins' })).findByRole('listitem', { name: 'new@test' })).toBeInTheDocument()
    expect(await lastBody('/api/admin/admins', 'POST')).toEqual({ email: 'new@test' })
  })

  it('shows an email that is already an Admin next to the email field', async () => {
    teamApi({ 'POST /api/admin/admins': () => problem(409, { email: ['This email is already an Admin of a Tenant.'] }) })
    renderApp('/admin/team')

    await userEvent.type(await screen.findByLabelText('Email'), 'colleague@test')
    await userEvent.click(screen.getByRole('button', { name: 'Invite' }))

    expect(await screen.findByLabelText('Email')).toHaveAccessibleDescription('This email is already an Admin of a Tenant.')
  })

  it('offers no remove action on your own row', async () => {
    teamApi()
    renderApp('/admin/team')

    const own = await screen.findByRole('listitem', { name: 'admin@test' })
    expect(within(own).queryByRole('button', { name: 'Remove' })).not.toBeInTheDocument()
    expect(within(screen.getByRole('listitem', { name: 'colleague@test' })).getByRole('button', { name: 'Remove' })).toBeInTheDocument()
  })

  it('removes a colleague after confirming', async () => {
    teamApi()
    renderApp('/admin/team')

    const row = await screen.findByRole('listitem', { name: 'colleague@test' })
    await userEvent.click(within(row).getByRole('button', { name: 'Remove' }))
    await userEvent.click(within(within(row).getByRole('alertdialog')).getByRole('button', { name: 'Remove' }))

    await vi.waitFor(() => expect(screen.queryByRole('listitem', { name: 'colleague@test' })).not.toBeInTheDocument())
    expect(requestsTo('/api/admin/admins/m2', 'DELETE')).toHaveLength(1)
  })
})

describe('paging the team', () => {
  it('requests the page in the URL and shows the pagination', async () => {
    mockApi({
      ...adminSession,
      'GET /api/admin/admins': (url: URL) => json(paged([colleague], Number(url.searchParams.get('page')), 20, 25)),
    })

    renderApp('/admin/team?page=2')

    expect(await screen.findByText('Page 2 of 2')).toBeInTheDocument()
    expect(requestsTo('/api/admin/admins').at(-1)!.url).toContain('page=2')
    expect(screen.getByRole('link', { name: 'Previous' })).toHaveAttribute('href', '/admin/team')
  })
})
