/** A paged list response from the API: { items, page, pageSize, totalCount }. */
export type Page<T> = { items: T[]; page: number; pageSize: number; totalCount: number }

/** Largest page size the API accepts; used when every item is needed. */
export const maxPageSize = 50

/**
 * Loads every page of a paged list, in order, for screens that need all items (for example every
 * Attribute Definition in the product editor). Stops once totalCount items are loaded or a page is empty.
 */
export async function fetchAllPages<T>(fetchPage: (page: number) => Promise<Page<T>>): Promise<T[]> {
  const items: T[] = []
  for (let page = 1; ; page++) {
    const result = await fetchPage(page)
    items.push(...result.items)
    if (result.items.length === 0 || items.length >= result.totalCount) return items
  }
}
