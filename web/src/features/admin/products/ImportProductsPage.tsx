import { useMutation, useQueryClient } from '@tanstack/react-query'
import { type ChangeEvent, useState } from 'react'
import { Link } from 'react-router'
import { api, type components } from '@/api/client'
import { ApiProblem, call } from '@/api/problem'
import { Button, ButtonLink } from '@/components/ui/button'
import { cn } from '@/lib/utils'

type Report = components['schemas']['ImportProductsReport']

function importCsv(file: File, dryRun: boolean) {
  const body = new FormData()
  body.append('file', file)
  return call(api.POST('/api/admin/products/import', { params: { query: { dryRun } }, body: body as never }))
}

/** Product import: preview a CSV (dry run), then apply it. */
export function ImportProductsPage() {
  const queryClient = useQueryClient()
  const [file, setFile] = useState<File | null>(null)
  const preview = useMutation({ mutationFn: (f: File) => importCsv(f, true) })
  const apply = useMutation({
    mutationFn: (f: File) => importCsv(f, false),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin', 'products'] }),
  })
  const error = apply.error ?? preview.error
  const errorMessage = error instanceof ApiProblem ? (Object.values(error.fields)[0] ?? error.message) : error?.message

  function choose(event: ChangeEvent<HTMLInputElement>) {
    const chosen = event.target.files?.[0] ?? null
    event.target.value = ''
    setFile(chosen)
    apply.reset()
    preview.reset()
    if (chosen) preview.mutate(chosen)
  }

  return (
    <main className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <Link to="/admin/products" className="text-sm text-muted-foreground hover:underline">
          ← Products
        </Link>
        <h1 className="text-2xl font-semibold tracking-tight">Import products</h1>
      </div>
      <div className="space-y-2 text-sm text-muted-foreground">
        <p>
          Use the same format as <strong>Export CSV</strong>: the first row has the column names <code>sku</code> and <code>name</code>,
          optionally <code>description</code>, <code>featured</code> (true or false), <code>categories</code> (paths such as{' '}
          <code>Tools &gt; Drills</code>, several joined by <code>|</code>) and one column per Attribute named exactly like it.
        </p>
        <p>
          Rows are matched by SKU: an existing product is updated, a new SKU creates a Draft. An empty cell clears that field. Images are not
          imported. Nothing changes until you apply the import.
        </p>
      </div>
      <div>
        <label className="inline-flex cursor-pointer items-center gap-2">
          <input type="file" accept=".csv,text/csv" className="sr-only" aria-label="CSV file" onChange={choose} />
          <span className="inline-flex h-9 items-center rounded-md border px-4 text-sm font-medium shadow-xs hover:bg-accent">Choose CSV file</span>
          {file && <span className="text-sm text-muted-foreground">{file.name}</span>}
        </label>
      </div>
      {preview.isPending && <p role="status" className="text-muted-foreground">Checking the file…</p>}
      {errorMessage && (
        <p role="alert" className="text-sm text-destructive">
          {errorMessage}
        </p>
      )}
      {apply.data ? (
        <section aria-label="Import result" className="flex flex-col gap-4">
          <p role="status" className="font-medium">
            Import applied: {apply.data.created} created, {apply.data.updated} updated, {apply.data.skipped} skipped.
          </p>
          <ImportTable report={apply.data} />
          <div>
            <ButtonLink to="/admin/products">Back to products</ButtonLink>
          </div>
        </section>
      ) : (
        preview.data && (
          <section aria-label="Import preview" className="flex flex-col gap-4">
            <p className="font-medium">
              Preview: {preview.data.created} to create, {preview.data.updated} to update, {preview.data.skipped} skipped.
            </p>
            <ImportTable report={preview.data} />
            <div>
              <Button disabled={!file || apply.isPending || preview.data.created + preview.data.updated === 0} onClick={() => file && apply.mutate(file)}>
                Apply import
              </Button>
            </div>
          </section>
        )
      )}
    </main>
  )
}

const outcomeLabels = { Create: 'Create', Update: 'Update', Skip: 'Skip' } as const

/** Rows of an import report; skipped rows are highlighted with their errors. */
export function ImportTable({ report }: { report: Report }) {
  return (
    <>
      {report.warnings.length > 0 && (
        <ul aria-label="Warnings" className="list-disc pl-5 text-sm text-warning">
          {report.warnings.map((warning) => (
            <li key={warning}>{warning}</li>
          ))}
        </ul>
      )}
      <div className="overflow-x-auto rounded-lg border">
        <table className="w-full text-sm">
          <thead className="bg-muted/50 text-left text-muted-foreground">
            <tr>
              <th className="px-3 py-2 font-medium">Row</th>
              <th className="px-3 py-2 font-medium">SKU</th>
              <th className="px-3 py-2 font-medium">Outcome</th>
              <th className="px-3 py-2 font-medium">Errors</th>
            </tr>
          </thead>
          <tbody>
            {report.rows.map((row) => (
              <tr key={row.row} className={cn('border-t', row.outcome === 'Skip' && 'bg-destructive/5')}>
                <td className="px-3 py-2">{row.row}</td>
                <td className="px-3 py-2 font-mono text-xs">{row.sku ?? '—'}</td>
                <td className={cn('px-3 py-2', row.outcome === 'Skip' && 'font-medium text-destructive')}>{outcomeLabels[row.outcome]}</td>
                <td className="px-3 py-2">
                  {row.errors.length > 0 && (
                    <ul>
                      {row.errors.map((error, index) => (
                        <li key={index}>
                          <span className="font-mono text-xs">{error.column}</span>: {error.message}
                        </li>
                      ))}
                    </ul>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}
