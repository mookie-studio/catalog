import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

/**
 * An empty list (DESIGN.md: Iconography, empty states): the brand's eyes symbol, a short title, one
 * sentence on what will appear here, and optionally the action that adds the first item.
 */
export function EmptyState({
  title,
  children,
  action,
  className,
}: {
  title: string
  children?: ReactNode
  action?: ReactNode
  className?: string
}) {
  return (
    <div className={cn('flex flex-col items-center gap-2 rounded-l border border-dashed border-border-strong/60 px-6 py-10 text-center', className)}>
      {/* Decorative: plum eyes on light, lilac on dark. */}
      <img src="/brand/just-looking-symbol.svg" alt="" className="mb-2 h-6 w-auto dark:hidden" />
      <img src="/brand/just-looking-symbol-reversed.svg" alt="" className="mb-2 hidden h-6 w-auto dark:block" />
      <p className="font-semibold text-ink">{title}</p>
      {children && <p className="max-w-[46ch] text-sm text-ink-muted">{children}</p>}
      {action && <div className="mt-3">{action}</div>}
    </div>
  )
}
