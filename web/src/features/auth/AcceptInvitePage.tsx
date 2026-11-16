import { useQuery, useQueryClient } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { AuthCard } from '@/features/auth/AuthCard'
import { currentUserQueryKey, homeFor, loadCurrentUser } from '@/features/auth/current-user'
import { hasSession, setPassword } from '@/features/auth/session'

const minimumPasswordLength = 8

// The invitation email links here; Supabase reads the invitation token from the
// address and signs the invited person in, who then chooses a password.
export function AcceptInvitePage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const session = useQuery({ queryKey: ['invite-session'], queryFn: hasSession, retry: false })
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    const password = String(form.get('password'))
    if (password.length < minimumPasswordLength) {
      setError(`The password must have at least ${minimumPasswordLength} characters.`)
      return
    }
    if (password !== String(form.get('confirmPassword'))) {
      setError('The passwords do not match.')
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      const failure = await setPassword(password)
      if (failure) {
        setError(failure)
        return
      }
      const user = await queryClient.fetchQuery({ queryKey: currentUserQueryKey, queryFn: loadCurrentUser, staleTime: 0 })
      navigate((user && homeFor(user)) ?? '/admin', { replace: true })
    } catch {
      setError('Something went wrong. Please try again.')
    } finally {
      setSubmitting(false)
    }
  }

  if (session.isPending) {
    return <p role="status" className="p-6 text-muted-foreground">Loading…</p>
  }
  if (!session.data) {
    return (
      <AuthCard title="Invitation expired">
        <p className="text-muted-foreground">This invitation link is not valid any more. Ask for a new invitation.</p>
      </AuthCard>
    )
  }

  return (
    <AuthCard title="Choose your password">
      <form className="space-y-4" onSubmit={handleSubmit}>
        <div className="space-y-2">
          <Label htmlFor="password">Password</Label>
          <Input id="password" name="password" type="password" autoComplete="new-password" required />
        </div>
        <div className="space-y-2">
          <Label htmlFor="confirmPassword">Repeat password</Label>
          <Input id="confirmPassword" name="confirmPassword" type="password" autoComplete="new-password" required />
        </div>
        {error && (
          <p role="alert" className="text-sm text-destructive">
            {error}
          </p>
        )}
        <Button type="submit" className="w-full" disabled={submitting}>
          Save password
        </Button>
      </form>
    </AuthCard>
  )
}
