import { useEffect, useRef } from 'react'
import { api } from '@/api/client'

/**
 * Counts one Product view when a product page opens (ADR 0012): once per product shown, using the product's
 * current slug so replacing an Old slug in the address does not count twice. Nothing is stored in the browser
 * and failures (including rate limits) are ignored.
 */
export function useRecordProductView(tenantSlug: string, currentSlug: string | undefined) {
  const recorded = useRef<string | null>(null)

  useEffect(() => {
    if (!currentSlug) return
    const key = `${tenantSlug}/${currentSlug}`
    if (recorded.current === key) return
    recorded.current = key
    api
      .POST('/api/public/{tenantSlug}/products/{productSlug}/views', {
        params: { path: { tenantSlug, productSlug: currentSlug } },
      })
      .catch(() => {})
  }, [tenantSlug, currentSlug])
}
