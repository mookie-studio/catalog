import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { useSearchParams } from 'react-router'
import { api } from '@/api/client'
import { ApiProblem, call, hasFieldErrors } from '@/api/problem'
import { Field } from '@/components/Field'
import { Pagination } from '@/components/Pagination'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { TenantPanel } from '@/features/platform/tenants/TenantPanel'
import { tenantsQueryKey } from '@/features/platform/tenants/tenants-api'

export function TenantsPage() {
  const [searchParams] = useSearchParams()
  const page = Math.max(1, Number.parseInt(searchParams.get('page') ?? '', 10) || 1)
  const tenants = useQuery({
    queryKey: [...tenantsQueryKey, page],
    queryFn: () => call(api.GET('/api/platform/tenants', { params: { query: { page } } })),
    placeholderData: keepPreviousData,
  })
  const [openId, setOpenId] = useState<string | null>(null)

  return (
    <main className="flex max-w-4xl flex-col gap-6">
      <h1 className="text-2xl font-semibold tracking-tight">Tenants</h1>
      <CreateTenantForm />
      {tenants.isPending ? (
        <p role="status" className="text-muted-foreground">Loading…</p>
      ) : tenants.isError ? (
        <p role="alert">The Tenants could not be loaded.</p>
      ) : tenants.data.items.length === 0 ? (
        <p className="text-muted-foreground">No Tenants yet.</p>
      ) : (
        <>
          <ul aria-label="Tenants" className="flex flex-col gap-2">
            {tenants.data.items.map((tenant) => (
              <li key={tenant.id} aria-label={tenant.name} className="rounded-lg border">
                <div className="flex flex-wrap items-center justify-between gap-3 px-4 py-3">
                  <div className="flex flex-wrap items-center gap-3">
                    <span className="font-medium">{tenant.name}</span>
                    <span className="font-mono text-sm text-muted-foreground">/{tenant.slug}</span>
                    <Badge className={tenant.isActive ? 'border-success/30 bg-success-soft text-success' : 'text-muted-foreground'}>
                      {tenant.isActive ? 'Active' : 'Deactivated'}
                    </Badge>
                  </div>
                  <Button size="sm" variant="outline" aria-expanded={openId === tenant.id} onClick={() => setOpenId(openId === tenant.id ? null : tenant.id)}>
                    {openId === tenant.id ? 'Close' : 'Manage'}
                  </Button>
                </div>
                {openId === tenant.id && <TenantPanel tenant={tenant} />}
              </li>
            ))}
          </ul>
          <Pagination page={tenants.data.page} pageSize={tenants.data.pageSize} totalCount={tenants.data.totalCount} hrefFor={(p) => `?page=${p}`} />
        </>
      )}
    </main>
  )
}

function CreateTenantForm() {
  const queryClient = useQueryClient()
  const [values, setValues] = useState({ name: '', slug: '' })
  const create = useMutation({
    mutationFn: () => call(api.POST('/api/platform/tenants', { body: values })),
    onSuccess: () => {
      setValues({ name: '', slug: '' })
      queryClient.invalidateQueries({ queryKey: tenantsQueryKey })
    },
  })
  const fields = create.error instanceof ApiProblem ? create.error.fields : {}

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    create.mutate()
  }

  return (
    <form aria-label="New Tenant" className="grid gap-4 rounded-lg border p-4 sm:grid-cols-[1fr_1fr_auto]" onSubmit={handleSubmit}>
      <Field label="Name" error={fields.name}>
        <Input value={values.name} onChange={(e) => setValues({ ...values, name: e.target.value })} required />
      </Field>
      <Field label="Tenant slug" error={fields.slug}>
        <Input mono value={values.slug} onChange={(e) => setValues({ ...values, slug: e.target.value })} placeholder="acme-hardware" required />
      </Field>
      <Button type="submit" className="sm:mt-6" disabled={create.isPending}>
        Create tenant
      </Button>
      {create.isError && !hasFieldErrors(create.error) && (
        <p role="alert" className="text-sm text-destructive sm:col-span-3">
          {create.error.message}
        </p>
      )}
    </form>
  )
}
