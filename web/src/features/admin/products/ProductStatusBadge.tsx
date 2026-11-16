import { Badge } from '@/components/ui/badge'
import type { ProductStatus } from '@/features/admin/products/products-api'
import { cn } from '@/lib/utils'

const styles: Record<ProductStatus, string> = {
  Draft: 'bg-muted text-muted-foreground',
  Published: 'border-emerald-600/30 bg-emerald-50 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-200',
  Archived: 'border-amber-600/30 bg-amber-50 text-amber-800 dark:bg-amber-950 dark:text-amber-200',
}

export function ProductStatusBadge({ status }: { status: ProductStatus }) {
  return <Badge className={cn(styles[status])}>{status}</Badge>
}
