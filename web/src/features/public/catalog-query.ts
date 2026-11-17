// The catalog's search, page, Category, attribute filters, sort and view live in the URL
// query string, so a reload or a shared link shows the same list.
export type CatalogSort = 'relevance' | 'featured' | 'name' | 'newest'
export type CatalogView = 'grid' | 'list'

export type CatalogQuery = {
  q: string
  page: number
  categoryId: string | null
  /** Selected values per Attribute Definition id. */
  attributes: Record<string, string[]>
  /** null means the default order: relevance while searching, featured otherwise. */
  sort: CatalogSort | null
  /** null means no view in the URL (the remembered one, else grid, is used). */
  view: CatalogView | null
}

const sorts: CatalogSort[] = ['relevance', 'featured', 'name', 'newest']

/** The order used when none is chosen. */
export const defaultSort = (query: Pick<CatalogQuery, 'q'>): CatalogSort => (query.q ? 'relevance' : 'featured')

/** Sorts a visitor can choose: relevance only makes sense while searching. */
export const sortOptions = (query: Pick<CatalogQuery, 'q'>): CatalogSort[] => (query.q ? sorts : sorts.filter((s) => s !== 'relevance'))

/** The effective order: the chosen one if it applies, otherwise the default. */
export function effectiveSort(query: Pick<CatalogQuery, 'q' | 'sort'>): CatalogSort {
  return query.sort && sortOptions(query).includes(query.sort) ? query.sort : defaultSort(query)
}

/** Choosing an order goes back to the first page; choosing the default removes it from the URL. */
export function withSort(query: CatalogQuery, sort: CatalogSort): CatalogQuery {
  return { ...query, sort: sort === defaultSort(query) ? null : sort, page: 1 }
}

const attributePrefix = 'attr.'

export function readCatalogQuery(params: URLSearchParams): CatalogQuery {
  const page = Number.parseInt(params.get('page') ?? '', 10)
  const attributes: Record<string, string[]> = {}
  for (const [key, value] of params) {
    if (key.startsWith(attributePrefix) && value !== '') {
      const id = key.slice(attributePrefix.length)
      attributes[id] = [...(attributes[id] ?? []), value]
    }
  }
  return {
    q: params.get('q') ?? '',
    page: Number.isFinite(page) && page > 0 ? page : 1,
    categoryId: params.get('categoryId') || null,
    attributes,
    sort: sorts.find((s) => s === params.get('sort')) ?? null,
    view: params.get('view') === 'list' ? 'list' : params.get('view') === 'grid' ? 'grid' : null,
  }
}

export function writeCatalogQuery(query: CatalogQuery): URLSearchParams {
  const params = new URLSearchParams()
  if (query.q) params.set('q', query.q)
  if (query.categoryId) params.set('categoryId', query.categoryId)
  for (const [id, values] of Object.entries(query.attributes)) {
    for (const value of values) params.append(attributePrefix + id, value)
  }
  if (query.sort) params.set('sort', query.sort)
  if (query.view === 'list') params.set('view', 'list')
  if (query.page > 1) params.set('page', String(query.page))
  return params
}

/** Changing what is searched or filtered goes back to the first page. */
export function refine(query: CatalogQuery, change: Partial<Omit<CatalogQuery, 'page'>>): CatalogQuery {
  return { ...query, ...change, page: 1 }
}

export function withAttribute(query: CatalogQuery, definitionId: string, values: string[]): CatalogQuery {
  const attributes = { ...query.attributes }
  if (values.length === 0) delete attributes[definitionId]
  else attributes[definitionId] = values
  return refine(query, { attributes })
}

/** Query parameters for GET /api/public/{tenantSlug}/products. */
export function toApiQuery(query: CatalogQuery) {
  const apiQuery: Record<string, string | number | string[]> = { page: query.page }
  if (query.q) apiQuery.q = query.q
  if (query.categoryId) apiQuery.categoryId = query.categoryId
  if (query.sort) apiQuery.sort = effectiveSort(query)
  for (const [id, values] of Object.entries(query.attributes)) apiQuery[attributePrefix + id] = values
  // attr.{definitionId} parameters are dynamic, so the generated type cannot list them.
  return apiQuery as { page: number; q?: string; categoryId?: string; sort?: string }
}
