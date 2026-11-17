import { fireEvent, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { adminSession, created, noContent, product } from '@/test/admin'
import { json, lastBody, mockApi, requestsTo } from '@/test/api'
import { renderApp } from '@/test/render'

vi.mock('@/features/auth/session', () => import('@/test/session'))

const categories = [{ id: 'tools', name: 'Tools', children: [{ id: 'drills', name: 'Drills', children: [] }] }]
const image = (id: string, position: number) => ({
  id,
  url: `https://images.test/${id}.jpg`,
  thumbnailUrl: `https://images.test/${id}-thumb.webp`,
  position,
})

function productApi(overrides: Record<string, unknown> = {}, routes: Record<string, unknown> = {}) {
  mockApi({
    ...adminSession,
    'GET /api/admin/products/p1': () => json(product(overrides)),
    'GET /api/admin/categories': categories,
    'GET /api/admin/attribute-definitions': [],
    ...routes,
  })
}

const file = (name: string, type: string, bytes = 1024) => new File([new Uint8Array(bytes)], name, { type })

describe('product categories', () => {
  it('shows the selected Categories and saves the new selection', async () => {
    productApi({ categoryIds: ['tools'] }, { 'PUT /api/admin/products/p1/categories': () => json(product({ categoryIds: ['drills'] })) })
    renderApp('/admin/products/p1')
    const section = await screen.findByRole('region', { name: 'Categories' })

    expect(within(section).getByLabelText('Tools')).toBeChecked()
    await userEvent.click(within(section).getByLabelText('Tools'))
    await userEvent.click(within(section).getByLabelText('Drills'))
    await userEvent.click(within(section).getByRole('button', { name: 'Save categories' }))

    expect(await within(section).findByText('Categories saved')).toBeInTheDocument()
    expect(await lastBody('/api/admin/products/p1/categories', 'PUT')).toEqual({ categoryIds: ['drills'] })
  })

  it('saves a product with no Categories', async () => {
    productApi({ categoryIds: ['drills'] }, { 'PUT /api/admin/products/p1/categories': () => json(product()) })
    renderApp('/admin/products/p1')
    const section = await screen.findByRole('region', { name: 'Categories' })

    await userEvent.click(within(section).getByLabelText('Drills'))
    await userEvent.click(within(section).getByRole('button', { name: 'Save categories' }))

    await within(section).findByText('Categories saved')
    expect(await lastBody('/api/admin/products/p1/categories', 'PUT')).toEqual({ categoryIds: [] })
  })
})

describe('product images', () => {
  it('shows the images in order with the first labelled Main image', async () => {
    productApi({ images: [image('b', 1), image('a', 0)] })
    renderApp('/admin/products/p1')
    const section = await screen.findByRole('region', { name: 'Images' })

    const items = within(section).getAllByRole('img').map((img) => img.getAttribute('src'))
    expect(items).toEqual(['https://images.test/a-thumb.webp', 'https://images.test/b-thumb.webp'])
    expect(within(within(section).getByRole('listitem', { name: 'Image 1' })).getByText('Main image')).toBeInTheDocument()
  })

  it('uploads a valid image', async () => {
    productApi({}, { 'POST /api/admin/products/p1/images': () => created(image('a', 0)) })
    renderApp('/admin/products/p1')
    const section = await screen.findByRole('region', { name: 'Images' })

    await userEvent.upload(within(section).getByLabelText('Upload image'), file('photo.webp', 'image/webp'))

    await vi.waitFor(() => expect(requestsTo('/api/admin/products/p1/images', 'POST')).toHaveLength(1))
    const body = await requestsTo('/api/admin/products/p1/images', 'POST')[0].formData()
    expect((body.get('file') as File).size).toBe(1024)
  })

  it.each([
    ['a wrong type', file('doc.gif', 'image/gif'), 'Only JPG, PNG and WebP images are allowed.'],
    ['an oversized file', file('big.jpg', 'image/jpeg', 2 * 1024 * 1024 + 1), 'The image must be at most 2 MB.'],
  ])('blocks %s before uploading', async (_case, bad, message) => {
    productApi()
    renderApp('/admin/products/p1')
    const section = await screen.findByRole('region', { name: 'Images' })

    fireEvent.change(within(section).getByLabelText('Upload image'), { target: { files: [bad] } })

    expect(await within(section).findByText(message)).toBeInTheDocument()
    expect(requestsTo('/api/admin/products/p1/images', 'POST')).toHaveLength(0)
  })

  it('blocks a 6th image before uploading', async () => {
    productApi({ images: ['a', 'b', 'c', 'd', 'e'].map(image) })
    renderApp('/admin/products/p1')
    const section = await screen.findByRole('region', { name: 'Images' })

    await userEvent.upload(within(section).getByLabelText('Upload image'), file('photo.png', 'image/png'))

    expect(await within(section).findByText('A product can have at most 5 images.')).toBeInTheDocument()
    expect(requestsTo('/api/admin/products/p1/images', 'POST')).toHaveLength(0)
  })

  it('reorders with the new list of image ids', async () => {
    productApi({ images: [image('a', 0), image('b', 1), image('c', 2)] }, { 'PUT /api/admin/products/p1/images/order': () => noContent() })
    renderApp('/admin/products/p1')
    const section = await screen.findByRole('region', { name: 'Images' })

    expect(within(section).getByRole('button', { name: 'Move image 1 left' })).toBeDisabled()
    await userEvent.click(within(section).getByRole('button', { name: 'Move image 3 left' }))

    await vi.waitFor(() => expect(requestsTo('/api/admin/products/p1/images/order', 'PUT')).toHaveLength(1))
    expect(await lastBody('/api/admin/products/p1/images/order', 'PUT')).toEqual({ imageIds: ['a', 'c', 'b'] })
  })

  it('deletes an image', async () => {
    productApi({ images: [image('a', 0)] }, { 'DELETE /api/admin/products/p1/images/a': () => noContent() })
    renderApp('/admin/products/p1')

    await userEvent.click(within(await screen.findByRole('region', { name: 'Images' })).getByRole('button', { name: 'Delete image 1' }))

    await vi.waitFor(() => expect(requestsTo('/api/admin/products/p1/images/a', 'DELETE')).toHaveLength(1))
  })
})
