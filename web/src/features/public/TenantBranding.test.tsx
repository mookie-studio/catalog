import { screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { mockApi } from '@/test/api'
import { renderApp } from '@/test/render'

const plainTenant = {
  name: 'Acme Hardware',
  slug: 'acme',
  description: null,
  contactEmail: null,
  phone: null,
  address: null,
  websiteUrl: null,
  brandColor: null,
  logoUrl: null,
}

function catalog(tenant: Record<string, unknown>) {
  mockApi({
    '/api/public/acme': { ...plainTenant, ...tenant },
    '/api/public/acme/products': { items: [], page: 1, pageSize: 20, totalCount: 0 },
    '/api/public/acme/filters': [],
    '/api/public/acme/categories': [],
  })
}

describe('tenant branding', () => {
  it('shows the Logo and the Brand color in the header', async () => {
    catalog({ logoUrl: 'https://images.test/logo.png', brandColor: '#000080' })

    renderApp('/acme')

    const home = await screen.findByRole('link', { name: 'Acme Hardware' })
    expect(within(home).getByRole('presentation')).toHaveAttribute('src', 'https://images.test/logo.png')
    const root = home.closest('header')!.parentElement!
    expect(root.style.getPropertyValue('--brand')).toBe('#000080')
    expect(root.style.getPropertyValue('--brand-foreground')).toBe('#FFFFFF')
  })

  it('keeps the default look without a profile', async () => {
    catalog({})

    renderApp('/acme')

    const home = await screen.findByRole('link', { name: 'Acme Hardware' })
    expect(home.closest('header')!.parentElement!.getAttribute('style')).toBeNull()
    expect(screen.queryByRole('contentinfo')).not.toBeInTheDocument()
  })

  it('shows the description and contact details with the right links', async () => {
    catalog({
      description: 'Tools for every job.',
      contactEmail: 'hello@acme.example',
      phone: '+34 600 000 000',
      address: 'Main Street 1',
      websiteUrl: 'https://acme.example',
    })

    renderApp('/acme')

    const about = await screen.findByRole('contentinfo', { name: 'About Acme Hardware' })
    expect(within(about).getByText('Tools for every job.')).toBeInTheDocument()
    expect(within(about).getByRole('link', { name: 'hello@acme.example' })).toHaveAttribute('href', 'mailto:hello@acme.example')
    expect(within(about).getByRole('link', { name: '+34 600 000 000' })).toHaveAttribute('href', 'tel:+34600000000')
    const website = within(about).getByRole('link', { name: 'acme.example' })
    expect(website).toHaveAttribute('href', 'https://acme.example')
    expect(website).toHaveAttribute('rel', 'noopener noreferrer')
    expect(within(about).getByText('Main Street 1')).toBeInTheDocument()
  })
})
