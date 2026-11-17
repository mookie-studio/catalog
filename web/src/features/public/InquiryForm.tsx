import { useMutation } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { api } from '@/api/client'
import { ApiProblem, toProblem } from '@/api/problem'
import { Field } from '@/components/Field'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'

type Props = { tenantSlug: string; productSlug: string; productName: string }

const empty = { name: '', email: '', message: '', website: '' }

/** "Ask about this product": sends an Inquiry. `website` is a honeypot that people never see or fill. */
export function InquiryForm({ tenantSlug, productSlug, productName }: Props) {
  const [open, setOpen] = useState(false)
  const [values, setValues] = useState(empty)
  const send = useMutation({
    mutationFn: async (body: typeof empty) => {
      const { error, response } = await api.POST('/api/public/{tenantSlug}/products/{productSlug}/inquiries', {
        params: { path: { tenantSlug, productSlug } },
        body,
      })
      if (!response.ok) throw toProblem(error, response)
    },
    onSuccess: () => setValues(empty),
  })
  const errors = send.error instanceof ApiProblem ? send.error.fields : {}
  const set = (field: keyof typeof empty) => (event: { target: { value: string } }) => setValues({ ...values, [field]: event.target.value })

  function submit(event: FormEvent) {
    event.preventDefault()
    send.mutate(values)
  }

  if (!open) {
    return (
      <div>
        <Button
          onClick={() => {
            send.reset()
            setOpen(true)
          }}
        >
          Ask about this product
        </Button>
      </div>
    )
  }

  if (send.isSuccess) {
    return (
      <div role="status" className="rounded-lg border p-4 text-sm">
        <p className="font-medium">Thank you, your message was sent.</p>
        <p className="text-muted-foreground">The business will reply to you by email.</p>
      </div>
    )
  }

  const generalError =
    send.error instanceof ApiProblem && send.error.status === 429
      ? 'Too many messages, try again later.'
      : send.error && Object.keys(errors).length === 0
        ? 'Your message could not be sent. Please try again.'
        : null

  return (
    <form onSubmit={submit} aria-label={`Ask about ${productName}`} className="flex flex-col gap-4 rounded-lg border p-4">
      <h2 className="font-semibold">Ask about this product</h2>
      <Field label="Your name" error={errors.name}>
        <Input value={values.name} onChange={set('name')} autoComplete="name" />
      </Field>
      <Field label="Your email" error={errors.email}>
        <Input type="email" value={values.email} onChange={set('email')} autoComplete="email" />
      </Field>
      <Field label="Message" error={errors.message}>
        <Textarea value={values.message} onChange={set('message')} rows={4} />
      </Field>
      <div aria-hidden="true" className="absolute -left-[9999px] h-px w-px overflow-hidden">
        <label>
          Website
          <input name="website" tabIndex={-1} autoComplete="off" value={values.website} onChange={set('website')} />
        </label>
      </div>
      {generalError && (
        <p role="alert" className="text-sm text-destructive">
          {generalError}
        </p>
      )}
      <div className="flex gap-2">
        <Button type="submit" disabled={send.isPending}>
          Send
        </Button>
        <Button type="button" variant="ghost" onClick={() => setOpen(false)}>
          Cancel
        </Button>
      </div>
    </form>
  )
}
