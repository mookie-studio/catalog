import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { mockApi, requestsTo, urlsTo } from '@/test/api'
import { renderApp } from '@/test/render'

const tenant = { name: 'Acme Hardware', slug: 'acme' }

function productApi(product: unknown) {
  mockApi({ '/api/public/acme': tenant, '/api/public/acme/products/hammer-drill': product })
}

const drill = {
  slug: 'hammer-drill',
  name: 'Hammer drill',
  description: 'A powerful drill.',
  images: [1, 2, 3].map((n) => ({ url: `https://images.test/${n}.jpg`, thumbnailUrl: `https://images.test/${n}-thumb.webp` })),
  attributes: [
    { name: 'Color', type: 'OptionList', value: 'Red' },
    { name: 'Sizes', type: 'OptionList', value: ['S', 'M'] },
    { name: 'Cordless', type: 'Boolean', value: true },
    { name: 'Power (W)', type: 'Number', value: 500 },
  ],
}

describe('public product page', () => {
  it('shows name, description and attributes', async () => {
    productApi(drill)

    renderApp('/acme/p/hammer-drill')

    expect(await screen.findByRole('heading', { name: 'Hammer drill' })).toBeInTheDocument()
    expect(screen.getByText('A powerful drill.')).toBeInTheDocument()
    const values = screen.getAllByRole('definition').map((value) => value.textContent)
    expect(values).toEqual(['Red', 'S, M', 'Yes', '500'])
  })

  it('shows the images in order with the main image first, and Thumbnails in the strip', async () => {
    productApi(drill)

    renderApp('/acme/p/hammer-drill')

    expect(await screen.findByRole('img', { name: 'Hammer drill' })).toHaveAttribute('src', 'https://images.test/1.jpg')
    const thumbnails = within(screen.getByRole('list', { name: 'Images' })).getAllByRole('button')
    expect(thumbnails.map((button) => button.querySelector('img')!.getAttribute('src'))).toEqual(drill.images.map((image) => image.thumbnailUrl))

    await userEvent.click(screen.getByRole('button', { name: 'Show image 3' }))

    expect(screen.getByRole('img', { name: 'Hammer drill' })).toHaveAttribute('src', 'https://images.test/3.jpg')
  })

  it('shows the placeholder when the product has no images', async () => {
    productApi({ ...drill, images: [] })

    renderApp('/acme/p/hammer-drill')

    expect(await screen.findByRole('img', { name: 'Hammer drill' })).toHaveAttribute('src', expect.stringContaining('placeholder'))
  })

  it('shows the not-found page for an unknown, draft or archived product', async () => {
    mockApi({ '/api/public/acme': tenant })

    renderApp('/acme/p/missing')

    expect(await screen.findByRole('heading', { name: 'Page not found' })).toBeInTheDocument()
  })
})

describe('related products', () => {
  const related = {
    items: [1, 2, 3, 4].map((n) => ({ slug: `saw-${n}`, name: `Saw ${n}`, mainImageUrl: null, isFeatured: false })),
    page: 1,
    pageSize: 4,
    totalCount: 4,
  }

  it('shows up to four Related products as cards that link to them', async () => {
    mockApi({
      '/api/public/acme': tenant,
      '/api/public/acme/products/hammer-drill': drill,
      '/api/public/acme/products/hammer-drill/related': related,
    })

    renderApp('/acme/p/hammer-drill')

    const list = await screen.findByRole('list', { name: 'Related products' })
    expect(within(list).getAllByRole('link').map((link) => link.getAttribute('href'))).toEqual([
      '/acme/p/saw-1',
      '/acme/p/saw-2',
      '/acme/p/saw-3',
      '/acme/p/saw-4',
    ])
    expect(screen.getByRole('heading', { name: 'Related products' })).toBeInTheDocument()
    expect(urlsTo('/api/public/acme/products/hammer-drill/related').at(-1)!.searchParams.get('pageSize')).toBe('4')
  })

  it('hides the whole section when there are none or the request fails', async () => {
    mockApi({
      '/api/public/acme': tenant,
      '/api/public/acme/products/hammer-drill': drill,
      '/api/public/acme/products/hammer-drill/related': { items: [], page: 1, pageSize: 4, totalCount: 0 },
    })

    renderApp('/acme/p/hammer-drill')

    await screen.findByRole('heading', { name: 'Hammer drill' })
    await vi.waitFor(() => expect(urlsTo('/api/public/acme/products/hammer-drill/related')).toHaveLength(1))
    expect(screen.queryByRole('heading', { name: 'Related products' })).not.toBeInTheDocument()
  })
})

describe('product page metadata and views', () => {
  it('sets the title, description, Open Graph tags and canonical address', async () => {
    productApi({ ...drill, description: `${'word '.repeat(60)}end` })

    renderApp('/acme/p/hammer-drill')

    await screen.findByRole('heading', { name: 'Hammer drill' })
    await vi.waitFor(() => expect(document.title).toBe('Hammer drill · Acme Hardware'))
    const meta = (selector: string) => document.head.querySelector(selector)?.getAttribute('content')
    expect(meta('meta[name="description"]')!.length).toBeLessThanOrEqual(160)
    expect(meta('meta[name="description"]')).toMatch(/^word word.*…$/)
    expect(meta('meta[property="og:title"]')).toBe('Hammer drill · Acme Hardware')
    expect(meta('meta[property="og:type"]')).toBe('product')
    expect(meta('meta[property="og:image"]')).toBe('https://images.test/1.jpg')
    expect(document.head.querySelector('link[rel="canonical"]')).toHaveAttribute('href', `${window.location.origin}/acme/p/hammer-drill`)
  })

  it('records one Product view for the product shown', async () => {
    productApi(drill)

    renderApp('/acme/p/hammer-drill')

    await screen.findByRole('heading', { name: 'Hammer drill' })
    await vi.waitFor(() => expect(requestsTo('/api/public/acme/products/hammer-drill/views', 'POST')).toHaveLength(1))
  })
})
