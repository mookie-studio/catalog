import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

/** The price stamp (DESIGN.md: Stamp). Ink plum-700 on mustard and lilac; `onPlum` for mustard ink. */
export function Stamp({ children, onPlum, className }: { children: ReactNode; onPlum?: boolean; className?: string }) {
  return (
    <span
      className={cn(
        'inline-block -rotate-6 rounded-s border-[2.5px] border-current px-2 py-0.5 font-display text-[15px] leading-[1.1] font-black tracking-[0.04em] uppercase [font-stretch:75%]',
        onPlum ? 'text-mustard-400' : 'text-plum-700',
        className,
      )}
    >
      {children}
    </span>
  )
}
