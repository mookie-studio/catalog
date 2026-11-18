import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { useSearchParams } from 'react-router'
import { api } from '@/api/client'
import { ApiProblem, call } from '@/api/problem'
import { ConfirmPanel } from '@/components/ConfirmPanel'
import { Pagination } from '@/components/Pagination'
import { Field } from '@/components/Field'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { useCurrentUser } from '@/features/auth/current-user'
import { pageParam, pageSearch } from '@/lib/page-param'

const adminsQueryKey = ['admin', 'admins'] as const

export function TeamPage() {
  const [searchParams] = useSearchParams()
  const page = pageParam(searchParams)
  const admins = useQuery({
    queryKey: [...adminsQueryKey, page],
    queryFn: () => call(api.GET('/api/admin/admins', { params: { query: { page } } })),
  })
  const me = useCurrentUser()

  return (
    <main className="flex max-w-3xl flex-col gap-6">
      <h1 className="display-m">Team</h1>
      <p className="text-sm text-muted-foreground">Admins manage this Tenant's catalog and its other Admins.</p>
      <InviteForm />
      {admins.isPending ? (
        <p role="status" className="text-muted-foreground">Loading…</p>
      ) : admins.isError ? (
        <p role="alert">The Admins could not be loaded.</p>
      ) : (
        <>
          <ul aria-label="Admins" className="flex flex-col gap-2">
            {admins.data.items.map((admin) => (
              <AdminRow key={admin.id} admin={admin} isMe={admin.userId === me.data?.userId} />
            ))}
          </ul>
          <Pagination page={admins.data.page} pageSize={admins.data.pageSize} totalCount={admins.data.totalCount} hrefFor={pageSearch} />
        </>
      )}
    </main>
  )
}

function InviteForm() {
  const queryClient = useQueryClient()
  const [email, setEmail] = useState('')
  const [invited, setInvited] = useState<string | null>(null)
  const invite = useMutation({
    mutationFn: () => call(api.POST('/api/admin/admins', { body: { email } })),
    onSuccess: (admin) => {
      setInvited(admin.email)
      setEmail('')
      queryClient.invalidateQueries({ queryKey: adminsQueryKey })
    },
  })
  const error = invite.error instanceof ApiProblem ? (invite.error.fields.email ?? invite.error.message) : undefined

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setInvited(null)
    invite.mutate()
  }

  return (
    <form aria-label="Invite an Admin" className="flex flex-col gap-2" onSubmit={handleSubmit}>
      <div className="flex items-start gap-2">
        <div className="flex-1">
          <Field label="Email" error={error}>
            <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </Field>
        </div>
        <Button type="submit" className="mt-6" disabled={invite.isPending}>
          Invite
        </Button>
      </div>
      {invited && (
        <p role="status" className="text-sm text-muted-foreground">
          Invitation sent to {invited}.
        </p>
      )}
    </form>
  )
}

type Admin = { id: string; userId: string; email: string }

function AdminRow({ admin, isMe }: { admin: Admin; isMe: boolean }) {
  const queryClient = useQueryClient()
  const [confirming, setConfirming] = useState(false)
  const remove = useMutation({
    mutationFn: () => call(api.DELETE('/api/admin/admins/{id}', { params: { path: { id: admin.id } } })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: adminsQueryKey }),
  })

  return (
    <li aria-label={admin.email} className="flex flex-col gap-2 rounded-md border px-3 py-2">
      <div className="flex items-center justify-between gap-2">
        <span className="text-sm">
          {admin.email}
          {isMe && <span className="ml-2 text-muted-foreground">(you)</span>}
        </span>
        {!isMe && (
          <Button size="sm" variant="ghost" onClick={() => setConfirming(true)}>
            Remove
          </Button>
        )}
      </div>
      {confirming && (
        <ConfirmPanel
          title={`Remove ${admin.email}?`}
          confirmLabel="Remove"
          busy={remove.isPending}
          onConfirm={() => remove.mutate()}
          onCancel={() => setConfirming(false)}
        >
          They will no longer be able to sign in to this Tenant.
        </ConfirmPanel>
      )}
      {remove.isError && (
        <p role="alert" className="text-sm text-destructive">
          {remove.error.message}
        </p>
      )}
    </li>
  )
}
