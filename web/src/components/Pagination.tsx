import { Link } from 'react-router'

type Props = { page: number; pageSize: number; totalCount: number } & (
  | { /** Pages kept in the URL: links to each page. */ hrefFor: (page: number) => string; onPageChange?: never }
  | { /** Pages kept in local state: buttons. */ onPageChange: (page: number) => void; hrefFor?: never }
)

export function Pagination({ page, pageSize, totalCount, hrefFor, onPageChange }: Props) {
  const pageCount = Math.ceil(totalCount / pageSize)
  if (pageCount <= 1) return null

  const to = (target: number, label: string) =>
    hrefFor ? (
      <Link to={hrefFor(target)}>{label}</Link>
    ) : (
      <button type="button" className="hover:underline" onClick={() => onPageChange(target)}>
        {label}
      </button>
    )

  return (
    <nav aria-label="Pagination" className="flex items-center justify-between gap-4 text-sm">
      {page > 1 ? to(page - 1, 'Previous') : <span />}
      <span className="text-muted-foreground">
        Page {page} of {pageCount}
      </span>
      {page < pageCount ? to(page + 1, 'Next') : <span />}
    </nav>
  )
}
