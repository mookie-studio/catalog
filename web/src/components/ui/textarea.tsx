import type { ComponentProps } from 'react'
import { controlClass } from '@/components/ui/control'
import { cn } from '@/lib/utils'

export function Textarea({ className, ...props }: ComponentProps<'textarea'>) {
  return <textarea className={cn(controlClass, 'flex min-h-24 px-3 py-2.5', className)} {...props} />
}
