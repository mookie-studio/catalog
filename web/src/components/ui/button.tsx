import { cva, type VariantProps } from 'class-variance-authority'
import type { ComponentProps } from 'react'
import { Link } from 'react-router'
import { cn } from '@/lib/utils'

// Button per DESIGN.md: Archivo 800 at 108% width, radius-m, ease-out transitions.
const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-m border-2 border-transparent font-display font-extrabold leading-none [font-stretch:108%] transition-[background-color,border-color,box-shadow] duration-150 ease-out cursor-pointer disabled:cursor-not-allowed disabled:opacity-45 aria-disabled:cursor-not-allowed aria-disabled:opacity-45 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring',
  {
    variants: {
      variant: {
        default: 'bg-primary text-primary-foreground hover:not-disabled:bg-accent-hover',
        outline: 'border-border-strong bg-transparent text-ink hover:not-disabled:bg-surface-sunken',
        ghost: 'bg-transparent text-ink hover:not-disabled:bg-surface-sunken',
        destructive: 'bg-danger text-surface-raised hover:not-disabled:opacity-90',
      },
      size: {
        default: 'px-5 py-3 text-base',
        sm: 'px-3.5 py-2 text-sm',
      },
    },
    defaultVariants: { variant: 'default', size: 'default' },
  },
)

export function Button({
  className,
  variant,
  size,
  ...props
}: ComponentProps<'button'> & VariantProps<typeof buttonVariants>) {
  return <button className={cn(buttonVariants({ variant, size, className }))} {...props} />
}

/** A link that looks like a button. */
export function ButtonLink({
  className,
  variant,
  size,
  ...props
}: ComponentProps<typeof Link> & VariantProps<typeof buttonVariants>) {
  return <Link className={cn(buttonVariants({ variant, size, className }))} {...props} />
}
