import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { api } from '@/api/client'
import { call } from '@/api/problem'
import { ConfirmPanel } from '@/components/ConfirmPanel'
import { Pagination } from '@/components/Pagination'
import { Button } from '@/components/ui/button'
import { Select } from '@/components/ui/select'
import { StatusBadge } from '@/components/StatusBadge'
import { type Inquiry, type InquiryStatus, inquiriesQueryKey, useInquiries } from '@/features/admin/inquiries/inquiries-api'

const dateFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' })
const statuses: InquiryStatus[] = ['Open', 'Handled']

function readQuery(params: URLSearchParams) {
  const status = params.get('status') as InquiryStatus | null
  const page = Number.parseInt(params.get('page') ?? '', 10)
  return { status: status && statuses.includes(status) ? status : null, page: Number.isFinite(page) && page > 0 ? page : 1 }
}

function writeQuery(query: { status: InquiryStatus | null; page: number }) {
  const params = new URLSearchParams()
  if (query.status) params.set('status', query.status)
  if (query.page > 1) params.set('page', String(query.page))
  return params
}

/** The tenant's Inquiries inbox, newest first. */
export function InquiriesPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const query = useMemo(() => readQuery(searchParams), [searchParams])
  const inquiries = useInquiries(query.status, query.page)

  return (
    <main className="flex flex-col gap-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <h1 className="text-2xl font-semibold tracking-tight">Inquiries</h1>
        <Select
          aria-label="Show"
          className="w-40"
          value={query.status ?? ''}
          onChange={(event) => setSearchParams(writeQuery({ status: (event.target.value || null) as InquiryStatus | null, page: 1 }))}
        >
          <option value="">All</option>
          <option value="Open">Open</option>
          <option value="Handled">Handled</option>
        </Select>
      </div>
      {inquiries.isPending ? (
        <p role="status" className="text-muted-foreground">Loading…</p>
      ) : inquiries.isError ? (
        <p role="alert">The inquiries could not be loaded.</p>
      ) : inquiries.data.items.length === 0 ? (
        <p className="text-muted-foreground">
          No inquiries. Visitors can send them from a product page when Inquiries are enabled in your{' '}
          <Link to="/admin/profile" className="underline">
            profile
          </Link>
          .
        </p>
      ) : (
        <>
          <ul aria-label="Inquiries" className="flex flex-col gap-3">
            {inquiries.data.items.map((inquiry) => (
              <InquiryRow key={inquiry.id} inquiry={inquiry} />
            ))}
          </ul>
          <Pagination
            page={inquiries.data.page}
            pageSize={inquiries.data.pageSize}
            totalCount={inquiries.data.totalCount}
            hrefFor={(page) => `?${writeQuery({ ...query, page }).toString()}`}
          />
        </>
      )}
    </main>
  )
}

function InquiryRow({ inquiry }: { inquiry: Inquiry }) {
  const queryClient = useQueryClient()
  const [confirming, setConfirming] = useState(false)
  const refresh = () => queryClient.invalidateQueries({ queryKey: inquiriesQueryKey })
  const changeStatus = useMutation({
    mutationFn: (status: InquiryStatus) =>
      call(api.PUT('/api/admin/inquiries/{id}/status', { params: { path: { id: inquiry.id } }, body: { status } })),
    onSettled: refresh,
  })
  const remove = useMutation({
    mutationFn: () => call(api.DELETE('/api/admin/inquiries/{id}', { params: { path: { id: inquiry.id } } })),
    onSettled: refresh,
  })
  const busy = changeStatus.isPending || remove.isPending
  const handled = inquiry.status === 'Handled'

  return (
    <li aria-label={`Inquiry from ${inquiry.name}`} className="flex flex-col gap-3 rounded-lg border p-4">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <div>
          <span className="font-medium">{inquiry.name}</span>{' '}
          <a href={`mailto:${inquiry.email}`} className="text-sm underline-offset-4 hover:underline">
            {inquiry.email}
          </a>
        </div>
        <span className="text-sm text-muted-foreground">{dateFormat.format(new Date(inquiry.createdAt))}</span>
      </div>
      <p className="text-sm">
        About{' '}
        <Link to={`/admin/products/${inquiry.product.id}`} className="font-medium underline-offset-4 hover:underline">
          {inquiry.product.name}
        </Link>
        <StatusBadge status={inquiry.status} className="ml-2" />
      </p>
      <p className="whitespace-pre-line text-sm text-muted-foreground">{inquiry.message}</p>
      {confirming ? (
        <ConfirmPanel
          title="Delete this inquiry?"
          confirmLabel="Delete"
          busy={busy}
          onConfirm={() => remove.mutate()}
          onCancel={() => setConfirming(false)}
        >
          The message and the sender's details are removed for good.
        </ConfirmPanel>
      ) : (
        <div className="flex gap-2">
          <Button size="sm" variant="outline" disabled={busy} onClick={() => changeStatus.mutate(handled ? 'Open' : 'Handled')}>
            {handled ? 'Reopen' : 'Mark handled'}
          </Button>
          <Button size="sm" variant="ghost" disabled={busy} onClick={() => setConfirming(true)}>
            Delete
          </Button>
        </div>
      )}
    </li>
  )
}
