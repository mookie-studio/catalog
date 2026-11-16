import { useQueryClient } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { AuthCard } from '@/features/auth/AuthCard'
import { currentUserQueryKey, homeFor, loadCurrentUser } from '@/features/auth/current-user'
import { signIn } from '@/features/auth/session'

/** Only same-site paths are followed after signing in. */
function safeRedirect(value: string | null): string | null {
  return value && value.startsWith('/') && !value.startsWith('//') ? value : null
}

export function LoginPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    setSubmitting(true)
    setError(null)
    try {
      const failure = await signIn(String(form.get('email')), String(form.get('password')))
      if (failure) {
        setError(failure)
        return
      }
      const user = await queryClient.fetchQuery({ queryKey: currentUserQueryKey, queryFn: loadCurrentUser, staleTime: 0 })
      navigate(safeRedirect(searchParams.get('redirectTo')) ?? (user && homeFor(user)) ?? '/admin', { replace: true })
    } catch {
      setError('Something went wrong. Please try again.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <AuthCard title="Sign in">
      <form className="space-y-4" onSubmit={handleSubmit}>
        <div className="space-y-2">
          <Label htmlFor="email">Email</Label>
          <Input id="email" name="email" type="email" autoComplete="email" required />
        </div>
        <div className="space-y-2">
          <Label htmlFor="password">Password</Label>
          <Input id="password" name="password" type="password" autoComplete="current-password" required />
        </div>
        {error && (
          <p role="alert" className="text-sm text-destructive">
            {error}
          </p>
        )}
        <Button type="submit" className="w-full" disabled={submitting}>
          Sign in
        </Button>
      </form>
    </AuthCard>
  )
}
