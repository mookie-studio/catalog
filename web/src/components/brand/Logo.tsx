import { cn } from '@/lib/utils'

type LogoVariant = 'default' | 'reversed' | 'landing'
type LogoKind = 'wordmark' | 'symbol'

const files: Record<LogoKind, Record<LogoVariant, string>> = {
  wordmark: {
    default: '/brand/just-looking-wordmark.svg',
    reversed: '/brand/just-looking-wordmark-reversed.svg',
    landing: '/brand/just-looking-wordmark-plum-mustard.svg',
  },
  symbol: {
    default: '/brand/just-looking-symbol.svg',
    reversed: '/brand/just-looking-symbol-reversed.svg',
    landing: '/brand/just-looking-symbol-reversed.svg',
  },
}

/**
 * The Just Looking logo (DESIGN.md: Logo). Always an image of the outlined files in /brand/,
 * never retyped. `default` for light grounds, `reversed` for plum and dark grounds, `landing`
 * for landing-ground only. Set the height with className (at least h-5 wordmark, h-4 symbol).
 */
export function Logo({
  variant = 'default',
  kind = 'wordmark',
  className,
}: {
  variant?: LogoVariant
  kind?: LogoKind
  className?: string
}) {
  return (
    <img
      src={files[kind][variant]}
      alt="Just Looking"
      className={cn('inline-block w-auto', kind === 'wordmark' ? 'h-7' : 'h-5', className)}
    />
  )
}
