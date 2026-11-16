import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { mockApi, urlsTo } from '@/test/api'
import { renderApp } from '@/test/render'

const tenant = { name: 'Acme Hardware', slug: 'acme' }
const drill = { slug: 'hammer-drill', name: 'Hammer drill', mainImageUrl: 'https://images.test/drill.jpg' }
const saw = { slug: 'circular-saw', name: 'Circular saw', mainImageUrl: null }

const colorId = '0198a000-0000-7000-8000-000000000001'
const red = '0198a000-0000-7000-8000-0000000000a1'
const blue = '0198a000-0000-7000-8000-0000000000a2'
const cordlessId = '0198a000-0000-7000-8000-000000000002'
const toolsId = '0198a000-0000-7000-8000-0000000000c1'
const drillsId = '0198a000-0000-7000-8000-0000000000c2'

const filters = [
  { definitionId: colorId, name: 'Color', type: 'OptionList', allowsMultiple: false, options: [{ id: red, value: 'Red' }, { id: blue, value: 'Blue' }] },
  { definitionId: cordlessId, name: 'Cordless', type: 'Boolean', allowsMultiple: false, options: null },
]
const categories = [{ id: toolsId, name: 'Tools', children: [{ id: drillsId, name: 'Drills', children: [] }] }]

function catalog(options: { totalCount?: number; categories?: unknown[] } = {}) {
  mockApi({
    '/api/public/acme': tenant,
    '/api/public/acme/products': (url: URL) =>
      Response.json({ items: [drill, saw], page: Number(url.searchParams.get('page')), pageSize: 20, totalCount: options.totalCount ?? 2 }),
    '/api/public/acme/filters': filters,
    '/api/public/acme/categories': options.categories ?? categories,
  })
}

const lastProductsRequest = () => urlsTo('/api/public/acme/products').at(-1)!

describe('public product list', () => {
  it('shows the tenant name and a card per product linking to its page', async () => {
    catalog()

    renderApp('/acme')

    expect(await screen.findByRole('link', { name: 'Acme Hardware' })).toBeInTheDocument()
    expect(await screen.findByRole('link', { name: /Hammer drill/ })).toHaveAttribute('href', '/acme/p/hammer-drill')
    expect(screen.getByRole('img', { name: 'Hammer drill' })).toHaveAttribute('src', 'https://images.test/drill.jpg')
  })

  it('shows the placeholder for a product without images', async () => {
    catalog()

    renderApp('/acme')

    expect(await screen.findByRole('img', { name: 'Circular saw' })).toHaveAttribute('src', expect.stringContaining('placeholder'))
  })

  it('keeps the search in the URL and starts again from the first page', async () => {
    catalog({ totalCount: 60 })
    const router = renderApp('/acme?page=2')
    await screen.findByRole('link', { name: /Hammer drill/ })

    await userEvent.type(screen.getByRole('searchbox', { name: 'Search products' }), 'drill')
    await userEvent.click(screen.getByRole('button', { name: 'Search' }))

    await screen.findByText('Page 1 of 3')
    expect(router.state.location.search).toBe('?q=drill')
    expect(lastProductsRequest().searchParams.get('q')).toBe('drill')
    expect(lastProductsRequest().searchParams.get('page')).toBe('1')
  })

  it('keeps the page in the URL', async () => {
    catalog({ totalCount: 45 })
    const router = renderApp('/acme?q=drill')

    await userEvent.click(await screen.findByRole('link', { name: 'Next' }))

    expect(await screen.findByText('Page 2 of 3')).toBeInTheDocument()
    expect(router.state.location.search).toBe('?q=drill&page=2')
    expect(lastProductsRequest().searchParams.get('page')).toBe('2')
    expect(lastProductsRequest().searchParams.get('q')).toBe('drill')
  })

  it('shows the not-found page for an unknown or deactivated tenant', async () => {
    mockApi({})

    renderApp('/nobody')

    expect(await screen.findByRole('heading', { name: 'Page not found' })).toBeInTheDocument()
  })
})

describe('public filters', () => {
  it('updates the URL and the list when filters are selected', async () => {
    catalog()
    const router = renderApp('/acme')

    await userEvent.click(await screen.findByRole('checkbox', { name: 'Red' }))
    await userEvent.click(screen.getByRole('checkbox', { name: 'Blue' }))
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Cordless' }), 'Yes')

    const params = new URLSearchParams(router.state.location.search)
    expect(params.getAll(`attr.${colorId}`)).toEqual([red, blue])
    expect(params.get(`attr.${cordlessId}`)).toBe('true')
    expect(lastProductsRequest().searchParams.getAll(`attr.${colorId}`)).toEqual([red, blue])
    expect(lastProductsRequest().searchParams.get(`attr.${cordlessId}`)).toBe('true')
  })

  it('restores the filters from the URL', async () => {
    catalog()

    renderApp(`/acme?q=drill&attr.${colorId}=${blue}&attr.${cordlessId}=false`)

    expect(await screen.findByRole('checkbox', { name: 'Blue' })).toBeChecked()
    expect(screen.getByRole('checkbox', { name: 'Red' })).not.toBeChecked()
    expect(screen.getByRole('combobox', { name: 'Cordless' })).toHaveValue('false')
    expect(lastProductsRequest().searchParams.get(`attr.${colorId}`)).toBe(blue)
    expect(lastProductsRequest().searchParams.get('q')).toBe('drill')
  })

  it('applies a text filter when the visitor presses Enter', async () => {
    const materialId = '0198a000-0000-7000-8000-000000000003'
    mockApi({
      '/api/public/acme': tenant,
      '/api/public/acme/products': { items: [], page: 1, pageSize: 20, totalCount: 0 },
      '/api/public/acme/filters': [{ definitionId: materialId, name: 'Material', type: 'Text', allowsMultiple: false, options: null }],
      '/api/public/acme/categories': [],
    })
    const router = renderApp('/acme')

    await userEvent.type(await screen.findByRole('textbox', { name: 'Material' }), 'steel{Enter}')

    expect(new URLSearchParams(router.state.location.search).get(`attr.${materialId}`)).toBe('steel')
  })
})

describe('public category navigation', () => {
  it('filters the list by the selected category and keeps the search', async () => {
    catalog()
    const router = renderApp('/acme?q=drill')
    const nav = await screen.findByRole('navigation', { name: 'Categories' })

    await userEvent.click(within(nav).getByRole('button', { name: 'Drills' }))

    expect(router.state.location.search).toBe(`?q=drill&categoryId=${drillsId}`)
    expect(within(nav).getByRole('button', { name: 'Drills' })).toHaveAttribute('aria-current', 'true')
    expect(lastProductsRequest().searchParams.get('categoryId')).toBe(drillsId)
    expect(lastProductsRequest().searchParams.get('q')).toBe('drill')
  })

  it('shows no navigation when the tenant has no categories', async () => {
    catalog({ categories: [] })

    renderApp('/acme')

    await screen.findByRole('link', { name: /Hammer drill/ })
    expect(screen.queryByRole('navigation', { name: 'Categories' })).not.toBeInTheDocument()
  })
})
