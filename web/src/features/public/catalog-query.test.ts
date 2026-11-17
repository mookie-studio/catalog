import { describe, expect, it } from 'vitest'
import {
  effectiveSort,
  readCatalogQuery,
  refine,
  sortOptions,
  toApiQuery,
  withAttribute,
  withSort,
  writeCatalogQuery,
} from '@/features/public/catalog-query'

describe('catalog query', () => {
  it('reads search, page, category and repeated attribute values from the URL', () => {
    const query = readCatalogQuery(new URLSearchParams('q=drill&page=3&categoryId=c1&attr.a1=o1&attr.a1=o2&attr.a2=true'))

    expect(query).toEqual({ q: 'drill', page: 3, categoryId: 'c1', attributes: { a1: ['o1', 'o2'], a2: ['true'] }, sort: null, view: null })
  })

  it('falls back to the first page for a missing or invalid page', () => {
    expect(readCatalogQuery(new URLSearchParams('page=abc')).page).toBe(1)
    expect(readCatalogQuery(new URLSearchParams('page=0')).page).toBe(1)
  })

  it('writes back the same query string', () => {
    const params = 'q=drill&categoryId=c1&attr.a1=o1&attr.a1=o2&page=2'

    expect(writeCatalogQuery(readCatalogQuery(new URLSearchParams(params))).toString()).toBe(params)
  })

  it('goes back to the first page when the search or filters change', () => {
    const query = readCatalogQuery(new URLSearchParams('q=drill&page=4'))

    expect(refine(query, { q: 'saw' }).page).toBe(1)
    expect(withAttribute(query, 'a1', ['o1']).page).toBe(1)
    expect(withAttribute({ ...query, attributes: { a1: ['o1'] } }, 'a1', []).attributes).toEqual({})
  })

  it('sends attribute filters as attr parameters', () => {
    expect(toApiQuery(readCatalogQuery(new URLSearchParams('attr.a1=o1&attr.a1=o2')))).toEqual({ page: 1, 'attr.a1': ['o1', 'o2'] })
  })

  it('reads, defaults and writes the sort', () => {
    const browsing = readCatalogQuery(new URLSearchParams('sort=newest'))
    expect(browsing.sort).toBe('newest')
    expect(readCatalogQuery(new URLSearchParams('sort=price')).sort).toBeNull()
    expect(effectiveSort(readCatalogQuery(new URLSearchParams('')))).toBe('featured')
    expect(effectiveSort(readCatalogQuery(new URLSearchParams('q=drill')))).toBe('relevance')
    // Relevance without a search falls back to the default.
    expect(effectiveSort(readCatalogQuery(new URLSearchParams('sort=relevance')))).toBe('featured')
    expect(writeCatalogQuery(browsing).toString()).toBe('sort=newest')
  })

  it('removes the sort from the URL when the default is chosen and goes back to page 1', () => {
    const query = readCatalogQuery(new URLSearchParams('q=drill&sort=name&page=3'))

    expect(writeCatalogQuery(withSort(query, 'relevance')).toString()).toBe('q=drill')
    expect(writeCatalogQuery(withSort(query, 'newest')).toString()).toBe('q=drill&sort=newest')
  })

  it('offers relevance only while searching', () => {
    expect(sortOptions({ q: '' })).toEqual(['featured', 'name', 'newest'])
    expect(sortOptions({ q: 'drill' })).toEqual(['relevance', 'featured', 'name', 'newest'])
  })

  it('sends the chosen sort to the API', () => {
    expect(toApiQuery(readCatalogQuery(new URLSearchParams('sort=name')))).toMatchObject({ sort: 'name' })
    expect(toApiQuery(readCatalogQuery(new URLSearchParams('')))).not.toHaveProperty('sort')
  })

  it('reads and writes the view, keeping grid out of the URL', () => {
    expect(readCatalogQuery(new URLSearchParams('view=list')).view).toBe('list')
    expect(readCatalogQuery(new URLSearchParams('view=grid')).view).toBe('grid')
    expect(readCatalogQuery(new URLSearchParams('view=table')).view).toBeNull()
    expect(writeCatalogQuery(readCatalogQuery(new URLSearchParams('view=list&page=2'))).toString()).toBe('view=list&page=2')
    expect(writeCatalogQuery(readCatalogQuery(new URLSearchParams('view=grid'))).toString()).toBe('')
  })
})
