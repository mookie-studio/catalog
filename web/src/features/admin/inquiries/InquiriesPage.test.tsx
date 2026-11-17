import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { adminSession, noContent } from '@/test/admin'
import { json, lastBody, mockApi, requestsTo } from '@/test/api'
import { renderApp } from '@/test/render'

vi.mock('@/features/auth/session', () => import('@/test/session'))

const product = { id: 'p1', name: 'Hammer drill', slug: 'hammer-drill' }
const ana = { id: 'i1', name: 'Ana', email: 'ana@example.com', message: 'Is it in stock?', status: 'Open', createdAt: '2026-11-17T10:00:00Z', product }
const ben = { id: 'i2', name: 'Ben', email: 'ben@example.com', message: 'Colours?', status: 'Handled', createdAt: '2026-11-16T10:00:00Z', product }

function inboxApi() {
  let inquiries = [ana, ben]
  mockApi({
    ...adminSession,
    'GET /api/admin/inquiries': (url: URL) => {
      const status = url.searchParams.get('status')
      const items = inquiries.filter((i) => !status || i.status === status)
      const pageSize = Number(url.searchParams.get('pageSize') ?? 20)
      return json({ items: items.slice(0, pageSize), page: 1, pageSize, totalCount: items.length })
    },
    'PUT /api/admin/inquiries/i1/status': async (_url: URL, request: Request) => {
      const { status } = (await request.json()) as { status: string }
      inquiries = inquiries.map((i) => (i.id === 'i1' ? { ...i, status } : i))
      return json(inquiries[0])
    },
    'DELETE /api/admin/inquiries/i2': () => {
      inquiries = inquiries.filter((i) => i.id !== 'i2')
      return noContent()
    },
  })
}

describe('admin inquiries', () => {
  it('lists Inquiries with their product and shows the Open count in the navigation', async () => {
    inboxApi()
    renderApp('/admin/inquiries')

    const row = await screen.findByRole('listitem', { name: 'Inquiry from Ana' })
    expect(within(row).getByRole('link', { name: 'Hammer drill' })).toHaveAttribute('href', '/admin/products/p1')
    expect(within(row).getByRole('link', { name: 'ana@example.com' })).toHaveAttribute('href', 'mailto:ana@example.com')
    expect(await screen.findByLabelText('1 open')).toBeInTheDocument()
  })

  it('marks an Inquiry as handled and updates the Open count', async () => {
    inboxApi()
    renderApp('/admin/inquiries')

    const row = await screen.findByRole('listitem', { name: 'Inquiry from Ana' })
    await screen.findByLabelText('1 open')
    await userEvent.click(within(row).getByRole('button', { name: 'Mark handled' }))

    expect(await lastBody('/api/admin/inquiries/i1/status', 'PUT')).toEqual({ status: 'Handled' })
    await vi.waitFor(() => expect(screen.queryByLabelText('1 open')).not.toBeInTheDocument())
    expect(await within(screen.getByRole('listitem', { name: 'Inquiry from Ana' })).findByRole('button', { name: 'Reopen' })).toBeInTheDocument()
  })

  it('filters by status through the URL', async () => {
    inboxApi()
    const router = renderApp('/admin/inquiries')

    await screen.findByRole('listitem', { name: 'Inquiry from Ana' })
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Show' }), 'Handled')

    await vi.waitFor(() => expect(router.state.location.search).toBe('?status=Handled'))
    expect(await screen.findByRole('listitem', { name: 'Inquiry from Ben' })).toBeInTheDocument()
    expect(screen.queryByRole('listitem', { name: 'Inquiry from Ana' })).not.toBeInTheDocument()
  })

  it('deletes an Inquiry after confirming', async () => {
    inboxApi()
    renderApp('/admin/inquiries')

    const row = await screen.findByRole('listitem', { name: 'Inquiry from Ben' })
    await userEvent.click(within(row).getByRole('button', { name: 'Delete' }))
    await userEvent.click(within(within(row).getByRole('alertdialog')).getByRole('button', { name: 'Delete' }))

    await vi.waitFor(() => expect(screen.queryByRole('listitem', { name: 'Inquiry from Ben' })).not.toBeInTheDocument())
    expect(requestsTo('/api/admin/inquiries/i2', 'DELETE')).toHaveLength(1)
  })
})
