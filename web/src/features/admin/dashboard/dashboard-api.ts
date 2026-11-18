import { useQuery } from '@tanstack/react-query'
import { api } from '@/api/client'
import { call } from '@/api/problem'

export const dashboardQueryKey = ['admin', 'dashboard'] as const

export function useDashboard() {
  return useQuery({ queryKey: dashboardQueryKey, queryFn: () => call(api.GET('/api/admin/dashboard')) })
}

export type Period = 7 | 30 | 90

export const periods: Period[] = [7, 30, 90]

/** UTC calendar day as yyyy-MM-dd. */
const day = (date: Date) => date.toISOString().slice(0, 10)

/** The inclusive UTC date range of the last `days` days, ending today. */
export function periodRange(days: Period, now = new Date()) {
  const from = new Date(now)
  from.setUTCDate(from.getUTCDate() - (days - 1))
  return { from: day(from), to: day(now) }
}

export function useProductStats(days: Period, page: number) {
  const { from, to } = periodRange(days)
  return useQuery({
    queryKey: ['admin', 'stats', 'products', from, to, page],
    queryFn: () => call(api.GET('/api/admin/stats/products', { params: { query: { from, to, page, pageSize: 10 } } })),
  })
}
