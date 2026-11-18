import { SignOutButton } from '@/features/auth/SignOutButton'

export function NoAccessPage() {
  return (
    <main className="flex min-h-svh flex-col items-center justify-center gap-3 p-6 text-center">
      <h1 className="display-m">No access</h1>
      <p className="text-muted-foreground">Your account does not have access to this area.</p>
      <SignOutButton />
    </main>
  )
}
