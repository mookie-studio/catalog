import { describe, expect, it } from 'vitest'
import { readCatalogQuery, refine, toApiQuery, withAttribute, writeCatalogQuery } from '@/features/public/catalog-query'

describe('catalog query', () => {
  it('reads search, page, category and repeated attribute values from the URL', () => {
    const query = readCatalogQuery(new URLSearchParams('q=drill&page=3&categoryId=c1&attr.a1=o1&attr.a1=o2&attr.a2=true'))

    expect(query).toEqual({ q: 'drill', page: 3, categoryId: 'c1', attributes: { a1: ['o1', 'o2'], a2: ['true'] } })
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
})
