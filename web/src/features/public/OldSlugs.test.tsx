import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { mockApi } from '@/test/api'
import { renderApp } from '@/test/render'

const tenant = { name: 'Acme Hardware', slug: 'acme-tools' }
const drill = { slug: 'power-drill', name: 'Power drill', description: null, images: [], attributes: [] }
const emptyPage = { items: [], page: 1, pageSize: 20, totalCount: 0 }

describe('old addresses', () => {
  it('replaces an old Tenant slug with the current one, keeping the query string', async () => {
    mockApi({
      '/api/public/acme': tenant,
      '/api/public/acme-tools': tenant,
      '/api/public/acme-tools/products': emptyPage,
      '/api/public/acme-tools/categories': [],
      '/api/public/acme-tools/filters': [],
    })

    const router = renderApp('/acme?q=drill&page=2')

    await screen.findByRole('link', { name: 'Acme Hardware' })
    await expect.poll(() => router.state.location.pathname).toBe('/acme-tools')
    expect(router.state.location.search).toBe('?q=drill&page=2')
    expect(router.state.historyAction).toBe('REPLACE')
  })

  it('replaces an old Product slug with the current one, keeping the query string', async () => {
    mockApi({
      '/api/public/acme-tools': tenant,
      '/api/public/acme-tools/products/drill': drill,
      '/api/public/acme-tools/products/power-drill': drill,
    })

    const router = renderApp('/acme-tools/p/drill?ref=list')

    expect(await screen.findByRole('heading', { name: 'Power drill' })).toBeInTheDocument()
    await expect.poll(() => router.state.location.pathname).toBe('/acme-tools/p/power-drill')
    expect(router.state.location.search).toBe('?ref=list')
    expect(router.state.historyAction).toBe('REPLACE')
  })

  it('leaves the address alone when the slugs are current', async () => {
    mockApi({
      '/api/public/acme-tools': tenant,
      '/api/public/acme-tools/products/power-drill': drill,
    })

    const router = renderApp('/acme-tools/p/power-drill')

    expect(await screen.findByRole('heading', { name: 'Power drill' })).toBeInTheDocument()
    expect(router.state.historyAction).toBe('POP')
  })
})
