import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { api } from '@/api/client'
import { ApiProblem, call } from '@/api/problem'
import { ConfirmPanel } from '@/components/ConfirmPanel'
import { Pagination } from '@/components/Pagination'
import { Field } from '@/components/Field'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { type Tenant, tenantsQueryKey } from '@/features/platform/tenants/tenants-api'

/** Admins, activation and Tenant slug of one Tenant. */
export function TenantPanel({ tenant }: { tenant: Tenant }) {
  return (
    <div className="flex flex-col gap-6 border-t px-4 py-4">
      <TenantAdmins tenantId={tenant.id} />
      <ChangeSlugForm tenant={tenant} />
      <ActivationAction tenant={tenant} />
    </div>
  )
}

function TenantAdmins({ tenantId }: { tenantId: string }) {
  const queryClient = useQueryClient()
  const path = { params: { path: { id: tenantId } } }
  const queryKey = ['platform', 'tenant-admins', tenantId]
  const [page, setPage] = useState(1)
  const admins = useQuery({
    queryKey: [...queryKey, page],
    queryFn: () => call(api.GET('/api/platform/tenants/{id}/admins', { params: { path: { id: tenantId }, query: { page } } })),
  })
  const [email, setEmail] = useState('')
  const invite = useMutation({
    mutationFn: () => call(api.POST('/api/platform/tenants/{id}/admins', { ...path, body: { email } })),
    onSuccess: () => {
      setEmail('')
      queryClient.invalidateQueries({ queryKey })
    },
  })
  const error = invite.error instanceof ApiProblem ? (invite.error.fields.email ?? invite.error.message) : undefined

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    invite.mutate()
  }

  return (
    <section aria-label="Admins" className="flex flex-col gap-3">
      <h3 className="text-sm font-semibold">Admins</h3>
      {admins.isPending ? (
        <p role="status" className="text-sm text-muted-foreground">Loading…</p>
      ) : admins.isError ? (
        <p role="alert" className="text-sm">The Admins could not be loaded.</p>
      ) : admins.data.totalCount === 0 ? (
        <p className="text-sm text-muted-foreground">No Admins yet. Invite the first one.</p>
      ) : (
        <>
          <ul className="flex flex-col gap-1 text-sm">
            {admins.data.items.map((admin) => (
              <li key={admin.id}>{admin.email}</li>
            ))}
          </ul>
          <Pagination page={admins.data.page} pageSize={admins.data.pageSize} totalCount={admins.data.totalCount} onPageChange={setPage} />
        </>
      )}
      <form aria-label="Invite an Admin" className="flex items-start gap-2" onSubmit={handleSubmit}>
        <div className="flex-1">
          <Field label="Admin email" error={error}>
            <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </Field>
        </div>
        <Button type="submit" size="sm" className="mt-6" disabled={invite.isPending}>
          Invite
        </Button>
      </form>
    </section>
  )
}

function ChangeSlugForm({ tenant }: { tenant: Tenant }) {
  const queryClient = useQueryClient()
  const [slug, setSlug] = useState(tenant.slug)
  const [changedTo, setChangedTo] = useState<string | null>(null)
  const change = useMutation({
    mutationFn: () => call(api.PUT('/api/platform/tenants/{id}/slug', { params: { path: { id: tenant.id } }, body: { slug } })),
    onSuccess: (updated) => {
      setChangedTo(updated.slug)
      queryClient.invalidateQueries({ queryKey: tenantsQueryKey })
    },
  })
  const error = change.error instanceof ApiProblem ? (change.error.fields.slug ?? change.error.message) : undefined

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setChangedTo(null)
    change.mutate()
  }

  return (
    <section aria-label="Tenant slug" className="flex flex-col gap-2">
      <form className="flex items-start gap-2" onSubmit={handleSubmit}>
        <div className="flex-1">
          <Field
            label="Tenant slug"
            error={error}
            hint={slug !== tenant.slug ? `Existing public links to /${tenant.slug} will stop working.` : undefined}
          >
            <Input mono value={slug} onChange={(e) => setSlug(e.target.value)} required />
          </Field>
        </div>
        <Button type="submit" size="sm" className="mt-6" disabled={slug === tenant.slug || change.isPending}>
          Change slug
        </Button>
      </form>
      {changedTo && (
        <p role="status" className="text-sm text-muted-foreground">
          The public catalog is now at /{changedTo}.
        </p>
      )}
    </section>
  )
}

function ActivationAction({ tenant }: { tenant: Tenant }) {
  const queryClient = useQueryClient()
  const [confirming, setConfirming] = useState(false)
  const path = { params: { path: { id: tenant.id } } }
  const toggle = useMutation({
    mutationFn: () =>
      call(tenant.isActive ? api.POST('/api/platform/tenants/{id}/deactivate', path) : api.POST('/api/platform/tenants/{id}/reactivate', path)),
    onSuccess: () => {
      setConfirming(false)
      queryClient.invalidateQueries({ queryKey: tenantsQueryKey })
    },
  })
  const label = tenant.isActive ? 'Deactivate' : 'Reactivate'

  return (
    <section aria-label="Activation" className="flex flex-col gap-2">
      <div>
        <Button size="sm" variant="outline" onClick={() => setConfirming(true)}>
          {label}
        </Button>
      </div>
      {confirming && (
        <ConfirmPanel
          title={`${label} ${tenant.name}?`}
          confirmLabel={label}
          busy={toggle.isPending}
          onConfirm={() => toggle.mutate()}
          onCancel={() => setConfirming(false)}
        >
          {tenant.isActive
            ? 'Its Admins will not be able to sign in and its public catalog will not be visible. You can reactivate it later.'
            : 'Its Admins will be able to sign in again and its public catalog will be visible.'}
        </ConfirmPanel>
      )}
      {toggle.isError && (
        <p role="alert" className="text-sm text-destructive">
          {toggle.error.message}
        </p>
      )}
    </section>
  )
}
