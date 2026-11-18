import { cn } from '@/lib/utils'

/** The Just Looking wordmark in app headers: plum ink in the light theme, reversed in the dark one. */
export function AppLogo({ className }: { className?: string }) {
  return (
    <>
      <img src="/brand/just-looking-wordmark.svg" alt="Just Looking" className={cn('h-6 w-auto dark:hidden', className)} />
      <img src="/brand/just-looking-wordmark-reversed.svg" alt="Just Looking" className={cn('hidden h-6 w-auto dark:block', className)} />
    </>
  )
}
