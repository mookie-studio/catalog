/** Reads the 1-based `page` query parameter; anything missing or invalid is page 1. */
export function pageParam(params: URLSearchParams): number {
  const page = Number.parseInt(params.get('page') ?? '', 10)
  return Number.isFinite(page) && page > 0 ? page : 1
}

/** Search string for a page: page 1 is the bare address. */
export function pageSearch(page: number): string {
  return page > 1 ? `?page=${page}` : '?'
}
