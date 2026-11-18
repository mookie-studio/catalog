import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { adminSession } from '@/test/admin'
import { json, mockApi, urlsTo } from '@/test/api'
import { renderApp } from '@/test/render'

vi.mock('@/features/auth/session', () => import('@/test/session'))

const counts = {
  draftCount: 2,
  publishedCount: 7,
  archivedCount: 1,
  featuredCount: 3,
  openInquiryCount: 4,
  publishedWithoutImagesCount: 0,
  publishedWithoutCategoriesCount: 0,
  hasCategories: true,
}

const stat = { productId: 'p1', name: 'Hammer drill', slug: 'hammer-drill', status: 'Published', views: 12, inquiries: 2 }

function dashboardApi(dashboard: Record<string, unknown>, items: unknown[] = []) {
  mockApi({
    ...adminSession,
    '/api/admin/dashboard': { ...counts, ...dashboard },
    '/api/admin/stats/products': () => json({ items, page: 1, pageSize: 10, totalCount: items.length }),
    '/api/admin/inquiries': { items: [], page: 1, pageSize: 1, totalCount: 4 },
  })
}

describe('admin dashboard', () => {
  it('is where the admin area opens and links every count to its filtered list', async () => {
    dashboardApi({})

    const router = renderApp('/admin')

    expect(await screen.findByText('Draft')).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/admin/dashboard')
    const hrefOf = (label: string) => screen.getByText(label).closest('a')!.getAttribute('href')
    expect(hrefOf('Draft')).toBe('/admin/products?status=Draft')
    expect(hrefOf('Published')).toBe('/admin/products?status=Published')
    expect(hrefOf('Archived')).toBe('/admin/products?status=Archived')
    expect(hrefOf('Featured')).toBe('/admin/products?status=Published&featured=true')
    expect(hrefOf('Open inquiries')).toBe('/admin/inquiries?status=Open')
    expect(screen.getByText('Open inquiries').previousElementSibling).toHaveTextContent('4')
    expect(screen.getByText('Everything looks good.')).toBeInTheDocument()
  })

  it('links each warning to the filtered list and hides the ones with count 0', async () => {
    dashboardApi({ publishedWithoutImagesCount: 1, publishedWithoutCategoriesCount: 5 })

    renderApp('/admin/dashboard')

    const section = await screen.findByRole('region', { name: 'Needs attention' })
    expect(within(section).getByRole('link', { name: '1 published product has no images' })).toHaveAttribute(
      'href',
      '/admin/products?status=Published&hasImages=false',
    )
    expect(within(section).getByRole('link', { name: '5 published products have no categories' })).toHaveAttribute(
      'href',
      '/admin/products?status=Published&hasCategories=false',
    )
  })

  it('hides the "no categories" warning when the tenant has no Categories', async () => {
    dashboardApi({ hasCategories: false, publishedWithoutCategoriesCount: 5, publishedWithoutImagesCount: 2 })

    renderApp('/admin/dashboard')

    const section = await screen.findByRole('region', { name: 'Needs attention' })
    expect(within(section).getAllByRole('link')).toHaveLength(1)
    expect(within(section).queryByText(/no categories/)).not.toBeInTheDocument()
  })
})

describe('most viewed products', () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date('2026-11-18T10:00:00Z'))
  })
  afterEach(() => vi.useRealTimers())

  it('lists views and Inquiries and asks for the chosen period as UTC dates', async () => {
    dashboardApi({}, [stat])
    const router = renderApp('/admin/dashboard')

    const row = (await screen.findByRole('link', { name: 'Hammer drill' })).closest('tr')!
    expect(row).toHaveTextContent('Published12 2'.replace(' ', ''))
    expect(screen.getByRole('link', { name: 'Hammer drill' })).toHaveAttribute('href', '/admin/products/p1')
    expect(Object.fromEntries(urlsTo('/api/admin/stats/products').at(-1)!.searchParams)).toMatchObject({
      from: '2026-10-20',
      to: '2026-11-18',
    })

    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Period' }), '7')

    await vi.waitFor(() => expect(router.state.location.search).toBe('?period=7'))
    await vi.waitFor(() =>
      expect(Object.fromEntries(urlsTo('/api/admin/stats/products').at(-1)!.searchParams)).toMatchObject({
        from: '2026-11-12',
        to: '2026-11-18',
      }),
    )
  })

  it('explains an empty period', async () => {
    dashboardApi({}, [])

    renderApp('/admin/dashboard')

    expect(await screen.findByText('No views yet in this period.')).toBeInTheDocument()
    expect(screen.getByText(/without storing anything about visitors/)).toBeInTheDocument()
  })
})
