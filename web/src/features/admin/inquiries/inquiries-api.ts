import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { api, type components } from '@/api/client'
import { call } from '@/api/problem'

export type Inquiry = components['schemas']['InquiryResponse']
export type InquiryStatus = components['schemas']['InquiryStatus']

export const inquiriesQueryKey = ['admin', 'inquiries'] as const

export function useInquiries(status: InquiryStatus | null, page: number) {
  return useQuery({
    queryKey: [...inquiriesQueryKey, status, page],
    queryFn: () => call(api.GET('/api/admin/inquiries', { params: { query: { status: status ?? undefined, page } } })),
    placeholderData: keepPreviousData,
  })
}

/** How many Inquiries are Open: the list's totalCount with a page of one. */
export function useOpenInquiryCount() {
  return useQuery({
    queryKey: [...inquiriesQueryKey, 'open-count'],
    queryFn: async () =>
      (await call(api.GET('/api/admin/inquiries', { params: { query: { status: 'Open', pageSize: 1 } } }))).totalCount,
  })
}
