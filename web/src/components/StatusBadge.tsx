import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'

export type Status = 'Draft' | 'Published' | 'Archived' | 'Open' | 'Handled' | 'Featured'

// DESIGN.md: StatusBadge. Each status is a word plus a dot shape, never colour alone.
const styles: Record<Status, { badge: string; hollow?: boolean }> = {
  Published: { badge: 'bg-success-soft text-success' },
  Draft: { badge: 'bg-warning-soft text-warning' },
  Archived: { badge: 'bg-neutral-soft text-ink-muted', hollow: true },
  Handled: { badge: 'bg-neutral-soft text-ink-muted', hollow: true },
  Open: { badge: 'bg-lilac-100 text-plum-900' },
  Featured: { badge: 'bg-highlight text-on-highlight' },
}

/** The status of a Product or an Inquiry, or the Featured mark, as a small labelled badge. */
export function StatusBadge({ status, className }: { status: Status; className?: string }) {
  const style = styles[status]
  return (
    <Badge className={cn('gap-1.5 rounded-s border-0 px-[9px] py-[5px] text-[13px] font-semibold leading-none', style.badge, className)}>
      <span
        aria-hidden="true"
        data-dot={style.hollow ? 'hollow' : 'solid'}
        className={cn('size-[7px] shrink-0 rounded-full', style.hollow ? 'border-[1.5px] border-current' : 'bg-current')}
      />
      {status}
    </Badge>
  )
}
