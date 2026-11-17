import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { adminSession } from '@/test/admin'
import { json, lastBody, mockApi, problem } from '@/test/api'
import { renderApp } from '@/test/render'

vi.mock('@/features/auth/session', () => import('@/test/session'))

const empty = {
  name: 'Acme',
  slug: 'acme',
  description: null,
  contactEmail: null,
  phone: null,
  address: null,
  websiteUrl: null,
  brandColor: null,
  logoUrl: null,
}

describe('admin profile', () => {
  it('saves the profile', async () => {
    mockApi({
      ...adminSession,
      'GET /api/admin/tenant/profile': () => json(empty),
      'PUT /api/admin/tenant/profile': async (_url: URL, request: Request) => json({ ...empty, ...((await request.json()) as object) }),
    })
    renderApp('/admin/profile')

    await userEvent.type(await screen.findByLabelText('Description'), 'Tools for every job.')
    await userEvent.type(screen.getByLabelText('Brand color'), '#112233')
    await userEvent.click(screen.getByRole('button', { name: 'Save profile' }))

    expect(await screen.findByRole('status')).toHaveTextContent('Saved.')
    expect(await lastBody('/api/admin/tenant/profile', 'PUT')).toEqual({
      description: 'Tools for every job.',
      contactEmail: '',
      phone: '',
      address: '',
      websiteUrl: '',
      brandColor: '#112233',
    })
  })

  it('shows server validation errors next to their fields', async () => {
    mockApi({
      ...adminSession,
      'GET /api/admin/tenant/profile': () => json(empty),
      'PUT /api/admin/tenant/profile': () => problem(400, { websiteUrl: ['The website must be an absolute http or https URL.'] }),
    })
    renderApp('/admin/profile')

    await userEvent.type(await screen.findByLabelText('Website'), 'acme.example')
    await userEvent.click(screen.getByRole('button', { name: 'Save profile' }))

    expect(await screen.findByLabelText('Website')).toHaveAccessibleDescription('The website must be an absolute http or https URL.')
  })
})
