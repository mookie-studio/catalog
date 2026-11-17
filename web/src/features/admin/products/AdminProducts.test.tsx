import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { adminSession, created, noContent, product } from '@/test/admin'
import { json, lastBody, mockApi, problem, requestsTo, urlsTo } from '@/test/api'
import { renderApp } from '@/test/render'

vi.mock('@/features/auth/session', () => import('@/test/session'))

const categories = [{ id: 'c1', name: 'Tools', children: [{ id: 'c2', name: 'Drills', children: [] }] }]

function listApi(items: unknown[], totalCount = items.length) {
  mockApi({
    ...adminSession,
    '/api/admin/categories': categories,
    'GET /api/admin/products': (url: URL) =>
      json({ items, page: Number(url.searchParams.get('page')), pageSize: 20, totalCount }),
  })
}

const lastListRequest = () => urlsTo('/api/admin/products').at(-1)!

describe('admin product list', () => {
  it('shows name, SKU, status and last update of each product', async () => {
    listApi([{ id: 'p1', name: 'Hammer drill', sku: 'HD-1', slug: 'hammer-drill', status: 'Published', updatedAt: '2026-11-16T10:00:00Z' }])

    renderApp('/admin/products')

    const row = (await screen.findByRole('link', { name: 'Hammer drill' })).closest('tr')!
    expect(within(row).getByText('HD-1')).toBeInTheDocument()
    expect(within(row).getByText('Published')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Hammer drill' })).toHaveAttribute('href', '/admin/products/p1')
    expect(screen.getByRole('navigation', { name: 'Admin' })).toHaveTextContent('ProductsAttributesCategoriesInquiriesTeamProfile')
  })

  it('reflects search, status, category and page in the URL and the API calls', async () => {
    listApi([{ id: 'p1', name: 'Hammer drill', sku: 'HD-1', slug: 'hammer-drill', status: 'Draft', updatedAt: '2026-11-16T10:00:00Z' }], 45)
    const router = renderApp('/admin/products')
    await screen.findByRole('link', { name: 'Hammer drill' })

    await userEvent.type(screen.getByRole('searchbox', { name: 'Search products' }), 'drill')
    await userEvent.click(screen.getByRole('button', { name: 'Search' }))
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Status' }), 'Published')
    await userEvent.selectOptions(await screen.findByRole('combobox', { name: 'Category' }), 'c2')
    await userEvent.click(await screen.findByRole('link', { name: 'Next' }))

    expect(router.state.location.search).toBe('?q=drill&status=Published&categoryId=c2&page=2')
    await screen.findByText('Page 2 of 3')
    const request = lastListRequest()
    expect(Object.fromEntries(request.searchParams)).toEqual({ page: '2', q: 'drill', status: 'Published', categoryId: 'c2' })
  })

  it('shows an empty state when there are no products', async () => {
    listApi([])

    renderApp('/admin/products')

    expect(await screen.findByText('No products yet')).toBeInTheDocument()
  })
})

describe('creating a product', () => {
  it('creates a Draft and opens its edit page', async () => {
    mockApi({
      ...adminSession,
      'POST /api/admin/products': () => created(product()),
      'GET /api/admin/products/p1': product(),
    })
    const router = renderApp('/admin/products/new')

    await userEvent.type(await screen.findByLabelText('Name'), 'Hammer drill')
    await userEvent.type(screen.getByLabelText('SKU'), 'HD-1')
    await userEvent.type(screen.getByLabelText('Description'), 'A powerful drill.')
    await userEvent.click(screen.getByRole('button', { name: 'Create product' }))

    expect(await screen.findByRole('heading', { name: 'Hammer drill' })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/admin/products/p1')
    expect(await lastBody('/api/admin/products', 'POST')).toEqual({ name: 'Hammer drill', sku: 'HD-1', description: 'A powerful drill.' })
  })

  it('shows a taken SKU next to the SKU field', async () => {
    mockApi({ ...adminSession, 'POST /api/admin/products': () => problem(409, { sku: ['The SKU is already in use in this Tenant.'] }) })
    renderApp('/admin/products/new')

    await userEvent.type(await screen.findByLabelText('Name'), 'Hammer drill')
    await userEvent.type(screen.getByLabelText('SKU'), 'HD-1')
    await userEvent.click(screen.getByRole('button', { name: 'Create product' }))

    expect(await screen.findByText('The SKU is already in use in this Tenant.')).toBeInTheDocument()
    expect(screen.getByLabelText('SKU')).toHaveAccessibleDescription('The SKU is already in use in this Tenant.')
  })
})

describe('editing a product', () => {
  it('saves name, description, SKU and Product slug', async () => {
    mockApi({
      ...adminSession,
      'GET /api/admin/products/p1': product(),
      'PUT /api/admin/products/p1': () => json(product({ name: 'Hammer drill 500W', slug: 'hammer-drill-500w' })),
    })
    renderApp('/admin/products/p1')

    const name = await screen.findByLabelText('Name')
    await userEvent.clear(name)
    await userEvent.type(name, 'Hammer drill 500W')
    await userEvent.clear(screen.getByLabelText('Product slug'))
    await userEvent.type(screen.getByLabelText('Product slug'), 'hammer-drill-500w')
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('Saved')).toBeInTheDocument()
    expect(await lastBody('/api/admin/products/p1', 'PUT')).toEqual({
      name: 'Hammer drill 500W',
      sku: 'HD-1',
      slug: 'hammer-drill-500w',
      description: 'A powerful drill.',
    })
  })

  it('shows a taken Product slug next to the slug field', async () => {
    mockApi({
      ...adminSession,
      'GET /api/admin/products/p1': product(),
      'PUT /api/admin/products/p1': () => problem(409, { slug: ['The Product slug is already in use in this Tenant.'] }),
    })
    renderApp('/admin/products/p1')

    await userEvent.click(await screen.findByRole('button', { name: 'Save' }))

    expect(await screen.findByLabelText('Product slug')).toHaveAccessibleDescription('The Product slug is already in use in this Tenant.')
  })

  it('shows the description error when publishing without a description', async () => {
    mockApi({
      ...adminSession,
      'GET /api/admin/products/p1': product({ description: null }),
      'POST /api/admin/products/p1/status': () => problem(400, { description: ['A description is required to publish.'] }),
    })
    renderApp('/admin/products/p1')

    await userEvent.click(await screen.findByRole('button', { name: 'Publish' }))

    expect(await screen.findByLabelText('Description')).toHaveAccessibleDescription('A description is required to publish.')
    expect(await lastBody('/api/admin/products/p1/status', 'POST')).toEqual({ status: 'Published' })
  })

  it.each([
    ['Draft', ['Publish', 'Archive']],
    ['Published', ['Move to draft', 'Archive']],
    ['Archived', ['Restore']],
  ])('offers the status actions of a %s product', async (status, actions) => {
    mockApi({ ...adminSession, 'GET /api/admin/products/p1': product({ status }) })

    renderApp('/admin/products/p1')

    const section = await screen.findByRole('region', { name: 'Status' })
    expect(within(section).getAllByRole('button').map((b) => b.textContent)).toEqual(actions)
  })

  it('changes the status and shows the new one', async () => {
    let status = 'Published'
    mockApi({
      ...adminSession,
      'GET /api/admin/products/p1': () => json(product({ status })),
      'POST /api/admin/products/p1/status': async (_url: URL, request: Request) => {
        status = ((await request.json()) as { status: string }).status
        return noContent()
      },
    })
    renderApp('/admin/products/p1')

    await userEvent.click(await screen.findByRole('button', { name: 'Archive' }))

    expect(await screen.findByRole('button', { name: 'Restore' })).toBeInTheDocument()
    expect(requestsTo('/api/admin/products/p1/status', 'POST')).toHaveLength(1)
  })

  it('shows the not-found page for a product of another tenant', async () => {
    mockApi(adminSession)

    renderApp('/admin/products/other')

    expect(await screen.findByRole('heading', { name: 'Page not found' })).toBeInTheDocument()
  })
})
