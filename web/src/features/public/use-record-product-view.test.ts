import { renderHook } from '@testing-library/react'
import { StrictMode } from 'react'
import { describe, expect, it } from 'vitest'
import { fetchMock, mockApi, requestsTo } from '@/test/api'
import { useRecordProductView } from '@/features/public/use-record-product-view'

const path = (slug: string) => `/api/public/acme/products/${slug}/views`
const wrapper = StrictMode

describe('useRecordProductView', () => {
  it('records one view per product shown, even when effects run twice or the component re-renders', async () => {
    mockApi({})
    const { rerender } = renderHook(({ slug }) => useRecordProductView('acme', slug), {
      initialProps: { slug: 'drill' as string | undefined },
      wrapper,
    })

    rerender({ slug: 'drill' })
    await expect.poll(() => requestsTo(path('drill'), 'POST')).toHaveLength(1)

    rerender({ slug: 'saw' })
    await expect.poll(() => requestsTo(path('saw'), 'POST')).toHaveLength(1)
    expect(requestsTo(path('drill'), 'POST')).toHaveLength(1)
  })

  it('records nothing until the product has loaded', () => {
    mockApi({})
    fetchMock.mockClear()

    renderHook(() => useRecordProductView('acme', undefined))

    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('ignores failures', async () => {
    fetchMock.mockImplementation(async () => new Response(null, { status: 429 }))

    renderHook(() => useRecordProductView('acme', 'drill'))

    await expect.poll(() => requestsTo(path('drill'), 'POST')).toHaveLength(1)
  })
})
