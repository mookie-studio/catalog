import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { mockApi } from '@/test/api'
import { renderApp } from '@/test/render'

const tenant = { name: 'Acme Hardware', slug: 'acme' }

function productApi(product: unknown) {
  mockApi({ '/api/public/acme': tenant, '/api/public/acme/products/hammer-drill': product })
}

const drill = {
  slug: 'hammer-drill',
  name: 'Hammer drill',
  description: 'A powerful drill.',
  images: ['https://images.test/1.jpg', 'https://images.test/2.jpg', 'https://images.test/3.jpg'],
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

  it('shows the images in order with the main image first', async () => {
    productApi(drill)

    renderApp('/acme/p/hammer-drill')

    expect(await screen.findByRole('img', { name: 'Hammer drill' })).toHaveAttribute('src', 'https://images.test/1.jpg')
    const thumbnails = within(screen.getByRole('list', { name: 'Images' })).getAllByRole('button')
    expect(thumbnails.map((button) => button.querySelector('img')!.getAttribute('src'))).toEqual(drill.images)

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
