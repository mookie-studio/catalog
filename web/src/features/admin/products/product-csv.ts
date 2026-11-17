import { api } from '@/api/client'
import { toProblem } from '@/api/problem'

/** Downloads every product as products.csv (the request carries the Admin's token, so a plain link would not work). */
export async function downloadProductsCsv() {
  const { data, error, response } = await api.GET('/api/admin/products/export', { parseAs: 'blob' })
  if (!response.ok || !data) throw toProblem(error, response)
  const url = URL.createObjectURL(data as Blob)
  const link = document.createElement('a')
  link.href = url
  link.download = 'products.csv'
  document.body.append(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}
