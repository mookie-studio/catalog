import { StatusBadge } from '@/components/StatusBadge'
import type { ProductStatus } from '@/features/admin/products/products-api'

export function ProductStatusBadge({ status }: { status: ProductStatus }) {
  return <StatusBadge status={status} />
}
