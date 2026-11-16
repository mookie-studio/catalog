import type { ReactNode } from 'react'
import { Button } from '@/components/ui/button'

type Props = { title: string; children: ReactNode; confirmLabel: string; busy?: boolean; onConfirm: () => void; onCancel: () => void }

/** An inline confirmation for a destructive action. */
export function ConfirmPanel({ title, children, confirmLabel, busy, onConfirm, onCancel }: Props) {
  return (
    <div role="alertdialog" aria-label={title} className="space-y-3 rounded-lg border border-destructive/40 bg-destructive/5 p-4">
      <p className="font-medium">{title}</p>
      <div className="text-sm text-muted-foreground">{children}</div>
      <div className="flex gap-2">
        <Button size="sm" className="bg-destructive text-white hover:bg-destructive/90" disabled={busy} onClick={onConfirm}>
          {confirmLabel}
        </Button>
        <Button size="sm" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
      </div>
    </div>
  )
}
