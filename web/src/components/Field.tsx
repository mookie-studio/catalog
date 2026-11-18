import { type ReactElement, cloneElement, useId } from 'react'
import { Label } from '@/components/ui/label'

type FieldProps = { label: string; error?: string; hint?: string; children: ReactElement<{ id?: string; 'aria-invalid'?: boolean; 'aria-describedby'?: string }> }

/** A labelled input with its error message (for example a ProblemDetails field error). */
export function Field({ label, error, hint, children }: FieldProps) {
  const id = useId()
  const messageId = `${id}-message`
  return (
    <div className="grid gap-1.5">
      <Label htmlFor={id} className="text-sm font-semibold text-ink">
        {label}
      </Label>
      {cloneElement(children, { id, 'aria-invalid': error ? true : undefined, 'aria-describedby': error || hint ? messageId : undefined })}
      {error ? (
        <p id={messageId} className="text-sm font-semibold text-danger">
          {error}
        </p>
      ) : hint ? (
        <p id={messageId} className="text-sm text-ink-muted">
          {hint}
        </p>
      ) : null}
    </div>
  )
}
