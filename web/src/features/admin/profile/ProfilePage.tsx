import { useMutation, useQueryClient } from '@tanstack/react-query'
import { type ChangeEvent, type FormEvent, useRef, useState } from 'react'
import { api } from '@/api/client'
import { ApiProblem, call } from '@/api/problem'
import { Field } from '@/components/Field'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { checkLogo, logoTypes, profileQueryKey, type TenantProfile, useTenantProfile } from '@/features/admin/profile/profile-api'

type ProfileValues = {
  description: string
  contactEmail: string
  phone: string
  address: string
  websiteUrl: string
  brandColor: string
}

const toValues = (profile: TenantProfile): ProfileValues => ({
  description: profile.description ?? '',
  contactEmail: profile.contactEmail ?? '',
  phone: profile.phone ?? '',
  address: profile.address ?? '',
  websiteUrl: profile.websiteUrl ?? '',
  brandColor: profile.brandColor ?? '',
})

/** The Tenant profile: what the tenant shows about itself in its public catalog. */
export function ProfilePage() {
  const profile = useTenantProfile()

  if (profile.isPending) return <p role="status" className="text-muted-foreground">Loading…</p>
  if (profile.isError) return <p role="alert">The profile could not be loaded.</p>

  return (
    <div className="flex flex-col gap-8">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Profile</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          {profile.data.name} · <span className="font-mono">/{profile.data.slug}</span>. Only a Platform Admin can change the name and the
          Tenant slug.
        </p>
      </div>
      <ProfileForm profile={profile.data} />
      <LogoEditor logoUrl={profile.data.logoUrl ?? null} />
    </div>
  )
}

function ProfileForm({ profile }: { profile: TenantProfile }) {
  const queryClient = useQueryClient()
  const [values, setValues] = useState(() => toValues(profile))
  const save = useMutation({
    mutationFn: (body: ProfileValues) => call(api.PUT('/api/admin/tenant/profile', { body })),
    onSuccess: (data) => queryClient.setQueryData(profileQueryKey, data),
  })
  const errors = save.error instanceof ApiProblem ? save.error.fields : {}
  const set = (field: keyof ProfileValues) => (event: { target: { value: string } }) => {
    save.reset()
    setValues({ ...values, [field]: event.target.value })
  }

  function submit(event: FormEvent) {
    event.preventDefault()
    save.mutate(values)
  }

  const swatch = /^#[0-9a-fA-F]{6}$/.test(values.brandColor) ? values.brandColor : '#000000'

  return (
    <form onSubmit={submit} className="flex flex-col gap-4" aria-label="Profile">
      <Field label="Description" error={errors.description}>
        <Textarea value={values.description} onChange={set('description')} rows={5} />
      </Field>
      <div className="grid gap-4 md:grid-cols-2">
        <Field label="Contact email" error={errors.contactEmail}>
          <Input type="email" value={values.contactEmail} onChange={set('contactEmail')} />
        </Field>
        <Field label="Phone" error={errors.phone}>
          <Input value={values.phone} onChange={set('phone')} />
        </Field>
        <Field label="Address" error={errors.address}>
          <Input value={values.address} onChange={set('address')} />
        </Field>
        <Field label="Website" error={errors.websiteUrl} hint="Starting with https://">
          <Input value={values.websiteUrl} onChange={set('websiteUrl')} />
        </Field>
        <Field label="Brand color" error={errors.brandColor} hint="Used for the header and buttons of your public catalog. Leave empty for the default.">
          <Input value={values.brandColor} onChange={set('brandColor')} placeholder="#1A2B3C" />
        </Field>
        <div className="flex items-end gap-2 pb-7">
          <input
            type="color"
            aria-label="Pick brand color"
            value={swatch.toLowerCase()}
            onChange={(event) => setValues({ ...values, brandColor: event.target.value.toUpperCase() })}
            className="h-9 w-14 cursor-pointer rounded border"
          />
          {values.brandColor && (
            <Button type="button" variant="ghost" size="sm" onClick={() => setValues({ ...values, brandColor: '' })}>
              Clear color
            </Button>
          )}
        </div>
      </div>
      {save.error && Object.keys(errors).length === 0 && (
        <p role="alert" className="text-sm text-destructive">
          {save.error.message}
        </p>
      )}
      <div className="flex items-center gap-3">
        <Button type="submit" disabled={save.isPending}>
          Save profile
        </Button>
        {save.isSuccess && (
          <span role="status" className="text-sm text-muted-foreground">
            Saved.
          </span>
        )}
      </div>
    </form>
  )
}

function LogoEditor({ logoUrl }: { logoUrl: string | null }) {
  const queryClient = useQueryClient()
  const input = useRef<HTMLInputElement>(null)
  const [clientError, setClientError] = useState<string | null>(null)
  const refresh = () => queryClient.invalidateQueries({ queryKey: profileQueryKey })
  const upload = useMutation({
    mutationFn: (file: File) => {
      const body = new FormData()
      body.append('file', file)
      return call(api.PUT('/api/admin/tenant/logo', { body: body as never }))
    },
    onSettled: refresh,
  })
  const remove = useMutation({ mutationFn: () => call(api.DELETE('/api/admin/tenant/logo')), onSettled: refresh })
  const busy = upload.isPending || remove.isPending
  const serverError = upload.error ?? remove.error
  const error =
    clientError ?? (serverError instanceof ApiProblem ? (Object.values(serverError.fields)[0] ?? serverError.message) : serverError?.message)

  function handleFile(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return
    const problem = checkLogo(file)
    setClientError(problem)
    upload.reset()
    if (!problem) upload.mutate(file)
  }

  return (
    <section aria-label="Logo" className="flex flex-col gap-4 border-t pt-6">
      <h2 className="text-lg font-semibold">Logo</h2>
      {logoUrl ? (
        <img src={logoUrl} alt="Current logo" className="h-20 w-auto max-w-xs rounded border object-contain p-1" />
      ) : (
        <p className="text-sm text-muted-foreground">No logo. Your catalog shows only its name.</p>
      )}
      {error && (
        <p role="alert" className="text-sm text-destructive">
          {error}
        </p>
      )}
      <div className="flex gap-2">
        <input ref={input} type="file" accept={logoTypes.join(',')} className="sr-only" aria-label="Upload logo" onChange={handleFile} />
        <Button variant="outline" disabled={busy} onClick={() => input.current?.click()}>
          {logoUrl ? 'Replace logo' : 'Upload logo'}
        </Button>
        {logoUrl && (
          <Button variant="ghost" disabled={busy} onClick={() => remove.mutate()}>
            Remove logo
          </Button>
        )}
      </div>
      <p className="text-sm text-muted-foreground">JPG, PNG or WebP, up to 1 MB.</p>
    </section>
  )
}
