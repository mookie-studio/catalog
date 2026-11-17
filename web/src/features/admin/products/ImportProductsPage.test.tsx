import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { adminSession } from '@/test/admin'
import { json, mockApi, requestsTo } from '@/test/api'
import { renderApp } from '@/test/render'

vi.mock('@/features/auth/session', () => import('@/test/session'))

const previewReport = {
  created: 1,
  updated: 1,
  skipped: 1,
  warnings: ["Unknown column 'Weight' was ignored."],
  rows: [
    { row: 1, sku: 'HD-1', outcome: 'Update', errors: [] },
    { row: 2, sku: 'NEW-1', outcome: 'Create', errors: [] },
    { row: 3, sku: 'BAD-1', outcome: 'Skip', errors: [{ column: 'Color', message: "'Green' is not an Option of Color." }] },
  ],
}

function importApi() {
  mockApi({
    ...adminSession,
    'POST /api/admin/products/import': (url: URL) =>
      url.searchParams.get('dryRun') === 'true' ? json(previewReport) : json({ ...previewReport, warnings: [] }),
  })
}

const csv = () => new File(['sku,name\nHD-1,Drill\n'], 'products.csv', { type: 'text/csv' })

describe('product import', () => {
  it('previews the file with outcomes, errors and warnings, then applies it', async () => {
    importApi()
    renderApp('/admin/products/import')

    await userEvent.upload(await screen.findByLabelText('CSV file'), csv())

    const preview = await screen.findByRole('region', { name: 'Import preview' })
    expect(within(preview).getByText('Preview: 1 to create, 1 to update, 1 skipped.')).toBeInTheDocument()
    const rows = within(preview).getAllByRole('row')
    expect(within(rows[3]).getByText('Skip')).toBeInTheDocument()
    expect(within(rows[3]).getByText(/'Green' is not an Option of Color\./)).toBeInTheDocument()
    expect(within(preview).getByRole('list', { name: 'Warnings' })).toHaveTextContent("Unknown column 'Weight' was ignored.")
    expect(requestsTo('/api/admin/products/import', 'POST')).toHaveLength(1)

    await userEvent.click(within(preview).getByRole('button', { name: 'Apply import' }))

    expect(await screen.findByText('Import applied: 1 created, 1 updated, 1 skipped.')).toBeInTheDocument()
    const calls = requestsTo('/api/admin/products/import', 'POST').map((r) => new URL(r.url).searchParams.get('dryRun'))
    expect(calls).toEqual(['true', 'false'])
  })

  it('offers no Apply button before a preview', async () => {
    importApi()
    renderApp('/admin/products/import')

    await screen.findByLabelText('CSV file')
    expect(screen.queryByRole('button', { name: 'Apply import' })).not.toBeInTheDocument()
  })
})
