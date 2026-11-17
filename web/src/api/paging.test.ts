import { describe, expect, it, vi } from 'vitest'
import { fetchAllPages, type Page } from '@/api/paging'

const pageOf = (items: number[], page: number, totalCount: number): Page<number> => ({ items, page, pageSize: 2, totalCount })

describe('fetchAllPages', () => {
  it('requests pages in order until every item is loaded', async () => {
    const pages = [[1, 2], [3, 4], [5]]
    const fetchPage = vi.fn(async (page: number) => pageOf(pages[page - 1], page, 5))

    expect(await fetchAllPages(fetchPage)).toEqual([1, 2, 3, 4, 5])
    expect(fetchPage.mock.calls.map(([page]) => page)).toEqual([1, 2, 3])
  })

  it('stops after one request when everything fits in the first page', async () => {
    const fetchPage = vi.fn(async (page: number) => pageOf([1], page, 1))

    expect(await fetchAllPages(fetchPage)).toEqual([1])
    expect(fetchPage).toHaveBeenCalledTimes(1)
  })

  it('stops at an empty page even if totalCount promised more', async () => {
    const fetchPage = vi.fn(async (page: number) => pageOf(page === 1 ? [1, 2] : [], page, 10))

    expect(await fetchAllPages(fetchPage)).toEqual([1, 2])
    expect(fetchPage).toHaveBeenCalledTimes(2)
  })
})
