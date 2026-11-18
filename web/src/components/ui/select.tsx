import type { ComponentProps } from 'react'
import { controlClass } from '@/components/ui/control'
import { cn } from '@/lib/utils'

/** A native select styled like the other inputs. */
export function Select({ className, ...props }: ComponentProps<'select'>) {
  return <select className={cn(controlClass, 'flex h-10 px-2.5', className)} {...props} />
}
