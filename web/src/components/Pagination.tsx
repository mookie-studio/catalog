import { Link } from 'react-router'

type Props = { page: number; pageSize: number; totalCount: number; hrefFor: (page: number) => string }

export function Pagination({ page, pageSize, totalCount, hrefFor }: Props) {
  const pageCount = Math.ceil(totalCount / pageSize)
  if (pageCount <= 1) return null
  return (
    <nav aria-label="Pagination" className="flex items-center justify-between gap-4 text-sm">
      {page > 1 ? <Link to={hrefFor(page - 1)}>Previous</Link> : <span />}
      <span className="text-muted-foreground">
        Page {page} of {pageCount}
      </span>
      {page < pageCount ? <Link to={hrefFor(page + 1)}>Next</Link> : <span />}
    </nav>
  )
}
